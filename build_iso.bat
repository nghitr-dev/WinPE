@echo off
:: ============================================================
:: WinPE Nghitr Dev — 1-Click Build ISO Script
:: Chuột phải vào file này -> Chọn "Run as administrator"
:: ============================================================
title WinPE Nghitr Dev - Build ISO
cd /d "%~dp0"

echo ============================================================
echo   WinPE Nghitr Dev - He thong dong goi ISO tu dong
echo ============================================================
echo.

:: Kiểm tra quyền Administrator
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [!] CHU Y: Script nay bat buoc phai chay bang quyen Administrator!
    echo.
    echo -> Hay click CHUOT PHAI vao file build_iso.bat nay
    echo -> Chon "Run as administrator" (Chay voi tu cach quan tri vien)
    echo.
    pause
    exit /b 1
)

echo [OK] Da co quyen Administrator.
echo Dang khoi chay build ISO tu dong...
echo (Qua trinh nay se mount WIM, nap packages, copy app va tao file ISO)
echo.

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "build\Build-WinPE.ps1" -Clean

echo.
echo ============================================================
echo Build ket thuc. Hay kiem tra file ISO trong thu muc output\
echo ============================================================
echo.
pause
