#!/usr/bin/env python3
"""Generate dark-themed aircraft flight-path contact sheets from a CSV file."""

from __future__ import annotations

import argparse
import csv
import math
import os
import sqlite3
import urllib.parse
import urllib.request
from contextlib import closing
from collections import defaultdict
from dataclasses import dataclass
from io import BytesIO
from pathlib import Path
from typing import Sequence

import matplotlib

# Select a non-interactive backend before importing pyplot so the command works headlessly.
matplotlib.use("Agg")

import matplotlib.pyplot as plt
import numpy as np
from matplotlib import colormaps, colors
from mpl_toolkits.mplot3d.art3d import Line3DCollection, Poly3DCollection
from PIL import Image


BACKGROUND_COLOUR = "#10151c"
CELL_COLOUR = "#ffffff"
EMPTY_CELL_COLOUR = "#18212b"
TEXT_COLOUR = "#e6edf3"
MUTED_TEXT_COLOUR = "#5f6b76"
PATH_COLOUR_MAP = "plasma"
EARTH_RADIUS_METRES = 6_371_000.0


@dataclass(frozen=True)
class AircraftRequest:
    """Identify one aircraft flight path to include in a contact sheet."""

    address: str
    session_id: int


@dataclass(frozen=True)
class FlightPath:
    """Hold the display name and geographic points for one observed aircraft."""

    address: str
    callsign: str
    latitudes: np.ndarray
    longitudes: np.ndarray
    altitudes: np.ndarray


def positive_integer(value: str) -> int:
    """Parse a command-line value that must be a positive integer.

    :param value: Text supplied on the command line.
    :return: The parsed positive integer.
    """
    # Use argparse's own error type so invalid layout options produce concise usage help.
    try:
        result = int(value)
    except ValueError as error:
        raise argparse.ArgumentTypeError("must be a positive integer") from error
    if result < 1:
        raise argparse.ArgumentTypeError("must be a positive integer")
    return result


def parse_arguments(arguments: Sequence[str] | None = None) -> argparse.Namespace:
    """Parse contact-sheet command-line arguments.

    :param arguments: Optional argument sequence; defaults to the process arguments.
    :return: Parsed command-line arguments.
    """
    # Keep project-specific defaults here so they are visible in --help output.
    project_root = Path(__file__).resolve().parent.parent.parent
    default_output = project_root / "data" / "reports" / "contact-sheets"

    parser = argparse.ArgumentParser()
    parser.add_argument("-i", "--input", type=Path, help="CSV file with Address,Session ID columns")
    parser.add_argument("-o", "--orientation", choices=("portrait", "landscape"), default="portrait",
                        help="sheet orientation")
    parser.add_argument("-r", "--rows", type=positive_integer, default=5, help="rows per sheet")
    parser.add_argument("-c", "--columns", type=positive_integer, default=4, help="columns per sheet")
    parser.add_argument("-d", "--database", type=Path, default=None, help="SQLite tracker database")
    parser.add_argument("-od", "--output-directory", type=Path, default=default_output,
                        help="PNG destination directory")
    parser.add_argument("-t", "--token", default=None, help="Mapbox token")
    return parser.parse_args(arguments)


def read_requests(csv_path: Path) -> list[AircraftRequest]:
    """Read and validate aircraft requests from a CSV file.

    :param csv_path: CSV file containing Address and Session ID columns.
    :return: Validated aircraft requests in input order.
    """
    # utf-8-sig transparently accepts files exported with or without a byte-order mark.
    requests: list[AircraftRequest] = []
    with csv_path.open("r", encoding="utf-8-sig", newline="") as csv_file:
        reader = csv.DictReader(csv_file)
        if reader.fieldnames != ["Address", "Session ID"]:
            raise ValueError("expected CSV header: Address,Session ID")

        for line_number, row in enumerate(reader, start=2):
            address = (row.get("Address") or "").strip().upper()
            session_text = (row.get("Session ID") or "").strip()
            if not address and not session_text:
                continue
            if len(address) != 6 or any(character not in "0123456789ABCDEF" for character in address):
                raise ValueError(f"invalid aircraft address on row {line_number}: {address}")
            try:
                session_id = int(session_text)
            except ValueError as error:
                raise ValueError(f"invalid session ID on row {line_number}: {session_text}") from error
            if session_id < 1 or session_text != str(session_id):
                raise ValueError(f"invalid session ID on row {line_number}: {session_text}")
            requests.append(AircraftRequest(address, session_id))

    if not requests:
        raise ValueError("the CSV file contains no aircraft requests")
    return requests


def resolve_database_path(argument_path: Path | None) -> Path:
    """Resolve and validate the tracker database path.

    :param argument_path: Optional path supplied explicitly on the command line.
    :return: Existing SQLite database path.
    """
    # An explicit argument takes precedence over the reporting suite environment variable.
    configured_path = argument_path or (Path(os.environ["AIRCRAFT_TRACKER_DB"]) if os.environ.get("AIRCRAFT_TRACKER_DB") else None)
    if configured_path is None:
        raise ValueError("set AIRCRAFT_TRACKER_DB or supply --database")
    if not configured_path.is_file():
        raise ValueError(f"database file not found: {configured_path}")
    return configured_path


def load_flight_path(connection: sqlite3.Connection, request: AircraftRequest) -> FlightPath:
    """Load one aircraft's recorded positions and callsign.

    :param connection: Open tracker SQLite connection.
    :param request: Aircraft address and observation session to query.
    :return: Flight path ready to plot; arrays are empty when no positions exist.
    """
    # Parameter binding prevents an address or session value from changing query semantics.
    rows = connection.execute(
        """
        SELECT ta.Callsign, p.Latitude, p.Longitude, p.Altitude
        FROM TRACKED_AIRCRAFT AS ta
        LEFT OUTER JOIN POSITION AS p ON p.AircraftId = ta.Id
        WHERE ta.SessionId = ? AND UPPER(ta.Address) = ?
        ORDER BY p.Timestamp ASC
        """,
        (request.session_id, request.address),
    ).fetchall()

    # Choose the first observed non-empty callsign and independently retain valid coordinates.
    callsign = next(
        (str(row[0]).strip().upper() for row in rows if row[0] is not None and str(row[0]).strip()),
        "NONE",
    )
    valid_positions: list[tuple[float, float, float]] = []
    for row in rows:
        try:
            latitude, longitude = float(row[1]), float(row[2])
            altitude = (float(row[3]) * 0.3048) if row[3] is not None else 0.0
        except (TypeError, ValueError):
            continue
        if math.isfinite(latitude) and math.isfinite(longitude) and math.isfinite(altitude):
            valid_positions.append((latitude, longitude, altitude))

    positions = np.asarray(valid_positions, dtype=float)
    if positions.size == 0:
        positions = np.empty((0, 3), dtype=float)
    return FlightPath(
        request.address,
        callsign,
        positions[:, 0],
        positions[:, 1],
        positions[:, 2],
    )


def coordinates_to_local_xy(flight_path: FlightPath) -> tuple[np.ndarray, np.ndarray]:
    """Project latitude and longitude to local metre coordinates.

    :param flight_path: Flight path containing geographic coordinates.
    :return: Easting and northing arrays measured from the first position.
    """
    # The local equirectangular projection is accurate enough for receiver-range flight paths.
    latitude_radians = np.radians(flight_path.latitudes)
    longitude_radians = np.radians(flight_path.longitudes)
    reference_latitude = latitude_radians[0]
    reference_longitude = longitude_radians[0]
    x = (longitude_radians - reference_longitude) * math.cos(reference_latitude) * EARTH_RADIUS_METRES
    y = (latitude_radians - reference_latitude) * EARTH_RADIUS_METRES
    return x, y


def padded_range(values: np.ndarray, padding_ratio: float = 0.06) -> tuple[float, float]:
    """Calculate a non-zero plotting range with proportional padding.

    :param values: Coordinate values to encompass.
    :param padding_ratio: Fractional padding applied at both ends.
    :return: Padded minimum and maximum values.
    """
    # A fixed minimum span keeps vertical and single-point paths visible.
    minimum = float(np.min(values))
    maximum = float(np.max(values))
    span = maximum - minimum
    padding = max(span * padding_ratio, 25.0)
    return minimum - padding, maximum + padding


def fetch_ground_map(flight_path: FlightPath, token: str) -> np.ndarray:
    """Download a Mapbox static map covering a flight path.

    :param flight_path: Geographic flight path used to calculate map bounds.
    :param token: Mapbox API access token.
    :return: RGB image array with its origin at the north-west corner.
    """
    # Pad the geographic bounds so the track is not tight against the map edge.
    latitude_minimum = float(np.min(flight_path.latitudes))
    latitude_maximum = float(np.max(flight_path.latitudes))
    longitude_minimum = float(np.min(flight_path.longitudes))
    longitude_maximum = float(np.max(flight_path.longitudes))
    latitude_padding = max((latitude_maximum - latitude_minimum) * 0.06, 0.001)
    longitude_padding = max((longitude_maximum - longitude_minimum) * 0.06, 0.001)
    south, north = latitude_minimum - latitude_padding, latitude_maximum + latitude_padding
    west, east = longitude_minimum - longitude_padding, longitude_maximum + longitude_padding
    bounds = f"[{west},{south},{east},{north}]"
    quoted_token = urllib.parse.quote(token, safe="")
    url = (
        "https://api.mapbox.com/styles/v1/mapbox/outdoors-v12/static/"
        f"{bounds}/512x512?access_token={quoted_token}"
    )
    with urllib.request.urlopen(url, timeout=30) as response:
        image = Image.open(BytesIO(response.read())).convert("RGB")
    # Downsample because a dense surface in every contact-sheet cell is unnecessarily expensive.
    image.thumbnail((64, 64), Image.Resampling.LANCZOS)
    return np.asarray(image, dtype=float) / 255.0


def draw_ground_plane(
    axis: plt.Axes,
    x_range: tuple[float, float],
    y_range: tuple[float, float],
    z_floor: float,
    map_image: np.ndarray | None,
) -> None:
    """Draw either a textured map or a plain dark 3D ground plane.

    :param axis: Three-dimensional Matplotlib axis representing the cell.
    :param x_range: Minimum and maximum local eastings.
    :param y_range: Minimum and maximum local northings.
    :param z_floor: Altitude of the ground surface.
    :param map_image: Optional RGB Mapbox image.
    :return: None.
    """
    if map_image is None:
        # A two-by-two grid is sufficient for an untextured rectangular surface.
        x_grid, y_grid = np.meshgrid(x_range, y_range)
        axis.plot_surface(
            x_grid,
            y_grid,
            np.full_like(x_grid, z_floor),
            color="#e8edf2",
            shade=False,
            zorder=0,
        )
        return

    # Image rows run north to south, hence the reversed Y coordinates.
    height, width, _ = map_image.shape
    x_values = np.linspace(x_range[0], x_range[1], width)
    y_values = np.linspace(y_range[1], y_range[0], height)
    x_grid, y_grid = np.meshgrid(x_values, y_values)
    axis.plot_surface(
        x_grid,
        y_grid,
        np.full_like(x_grid, z_floor),
        facecolors=map_image,
        rstride=1,
        cstride=1,
        shade=False,
        antialiased=False,
        zorder=0,
    )


def plot_flight_path(axis: plt.Axes, flight_path: FlightPath, mapbox_token: str | None = None) -> None:
    """Draw one flight path and its caption into a contact-sheet cell.

    :param axis: Matplotlib axis representing the cell.
    :param flight_path: Flight path and caption values to draw.
    :param mapbox_token: Optional token used to texture the ground with a Mapbox map.
    :return: None.
    """
    # Every axis gets an explicit face colour and transparent 3D panes for the dark theme.
    axis.set_facecolor(CELL_COLOUR)
    # Matplotlib otherwise depth-sorts whole 3D artists and can paint the map over the path.
    axis.computed_zorder = False
    axis.set_xticks([])
    axis.set_yticks([])
    axis.set_zticks([])
    axis.grid(False)
    axis.xaxis.pane.fill = False
    axis.yaxis.pane.fill = False
    axis.zaxis.pane.fill = False
    axis.xaxis.pane.set_edgecolor((0, 0, 0, 0))
    axis.yaxis.pane.set_edgecolor((0, 0, 0, 0))
    axis.zaxis.pane.set_edgecolor((0, 0, 0, 0))

    if flight_path.latitudes.size == 0:
        axis.text2D(
            0.5,
            0.5,
            "NO POSITION DATA",
            color=MUTED_TEXT_COLOUR,
            fontsize=7,
            ha="center",
            va="center",
            transform=axis.transAxes,
        )
    else:
        # Project geographic positions and establish a ground floor below the lowest recorded altitude.
        x, y = coordinates_to_local_xy(flight_path)
        z = flight_path.altitudes
        x_range = padded_range(x)
        y_range = padded_range(y)
        altitude_span = max(float(np.ptp(z)), 250.0)
        z_floor = max(0.0, float(np.min(z)) - max(altitude_span * 0.10, 250.0))
        z_ceiling = float(np.max(z)) + max(altitude_span * 0.10, 250.0)

        # Map retrieval is best-effort: report generation remains useful if Mapbox is unavailable.
        map_image = None
        if mapbox_token:
            try:
                map_image = fetch_ground_map(flight_path, mapbox_token)
            except (OSError, ValueError):
                map_image = None
        draw_ground_plane(axis, x_range, y_range, z_floor, map_image)

        if len(x) == 1:
            axis.scatter(x, y, z, color="#f0f921", s=10, zorder=4)
        else:
            # Draw an altitude-coloured path, its ground trace, and a translucent vertical ribbon.
            points = np.column_stack((x, y, z))
            segments = np.stack((points[:-1], points[1:]), axis=1)
            normaliser = colors.Normalize(vmin=float(np.min(z)), vmax=max(float(np.max(z)), float(np.min(z)) + 1.0))
            collection = Line3DCollection(
                segments,
                cmap=PATH_COLOUR_MAP,
                norm=normaliser,
                linewidth=1.6,
                zorder=4,
            )
            collection.set_array((z[:-1] + z[1:]) / 2.0)
            axis.add_collection3d(collection)
            # Lift the ground trace slightly to prevent z-fighting with the map surface.
            ground_trace_z = z_floor + max((z_ceiling - z_floor) * 0.002, 1.0)
            axis.plot(
                x,
                y,
                np.full_like(z, ground_trace_z),
                color="#56616c",
                linewidth=0.7,
                alpha=0.85,
                zorder=3,
            )
            ribbon_colours = colormaps[PATH_COLOUR_MAP](normaliser((z[:-1] + z[1:]) / 2.0))
            # Match the notebook ribbon's rich, largely opaque appearance.
            ribbon_colours[:, 3] = 0.85
            ribbon_faces = [
                [(x[i], y[i], z_floor), (x[i], y[i], z[i]), (x[i + 1], y[i + 1], z[i + 1]), (x[i + 1], y[i + 1], z_floor)]
                for i in range(len(x) - 1)
            ]
            axis.add_collection3d(
                Poly3DCollection(
                    ribbon_faces,
                    facecolors=ribbon_colours,
                    edgecolors="none",
                    zorder=2,
                )
            )

        axis.set_xlim(x_range)
        axis.set_ylim(y_range)
        axis.set_zlim(z_floor, z_ceiling)
        axis.set_box_aspect((1.0, 1.0, 0.65))
        # Match Plotly's notebook-style default by viewing from the positive X/Y quadrant.
        axis.view_init(elev=28, azim=45)
    axis.set_title(
        f"{flight_path.address.upper()}-{flight_path.callsign.upper() or 'NONE'}",
        color=TEXT_COLOUR,
        fontsize=8,
        pad=5,
        y=-0.16,
    )


def render_page(
    flight_paths: Sequence[FlightPath],
    session_id: int,
    page_number: int,
    rows: int,
    columns: int,
    orientation: str,
    output_directory: Path,
    mapbox_token: str | None = None,
) -> Path:
    """Render one page of flight paths to a PNG file.

    :param flight_paths: Flight paths assigned to this page.
    :param session_id: Observation session represented by the page.
    :param page_number: One-based page number.
    :param rows: Number of cell rows on the page.
    :param columns: Number of cell columns on the page.
    :param orientation: Portrait or landscape page orientation.
    :param output_directory: Directory in which to create the PNG.
    :param mapbox_token: Optional token used to texture chart ground planes.
    :return: Path of the generated PNG file.
    """
    # A4 proportions provide a predictable sheet shape while PNG keeps output device-independent.
    figure_size = (8.27, 11.69) if orientation == "portrait" else (11.69, 8.27)
    figure, axes = plt.subplots(
        rows,
        columns,
        figsize=figure_size,
        squeeze=False,
        facecolor=BACKGROUND_COLOUR,
        subplot_kw={"projection": "3d"},
    )
    # Leave a stronger bottom border while keeping captions visually connected to the next row.
    figure.subplots_adjust(left=0.025, right=0.975, top=0.975, bottom=0.050, wspace=0.10, hspace=0.22)

    for index, axis in enumerate(axes.flat):
        if index < len(flight_paths):
            plot_flight_path(axis, flight_paths[index], mapbox_token)
        else:
            # Empty cells remain visibly part of the dark contact-sheet grid.
            axis.set_facecolor(EMPTY_CELL_COLOUR)
            axis.set_xticks([])
            axis.set_yticks([])
            axis.set_zticks([])
            axis.set_axis_off()

    output_directory.mkdir(parents=True, exist_ok=True)
    output_path = output_directory / f"contact-sheet-{session_id}-{page_number}.png"
    figure.savefig(output_path, dpi=200, facecolor=figure.get_facecolor())
    plt.close(figure)
    return output_path


def generate_contact_sheets(
    requests: Sequence[AircraftRequest],
    database_path: Path,
    output_directory: Path,
    orientation: str = "portrait",
    rows: int = 6,
    columns: int = 4,
    mapbox_token: str | None = None,
) -> list[Path]:
    """Generate all requested contact-sheet pages, grouped by session.

    :param requests: Aircraft requests in desired display order.
    :param database_path: Tracker SQLite database path.
    :param output_directory: Directory in which to write generated images.
    :param orientation: Portrait or landscape sheet orientation.
    :param rows: Number of rows per page.
    :param columns: Number of columns per page.
    :param mapbox_token: Optional token used to texture chart ground planes.
    :return: Generated PNG paths in session and page order.
    """
    # Preserve both the CSV's session order and aircraft order within each session.
    grouped_requests: dict[int, list[AircraftRequest]] = defaultdict(list)
    for request in requests:
        grouped_requests[request.session_id].append(request)

    generated_paths: list[Path] = []
    cells_per_page = rows * columns
    # closing is required because sqlite's context manager controls transactions but not connection lifetime.
    with closing(sqlite3.connect(f"file:{database_path}?mode=ro", uri=True)) as connection:
        for session_id, session_requests in grouped_requests.items():
            flight_paths = [load_flight_path(connection, request) for request in session_requests]
            for start in range(0, len(flight_paths), cells_per_page):
                page_number = start // cells_per_page + 1
                generated_paths.append(
                    render_page(
                        flight_paths[start : start + cells_per_page],
                        session_id,
                        page_number,
                        rows,
                        columns,
                        orientation,
                        output_directory,
                        mapbox_token,
                    )
                )
    return generated_paths


def main(arguments: Sequence[str] | None = None) -> int:
    """Run the contact-sheet command.

    :param arguments: Optional argument sequence; defaults to the process arguments.
    :return: Process exit status.
    """
    # Convert expected input problems into a short command-line error without hiding programming faults.
    parsed = parse_arguments(arguments)
    try:
        requests = read_requests(parsed.input)
        database_path = resolve_database_path(parsed.database)
        paths = generate_contact_sheets(
            requests,
            database_path,
            parsed.output_directory,
            parsed.orientation,
            parsed.rows,
            parsed.columns,
            parsed.token or os.environ.get("MAPBOX_API_KEY"),
        )
    except (OSError, ValueError, sqlite3.Error) as error:
        raise SystemExit(f"Error: {error}") from error

    for path in paths:
        print(path)
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
