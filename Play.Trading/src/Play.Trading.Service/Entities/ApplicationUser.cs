using Play.Common.Entities.Abstractions;

namespace Play.Trading.Service.Entities;

public class ApplicationUser : IEntity
{
    public Guid Id { get; set; }
    public decimal Gil { get; set; }
}