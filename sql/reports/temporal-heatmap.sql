SELECT CAST(STRFTIME('%w', ta.FirstSeen) AS INTEGER) AS "Weekday Number",
       CAST(STRFTIME('%H', ta.FirstSeen) AS INTEGER) AS "Hour",
       COUNT(*) AS "Observations"
FROM TRACKED_AIRCRAFT ta
GROUP BY STRFTIME('%w', ta.FirstSeen), STRFTIME('%H', ta.FirstSeen)
ORDER BY "Weekday Number", "Hour";
