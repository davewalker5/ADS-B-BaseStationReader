SELECT      al.Service,
            COUNT( al.Id ) AS "Calls"
FROM        API_LOG al
GROUP BY    al.Service
ORDER BY    al.Service;
