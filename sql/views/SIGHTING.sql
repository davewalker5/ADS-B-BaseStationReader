CREATE VIEW SIGHTING AS
    SELECT tracked.Id AS Id,
           aircraft.Id AS AircraftId,
           flight.Id AS FlightId,
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
           INNER JOIN
           FLIGHT AS flight ON flight.Id = (
                                               SELECT candidate.Id
                                                 FROM FLIGHT AS candidate
                                                WHERE candidate.Callsign = tracked.Callsign
                                                ORDER BY candidate.Id
                                                LIMIT 1
                                           )
     WHERE tracked.Address <> '000000' AND
           tracked.Callsign IS NOT NULL AND
           tracked.Callsign <> '' AND
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
