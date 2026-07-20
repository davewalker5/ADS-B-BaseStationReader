namespace BaseStationReader.BusinessLogic.Weather
{
    /// <summary>
    /// Decodes an aviation weather report after determining whether it is a METAR or TAF.
    /// </summary>
    public static class WeatherDecoder
    {
        /// <summary>
        /// Decodes a METAR or TAF into one human-readable string per line.
        /// </summary>
        /// <param name="report">The complete METAR or TAF report.</param>
        /// <returns>The decoded report, with one item for each output line.</returns>
        /// <exception cref="ArgumentException">Thrown when the report is empty or its type cannot be determined.</exception>
        public static IReadOnlyList<string> Decode(string report)
        {
            // Normalise leading whitespace before inspecting the report designator.
            var normalised = report?.Trim() ?? string.Empty;
            if (normalised.StartsWith("METAR ", StringComparison.OrdinalIgnoreCase) ||
                normalised.StartsWith("SPECI ", StringComparison.OrdinalIgnoreCase))
            {
                return MetarDecoder.Decode(normalised);
            }

            if (normalised.StartsWith("TAF ", StringComparison.OrdinalIgnoreCase))
            {
                return TafDecoder.Decode(normalised);
            }

            throw new ArgumentException("The weather report must begin with METAR, SPECI or TAF.", nameof(report));
        }
    }
}
