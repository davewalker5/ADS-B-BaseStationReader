SELECT      COUNT( 1 )
FROM        TRACKED_AIRCRAFT
WHERE       SessionId = 0;

SELECT      COUNT( 1 )
FROM        POSITION p
INNER JOIN  TRACKED_AIRCRAFT ta ON ta.Id = p.AircraftId
WHERE       ta.SessionId = 0;
