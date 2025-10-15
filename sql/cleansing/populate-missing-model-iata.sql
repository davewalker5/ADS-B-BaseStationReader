SELECT      'UPDATE MODEL SET IATA = ''' || m1.IATA || ''' WHERE Id = ' || m2.Id || ';'
FROM        MODEL m1
INNER JOIN  MODEL m2
            ON m2.ICAO = m1.ICAO
            AND LENGTH( m2.IATA ) = 0
            AND m2.Id <> m1.Id
WHERE       LENGTH( m1.ICAO ) > 0
AND         LENGTH( m1.IATA ) > 0;
