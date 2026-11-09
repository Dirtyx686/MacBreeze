@echo off
setlocal
REM ============================================================
REM  MacBreeze build script (basiert auf RPMac)
REM  Compiles the WPF GUI with the .NET Framework C# compiler
REM  that ships with Windows. No Visual Studio required.
REM ============================================================

set "FW=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319"
if not exist "%FW%\csc.exe" set "FW=%WINDIR%\Microsoft.NET\Framework\v4.0.30319"
if not exist "%FW%\csc.exe" (
  echo ERROR: .NET Framework 4 C# compiler ^(csc.exe^) not found.
  echo Make sure .NET Framework 4.x is installed ^(it is on every Windows 10/11^).
  exit /b 1
)

set "ROOT=%~dp0"
if not exist "%ROOT%build" mkdir "%ROOT%build"

echo MacBreeze wird kompiliert ...
"%FW%\csc.exe" /noconfig /nologo /target:winexe /platform:x86 ^
  /win32manifest:"%ROOT%src\gui\app.manifest" ^
  /win32icon:"%ROOT%resources\MacBreeze.ico" ^
  /out:"%ROOT%build\MacBreeze.exe" ^
  /reference:"%FW%\System.dll" ^
  /reference:"%FW%\System.Core.dll" ^
  /reference:"%FW%\System.Xaml.dll" ^
  /reference:"%FW%\WPF\WindowsBase.dll" ^
  /reference:"%FW%\WPF\PresentationCore.dll" ^
  /reference:"%FW%\WPF\PresentationFramework.dll" ^
  /reference:"%FW%\System.Windows.Forms.dll" ^
  /reference:"%FW%\System.Drawing.dll" ^
  "%ROOT%src\gui\Smc.cs" "%ROOT%src\gui\I18n.cs" "%ROOT%src\gui\I18n.Langs.cs" "%ROOT%src\gui\App.cs" "%ROOT%src\gui\SysMon.cs" "%ROOT%src\gui\TrayPeek.cs"

if errorlevel 1 (
  echo.
  echo BUILD FAILED
  exit /b 1
)

echo smccore wird kompiliert ...
"%FW%\csc.exe" /nologo /target:exe /platform:x86 ^
  /out:"%ROOT%build\smccore.exe" ^
  "%ROOT%src\smccore.cs"

if errorlevel 1 (
  echo.
  echo BUILD FAILED ^(smccore^)
  exit /b 1
)

echo Dateien werden kopiert ...

REM App config (enables catching corrupted-state exceptions so startup crashes are logged).
copy /y "%ROOT%src\gui\MacBreeze.exe.config" "%ROOT%build\MacBreeze.exe.config" >nul 2>&1

REM Copy inpout32.dll. If RPMac is running it holds the DLL open; that's fine,
REM the existing copy is identical, so don't fail the build over it.
copy /y "%ROOT%third_party\InpOut32\bin\Win32\inpout32.dll" "%ROOT%build\inpout32.dll" >nul 2>&1
if not exist "%ROOT%build\inpout32.dll" (
  echo BUILD FAILED: inpout32.dll missing
  exit /b 1
)

REM Copy the a prebuilt copy of the PawnIO driver
copy /y "%ROOT%dist\AppleT2Smc.bin" "%ROOT%build\AppleT2Smc.bin" >nul 2>&1
if not exist "%ROOT%build\AppleT2Smc.bin" (
  echo BUILD FAILED: AppleT2Smc.bin missing
  exit /b 1
)

REM T2 Macs: helper that installs PawnIO 2.0.1 unrestricted + test signing
copy /y "%ROOT%tools\setup-pawnio-t2.ps1" "%ROOT%build\setup-pawnio-t2.ps1" >nul 2>&1

echo.
REM Redistributable builds must carry their license and attribution files.
copy /y "%ROOT%LICENSE" "%ROOT%build\LICENSE.txt" >nul || exit /b 1
copy /y "%ROOT%NOTICE" "%ROOT%build\NOTICE.txt" >nul || exit /b 1
copy /y "%ROOT%THIRD-PARTY.md" "%ROOT%build\THIRD-PARTY.md" >nul || exit /b 1
copy /y "%ROOT%dist\README.txt" "%ROOT%build\README.txt" >nul || exit /b 1
copy /y "%ROOT%third_party\InpOut32\LICENSE-MIT.txt" "%ROOT%build\LICENSE-InpOut32.txt" >nul || exit /b 1
copy /y "%ROOT%LICENSES\LGPL-2.1.txt" "%ROOT%build\LICENSE-AppleT2Smc.txt" >nul || exit /b 1
echo BUILD OK  -^>  build\MacBreeze.exe
echo Einmal build\MacBreeze.exe starten ^(UAC bestaetigen^), danach die Desktop-Verknuepfung "MacBreeze" benutzen.
endlocal
exit /b 0
