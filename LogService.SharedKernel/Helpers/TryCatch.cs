namespace LogService.SharedKernel.Helpers;
using System;
using System.Threading.Tasks;

using Microsoft.Extensions.Logging;

public static class TryCatch
{
    public static async Task<TResult> ExecuteAsync<TResult>(
        Func<Task<TResult>> tryFunc,
        Func<Exception, Task<TResult>> catchFunc,
        ILogger logger,
        string context = null)
    {
        try
        {
            return await tryFunc();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in {Context}", context ?? typeof(TryCatch).Name);
            return await catchFunc(ex);
        }
    }

    public static async Task ExecuteAsync(
        Func<Task> tryFunc,
        ILogger logger,
        string context = null)
    {
        try
        {
            await tryFunc();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in {Context}", context ?? typeof(TryCatch).Name);
        }
    }
}
