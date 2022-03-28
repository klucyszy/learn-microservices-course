using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Common.Repositories.Abstractions;

namespace Play.Catalog.Service.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private readonly IRepository<Item> _repository;

    public ItemsController(IRepository<Item> repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<IEnumerable<ItemDto>> GetAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(x => x.AsDto());
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ItemDto>> GetByIdAsync(Guid id)
    {
        var item = await _repository.GetAsync(id);

        if (item is null)
        {
            return NotFound();
        }

        return item.AsDto();
    }

    [HttpPost]
    public async Task<ActionResult<ItemDto>> PostAsync(CreateItemDto createItemDto)
    {
        var item = new Item
        {
            Id = Guid.NewGuid(),
            Name = createItemDto.Name,
            Description = createItemDto.Description,
            Price = createItemDto.Price,
            CreatedDate = DateTimeOffset.UtcNow
        };
        
        await _repository.CreateAsync(item);
        
        var nameofGet = nameof(GetByIdAsync);
        return CreatedAtAction(nameofGet, new {Id = item.Id}, item);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult> PutAsync(Guid id, UpdateItemDto updateItemDto)
    {
        var existingItem = await _repository.GetAsync(id);
        
        if (existingItem is null)
        {
            return NotFound();
        }

        existingItem.Name = updateItemDto.Name;
        existingItem.Description = updateItemDto.Description;
        existingItem.Price = updateItemDto.Price;

        await _repository.UpdateAsync(existingItem);

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        var item = await _repository.GetAsync(id);
        
        if (item == null)
        {
            return NotFound();
        }
        
        await _repository.DeleteAsync(id);

        return NoContent();
    }
}