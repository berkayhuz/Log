namespace LogService.Infrastructure.Services.Messages;

using System;
using System.Text;
using System.Text.Json;

using LogService.Application.Abstractions.Logging;
using LogService.Application.Abstractions.Messages;
using LogService.SharedKernel.Enums;
using LogService.SharedKernel.Keys;

using Microsoft.Extensions.Caching.Distributed;

public class RedisLogMessageProvider : ILogMessageProvider
{
    private readonly IDistributedCache _redis;
    private readonly ILogServiceLogger _logLogger;

    public RedisLogMessageProvider(IDistributedCache redis, ILogServiceLogger logLogger)
    {
        _redis = redis;
        _logLogger = logLogger;
    }

    public string Get(string key, string? defaultMessage = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            var msg = LogMessageDefaults.Messages[LogMessageKeys.Log_InvalidMessageKey];

            _ = _logLogger.LogAsync(LogStage.Error, msg);
            return LogMessageDefaults.Messages[LogMessageKeys.Log_InvalidMessageKeyBracket];
        }

        try
        {
            var redisValue = _redis.GetString(key);
            if (!string.IsNullOrEmpty(redisValue))
            {
                return redisValue;
            }

            var warning = LogMessageDefaults.Messages[LogMessageKeys.Redis_MessageNotFound].Replace("{Key}", key);
            _ = _logLogger.LogAsync(LogStage.Warning, warning);
            return defaultMessage ?? LogMessageDefaults.Messages[LogMessageKeys.Redis_MissingLogMessage].Replace("{Key}", key);
        }
        catch (Exception ex)
        {
            _ = _logLogger.LogAsync(LogStage.Error, LogMessageDefaults.Messages[LogMessageKeys.Redis_AccessError].Replace("{Key}", key), ex);
            return defaultMessage ?? LogMessageDefaults.Messages[LogMessageKeys.Redis_ErrorReadingMessage].Replace("{Key}", key);
        }
    }

    public IEnumerable<string> GetKeys()
    {
        try
        {
            var serializedKeys = _redis.GetString("LogMessageKeys");
            if (string.IsNullOrEmpty(serializedKeys))
            {
                _ = _logLogger.LogAsync(LogStage.Warning, LogMessageDefaults.Messages[LogMessageKeys.Redis_NoKeysFound]);
                return Enumerable.Empty<string>();
            }

            var keys = JsonSerializer.Deserialize<List<string>>(serializedKeys);
            return keys ?? Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
            _ = _logLogger.LogAsync(LogStage.Error, LogMessageDefaults.Messages[LogMessageKeys.Redis_ErrorRetrievingKeys], ex);
            return Enumerable.Empty<string>();
        }
    }

    public async Task AddKeyAsync(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            await _logLogger.LogAsync(LogStage.Warning, "Attempted to add null or empty key.");
            return;
        }

        var serializedKeys = await _redis.GetStringAsync("LogMessageKeys");
        List<string> keys = string.IsNullOrEmpty(serializedKeys)
            ? new List<string>()
            : JsonSerializer.Deserialize<List<string>>(serializedKeys) ?? new List<string>();

        if (!keys.Contains(key))
        {
            keys.Add(key);
            await _redis.SetStringAsync("LogMessageKeys", JsonSerializer.Serialize(keys));
        }
    }
}
