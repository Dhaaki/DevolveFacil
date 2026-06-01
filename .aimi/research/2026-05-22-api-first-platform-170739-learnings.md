---
date: 2026-05-22
agent: learnings
topic: api-first-platform
depth: standard
---

# Institutional Learnings Search Results — API-First Platform

## Search Context

- **Feature/Task**: API-first platform transformation in ASP.NET Core 8 — adding Swagger/Swashbuckle, URL-path versioning, CORS configuration for multiple consumers
- **Keywords Used**: api-versioning, swagger, swashbuckle, cors, asp.net core 8, url-path versioning, multi-consumer, partner-api
- **Files Scanned**: No `.aimi/solutions/` directory exists in this repository. Two existing `.aimi/` artefacts were found and read in full.
- **Relevant Matches**: 2 files (both directly on-topic)

---

## No Solutions Directory

The `.aimi/solutions/` directory does not exist. This repository is early-stage and has not yet accumulated a solutions knowledge base. The search fell back to existing `.aimi/` artefacts.

---

## Relevant Learnings

### 1. API-First Platform Best Practices (existing research)

- **File**: `.aimi/research/2026-05-22-api-first-platform-164730-best-practices.md`
- **Relevance**: Directly authored for this transformation. Covers every topic requested: Swashbuckle setup, URL-path versioning with `Asp.Versioning.Http`, and CORS named policies.
- **Key Insights**:
  - Use `Asp.Versioning.Http` — the old `Microsoft.AspNetCore.Mvc.Versioning` NuGet is deprecated. This is the most common mistake teams make when adding versioning to .NET 8 projects.
  - Never combine `AllowAnyOrigin()` with `AllowCredentials()` — ASP.NET Core rejects this combination at runtime.
  - Define named CORS policies per consumer class (`InternalFrontend`, `TrustedPartners`) rather than a single wildcard policy.
  - Use `DocInclusionPredicate` in SwaggerGen to route controllers to the correct per-version OpenAPI document.
  - Use `[ApiVersion("1.0", Deprecated = true)]` with `Sunset`/`Deprecation` response headers for graceful deprecation.
  - Priority implementation order: versioning first → CORS → Swashbuckle → rate limiting → route group separation.

### 2. API-First Platform Brainstorm / Decision Record

- **File**: `.aimi/brainstorms/2026-05-22-api-first-platform-brainstorm.md`
- **Relevance**: Contains the scoped architectural decisions for DevolveFacil specifically, including what is deferred and why.
- **Key Insights**:
  - **Auth stays JWT for now**: OAuth2/OIDC (Keycloak, Duende) is explicitly deferred. All Phase 3 consumers are internal La Moda apps authenticating via the existing `POST /api/auth/customer/login` and `POST /api/auth/admin/login` flows.
  - **CORS open question**: The new consumer app's origin URL is not yet confirmed — CORS config cannot be finalised until that URL is known. Flag this as a required input before implementation.
  - **Mocks acceptable**: VTEX, Correios, and SAP adapters remain mocked for Phase 3 scope. No real integrations needed for the new consumer to go live.
  - **React frontend untouched**: Existing SPA becomes one of multiple consumers with no changes.
  - **Success definition**: New customer-facing app handles the complete return flow (create, upload evidence, track status, confirm delivery) via the API — without going through DevolveFacil's React UI.

---

## Recommendations

1. **Confirm the new consumer app's origin before writing CORS config.** This is the only hard blocker — you cannot add the correct `WithOrigins(...)` entry until the origin is known. Use `localhost:PORT` as a placeholder for local development, but get the production/staging URL before any cross-origin calls will work.

2. **Add `Asp.Versioning.Http` (not `Microsoft.AspNetCore.Mvc.Versioning`).** The old package is deprecated and will produce confusing errors on .NET 8. Use `Asp.Versioning.Http` from the dotnet/aspnet-api-versioning GitHub repo.

3. **Route all existing controllers under `/api/v1/...` as the baseline.** Annotate with `[ApiVersion("1.0")]` and `[Route("api/v{version:apiVersion}/[controller]")]`. Existing callers (the React SPA) must be updated to use the versioned base URL.

4. **Enable XML doc generation** (`<GenerateDocumentationFile>true</GenerateDocumentationFile>` in the `.csproj`) and add `[ProducesResponseType]` to every action before wiring Swashbuckle — this is easier to do before than after Swagger is running.

5. **Set `SetPreflightMaxAge`** on all CORS policies to reduce preflight traffic once the system has multiple consumers making frequent cross-origin calls.

6. **Do not expose the Swagger UI on production without auth-gating it.** Since all current consumers are internal, the UI endpoint (`/swagger`) should be protected (e.g., behind an IP allowlist or a simple header check) before the API is publicly reachable.

---

## No Additional Matches

No `.aimi/solutions/` files exist to search. All findings are from the two pre-existing `.aimi/` research artefacts above.
