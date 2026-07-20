using System.Text.RegularExpressions;

namespace BaseStationReader.BusinessLogic.Weather
{
    /// <summary>
    /// Decodes terminal aerodrome forecasts (TAFs).
    /// </summary>
    public static partial class TafDecoder
    {
        /// <summary>
        /// Decodes a TAF into one human-readable string per line.
        /// </summary>
        /// <param name="report">The complete encoded forecast.</param>
        /// <returns>The decoded forecast lines.</returns>
        /// <exception cref="ArgumentException">Thrown when the forecast is empty or malformed.</exception>
        public static IReadOnlyList<string> Decode(string report)
        {
            // Validate the fixed TAF header before processing forecast groups.
            var tokens = AviationWeatherDecoder.Tokenise(report ?? string.Empty);
            if (tokens.Length < 4 || tokens[0] != "TAF")
            {
                throw new ArgumentException("The report must be a TAF containing an airport, issue time and validity period.", nameof(report));
            }

            var lines = new List<string> { "Report: terminal aerodrome forecast (TAF)" };
            var index = 1;
            if (tokens[index] is "AMD" or "COR")
            {
                lines.Add(tokens[index++] == "AMD" ? "Status: amended forecast" : "Status: corrected forecast");
            }

            lines.Add($"Airport: {tokens[index++]}");
            if (index >= tokens.Length || !AviationWeatherDecoder.TryDecodeTime(tokens[index++], out var issueTime))
            {
                throw new ArgumentException("The TAF issue time is missing or invalid.", nameof(report));
            }
            lines.Add($"Issued: {issueTime}");

            if (index >= tokens.Length || !AviationWeatherDecoder.TryDecodePeriod(tokens[index++], out var validity))
            {
                throw new ArgumentException("The TAF validity period is missing or invalid.", nameof(report));
            }
            lines.Add($"Valid: {validity}");

            for (; index < tokens.Length; index++)
            {
                var token = tokens[index];
                if (token is "BECMG" or "TEMPO")
                {
                    // Change groups are followed by the period in which the conditions apply.
                    var label = token == "BECMG" ? "Becoming" : "Temporary conditions";
                    if (index + 1 < tokens.Length && AviationWeatherDecoder.TryDecodePeriod(tokens[index + 1], out var period))
                    {
                        lines.Add($"{label}: {period}");
                        index++;
                    }
                    else
                    {
                        lines.Add($"{label}:");
                    }
                }
                else if (FromTimeRegex().Match(token) is { Success: true } from)
                {
                    lines.Add($"From: day {from.Groups[1].Value} at {from.Groups[2].Value}:{from.Groups[3].Value} UTC");
                }
                else if (ProbabilityRegex().Match(token) is { Success: true } probability)
                {
                    lines.Add($"Probability: {probability.Groups[1].Value}%");
                }
                else if (AviationWeatherDecoder.TryDecodeWind(token, out var wind))
                {
                    lines.Add($"Wind: {wind}");
                }
                else if (AviationWeatherDecoder.TryDecodeVisibility(token, out var visibility))
                {
                    lines.Add($"Visibility: {visibility}");
                }
                else if (AviationWeatherDecoder.TryDecodeCloud(token, out var cloud))
                {
                    lines.Add($"Cloud: {cloud}");
                }
                else if (token == "CAVOK")
                {
                    lines.Add("Conditions: visibility 10 km or more, no significant cloud and no significant weather");
                }
                else if (token == "CNL")
                {
                    lines.Add("Status: forecast cancelled");
                }
                else if (AviationWeatherDecoder.TryDecodeWeather(token, out var weather))
                {
                    lines.Add($"Weather: {weather}");
                }
                else
                {
                    // Preserve uncommon national and runway groups so no source information disappears.
                    lines.Add($"Additional group: {token}");
                }
            }

            return lines;
        }

        /// <summary>
        /// Creates a generated regular expression for TAF FM change groups.
        /// </summary>
        /// <returns>The from-time regular expression.</returns>
        [GeneratedRegex(@"^FM(\d{2})(\d{2})(\d{2})$")]
        private static partial Regex FromTimeRegex();

        /// <summary>
        /// Creates a generated regular expression for forecast probabilities.
        /// </summary>
        /// <returns>The probability regular expression.</returns>
        [GeneratedRegex(@"^PROB(30|40)$")]
        private static partial Regex ProbabilityRegex();
    }
}
