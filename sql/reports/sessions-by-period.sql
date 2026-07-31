SELECT 'Day' AS "Period Type", STRFTIME('%Y-%m-%d', StartedAtUtc) AS "Period", COUNT(*) AS "Sessions"
FROM SESSION GROUP BY STRFTIME('%Y-%m-%d', StartedAtUtc)
UNION ALL
SELECT 'Week', STRFTIME('%Y-W%W', StartedAtUtc), COUNT(*)
FROM SESSION GROUP BY STRFTIME('%Y-W%W', StartedAtUtc)
UNION ALL
SELECT 'Month', STRFTIME('%Y-%m', StartedAtUtc), COUNT(*)
FROM SESSION GROUP BY STRFTIME('%Y-%m', StartedAtUtc)
ORDER BY "Period Type", "Period";
