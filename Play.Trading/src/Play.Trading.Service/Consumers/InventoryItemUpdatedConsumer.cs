using MassTransit;
using Play.Common.Repositories.Abstractions;
using Play.Inventory.Contracts;
using Play.Trading.Service.Entities;

namespace Play.Trading.Service.Consumers;

public class InventoryItemUpdatedConsumer : IConsumer<InventoryItemUpdated>
{
    private readonly IRepository<InventoryItem> _repository;

    public InventoryItemUpdatedConsumer(IRepository<InventoryItem> repository)
    {
        _repository = repository;
    }

    public async Task Consume(ConsumeContext<InventoryItemUpdated> context)
    {
        var message = context.Message;

        var inventoryItem = await _repository.GetAsync(item =>
            item.UserId == message.UserId && item.CatalogItemId == message.CatalogItemId);

        if (inventoryItem is null)
        {
            inventoryItem = new InventoryItem
            {
                CatalogItemId = message.CatalogItemId,
                UserId = message.UserId,
                Quantity = message.NewItemQuantity
            };

            await _repository.CreateAsync(inventoryItem);
        }
        else
        {
            inventoryItem.Quantity = message.NewItemQuantity;

            await _repository.UpdateAsync(inventoryItem);
        }
    }
}