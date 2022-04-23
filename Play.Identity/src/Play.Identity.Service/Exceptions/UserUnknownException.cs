namespace Play.Identity.Service.Exceptions;

[Serializable]
public class UserUnknownException : Exception
{
    public Guid UserId { get; }
    
    public UserUnknownException(Guid userId)
        : base($"Unknown user with id {userId}")
    {
        UserId = userId;
    }
}