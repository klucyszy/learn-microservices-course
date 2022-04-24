namespace Play.Trading.Service.Exceptions;

[Serializable]
public class UnknownItemException : Exception
{
    private Guid CatalogItemId { get; }
    public UnknownItemException(Guid catalogItemId)
    : base($"Unknown item with id {catalogItemId}")
    {
        CatalogItemId = catalogItemId;
    }
}