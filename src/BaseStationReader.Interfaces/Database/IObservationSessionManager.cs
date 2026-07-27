namespace BaseStationReader.Interfaces.Database
{
    public interface IObservationSessionManager
    {
        Task DeleteAsync(int sessionId, CancellationToken cancellationToken = default);
    }
}
