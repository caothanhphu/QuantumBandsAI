// QuantumBands.Application/Features/Exchange/Commands/CreateOrder/CreateShareOrderRequestValidator.cs
using FluentValidation;
using System; // For StringComparison

namespace QuantumBands.Application.Features.Exchange.Commands.CreateOrder;

public class CreateShareOrderRequestValidator : AbstractValidator<CreateShareOrderRequest>
{
    public CreateShareOrderRequestValidator() // Có thể inject IUnitOfWork để kiểm tra OrderTypeId tồn tại
    {
        RuleFor(x => x.TradingAccountId)
            .GreaterThan(0).WithMessage("Trading Account ID must be valid.");

        RuleFor(x => x.OrderTypeId)
            .GreaterThan(0).WithMessage("Order Type ID must be valid.");
        // TODO: Thêm rule để kiểm tra OrderTypeId có tồn tại trong DB không (cần inject IUnitOfWork)
        // .MustAsync(async (id, cancellation) => await unitOfWork.ShareOrderTypes.ExistsAsync(id))
        // .WithMessage("Specified Order Type ID does not exist.");

        RuleFor(x => x.OrderSide)
            .NotEmpty().WithMessage("Order side is required.")
            .Must(side => side.Equals("Buy", StringComparison.OrdinalIgnoreCase) ||
                           side.Equals("Sell", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Order side must be 'Buy' or 'Sell'.");

        RuleFor(x => x.QuantityOrdered)
            .GreaterThan(0).WithMessage("Quantity ordered must be greater than 0.");

        RuleFor(x => x.LimitPrice)
            .GreaterThan(0).WithMessage("Limit price must be greater than 0.")
            .When(x => x.LimitPrice.HasValue); // Chỉ validate nếu có giá trị
                                               // Logic kiểm tra LimitPrice có bắt buộc cho OrderType "Limit" hay không
                                               // nên được xử lý trong service hoặc dựa vào tên OrderType thay vì ID.
                                               // Hoặc validator này có thể phức tạp hơn nếu biết ID của "Limit" OrderType.
    }
}