SELECT  fnm.*
FROM    FLIGHT_NUMBER_MAPPING fnm
WHERE   NOT ( fnm.Callsign GLOB '[A-Za-z0-9]*' )
OR      NOT ( fnm.FlightIATA GLOB '[A-Za-z0-9]*' );
