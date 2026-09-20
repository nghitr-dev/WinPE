@echo off
setlocal EnableDelayedExpansion
cd /d "%~dp0"

title WinPE Nghitr Dev - Build ISO

echo ============================================================
echo   WinPE Nghitr Dev - Build ISO
echo ============================================================
echo.

rem Check Administrator privileges
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo Dang yeu cau quyen Administrator...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process cmd.exe -ArgumentList '/c \"\"%~f0\"\"' -Verb RunAs"
    exit /b
)

echo [OK] Quyen Administrator hop le.
echo Dang khoi chay build WinPE ISO...
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "build\Build-WinPE.ps1" -Clean

echo.
echo ============================================================
echo Hoan tat! File ISO o thu muc: output\
echo ============================================================
echo.
pause
