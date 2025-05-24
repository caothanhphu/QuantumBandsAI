// QuantumBands.Domain/Entities/Enums/ShareOrderStatusName.cs
namespace QuantumBands.Domain.Entities.Enums;

public enum ShareOrderStatusName
{
    Open,            // Lệnh đang mở, chờ khớp
    PartiallyFilled, // Lệnh đã khớp một phần
    Filled,          // Lệnh đã khớp toàn bộ
    Cancelled,       // Lệnh đã bị hủy
    Expired,         // Lệnh đã hết hạn (nếu có cơ chế này)
    PendingExecution // Lệnh thị trường đang chờ thực thi (tùy chọn)
    // Thêm các trạng thái khác nếu cần
}