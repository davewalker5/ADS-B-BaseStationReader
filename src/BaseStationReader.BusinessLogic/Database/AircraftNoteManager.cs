using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database;

internal sealed class AircraftNoteManager : IAircraftNoteManager
{
    private readonly BaseStationReaderDbContext _context;

    /// <summary>Initialises an aircraft-note manager.</summary>
    /// <param name="context">The database context.</param>
    public AircraftNoteManager(BaseStationReaderDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<AircraftNote> AddAsync(
        string address,
        string noteText,
        CancellationToken cancellationToken = default)
    {
        var normalisedAddress = NormaliseAddress(address);
        var trimmedText = noteText?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(trimmedText))
        {
            throw new ArgumentException("Note text is required.", nameof(noteText));
        }

        var note = new AircraftNote
        {
            Address = normalisedAddress,
            NoteText = trimmedText,
            Date = DateTime.Now
        };
        await _context.AircraftNotes.AddAsync(note, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return note;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<AircraftNote>> ListAsync(
        string address,
        CancellationToken cancellationToken = default)
    {
        var normalisedAddress = NormaliseAddress(address);
        return await _context.AircraftNotes
            .AsNoTracking()
            .Where(note => note.Address == normalisedAddress)
            .OrderByDescending(note => note.Date)
            .ThenByDescending(note => note.Id)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        int id,
        string address,
        CancellationToken cancellationToken = default)
    {
        var normalisedAddress = NormaliseAddress(address);
        var note = await _context.AircraftNotes
            .FirstOrDefaultAsync(item => item.Id == id && item.Address == normalisedAddress, cancellationToken);
        if (note is null)
        {
            throw new InvalidOperationException($"Aircraft note {id} was not found for address {normalisedAddress}.");
        }

        _context.AircraftNotes.Remove(note);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>Validates and normalises an aircraft address.</summary>
    /// <param name="address">The supplied address.</param>
    /// <returns>The normalised address.</returns>
    private static string NormaliseAddress(string address)
    {
        var normalisedAddress = address?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalisedAddress.Length != 6 || !normalisedAddress.All(Uri.IsHexDigit))
        {
            throw new ArgumentException("Aircraft address must contain exactly six hexadecimal characters.", nameof(address));
        }

        return normalisedAddress;
    }
}
