SELECT snapshot.Id AS "Snapshot Id",
       snapshot.SessionId AS "Session Id",
       snapshot.CapturedAtUtc AS "Captured At UTC",
       snapshot.PositionCount AS "Position Count",
       snapshot.MaximumBinCount AS "Maximum Bin Count",
       snapshot.MinimumLatitude AS "Minimum Latitude",
       snapshot.MaximumLatitude AS "Maximum Latitude",
       snapshot.MinimumLongitude AS "Minimum Longitude",
       snapshot.MaximumLongitude AS "Maximum Longitude",
       cell.Latitude AS "Cell Latitude",
       cell.Longitude AS "Cell Longitude",
       cell.Count AS "Cell Count"
FROM POSITION_DENSITY_SNAPSHOT snapshot
LEFT JOIN POSITION_DENSITY_SNAPSHOT_CELL cell
       ON cell.PositionDensitySnapshotId = snapshot.Id
WHERE snapshot.SessionId = $session_id
ORDER BY snapshot.CapturedAtUtc,
         snapshot.Id,
         cell.Latitude,
         cell.Longitude;
