namespace Sql2Csv.Core.Configuration;

public class OpenAIOptions
{
    public string ApiKey { get; set; } = string.Empty;
    public string AssistantId { get; set; } = string.Empty;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromMinutes(5);
}
