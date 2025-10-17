SELECT          ta.Address,
                ta.Callsign,
                fnm.AirlineIATA,
                fnm.AirlineICAO,
                fnm.AirlineName,
                fnm.AirportIATA,
                fnm.AirportICAO,
                fnm.AirportName,
                fnm.Embarkation,
                fnm.Destination,
                fnm.FlightIATA,
                ta.LastSeen
FROM            TRACKED_AIRCRAFT ta
INNER JOIN      AIRCRAFT a ON a.Address = ta.Address
INNER JOIN      FLIGHT_NUMBER_MAPPING fnm ON fnm.Callsign = ta.Callsign
LEFT OUTER JOIN EXCLUDED_ADDRESS ea ON ea.Address = ta.Address
WHERE           ta.LookupTimestamp IS NULL
AND             ea.Id IS NULL;