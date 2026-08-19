import importlib.util
from pathlib import Path
import sys

import numpy as np
from PIL import Image
import plotly.graph_objects as go


SCRIPT = Path(__file__).parents[1] / "scripts" / "plot-flight-path-kml.py"
SPEC = importlib.util.spec_from_file_location("plot_flight_path_kml", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
sys.modules[SPEC.name] = MODULE
SPEC.loader.exec_module(MODULE)


def test_loads_gx_track_and_linestring(tmp_path):
    kml = tmp_path / "flight.kml"
    kml.write_text(
        """<?xml version="1.0"?>
        <kml xmlns="http://www.opengis.net/kml/2.2" xmlns:gx="http://www.google.com/kml/ext/2.2"><Document>
          <Placemark><name>Recorded</name><gx:Track>
            <when>2026-01-01T12:00:00Z</when><when>2026-01-01T12:00:01Z</when>
            <gx:coord>-1.0 52.0 1000</gx:coord><gx:coord>-0.9 52.1 1100</gx:coord>
          </gx:Track></Placemark>
          <Placemark><name>Planned</name><LineString><coordinates>-0.8,52.2,1200 -0.7,52.3,1300</coordinates></LineString></Placemark>
        </Document></kml>""",
        encoding="utf-8",
    )

    tracks = MODULE.load_kml(kml)

    assert [track.name for track in tracks] == ["Recorded", "Planned"]
    np.testing.assert_allclose(tracks[0].altitude, [1000, 1100])
    assert tracks[0].timestamps == ["2026-01-01T12:00:00Z", "2026-01-01T12:00:01Z"]


def test_prefers_timestamped_points_over_duplicate_linestrings(tmp_path):
    kml = tmp_path / "flight.kml"
    kml.write_text(
        """<?xml version="1.0"?>
        <kml xmlns="http://www.opengis.net/kml/2.2"><Document>
          <Folder><name>Route</name>
            <Placemark><TimeStamp><when>2026-01-01T12:00:00Z</when></TimeStamp>
              <Point><altitudeMode>absolute</altitudeMode><coordinates>-1.0,52.0,300</coordinates></Point>
            </Placemark>
            <Placemark><TimeStamp><when>2026-01-01T12:00:01Z</when></TimeStamp>
              <Point><altitudeMode>absolute</altitudeMode><coordinates>-0.9,52.1,400</coordinates></Point>
            </Placemark>
          </Folder>
          <Folder><name>Track</name><Placemark><LineString>
            <coordinates>-1.0,52.0,300 -0.9,52.1,400</coordinates>
          </LineString></Placemark></Folder>
        </Document></kml>""",
        encoding="utf-8",
    )

    tracks = MODULE.load_kml(kml)

    assert len(tracks) == 1
    assert tracks[0].name == "Route"
    np.testing.assert_allclose(tracks[0].altitude, [300.0, 400.0])
    assert tracks[0].timestamps == ["2026-01-01T12:00:00Z", "2026-01-01T12:00:01Z"]


def test_altitude_range_matches_original_notebook_scaling():
    track = MODULE.Track(
        "Flight", np.array([0.0, 0.1]), np.array([0.0, 0.1]), np.array([297.18, 769.62]), []
    )

    np.testing.assert_allclose(MODULE.altitude_range([track]), (47.18, 1019.62))


def test_builds_three_plot_traces_per_track(tmp_path):
    track = MODULE.Track("Flight", np.array([-1.0, -0.9]), np.array([52.0, 52.1]), np.array([1000.0, 1100.0]), [])
    figure = MODULE.build_figure([track], "Test")
    assert len(figure.data) == 3
    assert figure.layout.scene.aspectmode == "cube"
    assert figure.layout.scene.zaxis.title.text == "Altitude (m)"


def test_range_filter_uses_nautical_miles_and_splits_excluded_sections():
    track = MODULE.Track(
        "Flight",
        np.array([0.0, 0.0, 0.0, 0.0, 0.0]),
        np.array([0.0, 0.01, 1.0, 0.01, 0.0]),
        np.array([100.0, 200.0, 300.0, 400.0, 500.0]),
        ["0", "1", "2", "3", "4"],
    )

    filtered = MODULE.filter_tracks_by_range([track], 0.0, 0.0, 1.0)

    assert len(filtered) == 2
    np.testing.assert_allclose(filtered[0].altitude, [100.0, 200.0])
    np.testing.assert_allclose(filtered[1].altitude, [400.0, 500.0])
    assert filtered[0].timestamps == ["0", "1"]
    assert filtered[1].timestamps == ["3", "4"]


def test_range_filter_includes_point_on_boundary():
    one_nautical_mile_in_degrees = np.degrees(MODULE.METRES_PER_NAUTICAL_MILE / MODULE.EARTH_RADIUS_M)
    track = MODULE.Track(
        "Flight", np.array([0.0]), np.array([one_nautical_mile_in_degrees]), np.array([100.0]), []
    )

    filtered = MODULE.filter_tracks_by_range([track], 0.0, 0.0, 1.0)

    assert len(filtered[0].latitude) == 1


def test_map_floor_retains_notebook_resolution(tmp_path):
    image_path = tmp_path / "map.png"
    Image.new("RGB", (1024, 1024), "green").save(image_path)
    figure = go.Figure()

    MODULE.add_map_floor(figure, image_path.read_bytes(), 0, 100, 0, 100, 0)

    assert len(figure.data[0].x) == 512 * 512
