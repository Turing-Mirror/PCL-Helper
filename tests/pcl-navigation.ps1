param([Parameter(Mandatory=$true)][string]$PclPath,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $root ('.test-data\navigation-'+[Guid]::NewGuid().ToString('N'))}
New-Item -ItemType Directory -Force $output | Out-Null
$target=Join-Path $output 'PCL.exe'
Copy-Item -LiteralPath (Resolve-Path -LiteralPath $PclPath).Path -Destination $target
$framework=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
$runner=Join-Path $output 'PclModpackE2e.exe'
& "$framework\csc.exe" /nologo "/out:$runner" "/reference:$framework\WPF\PresentationFramework.dll" "/reference:$framework\WPF\PresentationCore.dll" "/reference:$framework\WPF\WindowsBase.dll" "/reference:$framework\System.Xaml.dll" "$PSScriptRoot\PclModpackE2e.cs"
if($LASTEXITCODE -ne 0){throw 'PCL navigation compilation failed'}
'<configuration><startup><supportedRuntime version="v4.0" sku=".NETFramework,Version=v4.8"/></startup></configuration>' | Set-Content -LiteralPath "$runner.config" -Encoding UTF8
& $runner $target (Join-Path $output 'restricted') navigation false 2>&1 | Tee-Object -FilePath (Join-Path $output 'restricted.log')
if($LASTEXITCODE -ne 0){throw 'Original PCL navigation contrast failed'}
& "$root\scripts\build.ps1" -Console | Out-Null
& "$root\.test-data\PCL-Helper-CLI.exe" --enable $target
if($LASTEXITCODE -ne 0){throw 'PCL navigation patch failed'}
& $runner $target (Join-Path $output 'restored') navigation true 2>&1 | Tee-Object -FilePath (Join-Path $output 'restored.log')
if($LASTEXITCODE -ne 0){throw 'Restored PCL navigation failed'}
Write-Output "Evidence: $output"
