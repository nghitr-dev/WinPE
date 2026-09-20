@echo off
:: ============================================================
:: WinPE Nghitr Dev - Startup Script
:: startnet.cmd - Chay tu dong khi WinPE khoi dong
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

:: Kiem tra va khoi chay truc tiep GUI app
if exist "X:\WinPE\WinPE-Tool.exe" (
    echo [WinPE] Phat hien GUI app, dang khoi dong WinPE-Tool...
    echo [%date% %time%] Launching WinPE-Tool.exe >> "X:\WinPE\Logs\startup.log"
    start "" "X:\WinPE\WinPE-Tool.exe"
    goto shell
)

:: Neu GUI chua co, chay script Start-GUI.ps1
if exist "X:\WinPE\Scripts\startup\Start-GUI.ps1" (
    echo [WinPE] Dang chay script Start-GUI.ps1...
    powershell.exe -ExecutionPolicy Bypass -NoLogo -File "X:\WinPE\Scripts\startup\Start-GUI.ps1"
    goto shell
)

:shell
echo.
echo ============================================================
echo [WinPE] He thong san sang. Khoi dong PowerShell...
echo ============================================================
echo.
powershell.exe -ExecutionPolicy Bypass -NoExit
