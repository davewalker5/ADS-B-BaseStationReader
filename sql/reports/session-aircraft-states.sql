SELECT ta.Address AS "Address",
       COALESCE(NULLIF(TRIM(ta.Callsign), ''), 'No callsign') AS "Callsign",
       ta.Track AS "Heading",
       ta.VerticalRate AS "Vertical Rate",
       ta.Altitude AS "Altitude",
       ta.Distance AS "Distance",
       ta.LastSeen AS "Observed At"
FROM TRACKED_AIRCRAFT ta
WHERE ta.SessionId = $session_id
ORDER BY ta.LastSeen, ta.Address;
