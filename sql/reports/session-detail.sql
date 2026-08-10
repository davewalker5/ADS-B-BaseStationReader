SELECT s.Id AS "Session Id",
       s.Name AS "Session Name",
       s.StartedAtUtc AS "Started At UTC",
       COALESCE(MAX(ta.LastSeen), s.StartedAtUtc) AS "Ended At UTC",
       ROUND((JULIANDAY(COALESCE(MAX(ta.LastSeen), s.StartedAtUtc)) - JULIANDAY(s.StartedAtUtc)) * 86400.0) AS "Duration Seconds",
       s.ProfileName AS "Tracking Profile",
       s.Notes AS "Session Notes",
       s.Host AS "Receiver Host",
       s.Port AS "Receiver Port",
       s.ReceiverLatitude AS "Receiver Latitude",
       s.ReceiverLongitude AS "Receiver Longitude",
       s.ReceiverElevation AS "Receiver Elevation",
       s.MinimumAltitude AS "Configured Minimum Altitude",
       s.MaximumAltitude AS "Configured Maximum Altitude",
       s.MaximumDistance AS "Configured Maximum Distance",
       s.IncludedBehaviours AS "Included Behaviours",
       COUNT(DISTINCT ta.Id) AS "Aircraft Observations",
       COUNT(DISTINCT ta.Address) AS "Distinct Aircraft",
       COUNT(DISTINCT NULLIF(TRIM(ta.Callsign), '')) AS "Distinct Callsigns",
       COUNT(DISTINCT CASE WHEN p.Id IS NOT NULL THEN ta.Address END) AS "Aircraft With Positions",
       COUNT(p.Id) AS "Position Records",
       MAX(p.Distance) AS "Maximum Range",
       (SELECT COUNT(*)
        FROM POSITION_DENSITY_SNAPSHOT_CELL cell
        WHERE cell.PositionDensitySnapshotId = (
            SELECT snapshot.Id
            FROM POSITION_DENSITY_SNAPSHOT snapshot
            WHERE snapshot.SessionId = s.Id
            ORDER BY snapshot.CapturedAtUtc DESC, snapshot.Id DESC
            LIMIT 1)) AS "Final Occupied Cells",
       (SELECT COUNT(*)
        FROM POSITION_DENSITY_SNAPSHOT snapshot
        WHERE snapshot.SessionId = s.Id) AS "Density Snapshots"
FROM SESSION s
LEFT JOIN TRACKED_AIRCRAFT ta ON ta.SessionId = s.Id
LEFT JOIN POSITION p ON p.AircraftId = ta.Id
WHERE s.Id = $session_id
GROUP BY s.Id;
