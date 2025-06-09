# MT5 Data Pusher Service Installer
# Run this script as Administrator

param(
    [Parameter(Mandatory=$false)]
    [string]$ProjectPath = (Get-Location).Path,
    
    [Parameter(Mandatory=$false)]
    [string]$TaskName = "MT5DataPusher",
    
    [Parameter(Mandatory=$false)]
    [string]$ServiceDescription = "MetaTrader 5 Data Pusher Service"
)

Write-Host "🚀 MT5 Data Pusher Service Installer" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan

# Check if running as Administrator
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "❌ ERROR: This script must be run as Administrator!" -ForegroundColor Red
    Write-Host "Right-click PowerShell and select 'Run as Administrator'" -ForegroundColor Yellow
    exit 1
}

# Validate paths
$PythonExe = Join-Path $ProjectPath ".venv\Scripts\python.exe"
$ScriptPath = Join-Path $ProjectPath "mt5_data_pusher.py"
$ConfigPath = Join-Path $ProjectPath "mt5_config.ini"

Write-Host "📂 Validating project structure..." -ForegroundColor Yellow

if (-not (Test-Path $PythonExe)) {
    Write-Host "❌ ERROR: Python executable not found at: $PythonExe" -ForegroundColor Red
    Write-Host "Please ensure virtual environment is created and activated" -ForegroundColor Yellow
    exit 1
}

if (-not (Test-Path $ScriptPath)) {
    Write-Host "❌ ERROR: MT5 script not found at: $ScriptPath" -ForegroundColor Red
    exit 1
}

if (-not (Test-Path $ConfigPath)) {
    Write-Host "❌ ERROR: Config file not found at: $ConfigPath" -ForegroundColor Red
    Write-Host "Please create mt5_config.ini file first" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ All required files found" -ForegroundColor Green

# Check if task already exists
$ExistingTask = Get-ScheduledTask -TaskName $TaskName -ErrorAction SilentlyContinue
if ($ExistingTask) {
    Write-Host "⚠️  Task '$TaskName' already exists" -ForegroundColor Yellow
    $choice = Read-Host "Do you want to replace it? (y/N)"
    if ($choice -eq 'y' -or $choice -eq 'Y') {
        Write-Host "🗑️  Removing existing task..." -ForegroundColor Yellow
        Unregister-ScheduledTask -TaskName $TaskName -Confirm:$false
        Write-Host "✅ Existing task removed" -ForegroundColor Green
    } else {
        Write-Host "❌ Installation cancelled" -ForegroundColor Red
        exit 1
    }
}

# Create scheduled task
Write-Host "🔨 Creating scheduled task..." -ForegroundColor Yellow

try {
    # Define task action
    $Action = New-ScheduledTaskAction -Execute $PythonExe -Argument "mt5_data_pusher.py" -WorkingDirectory $ProjectPath

    # Define task trigger (at startup)
    $Trigger = New-ScheduledTaskTrigger -AtStartup

    # Define task principal (run as SYSTEM)
    $Principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

    # Define task settings
    $Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1) -ExecutionTimeLimit (New-TimeSpan -Hours 0)

    # Register the task
    Register-ScheduledTask -TaskName $TaskName -Action $Action -Trigger $Trigger -Principal $Principal -Settings $Settings -Description $ServiceDescription

    Write-Host "✅ Scheduled task created successfully!" -ForegroundColor Green

    # Start the task
    Write-Host "🚀 Starting the service..." -ForegroundColor Yellow
    Start-ScheduledTask -TaskName $TaskName
    
    # Wait a moment and check status
    Start-Sleep -Seconds 3
    $TaskInfo = Get-ScheduledTask -TaskName $TaskName
    $LastRunTime = (Get-ScheduledTaskInfo -TaskName $TaskName).LastRunTime
    
    Write-Host "📊 Service Status:" -ForegroundColor Cyan
    Write-Host "   Task State: $($TaskInfo.State)" -ForegroundColor White
    Write-Host "   Last Run: $LastRunTime" -ForegroundColor White
    
    if ($TaskInfo.State -eq "Running") {
        Write-Host "✅ Service is running successfully!" -ForegroundColor Green
    } else {
        Write-Host "⚠️  Service may not be running. Check logs for details." -ForegroundColor Yellow
    }

} catch {
    Write-Host "❌ ERROR: Failed to create scheduled task" -ForegroundColor Red
    Write-Host "Error: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

# Show management commands
Write-Host "`n🛠️  Service Management Commands:" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Start service:   Start-ScheduledTask -TaskName '$TaskName'" -ForegroundColor Yellow
Write-Host "Stop service:    Stop-ScheduledTask -TaskName '$TaskName'" -ForegroundColor Yellow
Write-Host "Check status:    Get-ScheduledTask -TaskName '$TaskName'" -ForegroundColor Yellow
Write-Host "Remove service:  Unregister-ScheduledTask -TaskName '$TaskName' -Confirm:`$false" -ForegroundColor Yellow
Write-Host "View logs:       Get-Content '$ProjectPath\mt5_pusher.log' -Tail 20" -ForegroundColor Yellow

# Show log file location
Write-Host "`n📋 Important Information:" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan
Write-Host "Service Name:    $TaskName" -ForegroundColor White
Write-Host "Working Dir:     $ProjectPath" -ForegroundColor White
Write-Host "Python Path:     $PythonExe" -ForegroundColor White
Write-Host "Script Path:     $ScriptPath" -ForegroundColor White
Write-Host "Log File:        $ProjectPath\mt5_pusher.log" -ForegroundColor White
Write-Host "Config File:     $ConfigPath" -ForegroundColor White

Write-Host "`n🎉 Installation completed successfully!" -ForegroundColor Green
Write-Host "The service will automatically start when Windows boots." -ForegroundColor Green
Write-Host "Monitor the log file to ensure everything is working correctly." -ForegroundColor Yellow 