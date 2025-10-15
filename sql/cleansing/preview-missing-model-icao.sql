SELECT      m1.ICAO AS "1st-ICAO",
            m1.IATA AS "1st-IATA",
            m1.Name AS "1st-Name",
            m2.ICAO AS "2nd-ICAO",
            m2.IATA AS "2nd-IATA",
            m2.Name AS "2nd-Name"
FROM        MODEL m1
INNER JOIN  MODEL m2
            ON m2.IATA = m1.IATA
            AND LENGTH( m2.ICAO ) = 0
            AND m2.Id <> m1.Id
WHERE       LENGTH( m1.IATA ) > 0
AND         LENGTH( m1.ICAO ) > 0;
