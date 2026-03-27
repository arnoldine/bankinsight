# CoreBanker Deployment Guide

## Runtime model

`CoreBanker` is a Blazor WebAssembly application. It is built into static files and must be hosted behind a web server with SPA fallback so every application route resolves to `index.html`.

## API configuration

The frontend reads `ApiBaseUrl` from `wwwroot/appsettings.json`.

- Local/default: `"/"` for same-origin proxy deployments
- Remote API: set an absolute URL such as `https://bankinsight.rproxyserv.net/`

For container builds, set the Docker build arg:

```sh
docker build -t corebanker-web --build-arg API_BASE_URL=https://bankinsight.rproxyserv.net/ .
```

## Container deployment

From `CoreBankerWeb/`:

```sh
docker build -t corebanker-web --build-arg API_BASE_URL=https://bankinsight.rproxyserv.net/ .
docker run -p 8080:80 corebanker-web
```

## Local Docker smoke test

To run the frontend locally against the existing local API on `http://localhost:5176`:

```sh
docker compose -f docker-compose.local.yml up -d --build
```

The frontend will be available at `http://localhost:3003`.

The included `nginx.conf` enables:

- SPA route fallback to `index.html`
- long cache headers for framework assets
- no-store for app settings files

## Direct publish

```sh
dotnet publish CoreBanker/CoreBanker.csproj -c Release --configfile NuGet.Config
```

Deploy the published `wwwroot` output behind any static host that supports SPA fallback.

## Production checklist

- `ApiBaseUrl` points to the intended BankInsight API
- API CORS allows the deployed CoreBanker origin if not same-origin
- Auth, MFA, token refresh, and logout work against production
- Permissioned routes redirect unauthorized users to `/access-denied`
- Core flows are smoke-tested:
  - login and session restore
  - customer onboarding
  - account opening
  - teller posting
  - loans and credit checks
  - approvals
  - group lending operations
  - accounting, treasury, vault, and EOD
  - reporting, security ops, and settings
