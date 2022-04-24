using Automatonymous;
using GreenPipes;
using Play.Common.Repositories.Abstractions;
using Play.Trading.Service.Entities;
using Play.Trading.Service.Exceptions;
using Play.Trading.Service.StateMachines;

namespace Play.Trading.Service.Activities;

public class CalculatePurchaseTotalPriceActivity : Activity<PurchaseState, PurchaseRequested>
{
    private readonly IRepository<CatalogItem> _catalogItemRepository;

    public CalculatePurchaseTotalPriceActivity(IRepository<CatalogItem> catalogItemRepository)
    {
        _catalogItemRepository = catalogItemRepository;
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("calculate-purchase-total-price");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }

    public async Task Execute(BehaviorContext<PurchaseState, PurchaseRequested> context,
        Behavior<PurchaseState, PurchaseRequested> next)
    {
        var message = context.Data;

        var catalogItem = await _catalogItemRepository.GetAsync(message.ItemId);
        if (catalogItem is null)
        {
            throw new UnknownItemException(message.ItemId);
        }

        var totalPrice = catalogItem.Price * message.Quantity;
        context.Instance.PurchaseTotal = totalPrice;
        context.Instance.LastUpdated = DateTimeOffset.UtcNow;

        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<PurchaseState, PurchaseRequested, TException> context,
        Behavior<PurchaseState, PurchaseRequested> next) where TException : Exception
    {
        return next.Faulted(context);
    }
}