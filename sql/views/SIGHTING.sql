CREATE VIEW SIGHTING AS
    SELECT tracked.Id AS Id,
           aircraft.Id AS AircraftId,
           flight.Id AS FlightId,
           COALESCE(flight.AirlineId, fallback_airline.Id) AS AirlineId,
           tracked.FirstSeen AS Timestamp
      FROM TRACKED_AIRCRAFT AS tracked
           INNER JOIN
           AIRCRAFT AS aircraft ON aircraft.Id = (
                                                     SELECT candidate.Id
                                                       FROM AIRCRAFT AS candidate
                                                      WHERE candidate.Address = tracked.Address
                                                      ORDER BY candidate.Id
                                                      LIMIT 1
                                                 )
           LEFT JOIN
           FLIGHT AS flight ON flight.Id = (
                                               SELECT candidate.Id
                                                 FROM FLIGHT AS candidate
                                                WHERE candidate.Callsign = tracked.Callsign
                                                ORDER BY candidate.Id
                                                LIMIT 1
                                           )
           LEFT JOIN
           AIRLINE AS fallback_airline ON flight.Id IS NULL AND
                                          fallback_airline.Id = (
                                              SELECT candidate.Id
                                                FROM AIRLINE AS candidate
                                               WHERE candidate.ICAO = SUBSTR(tracked.Callsign, 1, 3)
                                               ORDER BY candidate.Id
                                               LIMIT 1
                                          )
     WHERE tracked.Address <> '000000' AND
           tracked.Callsign IS NOT NULL AND
           tracked.Callsign <> '' AND
           (flight.Id IS NOT NULL OR
            (fallback_airline.Id IS NOT NULL AND
             LENGTH(tracked.Callsign) > 3 AND
             SUBSTR(tracked.Callsign, 4) GLOB '*[0-9]*')) AND
           NOT EXISTS (
                   SELECT 1
                     FROM EXCLUDED_ADDRESS AS excluded
                    WHERE excluded.Address = tracked.Address
               )
           AND
           NOT EXISTS (
                   SELECT 1
                     FROM EXCLUDED_CALLSIGN AS excluded
                    WHERE excluded.Callsign = tracked.Callsign
               );
