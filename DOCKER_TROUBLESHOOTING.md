# Hướng dẫn khắc phục sự cố Docker trên Self-Hosted Runner

## 🔍 Kiểm tra ban đầu

### 1. Kiểm tra quyền Administrator
```powershell
# Chạy PowerShell as Administrator và kiểm tra
$currentPrincipal = New-Object Security.Principal.WindowsPrincipal([Security.Principal.WindowsIdentity]::GetCurrent())
$isAdmin = $currentPrincipal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
Write-Host "Is Admin: $isAdmin"
```

### 2. Kiểm tra GitHub Actions Runner Service
```powershell
# Kiểm tra service runner
Get-Service -Name "actions.runner.*"

# Dừng và khởi động lại service với quyền admin
Stop-Service -Name "actions.runner.*"
Start-Service -Name "actions.runner.*"
```

## 🐳 Cài đặt và cấu hình Docker

### 1. Cài đặt Docker Desktop thủ công
- Tải về: https://www.docker.com/products/docker-desktop
- Chạy installer với quyền Administrator
- Trong quá trình cài đặt, chọn:
  - ✅ Enable Hyper-V Windows Features (nếu có)
  - ✅ Install required Windows components for WSL 2

### 2. Cấu hình Docker Desktop
```powershell
# Khởi động Docker Desktop
Start-Process "${env:ProgramFiles}\Docker\Docker\Docker Desktop.exe"

# Đợi Docker khởi động
do {
    Start-Sleep -Seconds 5
    $dockerReady = docker version 2>$null
} while ($LASTEXITCODE -ne 0)
```

### 3. Cấu hình Windows Features
```powershell
# Kích hoạt Hyper-V (yêu cầu khởi động lại)
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V -All

# Kích hoạt WSL 2
Enable-WindowsOptionalFeature -Online -FeatureName Microsoft-Windows-Subsystem-Linux
Enable-WindowsOptionalFeature -Online -FeatureName VirtualMachinePlatform

# Cài đặt WSL 2 kernel update
# Tải từ: https://aka.ms/wsl2kernel
```

## 🔧 Khắc phục sự cố Docker Services

### 1. Khởi động lại tất cả Docker services
```powershell
# Dừng tất cả Docker services
Get-Service | Where-Object {$_.Name -like "*docker*"} | Stop-Service -Force

# Khởi động lại
Start-Service -Name "Docker Desktop Service" -ErrorAction SilentlyContinue
Start-Service -Name "com.docker.service" -ErrorAction SilentlyContinue

# Khởi động Docker Desktop application
Start-Process "${env:ProgramFiles}\Docker\Docker\Docker Desktop.exe"
```

### 2. Reset Docker Desktop
```powershell
# Dừng Docker Desktop
Get-Process "Docker Desktop" | Stop-Process -Force

# Xóa Docker data (CẢNH BÁO: Sẽ mất tất cả containers và images)
Remove-Item "$env:APPDATA\Docker" -Recurse -Force -ErrorAction SilentlyContinue

# Khởi động lại
Start-Process "${env:ProgramFiles}\Docker\Docker\Docker Desktop.exe"
```

## 🚀 Cấu hình GitHub Actions Runner

### 1. Chạy Runner service với quyền Administrator
```cmd
# Dừng service hiện tại
sc stop "actions.runner.YOUR_RUNNER_NAME.YOUR_REPO"

# Cấu hình chạy với quyền admin
sc config "actions.runner.YOUR_RUNNER_NAME.YOUR_REPO" obj= "LocalSystem"

# Khởi động lại
sc start "actions.runner.YOUR_RUNNER_NAME.YOUR_REPO"
```

### 2. Cấu hình biến môi trường
```powershell
# Thêm Docker vào PATH
$dockerPath = "${env:ProgramFiles}\Docker\Docker\resources\bin"
[Environment]::SetEnvironmentVariable("PATH", "$env:PATH;$dockerPath", "Machine")

# Khởi động lại runner service để load PATH mới
Restart-Service -Name "actions.runner.*"
```

## 📊 Kiểm tra diagnostic

### 1. Kiểm tra Docker health
```powershell
docker version
docker info
docker run --rm hello-world
```

### 2. Kiểm tra Windows requirements
```powershell
# Kiểm tra Hyper-V
Get-WindowsOptionalFeature -Online -FeatureName Microsoft-Hyper-V-All

# Kiểm tra WSL
wsl --status
wsl --list --verbose

# Kiểm tra virtualization trong BIOS
Get-ComputerInfo | Select-Object -Property "HyperV*"
```

## 💡 Tips và ghi chú

1. **Yêu cầu hệ thống:**
   - Windows 10/11 Pro, Enterprise, hoặc Education
   - Hyper-V hoặc WSL 2 được kích hoạt
   - Virtualization được bật trong BIOS/UEFI

2. **Trường hợp không thể dùng Docker Desktop:**
   - Cân nhắc sử dụng Docker CE (Community Edition)
   - Hoặc sử dụng Linux-based runner thay vì Windows

3. **Performance optimization:**
   - Tăng RAM cho runner machine (tối thiểu 8GB)
   - Sử dụng SSD thay vì HDD
   - Đảm bảo network connection ổn định

## 🔗 Tài liệu tham khảo
- [Docker Desktop for Windows](https://docs.docker.com/desktop/windows/)
- [GitHub Actions self-hosted runners](https://docs.github.com/en/actions/hosting-your-own-runners)
- [WSL 2 installation guide](https://docs.microsoft.com/en-us/windows/wsl/install) 