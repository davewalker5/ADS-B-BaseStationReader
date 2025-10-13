SELECT      COUNT( of.FlightIATA ), 'Flights' AS "Matches"
FROM        OBSERVED_FLIGHT of
INNER JOIN  FLIGHT_NUMBER_MAPPING fnm ON fnm.FlightIATA = of.FlightIATA
UNION ALL
SELECT      COUNT( of.FlightIATA ), 'Callsigns' AS "Matches"
FROM        OBSERVED_FLIGHT of
INNER JOIN  FLIGHT_NUMBER_MAPPING fnm ON fnm.Callsign = of.FlightIATA;
