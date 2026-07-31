SELECT ta.Address AS "Address",
       COALESCE(NULLIF(a.Registration, ''), 'Unidentified') AS "Registration",
       COUNT(DISTINCT NULLIF(TRIM(ta.Callsign), '')) AS "Callsigns",
       GROUP_CONCAT(DISTINCT NULLIF(TRIM(ta.Callsign), '')) AS "Observed Callsigns",
       COUNT(DISTINCT ta.Id) AS "Observations"
FROM TRACKED_AIRCRAFT ta
LEFT JOIN AIRCRAFT a ON a.Address = ta.Address
GROUP BY ta.Address, a.Registration
ORDER BY "Callsigns" DESC, "Observations" DESC;
