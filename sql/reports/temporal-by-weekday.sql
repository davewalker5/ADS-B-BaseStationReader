SELECT CAST(STRFTIME('%w', ta.FirstSeen) AS INTEGER) AS "Weekday Number",
       CASE STRFTIME('%w', ta.FirstSeen)
         WHEN '0' THEN 'Sunday' WHEN '1' THEN 'Monday' WHEN '2' THEN 'Tuesday'
         WHEN '3' THEN 'Wednesday' WHEN '4' THEN 'Thursday' WHEN '5' THEN 'Friday'
         ELSE 'Saturday' END AS "Weekday",
       COUNT(*) AS "Observations",
       COUNT(DISTINCT ta.Address) AS "Aircraft"
FROM TRACKED_AIRCRAFT ta
GROUP BY STRFTIME('%w', ta.FirstSeen)
ORDER BY "Weekday Number";
