SELECT      m.ICAO,
            m.IATA,
            COUNT( m.Id )
FROM        MODEL m
GROUP BY    m.ICAO,
            m.IATA
HAVING      COUNT( m.Id ) > 1;