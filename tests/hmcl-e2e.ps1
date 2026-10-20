param([Parameter(Mandatory=$true)][string]$JavaHome,[Parameter(Mandatory=$true)][string]$HmclJar,[string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=Split-Path $PSScriptRoot -Parent
$output=if($OutputDirectory){[IO.Path]::GetFullPath($OutputDirectory)}else{Join-Path $root '.test-data\e2e'}
$artifact=(Resolve-Path -LiteralPath $HmclJar).Path
$javafx=Join-Path $output 'javafx'
New-Item -ItemType Directory -Force $javafx | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archive=[IO.Compression.ZipFile]::OpenRead($artifact)
try{$reader=[IO.StreamReader]::new($archive.GetEntry('assets/openjfx-dependencies.json').Open());try{$dependencies=($reader.ReadToEnd() | ConvertFrom-Json).'windows-x86_64'.classic}finally{$reader.Dispose()}}finally{$archive.Dispose()}
foreach($dependency in $dependencies){
 $name="$($dependency.artifactId)-$($dependency.version)-$($dependency.classifier).jar"
 $file=Join-Path $javafx $name
 if(-not(Test-Path -LiteralPath $file)){Invoke-WebRequest "https://repo.maven.apache.org/maven2/org/openjfx/$($dependency.artifactId)/$($dependency.version)/$name" -OutFile $file}
 if((Get-FileHash -LiteralPath $file -Algorithm SHA1).Hash -ne $dependency.sha1){throw "JavaFX hash mismatch: $name"}
}
$classpath="$artifact;$javafx\*;$output"
& "$JavaHome\bin\javac.exe" -cp $classpath -d $output "$PSScriptRoot\HmclE2e.java"
if($LASTEXITCODE -ne 0){throw 'HMCL e2e compilation failed'}
function Invoke-HmclTest([string]$Label,[string[]]$VmOptions,[string[]]$TestArguments){
 & "$JavaHome\bin\java.exe" '--enable-native-access=ALL-UNNAMED' @VmOptions '-cp' $classpath HmclE2e @TestArguments 2>&1 | Tee-Object -FilePath (Join-Path $output "$Label.log")
 if($LASTEXITCODE -ne 0){throw "HMCL e2e failed: $Label"}
}
Invoke-HmclTest 'hmcl-page' @('-Duser.timezone=Asia/Taipei','-Duser.country=CN') @('page',(Join-Path $output 'hmcl-page'),'false')
$listener=[Net.Sockets.TcpListener]::new([Net.IPAddress]::Loopback,0);$listener.Start();$unusedPort=$listener.LocalEndpoint.Port;$listener.Stop()
Invoke-HmclTest 'hmcl-page-restricted' @('-Duser.timezone=Asia/Shanghai','-Duser.country=CN','-Dhttps.proxyHost=127.0.0.1',"-Dhttps.proxyPort=$unusedPort") @('page',(Join-Path $output 'hmcl-page-restricted'),'true')
Invoke-HmclTest 'hmcl-install' @('-Duser.timezone=Asia/Taipei') @('install',(Join-Path $output 'hmcl'),'1.12.2')
Invoke-HmclTest 'hmcl-modern' @('-Duser.timezone=Asia/Taipei') @('install',(Join-Path $output 'hmcl-modern'),'1.20.1')
