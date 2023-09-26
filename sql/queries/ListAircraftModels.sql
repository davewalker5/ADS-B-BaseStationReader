PRAGMA journal_mode=WAL;

SELECT am.IATA, am.ICAO, m.Name AS "Manufacturer", am.Name AS "Model", wtc.Category AS "WTC"
FROM AIRCRAFT_MODEL am
INNER JOIN MANUFACTURER m ON m.Id = am.ManufacturerId
INNER JOIN WAKE_TURBULENCE_CATEGORY wtc ON wtc.Id = am.WakeTurbulenceCategoryId;
