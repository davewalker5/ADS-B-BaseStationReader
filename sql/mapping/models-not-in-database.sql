WITH input_codes(icao_code) AS (
    VALUES
        ( '' ),
        ( '' ),
)
SELECT i.icao_code
FROM input_codes AS i
WHERE NOT EXISTS (
    SELECT 1
    FROM MODEL AS m
    WHERE UPPER(TRIM(m.ICAO)) = UPPER(TRIM(i.icao_code))
);