SELECT  a.*
FROM    AIRLINE a
WHERE   a.IATA <> UPPER( a. IATA )
OR      a.IATA <> TRIM( a. IATA )
OR      (a.IATA <> '' AND LENGTH( a.IATA ) <> 2)
OR      a.ICAO <> UPPER( a. ICAO )
OR      a.ICAO <> TRIM( a. ICAO )
OR      (a.ICAO <> '' AND LENGTH( a.ICAO ) <> 3)
OR      LENGTH( a.Name ) == 0;