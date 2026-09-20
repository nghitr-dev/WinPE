@echo off
:: ============================================================
:: WinPE Nghitr Dev - Startup Script
:: startnet.cmd - Chay tu dong khi WinPE boot
:: ============================================================

wpeinit

:: Cau hinh kich thuoc man hinh console
mode con cols=120 lines=40

echo.
echo ============================================================
echo   WinPE Nghitr Dev v1.0.0 - Dang khoi dong he thong...
echo ============================================================
echo.

:: Tao thu muc log
if not exist "X:\WinPE\Logs" mkdir "X:\WinPE\Logs"

:: Ghi log khoi dong
echo [%date% %time%] WinPE startup >> "X:\WinPE\Logs\startup.log"
echo [%date% %time%] wpeinit completed >> "X:\WinPE\Logs\startup.log"

:: Khoi dong Desktop Shell (WinXShell) - Tao Taskbar, Start Menu, Desktop Icons, Wallpaper
if exist "X:\WinPE\Tools\WinXShell\WinXShell.exe" (
    echo [WinPE] Dang khoi dong Desktop Shell (WinXShell)...
    echo [%date% %time%] Launching WinXShell... >> "X:\WinPE\Logs\startup.log"
    start "" "X:\WinPE\Tools\WinXShell\WinXShell.exe" -winpe -shell
    timeout /t 2 /nobreak >nul 2>&1
) else if exist "X:\WinPE\WinXShell\WinXShell.exe" (
    echo [WinPE] Dang khoi dong Desktop Shell (WinXShell)...
    start "" "X:\WinPE\WinXShell\WinXShell.exe" -winpe -shell
    timeout /t 2 /nobreak >nul 2>&1
)

:: Khoi dong ung dung cuu ho WinPE-Tool len giua Desktop
if exist "X:\WinPE\WinPE-Tool.exe" (
    echo [WinPE] Dang mo ung dung WinPE-Tool...
    echo [%date% %time%] Launching WinPE-Tool.exe >> "X:\WinPE\Logs\startup.log"
    start "" "X:\WinPE\WinPE-Tool.exe"
)

:shell
echo.
echo ============================================================
echo [WinPE] He thong san sang. PowerShell hoat dong o che do cho.
echo ============================================================
echo.
powershell.exe -ExecutionPolicy Bypass -NoExit
