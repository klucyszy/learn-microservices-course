using Microsoft.AspNetCore.Mvc;
using Play.Common.Repositories.Abstractions;
using Play.Inventory.Service.Clients;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private const string UnknownText = "unknown";
    
    private readonly IRepository<InventoryItem> _repository;
    private readonly CatalogClient _catalogClient;

    public ItemsController(IRepository<InventoryItem> repository, CatalogClient catalogClient)
    {
        _repository = repository;
        _catalogClient = catalogClient;
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid id)
    {
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        var catalogItems = await _catalogClient.GetCatalogItemsAsync();
        var inventoryItems = await _repository
            .GetAllAsync(i => i.UserId == id);

        var items = inventoryItems.Select(i =>
        {
            var catalogItem = catalogItems.SingleOrDefault(ci => ci.Id == i.CatalogItemId);
            
            return i.AsDto(catalogItem?.Name ?? UnknownText, catalogItem?.Description ?? UnknownText);
        });

        return Ok(items);
    }

    [HttpPost]
    public async Task<ActionResult> PostAsync(GrantItemDto item)
    {
        var inventoryItem = await _repository
            .GetAsync(i => i.UserId == item.UserId && i.CatalogItemId == item.CatalogItemId);

        if (inventoryItem != null)
        {
            inventoryItem.Quantity += item.Quantity;

            await _repository.UpdateAsync(inventoryItem);
            
            return NoContent();
        }

        var newInventoryItem = new InventoryItem
        {
            Id = Guid.NewGuid(),
            UserId = item.UserId,
            CatalogItemId = item.CatalogItemId,
            AcquiredDate = DateTimeOffset.UtcNow,
            Quantity = item.Quantity,
        };

        await _repository.CreateAsync(newInventoryItem);

        return Created(string.Empty, new { Id = newInventoryItem.Id });
    }
}