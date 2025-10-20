SELECT          ta.Id,
                ta.Address,
                ta.Callsign,
                ta.Status,
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
LEFT OUTER JOIN EXCLUDED_CALLSIGN ec ON ec.Callsign = ta.Callsign
WHERE           ta.LookupTimestamp IS NULL
AND             ea.Id IS NULL
AND             ec.Id IS NULL;