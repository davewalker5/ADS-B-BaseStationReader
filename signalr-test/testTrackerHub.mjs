import * as signalR from "@microsoft/signalr";

const hubUrl = process.argv[2];

if (!hubUrl) {
    console.error("Usage: node test.mjs <hub-url>");
    process.exit(1);
}

const connection = new signalR.HubConnectionBuilder()
    .withUrl(hubUrl)
    .configureLogging(signalR.LogLevel.Information)
    .withAutomaticReconnect()
    .build();

// Replace these with the client method names used by your hub.
connection.on("ReceiveMessage", (...args) => {
    console.log("ReceiveMessage:", ...args);
});

connection.on("AircraftUpdated", (...args) => {
    console.log("AircraftUpdated:", ...args);
});

connection.onclose(error => {
    console.error("Connection closed:", error ?? "no error");
});

try {
    await connection.start();

    console.log("Connected");
    console.log("Connection ID:", connection.connectionId);
    console.log("Press Ctrl+C to disconnect");

    process.on("SIGINT", async () => {
        await connection.stop();
        process.exit(0);
    });
} catch (error) {
    console.error("Connection failed:", error);
    process.exit(1);
}