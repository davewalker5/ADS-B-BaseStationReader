PRAGMA journal_mode=WAL;

SELECT      a.Address, COUNT(1) AS 'Number'
FROM        TRACKED_AIRCRAFT a
GROUP BY    a.Address
ORDER BY    COUNT(1) DESC;
