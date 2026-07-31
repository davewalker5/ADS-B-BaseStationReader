SELECT CAST(STRFTIME('%H', ta.FirstSeen) AS INTEGER) AS "Hour",
       COUNT(*) AS "Observations",
       COUNT(DISTINCT ta.Address) AS "Aircraft"
FROM TRACKED_AIRCRAFT ta
GROUP BY STRFTIME('%H', ta.FirstSeen)
ORDER BY "Hour";
