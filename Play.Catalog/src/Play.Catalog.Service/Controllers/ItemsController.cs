using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Catalog.Contracts;
using Play.Catalog.Service.Dtos;
using Play.Catalog.Service.Entities;
using Play.Common.Repositories.Abstractions;

namespace Play.Catalog.Service.Controllers;

[ApiController]
[Route("items")]
public class ItemsController : ControllerBase
{
    private readonly IRepository<Item> _repository;
    private readonly IPublishEndpoint _publishEndpoint;

    public ItemsController(IRepository<Item> repository, IPublishEndpoint publishEndpoint)
    {
        _repository = repository;
        _publishEndpoint = publishEndpoint;
    }

    [HttpGet]
    [Authorize(Policies.Read)]
    public async Task<IEnumerable<ItemDto>> GetAsync()
    {
        var items = await _repository.GetAllAsync();
        return items.Select(x => x.AsDto());
    }

    [HttpGet("{id}")]
    
    [Authorize(Policies.Read)]
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
    
    [Authorize(Policies.Write)]
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
        await _publishEndpoint.Publish(new CatalogItemCreated(item.Id, item.Name, item.Description, item.Price));
        
        var nameofGet = nameof(GetByIdAsync);
        return CreatedAtAction(nameofGet, new {Id = item.Id}, item);
    }

    [HttpPut("{id}")]
    
    [Authorize(Policies.Write)]
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
        await _publishEndpoint.Publish(new CatalogItemUpdated(existingItem.Id,
            existingItem.Name, existingItem.Description, existingItem.Price));
        
        return NoContent();
    }

    [HttpDelete("{id}")]
    
    [Authorize(Policies.Write)]
    public async Task<IActionResult> DeleteAsync(Guid id)
    {
        var item = await _repository.GetAsync(id);
        
        if (item == null)
        {
            return NotFound();
        }
        
        await _repository.DeleteAsync(id);
        await _publishEndpoint.Publish(new CatalogItemDeleted(id));

        return NoContent();
    }
}