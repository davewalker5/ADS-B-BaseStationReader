SELECT      fnm.AirportIATA, fnm.AirportICAO, fnm.AirportName, COUNT( fnm.Id ) AS "Count"
FROM        FLIGHT_NUMBER_MAPPING fnm
GROUP BY    fnm.AirportIATA, fnm.AirportICAO, fnm.AirportName
ORDER BY    COUNT( fnm.Id ) DESC;