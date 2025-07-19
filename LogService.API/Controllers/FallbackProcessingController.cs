namespace LogService.API.Controllers;

using LogService.Application.Abstractions.Fallback;
using LogService.Application.Options;

using Microsoft.AspNetCore.Mvc;

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
    [ProducesResponseType(typeof(FallbackProcessingRuntimeOptions), StatusCodes.Status200OK)]
    public ActionResult<FallbackProcessingRuntimeOptions> GetOptions()
    {
        return _stateService.Current;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult UpdateOptions([FromBody] FallbackProcessingRuntimeOptions options)
    {
        _stateService.UpdateOptions(options);
        return Ok();
    }
}
