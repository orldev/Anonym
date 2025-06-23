using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toolkit.SignalR.Reactive.Interfaces;

namespace Server.Controllers;

/// <summary>
/// API controller for handling auxiliary operations.
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuxiliaryController(ICacheService cache) : ControllerBase
{
    /// <summary>
    /// Searches for a specified value in the cache.
    /// </summary>
    /// <param name="value">The string value to search for in the cache.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing a boolean indicating whether the value was found in the cache.
    /// Returns HTTP 200 OK with the search result if successful.
    /// </returns>
    /// <remarks>
    /// This endpoint requires authorization and checks if the provided value exists in the application cache.
    /// The search is case-sensitive and looks for an exact match of the string value.
    /// </remarks>
    /// <response code="200">Returns whether the value was found in the cache</response>
    [HttpPost]
    [Authorize]
    [Route("search")]
    public async Task<IActionResult> Search([FromBody] string value)
    {
        var contains = cache.TryGetValue<string>(value, out _);
        await Task.CompletedTask;
        return Ok(contains);
    }
}