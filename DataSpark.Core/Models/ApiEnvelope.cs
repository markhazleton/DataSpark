namespace DataSpark.Core.Models;

/// <summary>
/// Standard API response envelope.
/// </summary>
public sealed record ApiEnvelope<T>
{
    public required string Status { get; init; }

    public T? Data { get; init; }

    public ApiError? Error { get; init; }

    public ApiMeta Meta { get; init; } = new();

    public static ApiEnvelope<T> Success(T data) => new()
    {
        Status = "success",
        Data = data,
        Meta = ApiMeta.Create()
    };

    public static ApiEnvelope<T> Failure(string code, string message) => new()
    {
        Status = "error",
        Error = new ApiError { Code = code, Message = message },
        Meta = ApiMeta.Create()
    };
}

/// <summary>
/// API error payload.
/// </summary>
public sealed record ApiError
{
    public required string Code { get; init; }

    public required string Message { get; init; }
}

/// <summary>
/// API metadata payload.
/// </summary>
public sealed record ApiMeta
{
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;

    public string RequestId { get; init; } = Guid.NewGuid().ToString();

    public static ApiMeta Create() => new();
}
