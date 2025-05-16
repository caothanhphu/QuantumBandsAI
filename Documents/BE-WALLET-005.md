## Tài liệu API: Chuyển tiền Nội bộ (Internal Fund Transfer)

Base URL API: `/api/v1`

---

### A. Xác minh Thông tin Người nhận

Trước khi thực hiện chuyển tiền, người gửi nên xác minh email của người nhận để đảm bảo tính chính xác.

* **Endpoint:** `POST /wallets/me/internal-transfer/verify-recipient`
* **Yêu cầu Xác thực:** Có (JWT Bearer token).
* **Mô tả:** Kiểm tra xem một địa chỉ email có tồn tại trong hệ thống và thuộc về một người dùng đã đăng ký hay không. Nếu có, trả về một số thông tin cơ bản của người nhận.
* **Request Body:** `VerifyRecipientRequest`
    ```json
    {
      "recipientEmail": "receiver@example.com"
    }
    ```
* **Response Body (Success - 200 OK):** `RecipientInfoResponse`
    ```json
    {
      "recipientUserId": 789,
      "recipientUsername": "receiverUser",
      "recipientFullName": "Receiver Full Name" // Có thể null
    }
    ```
* **Response Body (Error):**
    * `400 Bad Request`: Email không hợp lệ.
    * `401 Unauthorized`.
    * `404 Not Found`: Email không tồn tại trong hệ thống.
        ```json
        { "message": "Recipient email not found or is invalid." }
        ```
    * `500 Internal Server Error`.

---

### B. Thực hiện Chuyển tiền Nội bộ

Sau khi người nhận đã được xác minh (tùy chọn nhưng khuyến nghị), người gửi thực hiện chuyển tiền.

* **Endpoint:** `POST /wallets/me/internal-transfer/execute`
* **Yêu cầu Xác thực:** Có (JWT Bearer token).
* **Mô tả:** Chuyển một số tiền từ ví của người dùng hiện tại sang ví của một người dùng khác trong hệ thống, dựa trên `RecipientUserId`.
* **Request Body:** `ExecuteInternalTransferRequest`
    ```json
    {
      "recipientUserId": 789, // ID của người nhận, lấy từ bước verify-recipient
      "amount": 25.00,
      "currencyCode": "USD", // Hiện tại chỉ hỗ trợ USD
      "description": "Trả tiền ăn trưa" // Optional
    }
    ```
* **Response Body (Success - 200 OK):** `WalletTransactionDto` (chi tiết giao dịch ghi nợ từ ví người gửi)
    ```json
    {
      "transactionId": 791,
      "transactionTypeName": "InternalTransferSent",
      "amount": 25.00,
      "currencyCode": "USD",
      "balanceAfter": 1601.25,
      "referenceId": "TRANSFER_TO_USER_789", // Chứa ID của người nhận
      "paymentMethod": "InternalTransfer",
      "externalTransactionId": null,
      "description": "Trả tiền ăn trưa (Gửi tới UserID 789)",
      "status": "Completed",
      "transactionDate": "2025-05-15T12:00:00Z"
    }
    ```
* **Response Body (Error):**
    * `400 Bad Request`: Dữ liệu không hợp lệ, số dư không đủ, hoặc `recipientUserId` không hợp lệ/không tìm thấy, hoặc cố gắng chuyển cho chính mình.
    * `401 Unauthorized`.
    * `404 Not Found`: Ví của người gửi hoặc người nhận không tìm thấy (hiếm khi xảy ra nếu user tồn tại).
    * `500 Internal Server Error`.

---

### Định nghĩa DTOs

**`VerifyRecipientRequest`**

| Thuộc tính       | Kiểu dữ liệu | Mô tả                             | Bắt buộc |
| :--------------- | :----------- | :-------------------------------- | :------- |
| `recipientEmail` | string       | Email của người dùng cần xác minh. | Có       |

**`RecipientInfoResponse`**

| Thuộc tính            | Kiểu dữ liệu | Mô tả                                |
| :-------------------- | :----------- | :----------------------------------- |
| `recipientUserId`     | integer      | ID của người dùng nhận.              |
| `recipientUsername`   | string       | Username của người dùng nhận.        |
| `recipientFullName`   | string (nullable) | Tên đầy đủ của người dùng nhận.    |

**`ExecuteInternalTransferRequest`**

| Thuộc tính        | Kiểu dữ liệu | Mô tả                                            | Bắt buộc |
| :---------------- | :----------- | :----------------------------------------------- | :------- |
| `recipientUserId` | integer      | ID của người dùng sẽ nhận tiền.                  | Có       |
| `amount`          | number       | Số tiền muốn chuyển.                             | Có       |
| `currencyCode`    | string       | Mã tiền tệ (hiện tại chỉ "USD").                | Có       |
| `description`     | string       | Mô tả tùy chọn cho giao dịch chuyển tiền.        | Không    |

*(`WalletTransactionDto` đã được định nghĩa/cập nhật ở BE-WALLET-002)*