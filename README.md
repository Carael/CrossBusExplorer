# Cross Bus Explorer

Cross Bus Explorer is a lightweight, open-source Azure Service Bus explorer for
Windows, macOS, and Linux.

The current desktop application uses React and Tauri for the native UI while the
stable Service Bus implementation remains in C#. Tauri starts the C# host as a
private loopback sidecar and proxies only the application's `/api/v1` routes.

## Features

- Connection profiles using Azure CLI (`az login`), Default Azure Credential,
  workload identity, interactive browser login, or a SAS connection string
- Lazy queue, topic, and subscription explorer with message counts
- Create, clone, edit, enable/disable, and delete queues, topics, and subscriptions
- Create, edit, and delete subscription SQL, correlation, true, and false rules
- Peek or receive messages by count, sequence number, or until the source is empty
- Inspect, edit/copy, send, requeue, import, and delete messages
- Purge and dead-letter resend operations with cancellable background progress
- Dark and light themes with responsive native-window layouts

Connection metadata and connection-string secrets are stored separately in the
platform application-data directory. Desktop files are owner-only on macOS and
Linux, and connection strings are never returned to the React client.

## Authentication

For the recommended Azure CLI flow:

```bash
az login
```

Then add a connection using the namespace host, for example
`orders.servicebus.windows.net`. The signed-in identity needs the Azure Service
Bus data roles required by the operation. Managing entities requires an owner role;
message-only roles are sufficient only for the corresponding send/receive calls.

Workload identity profiles require a tenant ID, client ID, and federated token file.
Default Azure Credential is useful in configured development, managed identity, and
CI environments.

## Development

Prerequisites:

- .NET 10 SDK
- Node.js 22.12 or newer
- Rust stable and the platform-specific [Tauri prerequisites](https://v2.tauri.app/start/prerequisites/)

Run the React UI against Vite:

```bash
cd src/Ui/Desktop
npm ci
npm run build
```

Run the native application (publishes the C# sidecar first):

```bash
cd src/Ui/Desktop
npm run tauri:dev
```

Create an optimized native package:

```bash
cd src/Ui/Desktop
npm run tauri:build
```

Run backend regression tests:

```bash
dotnet test tests/CrossBusExplorer.Tests/CrossBusExplorer.Tests.csproj
```

## Unsigned local builds

Local packages are not code-signed. macOS may require **Open Anyway** under
Privacy & Security, and Windows may show a SmartScreen warning. Official releases
should add platform signing and notarization credentials in CI.
