SELECT          ta.Address,
                ta.Callsign,
                a.Registration,
                m.Name AS "Model",
                fnm.FlightIATA,
                fnm.AirlineName,
                CASE WHEN   LENGTH( fnm.Embarkation ) > 0 AND
                            LENGTH ( fnm.Destination ) > 0
                    THEN fnm.Embarkation || ' - ' || fnm.Destination
                    ELSE ''
                    END AS "Route",
                p.Altitude,
                p.Latitude,
                p.Longitude,
                p.Distance, 
                p.Timestamp
FROM            TRACKED_AIRCRAFT ta
INNER JOIN      POSITION p on p.AircraftId = ta.Id
LEFT OUTER JOIN AIRCRAFT a ON a.Address = ta.Address
LEFT OUTER JOIN MODEL m ON m.Id = a.ModelId
LEFT OUTER JOIN FLIGHT_NUMBER_MAPPING fnm ON fnm.Callsign = ta.Callsign
WHERE           ta.Address = '$ADDRESS'
ORDER BY        p.Timestamp ASC;
