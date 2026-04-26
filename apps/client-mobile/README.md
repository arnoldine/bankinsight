# BankInsight Client Mobile Scaffold

This app is the React Native customer channel scaffold for BankInsight.

## Targets

- Android
- iOS
- Web

## Stack

- Expo
- React Native
- React Navigation
- TypeScript

## Scope

This scaffold is intentionally Phase 1 focused:

- secure customer home/dashboard
- account visibility
- statements
- alerts
- complaints and recourse
- security center

## Run

From this folder:

```powershell
npm install
npm run start
```

Set Expo public env vars before starting when you need a non-default API host or the development OTP preview:

```powershell
$env:EXPO_PUBLIC_API_BASE_URL="http://localhost:5176/api"
$env:EXPO_PUBLIC_SHOW_DEV_OTP="true"
npm run web
```

You can also copy [`.env.example`](C:\Backup old\dev\bankinsight\apps\client-mobile\.env.example) and set the values once for local development.

Platform shortcuts:

```powershell
npm run android
npm run ios
npm run web
```

## Next steps

1. Add authenticated session bootstrap and secure token storage.
2. Replace static screen data with API-backed queries.
3. Introduce step-up authentication flows for profile, complaint evidence, and security actions.
4. Add profile, support, and secure messaging screens.
5. Connect audit-event emission to all critical user actions.

## Environment Notes

- `EXPO_PUBLIC_API_BASE_URL` defaults to `http://localhost:5176/api` for local preview only.
- On web, if `EXPO_PUBLIC_API_BASE_URL` is unset, the app will target the current hostname on port `5176`.
- `EXPO_PUBLIC_SHOW_DEV_OTP` defaults to `false` and should stay off outside local development.
