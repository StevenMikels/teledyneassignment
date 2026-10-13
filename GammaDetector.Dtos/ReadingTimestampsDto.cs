namespace GammaDetector.Dtos;

public record ReadingTimestampsDto(DateTimeOffset MostRecent, DateTimeOffset LeastRecent);
