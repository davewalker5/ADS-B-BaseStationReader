SELECT          ta.Address,
                NULL AS "Registration",
                NULL AS "IATA",
                NULL AS "ICAO",
                NULL AS "Manufactured",
                'MANUAL' AS "Provenance",
                ta.Callsign,
                ta.LastSeen
FROM            TRACKED_AIRCRAFT ta
LEFT OUTER JOIN AIRCRAFT a ON a.Address = ta.Address
LEFT OUTER JOIN EXCLUDED_ADDRESS ea ON ea.Address = ta.Address
WHERE           a.Id IS NULL
AND             ea.Id IS NULL;
