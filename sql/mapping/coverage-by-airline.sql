SELECT      fnm.AirlineIATA, fnm.AirlineICAO, fnm.AirlineName, COUNT( fnm.Id ) AS "Count"
FROM        FLIGHT_NUMBER_MAPPING fnm
GROUP BY    fnm.AirlineIATA, fnm.AirlineICAO, fnm.AirlineName
ORDER BY    COUNT( fnm.Id ) DESC;