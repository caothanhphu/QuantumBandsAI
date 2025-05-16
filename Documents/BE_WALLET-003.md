## Tài liệu API: Nạp tiền vào Ví (Chuyển khoản Ngân hàng & Admin)

Base URL API: `/api/v1`

---

### A. Luồng Nạp tiền qua Chuyển khoản Ngân hàng (Dành cho Nhà đầu tư & Admin)

#### 1. Nhà đầu tư: Khởi tạo Yêu cầu Nạp tiền

Người dùng nhập số tiền USD muốn nạp. API sẽ tính toán số tiền VND tương ứng dựa trên tỷ giá hệ thống, tạo một mã tham chiếu duy nhất, và trả về thông tin tài khoản ngân hàng của công ty để người dùng thực hiện chuyển khoản.

* **Endpoint:** `POST /wallets/me/deposits/bank/initiate`
* **Yêu cầu Xác thực:** Có (JWT Bearer token).
* **Request Body:** `InitiateBankDepositRequest`
    ```json
    {
      "amountUSD": 100.00 // Số tiền USD người dùng muốn nạp
    }
    ```
* **Response Body (Success - 200 OK):** `BankDepositInfoResponse`
    ```json
    {
      "transactionId": 12345, // ID của WalletTransaction vừa được tạo
      "requestedAmountUSD": 100.00,
      "amountVND": 2500000, // Số tiền VND tương đương cần chuyển
      "exchangeRate": 25000.00, // Tỷ giá đã áp dụng
      "bankName": "NGAN HANG TMCP NGOAI THUONG VIET NAM (VIETCOMBANK)",
      "accountHolder": "CONG TY TNHH FINIX AI",
      "accountNumber": "00123456789XYZ",
      "referenceCode": "FINIXDEPABC123XYZ" // Mã tham chiếu duy nhất cho giao dịch này (nội dung chuyển khoản)
    }
    ```
* **Response Body (Error):**
    * `400 Bad Request`: Dữ liệu request không hợp lệ.
    * `401 Unauthorized`.
    * `500 Internal Server Error`: Lỗi server (ví dụ: không lấy được tỷ giá, lỗi tạo mã tham chiếu).

#### 2. Admin: Xác nhận Giao dịch Nạp tiền Ngân hàng

Sau khi người dùng chuyển khoản và Admin nhận được thông báo (quy trình ngoài hệ thống), Admin sẽ sử dụng API này để xác nhận giao dịch.

* **Endpoint:** `POST /admin/wallets/deposits/bank/confirm`
* **Yêu cầu Xác thực:** Có (JWT Bearer token) và **Yêu cầu Vai trò:** `Admin`.
* **Request Body:** `ConfirmBankDepositRequest`
    ```json
    {
      "transactionId": 12345, // ID của WalletTransaction cần xác nhận
      "actualAmountVNDReceived": 2500000, // Số tiền VND thực nhận (tùy chọn, để đối chiếu)
      "adminNotes": "User confirmed transfer with correct reference code." // Ghi chú của Admin (tùy chọn)
    }
    ```
* **Response Body (Success - 200 OK):** `WalletTransactionDto` (chi tiết giao dịch đã được cập nhật trạng thái "Completed" và số dư đã được cộng)
* **Response Body (Error):**
    * `400 Bad Request`: `transactionId` không hợp lệ, hoặc giao dịch không ở trạng thái chờ xác nhận.
    * `401 Unauthorized`, `403 Forbidden`.
    * `404 Not Found`: `transactionId` không tồn tại.
    * `500 Internal Server Error`.

#### 3. Admin: Hủy Yêu cầu Nạp tiền Ngân hàng

Admin có thể hủy một yêu cầu nạp tiền đang chờ xử lý.

* **Endpoint:** `POST /admin/wallets/deposits/bank/cancel`
* **Yêu cầu Xác thực:** Có (JWT Bearer token) và **Yêu cầu Vai trò:** `Admin`.
* **Request Body:** `CancelBankDepositRequest`
    ```json
    {
      "transactionId": 12345, // ID của WalletTransaction cần hủy
      "adminNotes": "User requested cancellation / Duplicate request." // Lý do hủy
    }
    ```
* **Response Body (Success - 200 OK):** `WalletTransactionDto` (chi tiết giao dịch đã được cập nhật trạng thái "Cancelled")
* **Response Body (Error):**
    * `400 Bad Request`: `transactionId` không hợp lệ, hoặc giao dịch không ở trạng thái có thể hủy.
    * `401 Unauthorized`, `403 Forbidden`.
    * `404 Not Found`: `transactionId` không tồn tại.
    * `500 Internal Server Error`.

---

### B. Nạp tiền Trực tiếp bởi Admin (Giữ nguyên như thiết kế trước)

Admin có thể cộng tiền trực tiếp vào ví của một người dùng.

* **Endpoint:** `POST /admin/wallets/deposit`
* **Yêu cầu Xác thực:** Có (JWT Bearer token) và **Yêu cầu Vai trò:** `Admin`.
* **Request Body:** `AdminDirectDepositRequest`
    ```json
    {
      "userId": 456,
      "amount": 25.50, // Số tiền USD
      "currencyCode": "USD",
      "description": "Bonus for Q1 performance",
      "referenceId": "ADMIN_BONUS_Q1_2025" // Optional
    }
    ```
* **Response Body (Success - 200 OK):** `WalletTransactionDto`
* **Response Body (Error):**
    * `400 Bad Request`, `401 Unauthorized`, `403 Forbidden`, `404 Not Found`, `500 Internal Server Error`.

---

### Định nghĩa DTOs (Data Transfer Objects)

**`InitiateBankDepositRequest`**

| Thuộc tính  | Kiểu dữ liệu | Mô tả                        | Bắt buộc |
| :---------- | :----------- | :--------------------------- | :------- |
| `amountUSD` | number       | Số tiền USD người dùng muốn nạp. | Có       |

**`BankDepositInfoResponse`**

| Thuộc tính           | Kiểu dữ liệu | Mô tả                                                    |
| :------------------- | :----------- | :------------------------------------------------------- |
| `transactionId`      | integer (long) | ID của `WalletTransaction` được tạo.                     |
| `requestedAmountUSD` | number       | Số tiền USD người dùng yêu cầu nạp.                       |
| `amountVND`          | number       | Số tiền VND tương đương cần chuyển khoản.                 |
| `exchangeRate`       | number       | Tỷ giá USD/VND đã được áp dụng.                          |
| `bankName`           | string       | Tên ngân hàng nhận tiền.                                 |
| `accountHolder`      | string       | Tên chủ tài khoản nhận tiền.                             |
| `accountNumber`      | string       | Số tài khoản nhận tiền.                                  |
| `referenceCode`      | string       | Mã tham chiếu duy nhất (nội dung chuyển khoản) cho giao dịch này. |

**`ConfirmBankDepositRequest`**

| Thuộc tính                  | Kiểu dữ liệu | Mô tả                                                    | Bắt buộc |
| :-------------------------- | :----------- | :------------------------------------------------------- | :------- |
| `transactionId`             | integer (long) | ID của `WalletTransaction` đang chờ xác nhận.             | Có       |
| `actualAmountVNDReceived`   | number       | Số tiền VND thực tế nhận được (để Admin đối chiếu).       | Không    |
| `adminNotes`                | string       | Ghi chú của Admin.                                       | Không    |

**`CancelBankDepositRequest`**

| Thuộc tính      | Kiểu dữ liệu | Mô tả                             | Bắt buộc |
| :-------------- | :----------- | :-------------------------------- | :------- |
| `transactionId` | integer (long) | ID của `WalletTransaction` cần hủy. | Có       |
| `adminNotes`    | string       | Lý do hủy, ghi chú của Admin.      | Có       |

**`AdminDirectDepositRequest`** (Như cũ)

| Thuộc tính     | Kiểu dữ liệu | Mô tả                                               | Bắt buộc |
| :------------- | :----------- | :-------------------------------------------------- | :------- |
| `userId`       | integer      | ID của người dùng sẽ nhận tiền.                     | Có       |
| `amount`       | number       | Số tiền nạp (USD).                                   | Có       |
| `currencyCode` | string       | Mã tiền tệ (mặc định "USD").                        | Có       |
| `description`  | string       | Mô tả cho giao dịch.                                | Có       |
| `referenceId`  | string       | ID tham chiếu tùy chọn do admin cung cấp.             | Không    |

*(`WalletTransactionDto` đã được định nghĩa/cập nhật ở BE-WALLET-002)*