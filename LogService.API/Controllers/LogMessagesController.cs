namespace LogService.API.Controllers;

using LogService.SharedKernel.Requests;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

using StackExchange.Redis;

[ApiController]
[Route("log-messages")]
public class LogMessagesController : ControllerBase
{
    private readonly IDistributedCache _redis;
    private readonly IMemoryCache _memory;

    public LogMessagesController(IDistributedCache redis, IMemoryCache memory)
    {
        _redis = redis;
        _memory = memory;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List()
    {
        var redisConn = HttpContext.RequestServices.GetRequiredService<IConnectionMultiplexer>();
        var server = redisConn.GetServer(redisConn.GetEndPoints().First());

        var result = new List<object>();

        await foreach (var key in server.KeysAsync(pattern: "log:msg:*"))
        {
            var value = await _redis.GetStringAsync(key);
            result.Add(new
            {
                Key = key.ToString(),
                Value = value ?? "<null>"
            });
        }

        return Ok(result);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] UpdateLogMessageRequest request)
    {
        if (!request.Key.StartsWith("log:msg:"))
            return BadRequest("Invalid key prefix.");

        await _redis.SetStringAsync(request.Key, request.Message);
        _memory.Remove(request.Key);

        return Ok(new { message = "Updated successfully" });
    }
}
