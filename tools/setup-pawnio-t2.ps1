<#
  setup-pawnio-t2.ps1 - get a T2 Mac (2018-2020) ready for RPMac

  RPMac reaches the T2's SMC through its own PawnIO module (AppleT2Smc.bin).
  That module is not signed yet, so it only loads in PawnIO's UNRESTRICTED
  edition, and that edition needs Windows test signing. The 2.1.0 and 2.2.0
  "unrestricted" installers still enforce signatures (PawnIO.Setup#11), so this
  script installs 2.0.1, whose unrestricted driver really skips the check.

  What it does (and nothing else):
    1. checks you are admin and that Secure Boot allows test signing
    2. turns test signing on (bcdedit), if it is off
    3. if a different PawnIO driver is installed: uninstalls it and removes its
       cached copies from the DriverStore (otherwise Windows keeps loading it)
    4. downloads PawnIO_setup.exe 2.0.1, verifies its SHA-256, installs it with
       -install -unrestricted
    5. tells you to reboot

  Run it from an ADMINISTRATOR PowerShell:
      powershell -ExecutionPolicy Bypass -File .\setup-pawnio-t2.ps1
  Run it again after the reboot: it only checks and reports.

  Undo:  PawnIO_setup.exe -uninstall   and   bcdedit /set testsigning off
#>

# A 32-bit host (RPMac.exe is x86) starts the 32-bit PowerShell, where bcdedit
# and pnputil do not exist. Re-launch in the native 64-bit PowerShell.
if ([Environment]::Is64BitOperatingSystem -and -not [Environment]::Is64BitProcess) {
    $ps64 = Join-Path $env:windir 'Sysnative\WindowsPowerShell\v1.0\powershell.exe'
    & $ps64 -NoProfile -ExecutionPolicy Bypass -File $PSCommandPath
    exit $LASTEXITCODE
}

$ErrorActionPreference = 'Stop'
try { Start-Transcript -Path (Join-Path $PSScriptRoot 'setup-log.txt') -Force | Out-Null } catch { }
trap { Write-Host "`nFEHLER: $_" -ForegroundColor Red; try { Stop-Transcript | Out-Null } catch { }; Read-Host 'Enter zum Schliessen'; exit 1 }

$Url    = 'https://github.com/namazso/PawnIO.Setup/releases/download/2.0.1/PawnIO_setup.exe'
$Sha256 = '3A34B5DF231F10F252C4B50D3C777534B2A2CEC5E1DF9466AB2DCA401C068C44'
$SysOk  = 75536     # PawnIO.sys size of 2.0.1 UNRESTRICTED (official 2.0.1 = 85344, 2.2.0 = 136504)
$SysPath = Join-Path $env:ProgramFiles 'PawnIO\PawnIO.sys'

function Say($msg, $color = 'Gray') { Write-Host $msg -ForegroundColor $color }
function Fail($msg) { Say "`nSTOP: $msg" Red; try { Stop-Transcript | Out-Null } catch { }; Read-Host 'Enter zum Schließen'; exit 1 }

# ---- 1. admin -------------------------------------------------------------
$id = [Security.Principal.WindowsIdentity]::GetCurrent()
if (-not (New-Object Security.Principal.WindowsPrincipal $id).IsInRole(
        [Security.Principal.WindowsBuiltInRole]::Administrator)) {
    Fail 'Bitte als Administrator ausführen (Rechtsklick -> Als Administrator ausführen).'
}

$cs = Get-CimInstance Win32_ComputerSystem
Say "Gerät: $($cs.Manufacturer) $($cs.Model)" Cyan
if ($cs.Manufacturer -notmatch 'Apple') {
    Say 'Das sieht nicht nach einem Mac aus. PawnIO unrestricted + Testsignatur senken die Sicherheit des PCs,' Yellow
    Say 'und MacBreeze kann auf Nicht-Apple-Hardware ohnehin keine Lüfter steuern.' Yellow
    if ((Read-Host 'Trotzdem fortfahren? (j/N)') -ne 'j') { exit 0 }
}

# ---- already done? --------------------------------------------------------
$size = if (Test-Path $SysPath) { (Get-Item $SysPath).Length } else { 0 }
$driverRunning = $false
try { $driverRunning = ((Get-Service PawnIO -ErrorAction Stop).Status -eq 'Running') } catch { }

# ---- 2. Secure Boot / test signing ----------------------------------------
$secureBoot = $false
try { $secureBoot = Confirm-SecureBootUEFI } catch { }   # throws on BIOS/CSM boot = no Secure Boot
if ($secureBoot) {
    Fail ("Secure Boot ist AN, und dann verweigert Windows die Testsignatur.`n" +
          "Auf einem T2-Mac: macOS-Wiederherstellung starten (Cmd+R) -> Dienstprogramme -> Startsicherheitsdienstprogramm`n" +
          "-> Sicherer Start: 'Keine Sicherheit'. Dann Windows starten und dieses Skript nochmal ausfuehren.")
}

$bcd = (& "$env:windir\System32\bcdedit.exe" /enum '{current}') -join "`n"
$testSigning = $bcd -match '(?im)^\s*testsigning\s+(Yes|Ja|Oui|S.)\s*$'
if (-not $testSigning) {
    Say 'Testsignatur wird eingeschaltet ...' Cyan
    & "$env:windir\System32\bcdedit.exe" /set testsigning on | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail 'bcdedit /set testsigning on ist fehlgeschlagen (ist Secure Boot wirklich aus?).' }
} else {
    Say 'Testsignatur: an' Green
}

if ($size -eq $SysOk) {
    if ($driverRunning -and $testSigning) {
        Say "`nPawnIO 2.0.1 unrestricted ist installiert und läuft. Alles fertig:" Green
        Say 'MacBreeze starten.' Green
    } else {
        Say "`nPawnIO 2.0.1 unrestricted ist installiert. Neu starten, dann MacBreeze öffnen." Yellow
    }
    Read-Host 'Enter zum Schließen'; exit 0
}

# ---- 3. remove a wrong PawnIO ---------------------------------------------
$tmp = Join-Path $env:TEMP 'rpmac-pawnio'
New-Item -ItemType Directory -Force -Path $tmp | Out-Null
$setup = Join-Path $tmp 'PawnIO_setup_2.0.1.exe'

Say 'PawnIO 2.0.1 wird heruntergeladen ...' Cyan
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
Invoke-WebRequest -Uri $Url -OutFile $setup -UseBasicParsing
$got = (Get-FileHash $setup -Algorithm SHA256).Hash
if ($got -ne $Sha256) { Remove-Item $setup -Force; Fail "Prüfsumme des Downloads stimmt nicht ($got). Es wird nichts installiert." }
Say 'SHA-256 geprüft.' Green

if ($size -ne 0) {
    Say "Ein anderer PawnIO-Treiber ist installiert (PawnIO.sys = $size Bytes). Wird entfernt ..." Cyan
    Start-Process $setup -ArgumentList '-uninstall' -Wait
}
# Language-neutral (pnputil output is localized; this is not).
$cached = Get-WindowsDriver -Online | Where-Object { $_.OriginalFileName -like '*\pawnio.inf' }
foreach ($d in $cached) {
    Say "  entferne zwischengespeicherten Treiber $($d.Driver)" Gray
    & "$env:windir\System32\pnputil.exe" /delete-driver $d.Driver /uninstall /force | Out-Null
}

# ---- 4. install -----------------------------------------------------------
Say 'PawnIO 2.0.1 (unrestricted) wird installiert ...' Cyan
$p = Start-Process $setup -ArgumentList '-install -unrestricted' -Wait -PassThru
if ($p.ExitCode -ne 0) { Fail "PawnIO-Setup endete mit Code $($p.ExitCode)." }

$size = if (Test-Path $SysPath) { (Get-Item $SysPath).Length } else { 0 }
if ($size -ne $SysOk) {
    Say "Installierte PawnIO.sys hat $size Bytes, erwartet $SysOk. Neu starten und dieses Skript nochmal ausführen;" Yellow
    Say 'weicht es dann noch ab, entfernt das Skript den zwischengespeicherten Treiber beim nächsten Lauf.' Yellow
} else {
    Say 'PawnIO 2.0.1 unrestricted installiert.' Green
}

# ---- 5. reboot ------------------------------------------------------------
Say "`nJetzt NEU STARTEN. Danach MacBreeze öffnen." Yellow
Say '(Windows zeigt unten rechts "Testmodus", solange die Testsignatur an ist - das ist normal.)' Gray
if ((Read-Host 'Jetzt neu starten? (j/N)') -eq 'j') { Restart-Computer }
