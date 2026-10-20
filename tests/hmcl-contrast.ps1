param([Parameter(Mandatory=$true)][string]$JavaHome,[Parameter(Mandatory=$true)][string]$HmclExe)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=Join-Path $root ('.test-data\hmcl-contrast-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force $output | Out-Null
& "$JavaHome\bin\javac.exe" -d $output "$PSScriptRoot\HmclRegionAgent.java"
if($LASTEXITCODE -ne 0){throw 'Agent compilation failed'}
$manifest=Join-Path $output 'MANIFEST.MF'
"Premain-Class: HmclRegionAgent`n" | Set-Content -LiteralPath $manifest -Encoding ASCII
$agent=Join-Path $output 'region-agent.jar'
& "$JavaHome\bin\jar.exe" --create --file $agent --manifest $manifest -C $output HmclRegionAgent.class
if($LASTEXITCODE -ne 0){throw 'Agent packaging failed'}
$runner=Join-Path $output 'HmclNativeContrastTest.exe'
& "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /nologo "/out:$runner" "$PSScriptRoot\HmclNativeContrastTest.cs" "$root\src\HmclLauncher.cs"
if($LASTEXITCODE -ne 0){throw 'Contrast runner compilation failed'}
$listener=[Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,0)
$listener.Start();$unusedPort=$listener.LocalEndpoint.Port;$listener.Stop()
& $runner (Resolve-Path -LiteralPath $HmclExe).Path $JavaHome $agent $output $unusedPort | Tee-Object -FilePath (Join-Path $output 'contrast.txt')
if($LASTEXITCODE -ne 0){throw "Native HMCL contrast failed. Evidence: $output"}
Write-Output "Evidence: $output"
