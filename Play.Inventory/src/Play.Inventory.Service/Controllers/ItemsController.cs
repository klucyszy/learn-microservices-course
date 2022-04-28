using System.Security.Claims;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using Play.Common.Repositories.Abstractions;
using Play.Inventory.Contracts;
using Play.Inventory.Service.Dtos;
using Play.Inventory.Service.Entities;

namespace Play.Inventory.Service.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private const string UnknownText = "unknown";
    
    private readonly IRepository<InventoryItem> _inventoryItemsRepository;
    private readonly IRepository<CatalogItem> _catalogItemsRepository;
    private readonly IPublishEndpoint _endpoint;

    public ItemsController(IRepository<InventoryItem> inventoryItemsRepository,
        IRepository<CatalogItem> catalogItemsRepository, IPublishEndpoint endpoint)
    {
        _inventoryItemsRepository = inventoryItemsRepository;
        _catalogItemsRepository = catalogItemsRepository;
        _endpoint = endpoint;
    }

    [HttpGet("{id}")]
    [Authorize]
    public async Task<ActionResult<IEnumerable<InventoryItemDto>>> GetAsync(Guid id)
    {
        var currentUserId = User.FindFirstValue(JwtRegisteredClaimNames.Sub);
        var parseSuccess = Guid.TryParse(currentUserId, out var guidCurrentUserId);
        if (id != guidCurrentUserId)
        {
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }
        }
        
        if (id == Guid.Empty)
        {
            return BadRequest();
        }

        var inventoryItems = await _inventoryItemsRepository
            .GetAllAsync(i => i.UserId == id);
        var catalogItemsIds = inventoryItems.Select(i => i.CatalogItemId);
        var catalogItems = await _catalogItemsRepository
            .GetAllAsync(c => catalogItemsIds.Contains(c.Id));
        

        var items = inventoryItems.Select(i =>
        {
            var catalogItem = catalogItems.SingleOrDefault(ci => ci.Id == i.CatalogItemId);
            
            return i.AsDto(catalogItem?.Name ?? UnknownText, catalogItem?.Description ?? UnknownText);
        });

        return Ok(items);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> PostAsync(GrantItemDto item)
    {
        var inventoryItem = await _inventoryItemsRepository
            .GetAsync(i => i.UserId == item.UserId && i.CatalogItemId == item.CatalogItemId);

        if (inventoryItem != null)
        {
            inventoryItem.Quantity += item.Quantity;

            await _inventoryItemsRepository.UpdateAsync(inventoryItem);
            
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

        await _inventoryItemsRepository.CreateAsync(newInventoryItem);
        
        await _endpoint.Publish(new InventoryItemUpdated(
            inventoryItem.UserId,
            inventoryItem.CatalogItemId,
            inventoryItem.Quantity));

        return Created(string.Empty, new { Id = newInventoryItem.Id });
    }
}