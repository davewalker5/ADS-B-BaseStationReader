SELECT DISTINCT s.Id AS "SessionId",
                s.Name AS "Session",
                a.Address,
                a.Registration,
                m.Name AS "Model",
                m.ICAO,
                ma.Name AS "Manufacturer",
                a.Manufactured,
                CAST(strftime('%Y', 'now') AS INTEGER) - a.Manufactured AS "Age"
FROM            SESSION s
INNER JOIN      TRACKED_AIRCRAFT ta ON ta.SessionId = s.Id
INNER JOIN      AIRCRAFT a ON a.Address = ta.Address
INNER JOIN      MODEL m ON m.Id = a.ModelId
INNER JOIN      MANUFACTURER ma ON ma.Id = m.ManufacturerId
WHERE           ( CAST(strftime('%Y', 'now') AS INTEGER) - a.Manufactured ) > 60;