using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Defines the supported read-only tracking-session search criteria.
/// </summary>
public sealed class TrackingSessionFilter
{
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Callsign { get; set; } = string.Empty;
    public string Registration { get; set; } = string.Empty;
    public string Airline { get; set; } = string.Empty;
    public string FlightNumber { get; set; } = string.Empty;
    public decimal? MinimumAltitude { get; set; }
    public decimal? MaximumAltitude { get; set; }
    public double? MinimumDistance { get; set; }
    public double? MaximumDistance { get; set; }
    public TrackingStatus? Status { get; set; }
    public bool? HasPositions { get; set; }
    public bool CompletedOnly { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
