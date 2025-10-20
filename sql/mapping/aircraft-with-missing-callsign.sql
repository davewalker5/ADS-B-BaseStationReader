SELECT          ta.Id,
                ta.Address,
                ta.Callsign,
                ta.LastSeen
FROM            TRACKED_AIRCRAFT ta
LEFT OUTER JOIN EXCLUDED_ADDRESS ea ON ea.Address = ta.Address
WHERE           ea.Id IS NULL
AND             ta.LookupTimestamp IS NULL
AND             ta.Callsign IS NULL;