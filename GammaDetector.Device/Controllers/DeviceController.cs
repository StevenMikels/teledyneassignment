using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using GammaDetector.Device.Services;
using GammaDetector.Dtos;

namespace GammaDetector.Device.Controllers;

/// <summary>
/// Stores gamma readings in a CSV file and exposes them for querying.
/// </summary>
[ApiController]
[Route("api/device")]
public class DeviceController : ControllerBase
{
    private const string NoStoredReadings = "No stored readings.";
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    private readonly CsvReader _store;

    public DeviceController(CsvReader store)
    {
        _store = store;
    }

    /// <summary>
    /// Stores a newly received gamma reading in the CSV file.
    /// </summary>
    [HttpPost]
    public ActionResult Create([FromBody] GammaReadingDto reading)
    {
        _store.Append(reading);
        return Ok(ToResponse(reading));
    }

    /// <summary>
    /// Returns every stored gamma reading with its detection amount expressed in CPS, or a message when none are stored.
    /// </summary>
    [HttpGet]
    public ActionResult GetAll()
    {
        var readings = _store.ReadAll();
        if (readings.Count == 0)
        {
            return Ok(NoStoredReadings);
        }

        return Ok(readings.Select(ToResponse));
    }

    /// <summary>
    /// Deletes all stored gamma readings, leaving only the CSV header.
    /// </summary>
    [HttpDelete]
    public IActionResult Clear()
    {
        _store.Clear();
        return NoContent();
    }

    /// <summary>
    /// Returns the stored reading with the lowest detection amount, or a message when none are stored.
    /// </summary>
    [HttpGet("min")]
    public ActionResult Min()
    {
        var readings = _store.ReadAll();
        if (readings.Count == 0)
        {
            return Ok(NoStoredReadings);
        }

        return Ok($"{readings.Min(r => r.Detections)} CPS");
    }

    /// <summary>
    /// Returns the stored reading with the highest detection amount, or a message when none are stored.
    /// </summary>
    [HttpGet("max")]
    public ActionResult Max()
    {
        var readings = _store.ReadAll();
        if (readings.Count == 0)
        {
            return Ok(NoStoredReadings);
        }

        return Ok($"{readings.Max(r => r.Detections)} CPS");
    }

    /// <summary>
    /// Returns the summed detection amount across all stored readings, expressed in CPS.
    /// </summary>
    [HttpGet("total")]
    public ActionResult Total()
    {
        var total = _store.ReadAll().Sum(r => (long)r.Detections);
        return Ok($"{total} CPS");
    }

    /// <summary>
    /// Returns the number of readings currently stored in the CSV file.
    /// </summary>
    [HttpGet("count")]
    public ActionResult Count()
    {
        var count = _store.ReadAll().Count;
        return Ok($"{count} readings");
    }

    /// <summary>
    /// Returns the timestamps of the most recent and least recent readings, or a message when none are stored.
    /// </summary>
    [HttpGet("timestamps")]
    public ActionResult Timestamps()
    {
        var readings = _store.ReadAll();
        if (readings.Count == 0)
        {
            return Ok(NoStoredReadings);
        }

        var mostRecent = readings.Max(r => r.Timestamp);
        var leastRecent = readings.Min(r => r.Timestamp);
        return Ok(new ReadingTimestampsDto(
            mostRecent.ToString(TimestampFormat, CultureInfo.InvariantCulture),
            leastRecent.ToString(TimestampFormat, CultureInfo.InvariantCulture)));
    }

    private static object ToResponse(GammaReadingDto reading) =>
        new
        {
            detections = $"{reading.Detections} CPS",
            timestamp = reading.Timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture)
        };
}
