using Microsoft.AspNetCore.Mvc;

namespace GammaDetector.Detector.Controllers;

/// <summary>
/// Controls the transmission of detected readings to the device.
/// </summary>
[ApiController]
[Route("api/detections")]
public class DetectionsController : ControllerBase
{
    private readonly Services.Detector _transmitter;

    public DetectionsController(Services.Detector transmitter)
    {
        _transmitter = transmitter;
    }

    /// <summary>
    /// Returns whether detection transmission is currently running.
    /// </summary>
    [HttpGet]
    public ActionResult<bool> GetStatus()
    {
        return Ok(_transmitter.IsRunning);
    }

    /// <summary>
    /// Starts sending detected readings to the device, reporting whether it started.
    /// </summary>
    [HttpPost("start")]
    public IActionResult Start()
    {
        var changed = _transmitter.Start();
        return Content(changed
            ? "Detection transmission started."
            : "Detection transmission is already running.");
    }

    /// <summary>
    /// Stops sending detected readings to the device, reporting whether it stopped.
    /// </summary>
    [HttpPost("stop")]
    public async Task<IActionResult> Stop()
    {
        var changed = await _transmitter.StopAsync();
        return Content(changed
            ? "Detection transmission stopped."
            : "Detection transmission is already stopped.");
    }
}
