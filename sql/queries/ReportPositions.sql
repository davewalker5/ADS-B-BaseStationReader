PRAGMA journal_mode=WAL;

SELECT a.Address, a.Callsign, a.Squawk, a.GroundSpeed, a.Track, a.VerticalRate, p.Altitude, p.Latitude, p.Longitude
FROM AIRCRAFT a
INNER JOIN AIRCRAFT_POSITION p on p.AircraftId = a.Id
WHERE a.Address = '';
