SELECT ICAO AS before,
       (
         WITH RECURSIVE tok(rest, word) AS (
           SELECT TRIM(ICAO), ''
           UNION ALL
           SELECT LTRIM(SUBSTR(rest, INSTR(rest || ' ', ' ') + 1)),
                  SUBSTR(rest, 1, INSTR(rest || ' ', ' ') - 1)
           FROM tok
           WHERE rest <> ''
         )
         SELECT word FROM tok WHERE rest = '' LIMIT 1
       ) AS after
FROM MODEL
WHERE ICAO LIKE '% %';
