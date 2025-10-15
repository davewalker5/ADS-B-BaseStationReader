SELECT          ta.Address,
                ta.Callsign,
                CASE ta.Status
                    WHEN 0 THEN 'Active'
                    WHEN 1 THEN 'Inactive'
                    WHEN 2 THEN 'Stale'
                    WHEN 3 THEN 'Locked'
                    ELSE 'Invalid'
                    END AS "Status",
                IFNULL( fnm.FlightIATA, 'No Mapping' ) AS "FlightIATA"
FROM            TRACKED_AIRCRAFT ta
LEFT OUTER JOIN FLIGHT_NUMBER_MAPPING fnm ON fnm.Callsign = ta.Callsign
WHERE           LENGTH( IFNULL( ta.Address, '' )) > 0 AND
                LENGTH( IFNULL( ta.Callsign, '' )) > 0 AND
                ta.LookupTimestamp IS NULL AND
                ta.Status <> 3 AND
                LookupAttempts < 5;
