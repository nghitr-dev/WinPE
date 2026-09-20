@echo off
:: ============================================================
:: WinPE Nghitr Dev - Startup Script
:: ============================================================

wpeinit

mode con cols=120 lines=40

echo.
echo ============================================================
echo   WinPE Nghitr Dev v1.0.0 - Dang khoi dong he thong...
echo ============================================================
echo.

if not exist "X:\WinPE\Logs" mkdir "X:\WinPE\Logs"

echo [%date% %time%] WinPE startup >> "X:\WinPE\Logs\startup.log" 2>nul
echo [%date% %time%] wpeinit completed >> "X:\WinPE\Logs\startup.log" 2>nul

:: 1. Khoi dong Desktop Shell - Tao Taskbar, Start Menu, Desktop, Dong ho
if exist "X:\WinPE\WinXShell\WinXShell.exe" goto launch_winxshell1
if exist "X:\WinPE\Tools\WinXShell\WinXShell.exe" goto launch_winxshell2
goto launch_app

:launch_winxshell1
echo [WinPE] Dang khoi dong Windows Desktop Shell...
start "" "X:\WinPE\WinXShell\WinXShell.exe" -regist -shell
goto wait_shell

:launch_winxshell2
echo [WinPE] Dang khoi dong Windows Desktop Shell...
start "" "X:\WinPE\Tools\WinXShell\WinXShell.exe" -regist -shell
goto wait_shell

:wait_shell
timeout /t 2 /nobreak >nul 2>&1

:launch_app
:: 2. Khoi dong ung dung cuu ho WinPE-Tool len tren Desktop
if exist "X:\WinPE\WinPE-Tool.exe" (
    echo [WinPE] Dang mo ung dung WinPE-Tool...
    start "" "X:\WinPE\WinPE-Tool.exe"
)

:shell
echo.
echo ============================================================
echo [WinPE] He thong san sang. PowerShell hoat dong o che do cho.
echo ============================================================
echo.
powershell.exe -ExecutionPolicy Bypass -NoExit
