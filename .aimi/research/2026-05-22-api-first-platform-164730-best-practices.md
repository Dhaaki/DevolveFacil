---
date: 2026-05-22
agent: best-practices
topic: api-first-platform
depth: standard
---

# API-First Platform: ASP.NET Core 8 Best Practices

> Scope: transforming an internal monolithic ASP.NET Core 8 app (JWT auth, RBAC, REST/DTO controllers, hexagonal architecture, MassTransit + RabbitMQ) into an API-first platform consumable by external frontends and partner systems.

---

## 1. API Versioning Strategies

### Package
Use **`Asp.Versioning.Http`** (formerly Microsoft.AspNetCore.Mvc.Versioning). The old `Microsoft.AspNetCore.Mvc.Versioning` NuGet is deprecated; the new package is maintained by dotnet/aspnet-api-versioning on GitHub.

```csharp
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true; // adds api-supported-versions response header
    options.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new HeaderApiVersionReader("X-Api-Version"),
        new QueryStringApiVersionReader("api-version")
    );
});
```

### URL Path (Recommended for public/partner APIs)
```
GET /api/v1/orders
GET /api/v2/orders
```
- Most visible, easiest to document and cache (CDN-friendly).
- Partner systems can target a version explicitly in their base URL.
- Works with Swagger/OpenAPI per-version document generation.
- **Use this as primary versioning strategy for B2B consumers.**

### Header (`X-Api-Version: 2`)
- Clean URLs, good for internal consumers and SPAs.
- Harder to test without tooling; CDN caching requires `Vary: X-Api-Version`.
- Useful as a secondary reader (via `Combine`) to allow gradual migration.

### Query String (`?api-version=2`)
- Easy to test in a browser, but leaks into logs and breaks caching.
- Use only as fallback or for internal tooling; do not publish as the canonical scheme for partners.

### Versioning Granularity
- Version at the **route group** or **controller** level, not individual actions.
- Deprecate gracefully: mark old versions `[ApiVersion("1.0", Deprecated = true)]` and emit `Sunset` and `Deprecation` response headers.
- Maintain at least two supported versions simultaneously during transition.

### Controller Organization
```
Controllers/
  V1/
    OrdersController.cs
  V2/
    OrdersController.cs
```
Attribute decoration:
```csharp
[ApiController]
[ApiVersion("2.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class OrdersController : ControllerBase { }
```

---

## 2. Authentication for External/Partner Consumers

### Layered Strategy (Recommended)
Do not pick one mechanism — layer them:

| Consumer Type | Recommended Auth |
|---|---|
| Internal frontends (SPA) | Existing JWT (Bearer) |
| External developer partners | OAuth2 Client Credentials |
| B2B system integrations | API Keys (long-lived, rotatable) |
| Human users (partner portals) | OAuth2 Authorization Code + PKCE |

### Option A: OAuth2 / OIDC (Best for Partner Portals)
- Stand up an **identity provider**: **Keycloak** (self-hosted, free), **Duende IdentityServer** (commercial, best .NET integration), or **Auth0/Entra ID** (managed).
- External partners get a `client_id` + `client_secret` and use the **Client Credentials** flow for M2M (machine-to-machine) access.
- Your API validates the JWT from the IdP; no internal changes to business logic needed.
- Scopes map cleanly to your existing RBAC: `orders:read`, `orders:write`, `shipments:manage`.

```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer("Internal", options => { /* existing config */ })
    .AddJwtBearer("External", options =>
    {
        options.Authority = "https://idp.yourcompany.com";
        options.Audience = "partner-api";
    });

builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .AddAuthenticationSchemes("Internal", "External")
        .Build();
});
```

### Option B: API Keys for B2B (Server-to-Server)
See Section 5 for full API key management patterns.

### Option C: Extending Existing JWT
If an external IdP is too heavy initially, issue JWTs from your own service:
- Add a `client_type` claim (`internal` / `partner`).
- Use policy-based authorization to gate endpoints by claim.
- **Risk**: you become the IdP — key rotation, token revocation, and JWKS endpoint maintenance become your problem. Prefer a dedicated IdP for anything public-facing.

---

## 3. OpenAPI / Swagger Documentation

### Swashbuckle vs NSwag

| | Swashbuckle.AspNetCore | NSwag |
|---|---|---|
| Maintenance (2025) | Active, widely used | Active, strong code-gen |
| .NET 8 support | Yes | Yes |
| Multi-version docs | Yes (with Asp.Versioning integration) | Yes |
| Code generation | No (use NSwag CLI separately) | Built-in (C#, TypeScript) |
| XML comments | Full support | Full support |
| Minimal API support | Improving | Good |
| Best for | Documentation-first, teams using ReDoc/Swagger UI | Teams who need client SDK auto-generation |

**Recommendation**: Use **Swashbuckle** for documentation; pair with **NSwag CLI** for partner SDK generation if needed.

### Swashbuckle with API Versioning
```csharp
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Partner API", Version = "v1" });
    options.SwaggerDoc("v2", new OpenApiInfo { Title = "Partner API", Version = "v2" });

    // Include XML comments
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));

    // Hide deprecated versions from public docs
    options.DocInclusionPredicate((docName, apiDesc) =>
    {
        if (!apiDesc.TryGetMethodInfo(out var method)) return false;
        var versions = method.DeclaringType?
            .GetCustomAttributes(true)
            .OfType<ApiVersionAttribute>()
            .SelectMany(a => a.Versions);
        return versions?.Any(v => $"v{v}" == docName) ?? false;
    });

    // Add JWT Bearer security definition
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
});
```

### Per-Version Swagger UI Endpoints
```csharp
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v2/swagger.json", "Partner API v2 (Current)");
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Partner API v1 (Deprecated)");
});
```

### Documentation Completeness for Partners
- Enable XML doc generation (`<GenerateDocumentationFile>true</GenerateDocumentationFile>`).
- Use `[ProducesResponseType]` on every action — partners depend on this for code generation.
- Add `[Produces("application/json")]` and `[Consumes("application/json")]` explicitly.
- Document error shapes: define a standard `ProblemDetails` response for 400/401/403/404/500.

---

## 4. CORS Configuration for Multi-Consumer Scenarios

### Core Principle: Named Policies Per Consumer Class
Never use a single wildcard policy for a multi-tenant API. Define named policies:

```csharp
builder.Services.AddCors(options =>
{
    // Internal SPA (your own frontend)
    options.AddPolicy("InternalFrontend", policy =>
        policy.WithOrigins("https://app.yourcompany.com", "https://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());

    // Trusted partner portals (curated whitelist)
    options.AddPolicy("TrustedPartners", policy =>
        policy.WithOrigins(GetPartnerOrigins()) // load from config/db
              .WithHeaders("Content-Type", "Authorization", "X-Api-Version")
              .WithMethods("GET", "POST", "PUT")
              .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));

    // Public read-only endpoints (e.g., status, catalog)
    options.AddPolicy("PublicReadOnly", policy =>
        policy.AllowAnyOrigin()
              .WithHeaders("Content-Type")
              .WithMethods("GET")
              .SetPreflightMaxAge(TimeSpan.FromHours(1)));
});
```

Apply globally with the default, then override at controller/action level:
```csharp
app.UseCors("InternalFrontend"); // default

[EnableCors("TrustedPartners")]
[ApiController]
public class PartnerOrdersController : ControllerBase { }
```

### Dynamic Partner Origins
Load allowed origins from configuration (database or appsettings) at startup, or implement a custom `ICorsPolicyProvider` to resolve partner origins at runtime without restarting:

```csharp
public class DynamicCorsPolicyProvider : ICorsPolicyProvider
{
    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string policyName)
    {
        var origins = await _partnerService.GetAllowedOriginsAsync();
        return new CorsPolicyBuilder()
            .WithOrigins(origins.ToArray())
            .AllowAnyHeader()
            .Build();
    }
}
```

### Security Rules
- Never combine `AllowAnyOrigin()` with `AllowCredentials()` — ASP.NET Core rejects this combination.
- Always set `SetPreflightMaxAge` to reduce preflight traffic.
- Restrict exposed headers to what partners actually need.

---

## 5. API Key Management Patterns for B2B

### Architecture

```
┌─────────────────────────────────────────────────┐
│  API Key Lifecycle                              │
│                                                 │
│  Generate → Hash → Store → Validate → Rotate  │
└─────────────────────────────────────────────────┘
```

### Storage Pattern
- **Never** store raw API keys — store a SHA-256 (or bcrypt) hash.
- Display the raw key to the partner only **once** at generation time.
- Store alongside: `PartnerId`, `KeyPrefix` (first 8 chars for display/identification), `HashedKey`, `CreatedAt`, `ExpiresAt`, `LastUsedAt`, `IsRevoked`, `Scopes[]`.

```csharp
public class ApiKey
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public string KeyPrefix { get; set; } = default!;   // e.g. "pk_live_a1b2"
    public string HashedKey { get; set; } = default!;   // SHA256(rawKey)
    public string[] Scopes { get; set; } = [];
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? LastUsedAt { get; set; }
    public bool IsRevoked { get; set; }
}
```

### Generation
```csharp
public static (string RawKey, string HashedKey, string Prefix) GenerateApiKey()
{
    var bytes = RandomNumberGenerator.GetBytes(32);
    var rawKey = $"pk_live_{Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=')}";
    var hashedKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));
    var prefix = rawKey[..12];
    return (rawKey, hashedKey, prefix);
}
```

### Validation via Custom Authentication Handler
```csharp
public class ApiKeyAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeader))
            return AuthenticateResult.NoResult();

        var rawKey = apiKeyHeader.ToString();
        var hashedKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey)));

        var apiKey = await _apiKeyService.FindByHashAsync(hashedKey);
        if (apiKey is null || apiKey.IsRevoked || apiKey.ExpiresAt < DateTimeOffset.UtcNow)
            return AuthenticateResult.Fail("Invalid or expired API key");

        // Fire-and-forget LastUsedAt update (don't block the request)
        _ = _apiKeyService.UpdateLastUsedAsync(apiKey.Id);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, apiKey.PartnerId.ToString()),
            new Claim("partner_id", apiKey.PartnerId.ToString()),
            new Claim("key_scopes", string.Join(",", apiKey.Scopes))
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);
        return AuthenticateResult.Success(ticket);
    }
}
```

Registration:
```csharp
builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>("ApiKey", _ => { });
```

### Rate Limiting (Essential for API Keys)
Use ASP.NET Core 8's built-in rate limiting (`Microsoft.AspNetCore.RateLimiting`):
```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("partner-api", context =>
    {
        var partnerId = context.User.FindFirst("partner_id")?.Value ?? context.Connection.RemoteIpAddress?.ToString();
        return RateLimitPartition.GetTokenBucketLimiter(partnerId, _ => new TokenBucketRateLimiterOptions
        {
            TokenLimit = 100,
            ReplenishmentPeriod = TimeSpan.FromMinutes(1),
            TokensPerPeriod = 100
        });
    });
});
```

### Key Rotation
- Partner portal UI shows `prefix` for key identification.
- Rotation generates a new key; old key remains valid for a configurable overlap period (e.g., 24h).
- Emit `X-Api-Key-Expires` header in responses when a key nears expiry.

---

## 6. Separating Internal from Public API Endpoints

### Strategy: Multiple Route Groups + Area Separation

Separate concerns at the **routing level**, not just with attributes.

#### Option A: Distinct Route Prefixes + Middleware Branching
```csharp
// Internal endpoints: /internal/...
// Partner API: /api/v{n}/...
// Public (no auth): /public/...

app.MapGroup("/internal")
   .RequireAuthorization("InternalOnly")
   .AddEndpointFilter<InternalNetworkFilter>(); // e.g., IP allowlist

app.MapGroup("/api")
   .RequireAuthorization("PartnerOrInternal");

app.MapGroup("/public")
   .AllowAnonymous()
   .CacheOutput(); // aggressive caching for public endpoints
```

#### Option B: Controller Areas (Traditional)
```
Controllers/
  Internal/   ← [Area("Internal")], route: /internal/...
  Partner/    ← [Area("Partner")], route: /api/v{n}/...
  Public/     ← [Area("Public")],  route: /public/...
```

With Swagger, generate separate documents:
- `/swagger/internal` — not exposed to external traffic (protected by middleware)
- `/swagger/v1`, `/swagger/v2` — partner-facing, exposed via public domain

#### Option C: Separate ASP.NET Core Applications (Best for True Isolation)
For mature platforms, run two processes:
- **Internal API** (port 5001) — never exposed to the public internet, accessed only by internal frontends via internal DNS.
- **Partner/External API** (port 5000) — behind WAF/API gateway, publicly accessible.

Both share the same domain/application layer (hexagonal architecture ports). Only the adapters (controllers, auth schemes) differ. This maps cleanly to your existing hexagonal structure.

### Hiding Internal Endpoints from Public Swagger
```csharp
// Custom attribute to mark internal-only actions
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class InternalOnlyAttribute : Attribute { }

// In SwaggerGen config:
options.DocInclusionPredicate((docName, apiDesc) =>
{
    if (docName == "internal") return true; // show everything in internal doc
    // For partner-facing docs, exclude internal-only endpoints
    return !apiDesc.ActionDescriptor.EndpointMetadata
        .OfType<InternalOnlyAttribute>().Any();
});
```

### Network-Level Enforcement
For endpoints that must not reach the internet, add a middleware filter:
```csharp
public class InternalNetworkMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var remoteIp = context.Connection.RemoteIpAddress;
        if (!IsInternalNetwork(remoteIp))
        {
            context.Response.StatusCode = 404; // return 404, not 403 (avoid info disclosure)
            return;
        }
        await next(context);
    }
}
```

---

## 7. Integration with MassTransit + Hexagonal Architecture

Since you already use MassTransit + RabbitMQ, external API calls that trigger async operations should follow the **Accept-Redirect-Poll** pattern (HTTP 202 Accepted):

```csharp
[HttpPost("v2/orders")]
public async Task<IActionResult> CreateOrder(CreateOrderRequest request)
{
    var correlationId = NewId.NextGuid();
    await _publishEndpoint.Publish(new CreateOrderCommand(correlationId, request.ToCommand()));

    return AcceptedAtAction(nameof(GetOrderStatus), new { correlationId }, new
    {
        correlationId,
        statusUrl = Url.Action(nameof(GetOrderStatus), new { correlationId })
    });
}

[HttpGet("v2/orders/{correlationId}/status")]
public async Task<IActionResult> GetOrderStatus(Guid correlationId) { ... }
```

This keeps your hexagonal ports clean: the controller (adapter) publishes a command, the domain handles it asynchronously, and partners poll for results via a status endpoint.

---

## Priority Implementation Order

1. **Add `Asp.Versioning.Http`** — establish URL-path versioning before exposing any external endpoints.
2. **Set up OAuth2 Client Credentials** via Keycloak or Duende — do not shortcut to API keys alone for partners with portal access.
3. **Implement API key handler** for server-to-server B2B integrations.
4. **Configure named CORS policies** — `InternalFrontend`, `TrustedPartners`.
5. **Add Swashbuckle** with per-version docs and `InternalOnly` doc exclusion predicate.
6. **Add rate limiting** per partner ID.
7. **Separate route groups** (`/internal`, `/api`, `/public`) at the middleware level.

---

## Sources and Authority

- **Official docs**: Microsoft ASP.NET Core 8 documentation (CORS, Authentication, Rate Limiting, Minimal APIs)
- **Package**: `Asp.Versioning.Http` — dotnet/aspnet-api-versioning GitHub (official Microsoft-maintained package, formerly `Microsoft.AspNetCore.Mvc.Versioning`)
- **Community consensus**: Swashbuckle for documentation, Duende/Keycloak for OAuth2 IdP, SHA-256 hashing for API keys
- **Hexagonal architecture alignment**: patterns derived from standard Ports & Adapters — controllers as adapters only, domain logic untouched by auth scheme changes
