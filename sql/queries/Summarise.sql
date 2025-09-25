PRAGMA journal_mode=WAL;

SELECT      'Aircraft' AS 'Record_Type', COUNT(1) AS 'Number'
FROM        TRACKED_AIRCRAFT
UNION ALL
SELECT      'Position', COUNT(1)
FROM        POSITION;
