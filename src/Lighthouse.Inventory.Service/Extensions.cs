using Lighthouse.Inventory.Service.Entities;

namespace Lighthouse.Inventory.Service;

public static class Extensions
{
    public static InventoryItemDto AsDto(this InventoryItem item, string name, string description)
    {
        return new InventoryItemDto(
            item.ItemId,
            name,
            description,
            item.Quantity,
            item.AcquiredDate
        );
    }
}