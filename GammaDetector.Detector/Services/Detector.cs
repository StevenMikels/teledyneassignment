using System.Net.Http.Json;
using GammaDetector.Dtos;

namespace GammaDetector.Detector.Services;

public sealed class Detector : IAsyncDisposable
{
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(1);

    private readonly Random _random = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<Detector> _logger;

    private const string DeviceUrl = "http://localhost:5123/api/device";

    private readonly object _gate = new();
    private CancellationTokenSource? _cts;
    private Task? _loop;

    public Detector(
        IHttpClientFactory httpClientFactory,
        ILogger<Detector> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public bool IsRunning
    {
        get
        {
            lock (_gate)
            {
                return _loop is { IsCompleted: false };
            }
        }
    }

    public bool Start()
    {
        lock (_gate)
        {
            if (_loop is { IsCompleted: false })
            {
                return false;
            }

            _cts = new CancellationTokenSource();
            _loop = Task.Run(() => RunAsync(_cts.Token));
            _logger.LogInformation("Detection request transmission started");
            return true;
        }
    }

    public async Task<bool> StopAsync()
    {
        CancellationTokenSource? cts;
        Task? loop;

        lock (_gate)
        {
            if (_loop is null or { IsCompleted: true })
            {
                return false;
            }

            cts = _cts;
            loop = _loop;
        }

        cts!.Cancel();

        try
        {
            await loop!.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            cts.Dispose();

            lock (_gate)
            {
                _cts = null;
                _loop = null;
            }

            _logger.LogInformation("Detection request transmission stopped");
        }

        return true;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(Interval);

        try
        {
            do
            {
                var reading = new GammaReadingDto(_random.Next(1, 51), DateTimeOffset.UtcNow);
                await SendAsync(reading, cancellationToken).ConfigureAwait(false);
            }
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false));
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task SendAsync(GammaReadingDto reading, CancellationToken cancellationToken)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("device");
            using var response = await client
                .PostAsJsonAsync(DeviceUrl, reading, cancellationToken)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "Sent {Detections} detections at {Timestamp:o}",
                    reading.Detections,
                    reading.Timestamp);
            }
            else
            {
                _logger.LogWarning(
                    "Device returned {StatusCode} for reading at {Timestamp:o}",
                    (int)response.StatusCode,
                    reading.Timestamp);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to send reading to device at {Url}", DeviceUrl);
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);
    }
}
