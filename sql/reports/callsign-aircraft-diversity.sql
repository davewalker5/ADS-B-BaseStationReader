SELECT COALESCE(NULLIF(TRIM(ta.Callsign), ''), 'No callsign') AS "Callsign",
       COUNT(DISTINCT ta.Address) AS "Aircraft",
       GROUP_CONCAT(DISTINCT ta.Address) AS "Observed Addresses",
       COUNT(DISTINCT ta.Id) AS "Observations"
FROM TRACKED_AIRCRAFT ta
GROUP BY COALESCE(NULLIF(TRIM(ta.Callsign), ''), 'No callsign')
ORDER BY "Aircraft" DESC, "Observations" DESC;
