SELECT          ta.Address, ta.Callsign, ta.LastSeen
FROM            TRACKED_AIRCRAFT ta
LEFT OUTER JOIN AIRCRAFT a ON a.Address = ta.Address
LEFT OUTER JOIN EXCLUDED_ADDRESS ea ON ea.Address = ta.Address
WHERE           a.Id IS NULL
AND             ea.Id IS NULL;