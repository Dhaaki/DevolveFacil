---
date: 2026-05-22
topic: api-first-platform
researchPaths:
  - .aimi/research/2026-05-22-api-first-platform-164730-best-practices.md
---

# API-First Platform — DevolveFacil

## What We're Building

Transform DevolveFacil from a standalone app (backend + its own React frontend) into an
API-first platform that multiple internal La Moda apps can consume. The existing React
frontend stays as-is and becomes one of those consumers. A new customer-facing app (built
by the La Moda team) will access the same API and fully own the customer return-request
flow, replacing what the React UI currently does for end customers.

The backend is already structured as a REST API (controllers, DTOs, JWT, roles) — the core
work is operational and configurational, not architectural.

## Why This Approach

Single obvious approach: the hexagonal architecture already separates the domain from
delivery. Controllers are thin adapters; the domain (Core) has no UI dependency. Exposing
the API to additional consumers requires no structural changes — only the surface-level
concerns (Swagger, CORS, versioning) need to be added. OAuth2 / OIDC is deliberately
deferred: all consumers are internal La Moda apps, so extending the existing JWT flow is
safe for now and avoids introducing an external IdP (Keycloak, Duende) before it's needed.

## Key Decisions

- **Auth stays as JWT**: All consumers (internal React app + new store app) authenticate via
  the existing `POST /api/auth/customer/login` and `POST /api/auth/admin/login` flows.
  Roles (`customer`, `Agent`, `QualityInspector`, `Finance`, `Supervisor`) continue to
  gate individual endpoints. OAuth2 is deferred to a future phase.

- **Full platform exposed**: Both customer-facing and admin endpoints are accessible through
  the API. The new consumer app decides which roles it acquires tokens for.

- **Swagger/OpenAPI added via Swashbuckle**: Interactive docs at `/swagger`. No SDK
  generation in this phase. Partners read the spec and integrate directly.

- **URL-path versioning** (`/api/v1/...`): Use `Asp.Versioning.Http` (the replacement for
  the deprecated `Microsoft.AspNetCore.Mvc.Versioning`). Consumers lock their base URL
  to `v1` — breaking changes will land under `v2` without breaking existing integrations.

- **CORS extended for new consumers**: The current config only allows `localhost:5173`.
  Named CORS policies will be added: one for the internal React SPA, one for each
  additional internal consumer origin. No wildcard origins.

- **React frontend stays unchanged**: It becomes one of multiple consumers. No pages
  removed, no shared state hacks between apps.

- **Success definition**: The new customer-facing app handles the complete return flow
  end-to-end (create return, upload evidence, track status, confirm delivery) without
  going through DevolveFacil's React UI. DevolveFacil's React UI continues to serve ops.

## Open Questions

- What origin URL will the new consumer app run on? (CORS config needs this before it can
  call the API across origins.) [deferred: to be decided during implementation setup]

- Will the new app need direct access to file uploads (MinIO), or will it go through
  DevolveFacil's existing `POST /api/uploads` endpoint? [deferred: likely via the API endpoint, confirm during implementation]

- VTEX, Correios, and SAP adapters are still mocked. Will the new consumer need real
  integrations to go live, or are mocks acceptable for the initial rollout? [deferred: mocks sufficient for Phase 3 scope per README]

## Next Steps
> Run `/aimi:plan` to generate implementation tasks
