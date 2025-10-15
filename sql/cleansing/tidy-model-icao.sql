UPDATE MODEL
SET ICAO = (
  WITH RECURSIVE tok(rest, word) AS (
    SELECT TRIM(ICAO), ''
    UNION ALL
    SELECT LTRIM(SUBSTR(rest, INSTR(rest || ' ', ' ') + 1)),
           SUBSTR(rest, 1, INSTR(rest || ' ', ' ') - 1)
    FROM tok
    WHERE rest <> ''
  )
  SELECT word
  FROM tok
  WHERE rest = ''         -- when there's nothing left, 'word' is the last token
  LIMIT 1
)
WHERE ICAO LIKE '% %';