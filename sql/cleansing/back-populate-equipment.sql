INSERT INTO SESSION_EQUIPMENT ( EquipmentId, SessionId )
SELECT      e.Id, s.Id
FROM        EQUIPMENT e
CROSS JOIN  SESSION s
WHERE       s.Host IN ( 'host.docker.internal', '127.0.0.1' )
AND         e.Id IN ( 2, 4 );