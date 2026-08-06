# SignalR Test Client

This directory contains a small Node.js client for testing a running Tracker Hub SignalR endpoint independently of the Integrated UI.

The client:

- Connects to the URL supplied on the command line
- Enables informational SignalR logging
- Displays the connection ID when the connection succeeds
- Writes received `ReceiveMessage` and `AircraftUpdated` events to the console
- Attempts to reconnect automatically if the connection is interrupted
- Stops the SignalR connection cleanly when **Ctrl+C** is pressed

It is intended as a lightweight development and diagnostic tool. It does not provide a user interface or store any of the data it receives.

## Prerequisites

Install a current supported version of [Node.js](https://nodejs.org/). The installation must include `npm`.

## Installing the dependencies

From the repository root, change to the `signalr-test` directory and install the dependencies recorded in `package-lock.json`:

```bash
cd signalr-test
npm ci
```

`npm ci` provides a reproducible installation and is recommended when using the checked-in lock file. Use `npm install` instead if you intentionally need to update the dependency lock.

## Running the client

Start Tracker Hub or another compatible SignalR server, then run:

```bash
node testTrackerHub.mjs http://<host>:<port>
```

Replace `<host>` and `<port>` with the address on which the SignalR endpoint is listening. For example:

```bash
node testTrackerHub.mjs http://localhost:5000
```

The URL must use the protocol expected by the server. Use `https://` when the endpoint is exposed over HTTPS.

After a successful connection, the client prints `Connected`, its SignalR connection ID, and any supported events it receives. Press **Ctrl+C** to disconnect and exit.

If the initial connection cannot be established, the client prints the connection error and exits with a non-zero status.
