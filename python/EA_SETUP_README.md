# QuantumBands DataPusher EA - Hướng dẫn cài đặt và sử dụng

## Tổng quan
EA này chuyển đổi từ Python script `mt5_data_pusher.py` để chạy trực tiếp trên MT5, loại bỏ sự phụ thuộc vào Python.

## Cài đặt

### 1. Copy file EA
- Copy file `QuantumBands_DataPusher.mq5` vào thư mục `MQL5/Experts/` trong Data Folder của MT5
- Compile EA trong MetaEditor

### 2. Cấu hình MT5
Để EA có thể gọi WebRequest, cần cho phép URL trong MT5:
1. Mở MT5 → Tools → Options → Expert Advisors
2. Tick vào "Allow WebRequest for listed URL"
3. Thêm URL: `https://finix-api.pmsa.com.vn`

### 3. Cấu hình EA Parameters
Khi attach EA vào chart, cấu hình các parameters:

**API Configuration:**
- `ApiBaseUrl`: `https://finix-api.pmsa.com.vn/api/v1`
- `ApiKey`: API key của bạn (thay thế giá trị mặc định)

**Timing Configuration:**
- `TimeIntervalSeconds`: Khoảng thời gian push data (mặc định: 30 giây)
- `LookbackHoursClosedTrades`: Số giờ lấy lịch sử trades đã đóng (mặc định: 72 giờ)

**Account Configuration:**
- `TradingAccountIdSystem`: ID tài khoản trong hệ thống .NET của bạn

**Network Configuration:**
- `DisableSSLVerification`: Tắt SSL verification (chỉ dùng cho development)
- `RequestTimeoutSeconds`: Timeout cho HTTP request (mặc định: 60 giây)
- `MaxRetries`: Số lần retry khi request thất bại (mặc định: 3)
- `RetryDelaySeconds`: Thời gian chờ giữa các lần retry (mặc định: 5 giây)

## Chức năng chính

### 1. Live Data Push
EA sẽ định kỳ gửi:
- Account Equity
- Account Balance  
- Danh sách positions đang mở với đầy đủ thông tin

### 2. Closed Trades Push
EA sẽ gửi lịch sử các trades đã đóng trong khoảng thời gian `LookbackHoursClosedTrades`

### 3. Error Handling & Retry
- Tự động retry khi request thất bại
- Logging chi tiết trong tab Expert/Journal
- Xử lý timeout và network errors

## So sánh với Python Script

| Tính năng | Python Script | EA MQL5 |
|-----------|---------------|---------|
| Multi-account | ✅ (từ config file) | ❌ (1 account/EA) |
| Config file | ✅ (INI file) | ❌ (EA parameters) |
| Dependencies | ✅ (Python, packages) | ❌ (chỉ cần MT5) |
| Performance | ⚠️ (external process) | ✅ (native MT5) |
| SSL config | ✅ (flexible) | ⚠️ (MT5 restrictions) |
| Logging | ✅ (file + console) | ✅ (MT5 journal) |

## Lưu ý quan trọng

### 1. WebRequest Permissions
EA cần được cấp quyền WebRequest. Nếu không cấu hình đúng, EA sẽ không thể gửi data.

### 2. Multi-account Support  
Khác với Python script có thể xử lý nhiều account, EA chỉ xử lý 1 account tại một thời điểm. Để xử lý nhiều account:
- Attach EA vào nhiều chart khác nhau
- Cấu hình `TradingAccountIdSystem` khác nhau cho mỗi EA instance

### 3. API Key Security
Không hard-code API key trong EA parameters. Sử dụng cách bảo mật khác nếu cần.

### 4. Error Monitoring
Theo dõi tab Expert/Journal để kiểm tra logs và errors.

## Troubleshooting

### WebRequest Error -1
```
Lỗi WebRequest: 5203
```
**Giải pháp:** Cấu hình WebRequest permissions như mục 2 ở trên.

### HTTP Error 403/401
```
HTTP Error 403: Unauthorized
```
**Giải pháp:** Kiểm tra API Key có đúng không.

### Timeout Errors
```
HTTP Error 0: Request timeout
```
**Giải pháp:** Tăng `RequestTimeoutSeconds` hoặc kiểm tra kết nối mạng.

## Migration từ Python

1. Stop Python script
2. Cài đặt và cấu hình EA như hướng dẫn trên
3. Verify data vẫn được push correctly
4. Remove Python script khỏi scheduled tasks/services

## Support

Nếu gặp vấn đề, kiểm tra:
1. MT5 Journal logs
2. Network connectivity
3. API endpoint availability
4. WebRequest permissions