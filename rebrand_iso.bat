@echo off
:: ============================================================================
:: WinPE Nghitr Dev - 1-Click Rebrand & Build ISO tu Anhdv Boot 26.2
:: ============================================================================
title WinPE Nghitr Dev - Build ISO tu Anhdv Boot

:: Tu dong nang quyen Administrator
>nul 2>&1 "%SYSTEMROOT%\system32\cacls.exe" "%SYSTEMROOT%\system32\config\system"
if '%errorlevel%' NEQ '0' (
    echo [WinPE] Dang yeu cau quyen Administrator...
    powershell -Command "Start-Process '%~f0' -Verb RunAs"
    exit /b
)

cd /d "%~dp0"

echo.
echo ============================================================================
echo   WinPE Nghitr Dev - Tu dong tuy bien va tao ISO tu Anhdv Boot 26.2
echo ============================================================================
echo.

powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0build\Build-From-Anhdv.ps1"

echo.
echo Nhap phim bat ky de thoat...
pause >nul
