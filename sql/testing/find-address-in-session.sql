SELECT      ta.*  
FROM        TRACKED_AIRCRAFT ta
WHERE       ta.SessionId IN (
    SELECT  MAX( Id )
    FROM    SESSION
)
AND         ta.Address = '';
