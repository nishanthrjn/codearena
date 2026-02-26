// Placeholder for Program.cs
using System.Text;
using System.Threading.RateLimiting;
using CodeArena.Api.Data;
using CodeArena.Api.Hubs;
using CodeArena.Api.Middleware;
using CodeArena.Api.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext} {Message:lj}{NewLine}{Exception}")
    .CreateLogger();
builder.Host.UseSerilog();

// ── Services ─────────────────────────────────────────────────────────────────
var cfg = builder.Configuration;

builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(cfg.GetConnectionString("Postgres")));

// Redis
var redis = ConnectionMultiplexer.Connect(cfg.GetConnectionString("Redis")!);
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// Data Protection (for GitHub token encryption)
// L-1: Verify dp-keys volume is mounted before starting — avoids silent data loss
var dpKeysPath = "/app/dp-keys";
var dpKeysDir  = new DirectoryInfo(dpKeysPath);
if (!dpKeysDir.Exists)
{
    try { dpKeysDir.Create(); }
    catch (Exception ex)
    {
        Log.Fatal(ex, "Cannot create Data Protection keys directory at {Path}. " +
                      "Ensure the dp-keys volume is mounted correctly.", dpKeysPath);
        throw;
    }
}
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(dpKeysDir)
    .SetApplicationName("CodeArena")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(365));

// ── Cookie policy (required for HttpOnly JWT cookie) ─────────────────────────
builder.Services.AddAntiforgery();
builder.Services.Configure<CookiePolicyOptions>(o =>
{
    o.HttpOnly   = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always;
    // M-3: Always require Secure flag — HTTPS must be terminated by the reverse proxy in prod
    o.Secure     = CookieSecurePolicy.Always;
    o.MinimumSameSitePolicy = SameSiteMode.Strict;
});

// ── Auth – JWT (reads from Authorization header OR ca_jwt HttpOnly cookie) ───
var jwtKey = cfg["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key missing");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer           = true,
            ValidateAudience         = true,
            ValidateLifetime         = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer              = cfg["Jwt:Issuer"],
            ValidAudience            = cfg["Jwt:Audience"],
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
        // H-2: Accept token ONLY from the HttpOnly cookie (ca_jwt).
        // Cookies are automatically sent on WebSocket upgrades for /hubs/* too,
        // so no query-string fallback is needed.
        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var cookieToken = ctx.Request.Cookies["ca_jwt"];
                if (!string.IsNullOrEmpty(cookieToken))
                    ctx.Token = cookieToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

// ── Rate limiting (M-1) ──────────────────────────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // General API limit: 60 req/min per authenticated user (by IP as fallback)
    opts.AddPolicy("api", ctx =>
    {
        var userId = ctx.User?.FindFirst("sub")?.Value
                  ?? ctx.Connection.RemoteIpAddress?.ToString()
                  ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter(userId, _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit          = 60,
            Window               = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit           = 0
        });
    });

    // Stricter limit for execution endpoints: 10 runs/min per user
    opts.AddPolicy("execution", ctx =>
    {
        var userId = ctx.User?.FindFirst("sub")?.Value
                  ?? ctx.Connection.RemoteIpAddress?.ToString()
                  ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter($"exec:{userId}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit          = 10,
            Window               = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit           = 0
        });
    });

    // Very strict limit for OAuth login: 10 initiations/min per IP
    opts.AddPolicy("auth", ctx =>
    {
        var ip = ctx.Connection.RemoteIpAddress?.ToString() ?? "anon";
        return RateLimitPartition.GetFixedWindowLimiter($"auth:{ip}", _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit          = 10,
            Window               = TimeSpan.FromMinutes(1),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit           = 0
        });
    });
});

// App services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ISnippetService, SnippetService>();
builder.Services.AddScoped<IExecutionService, ExecutionService>();
builder.Services.AddScoped<IGitHubService, GitHubService>();
builder.Services.AddSingleton<ITokenEncryptionService, TokenEncryptionService>();
builder.Services.AddHttpClient();

// SignalR
builder.Services.AddSignalR(opts => { opts.EnableDetailedErrors = builder.Environment.IsDevelopment(); });

// Validators
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Controllers + Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "CodeArena API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In   = ParameterLocation.Header, Name = "Authorization",
        Type = SecuritySchemeType.Http, Scheme = "bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } }] = []
    });
});

// CORS – frontend origin (credentials required for cookie auth)
builder.Services.AddCors(opts => opts.AddDefaultPolicy(p =>
    p.WithOrigins(cfg["AllowedOrigins"]?.Split(',') ?? ["http://localhost:5173"])
     .AllowAnyHeader().AllowAnyMethod().AllowCredentials()));

// Health checks
builder.Services.AddHealthChecks()
    .AddNpgSql(cfg.GetConnectionString("Postgres")!)
    .AddRedis(cfg.GetConnectionString("Redis")!);

// ── Build ─────────────────────────────────────────────────────────────────────
var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    // M-3: Enforce HTTPS in production via HSTS
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseCookiePolicy();
app.UseRateLimiter();   // M-1: rate limiting
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ExecutionHub>("/hubs/execution");
app.MapHealthChecks("/health");

// M-4: Only auto-migrate in development; use a migration runner in production CI/CD
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (app.Environment.IsDevelopment())
        db.Database.Migrate();
}

app.Run();