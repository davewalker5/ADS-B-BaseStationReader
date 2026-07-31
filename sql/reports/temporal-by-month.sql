SELECT STRFTIME('%Y-%m', ta.FirstSeen) AS "Month",
       COUNT(*) AS "Observations",
       COUNT(DISTINCT ta.Address) AS "Aircraft"
FROM TRACKED_AIRCRAFT ta
GROUP BY STRFTIME('%Y-%m', ta.FirstSeen)
ORDER BY "Month";
