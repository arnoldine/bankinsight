# Remote API-Only Deployment

This deployment path is for the BankInsight API-only remote runtime hosted behind Cloudflare Tunnel at:

- `https://bankinsightapi.rproxyserv.net`

## What this deploys

- `bankinsight-api`
- `bankinsight-postgres`

It does **not** deploy:

- BankInsight React frontend
- CoreBanker frontend

## Source files

- Compose: [bankinsight-api-only.compose.yml](C:\Backup old\dev\bankinsight\deploy\bankinsight-api-only.compose.yml)
- Runtime Dockerfile: [Dockerfile](C:\Backup old\dev\bankinsight\deploy\remote-api-runtime\Dockerfile)
- Deployment script: [Deploy-RemoteApiOnly.ps1](C:\Backup old\dev\bankinsight\deploy\Deploy-RemoteApiOnly.ps1)

## Local prerequisites

- `dotnet`
- `ssh`
- `scp`
- access to the remote host
- a valid remote `.env` file already present in `/opt/bankinsight-api-runtime`

## Remote expectations

The remote directory should already contain:

- `/opt/bankinsight-api-runtime/.env`

That `.env` should hold operational settings such as:

- database credentials
- JWT secret
- `CORS_ALLOWED_ORIGINS`
- SMTP2GO / SMTP settings

## Standard workflow

### 1. Stage only

This builds the API publish output and refreshes the local runtime bundle without touching the server:

```powershell
.\deploy\Deploy-RemoteApiOnly.ps1 -StageOnly
```

### 2. Full remote deployment

This:

- publishes the API
- refreshes `deploy/remote-api-runtime`
- uploads the runtime bundle and compose file
- rebuilds the remote Docker image
- recreates the remote API-only stack
- checks `/health`

```powershell
.\deploy\Deploy-RemoteApiOnly.ps1
```

## Notes

- The script uses `NuGet.Local.Config` when it exists, which helps keep restore deterministic in this repo.
- Deployment artifacts under `deploy/remote-api-runtime/` and `deploy/*.zip` are intentionally ignored by Git.
- If the remote database drifts behind the running API, the application bootstrapper should repair additive schema on startup. If startup still fails, inspect:
  - [DatabaseSchemaBootstrapper.cs](C:\Backup old\dev\bankinsight\BankInsight.API\Data\DatabaseSchemaBootstrapper.cs)
  - remote container logs

## Verification targets

- Local remote-tunnel health:
  - [https://bankinsightapi.rproxyserv.net/health](https://bankinsightapi.rproxyserv.net/health)
- Remote host direct health:
  - `http://localhost:5176/health`
