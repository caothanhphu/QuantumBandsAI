Tài liệu API: Xác thực Hai Yếu Tố (2FA)
Base URL: /api/v1/users/2fa

Tất cả các endpoint dưới đây yêu cầu người dùng đã được xác thực (JWT Bearer token hợp lệ).

1. Bắt đầu Thiết lập 2FA
Bắt đầu quá trình thiết lập 2FA cho người dùng hiện tại. API sẽ tạo ra một khóa bí mật mới và một URI để hiển thị mã QR.

Endpoint: POST /setup

Mô tả:

Nếu người dùng chưa bật 2FA, API sẽ tạo một khóa bí mật mới (shared key) và một authenticator URI.

Khóa bí mật này nên được hiển thị cho người dùng để họ có thể nhập thủ công vào ứng dụng xác thực.

Authenticator URI được dùng để frontend tạo mã QR cho người dùng quét bằng ứng dụng xác thực (ví dụ: Google Authenticator, Authy).

Khóa bí mật này sẽ được lưu trữ tạm thời (hoặc gắn với user với trạng thái "pending setup") cho đến khi người dùng xác minh và kích hoạt 2FA.

Request Body: Không có.

Response Body (Success - 200 OK):

{
  "sharedKey": "YOUR_BASE32_ENCODED_SECRET_KEY", // Khóa bí mật để người dùng nhập thủ công
  "authenticatorUri": "otpauth://totp/QuantumBandsAI:user@example.com?secret=YOUR_BASE32_ENCODED_SECRET_KEY&issuer=QuantumBandsAI&digits=6&period=30" // URI để tạo mã QR
}

Response Body (Error):

400 Bad Request: Nếu 2FA đã được kích hoạt cho người dùng này.

{
  "message": "Two-Factor Authentication is already enabled for this account."
}

500 Internal Server Error: Nếu có lỗi server.

2. Kích hoạt 2FA
Xác minh mã từ ứng dụng xác thực và kích hoạt 2FA cho người dùng.

Endpoint: POST /enable

Mô tả:

Người dùng nhập mã 6 chữ số từ ứng dụng xác thực của họ sau khi quét mã QR hoặc nhập khóa bí mật từ bước /setup.

API sẽ xác minh mã này với khóa bí mật đã được tạo ở bước /setup.

Nếu mã hợp lệ, 2FA sẽ được kích hoạt cho tài khoản người dùng, và khóa bí mật sẽ được lưu trữ an toàn (đã mã hóa) phía server.

Request Body:

{
  "verificationCode": "123456" // Mã 6 chữ số từ ứng dụng xác thực
}

Response Body (Success - 200 OK):

{
  "message": "Two-Factor Authentication enabled successfully."
  // (Nên cân nhắc trả về một bộ Recovery Codes ở đây cho người dùng lưu trữ)
  // "recoveryCodes": ["code1", "code2", ...]
}

Response Body (Error):

400 Bad Request:

Nếu mã xác thực không hợp lệ.

{
  "message": "Invalid verification code."
}

Nếu chưa thực hiện bước /setup hoặc khóa bí mật tạm thời không tìm thấy/hết hạn.

{
  "message": "2FA setup process not initiated or has expired. Please start setup again."
}

500 Internal Server Error: Nếu có lỗi server.

3. Xác minh Mã 2FA (Cho hành động nhạy cảm hoặc bước thứ hai của login)
Xác minh mã 2FA hiện tại của người dùng. Endpoint này được sử dụng khi người dùng cần thực hiện một hành động nhạy cảm yêu cầu xác minh 2FA bổ sung, hoặc là bước thứ hai của quá trình đăng nhập nếu 2FA đã được bật.

Endpoint: POST /verify

Mô tả:

Người dùng nhập mã 6 chữ số hiện tại từ ứng dụng xác thực của họ.

API sẽ xác minh mã này với khóa bí mật đã được lưu trữ an toàn của người dùng.

Endpoint này không được dùng để kích hoạt 2FA, mà để xác minh mã cho một người dùng đã bật 2FA.

Request Body:

{
  "verificationCode": "654321" // Mã 6 chữ số hiện tại từ ứng dụng xác thực
}

Response Body (Success - 200 OK):

{
  "success": true,
  "message": "2FA verification successful."
}

(Lưu ý: Nếu đây là bước thứ hai của login, response thành công có thể là LoginResponse đầy đủ với JWT và Refresh Token).

Response Body (Error):

400 Bad Request:

Nếu 2FA chưa được kích hoạt cho tài khoản này.

{
  "success": false,
  "message": "2FA is not enabled for this account."
}

Nếu mã xác thực không hợp lệ.

{
  "success": false,
  "message": "Invalid 2FA verification code."
}

500 Internal Server Error: Nếu có lỗi server.

4. Vô hiệu hóa 2FA
Vô hiệu hóa 2FA cho tài khoản người dùng hiện tại.

Endpoint: POST /disable

Mô tả:

Để vô hiệu hóa 2FA, người dùng cần cung cấp một mã xác thực 2FA hợp lệ hiện tại (để chứng minh họ vẫn kiểm soát ứng dụng xác thực) hoặc mật khẩu hiện tại của họ. Trong ví dụ này, chúng ta sẽ yêu cầu mã 2FA hiện tại.

Request Body:

{
  "verificationCode": "123456" // Mã 6 chữ số hiện tại từ ứng dụng xác thực
                              // Hoặc có thể là "password": "currentPassword" tùy theo thiết kế
}

Response Body (Success - 200 OK):

{
  "message": "Two-Factor Authentication disabled successfully."
}

Response Body (Error):

400 Bad Request:

Nếu 2FA chưa được kích hoạt.

{
  "message": "2FA is not currently enabled for this account."
}

Nếu mã xác thực (hoặc mật khẩu) không hợp lệ.

{
  "message": "Invalid verification code (or password)."
}

500 Internal Server Error: Nếu có lỗi server.

Lưu ý quan trọng cho Frontend:

QR Code: Frontend sẽ cần một thư viện để tạo mã QR từ authenticatorUri được cung cấp bởi endpoint /setup.

Recovery Codes: Khi 2FA được kích hoạt thành công (sau endpoint /enable), API nên trả về một danh sách các mã khôi phục (recovery codes). Frontend phải hiển thị các mã này cho người dùng một cách rõ ràng và yêu cầu họ lưu trữ ở nơi an toàn. Việc sử dụng recovery codes để đăng nhập khi mất thiết bị 2FA là một ticket riêng nhưng rất quan trọng.

Luồng Đăng nhập với 2FA:

Người dùng POST username/password đến /api/v1/auth/login.

Nếu password đúng và user có 2FA bật, API /login nên trả về một response đặc biệt (ví dụ: HTTP 200 OK với {"isTwoFactorRequired": true, "userId": 123} hoặc một "partial token").

Frontend hiển thị ô nhập mã 2FA.

Người dùng POST userId và verificationCode đến một endpoint mới, ví dụ POST /api/v1/auth/verify-2fa-login.

Nếu mã đúng, endpoint này trả về LoginResponse đầy đủ (JWT, Refresh Token).
(Ticket BE-AUTH-010 hiện tại định nghĩa /api/v1/users/2fa/verify, ngụ ý người dùng đã có JWT. Nếu mục đích là cho luồng đăng nhập, endpoint nên nằm trong AuthController và không yêu cầu JWT ban đầu).