/** Render a direct great-circle route on a low-detail Mercator world map. */
export async function renderRouteMap(elementId, route) {
    if (!window.Plotly) throw new Error("Plotly.js could not be loaded.");
    const element = document.getElementById(elementId);
    if (!element) return;

    const points = unwrapLongitudes(route.points ?? [], route.centreLongitude);
    const originLongitude = unwrapLongitude(route.origin.longitude, route.centreLongitude);
    const destinationLongitude = unwrapLongitude(route.destination.longitude, route.centreLongitude);
    const routeSegments = splitAtDateLine(points);
    const traces = routeSegments.map((segment, index) => ({
        type: "scattergeo",
        mode: "lines",
        lon: segment.map(point => point.longitude),
        lat: segment.map(point => point.latitude),
        line: {color: "#35d4d0", width: 3},
        hoverinfo: "skip",
        showlegend: false,
        name: index === 0 ? "Direct route" : undefined
    }));

    traces.push({
        type: "scattergeo",
        mode: "markers+text",
        lon: [originLongitude, destinationLongitude],
        lat: [route.origin.latitude, route.destination.latitude],
        text: [route.origin.iata, route.destination.iata],
        textposition: ["top right", "top left"],
        textfont: {color: "#eff6ff", size: 13},
        marker: {
            size: 11,
            color: ["#4fdaa1", "#ff7186"],
            line: {color: "#07111f", width: 2}
        },
        customdata: [
            [route.origin.name, route.origin.iata],
            [route.destination.name, route.destination.iata]
        ],
        hovertemplate: "<b>%{customdata[1]}</b><br>%{customdata[0]}<br>%{lat:.4f}°, %{lon:.4f}°<extra></extra>",
        showlegend: false
    });

    const frame = mercatorFrame(element, points);

    await window.Plotly.react(element, traces, {
        autosize: false,
        width: element.clientWidth,
        height: element.clientHeight,
        paper_bgcolor: "#0d1b2d",
        plot_bgcolor: "#0d1b2d",
        font: {color: "#b8cada"},
        margin: {l: 18, r: 18, t: 18, b: 18},
        geo: {
            domain: {x: [0, 1], y: [0, 1]},
            projection: {
                type: "mercator",
                scale: 1,
                rotation: {lon: route.centreLongitude}
            },
            lonaxis: {range: frame.longitudeRange},
            lataxis: {range: frame.latitudeRange},
            resolution: 110,
            showland: true,
            landcolor: "#182b3e",
            showocean: true,
            oceancolor: "#081524",
            showlakes: true,
            lakecolor: "#081524",
            showcountries: true,
            countrycolor: "#375069",
            countrywidth: 0.6,
            showcoastlines: true,
            coastlinecolor: "#58758f",
            coastlinewidth: 0.8,
            bgcolor: "#0d1b2d"
        },
        hovermode: "closest",
        dragmode: "pan"
    }, {
        responsive: true,
        displaylogo: false,
        scrollZoom: true
    });

    observeSize(element, points);
}

/** Prevent Plotly from drawing a line across the map edge for date-line routes. */
function splitAtDateLine(points) {
    if (!points.length) return [];
    const segments = [[points[0]]];
    for (let index = 1; index < points.length; index++) {
        const point = points[index];
        const previous = points[index - 1];
        if (Math.abs(point.longitude - previous.longitude) > 180) segments.push([]);
        segments.at(-1).push(point);
    }
    return segments.filter(segment => segment.length > 0);
}

/** Release Plotly event handlers when the Blazor component is removed. */
export function disposeRouteMap(elementId) {
    const element = document.getElementById(elementId);
    if (!element) return;
    routeMapObservers.get(element)?.disconnect();
    routeMapObservers.delete(element);
    if (window.Plotly) window.Plotly.purge(element);
}

const routeMapObservers = new WeakMap();

/** Keep the Plotly canvas fitted to the full rectangular map region. */
function observeSize(element, points) {
    routeMapObservers.get(element)?.disconnect();
    let previousWidth = element.clientWidth;
    let previousHeight = element.clientHeight;
    const observer = new ResizeObserver(entries => {
        const entry = entries[0];
        const width = Math.round(entry.contentRect.width);
        const height = Math.round(entry.contentRect.height);
        if (!width || !height || (width === previousWidth && height === previousHeight)) return;
        previousWidth = width;
        previousHeight = height;
        const frame = mercatorFrame(element, points, width, height);
        window.Plotly.relayout(element, {
            width,
            height,
            "geo.lonaxis.range": frame.longitudeRange,
            "geo.lataxis.range": frame.latitudeRange
        });
    });
    observer.observe(element);
    routeMapObservers.set(element, observer);
}

/** Build padded bounds whose projected aspect ratio matches the available rectangle. */
function mercatorFrame(element, points, width = element.clientWidth, height = element.clientHeight) {
    const longitudes = points.map(point => point.longitude);
    const latitudes = points.map(point => clampLatitude(point.latitude));
    let west = Math.min(...longitudes);
    let east = Math.max(...longitudes);
    let southY = mercatorY(Math.min(...latitudes));
    let northY = mercatorY(Math.max(...latitudes));

    const longitudePadding = Math.max(3, (east - west) * 0.12);
    const latitudePadding = Math.max(degreesToRadians(2), (northY - southY) * 0.14);
    west -= longitudePadding;
    east += longitudePadding;
    southY -= latitudePadding;
    northY += latitudePadding;

    const mapWidth = Math.max(1, width - 36);
    const mapHeight = Math.max(1, height - 36);
    const targetAspect = mapWidth / mapHeight;
    let longitudeSpanRadians = degreesToRadians(east - west);
    let mercatorSpan = northY - southY;
    const projectedAspect = longitudeSpanRadians / mercatorSpan;

    if (projectedAspect < targetAspect) {
        const expansion = (mercatorSpan * targetAspect - longitudeSpanRadians) / 2;
        west -= radiansToDegrees(expansion);
        east += radiansToDegrees(expansion);
    } else {
        const expansion = (longitudeSpanRadians / targetAspect - mercatorSpan) / 2;
        southY -= expansion;
        northY += expansion;
    }

    return {
        longitudeRange: [west, east],
        latitudeRange: [
            inverseMercatorY(southY),
            inverseMercatorY(northY)
        ]
    };
}

/** Move longitudes onto the continuous world copy centred on the plotted route. */
function unwrapLongitudes(points, centreLongitude) {
    return points.map(point => ({
        ...point,
        longitude: unwrapLongitude(point.longitude, centreLongitude)
    }));
}

function unwrapLongitude(longitude, centreLongitude) {
    let result = longitude;
    while (result - centreLongitude > 180) result -= 360;
    while (result - centreLongitude < -180) result += 360;
    return result;
}

function clampLatitude(latitude) {
    return Math.max(-85, Math.min(85, latitude));
}

function mercatorY(latitude) {
    const radians = degreesToRadians(clampLatitude(latitude));
    return Math.log(Math.tan(Math.PI / 4 + radians / 2));
}

function inverseMercatorY(value) {
    return clampLatitude(radiansToDegrees(2 * Math.atan(Math.exp(value)) - Math.PI / 2));
}

function degreesToRadians(value) {
    return value * Math.PI / 180;
}

function radiansToDegrees(value) {
    return value * 180 / Math.PI;
}
