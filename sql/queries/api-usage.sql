SELECT      al.Service,
            al.Endpoint,
            al.Property,
            COUNT( al.Id ) AS "Calls"
FROM        API_LOG al
GROUP BY    al.Service,
            al.Endpoint,
            al.Property
ORDER BY    al.Service,
            al.Endpoint,
            al.Property;
