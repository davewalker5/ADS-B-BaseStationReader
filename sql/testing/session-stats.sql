SELECT      s.Id, s.StartedAtUtc, s.Name, 'Aircraft' AS "Entity", COUNT( ta.Id ) AS "Count"
FROM        TRACKED_AIRCRAFT ta
INNER JOIN  SESSION s ON s.Id = ta.SessionId
WHERE       s.Id IN ( SELECT MAX( Id ) FROM SESSION )
GROUP BY    s.Id, s.StartedAtUtc, s.Name
UNION ALL
SELECT      s.Id, s.StartedAtUtc, s.Name, 'Positions' AS "Entity", COUNT( p.Id ) AS "Count"
FROM        POSITION p
INNER JOIN  TRACKED_AIRCRAFT ta ON ta.Id = p.AircraftId
INNER JOIN  SESSION s ON s.Id = ta.SessionId
WHERE       s.Id IN ( SELECT MAX( Id ) FROM SESSION )
GROUP BY    s.Id, s.StartedAtUtc, s.Name
UNION ALL
SELECT      s.Id, s.StartedAtUtc, s.Name, 'Position Density Snapshots' AS "Entity", COUNT( p.Id ) AS "Count"
FROM        POSITION_DENSITY_SNAPSHOT p
INNER JOIN  SESSION s ON s.Id = p.SessionId
WHERE       s.Id IN ( SELECT MAX( Id ) FROM SESSION )
GROUP BY    s.Id, s.StartedAtUtc, s.Name
UNION ALL
SELECT      s.Id, s.StartedAtUtc, s.Name, 'Position Density Cells' AS "Entity", COUNT( c.Id ) AS "Count"
FROM        POSITION_DENSITY_SNAPSHOT_CELL c
INNER JOIN  POSITION_DENSITY_SNAPSHOT p ON p.Id = c.PositionDensitySnapshotId
INNER JOIN  SESSION s ON s.Id = p.SessionId
WHERE       s.Id IN ( SELECT MAX( Id ) FROM SESSION )
GROUP BY    s.Id, s.StartedAtUtc, s.Name;
