namespace BaseStationReader.Entities.History;

/// <summary>
/// Contains a complete renderer-neutral two- and three-dimensional flight path.
/// </summary>
public sealed class FlightPathDto
{
    public int TrackingRecordId { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Callsign { get; init; } = string.Empty;
    public string Registration { get; init; } = string.Empty;
    public string Model { get; init; } = string.Empty;
    public string FlightNumber { get; init; } = string.Empty;
    public string Airline { get; init; } = string.Empty;
    public string Route { get; init; } = string.Empty;
    public IReadOnlyList<FlightPathPointDto> Points { get; init; } = [];
    public double? North { get; init; }
    public double? South { get; init; }
    public double? East { get; init; }
    public double? West { get; init; }
    public double? ReceiverLatitude { get; init; }
    public double? ReceiverLongitude { get; init; }
    public double MinimumAltitudeMetres { get; init; }
    public double MaximumAltitudeMetres { get; init; }
    public int SegmentCount { get; init; }
    public DateTime? FirstTimestamp => Points.FirstOrDefault()?.Timestamp;
    public DateTime? FinalTimestamp => Points.LastOrDefault()?.Timestamp;
}
