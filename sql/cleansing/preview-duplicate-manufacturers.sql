SELECT      m.Name,
            COUNT( m.Id )
FROM        MANUFACTURER m
GROUP BY    m.Name
HAVING      COUNT( m.Id ) > 1;
