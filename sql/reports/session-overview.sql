SELECT s.Id AS "Session Id",
       s.StartedAtUtc AS "Started At UTC",
       COALESCE(MAX(ta.LastSeen), s.StartedAtUtc) AS "Ended At UTC",
       ROUND((JULIANDAY(COALESCE(MAX(ta.LastSeen), s.StartedAtUtc)) - JULIANDAY(s.StartedAtUtc)) * 24.0, 2) AS "Duration Hours",
       COUNT(DISTINCT ta.Id) AS "Tracked Aircraft",
       COUNT(p.Id) AS "Position Records",
       s.ProfileName AS "Profile"
FROM SESSION s
LEFT JOIN TRACKED_AIRCRAFT ta ON ta.SessionId = s.Id
LEFT JOIN POSITION p ON p.AircraftId = ta.Id
GROUP BY s.Id, s.StartedAtUtc, s.ProfileName
ORDER BY s.StartedAtUtc;
