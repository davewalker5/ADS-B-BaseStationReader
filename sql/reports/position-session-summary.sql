SELECT s.Id AS "Session Id",
       s.StartedAtUtc AS "Started At UTC",
       COUNT(p.Id) AS "Position Records",
       COUNT(DISTINCT ta.Address) AS "Aircraft",
       ROUND(MAX(p.Distance), 2) AS "Maximum Distance",
       ROUND(MIN(p.Distance), 2) AS "Closest Approach"
FROM SESSION s
LEFT JOIN TRACKED_AIRCRAFT ta ON ta.SessionId = s.Id
LEFT JOIN POSITION p ON p.AircraftId = ta.Id
GROUP BY s.Id, s.StartedAtUtc
HAVING COUNT(p.Id) > 0
ORDER BY "Position Records" DESC;
