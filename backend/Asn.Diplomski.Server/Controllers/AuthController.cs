using Asn.Diplomski.Application.UseCases.SignIn;
using Asn.Diplomski.Application.UseCases.RefreshToken;
using Asn.Diplomski.Server.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Asn.Diplomski.Server.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AuthController : ControllerBase
    {
        private readonly SignInHandler _signInHandler;
        private readonly RefreshTokenHandler _refreshTokenHandler;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            SignInHandler signInHandler,
            RefreshTokenHandler refreshTokenHandler,
            ILogger<AuthController> logger)
        {
            _signInHandler = signInHandler;
            _refreshTokenHandler = refreshTokenHandler;
            _logger = logger;
        }

        /// <summary>
        /// Prijava s e-mailom i lozinkom — vraća JWT access token i refresh token
        /// </summary>
        /// <response code="200">Uspješna prijava, vraća access i refresh token</response>
        /// <response code="401">Neispravni e-mail ili lozinka</response>
        [HttpPost("signin")]
        [ProducesResponseType(typeof(SignInResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> SignIn([FromBody] SignInRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var command = new SignInCommand(request.Email, request.Password);
            var result = await _signInHandler.HandleAsync(command);

            if (result.IsUnauthorized)
            {
                _logger.LogWarning("Neuspješna prijava — neispravni kredencijali za {Email}", request.Email);
                return Unauthorized(new { message = "Neispravni e-mail ili lozinka." });
            }

            return Ok(new SignInResponseDto
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                RefreshTokenExpiresAt = result.RefreshTokenExpiresAt,
                TenantId = result.TenantId,
                Email = result.Email
            });
        }

        /// <summary>
        /// Osvježavanje access tokena s refresh tokenom
        /// </summary>
        /// <response code="200">Novi access token generiran</response>
        /// <response code="401">Neispravni refresh token</response>
        /// <response code="403">Refresh token istekao</response>
        [AllowAnonymous]
        [HttpPost("refresh")]
        [ProducesResponseType(typeof(RefreshTokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequestDto request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var command = new RefreshTokenCommand(request.RefreshToken);
            var result = await _refreshTokenHandler.HandleAsync(command);

            if (result.IsUnauthorized)
            {
                _logger.LogWarning("Osvježavanje tokena odbijeno — neispravni refresh token");
                return Unauthorized(new { message = "Neispravni refresh token." });
            }

            if (result.IsExpired)
            {
                _logger.LogWarning("Osvježavanje tokena odbijeno — refresh token istekao");
                return StatusCode(StatusCodes.Status403Forbidden, new { message = "Refresh token je istekao. Prijavite se ponovno." });
            }

            return Ok(new RefreshTokenResponseDto
            {
                AccessToken = result.AccessToken,
                RefreshToken = result.RefreshToken,
                RefreshTokenExpiresAt = result.RefreshTokenExpiresAt
            });
        }
    }
}
