using BaseStationReader.Entities.History;

namespace BaseStationReader.BusinessLogic.Tracking;

/// <summary>
/// Creates stable geographic bounds for position-density aggregation.
/// </summary>
public static class PositionDensityBoundsFactory
{
    /// <summary>
    /// Creates session-centred bounds from receiver settings.
    /// </summary>
    /// <param name="receiverLatitude"></param>
    /// <param name="receiverLongitude"></param>
    /// <param name="maximumDistance"></param>
    /// <returns></returns>
    public static PositionDensityBounds Create(
        double? receiverLatitude,
        double? receiverLongitude,
        int? maximumDistance)
    {
        if (!receiverLatitude.HasValue || !receiverLongitude.HasValue ||
            !double.IsFinite(receiverLatitude.Value) || !double.IsFinite(receiverLongitude.Value) ||
            receiverLatitude is < -90 or > 90 || receiverLongitude is < -180 or > 180)
        {
            // A fixed world viewport remains stable when receiver configuration is unavailable.
            return new PositionDensityBounds(-90d, 90d, -180d, 180d);
        }

        var range = maximumDistance is > 0 ? maximumDistance.Value : 250d;
        var latitudeRadius = range / 60d;
        var longitudeScale = Math.Max(Math.Cos(receiverLatitude.Value * Math.PI / 180d), 0.01d);
        var longitudeRadius = Math.Min(range / (60d * longitudeScale), 180d);
        return new PositionDensityBounds(
            Math.Max(-90d, receiverLatitude.Value - latitudeRadius),
            Math.Min(90d, receiverLatitude.Value + latitudeRadius),
            Math.Max(-180d, receiverLongitude.Value - longitudeRadius),
            Math.Min(180d, receiverLongitude.Value + longitudeRadius));
    }
}
