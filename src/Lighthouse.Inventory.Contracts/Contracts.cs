namespace Lighthouse.Inventory.Contracts;

public record GrantItems(
    Guid UserId,
    Guid CatalogItemId,
    int Quantity,
    Guid CorrelationId
);

public record InventoryItemsGranted(Guid CorrelationId);

public record RemoveItems(
    Guid UserId,
    Guid CatalogItemId,
    int Quantity,
    Guid CorrelationId
);

public record InventoryItemsRemoved(Guid CorrelationId);

