namespace Domain.Abstractions;

public interface IQueueItemContext
{
    
}

public static class IQueueContextExtensions
{
    public static string GetTypeAsString(this IQueueItemContext itemContext)
    {
        var type = itemContext.GetType();
        return string.Join(
            ".",
            type.Namespace,
            type.Name
        );
    }
}
