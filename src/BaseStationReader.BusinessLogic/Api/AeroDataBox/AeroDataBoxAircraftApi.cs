using System.Globalization;
using System.Text.Json.Nodes;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.BusinessLogic.Api.AeroDatabox
{
    public class AeroDataBoxAircraftApi : ExternalApiBase, IAircraftApi
    {
        private readonly string _baseAddress;
        private readonly string _host;
        private readonly string _key;

        public AeroDataBoxAircraftApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            string url,
            string key) : base(logger, client)
        {
            _baseAddress = $"{url}/icao24/";
            _key = key;

            // Extract the host from the url
            var uri = new Uri(url);
            _host = uri.Host;
        }

        /// <summary>
        /// Lookup an aircraft's details using its ICAO 24-bit address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Dictionary<ApiProperty, string>> LookupAircraftAsync(string address)
        {
            Logger.LogMessage(Severity.Info, $"Looking up aircraft with address {address}");
            var properties = await MakeApiRequestAsync($"{address}");
            return properties;
        }

        /// <summary>
        /// Make a request to the specified URL
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<Dictionary<ApiProperty, string>> MakeApiRequestAsync(string parameters)
        {
            Dictionary<ApiProperty, string> properties = [];

            try
            {
                // Make a request for the data from the API
                var url = $"{_baseAddress}{parameters}";
                var node = await GetAsync(Logger, ApiServiceType.AeroDataBox, url, new Dictionary<string, string>()
                {
                    { "X-RapidAPI-Key", _key },
                    { "X-RapidAPI-Host", _host },
                });

                if (node != null)
                {
                    // Extract the delivery date and use it to determine year of manufacture and age
                    int? manufactured = GetYearOfManufacture(node);
                    int? age = manufactured != null ? DateTime.Today.Year - manufactured : null;

                    // Extract the values into a dictionary
                    properties = new()
                    {
                        { ApiProperty.AircraftRegistration, node!["reg"]?.GetValue<string>() ?? "" },
                        { ApiProperty.AircraftManufactured, manufactured?.ToString() ?? "" },
                        { ApiProperty.AircraftAge, age?.ToString() ?? "" },
                        { ApiProperty.ModelICAO, node!["icaoCode"]?.GetValue<string>() ?? "" },
                        { ApiProperty.ModelIATA, node!["iataCodeShort"]?.GetValue<string>() ?? "" },
                        { ApiProperty.ModelName, node!["typeName"]?.GetValue<string>() ?? "" },
                        { ApiProperty.ManufacturerName, "" }
                    };

                    // Log the properties dictionary
                    LogProperties("Aircraft", properties);
                }
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Severity.Error, ex.Message);
                Logger.LogException(ex);
            }

            // Check there are some non-empty values
            var nonEmptyValueCount = properties.Values.Where(x => !string.IsNullOrEmpty(x)).Count();
            return nonEmptyValueCount > 0 ? properties : null;
        }

        /// <summary>
        /// Extract the year of manufacture from the response
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        private static int? GetYearOfManufacture(JsonNode node)
        {
            int? year = null;

            // Extract the delivery date from the response and attempt to parse it as a date
            var deliveryDate = node!["deliveryDate"]?.GetValue<string>() ?? "";
            if (!string.IsNullOrEmpty(deliveryDate) &&
                DateTime.TryParseExact(deliveryDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime delivered))
            {
                year = delivered.Year;
            }

            return year;
        }
    }
}