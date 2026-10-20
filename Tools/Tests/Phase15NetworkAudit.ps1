param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory,[string]$LegacyExecutable)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Use a new output directory'}
New-Item -ItemType Directory $OutputDirectory|Out-Null
$script:request=3000
function WriteCommand($path,$content){$deadline=[DateTime]::UtcNow.AddSeconds(2);do{try{[IO.File]::AppendAllText($path,$content);return}catch [IO.IOException]{if([DateTime]::UtcNow -ge $deadline){throw};Start-Sleep -Milliseconds 10}}while($true)}
function Rows($role){$p=Join-Path $OutputDirectory "$role.jsonl";if(Test-Path $p){@(Get-Content $p -Tail 600|ForEach-Object{try{$_|ConvertFrom-Json}catch{}})}else{@()}}
function Latest($role){@(Rows $role)[-1]}
function Check([bool]$ok,$name){$line="$(if($ok){'PASS'}else{'FAIL'}) $name";Add-Content (Join-Path $OutputDirectory 'Results.txt') $line;Write-Output $line;if(!$ok){throw $name}}
function WaitRow($role,[scriptblock]$test,[int]$seconds=20){$end=[DateTime]::UtcNow.AddSeconds($seconds);do{$r=Latest $role;if($r -and (& $test $r)){return $r};Start-Sleep -Milliseconds 60}while([DateTime]::UtcNow -lt $end);throw "Timeout $role $test"}
function Send($role,$op,[hashtable]$values=@{}){$script:request++;$d=@{id=$script:request;op=$op};foreach($k in $values.Keys){$d[$k]=$values[$k]};WriteCommand (Join-Path $OutputDirectory "$role.commands.jsonl") (($d|ConvertTo-Json -Compress)+"`r`n");$id=$script:request;$end=[DateTime]::UtcNow.AddSeconds(8);do{$r=@(Rows $role|Where-Object{$_.request -eq $id -and $_.kind -in 'command','exception'})|Select-Object -Last 1;if($r){return $r};Start-Sleep -Milliseconds 40}while([DateTime]::UtcNow -lt $end);throw "Command timeout $role $op"}
function Peer($path,$role){Start-Process -FilePath $path -ArgumentList @('-batchmode','-force-d3d11','-screen-width','640','-screen-height','360','-phase11c',$role,'-audit-dir',$OutputDirectory,'-logFile',"$OutputDirectory\$role.log") -WindowStyle Hidden -PassThru}
$hProcess=Peer $Executable host;$cProcess=Peer $Executable client
@{HostPid=$hProcess.Id;ClientPid=$cProcess.Id;Executable=$Executable}|ConvertTo-Json|Set-Content (Join-Path $OutputDirectory 'Processes.json')
try{
 $h=WaitRow host {param($r)$r.count -eq 2 -and $r.state -eq 'TurnActive'};$c=WaitRow client {param($r)$r.slot -eq 1 -and $r.frame -gt 0}
 Check ($h.slot -eq 0 -and $c.slot -eq 1) 'v3 HostP0 and ClientP1 remain deterministic'
 $c=WaitRow client {param($r)[Math]::Abs($r.displayAimOne-$r.aimOne) -lt .05}; $before=$c.displayAimOne;$r=Send client aimpower @{x=-.5;y=.8660254;power=.42};Check ($r.detail -eq 'aimpower:False' -and [Math]::Abs($r.displayAimOne-$before) -lt .1) 'wrong-turn aim does not anticipate as accepted input'
 if($LegacyExecutable){$old=Peer $LegacyExecutable legacy;Start-Sleep -Seconds 3;$r=Latest legacy;Check (!$r.connected -and $r.slot -eq -1) 'v2 peer cannot connect to v3 session';Check ([bool](Select-String -LiteralPath "$OutputDirectory\host.log" -Pattern 'NetworkConfig mismatch' -Quiet)) 'explicit NGO compatibility rejects v2';$null=Send legacy quit}
 $null=Send host powerfire @{x=0;y=1;power=1};$null=WaitRow client {param($r)$r.player -eq 1 -and $r.state -eq 'TurnActive'}
 $r=Send client aimpower @{x=-.5;y=.8660254;power=.42}
 Check ($r.detail -eq 'aimpower:True' -and [Math]::Abs($r.displayAimOne-60) -lt .1) 'local mirrored barrel visual anticipates in same command callback'
 Check ([Math]::Abs($r.actualAimOne-$r.displayAimOne) -gt 1) 'anticipation leaves authoritative barrel pivot untouched'
 $h=WaitRow host {param($r)[Math]::Abs($r.aimOne-60) -lt .1 -and [Math]::Abs($r.powerOne-.42) -lt .001}
 Check ($h.localHealthZero -eq $h.healthZero -and $h.localHealthOne -eq $h.healthOne) 'aim intent does not change damage authority'
 $null=Send client aimcommit @{x=-.7071068;y=.7071068;power=.31};$h=WaitRow host {param($r)[Math]::Abs($r.aimOne-45) -lt .1 -and [Math]::Abs($r.powerOne-.31) -lt .001};Start-Sleep -Milliseconds 500
 Check ([Math]::Abs((Latest host).powerOne-.31) -lt .001) 'reliable release commit retains final aim and power'
 $null=Send client rawaim @{x=-.3420201;y=.9396926;power=.55;sequence=10000;player=1};$null=WaitRow host {param($r)[Math]::Abs($r.aimOne-70) -lt .1}
 $null=Send client rawaim @{x=-.9396926;y=.3420201;power=.2;sequence=9999;player=1};$null=Send client rawaim @{x=-.9396926;y=.3420201;power=.2;sequence=10000;player=1};Start-Sleep -Milliseconds 300
 Check ([Math]::Abs((Latest host).aimOne-70) -lt .1 -and [Math]::Abs((Latest host).powerOne-.55) -lt .001) 'out-of-order and duplicate disposable aims cannot overwrite newer intent'
 Check (@(Rows client|Where-Object{$_.kind -eq 'ack' -and $_.detail.EndsWith(':Stale')}).Count -ge 2) 'stale aim drops acknowledged without reliable backlog'
 $null=Send host impair @{x=100};$null=Send client move @{x=-.25};Start-Sleep -Milliseconds 950;$x=(Latest host).one.x;Start-Sleep -Milliseconds 500
 Check ([Math]::Abs((Latest host).one.x-$x) -lt .1) 'missed Stop under delay loss duplication expires movement lease'
 $r=Send client rapidfire @{x=-.6;y=.8;power=.37};Check ($r.detail -eq 'rapidfire:True') 'rapid drag plus Fire queues final intent reliably without waiting for aim datagram'
 $deadline=[DateTime]::UtcNow.AddSeconds(6);do{$flight=@(Rows host|Where-Object{$_.active -and $_.shot -eq 2})|Select-Object -First 1;if($flight){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow -lt $deadline)
 $speed=12*(.25+.75*.37)
 Check ($flight.owner -eq 1 -and [Math]::Abs($flight.velocity.x+$speed*.6) -lt .03 -and [Math]::Abs($flight.velocity.y-$speed*.8) -lt .03) 'Host projectile uses exact reliable final world direction power and owner'
 $oldSession=$flight.session;$oldTurn=$flight.turn;$null=Send client rawaim @{x=-1;y=0;power=0;sequence=10001;player=1;session=$oldSession;turn=$oldTurn};$null=Send client powerfire @{x=-.6;y=.8;power=.37}
 $h=WaitRow host {param($r)$r.completed -eq 2 -and $r.state -eq 'TurnActive'}
 Check ($h.shot -eq 2) 'late aim and duplicate Fire create no additional projectile or turn advance'
 $null=Send host impair @{x=0};$null=Send host end;$null=Send host restart;$null=WaitRow client {param($r)$r.state -eq 'TurnActive' -and $r.player -eq 0 -and $r.healthZero -eq 100 -and $r.healthOne -eq 100}
 $null=Send client rawaim @{x=-1;y=0;power=0;sequence=20000;player=1;session=$oldSession;turn=$oldTurn};Start-Sleep -Milliseconds 250
 Check ((Latest host).session -ne $oldSession -and (Latest host).player -eq 0 -and (Latest host).shot -eq 0) 'restart rejects old-generation aim and clears flight history'
 $before=(Latest client).displayAimOne;$r=Send client aimpower @{x=-1;y=0;power=0};Check ($r.detail -eq 'aimpower:False' -and [Math]::Abs($r.displayAimOne-$before) -lt .1) 'wrong-turn post-restart aim cannot animate'
 $null=Send host sweep @{duration=2};Start-Sleep -Milliseconds 300;$null=Send client disconnect;$h=WaitRow host {param($r)$r.aborted -and $r.state -eq 'MatchEnded'}
 Check (!$h.canAct) 'disconnect during aim aborts session and stops control'
}finally{foreach($role in 'legacy','client','host'){if(Test-Path (Join-Path $OutputDirectory "$role.jsonl")){try{$null=Send $role quit}catch{}}}}
