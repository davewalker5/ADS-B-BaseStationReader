DROP TABLE IF EXISTS OBSERVED_FLIGHT;

CREATE TABLE OBSERVED_FLIGHT (
    AirlineName     TEXT    NOT NULL,
    FlightIATA      TEXT    NOT NULL,
    Embarkation     TEXT    NOT NULL,
    Destination     TEXT    NOT NULL );
