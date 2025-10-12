SELECT      COUNT( of.Id ), 'Flights' AS "Matches"
FROM        OBSERVED_FLIGHT of
INNER JOIN  FLIGHT_NUMBER_MAPPING fnm ON fnm.FlightIATA = of.Number
UNION ALL
SELECT      COUNT( of.Id ), 'Callsigns' AS "Matches"
FROM        OBSERVED_FLIGHT of
INNER JOIN  FLIGHT_NUMBER_MAPPING fnm ON fnm.Callsign = of.Number;
