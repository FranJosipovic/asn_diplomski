using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Asn.Diplomski.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SenzorsController : ControllerBase
    {
        [HttpPost("temperature")]
        public async Task<IActionResult> ReadTemperatureAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            Console.WriteLine($"Primljena temperatura: {body}");

            return Ok(new { message = "Primljeno", value = body });
        }
    }
}
