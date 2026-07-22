SELECT          ta.Address,
                ta.Callsign,
                a.Registration,
                m.Name AS "Model",
                f.IATA AS FlightIATA,
                al.Name AS AirlineName,
                CASE WHEN   LENGTH( f.Embarkation ) > 0 AND
                            LENGTH ( f.Destination ) > 0
                    THEN f.Embarkation || ' - ' || f.Destination
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
LEFT OUTER JOIN FLIGHT f ON f.Callsign = ta.Callsign
LEFT OUTER JOIN AIRLINE al ON al.Id = f.AirlineId
WHERE           ta.Address = '$ADDRESS'
ORDER BY        p.Timestamp ASC;
