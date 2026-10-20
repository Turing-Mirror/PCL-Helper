param([Parameter(Mandatory=$true)][string]$JavaPath,[Parameter(Mandatory=$true)][string]$HmclJar)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$evidence=Join-Path $root '.test-data\verification-v2'
New-Item -ItemType Directory -Force $evidence | Out-Null
& $JavaPath '-Duser.timezone=Asia/Taipei' '-Duser.country=CN' '-Duser.language=zh' '-cp' $HmclJar (Join-Path $PSScriptRoot 'HmclRegionProbe.java') | Tee-Object -FilePath (Join-Path $evidence 'hmcl-region.txt')
if($LASTEXITCODE -ne 0){throw 'Actual HMCL region verification failed'}
