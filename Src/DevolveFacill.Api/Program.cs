using System.Text;
using DevolveFacill.Adapters.Carrier.Mock;
using DevolveFacill.Adapters.Commerce.Mock;
using DevolveFacill.Adapters.Erp.Mock;
using DevolveFacill.Adapters.Storage;
using DevolveFacill.Adapters.Storage.Mock;
using DevolveFacill.Api.Auth;
using DevolveFacill.Api.Middleware;
using DevolveFacill.Core.Domain.Entities;
using DevolveFacill.Core.Events;
using DevolveFacill.Core.Ports;
using DevolveFacill.Infrastructure.Persistence;
using DevolveFacill.Infrastructure.Repositories;
using DevolveFacill.Workers;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ─────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateLogger();
builder.Host.UseSerilog();

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("Postgres")));

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<CustomerRepository>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<ReturnRequestRepository>();

// ── Auth ──────────────────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is required");

builder.Services.AddSingleton<JwtService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });
builder.Services.AddAuthorization();

// ── Commerce Platform adapter ─────────────────────────────────────────────────
var commerceProvider = builder.Configuration["Commerce:Provider"] ?? "Mock";
builder.Services.AddSingleton<ICommercePlatform>(_ => commerceProvider switch
{
    "Vtex" => throw new NotImplementedException("VtexCommercePlatform — implement in Phase 4"),
    _ => new MockCommercePlatform()
});

// ── Carrier adapter ───────────────────────────────────────────────────────────
var carrierProvider = builder.Configuration["Carrier:Provider"] ?? "Mock";
builder.Services.AddSingleton<ICarrier>(_ => carrierProvider switch
{
    "Correios" => throw new NotImplementedException("CorreiosCarrier — implement in Phase 4"),
    _ => new MockCarrier()
});

// ── ERP adapter ───────────────────────────────────────────────────────────────
var erpProvider = builder.Configuration["Erp:Provider"] ?? "Mock";
builder.Services.AddSingleton<IErpIntegration>(sp => erpProvider switch
{
    "Sap" => throw new NotImplementedException("SapErpIntegration — implement in Phase 4"),
    _ => new MockErpIntegration(sp.GetRequiredService<ILogger<MockErpIntegration>>())
});

// ── Storage adapter ───────────────────────────────────────────────────────────
var storageProvider = builder.Configuration["Storage:Provider"] ?? "Mock";
builder.Services.AddSingleton<IStorageService>(sp => storageProvider switch
{
    "S3" => new S3StorageService(
        sp.GetRequiredService<IConfiguration>(),
        sp.GetRequiredService<ILogger<S3StorageService>>()),
    _ => new MockStorageService(sp.GetRequiredService<ILogger<MockStorageService>>())
});

// ── MassTransit + RabbitMQ ────────────────────────────────────────────────────
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<LabelGeneratorConsumer>();
    x.AddConsumer<CarrierEventConsumer>();
    x.AddConsumer<PostQualityConsumer>();
    x.AddConsumer<VoucherConsumer>();
    x.AddConsumer<FinanceNotifierConsumer>();
    x.AddConsumer<CustomerNotifierConsumer>();

    x.UsingRabbitMq((ctx, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMq:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMq:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMq:Password"] ?? "guest");
        });

        cfg.ReceiveEndpoint("return-label-queue", e =>
        {
            e.ConfigureConsumer<LabelGeneratorConsumer>(ctx);
            e.UseMessageRetry(r => r.Intervals(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30)));
        });

        cfg.ReceiveEndpoint("carrier-event-queue", e =>
        {
            e.ConfigureConsumer<CarrierEventConsumer>(ctx);
            e.UseMessageRetry(r => r.Intervals(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30)));
        });

        cfg.ReceiveEndpoint("post-quality-queue", e =>
            e.ConfigureConsumer<PostQualityConsumer>(ctx));

        cfg.ReceiveEndpoint("voucher-queue", e =>
        {
            e.ConfigureConsumer<VoucherConsumer>(ctx);
            e.UseMessageRetry(r => r.Intervals(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60)));
        });

        cfg.ReceiveEndpoint("finance-notify-queue", e =>
        {
            e.ConfigureConsumer<FinanceNotifierConsumer>(ctx);
            e.UseMessageRetry(r => r.Intervals(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(60)));
        });

        cfg.ReceiveEndpoint("customer-notify-queue", e =>
            e.ConfigureConsumer<CustomerNotifierConsumer>(ctx));
    });
});

// ── Background services ───────────────────────────────────────────────────────
builder.Services.AddHostedService<TrackingPollerService>();

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173"];

builder.Services.AddCors(opt => opt.AddDefaultPolicy(p =>
    p.WithOrigins(allowedOrigins)
     .AllowAnyHeader()
     .AllowAnyMethod()
     .AllowCredentials()));

// ── Controllers + Health ──────────────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

// ── Startup: migrate + seed ───────────────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // EnsureCreatedAsync creates all tables from the EF model without requiring
    // migration files. Switch to MigrateAsync() once migrations are generated:
    //   docker run --rm -v $(pwd):/src mcr.microsoft.com/dotnet/sdk:8.0 \
    //     dotnet ef migrations add InitialCreate --project Src/DevolveFacill.Infrastructure \
    //     --startup-project Src/DevolveFacill.Api
    if (app.Environment.IsDevelopment())
        await db.Database.EnsureCreatedAsync();

    // Seed default Supervisor admin on first run (idempotent)
    if (!await db.AdminUsers.AnyAsync())
    {
        var seedEmail = app.Configuration["Seed:AdminEmail"] ?? "admin@lamoda.com.br";
        var seedPassword = app.Configuration["Seed:AdminPassword"]
            ?? throw new InvalidOperationException("Seed:AdminPassword is required");

        db.AdminUsers.Add(new AdminUser
        {
            Id = Guid.NewGuid(),
            Name = app.Configuration["Seed:AdminName"] ?? "Administrador",
            Email = seedEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(seedPassword),
            Role = app.Configuration["Seed:AdminRole"] ?? "Supervisor",
            Active = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync();
        Log.Information("Seeded default admin user {Email}", seedEmail);
    }
}

app.Run();
