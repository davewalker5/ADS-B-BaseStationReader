using System.ComponentModel.DataAnnotations;

namespace BaseStationReader.Entities.Tracking;

/// <summary>
/// A dated note associated with an aircraft ICAO address.
/// </summary>
public sealed class AircraftNote
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(6)]
    public string Address { get; set; } = string.Empty;

    [Required]
    public string NoteText { get; set; } = string.Empty;

    public DateTime Date { get; set; }
}
