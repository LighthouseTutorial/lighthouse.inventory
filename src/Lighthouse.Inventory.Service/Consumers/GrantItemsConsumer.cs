using System;
using System.Threading.Tasks;
using Lighthouse.Common;
using Lighthouse.Inventory.Contracts;
using Lighthouse.Inventory.Exceptions;
using Lighthouse.Inventory.Service.Entities;
using MassTransit;

namespace Lighthouse.Inventory.Consumers;

public class GrantItemsConsumer : IConsumer<GrantItems>
{
    private readonly IRepository<InventoryItem> _inventoryItemsRepository;
    private readonly IRepository<CatalogItem> _catalogItemsRepository;
    public GrantItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository)
    {
        _inventoryItemsRepository = inventoryItemsRepository;
        _catalogItemsRepository = catalogItemsRepository;
    }
    public async Task Consume(ConsumeContext<GrantItems> context)
    {
        var message = context.Message;

        var item = await _catalogItemsRepository.GetAsync(message.CatalogItemId);

        if (item is null)
        {
            throw new UnknownItemException(message.CatalogItemId);
        }

        var existingItem = await _inventoryItemsRepository.GetAsync(
            item => item.UserId == message.UserId
            && item.ItemId == message.CatalogItemId
        );

        if (existingItem is null)
        {
            var newItem = new InventoryItem
            {
                ItemId = message.CatalogItemId,
                UserId = message.UserId,
                Quantity = message.Quantity,
                AcquiredDate = DateTimeOffset.UtcNow
            };

            await _inventoryItemsRepository.CreateAsync(newItem);
        }
        else
        {
            existingItem.Quantity += message.Quantity;
            await _inventoryItemsRepository.UpdateAsync(existingItem);
        }

        await context.Publish(new InventoryItemsGranted(message.CorrelationId));
    }
}