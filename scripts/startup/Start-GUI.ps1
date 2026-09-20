<#
.SYNOPSIS
    WinPE Startup — Khởi động GUI hoặc fallback menu
.DESCRIPTION
    Chạy từ startnet.cmd sau wpeinit
    Kiểm tra GUI app, nếu có thì khởi động, không thì hiển thị menu text
#>

$ErrorActionPreference = "Continue"
$WinPE_Root = "X:\WinPE"
$LogFile    = "$WinPE_Root\Logs\startup.log"
$ConfigFile = "$WinPE_Root\Config\winpe-config.json"
$GUIApp     = "$WinPE_Root\WinPE-Tool.exe"

function Write-Log {
    param([string]$msg, [string]$level = "INFO")
    $ts = Get-Date -Format "HH:mm:ss"
    $line = "[$ts][$level] $msg"
    Add-Content $LogFile $line -Encoding UTF8 -ErrorAction SilentlyContinue
    Write-Host $line
}

function Show-FallbackMenu {
    Clear-Host
    Write-Host ""
    Write-Host "  ╔══════════════════════════════════════════════╗" -ForegroundColor Cyan
    Write-Host "  ║        WinPE Nghitr Dev  v1.0.0             ║" -ForegroundColor Cyan
    Write-Host "  ║     Cong cu cuu ho & bao tri Windows        ║" -ForegroundColor Cyan
    Write-Host "  ╚══════════════════════════════════════════════╝" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "  [GUI chua duoc build — che do Console]" -ForegroundColor Yellow
    Write-Host ""
    Write-Host "  1. Command Prompt" -ForegroundColor White
    Write-Host "  2. PowerShell" -ForegroundColor White
    Write-Host "  3. Disk Info" -ForegroundColor White
    Write-Host "  4. Network Info" -ForegroundColor White
    Write-Host "  5. Hardware Info" -ForegroundColor White
    Write-Host "  6. Windows Detection" -ForegroundColor White
    Write-Host "  7. Reboot" -ForegroundColor White
    Write-Host "  8. Shutdown" -ForegroundColor White
    Write-Host ""

    $choice = Read-Host "  Chon [1-8]"
    switch ($choice) {
        "1" { Start-Process cmd.exe -Wait }
        "2" { Start-Process powershell.exe -ArgumentList "-NoExit" -Wait }
        "3" {
            Get-Disk | Format-Table Number, FriendlyName, Size, PartitionStyle -AutoSize
            Get-Partition | Format-Table DiskNumber, PartitionNumber, DriveLetter, Size, Type -AutoSize
            Read-Host "Enter de tiep tuc"
        }
        "4" {
            Get-NetIPConfiguration | Format-List InterfaceAlias, IPv4Address, IPv4DefaultGateway, DNSServer
            Read-Host "Enter de tiep tuc"
        }
        "5" {
            Get-WmiObject Win32_ComputerSystem | Select-Object Manufacturer, Model, TotalPhysicalMemory
            Get-WmiObject Win32_Processor | Select-Object Name, NumberOfCores, MaxClockSpeed
            Read-Host "Enter de tiep tuc"
        }
        "6" {
            $drives = Get-PSDrive -PSProvider FileSystem | Select-Object -ExpandProperty Root
            foreach ($d in $drives) {
                $winDir = "${d}Windows"
                if (Test-Path $winDir) {
                    Write-Host "  ✅ Windows found at: $winDir" -ForegroundColor Green
                }
            }
            Read-Host "Enter de tiep tuc"
        }
        "7" { Restart-Computer -Force }
        "8" { Stop-Computer -Force }
    }
    Show-FallbackMenu
}

# ─── MAIN ───────────────────────────────────────────────────
Write-Log "WinPE startup script running..." "INFO"
Write-Log "WinPE Root: $WinPE_Root" "INFO"

# Wait for wpeinit to finish
Start-Sleep -Seconds 2

# Check GUI app
if (Test-Path $GUIApp) {
    Write-Log "GUI app found: $GUIApp" "INFO"
    Write-Log "Launching GUI..." "INFO"
    try {
        Start-Process $GUIApp -Wait
        Write-Log "GUI exited." "INFO"
    } catch {
        Write-Log "GUI launch failed: $_" "ERROR"
    }
} else {
    Write-Log "GUI app not found at $GUIApp — using fallback menu" "WARN"
    Show-FallbackMenu
}
