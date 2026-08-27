using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Systems;

public class PaymentMethodSeeder
{
    private readonly ICommandRepository<PaymentMethod> _repository;
    private readonly IUnitOfWork _unitOfWork;

    public PaymentMethodSeeder(
        ICommandRepository<PaymentMethod> repository,
        IUnitOfWork unitOfWork
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var methods = new List<PaymentMethod>
        {
            new PaymentMethod { Name = "Cash",           Code = "CASH",   SystemMethod = true },
            new PaymentMethod { Name = "Bank Transfer",  Code = "BANK",   SystemMethod = true },
            new PaymentMethod { Name = "Card",           Code = "CARD",   SystemMethod = true },
            new PaymentMethod { Name = "Mobile Banking", Code = "MOBILE", SystemMethod = true },
            new PaymentMethod { Name = "Cheque",         Code = "CHEQUE", SystemMethod = true }
        };

        foreach (var method in methods)
        {
            await _repository.CreateAsync(method);
        }

        await _unitOfWork.SaveAsync();
    }
}
