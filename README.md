<div align="center">
<img width="1200" height="475" alt="GHBanner" src="https://github.com/user-attachments/assets/0aa67016-6eaf-458a-adb2-6e31a0763ed6" />
</div>

# BankInsight

BankInsight is a banking operations platform with:

- React + Vite frontend
- .NET 8 API
- PostgreSQL database

## Run Locally

Prerequisites:

- Node.js
- .NET 8 SDK
- PostgreSQL

Frontend:

1. Install dependencies:
   `npm install`
2. Run the app:
   `npm run dev`

API:

1. Set `DB_CONNECTION_STRING`
2. Set `JWT_SECRET`
3. Run the API from [BankInsight.API](C:\Backup old\dev\bankinsight\BankInsight.API)

Docker:

- Use [docker-compose.yml](C:\Backup old\dev\bankinsight\docker-compose.yml)

## Free Deployment

Recommended free-tier-friendly stack:

- Frontend: Render Static Site
- API: Render Web Service
- Database: Render Postgres or Neon

Deployment assets already included:

- [vercel.json](C:\Backup old\dev\bankinsight\vercel.json)
- [render.yaml](C:\Backup old\dev\bankinsight\render.yaml)
- [.env.vercel.example](C:\Backup old\dev\bankinsight\.env.vercel.example)
- [FREE_DEPLOYMENT_GUIDE.md](C:\Backup old\dev\bankinsight\docs\FREE_DEPLOYMENT_GUIDE.md)

Provider docs:

- [Render static sites](https://render.com/docs/static-sites)
- [Render deploy button](https://render.com/docs/deploy-to-render)
- [Render blueprint spec](https://render.com/docs/blueprint-spec)
- [Render Postgres](https://render.com/docs/databases)
- [Neon connection strings](https://neon.com/docs/get-started-with-neon/connect-neon)

### Deploy to Render

This repo now supports hosting both the frontend and API on Render from the same Blueprint.

Once this repo is on GitHub, replace `YOUR_REPO_URL` below with the full repo URL:

```md
[![Deploy to Render](https://render.com/images/deploy-to-render-button.svg)](https://render.com/deploy?repo=YOUR_REPO_URL)
```

### Production Checklist

- Choose a database:
  - Render Postgres for all-in-Render deployment
  - Neon for a longer-lived free database option
- Set `JWT_SECRET` in Render
- Sync the Render Blueprint
- Verify the static frontend can reach the API service
- Verify login, API calls, and database connectivity

Important:

- Render can host the frontend, API, and Postgres database.
- On the free tier, Render Postgres expires after 30 days and has no backups, so Neon is still the safer free option for anything you want to keep longer.

For full instructions and troubleshooting, see [FREE_DEPLOYMENT_GUIDE.md](C:\Backup old\dev\bankinsight\docs\FREE_DEPLOYMENT_GUIDE.md).
