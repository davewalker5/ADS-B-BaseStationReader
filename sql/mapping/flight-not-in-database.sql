SELECT          ta.Callsign,
                ta.LastSeen
FROM            TRACKED_AIRCRAFT ta
LEFT OUTER JOIN FLIGHT_NUMBER_MAPPING fnm ON fnm.Callsign = ta.Callsign
WHERE           ta.LookupTimestamp IS NULL
AND             LENGTH( IFNULL( ta.Callsign, '' )) > 0
AND             fnm.Id IS NULL;
