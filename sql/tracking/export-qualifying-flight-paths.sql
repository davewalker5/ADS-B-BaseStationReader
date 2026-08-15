WITH parameters AS (
    SELECT  200 AS MinimumPoints,
            5000 AS MinimumAltitudeChange,
            1.10 AS MinimumCurvatureRatio,
            1.0 AS MinimumExcessDistance
),
ordered_positions AS (
    SELECT  p.Id,
            p.AircraftId,
            p.Altitude,
            p.Latitude,
            p.Longitude,
            LAG( p.Latitude ) OVER (
                PARTITION BY p.AircraftId
                ORDER BY p.Timestamp, p.Id
            ) AS PreviousLatitude,
            LAG( p.Longitude ) OVER (
                PARTITION BY p.AircraftId
                ORDER BY p.Timestamp, p.Id
            ) AS PreviousLongitude,
            ROW_NUMBER() OVER (
                PARTITION BY p.AircraftId
                ORDER BY p.Timestamp, p.Id
            ) AS PositionNumber,
            ROW_NUMBER() OVER (
                PARTITION BY p.AircraftId
                ORDER BY p.Timestamp DESC, p.Id DESC
            ) AS ReversePositionNumber
    FROM    POSITION p
    WHERE   p.Latitude IS NOT NULL
    AND     p.Longitude IS NOT NULL
    AND     p.Altitude IS NOT NULL
),
flight_path_metrics AS (
    SELECT  op.AircraftId,
            COUNT( op.Id ) AS PointCount,
            MAX( op.Altitude ) - MIN( op.Altitude ) AS AltitudeChange,
            SUM(
                CASE
                    WHEN op.PreviousLatitude IS NULL THEN 0
                    ELSE SQRT(
                        ( op.Latitude - op.PreviousLatitude )
                            * ( op.Latitude - op.PreviousLatitude )
                        + ( op.Longitude - op.PreviousLongitude )
                            * ( op.Longitude - op.PreviousLongitude )
                            * COS( op.Latitude * 3.141592653589793 / 180.0 )
                            * COS( op.Latitude * 3.141592653589793 / 180.0 )
                    ) * 60.0
                END
            ) AS TravelledDistance,
            SQRT(
                ( MAX( CASE WHEN op.ReversePositionNumber = 1 THEN op.Latitude END )
                    - MAX( CASE WHEN op.PositionNumber = 1 THEN op.Latitude END ) )
                    * ( MAX( CASE WHEN op.ReversePositionNumber = 1 THEN op.Latitude END )
                        - MAX( CASE WHEN op.PositionNumber = 1 THEN op.Latitude END ) )
                + ( MAX( CASE WHEN op.ReversePositionNumber = 1 THEN op.Longitude END )
                    - MAX( CASE WHEN op.PositionNumber = 1 THEN op.Longitude END ) )
                    * ( MAX( CASE WHEN op.ReversePositionNumber = 1 THEN op.Longitude END )
                        - MAX( CASE WHEN op.PositionNumber = 1 THEN op.Longitude END ) )
                    * COS( AVG( op.Latitude ) * 3.141592653589793 / 180.0 )
                    * COS( AVG( op.Latitude ) * 3.141592653589793 / 180.0 )
            ) * 60.0 AS EndpointDistance
    FROM    ordered_positions op
    GROUP BY op.AircraftId
)
SELECT      ta.Address,
            s.Id AS "Session ID"
FROM        SESSION s
INNER JOIN  TRACKED_AIRCRAFT ta ON ta.SessionId = s.Id
INNER JOIN  flight_path_metrics fpm ON fpm.AircraftId = ta.Id
CROSS JOIN  parameters criteria
WHERE       s.Id = 28
AND         fpm.PointCount >= criteria.MinimumPoints
AND         (
                fpm.AltitudeChange > criteria.MinimumAltitudeChange
                OR (
                    fpm.TravelledDistance - fpm.EndpointDistance
                        >= criteria.MinimumExcessDistance
                    AND fpm.TravelledDistance
                        >= fpm.EndpointDistance * criteria.MinimumCurvatureRatio
                )
            )
ORDER BY    fpm.PointCount DESC,
            fpm.AltitudeChange DESC;
