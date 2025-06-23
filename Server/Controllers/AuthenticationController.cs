using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Toolkit.Authentication.JwtBearer;

namespace Server.Controllers;

/// <summary>
/// Controller for handling authentication operations including user sign-up and token validation.
/// </summary>
[ApiController]
[Route("[controller]")]
public class AuthenticationController(ITokenProvider tokenProvider) : ControllerBase
{
    /// <summary>
    /// Registers a new user and generates an access token.
    /// </summary>
    /// <param name="identifier">The unique identifier for the user to register.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing the generated access token.
    /// Returns HTTP 200 OK with the token if successful.
    /// </returns>
    /// <remarks>
    /// This endpoint allows anonymous access and creates a new user identity with the provided identifier.
    /// The generated token contains a NameIdentifier claim with the provided value.
    /// </remarks>
    /// <response code="200">Returns the newly generated access token</response>
    [HttpPost]
    [AllowAnonymous]
    [Route("signup")]
    public async Task<IActionResult> SignUp([FromBody] string identifier)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, identifier)
        };
        var accessToken = tokenProvider.Create(claims);
        
        await Task.CompletedTask;
        
        return Ok(accessToken);
    }

    /// <summary>
    /// Validates an existing authentication token.
    /// </summary>
    /// <param name="token">The token to validate.</param>
    /// <returns>
    /// An <see cref="IActionResult"/> containing the validation result.
    /// Returns HTTP 200 OK with the validation outcome if successful.
    /// </returns>
    /// <remarks>
    /// This endpoint allows anonymous access and verifies the validity of the provided token.
    /// The validation result typically indicates whether the token is valid and may include decoded claims.
    /// </remarks>
    /// <response code="200">Returns the token validation result</response>
    [HttpPost]
    [AllowAnonymous]
    [Route("validate")]
    public async Task<IActionResult> Validate([FromBody] string token)
    {
        var value = await tokenProvider.Validate(token);
        return Ok(value);
    }
}