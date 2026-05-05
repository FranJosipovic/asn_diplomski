# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
npm run dev      # Dev server at http://localhost:5173
npm run build    # Type-check (tsc) then bundle
npm run preview  # Serve the built output locally
```

No linter or test runner is configured.

## API Proxy

Vite proxies all `/api/*` requests to `http://localhost:5017`. The backend must be running on that port for API calls to work during development.

## Architecture

This is a React 18 + TypeScript + Vite frontend for **Automatski sustav za navodnjavanje** (Automated Irrigation System). It has no routing library — navigation is managed via a `currentView` state in [App.tsx](src/App.tsx) using a discriminated union: `{ kind: 'home' } | { kind: 'tenant'; id: number }`.

### Key files

| File | Role |
|---|---|
| [src/types.ts](src/types.ts) | All domain types, enums (`DeviceType`, `SensorType`), and pure utility functions (`formatDateTime`, `timeAgo`, `isRecentlySeen`) |
| [src/api.ts](src/api.ts) | Fetch-based API client — `createTenant(req)` and `getTenant(id)` |
| [src/styles.css](src/styles.css) | All styling (~769 lines); dark theme with green accent `#00cc96`, CSS variables for design tokens, custom fonts Rajdhani and JetBrains Mono |

### Views and components

- **[HomeView](src/views/HomeView.tsx)** — tenant creation (left panel) and recent/search tenants (right panel). Recent tenants are persisted to `localStorage` under key `asn_recent_tenants` (max 10).
- **[TenantView](src/views/TenantView.tsx)** — tenant details plus device cards. Auto-refreshes every 30 s; the interval resets on manual refresh.
- **[CreateTenantForm](src/components/CreateTenantForm.tsx)** — required fields + collapsible optional section (contact info, location).
- **[DeviceCard](src/components/DeviceCard.tsx)** — displays a single device with its sensor readings.

### State management

No external state library. Each view/component manages its own state with `useState`/`useCallback`. Data flows down as props; events flow up as callbacks.

### Domain model

Tenants own Devices (type `SensorUnit` or `PumpUnit`). Each Device has Sensors (temperature, soil moisture, water level, pump command). Dates are formatted for the `hr-HR` locale.
