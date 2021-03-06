using MassTransit;
using Play.Common.Repositories.Abstractions;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers;

public class SubstractItemsConsumer : IConsumer<SubstractItems>
{
    private readonly IRepository<InventoryItem> _inventoryItemsRepository;
    private readonly IRepository<CatalogItem> _catalogItemsRepository;

    public SubstractItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository,
        IRepository<CatalogItem> catalogItemsRepository)
    {
        _inventoryItemsRepository = inventoryItemsRepository;
        _catalogItemsRepository = catalogItemsRepository;
    }

    public async Task Consume(ConsumeContext<SubstractItems> context)
    {
        var message = context.Message;
        var catalogItem = await _catalogItemsRepository.GetAsync(message.CatalogItemId);
        if (catalogItem is null)
        {
            throw new UnknownItemException(message.CatalogItemId);
        }
        
        var inventoryItem = await _inventoryItemsRepository
            .GetAsync(i => i.UserId == message.UserId && i.CatalogItemId == message.CatalogItemId);

        if (inventoryItem is not null)
        {
            if (inventoryItem.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new InventoryItemsGranted(message.CorrelationId));
            }
            
            inventoryItem.Quantity -= message.Quantity;
            inventoryItem.MessageIds.Add(context.MessageId.Value);
            
            await _inventoryItemsRepository.UpdateAsync(inventoryItem);
            
            await context.Publish(new InventoryItemUpdated(
                inventoryItem.UserId,
                inventoryItem.CatalogItemId,
                inventoryItem.Quantity));
        }

        await context.Publish(new InventoryItemsSubstracted(message.CorrelationId));
    }
}