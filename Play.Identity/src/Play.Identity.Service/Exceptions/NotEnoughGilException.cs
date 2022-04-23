namespace Play.Identity.Service.Exceptions;

[Serializable]
public class NotEnoughGilException : Exception
{
    public Guid UserId { get; }
    public decimal GilToDebitToDebit { get; }
    
    public NotEnoughGilException(Guid userId, decimal gilToDebit)
        : base($"Not enough gil to debit {gilToDebit} for user {userId}")
    {
        UserId = UserId;
        GilToDebitToDebit = gilToDebit;
    }
}