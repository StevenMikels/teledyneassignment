using System.Globalization;
using CsvHelper;
using GammaDetector.Dtos;

namespace GammaDetector.Device.Services;

public sealed class CsvReader
{
    private const string TimestampColumn = "Timestamp";
    private const string DetectionsColumn = "Detections";
    private const string CsvFileName = "readings.csv";
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss.fff";

    private readonly object _gate = new();
    private readonly string _path;

    public CsvReader(IWebHostEnvironment environment)
    {
        _path = System.IO.Path.Combine(environment.ContentRootPath, CsvFileName);

        lock (_gate)
        {
            var directory = System.IO.Path.GetDirectoryName(_path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(_path))
            {
                WriteHeader();
            }
        }
    }

    public string FilePath => _path;

    public void Append(GammaReadingDto reading)
    {
        lock (_gate)
        {
            using var writer = new StreamWriter(_path, append: true);
            using var csv = CreateWriter(writer);
            csv.WriteField(reading.Timestamp.ToString(TimestampFormat, CultureInfo.InvariantCulture));
            csv.WriteField(reading.Detections);
            csv.NextRecord();
        }
    }

    public IReadOnlyList<GammaReadingDto> ReadAll()
    {
        lock (_gate)
        {
            if (!File.Exists(_path))
            {
                return Array.Empty<GammaReadingDto>();
            }

            using var reader = new StreamReader(_path);
            using var csv = CreateReader(reader);

            csv.Read();
            csv.ReadHeader();

            var readings = new List<GammaReadingDto>();
            while (csv.Read())
            {
                var timestamp = DateTimeOffset.ParseExact(
                    csv.GetField(TimestampColumn)!,
                    TimestampFormat,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal);
                var detections = csv.GetField<int>(DetectionsColumn);
                readings.Add(new GammaReadingDto(detections, timestamp));
            }

            return readings;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            WriteHeader();
        }
    }

    private void WriteHeader()
    {
        using var writer = new StreamWriter(_path, append: false);
        using var csv = CreateWriter(writer);
        csv.WriteField(TimestampColumn);
        csv.WriteField(DetectionsColumn);
        csv.NextRecord();
    }

    private static CsvWriter CreateWriter(TextWriter writer) =>
        new(writer, CultureInfo.InvariantCulture);

    private static CsvHelper.CsvReader CreateReader(TextReader reader) =>
        new(reader, CultureInfo.InvariantCulture);
}
