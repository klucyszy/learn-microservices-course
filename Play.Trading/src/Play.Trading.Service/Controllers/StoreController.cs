using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Common.Repositories.Abstractions;
using Play.Trading.Service.Entities;
using Play.Trading.Service.Models;

namespace Play.Trading.Service.Controllers;

[ApiController]
[Route("store")]
[Authorize]
public class StoreController : ControllerBase
{
    private readonly IRepository<CatalogItem> _catalogItemRepository;
    private readonly IRepository<InventoryItem> _inventoryItemRepository;
    private readonly IRepository<ApplicationUser> _userRepository;

    public StoreController(IRepository<CatalogItem> catalogItemRepository,
        IRepository<InventoryItem> inventoryItemRepository, IRepository<ApplicationUser> userRepository)
    {
        _catalogItemRepository = catalogItemRepository;
        _inventoryItemRepository = inventoryItemRepository;
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<ActionResult<StoreDto>> GetAsync()
    {
        var userId = User.FindFirstValue("sub");

        var catalogItems =
            await _catalogItemRepository.GetAllAsync();
        var inventoryItems =
            await _inventoryItemRepository.GetAllAsync(item => item.UserId == Guid.Parse(userId));
        var user = await _userRepository.GetAsync(Guid.Parse(userId));

        var storeDto = new StoreDto(
            catalogItems.Select(x => new StoreItemDto(
                    x.Id,
                    x.Name,
                    x.Description,
                    x.Price,
                    inventoryItems.FirstOrDefault(i => i.CatalogItemId == x.Id)?.Quantity ?? 0)),
                user?.Gil ?? 0);

        return Ok(storeDto);
    }
}