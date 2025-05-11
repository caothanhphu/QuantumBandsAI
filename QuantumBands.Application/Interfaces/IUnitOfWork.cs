// QuantumBands.Application/Interfaces/IUnitOfWork.cs
namespace QuantumBands.Application.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IUserRoleRepository UserRoles { get; } // Ví dụ
    // Thêm các repository khác ở đây
    // IUserRepository Users { get; }

    Task<int> CompleteAsync(); // Để gọi SaveChangesAsync
}