SELECT      fnm.AirportIATA,
            fnm.AirportICAO,
            fnm.AirportName,
            CASE fnm.AirportType
                WHEN 1 THEN 'Departure'
                WHEN 2 THEN 'Arrival'
                WHEN 0 THEN 'Unknown'
                END AS "AirportType",
            COUNT( fnm.Id ) AS "Count"
FROM        FLIGHT_NUMBER_MAPPING fnm
GROUP BY    fnm.AirportIATA, fnm.AirportICAO, fnm.AirportName, fnm.AirportType
ORDER BY    COUNT( fnm.Id ) DESC;