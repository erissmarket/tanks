param([Parameter(Mandatory=$true)][string]$Executable,[Parameter(Mandatory=$true)][string]$OutputDirectory,[string]$LegacyExecutable)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Use a new isolated output directory.'}
New-Item -ItemType Directory -Path $OutputDirectory | Out-Null
$script:request=1000
$report=Join-Path $OutputDirectory 'Results.txt'
function Rows($role){$p=Join-Path $OutputDirectory "$role.jsonl";if(Test-Path $p){@(Get-Content $p -Tail 600|ForEach-Object{try{$_|ConvertFrom-Json}catch{}})}else{@()}}
function Latest($role){@(Rows $role)[-1]}
function Check([bool]$ok,$name){$line="$(if($ok){'PASS'}else{'FAIL'}) $name";[IO.File]::AppendAllText($report,$line+"`r`n");Write-Output $line;if(!$ok){throw $name}}
function WaitRow($role,[scriptblock]$predicate,[int]$seconds=20){$end=[DateTime]::UtcNow.AddSeconds($seconds);do{$r=Latest $role;if($r -and (& $predicate $r)){return $r};Start-Sleep -Milliseconds 80}while([DateTime]::UtcNow -lt $end);throw "Timeout $role $predicate"}
function Send($role,$op,[hashtable]$values=@{}){$script:request++;$d=@{id=$script:request;op=$op};foreach($key in $values.Keys){$d[$key]=$values[$key]};[IO.File]::AppendAllText((Join-Path $OutputDirectory "$role.commands.jsonl"),($d|ConvertTo-Json -Compress)+"`r`n");$id=$script:request;$end=[DateTime]::UtcNow.AddSeconds(8);do{$r=@(Rows $role|Where-Object{$_.request -eq $id -and $_.kind -in 'command','exception'})|Select-Object -Last 1;if($r){return $r};Start-Sleep -Milliseconds 80}while([DateTime]::UtcNow -lt $end);throw "Command timeout $role $op"}
function StartPeer($path,$role,[bool]$discovery){$args=@('-batchmode','-force-d3d11','-phase11c',$role,'-audit-dir',$OutputDirectory,'-logFile',"$OutputDirectory\$role.log");if($discovery){$args+='-phase12-discovery'};Start-Process -FilePath $path -ArgumentList $args -WindowStyle Hidden -PassThru}
$hostPeer=StartPeer $Executable 'host' $true
$clientPeer=StartPeer $Executable 'client' $true
@{HostPid=$hostPeer.Id;ClientPid=$clientPeer.Id;Executable=$Executable;Utc=[DateTime]::UtcNow.ToString('o')}|ConvertTo-Json|Set-Content (Join-Path $OutputDirectory 'Processes.json')
try{
 $h=WaitRow host {param($r)$r.connected -and $r.count -eq 2 -and $r.state -eq 'TurnActive'}
 $c=WaitRow client {param($r)$r.slot -eq 1 -and $r.frame -gt 0}
 Check ($h.slot -eq 0 -and $c.slot -eq 1) 'Discovery join preserves playing P0/P1 ownership'
 if($LegacyExecutable){$legacy=StartPeer $LegacyExecutable 'legacy' $false;Start-Sleep -Seconds 3;$r=Latest legacy;Check (!$r.connected -and $r.slot -eq -1) 'Old protocol peer cannot connect or receive ownership';Check ([bool](Select-String -LiteralPath "$OutputDirectory\host.log" -Pattern 'NetworkConfig mismatch' -Quiet)) 'NGO rejects incompatible NetworkConfig before gameplay';$null=Send legacy quit}
 $start=$h.zero.x;$null=Send host move @{x=1};Start-Sleep -Milliseconds 850;$stopped=(Latest host).zero.x;Start-Sleep -Milliseconds 500
 Check ($stopped-$start -gt .05 -and $stopped-$start -lt 1 -and [Math]::Abs((Latest host).zero.x-$stopped) -lt .06) 'Host lost Stop expires lease and remains stopped'
 $null=Send host holdmove @{x=1;duration=3};$h=WaitRow host {param($r)$r.budgetZero -eq 0};$c=WaitRow client {param($r)$r.budgetZero -eq 0}
 Check ([Math]::Abs($h.zero.x-$start) -le 3.12 -and [Math]::Abs($h.zero.x-$c.zero.x) -lt .05) 'Budget limits actual travel and synchronizes to client'
 Start-Sleep -Milliseconds 1000; $stopped=(Latest host).zero.x;$null=Send host move @{x=1};Start-Sleep -Milliseconds 400;Check ([Math]::Abs((Latest host).zero.x-$stopped) -lt .06) 'Repeated Move cannot refill exhausted budget'
 $null=Send host aimpower @{x=.7071068;y=.7071068;power=.5};$c=WaitRow client {param($r)[Math]::Abs($r.powerZero-.5) -lt .001}
 Check ([Math]::Abs($c.actualAimZero-$c.aimZero) -lt .02) 'World joystick aim and power reach host and replica'
 $r=Send host powerfire @{x=0;y=1;power=2};Check ($r.kind -eq 'exception' -and (Latest host).state -eq 'TurnActive') 'Invalid shot power leaves match usable'
 $null=Send host end;$null=Send host restart;$null=WaitRow host {param($r)$r.state -eq 'TurnActive' -and $r.healthZero -eq 100};Start-Sleep -Milliseconds 800
 foreach($shot in @(@{role='host';power=1;owner=0;speed=12},@{role='client';power=.5;owner=1;speed=7.5},@{role='host';power=0;owner=0;speed=3})){
  if($shot.role -eq 'client'){$null=Send client move @{x=-.25};Start-Sleep -Milliseconds 850;$x=(Latest client).one.x;Start-Sleep -Milliseconds 400;Check ([Math]::Abs((Latest client).one.x-$x) -lt .06) 'Remote lost Stop expires authoritative lease';$null=Send client raw @{x=0;y=1;sequence=1};Start-Sleep -Milliseconds 200;Check ([Math]::Abs((Latest client).one.x-$x) -lt .06) 'Delayed stale Move cannot renew lease'}
  $before=Latest host;$shotId=$before.shot+1;$health=$before.healthZero+$before.healthOne
  $r=Send $shot.role powerfire @{x=0;y=1;power=$shot.power};Check ($r.detail -eq 'powerfire:True') "Fire accepted power=$($shot.power)"
  $deadline=[DateTime]::UtcNow.AddSeconds(5);do{$flight=@(Rows host|Where-Object{$_.active -and $_.shot -eq $shotId})|Select-Object -First 1;if($flight){break};Start-Sleep -Milliseconds 50}while([DateTime]::UtcNow -lt $deadline)
  $speed=[Math]::Sqrt($flight.velocity.x*$flight.velocity.x+$flight.velocity.y*$flight.velocity.y)
  Check ($flight.owner -eq $shot.owner -and [Math]::Abs($speed-$shot.speed) -lt .15) "Host launch velocity/owner match power=$($shot.power)"
  $duplicate=Send $shot.role powerfire @{x=0;y=1;power=$shot.power};Check ($duplicate.detail -eq 'powerfire:False') 'Duplicate Fire cannot create another projectile'
  $h=WaitRow host {param($r)$r.completed -eq $shotId -and $r.state -eq 'TurnActive'};$c=WaitRow client {param($r)$r.completed -eq $shotId -and $r.state -eq 'TurnActive'}
  $afterHealth=$h.healthZero+$h.healthOne
  Check ($health-$afterHealth -eq 35 -and $h.healthZero -eq $c.healthZero -and $h.healthOne -eq $c.healthOne) "Actual explosion damage remains35 at power=$($shot.power)"
  Check ($h.budgetZero -eq $c.budgetZero -and $h.budgetOne -eq $c.budgetOne) 'Turn budget observations agree after flight'
 }
 $null=Send client holdmove @{x=-1;duration=3};Start-Sleep -Milliseconds 150;$null=Send client disconnect;$h=WaitRow host {param($r)$r.aborted -and $r.state -eq 'MatchEnded'};$x=$h.one.x;Start-Sleep -Milliseconds 500
 Check ([Math]::Abs((Latest host).one.x-$x) -lt .1 -and !(Latest host).canAct) 'Disconnect ends match and stops remote movement'
}finally{foreach($role in 'legacy','client','host'){if(Test-Path (Join-Path $OutputDirectory "$role.jsonl")){try{$null=Send $role quit}catch{}}}}
