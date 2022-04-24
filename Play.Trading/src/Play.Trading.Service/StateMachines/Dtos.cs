using System.ComponentModel.DataAnnotations;

namespace Play.Trading.Service.StateMachines;

public record SubmitPurchaseDto(
    [Required] Guid? ItemId,
    [Range(1, 100)] int Quantity,
    [Required] Guid? IdempotencyId);
    
public record PurchaseDto(
    Guid UserId,
    Guid ItemId,
    decimal? PurchaseTotal,
    int Quantity,
    string State,
    string Reason,
    DateTimeOffset Received,
    DateTimeOffset Updated);