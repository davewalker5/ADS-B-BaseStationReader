SELECT      s.Id, s.Name, s.StartedAtUtc AS "Started", MAX( ta.LastSeen ) AS "Ended"
FROM        SESSION s
INNER JOIN  TRACKED_AIRCRAFT ta ON ta.SessionId = s.Id
GROUP BY    s.Id, s.Name, s.StartedAtUtc
ORDER BY    s.Id;
