param([string]$UnityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.5.10f1/Editor')
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$mono = Join-Path $UnityEditor 'Data/MonoBleedingEdge/bin/mono.exe'
$compiler = Join-Path $UnityEditor 'Data/MonoBleedingEdge/lib/mono/4.5/csc.exe'
$output = Join-Path $root 'TestOutput'
New-Item -ItemType Directory -Force $output | Out-Null
$sources = @(Get-ChildItem "$root/Runtime" -Filter *.cs | ForEach-Object FullName)
& $mono $compiler /nologo /langversion:8 /warnaserror+ /target:exe "/out:$output/GuardTests.exe" @sources "$PSScriptRoot/GuardTests.cs"
if ($LASTEXITCODE) { throw 'Compilation failed' }
& $mono "$output/GuardTests.exe"
if ($LASTEXITCODE) { throw 'Anti-cheat checks failed' }
