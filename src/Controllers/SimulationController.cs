using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DroneKurye.Middleware;
using DroneKurye.Services;

namespace DroneKurye.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = Policies.AdminOnly)]
public class SimulationController : ControllerBase
{
    private readonly IDroneSimulationService _sim;
    public SimulationController(IDroneSimulationService sim) => _sim = sim;

    [HttpGet("status")]
    public IActionResult Status() => Ok(new { isRunning = _sim.IsRunning });

    [HttpPost("start")]
    public IActionResult Start() { _sim.Start(); return Ok(new { message = "Simülasyon başlatıldı.", isRunning = true }); }

    [HttpPost("stop")]
    public IActionResult Stop()  { _sim.Stop();  return Ok(new { message = "Simülasyon durduruldu.", isRunning = false }); }
}
