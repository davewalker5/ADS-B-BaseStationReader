SELECT CASE
           WHEN p.Altitude IS NULL THEN 'Unknown'
           WHEN p.Altitude < 5000 THEN 'Below 5,000 ft'
           WHEN p.Altitude < 10000 THEN '5,000-9,999 ft'
           WHEN p.Altitude < 20000 THEN '10,000-19,999 ft'
           WHEN p.Altitude < 30000 THEN '20,000-29,999 ft'
           WHEN p.Altitude < 40000 THEN '30,000-39,999 ft'
           ELSE '40,000 ft and above'
       END AS "Altitude Band",
       CASE
           WHEN p.Altitude IS NULL THEN 7
           WHEN p.Altitude < 5000 THEN 1 WHEN p.Altitude < 10000 THEN 2
           WHEN p.Altitude < 20000 THEN 3 WHEN p.Altitude < 30000 THEN 4
           WHEN p.Altitude < 40000 THEN 5 ELSE 6
       END AS "Band Order",
       COUNT(*) AS "Position Records"
FROM POSITION p
GROUP BY "Altitude Band", "Band Order"
ORDER BY "Band Order";
