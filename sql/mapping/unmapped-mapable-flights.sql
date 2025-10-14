SELECT      ta.Address,
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
FROM        TRACKED_AIRCRAFT ta
INNER JOIN  FLIGHT_NUMBER_MAPPING fnm ON fnm.Callsign = ta.Callsign
WHERE       ta.LookupTimestamp IS NULL;