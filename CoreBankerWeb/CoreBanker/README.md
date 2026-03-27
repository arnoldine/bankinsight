# CoreBanker MudBlazor Frontend

`CoreBanker` is the MudBlazor replatform of BankInsight. It is a Blazor WebAssembly app with live integration across the operational banking modules, not just a page scaffold.

## Current scope

- authentication, MFA verification, session restore, token refresh, and logout
- permission-aware navigation and route-level authorization
- dashboard, customers, accounts, teller, transactions, and loans
- approvals, reporting, audit, security operations, and settings
- migration, BankingOS control, extensibility, and workspace views
- group lending, accounting, statements, end-of-day, treasury, vault, and operations risk

## Prerequisites

- .NET 10 SDK or later

## Local build

```sh
dotnet build "CoreBankerWeb/CoreBanker/CoreBanker.csproj" --configfile "CoreBankerWeb/NuGet.Config"
```

## Publish

```sh
dotnet publish "CoreBankerWeb/CoreBanker/CoreBanker.csproj" -c Release --configfile "CoreBankerWeb/NuGet.Config"
```

## Configuration

The frontend reads `ApiBaseUrl` from `wwwroot/appsettings.json`.

- use `"/"` when the frontend and API are served from the same origin
- use an absolute API URL when the frontend is hosted separately

See `CoreBankerWeb/DEPLOYMENT.md` for container and rollout guidance.

## Project structure

- `Pages/` operational workspaces
- `Layouts/` shell and login layouts
- `Components/Shared/` shared navigation and UI helpers
- `Services/` API clients and workflow orchestration
- `Auth/` session, MFA, and permission handling
- `State/` frontend session state

## Status

The project is now at functional parity with the React workspace across the main operational modules and is ready for deployment validation against the live BankInsight API.
