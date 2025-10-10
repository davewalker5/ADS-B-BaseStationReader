SELECT      a.Address,
            a.Callsign,
            fnm.FlightIATA,
            fnm.AirlineIATA,
            fnm.AirlineICAO,
            fnm.AirlineName, 
            CASE fnm.AirportType
                WHEN 1 THEN 'Departure'
                WHEN 2 THEN 'Arrival'
                ELSE 'Unknown' END
                AS "AirportType"
FROM        FLIGHT_NUMBER_MAPPING fnm
INNER JOIN  TRACKED_AIRCRAFT a ON a.Callsign = fnm.Callsign;
