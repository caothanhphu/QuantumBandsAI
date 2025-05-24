// QuantumBands.Domain/Entities/Enums/OfferingStatus.cs
namespace QuantumBands.Domain.Entities.Enums;

public enum OfferingStatus
{
    Active,     // Đợt chào bán đang diễn ra
    Pending,    // Đợt chào bán đang chờ bắt đầu (nếu có ngày bắt đầu trong tương lai)
    Completed,  // Đợt chào bán đã hoàn thành (ví dụ: bán hết hoặc hết hạn và có người mua)
    Cancelled,  // Đợt chào bán đã bị Admin hủy
    Expired     // Đợt chào bán đã hết hạn mà không có hoặc không bán hết cổ phần (tùy chọn)
}