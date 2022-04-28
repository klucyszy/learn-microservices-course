using MassTransit;
using Play.Common.Repositories.Abstractions;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Entities;
using Play.Inventory.Service.Exceptions;

namespace Play.Inventory.Service.Consumers;

public class GrantItemsConsumer : IConsumer<GrantItems>
{
    private readonly IRepository<InventoryItem> _inventoryItemsRepository;
    private readonly IRepository<CatalogItem> _catalogItemsRepository;

    public GrantItemsConsumer(IRepository<InventoryItem> inventoryItemsRepository,
        IRepository<CatalogItem> catalogItemsRepository)
    {
        _inventoryItemsRepository = inventoryItemsRepository;
        _catalogItemsRepository = catalogItemsRepository;
    }

    public async Task Consume(ConsumeContext<GrantItems> context)
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
            
            inventoryItem.Quantity += message.Quantity;
            inventoryItem.MessageIds.Add(context.MessageId.Value);

            await _inventoryItemsRepository.UpdateAsync(inventoryItem);
        }
        else
        {
            var newInventoryItem = new InventoryItem
            {
                Id = Guid.NewGuid(),
                UserId = message.UserId,
                CatalogItemId = message.CatalogItemId,
                AcquiredDate = DateTimeOffset.UtcNow,
                Quantity = message.Quantity,
            };

            newInventoryItem.MessageIds.Add(context.MessageId.Value);

            await _inventoryItemsRepository.CreateAsync(newInventoryItem);
        }

        var itemsGrantedTask = context.Publish(new InventoryItemsGranted(message.CorrelationId));
        var inventoryUpdatedTask = context.Publish(new InventoryItemUpdated(
            inventoryItem.UserId,
            inventoryItem.CatalogItemId,
            inventoryItem.Quantity));
        
        await Task.WhenAll(itemsGrantedTask, inventoryUpdatedTask);
    }
}