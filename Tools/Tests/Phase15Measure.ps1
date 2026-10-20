param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory,[switch]$Optimized)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Use a new isolated output directory'}
New-Item -ItemType Directory $OutputDirectory|Out-Null
$script:request=1500
function Rows($role){$p=Join-Path $OutputDirectory "$role.jsonl";if(Test-Path $p){@(Get-Content $p -Tail 500|ForEach-Object{try{$_|ConvertFrom-Json}catch{}})}else{@()}}
function Latest($role){@(Rows $role)[-1]}
function Metrics($role){$p=Join-Path $OutputDirectory "$role.metrics.jsonl";if(Test-Path $p){Get-Content $p -Tail 1|ConvertFrom-Json}}
function WaitRow($role,[scriptblock]$test,[int]$seconds=20){$end=[DateTime]::UtcNow.AddSeconds($seconds);do{$r=Latest $role;if($r -and (& $test $r)){return $r};Start-Sleep -Milliseconds 80}while([DateTime]::UtcNow -lt $end);throw "Timeout $role $test"}
function Send($role,$op,[hashtable]$values=@{}){$script:request++;$d=@{id=$script:request;op=$op};foreach($k in $values.Keys){$d[$k]=$values[$k]};[IO.File]::AppendAllText((Join-Path $OutputDirectory "$role.commands.jsonl"),($d|ConvertTo-Json -Compress)+"`r`n");$id=$script:request;$end=[DateTime]::UtcNow.AddSeconds(8);do{$r=@(Rows $role|Where-Object{$_.request -eq $id -and $_.kind -in 'command','exception'})|Select-Object -Last 1;if($r){if($r.kind -eq 'exception'){throw $r.detail};return $r};Start-Sleep -Milliseconds 40}while([DateTime]::UtcNow -lt $end);throw "No response $role $op"}
function StartPeer($role){Start-Process -FilePath $Executable -ArgumentList @('-batchmode','-force-d3d11','-screen-width','640','-screen-height','360','-phase11c',$role,'-audit-dir',$OutputDirectory,'-phase12-discovery','-phase15','-logFile',"$OutputDirectory\$role.log") -WindowStyle Hidden -PassThru}
function DeltaMean($a,$b,$mean,$count){$n=$b.$count-$a.$count;if($n -gt 0){($b.$mean*$b.$count-$a.$mean*$a.$count)/$n}else{-1}}
$report=[Collections.Generic.List[object]]::new()
function MeasureCase($label,$role,[int]$seconds=6){
 $h0=Metrics host;$c0=Metrics client;$null=Send $role sweep @{duration=$seconds};Start-Sleep -Seconds ($seconds+1);$h=Metrics host;$c=Metrics client
 $dt=$c.seconds-$c0.seconds;$dh=$h.seconds-$h0.seconds
 $row=[ordered]@{Case=$label;Role=$role;HostFps=($h.frames-$h0.frames)/$dh;ClientFps=($c.frames-$c0.frames)/$dt;Tick=$c.tick;RttMs=$c.rttMs;HostAimRate=($h.aimProcessed-$h0.aimProcessed)/$dh;ClientAimSendRate=($c.aimSent-$c0.aimSent)/$dt;SnapshotSendHz=($h.snapshotsSent-$h0.snapshotsSent)/$dh;SnapshotReceiveHz=($c.snapshotsReceived-$c0.snapshotsReceived)/$dt;AimObservationHz=($c.aimObservations-$c0.aimObservations)/$dt;AckMs=(DeltaMean $c0 $c ackMs acknowledgements);StateEchoMs=(DeltaMean $c0 $c observationMs matchedObservations);Matched=$c.matchedObservations-$c0.matchedObservations;HostPayloadBytesPerSecond=($h.payloadBytes-$h0.payloadBytes)/$dh;ClientPayloadBytesPerSecond=($c.payloadBytes-$c0.payloadBytes)/$dt;RemoteVisualChangesPerSecond=$(if($role -eq 'host'){($c.visualChanges-$c0.visualChanges)/$dt}else{($h.visualChanges-$h0.visualChanges)/$dh});ReceiveAgeMs=$c.visualAgeMs;LocalAnticipations=$c.localAnticipations-$c0.localAnticipations;RemoteStepRms=$(if($role -eq 'host'){[Math]::Sqrt([Math]::Max([double]0,($c.visualStepSquared-$c0.visualStepSquared)/($c.frames-$c0.frames)))}else{[Math]::Sqrt([Math]::Max([double]0,($h.visualStepSquared-$h0.visualStepSquared)/($h.frames-$h0.frames)))});StaleAim=$h.staleAim-$h0.staleAim}
 $report.Add([pscustomobject]$row);$report|ConvertTo-Json -Depth 5|Set-Content (Join-Path $OutputDirectory 'Measurements.json');[pscustomobject]$row|Format-List|Out-String|Write-Output
}
$hostPeer=StartPeer host;$clientPeer=StartPeer client
@{HostPid=$hostPeer.Id;ClientPid=$clientPeer.Id;Executable=$Executable}|ConvertTo-Json|Set-Content (Join-Path $OutputDirectory 'Processes.json')
try{
 $null=WaitRow host {param($r)$r.connected -and $r.count -eq 2 -and $r.state -eq 'TurnActive'};$null=WaitRow client {param($r)$r.slot -eq 1 -and $r.frame -gt 0};Start-Sleep -Seconds 1
 MeasureCase 'Host-direct-10Hz' host
 $null=Send host powerfire @{x=0;y=1;power=1};$null=WaitRow client {param($r)$r.player -eq 1 -and $r.state -eq 'TurnActive'}
 if($Optimized){foreach($config in @(@{name='A-direct';delay=0;anticipation=0;rate=20},@{name='B-short-interpolation';delay=.035;anticipation=0;rate=20},@{name='C-anticipation-interpolation';delay=.035;anticipation=1;rate=20},@{name='C-30Hz-candidate';delay=.035;anticipation=1;rate=30})){foreach($peer in 'host','client'){$null=Send $peer aimconfig @{x=$config.rate;y=$config.delay;power=$config.anticipation}};MeasureCase $config.name client};foreach($peer in 'host','client'){$null=Send $peer aimconfig @{x=20;y=.035;power=1}}}else{foreach($rate in @(10,20,30)){$null=Send host snaprate @{x=$rate};MeasureCase "Client-snapshots-$rate" client}}
 foreach($angle in @(20,35,50,65,80)){$rad=$angle*[Math]::PI/180;$null=Send client aimpower @{x=-[Math]::Cos($rad);y=[Math]::Sin($rad);power=.5};Start-Sleep -Milliseconds 350}
 $null=Send host snaprate @{x=10};$null=Send host impair @{x=100};MeasureCase 'Client-100ms20jitter5loss5dup' client 8
 $null=Send host impair @{x=0};Start-Sleep -Seconds 1
 $null=Send client powerfire @{x=-.7071068;y=.7071068;power=.5};$null=WaitRow host {param($r)$r.shot -eq 2};'PASS reliable final Fire after aim sweep'|Set-Content (Join-Path $OutputDirectory 'Results.txt')
}finally{foreach($role in 'client','host'){try{$null=Send $role quit}catch{}}}
