UPDATE  TRACKED_AIRCRAFT
SET     LookupTimestamp = strftime('%Y-%m-%d %H:%M:%S.000', 'now'),
        LookupAttempts = 100
WHERE   LookupTimestamp IS NULL;