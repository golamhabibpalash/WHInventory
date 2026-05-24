using Application.Common.Repositories;
using Domain.Entities;

namespace Infrastructure.SeedManager.Demos;

public class ProductGroupSeeder
{
    private readonly ICommandRepository<ProductGroup> _productGroupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductGroupSeeder(
        ICommandRepository<ProductGroup> productGroupRepository,
        IUnitOfWork unitOfWork
    )
    {
        _productGroupRepository = productGroupRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task GenerateDataAsync()
    {
        var hardware = new ProductGroup { Name = "Hardware" };
        var networking = new ProductGroup { Name = "Networking" };
        var storage = new ProductGroup { Name = "Storage" };
        var software = new ProductGroup { Name = "Software" };
        var service = new ProductGroup { Name = "Service" };

        await _productGroupRepository.CreateAsync(hardware);
        await _productGroupRepository.CreateAsync(networking);
        await _productGroupRepository.CreateAsync(storage);
        await _productGroupRepository.CreateAsync(software);
        await _productGroupRepository.CreateAsync(service);

        var device = new ProductGroup { Name = "Device", ParentId = hardware.Id };
        var motherboard = new ProductGroup { Name = "Motherboard", ParentId = hardware.Id };
        var cpu = new ProductGroup { Name = "CPU", ParentId = hardware.Id };
        var ram = new ProductGroup { Name = "RAM", ParentId = hardware.Id };

        await _productGroupRepository.CreateAsync(device);
        await _productGroupRepository.CreateAsync(motherboard);
        await _productGroupRepository.CreateAsync(cpu);
        await _productGroupRepository.CreateAsync(ram);

        var router = new ProductGroup { Name = "Router", ParentId = networking.Id };
        var switch_ = new ProductGroup { Name = "Switch", ParentId = networking.Id };
        var cable = new ProductGroup { Name = "Cable", ParentId = networking.Id };

        await _productGroupRepository.CreateAsync(router);
        await _productGroupRepository.CreateAsync(switch_);
        await _productGroupRepository.CreateAsync(cable);

        var hdd = new ProductGroup { Name = "HDD", ParentId = storage.Id };
        var ssd = new ProductGroup { Name = "SSD", ParentId = storage.Id };
        var nas = new ProductGroup { Name = "NAS", ParentId = storage.Id };

        await _productGroupRepository.CreateAsync(hdd);
        await _productGroupRepository.CreateAsync(ssd);
        await _productGroupRepository.CreateAsync(nas);

        await _unitOfWork.SaveAsync();
    }
}
