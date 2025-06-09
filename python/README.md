# MT5 Data Pusher

Ứng dụng Python tự động lấy dữ liệu từ MetaTrader 5 và đẩy lên API server theo thời gian thực.

## Tính năng

- ✅ Lấy thông tin tài khoản (balance, equity) theo thời gian thực
- ✅ Lấy danh sách lệnh đang mở (open positions)
- ✅ Lấy lịch sử lệnh đã đóng (closed trades)
- ✅ Đẩy dữ liệu lên API server tự động
- ✅ Hỗ trợ nhiều tài khoản MT5
- ✅ Retry logic khi API server không phản hồi
- ✅ Logging chi tiết với UTF-8 encoding
- ✅ SSL verification có thể tắt cho development

## Yêu cầu hệ thống

- **Python 3.7+**
- **MetaTrader 5** đã cài đặt và đăng nhập
- **Kết nối internet** để gọi API
- **Windows** (do MetaTrader5 library chỉ hỗ trợ Windows)

> ⚠️ **QUAN TRỌNG**: Ứng dụng này **KHÔNG thể chạy trên Docker** vì:
> - MetaTrader5 Python library chỉ hoạt động trên Windows
> - Cần MT5 terminal GUI phải được cài đặt và đăng nhập
> - Docker containers thường chạy Linux và headless
> 
> **Giải pháp thay thế:**
> - Chạy trên Windows VPS/Server
> - Sử dụng MT5 WebAPI thay vì local terminal
> - Architecture với Windows bridge service

## Cài đặt

### 1. Tạo môi trường ảo Python

```bash
python -m venv .venv
```

### 2. Kích hoạt môi trường ảo

```bash
# Windows PowerShell
.\.venv\Scripts\Activate.ps1

# Windows Command Prompt
.\.venv\Scripts\activate.bat
```

### 3. Cài đặt dependencies

```bash
pip install -r requirements.txt
```

## Cấu hình

### 1. Cấu hình file `mt5_config.ini`

Sao chép và chỉnh sửa file config:

```ini
[General]
ApiBaseUrl = http://localhost:5047/api/v1
ApiKey = YOUR_SECRET_API_KEY_HERE
TimeIntervalSeconds = 3
LookbackHoursClosedTrades = 24
LogLevel = INFO
DisableSSLVerification = true
RequestTimeoutSeconds = 60
MaxRetries = 3
RetryDelaySeconds = 5

[MT5Account_1]
TradingAccountIdSystem = 1
Login = YOUR_MT5_LOGIN
Password = YOUR_MT5_PASSWORD
Server = YOUR_MT5_SERVER
Enabled = true

; Thêm nhiều tài khoản nếu cần
;[MT5Account_2]
;TradingAccountIdSystem = 2
;Login = YOUR_MT5_LOGIN_2
;Password = YOUR_MT5_PASSWORD_2
;Server = YOUR_MT5_SERVER_2
;Enabled = true
```

### 2. Giải thích các tham số cấu hình

#### **[General]**
- `ApiBaseUrl`: URL của API server (ví dụ: http://localhost:5047/api/v1)
- `ApiKey`: API key để xác thực với server
- `TimeIntervalSeconds`: Tần suất chạy script (giây) - khuyến nghị 3-10 giây
- `LookbackHoursClosedTrades`: Số giờ nhìn lại để lấy lệnh đã đóng (ví dụ: 24)
- `LogLevel`: Mức độ log (DEBUG, INFO, WARNING, ERROR)
- `DisableSSLVerification`: Tắt kiểm tra SSL certificate (true/false)
- `RequestTimeoutSeconds`: Timeout cho HTTP requests (giây)
- `MaxRetries`: Số lần thử lại khi request thất bại
- `RetryDelaySeconds`: Thời gian chờ giữa các lần retry (giây)

#### **[MT5Account_X]**
- `TradingAccountIdSystem`: ID của tài khoản trong hệ thống API
- `Login`: Tài khoản MT5
- `Password`: Mật khẩu MT5
- `Server`: Server MT5 (ví dụ: ICMarketsSC-MT5-2)
- `Enabled`: Bật/tắt tài khoản này (true/false)

## Chạy ứng dụng

### 1. Chạy test một lần

```bash
python mt5_data_pusher.py
```

### 2. Chạy như Windows Service (khuyến nghị cho production)

**🚀 Cách 1: Tự động setup (Dễ nhất)**
```powershell
# Chạy PowerShell as Administrator
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser

# Chạy script installer tự động
.\install_service.ps1
```

**🔧 Cách 2: Manual setup**
```powershell
# Tạo service bằng Task Scheduler (run as Administrator)
$Action = New-ScheduledTaskAction -Execute ".\.venv\Scripts\python.exe" -Argument "mt5_data_pusher.py" -WorkingDirectory (Get-Location)
$Trigger = New-ScheduledTaskTrigger -AtStartup
$Principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
$Settings = New-ScheduledTaskSettingsSet -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -StartWhenAvailable -RestartCount 3
Register-ScheduledTask -TaskName "MT5DataPusher" -Action $Action -Trigger $Trigger -Principal $Principal -Settings $Settings
```

### 3. Quản lý Windows Service

```powershell
# Bật service
Start-ScheduledTask -TaskName "MT5DataPusher"

# Tắt service  
Stop-ScheduledTask -TaskName "MT5DataPusher"

# Kiểm tra status
Get-ScheduledTask -TaskName "MT5DataPusher"

# Xem thông tin chi tiết
Get-ScheduledTaskInfo -TaskName "MT5DataPusher"

# Xóa service
Unregister-ScheduledTask -TaskName "MT5DataPusher" -Confirm:$false
```

### 4. Dừng ứng dụng

```bash
# Dừng service
Stop-ScheduledTask -TaskName "MT5DataPusher"

# Hoặc dừng trong terminal (nếu chạy manual)
# Nhấn Ctrl+C

# Force kill process (emergency)
taskkill /f /im python.exe
```

> 📝 **Lưu ý**: Service được cấu hình tự động khởi động cùng Windows và restart khi crash. Xem file `setup_service.md` để biết thêm chi tiết về các phương pháp setup service khác (NSSM, Python Service).

## Monitoring và Logging

### 1. File log

Ứng dụng tự động tạo file `mt5_pusher.log` với thông tin chi tiết:

```bash
# Xem log realtime
Get-Content mt5_pusher.log -Wait -Tail 20

# Xem log cuối
Get-Content mt5_pusher.log -Tail 50
```

### 2. Các loại log

- **INFO**: Hoạt động bình thường (kết nối, gửi dữ liệu)
- **WARNING**: Cảnh báo (SSL disabled, không có data)
- **ERROR**: Lỗi (timeout, connection failed)
- **DEBUG**: Chi tiết debug (chỉ khi LogLevel=DEBUG)

## Xử lý sự cố

### 1. Lỗi kết nối MT5

```
MT5 initialize() thất bại cho login XXX
```

**Giải pháp:**
- Kiểm tra MetaTrader 5 đã mở và đăng nhập
- Xác minh login/password/server trong config
- Kiểm tra MT5 có cho phép kết nối external

### 2. Lỗi timeout API

```
Connection to localhost timed out
```

**Giải pháp:**
- Kiểm tra API server đang chạy
- Tăng `RequestTimeoutSeconds` trong config
- Kiểm tra firewall/network

### 3. Lỗi encoding

```
UnicodeDecodeError: 'charmap' codec can't decode
```

**Giải pháp:**
- Script đã được fix để sử dụng UTF-8
- Đảm bảo chạy script với Python 3.7+

### 4. Lỗi SSL

```
certificate verify failed: self-signed certificate
```

**Giải pháp:**
- Set `DisableSSLVerification = true` trong config
- Hoặc cài đặt proper SSL certificate cho API server

## Cấu trúc thư mục

```
python/
├── mt5_data_pusher.py      # Script chính
├── mt5_config.ini          # File cấu hình
├── requirements.txt        # Dependencies
├── mt5_pusher.log         # File log (tự động tạo)
├── README.md              # Hướng dẫn này
└── .venv/                 # Môi trường ảo Python
```

## API Endpoints

Script gửi dữ liệu đến 2 endpoints:

1. **Live Data**: `POST /trading-accounts/{id}/live-data`
   - Account info (balance, equity)
   - Open positions

2. **Closed Trades**: `POST /trading-accounts/{id}/closed-trades`
   - Lịch sử lệnh đã đóng

## Troubleshooting Performance

### Tối ưu hóa hiệu suất:

1. **TimeIntervalSeconds**: Không set quá nhỏ (<3s) để tránh spam API
2. **LookbackHoursClosedTrades**: Giảm xuống nếu có quá nhiều lệnh cũ
3. **RequestTimeoutSeconds**: Tăng nếu API server chậm
4. **LogLevel**: Set thành WARNING hoặc ERROR trong production

### Monitoring tài nguyên:

```powershell
# Kiểm tra CPU/Memory usage
Get-Process python | Format-Table ProcessName, CPU, WorkingSet
```

## Support

Nếu gặp vấn đề, hãy kiểm tra:
1. File log `mt5_pusher.log`
2. MT5 terminal đang chạy
3. API server đang hoạt động
4. Network connectivity
5. Config file syntax
