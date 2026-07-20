using System.Text.RegularExpressions;

namespace BaseStationReader.BusinessLogic.Weather
{
    /// <summary>
    /// Decodes METAR and SPECI airport weather observations.
    /// </summary>
    public static partial class MetarDecoder
    {
        /// <summary>
        /// Decodes a METAR or SPECI into one human-readable string per line.
        /// </summary>
        /// <param name="report">The complete encoded observation.</param>
        /// <returns>The decoded observation lines.</returns>
        /// <exception cref="ArgumentException">Thrown when the observation is empty or malformed.</exception>
        public static IReadOnlyList<string> Decode(string report)
        {
            // Validate the fixed report header before interpreting its remaining groups.
            var tokens = AviationWeatherDecoder.Tokenise(report ?? string.Empty);
            if (tokens.Length < 3 || (tokens[0] != "METAR" && tokens[0] != "SPECI"))
            {
                throw new ArgumentException("The report must be a METAR or SPECI containing an airport and observation time.", nameof(report));
            }

            var lines = new List<string>
            {
                $"Report: {(tokens[0] == "SPECI" ? "special weather observation (SPECI)" : "routine weather observation (METAR)")}",
                $"Airport: {tokens[1]}"
            };
            var index = 2;
            if (!AviationWeatherDecoder.TryDecodeTime(tokens[index++], out var time))
            {
                throw new ArgumentException("The METAR observation time is missing or invalid.", nameof(report));
            }

            lines.Add($"Observed: {time}");

            // Decode modifiers that may appear immediately after the observation time.
            while (index < tokens.Length && tokens[index] is "AUTO" or "COR")
            {
                lines.Add(tokens[index++] == "AUTO" ? "Observation: automated report" : "Observation: corrected report");
            }

            for (; index < tokens.Length; index++)
            {
                var token = tokens[index];
                if (AviationWeatherDecoder.TryDecodeWind(token, out var wind))
                {
                    // Fold a following variable-direction group into the wind line.
                    if (index + 1 < tokens.Length && AviationWeatherDecoder.TryDecodeVariableWind(tokens[index + 1], out var variation))
                    {
                        wind += $", {variation}";
                        index++;
                    }
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
                else if (TemperatureRegex().Match(token) is { Success: true } temperatures)
                {
                    lines.Add($"Temperature: {AviationWeatherDecoder.DecodeTemperature(temperatures.Groups[1].Value)}");
                    lines.Add($"Dew point: {AviationWeatherDecoder.DecodeTemperature(temperatures.Groups[2].Value)}");
                }
                else if (PressureRegex().Match(token) is { Success: true } pressure)
                {
                    // Q uses hectopascals; A uses hundredths of an inch of mercury.
                    lines.Add(pressure.Groups[1].Value == "Q"
                        ? $"Pressure (QNH): {int.Parse(pressure.Groups[2].Value)} hPa"
                        : $"Altimeter: {int.Parse(pressure.Groups[2].Value) / 100m:0.00} inHg");
                }
                else if (token == "CAVOK")
                {
                    lines.Add("Conditions: visibility 10 km or more, no significant cloud and no significant weather");
                }
                else if (token == "NOSIG")
                {
                    lines.Add("Trend: no significant change expected");
                }
                else if (token == "RMK")
                {
                    // Remarks are free-form and are preserved rather than guessed at.
                    lines.Add($"Remarks: {string.Join(' ', tokens[(index + 1)..])}");
                    break;
                }
                else if (AviationWeatherDecoder.TryDecodeWeather(token, out var weather))
                {
                    lines.Add($"Weather: {weather}");
                }
                else
                {
                    lines.Add($"Additional group: {token}");
                }
            }

            return lines;
        }

        /// <summary>
        /// Creates a generated regular expression for temperature and dew point.
        /// </summary>
        /// <returns>The temperature regular expression.</returns>
        [GeneratedRegex(@"^(M?\d{2})/(M?\d{2})$")]
        private static partial Regex TemperatureRegex();

        /// <summary>
        /// Creates a generated regular expression for QNH and altimeter settings.
        /// </summary>
        /// <returns>The pressure regular expression.</returns>
        [GeneratedRegex(@"^([QA])(\d{4})$")]
        private static partial Regex PressureRegex();
    }
}
