@echo off
REM Installiert PawnIO 2.0.1 unrestricted + Testsignatur (als Administrator). Vorher build.bat ausfuehren.
powershell -NoProfile -Command "Start-Process powershell -Verb RunAs -ArgumentList '-NoProfile -ExecutionPolicy Bypass -File \"%~dp0build\setup-pawnio-t2.ps1\"'"
