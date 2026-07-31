SELECT MIN(s.StartedAtUtc) AS "Earliest Session",
       MAX(s.StartedAtUtc) AS "Latest Session",
       COUNT(*) AS "Session Count"
FROM SESSION s;
