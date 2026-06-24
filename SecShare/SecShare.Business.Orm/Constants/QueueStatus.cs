namespace SecShare.Business.Orm.Constants;

public enum QueueStatus : short
{
    Pending = 1,
    InProcess,
    Success,
    Fail
}
