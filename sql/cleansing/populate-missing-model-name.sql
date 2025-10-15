SELECT      'UPDATE MODEL SET Name = ''' || m1.Name || ''' WHERE Id = ' || m2.Id || ';'
FROM        MODEL m1
INNER JOIN  MODEL m2
            ON ((m2.IATA = m1.IATA) OR (m2.ICAO = m1.ICAO))
            AND LENGTH( m2.Name ) = 0
            AND m2.Id <> m1.Id
WHERE       LENGTH( m1.IATA ) > 0
AND         LENGTH( m1.ICAO ) > 0
AND         LENGTH( m1.Name ) > 0;
