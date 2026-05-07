using Asn.Diplomski.Application.UseCases.GetSoilMoistureReadings;
using Asn.Diplomski.Application.UseCases.GetTemperatureReadings;
using Asn.Diplomski.Server.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Asn.Diplomski.Server.Controllers
{
    /// <summary>
    /// Dohvat očitanja senzora
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/sensors")]
    [Produces("application/json")]
    public class SenzorsController : ControllerBase
    {
        private readonly GetTemperatureReadingsHandler _temperatureHandler;
        private readonly GetSoilMoistureReadingsHandler _soilMoistureHandler;

        public SenzorsController(
            GetTemperatureReadingsHandler temperatureHandler,
            GetSoilMoistureReadingsHandler soilMoistureHandler)
        {
            _temperatureHandler = temperatureHandler;
            _soilMoistureHandler = soilMoistureHandler;
        }

        /// <summary>
        /// Posljednje očitanje temperature za senzor
        /// </summary>
        /// <param name="sensorId">Sensor ID</param>
        /// <response code="200">Očitanje pronađeno</response>
        /// <response code="404">Nema dostupnih očitanja</response>
        [HttpGet("{sensorId:long}/temperature/latest")]
        [ProducesResponseType(typeof(ReadingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatestTemperature(long sensorId)
        {
            var reading = await _temperatureHandler.GetLatestAsync(sensorId);

            if (reading is null)
                return NotFound(new { message = $"Nema dostupnih očitanja za senzor {sensorId}." });

            return Ok(new ReadingResponse
            {
                Id = reading.Id,
                Value = reading.Value,
                RecordedAt = reading.RecordedAt
            });
        }

        /// <summary>
        /// Povijest temperature za senzor
        /// </summary>
        /// <param name="sensorId">Sensor ID</param>
        /// <param name="window">Vremenski prozor: 1h, 6h, 24h, 7d, 30d (zadano: 24h)</param>
        /// <response code="200">Lista očitanja (može biti prazna)</response>
        [HttpGet("{sensorId:long}/temperature")]
        [ProducesResponseType(typeof(ReadingHistoryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetTemperatureHistory(
            long sensorId,
            [FromQuery] string window = "24h")
        {
            var span = ParseWindow(window);
            var readings = await _temperatureHandler.GetHistoryAsync(sensorId, span);

            return Ok(new ReadingHistoryResponse
            {
                SensorId = sensorId,
                Unit = "°C",
                Readings = readings.Select(r => new ReadingResponse
                {
                    Id = r.Id,
                    Value = r.Value,
                    RecordedAt = r.RecordedAt
                }).ToList()
            });
        }

        /// <summary>
        /// Posljednje očitanje vlage tla za senzor
        /// </summary>
        /// <param name="sensorId">Sensor ID</param>
        /// <response code="200">Očitanje pronađeno</response>
        /// <response code="404">Nema dostupnih očitanja</response>
        [HttpGet("{sensorId:long}/soil-moisture/latest")]
        [ProducesResponseType(typeof(ReadingResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetLatestSoilMoisture(long sensorId)
        {
            var reading = await _soilMoistureHandler.GetLatestAsync(sensorId);

            if (reading is null)
                return NotFound(new { message = $"Nema dostupnih očitanja za senzor {sensorId}." });

            return Ok(new ReadingResponse
            {
                Id = reading.Id,
                Value = reading.Value,
                RecordedAt = reading.RecordedAt
            });
        }

        /// <summary>
        /// Povijest vlage tla za senzor
        /// </summary>
        /// <param name="sensorId">Sensor ID</param>
        /// <param name="window">Vremenski prozor: 1h, 6h, 24h, 7d, 30d (zadano: 24h)</param>
        /// <response code="200">Lista očitanja (može biti prazna)</response>
        [HttpGet("{sensorId:long}/soil-moisture")]
        [ProducesResponseType(typeof(ReadingHistoryResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetSoilMoistureHistory(
            long sensorId,
            [FromQuery] string window = "24h")
        {
            var span = ParseWindow(window);
            var readings = await _soilMoistureHandler.GetHistoryAsync(sensorId, span);

            return Ok(new ReadingHistoryResponse
            {
                SensorId = sensorId,
                Unit = "%",
                Readings = readings.Select(r => new ReadingResponse
                {
                    Id = r.Id,
                    Value = r.Value,
                    RecordedAt = r.RecordedAt
                }).ToList()
            });
        }

        // ── Helpers ──────────────────────────────────────────────

        private static TimeSpan ParseWindow(string window) => window.ToLowerInvariant() switch
        {
            "1h" => TimeSpan.FromHours(1),
            "6h" => TimeSpan.FromHours(6),
            "24h" => TimeSpan.FromHours(24),
            "7d" => TimeSpan.FromDays(7),
            "30d" => TimeSpan.FromDays(30),
            _ => TimeSpan.FromHours(24)
        };
    }
}
