@echo off
cd /d "%~dp0\PyStudioDesktopSharp"
dotnet publish -c Release -r win-x64 --self-contained false
pause
