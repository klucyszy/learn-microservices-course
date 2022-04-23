using System.Security.Claims;
using MassTransit;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Play.Trading.Service.StateMachines;

namespace Play.Trading.Service.Controllers;

[ApiController]
[Route("purchase")]
[Authorize]
public class PurchaseController : ControllerBase
{
    private readonly IPublishEndpoint _endpoint;
    private readonly IRequestClient<GetPurchaseState> _requestClient;

    public PurchaseController(IPublishEndpoint endpoint, IRequestClient<GetPurchaseState> requestClient)
    {
        _endpoint = endpoint;
        _requestClient = requestClient;
    }
    
    [HttpPost]
    public async Task<IActionResult> PostAsync([FromBody] SubmitPurchaseDto request)
    {
        var userId = User.FindFirstValue("sub");
        var correlationId = Guid.NewGuid();
        
        var message = new PurchaseRequested(Guid.Parse(userId), request.ItemId.Value, request.Quantity, correlationId);
        
        await _endpoint.Publish(message);
        return AcceptedAtAction(nameof(GetStatusAsync), new { correlationId }, new { correlationId });
    }
    
    [HttpGet("status/{correlationId}")]
    public async Task<ActionResult<PurchaseDto>> GetStatusAsync(Guid correlationId)
    {
        var userId = User.FindFirstValue("sub");
        
        var response = await _requestClient.GetResponse<PurchaseState>(
            new GetPurchaseState(correlationId));

        var purchaseState = response.Message;
        return Ok(new PurchaseDto(
            purchaseState.UserId,
            purchaseState.ItemId,
            purchaseState.PurchaseTotal,
            purchaseState.Quantity,
            purchaseState.CurrentState,
            purchaseState.ErrorMessage,
            purchaseState.Received,
            purchaseState.Updated));
    }
}