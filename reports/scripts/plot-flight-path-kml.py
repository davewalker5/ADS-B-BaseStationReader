#!/usr/bin/env python3
"""Plot an interactive 3D flight path directly from a KML file."""

from __future__ import annotations

import argparse
import io
import math
import os
from dataclasses import dataclass
from pathlib import Path
import xml.etree.ElementTree as ET

import numpy as np
from PIL import Image
import plotly.graph_objects as go
from plotly.subplots import make_subplots
import requests


EARTH_RADIUS_M = 6_371_000.0
METRES_PER_NAUTICAL_MILE = 1_852.0
KML_NAMESPACE = "http://www.opengis.net/kml/2.2"
GX_NAMESPACE = "http://www.google.com/kml/ext/2.2"


@dataclass
class Track:
    """One continuous path found in a KML geometry."""

    name: str
    longitude: np.ndarray
    latitude: np.ndarray
    altitude: np.ndarray
    timestamps: list[str]


def _coordinates(text: str | None) -> tuple[np.ndarray, np.ndarray, np.ndarray]:
    """Parse KML lon,lat[,alt] coordinate tuples (altitude is metres)."""
    points: list[tuple[float, float, float]] = []
    for token in (text or "").split():
        values = token.split(",")
        if len(values) < 2:
            continue
        points.append((float(values[0]), float(values[1]), float(values[2]) if len(values) > 2 else 0.0))
    if not points:
        return np.array([]), np.array([]), np.array([])
    values = np.asarray(points, dtype=float)
    return values[:, 0], values[:, 1], values[:, 2]


def load_kml(path: Path) -> list[Track]:
    """Load a flight path without requiring a database.

    Flightradar24 KML exports contain the same path twice: timestamped Point
    placemarks and separate two-point LineStrings. Prefer the Points when they
    are present so the result is one continuous, timestamped track rather than
    hundreds of duplicated fragments.
    """
    root = ET.parse(path).getroot()
    namespaces = {"kml": KML_NAMESPACE, "gx": GX_NAMESPACE}
    tracks: list[Track] = []

    point_tracks: list[Track] = []
    for folder_number, folder in enumerate(root.findall(".//kml:Folder", namespaces), 1):
        points: list[tuple[float, float, float, str]] = []
        for placemark in folder.findall("kml:Placemark", namespaces):
            point = placemark.find("kml:Point", namespaces)
            if point is None:
                continue
            longitude, latitude, altitude = _coordinates(
                point.findtext("kml:coordinates", namespaces=namespaces)
            )
            if not len(longitude):
                continue
            timestamp = placemark.findtext("kml:TimeStamp/kml:when", default="", namespaces=namespaces).strip()
            points.append((longitude[0], latitude[0], altitude[0], timestamp))

        if points:
            folder_name = folder.findtext("kml:name", default="", namespaces=namespaces).strip()
            values = np.asarray([point[:3] for point in points], dtype=float)
            timestamps = [point[3] for point in points]
            label = folder_name or f"Track {folder_number}"
            point_tracks.append(Track(label, values[:, 0], values[:, 1], values[:, 2], timestamps))

    if point_tracks:
        return point_tracks

    for placemark_number, placemark in enumerate(root.findall(".//kml:Placemark", namespaces), 1):
        name_node = placemark.find("kml:name", namespaces)
        name = (name_node.text or "").strip() if name_node is not None else ""
        name = name or f"Track {placemark_number}"

        for track_number, geometry in enumerate(placemark.findall(".//gx:Track", namespaces), 1):
            coordinate_nodes = geometry.findall("gx:coord", namespaces)
            points = [tuple(map(float, (node.text or "").split())) for node in coordinate_nodes if node.text]
            if not points:
                continue
            values = np.asarray(points, dtype=float)
            if values.shape[1] == 2:
                values = np.column_stack((values, np.zeros(len(values))))
            when = [(node.text or "").strip() for node in geometry.findall("kml:when", namespaces)]
            label = name if track_number == 1 else f"{name} ({track_number})"
            tracks.append(Track(label, values[:, 0], values[:, 1], values[:, 2], when))

        for line_number, geometry in enumerate(placemark.findall(".//kml:LineString", namespaces), 1):
            longitude, latitude, altitude = _coordinates(geometry.findtext("kml:coordinates", namespaces=namespaces))
            if len(longitude):
                label = name if line_number == 1 else f"{name} ({line_number})"
                tracks.append(Track(label, longitude, latitude, altitude, []))

    if not tracks:
        raise ValueError("The KML file contains no gx:Track or LineString coordinates")
    return tracks


def distances_from(latitude, longitude, reference_latitude, reference_longitude):
    """Return great-circle distances in metres from a reference location."""
    latitude = np.radians(np.asarray(latitude, dtype=float))
    longitude = np.radians(np.asarray(longitude, dtype=float))
    reference_latitude = math.radians(reference_latitude)
    reference_longitude = math.radians(reference_longitude)
    delta_latitude = latitude - reference_latitude
    delta_longitude = longitude - reference_longitude
    haversine = (
        np.sin(delta_latitude / 2) ** 2
        + math.cos(reference_latitude) * np.cos(latitude) * np.sin(delta_longitude / 2) ** 2
    )
    return 2 * EARTH_RADIUS_M * np.arcsin(np.minimum(1.0, np.sqrt(haversine)))


def filter_tracks_by_range(
    tracks: list[Track], latitude: float, longitude: float, maximum_range_nm: float
) -> list[Track]:
    """Keep points within the radius, splitting tracks around excluded runs."""
    maximum_range_m = maximum_range_nm * METRES_PER_NAUTICAL_MILE
    filtered: list[Track] = []

    for track in tracks:
        inside = distances_from(track.latitude, track.longitude, latitude, longitude) <= maximum_range_m
        retained_indices = np.flatnonzero(inside)
        if not len(retained_indices):
            continue

        # Split on gaps so excluded points never create a false connector across the plot.
        sections = np.split(retained_indices, np.flatnonzero(np.diff(retained_indices) > 1) + 1)
        for section_number, indices in enumerate(sections, 1):
            name = track.name if len(sections) == 1 else f"{track.name} (section {section_number})"
            timestamps = [track.timestamps[index] for index in indices] if len(track.timestamps) == len(track.latitude) else []
            filtered.append(
                Track(name, track.longitude[indices], track.latitude[indices], track.altitude[indices], timestamps)
            )

    if not filtered:
        raise ValueError(
            f"The KML file contains no points within {maximum_range_nm:g} nautical miles "
            f"of {latitude:g}, {longitude:g}"
        )
    return filtered


def coordinates_to_local_xy(latitude, longitude, reference_latitude, reference_longitude):
    latitude = np.asarray(latitude, dtype=float)
    longitude = np.asarray(longitude, dtype=float)
    reference_latitude_rad = math.radians(reference_latitude)
    x = (np.radians(longitude) - math.radians(reference_longitude)) * math.cos(reference_latitude_rad) * EARTH_RADIUS_M
    y = (np.radians(latitude) - reference_latitude_rad) * EARTH_RADIUS_M
    return x, y


def altitude_range(tracks: list[Track]) -> tuple[float, float]:
    altitude = np.concatenate([track.altitude for track in tracks])
    minimum, maximum = float(np.nanmin(altitude)), float(np.nanmax(altitude))
    padding = max((maximum - minimum) * 0.1, 250.0)
    return max(0.0, minimum - padding), maximum + padding


def bounding_box(tracks: list[Track], padding=0.06):
    latitude = np.concatenate([track.latitude for track in tracks])
    longitude = np.concatenate([track.longitude for track in tracks])
    latitude_pad = (np.ptp(latitude) or 1e-6) * padding
    longitude_pad = (np.ptp(longitude) or 1e-6) * padding
    return longitude.min() - longitude_pad, latitude.min() - latitude_pad, longitude.max() + longitude_pad, latitude.max() + latitude_pad


def fetch_mapbox(bbox, token: str) -> bytes:
    west, south, east, north = bbox
    url = f"https://api.mapbox.com/styles/v1/mapbox/streets-v12/static/[{west},{south},{east},{north}]/1024x1024@2x"
    response = requests.get(url, params={"access_token": token}, timeout=30)
    response.raise_for_status()
    return response.content


def add_map_floor(fig, image_bytes, x_min, x_max, y_min, y_max, z_floor, max_pixels=512):
    image = Image.open(io.BytesIO(image_bytes)).convert("RGB")
    scale = max(image.size) / max_pixels
    if scale > 1:
        image = image.resize(tuple(round(value / scale) for value in image.size), Image.Resampling.LANCZOS)
    rgb = np.asarray(image)
    height, width, _ = rgb.shape
    xx, yy = np.meshgrid(np.linspace(x_min, x_max, width), np.linspace(y_max, y_min, height))
    index = np.arange(height * width).reshape(height, width)
    i = np.column_stack((index[:-1, :-1].ravel(), index[:-1, :-1].ravel())).ravel()
    j = np.column_stack((index[1:, :-1].ravel(), index[1:, 1:].ravel())).ravel()
    k = np.column_stack((index[1:, 1:].ravel(), index[:-1, 1:].ravel())).ravel()
    colours = [f"rgb({red},{green},{blue})" for red, green, blue in rgb.reshape(-1, 3)]
    fig.add_trace(go.Mesh3d(x=xx.ravel(), y=yy.ravel(), z=np.full(xx.size, z_floor), i=i, j=j, k=k,
                            vertexcolor=colours, name="Map", flatshading=True, showscale=False, hoverinfo="skip"))


def build_figure(tracks: list[Track], title: str, mapbox_token: str = "") -> go.Figure:
    reference_latitude = float(tracks[0].latitude[0])
    reference_longitude = float(tracks[0].longitude[0])
    z_min, z_max = altitude_range(tracks)
    figure = go.Figure()
    all_x, all_y = [], []

    for number, track in enumerate(tracks):
        x, y = coordinates_to_local_xy(track.latitude, track.longitude, reference_latitude, reference_longitude)
        all_x.extend(x)
        all_y.extend(y)
        if len(track.altitude) > 1:
            ribbon_z = np.vstack((track.altitude, np.full(len(track.altitude), z_min)))
            figure.add_trace(go.Surface(
                x=np.vstack((x, x)), y=np.vstack((y, y)), z=ribbon_z, surfacecolor=ribbon_z,
                cmin=z_min, cmax=z_max, colorscale="Plasma", opacity=0.85, name=f"{track.name} ribbon",
                showscale=number == 0, colorbar={"title": "Altitude (m)", "x": 1.05},
                lighting={"ambient": 0.6, "diffuse": 0.8, "specular": 0.3, "roughness": 0.5},
            ))
        hover = None
        if len(track.timestamps) == len(track.altitude):
            hover = [f"{timestamp}<br>Altitude: {altitude:.0f} m" for timestamp, altitude in zip(track.timestamps, track.altitude)]
        figure.add_trace(go.Scatter3d(x=x, y=y, z=track.altitude, mode="lines", line={"width": 5},
                                      name=track.name, text=hover, hoverinfo="text" if hover else "x+y+z+name"))
        figure.add_trace(go.Scatter3d(x=x, y=y, z=np.full(len(x), z_min), mode="lines",
                                      line={"width": 2, "dash": "dash"}, name=f"{track.name} ground trace"))

    if mapbox_token:
        add_map_floor(figure, fetch_mapbox(bounding_box(tracks), mapbox_token), min(all_x), max(all_x), min(all_y), max(all_y), z_min)

    figure.update_layout(
        title={"text": title, "x": 0.5, "xanchor": "center"},
        legend={"x": 0.02, "y": 0.98, "bgcolor": "rgba(255,255,255,0.7)"},
        scene={
            # Match the database-backed notebook's final chart. A data-proportional scene makes a
            # low-altitude flight tens of kilometres long appear almost completely flat.
            "aspectmode": "cube", "xaxis_title": "East / West (m)", "yaxis_title": "North / South (m)",
            "zaxis": {"title": "Altitude (m)", "range": [z_min, z_max], "autorange": False},
        },
    )
    return figure


def add_info_strip(figure: go.Figure, source: Path, tracks: list[Track]) -> go.Figure:
    combined = make_subplots(rows=2, cols=1, specs=[[{"type": "scene"}], [{"type": "domain"}]],
                             row_heights=[0.88, 0.12], vertical_spacing=0.04)
    for trace in figure.data:
        combined.add_trace(trace, row=1, col=1)
    scene = figure.layout.scene.to_plotly_json()
    scene.pop("domain", None)
    combined.update_layout(scene=scene, title=figure.layout.title, legend=figure.layout.legend, height=820,
                           margin={"l": 60, "r": 60, "t": 70, "b": 20})
    points = sum(len(track.latitude) for track in tracks)
    altitude = np.concatenate([track.altitude for track in tracks])
    values = [[f"<b>Source</b>  {source.name}"], [f"<b>Tracks</b>  {len(tracks)}"],
              [f"<b>Points</b>  {points}"], [f"<b>Altitude</b>  {altitude.min():.0f}–{altitude.max():.0f} m"]]
    combined.add_trace(go.Table(header={"values": ["", "", "", ""], "height": 8, "line": {"width": 0}},
                                cells={"values": values, "align": "left", "height": 24}), row=2, col=1)
    return combined


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("-k", "--kml", type=Path, help="KML file containing gx:Track or LineString geometry")
    parser.add_argument("-o", "--output", type=Path, help="Output HTML path (default: beside the input KML)")
    parser.add_argument("-t", "--title", help="Plot title (default: derived from the KML filename)")
    parser.add_argument("-tk", "--mapbox-token", default=os.environ.get("MAPBOX_API_KEY", ""), help="Optional Mapbox API token")
    parser.add_argument("-la", "--latitude", type=float, help="Latitude of the range-filter centre, in decimal degrees")
    parser.add_argument("-lo", "--longitude", type=float, help="Longitude of the range-filter centre, in decimal degrees")
    parser.add_argument("-r", "--max-range-nm", type=float, help="Maximum distance from the filter centre, in nautical miles")
    parser.add_argument("-op", "--open", action="store_true", help="Open the plot in the default browser after writing it")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    range_arguments = (args.latitude, args.longitude, args.max_range_nm)
    if any(value is not None for value in range_arguments) and not all(value is not None for value in range_arguments):
        raise SystemExit("--latitude, --longitude and --max-range-nm must be supplied together")
    if args.latitude is not None and not -90 <= args.latitude <= 90:
        raise SystemExit("--latitude must be between -90 and 90")
    if args.longitude is not None and not -180 <= args.longitude <= 180:
        raise SystemExit("--longitude must be between -180 and 180")
    if args.max_range_nm is not None and args.max_range_nm < 0:
        raise SystemExit("--max-range-nm must not be negative")
    if args.kml is None:
        raise SystemExit("A KML file must be supplied with --kml")
    source = args.kml.expanduser().resolve()
    if not source.is_file():
        raise SystemExit(f"KML file not found: {source}")
    output = (args.output or source.with_suffix(".html")).expanduser().resolve()
    tracks = load_kml(source)
    if args.latitude is not None:
        tracks = filter_tracks_by_range(tracks, args.latitude, args.longitude, args.max_range_nm)
    figure = build_figure(tracks, args.title or f"Flight Path — {source.stem}", args.mapbox_token)
    final_figure = add_info_strip(figure, source, tracks)
    output.parent.mkdir(parents=True, exist_ok=True)
    final_figure.write_html(output, include_plotlyjs="cdn", auto_open=args.open)
    print(f"Plotted {sum(len(track.latitude) for track in tracks)} points from {len(tracks)} track(s): {output}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
