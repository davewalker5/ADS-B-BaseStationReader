SELECT          ta.Id,
                ta.Address,
                ta.Callsign,
                ta.LastSeen
FROM            TRACKED_AIRCRAFT ta
LEFT OUTER JOIN FLIGHT_NUMBER_MAPPING fnm ON fnm.Callsign = ta.Callsign
LEFT OUTER JOIN EXCLUDED_ADDRESS ea ON ea.Address = ta.Address
LEFT OUTER JOIN EXCLUDED_CALLSIGN ec ON ec.Callsign = ta.Callsign
WHERE           ta.LookupTimestamp IS NULL
AND             ea.Id IS NULL
AND             ec.Id IS NULL
AND             LENGTH( IFNULL( ta.Callsign, '' )) > 0
AND             fnm.Id IS NULL;
