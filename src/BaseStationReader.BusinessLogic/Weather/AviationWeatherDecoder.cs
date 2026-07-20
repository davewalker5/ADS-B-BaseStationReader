using System.Text.RegularExpressions;

namespace BaseStationReader.BusinessLogic.Weather
{
    /// <summary>
    /// Provides decoding shared by METAR and TAF reports.
    /// </summary>
    internal static partial class AviationWeatherDecoder
    {
        private static readonly Dictionary<string, string> CloudAmounts = new(StringComparer.OrdinalIgnoreCase)
        {
            ["FEW"] = "few clouds",
            ["SCT"] = "scattered clouds",
            ["BKN"] = "broken cloud",
            ["OVC"] = "overcast",
            ["VV"] = "vertical visibility"
        };

        private static readonly Dictionary<string, string> WeatherCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            ["MI"] = "shallow", ["PR"] = "partial", ["BC"] = "patches of", ["DR"] = "low drifting",
            ["BL"] = "blowing", ["SH"] = "showers of", ["TS"] = "thunderstorm with", ["FZ"] = "freezing",
            ["DZ"] = "drizzle", ["RA"] = "rain", ["SN"] = "snow", ["SG"] = "snow grains",
            ["IC"] = "ice crystals", ["PL"] = "ice pellets", ["GR"] = "hail", ["GS"] = "small hail or snow pellets",
            ["UP"] = "unidentified precipitation", ["BR"] = "mist", ["FG"] = "fog", ["FU"] = "smoke",
            ["VA"] = "volcanic ash", ["DU"] = "widespread dust", ["SA"] = "sand", ["HZ"] = "haze",
            ["PY"] = "spray", ["PO"] = "dust or sand whirls", ["SQ"] = "squalls", ["FC"] = "funnel cloud or tornado",
            ["SS"] = "sandstorm", ["DS"] = "dust storm"
        };

        /// <summary>
        /// Splits and normalises a weather report into individual groups.
        /// </summary>
        /// <param name="report">The raw report.</param>
        /// <returns>The normalised report groups.</returns>
        internal static string[] Tokenise(string report)
        {
            // Reports are group-based, so all runs of whitespace have the same meaning.
            return WhitespaceRegex().Split(report.Trim().TrimEnd('='))
                .Where(token => token.Length > 0)
                .Select(token => token.ToUpperInvariant())
                .ToArray();
        }

        /// <summary>
        /// Decodes a day and UTC time group.
        /// </summary>
        /// <param name="token">A DDHHMMZ group.</param>
        /// <param name="description">The decoded description.</param>
        /// <returns>True when the token was a valid time group.</returns>
        internal static bool TryDecodeTime(string token, out string description)
        {
            // METAR and TAF issue times always contain a two-digit day, hour and minute.
            var match = TimeRegex().Match(token);
            description = match.Success
                ? $"day {match.Groups[1].Value} at {match.Groups[2].Value}:{match.Groups[3].Value} UTC"
                : string.Empty;
            return match.Success;
        }

        /// <summary>
        /// Decodes a TAF validity or change period.
        /// </summary>
        /// <param name="token">A DDHH/DDHH group.</param>
        /// <param name="description">The decoded period.</param>
        /// <returns>True when the token was a valid period.</returns>
        internal static bool TryDecodePeriod(string token, out string description)
        {
            // A value of 24 is retained because it means midnight at the end of the stated day.
            var match = PeriodRegex().Match(token);
            description = match.Success
                ? $"day {match.Groups[1].Value} at {match.Groups[2].Value}:00 to day {match.Groups[3].Value} at {match.Groups[4].Value}:00 UTC"
                : string.Empty;
            return match.Success;
        }

        /// <summary>
        /// Decodes a surface wind group.
        /// </summary>
        /// <param name="token">The encoded wind group.</param>
        /// <param name="description">The decoded wind.</param>
        /// <returns>True when the token was a wind group.</returns>
        internal static bool TryDecodeWind(string token, out string description)
        {
            // Wind groups contain a direction, sustained speed, optional gust and unit.
            var match = WindRegex().Match(token);
            if (!match.Success)
            {
                description = string.Empty;
                return false;
            }

            var direction = match.Groups[1].Value == "VRB" ? "variable" : $"from {match.Groups[1].Value}°";
            var unit = match.Groups[4].Value switch
            {
                "MPS" => "metres per second",
                "KMH" => "kilometres per hour",
                _ => "knots"
            };
            var gust = match.Groups[3].Success ? $", gusting to {int.Parse(match.Groups[3].Value)} {unit}" : string.Empty;
            description = $"{direction} at {int.Parse(match.Groups[2].Value)} {unit}{gust}";
            return true;
        }

        /// <summary>
        /// Decodes a variable wind direction group.
        /// </summary>
        /// <param name="token">The encoded direction range.</param>
        /// <param name="description">The decoded direction range.</param>
        /// <returns>True when the token was a direction range.</returns>
        internal static bool TryDecodeVariableWind(string token, out string description)
        {
            // This group follows a wind group when direction varies by at least 60 degrees.
            var match = VariableWindRegex().Match(token);
            description = match.Success
                ? $"varying between {match.Groups[1].Value}° and {match.Groups[2].Value}°"
                : string.Empty;
            return match.Success;
        }

        /// <summary>
        /// Decodes prevailing visibility.
        /// </summary>
        /// <param name="token">The encoded visibility group.</param>
        /// <param name="description">The decoded visibility.</param>
        /// <returns>True when the token was a visibility group.</returns>
        internal static bool TryDecodeVisibility(string token, out string description)
        {
            // Four digits represent metres, while US reports commonly use statute miles.
            if (token == "9999")
            {
                description = "10 km or more";
                return true;
            }

            if (VisibilityRegex().IsMatch(token))
            {
                description = $"{int.Parse(token):N0} metres";
                return true;
            }

            var match = StatuteMileRegex().Match(token);
            if (match.Success)
            {
                var prefix = match.Groups[1].Value == "P" ? "more than " : match.Groups[1].Value == "M" ? "less than " : string.Empty;
                description = $"{prefix}{match.Groups[2].Value.Replace('/', '⁄')} statute miles";
                return true;
            }

            description = string.Empty;
            return false;
        }

        /// <summary>
        /// Decodes a cloud layer or clear-sky group.
        /// </summary>
        /// <param name="token">The encoded cloud group.</param>
        /// <param name="description">The decoded cloud information.</param>
        /// <returns>True when the token was a cloud group.</returns>
        internal static bool TryDecodeCloud(string token, out string description)
        {
            // Clear-sky abbreviations differ between automatic and manually observed reports.
            if (token is "NCD" or "NSC" or "SKC" or "CLR")
            {
                description = token == "NCD" ? "no cloud detected" : "no significant cloud";
                return true;
            }

            var match = CloudRegex().Match(token);
            if (!match.Success)
            {
                description = string.Empty;
                return false;
            }

            var amount = CloudAmounts[match.Groups[1].Value];
            var height = int.Parse(match.Groups[2].Value) * 100;
            var type = match.Groups[3].Value switch
            {
                "CB" => ", cumulonimbus",
                "TCU" => ", towering cumulus",
                _ => string.Empty
            };
            description = $"{amount} at {height:N0} feet{type}";
            return true;
        }

        /// <summary>
        /// Decodes a significant weather group.
        /// </summary>
        /// <param name="token">The encoded weather group.</param>
        /// <param name="description">The decoded weather.</param>
        /// <returns>True when the token was a recognised weather group.</returns>
        internal static bool TryDecodeWeather(string token, out string description)
        {
            // NSW explicitly cancels significant weather in a forecast change group.
            if (token == "NSW")
            {
                description = "no significant weather";
                return true;
            }

            var working = token;
            var parts = new List<string>();
            if (working.StartsWith('+') || working.StartsWith('-'))
            {
                parts.Add(working[0] == '+' ? "heavy" : "light");
                working = working[1..];
            }
            else if (working.StartsWith("VC"))
            {
                parts.Add("in the vicinity");
                working = working[2..];
            }

            // Weather abbreviations are pairs, including optional descriptor and phenomena.
            while (working.Length >= 2 && WeatherCodes.TryGetValue(working[..2], out var meaning))
            {
                parts.Add(meaning);
                working = working[2..];
            }

            description = working.Length == 0 && parts.Count > 0 ? string.Join(' ', parts) : string.Empty;
            return description.Length > 0;
        }

        /// <summary>
        /// Formats a signed METAR temperature value.
        /// </summary>
        /// <param name="value">The encoded temperature value.</param>
        /// <returns>The temperature in degrees Celsius.</returns>
        internal static string DecodeTemperature(string value)
        {
            // M is the METAR prefix for a temperature below zero.
            return value.StartsWith('M') ? $"-{int.Parse(value[1..])}°C" : $"{int.Parse(value)}°C";
        }

        /// <summary>
        /// Creates a generated regular expression for report whitespace.
        /// </summary>
        /// <returns>The whitespace regular expression.</returns>
        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        /// <summary>
        /// Creates a generated regular expression for issue and observation times.
        /// </summary>
        /// <returns>The time regular expression.</returns>
        [GeneratedRegex(@"^(\d{2})(\d{2})(\d{2})Z$")]
        private static partial Regex TimeRegex();

        /// <summary>
        /// Creates a generated regular expression for TAF periods.
        /// </summary>
        /// <returns>The period regular expression.</returns>
        [GeneratedRegex(@"^(\d{2})(\d{2})/(\d{2})(\d{2})$")]
        private static partial Regex PeriodRegex();

        /// <summary>
        /// Creates a generated regular expression for wind groups.
        /// </summary>
        /// <returns>The wind regular expression.</returns>
        [GeneratedRegex(@"^(\d{3}|VRB)(\d{2,3})(?:G(\d{2,3}))?(KT|MPS|KMH)$")]
        private static partial Regex WindRegex();

        /// <summary>
        /// Creates a generated regular expression for variable wind direction.
        /// </summary>
        /// <returns>The variable-wind regular expression.</returns>
        [GeneratedRegex(@"^(\d{3})V(\d{3})$")]
        private static partial Regex VariableWindRegex();

        /// <summary>
        /// Creates a generated regular expression for visibility in metres.
        /// </summary>
        /// <returns>The visibility regular expression.</returns>
        [GeneratedRegex(@"^\d{4}$")]
        private static partial Regex VisibilityRegex();

        /// <summary>
        /// Creates a generated regular expression for visibility in statute miles.
        /// </summary>
        /// <returns>The statute-mile regular expression.</returns>
        [GeneratedRegex(@"^([PM]?)(\d+(?:/\d+)?)SM$")]
        private static partial Regex StatuteMileRegex();

        /// <summary>
        /// Creates a generated regular expression for cloud layers.
        /// </summary>
        /// <returns>The cloud regular expression.</returns>
        [GeneratedRegex(@"^(FEW|SCT|BKN|OVC|VV)(\d{3})(CB|TCU)?$")]
        private static partial Regex CloudRegex();
    }
}
