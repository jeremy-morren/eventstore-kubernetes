namespace EventStoreProxy.Authentication;

public class AllNodesUnreachableException : Exception
{
    public AllNodesUnreachableException()
        : base("No cluster nodes could be reached for authentication") {}
}