SELECT      ta.Address, ta.Callsign, fnm.FlightIATA, ta.LastSeen
FROM        TRACKED_AIRCRAFT ta
INNER JOIN  FLIGHT_NUMBER_MAPPING fnm ON fnm.Callsign = ta.Callsign
WHERE       ta.LookupTimestamp IS NULL;