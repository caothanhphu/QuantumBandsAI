# Hướng dẫn Setup MT5 Data Pusher như Windows Service

## 🚀 Phương pháp 1: NSSM (Non-Sucking Service Manager) - KHUYẾN NGHỊ

### Bước 1: Tải và cài đặt NSSM

1. **Tải NSSM:**
   ```powershell
   # Tải file zip
   Invoke-WebRequest -Uri "https://nssm.cc/release/nssm-2.24.zip" -OutFile "nssm.zip"
   
   # Extract
   Expand-Archive -Path "nssm.zip" -DestinationPath "nssm" -Force
   ```

2. **Copy NSSM executable:**
   ```powershell
   # Copy nssm.exe vào System32 (cần admin rights)
   Copy-Item "nssm\nssm-2.24\win64\nssm.exe" "C:\Windows\System32\"
   ```

### Bước 2: Tạo Service

1. **Chạy PowerShell as Administrator**
2. **Tạo service với NSSM:**
   ```powershell
   # Mở NSSM GUI để config
   nssm install MT5DataPusher
   ```

3. **Cấu hình trong NSSM GUI:**
   - **Application tab:**
     - Path: `D:\Develop\QuantumBandsAI\python\.venv\Scripts\python.exe`
     - Startup directory: `D:\Develop\QuantumBandsAI\python`
     - Arguments: `mt5_data_pusher.py`
   
   - **Details tab:**
     - Display name: `MT5 Data Pusher`
     - Description: `MetaTrader 5 Data Pusher Service`
     - Startup type: `Automatic`

4. **Cài đặt và khởi động:**
   ```powershell
   # Cài đặt service
   nssm install MT5DataPusher "D:\Develop\QuantumBandsAI\python\.venv\Scripts\python.exe" "mt5_data_pusher.py"
   
   # Set working directory
   nssm set MT5DataPusher AppDirectory "D:\Develop\QuantumBandsAI\python"
   
   # Set service description
   nssm set MT5DataPusher Description "MetaTrader 5 Data Pusher Service"
   
   # Start service
   nssm start MT5DataPusher
   ```

### Bước 3: Quản lý Service

```powershell
# Kiểm tra status
nssm status MT5DataPusher

# Start service
nssm start MT5DataPusher

# Stop service
nssm stop MT5DataPusher

# Restart service
nssm restart MT5DataPusher

# Remove service
nssm remove MT5DataPusher confirm
```

---

## 🔧 Phương pháp 2: Windows Task Scheduler

### Bước 1: Tạo Basic Task

1. **Mở Task Scheduler:**
   ```powershell
   taskschd.msc
   ```

2. **Create Basic Task:**
   - Name: `MT5 Data Pusher`
   - Description: `MetaTrader 5 Data Pusher Service`
   - Trigger: `At startup`
   - Action: `Start a program`

3. **Program Settings:**
   - Program: `D:\Develop\QuantumBandsAI\python\.venv\Scripts\python.exe`
   - Arguments: `mt5_data_pusher.py`
   - Start in: `D:\Develop\QuantumBandsAI\python`

### Bước 2: Advanced Settings

1. **General tab:**
   - ✅ Run whether user is logged on or not
   - ✅ Run with highest privileges
   - ✅ Hidden

2. **Conditions tab:**
   - ❌ Start the task only if the computer is on AC power
   - ✅ Wake the computer to run this task

3. **Settings tab:**
   - ✅ Allow task to be run on demand
   - ✅ Run task as soon as possible after a scheduled start is missed
   - ✅ If the running task does not end when requested, force it to stop
   - Restart: `Every 1 minute` (for 3 attempts)

### Bước 3: PowerShell Command để tạo Task

```powershell
# Tạo task bằng PowerShell (run as Administrator)
$Action = New-ScheduledTaskAction -Execute "D:\Develop\QuantumBandsAI\python\.venv\Scripts\python.exe" -Argument "mt5_data_pusher.py" -WorkingDirectory "D:\Develop\QuantumBandsAI\python"

$Trigger = New-ScheduledTaskTrigger -AtStartup

$Principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest

$Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -RestartCount 3 -RestartInterval (New-TimeSpan -Minutes 1)

Register-ScheduledTask -TaskName "MT5DataPusher" -Action $Action -Trigger $Trigger -Principal $Principal -Settings $Settings -Description "MetaTrader 5 Data Pusher Service"
```

### Bước 4: Quản lý Task

```powershell
# Start task
Start-ScheduledTask -TaskName "MT5DataPusher"

# Stop task
Stop-ScheduledTask -TaskName "MT5DataPusher"

# Get task info
Get-ScheduledTask -TaskName "MT5DataPusher"

# Remove task
Unregister-ScheduledTask -TaskName "MT5DataPusher" -Confirm:$false
```

---

## 🐍 Phương pháp 3: Python Windows Service

### Bước 1: Cài đặt pywin32

```powershell
# Activate venv
.\.venv\Scripts\Activate.ps1

# Install pywin32
pip install pywin32
```

### Bước 2: Tạo Service Wrapper

Tạo file `mt5_service.py`:

```python
import win32serviceutil
import win32service
import win32event
import servicemanager
import sys
import os
import subprocess

class MT5DataPusherService(win32serviceutil.ServiceFramework):
    _svc_name_ = "MT5DataPusher"
    _svc_display_name_ = "MT5 Data Pusher Service"
    _svc_description_ = "MetaTrader 5 Data Pusher Service"

    def __init__(self, args):
        win32serviceutil.ServiceFramework.__init__(self, args)
        self.hWaitStop = win32event.CreateEvent(None, 0, 0, None)
        self.is_alive = True

    def SvcStop(self):
        self.ReportServiceStatus(win32service.SERVICE_STOP_PENDING)
        win32event.SetEvent(self.hWaitStop)
        self.is_alive = False

    def SvcDoRun(self):
        servicemanager.LogMsg(servicemanager.EVENTLOG_INFORMATION_TYPE,
                             servicemanager.PYS_SERVICE_STARTED,
                             (self._svc_name_, ''))
        self.main()

    def main(self):
        # Đường dẫn đến script
        script_path = os.path.join(os.path.dirname(__file__), 'mt5_data_pusher.py')
        python_path = sys.executable
        
        while self.is_alive:
            try:
                # Chạy script
                proc = subprocess.Popen([python_path, script_path], 
                                      cwd=os.path.dirname(__file__))
                
                # Chờ process hoặc stop signal
                while self.is_alive and proc.poll() is None:
                    rc = win32event.WaitForSingleObject(self.hWaitStop, 1000)
                    if rc == win32event.WAIT_OBJECT_0:
                        break
                
                # Terminate process if still running
                if proc.poll() is None:
                    proc.terminate()
                    proc.wait()
                    
            except Exception as e:
                servicemanager.LogErrorMsg(f"Error in service: {e}")
                
            # Chờ trước khi restart (nếu service chưa stop)
            if self.is_alive:
                win32event.WaitForSingleObject(self.hWaitStop, 5000)

if __name__ == '__main__':
    if len(sys.argv) == 1:
        servicemanager.Initialize()
        servicemanager.PrepareToHostSingle(MT5DataPusherService)
        servicemanager.StartServiceCtrlDispatcher()
    else:
        win32serviceutil.HandleCommandLine(MT5DataPusherService)
```

### Bước 3: Cài đặt Service

```powershell
# Install service (run as Administrator)
python mt5_service.py install

# Start service
python mt5_service.py start

# Stop service
python mt5_service.py stop

# Remove service
python mt5_service.py remove
```

---

## 📊 Monitoring Services

### 1. Kiểm tra Service Status

```powershell
# Kiểm tra tất cả services
Get-Service | Where-Object {$_.Name -like "*MT5*"}

# Kiểm tra service cụ thể
Get-Service -Name "MT5DataPusher"

# Kiểm tra processes
Get-Process python
```

### 2. Xem Event Logs

```powershell
# Xem System Event Log
Get-EventLog -LogName System -Source "Service Control Manager" | Where-Object {$_.Message -like "*MT5*"}

# Xem Application Event Log
Get-EventLog -LogName Application -Source "MT5DataPusher"
```

### 3. Monitor Performance

```powershell
# CPU/Memory usage
Get-Process python | Format-Table ProcessName, CPU, WorkingSet, VirtualMemorySize

# Network connections
netstat -ano | findstr python.exe
```

---

## 🔧 Troubleshooting

### 1. Service không start

**Kiểm tra:**
- Path đến python.exe đúng chưa
- Working directory đúng chưa  
- User permissions đủ chưa
- MT5 terminal đã login chưa

**Fix:**
```powershell
# Kiểm tra path
Test-Path "D:\Develop\QuantumBandsAI\python\.venv\Scripts\python.exe"

# Chạy script manually để test
cd "D:\Develop\QuantumBandsAI\python"
python mt5_data_pusher.py
```

### 2. Service start nhưng không hoạt động

**Kiểm tra:**
- Log file: `mt5_pusher.log`
- Event Viewer: Windows Logs > Application
- MT5 terminal connection

### 3. Service crash liên tục

**Fix:**
- Thêm restart policy trong Task Scheduler
- Tăng timeout trong NSSM
- Kiểm tra dependencies (MT5 terminal, network)

---

## 🎯 Khuyến nghị

**Production Environment:**
1. **NSSM** - Dễ setup, ổn định
2. **Task Scheduler** - Built-in Windows, reliable
3. **Python Service** - Flexible nhưng phức tạp hơn

**Development Environment:**
- Chạy trong PowerShell/Terminal để debug dễ hơn

**Security:**
- Chạy service với dedicated user account
- Restrict permissions cho config file
- Regular backup config và logs 