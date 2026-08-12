using BaseStationReader.Entities.History;

namespace BaseStationReader.BusinessLogic.Tracking;

/// <summary>Applies shared physical and rate-based altitude validation to flight charts and paths.</summary>
internal static class FlightAltitudeFilter
{
    private const double FeetToMetres = 0.3048;
    private const double MinimumPlausibleAltitudeMetres = -500;
    private const double MaximumPlausibleAltitudeMetres = 20_000;
    private const double MaximumPlausibleVerticalSpeedMetresPerSecond = 50;
    private static readonly TimeSpan MaximumSpikeDuration = TimeSpan.FromSeconds(90);

    /// <summary>Removes physically impossible altitudes, short excursions, and implausible terminal readings.</summary>
    public static FlightProfilePoint[] DiscardImplausibleAltitudes(IReadOnlyList<FlightProfilePoint> points)
    {
        // Broad bounds also protect endpoints, where no recovery observation can exist.
        var filtered = points
            .Where(point => point.Altitude.HasValue && IsWithinPlausibleBounds(point.Altitude.Value))
            .ToArray();
        if (filtered.Length < 3)
        {
            return filtered;
        }

        var rejected = new HashSet<int>();
        var position = 1;
        while (position < filtered.Length - 1)
        {
            var previous = filtered[position - 1];
            var current = filtered[position];
            var secondsBefore = (current.Timestamp - previous.Timestamp).TotalSeconds;
            if (secondsBefore <= 0 || VerticalSpeed(previous, current, secondsBefore) <= MaximumPlausibleVerticalSpeedMetresPerSecond)
            {
                position++;
                continue;
            }

            // Repeated corrupt values form a short plateau. Search for the first observation reachable from the
            // last good point within the same 90-second chart segment.
            var recovered = false;
            for (var recoveryPosition = position + 1; recoveryPosition < filtered.Length; recoveryPosition++)
            {
                var secondsAcross = (filtered[recoveryPosition].Timestamp - previous.Timestamp).TotalSeconds;
                if (secondsAcross <= 0)
                {
                    continue;
                }

                if (secondsAcross > MaximumSpikeDuration.TotalSeconds)
                {
                    break;
                }

                if (VerticalSpeed(previous, filtered[recoveryPosition], secondsAcross) <= MaximumPlausibleVerticalSpeedMetresPerSecond)
                {
                    for (var rejectedPosition = position; rejectedPosition < recoveryPosition; rejectedPosition++)
                    {
                        rejected.Add(rejectedPosition);
                    }

                    position = recoveryPosition + 1;
                    recovered = true;
                    break;
                }
            }

            if (!recovered)
            {
                position++;
            }
        }

        // A final bad value has no later observation to prove a return. Elapsed time still permits a genuine
        // change following a sufficiently long reception gap.
        var finalPosition = filtered.Length - 1;
        var previousPosition = finalPosition - 1;
        while (previousPosition > 0 && rejected.Contains(previousPosition))
        {
            previousPosition--;
        }

        var secondsToFinal = (filtered[finalPosition].Timestamp - filtered[previousPosition].Timestamp).TotalSeconds;
        if (secondsToFinal > 0 &&
            VerticalSpeed(filtered[previousPosition], filtered[finalPosition], secondsToFinal) > MaximumPlausibleVerticalSpeedMetresPerSecond)
        {
            rejected.Add(finalPosition);
        }

        return filtered.Where((_, index) => !rejected.Contains(index)).ToArray();
    }

    private static bool IsWithinPlausibleBounds(decimal altitudeFeet)
    {
        var altitudeMetres = (double)altitudeFeet * FeetToMetres;
        return altitudeMetres >= MinimumPlausibleAltitudeMetres && altitudeMetres <= MaximumPlausibleAltitudeMetres;
    }

    private static double VerticalSpeed(FlightProfilePoint first, FlightProfilePoint second, double elapsedSeconds) =>
        Math.Abs((double)(second.Altitude!.Value - first.Altitude!.Value)) * FeetToMetres / elapsedSeconds;
}
