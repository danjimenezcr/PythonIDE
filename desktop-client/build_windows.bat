@echo off
title PyStudio Desktop - Build Windows
echo =========================================
echo Generando ejecutable de PyStudio Desktop
echo =========================================
python --version
if errorlevel 1 (
    echo Python no esta instalado o no esta en PATH.
    pause
    exit /b 1
)
python -m pip install --upgrade pip
python -m pip install pyinstaller
pyinstaller --onefile --windowed --name PyStudioDesktop main.py
if errorlevel 1 (
    echo No se pudo generar el ejecutable.
    pause
    exit /b 1
)
echo.
echo Ejecutable generado en: dist\PyStudioDesktop.exe
pause
