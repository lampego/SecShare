namespace SecShare.Business.Dto;

public class JsonCommonResponse
{
    public string Status { get; init; } = "ok";
    public string Message { get; init; } = string.Empty;
    public object Data { get; init; } = new { };
}
