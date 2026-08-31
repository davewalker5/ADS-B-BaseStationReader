WITH totals AS (
    SELECT COUNT(DISTINCT tracked.Address) AS Aircraft,
           COUNT(DISTINCT NULLIF(TRIM(tracked.Callsign), '')) AS Callsigns
    FROM TRACKED_AIRCRAFT tracked
),
aircraft_identified AS (
    SELECT COUNT(DISTINCT tracked.Address) AS Identified
    FROM TRACKED_AIRCRAFT tracked
    INNER JOIN AIRCRAFT aircraft ON aircraft.Address = tracked.Address
),
aircraft_without_callsign AS (
    SELECT COUNT(DISTINCT tracked.Address) AS Identified
    FROM TRACKED_AIRCRAFT tracked
    WHERE NULLIF(TRIM(tracked.Callsign), '') IS NULL
),
airlines_identified AS (
    SELECT COUNT(DISTINCT NULLIF(TRIM(tracked.Callsign), '')) AS Identified
    FROM TRACKED_AIRCRAFT tracked
    INNER JOIN SIGHTING sighting ON sighting.Id = tracked.Id
    WHERE sighting.AirlineId IS NOT NULL
),
flights_identified AS (
    SELECT COUNT(DISTINCT NULLIF(TRIM(tracked.Callsign), '')) AS Identified
    FROM TRACKED_AIRCRAFT tracked
    INNER JOIN FLIGHT flight ON flight.Id = (
        SELECT candidate.Id
        FROM FLIGHT candidate
        WHERE candidate.Callsign = TRIM(tracked.Callsign)
        ORDER BY candidate.Id
        LIMIT 1)
)
SELECT 'Aircraft identified' AS "Measure",
       aircraft_identified.Identified AS "Identified",
       totals.Aircraft AS "Total",
       ROUND(100.0 * aircraft_identified.Identified / NULLIF(totals.Aircraft, 0), 1) AS "% Identified"
FROM totals, aircraft_identified
UNION ALL
SELECT 'Aircraft with no callsign',
       aircraft_without_callsign.Identified,
       totals.Aircraft,
       ROUND(100.0 * aircraft_without_callsign.Identified / NULLIF(totals.Aircraft, 0), 1)
FROM totals, aircraft_without_callsign
UNION ALL
SELECT 'Airlines identified',
       airlines_identified.Identified,
       totals.Callsigns,
       ROUND(100.0 * airlines_identified.Identified / NULLIF(totals.Callsigns, 0), 1)
FROM totals, airlines_identified
UNION ALL
SELECT 'Flights identified',
       flights_identified.Identified,
       totals.Callsigns,
       ROUND(100.0 * flights_identified.Identified / NULLIF(totals.Callsigns, 0), 1)
FROM totals, flights_identified;
