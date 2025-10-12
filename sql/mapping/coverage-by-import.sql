SELECT      fnm.FileName, COUNT( fnm.Id ) AS "Count"
FROM        FLIGHT_NUMBER_MAPPING fnm
GROUP BY    fnm.FileName
ORDER BY    COUNT( fnm.Id ) DESC;