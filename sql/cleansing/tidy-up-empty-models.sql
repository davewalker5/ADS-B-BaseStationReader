SELECT      m.*
FROM        MODEL m
WHERE       LENGTH( m.ICAO ) = 0
AND         LENGTH( m.IATA ) = 0
AND         LENGTH( m.Name ) = 0;

SELECT      a.*
FROM        AIRCRAFT a
INNER JOIN  MODEL m ON m.Id = a.ModelId
WHERE       LENGTH( m.ICAO ) = 0
AND         LENGTH( m.IATA ) = 0
AND         LENGTH( m.Name ) = 0;

DELETE FROM MODEL
WHERE       LENGTH( ICAO ) = 0
AND         LENGTH( IATA ) = 0
AND         LENGTH( Name ) = 0;
