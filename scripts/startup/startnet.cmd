@echo off
:: ============================================================
:: WinPE Nghitr Dev — Startup Script
:: startnet.cmd — chạy tự động khi WinPE boot
:: ============================================================

wpeinit

:: Cấu hình màn hình
mode con cols=120 lines=40

echo.
echo ============================================================
echo   WinPE Nghitr Dev v1.0.0 - Khoi dong he thong...
echo ============================================================
echo.

:: Tạo thư mục log
if not exist "X:\WinPE\Logs" mkdir "X:\WinPE\Logs"

:: Ghi log
echo [%date% %time%] WinPE startup >> "X:\WinPE\Logs\startup.log"
echo [%date% %time%] wpeinit completed >> "X:\WinPE\Logs\startup.log"

:: Khởi động PowerShell startup script
echo Dang khoi dong...
powershell.exe -ExecutionPolicy Bypass -NonInteractive -File "X:\WinPE\Scripts\startup\Start-GUI.ps1"

:: Nếu GUI crash, fallback về PowerShell
echo.
echo [WinPE] GUI da dong hoac gap loi.
echo [WinPE] Khoi dong PowerShell de ho tro...
echo.
powershell.exe -ExecutionPolicy Bypass -NoExit
