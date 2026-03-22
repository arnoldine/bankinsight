# Free Deployment Guide

This repo is prepared for a free-tier-friendly setup with:

- Frontend: Render Static Site
- API: Render Web Service
- Database: Render Postgres or Neon Postgres

Recommended stack:

- Render for both the Vite/React frontend and the `.NET 8` API
- Render Postgres for a single-platform deployment
- Neon if you want a longer-lived free Postgres option

References:

- Render static sites: https://render.com/docs/static-sites
- Render blueprints: https://render.com/docs/blueprint-spec
- Render Docker services: https://render.com/docs/docker
- Render multi-service architecture: https://render.com/docs/multi-service-architecture
- Render Postgres: https://render.com/docs/databases
- Neon connection strings: https://neon.com/docs/get-started-with-neon/connect-neon

## What is automated already

The repo now includes:

- `render.yaml` for both:
  - a Render static frontend
  - a Render API service
- `.env.vercel.example` showing the frontend API variable

This means deployment can be mostly automated once the repo is connected to Render.

## Production Environment Checklist

Before going live, confirm:

- Neon database exists
- Neon pooled Postgres connection string copied
- Render `DB_CONNECTION_STRING` set
- Render `JWT_SECRET` set
- Vercel `VITE_API_URL` set to the Render API URL plus `/api`
- First Render deployment succeeds
- First Vercel deployment succeeds
- Login works from the deployed frontend
- Browser network calls resolve to the deployed Render API
- API can migrate and seed the Neon database

## What still requires dashboard setup

These parts still need to be configured in provider dashboards:

- Choose a database strategy:
  - use the Render Postgres database declared in `render.yaml`, or
  - use Neon and set `DB_CONNECTION_STRING` manually
- Trigger the first deploys

## 1. Choose the Database

### Option A: Render Postgres

This repo now includes a Render Postgres database in [render.yaml](C:\Backup old\dev\bankinsight\render.yaml).

That means the Blueprint can create:

- the frontend
- the API
- the Postgres database

and wire the API `DB_CONNECTION_STRING` from the database automatically.

Important free-tier limitation from Render docs:

- only one free Render Postgres database per workspace
- 1 GB limit
- 30-day expiration
- no backups

Source:

- [Render free instances](https://render.com/docs/free)

### Option B: Neon

If you want a more durable free database, use Neon instead.

1. Create a free Neon project.
2. Create or use the default database and role.
3. Copy the pooled Postgres connection string from the Neon dashboard.

Use a connection string that looks like:

`postgresql://USER:PASSWORD@HOST/DBNAME?sslmode=require`

For Render, set this as:

- `DB_CONNECTION_STRING`

## 2. Deploy Frontend, API, and Optional Database on Render

This repo includes `render.yaml` at the root, so you can use one Blueprint deployment for both services.

1. Push this repo to GitHub.
2. In Render, create a new Blueprint from the repo.
3. Render should detect `render.yaml`.
4. Before the first deploy completes, set:
   - nothing extra if you use the Render Postgres declared in `render.yaml`
   - or `DB_CONNECTION_STRING` to your Neon connection string if you prefer Neon
5. Keep the generated `JWT_SECRET`, or replace it with your own long secret.
6. If your repo is public, you can add a Deploy to Render button to the README using Render's button format.

The Blueprint defines:

- `bankinsight-api` as a Docker-based web service
- `bankinsight-frontend` as a Render static site
- `bankinsight-postgres` as a Render Postgres database

The frontend is configured to read the API URL from the Render API service inside the same Blueprint.

Expected API URL:

- `https://your-render-service-name.onrender.com`

Expected frontend URL:

- `https://your-render-frontend-name.onrender.com`

Important API environment variables:

- `DB_CONNECTION_STRING` if using Neon
- `JWT_SECRET`
- `ASPNETCORE_ENVIRONMENT=Production`
- `ASPNETCORE_URLS=http://+:8080`

## 3. Optional: Deploy Frontend on Vercel Instead

This repo still includes [vercel.json](C:\Backup old\dev\bankinsight\vercel.json) in case you prefer:

- frontend on Vercel
- API on Render
- database on Neon

If you choose Vercel instead, set:

- `VITE_API_URL=https://your-render-service-name.onrender.com/api`

## Render Deploy Button

Render documents a deploy button flow for Blueprint repos. Once this repo is on GitHub, you can use:

```md
[![Deploy to Render](https://render.com/images/deploy-to-render-button.svg)](https://render.com/deploy?repo=https://github.com/YOUR_ORG/YOUR_REPO)
```

Render recommends setting the `repo` parameter explicitly instead of relying on the browser referrer.

## 4. Post-deploy check

After both deployments:

1. Open the Render frontend URL
2. Try login
3. Confirm browser requests are going to the Render API URL
4. Confirm the Render API can connect to Neon
5. Confirm the API service has completed database migration/seeding on startup

## Troubleshooting

### Render frontend loads but API calls fail

Check:

- the Render frontend service deployed successfully
- `VITE_API_URL` is populated from the API service
- the API URL resolves to the Render API service
- the browser is calling the API path you expect

Render applies environment variable changes on a new deploy.

### Render deploy succeeds but the API fails to boot

Check:

- `DB_CONNECTION_STRING` is present
- `JWT_SECRET` is present
- the Neon connection string is valid
- the connection string includes `sslmode=require`

### Render app boots but cannot reach the database

Check:

- if using Render Postgres, verify the Blueprint created the database and wired `DB_CONNECTION_STRING`
- if using Neon, verify you copied the Neon connection string from the Connect panel
- verify the database, role, and password are correct

Neon documents both pooled and direct connection strings. For most app hosting cases, pooled is the safer default.

### Frontend routes 404 on refresh

This repo includes a static-site rewrite rule in [render.yaml](C:\Backup old\dev\bankinsight\render.yaml) that sends app routes to `index.html`. If you still see 404s, verify that the frontend static site was created from the Blueprint and that the rewrite rule is present in the synced Blueprint.

## Automation summary

I can automate the repo side, and I have already done that:

- deployment config files
- provider-ready env var structure
- SPA routing config
- Render API blueprint config

What I cannot fully automate from inside this local workspace alone is:

- creating your Vercel account/project
- creating your Render account/project
- creating your Neon project
- pasting secrets into those dashboards
- linking GitHub repos on your behalf

If you want, the next step I can do is add:

- a `Deploy to Render` button in the README
- a production env checklist
- a small deployment troubleshooting section for common Vercel/Render/Neon issues
