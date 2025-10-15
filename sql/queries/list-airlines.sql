SELECT  a.ICAO,
        a.IATA,
        a.Name,
        'Y' AS "Active"
FROM    AIRLINE a;
