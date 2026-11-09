$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
$frameworkRoot = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319'
if (-not (Test-Path (Join-Path $frameworkRoot 'csc.exe'))) { $frameworkRoot = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319' }
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$payloadPath = Join-Path $projectRoot 'build\installer-payload.zip'
if(Test-Path -LiteralPath $payloadPath){Remove-Item -LiteralPath $payloadPath}
$files = @('MacBreeze.exe','MacBreeze.exe.config','smccore.exe','inpout32.dll','AppleT2Smc.bin','setup-pawnio-t2.ps1','README.txt','LICENSE.txt','NOTICE.txt','THIRD-PARTY.md','LICENSE-InpOut32.txt','LICENSE-AppleT2Smc.txt')
$archive = [System.IO.Compression.ZipFile]::Open($payloadPath,[System.IO.Compression.ZipArchiveMode]::Create)
try { foreach($file in $files) { [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($archive,(Join-Path $projectRoot ('build\'+$file)),$file) | Out-Null } } finally { $archive.Dispose() }
$setupPath = Join-Path $projectRoot 'build\MacBreeze-2.1-Setup.exe'
& (Join-Path $frameworkRoot 'csc.exe') /nologo /target:winexe /platform:x86 "/out:$setupPath" "/resource:$payloadPath,payload.zip" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.IO.Compression.dll /reference:System.IO.Compression.FileSystem.dll (Join-Path $PSScriptRoot 'Setup.cs')
if($LASTEXITCODE -ne 0){throw 'Installer build failed'}
$verification = Start-Process -FilePath $setupPath -ArgumentList '/verify' -Wait -PassThru -WindowStyle Hidden
if($verification.ExitCode -ne 0){throw 'Installer payload check failed'}
Write-Output $setupPath
