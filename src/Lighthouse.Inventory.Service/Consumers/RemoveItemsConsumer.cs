using System;
using System.Threading.Tasks;
using Lighthouse.Common;
using Lighthouse.Inventory.Contracts;
using Lighthouse.Inventory.Exceptions;
using Lighthouse.Inventory.Service.Entities;
using MassTransit;

namespace Lighthouse.Inventory.Consumers;

public class RemoveItemsConsumer : IConsumer<RemoveItems>
{
    private readonly IRepository<InventoryItem> _inventoryItemsRepository;
    private readonly IRepository<CatalogItem> _catalogItemsRepository;
    public RemoveItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository, IRepository<CatalogItem> catalogItemsRepository)
    {
        _inventoryItemsRepository = inventoryItemsRepository;
        _catalogItemsRepository = catalogItemsRepository;
    }
    public async Task Consume(ConsumeContext<RemoveItems> context)
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

        if (existingItem is not null)
        {
            existingItem.Quantity -= message.Quantity;
            await _inventoryItemsRepository.UpdateAsync(existingItem);
        }

        await context.Publish(new InventoryItemsRemoved(message.CorrelationId));
    }
}