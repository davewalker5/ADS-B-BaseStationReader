SELECT      al.Service,
            al.Endpoint,
            COUNT( al.Id ) AS "Calls"
FROM        API_LOG al
GROUP BY    al.Service,
            al.Endpoint
ORDER BY    al.Service,
            al.Endpoint;
