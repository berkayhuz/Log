namespace LogService.API.Controllers;

using LogService.Application.Options;
using LogService.Infrastructure.Services.Fallback.Abstractions;

using Microsoft.AspNetCore.Mvc;

using SharedKernel.Common.Results;
using SharedKernel.Common.Results.Objects;

[ApiController]
[Route("api/fallback-processing")]
public class FallbackProcessingController : ControllerBase
{
    private readonly IFallbackProcessingStateService _stateService;

    public FallbackProcessingController(IFallbackProcessingStateService stateService)
    {
        _stateService = stateService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(Result<FallbackProcessingRuntimeOptions>), StatusCodes.Ok)]
    public IActionResult GetOptions()
    {
        var result = Result.Success(_stateService.Current);
        return result.ToActionResult();
    }

    [HttpPost]
    [ProducesResponseType(typeof(Result), StatusCodes.Ok)]
    public IActionResult UpdateOptions([FromBody] FallbackProcessingRuntimeOptions options)
    {
        if (options is null)
        {
            return Result.Failure("Options payload null olamaz.")
                .WithStatusCode(StatusCodes.BadRequest)
                .WithErrorType(ErrorType.Validation)
                .ToActionResult();
        }

        _stateService.UpdateOptions(options);
        return Result.Success().ToActionResult();
    }
}
