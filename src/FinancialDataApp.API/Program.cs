using System.Text;
using FinancialDataApp.Core.Interfaces;
using FinancialDataApp.Infrastructure.Data;
using FinancialDataApp.Infrastructure.Repositories;
using FinancialDataApp.Infrastructure.Services;
using FinancialDataApp.Infrastructure.Ingestion;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using FinancialDataApp.API.Hosted;
using FinancialDataApp.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, lc) => lc.ReadFrom.Configuration(ctx.Configuration));

// DbContext
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// DI
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDataSourceService, DataSourceService>();
builder.Services.AddScoped<IRealTimeDataService, RealTimeDataService>();
builder.Services.AddScoped<IHealthService, HealthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IDynamicFieldService, DynamicFieldService>();
builder.Services.AddScoped<IExportService, ExportService>();
builder.Services.AddScoped<DemoDataSeeder>();

// SignalR
builder.Services.AddSignalR();

// Ingestion
builder.Services.Configure<IngestionOptions>(builder.Configuration.GetSection("Ingestion"));
builder.Services.AddHostedService<DataIngestionHostedService>();
builder.Services.AddHostedService<DemoSeederHostedService>();

// Auth (JWT)
var key = builder.Configuration["Jwt:Key"] ?? throw new Exception("Jwt:Key missing");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.TokenValidationParameters = new()
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            ValidateIssuer = false,
            ValidateAudience = false
        };
        o.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var accessToken = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

// Controllers + Swagger
builder.Services
    .AddControllers()
    // Ensure controllers are discovered even in published containers
    .AddApplicationPart(typeof(FinancialDataApp.API.Controllers.DataSourcesController).Assembly);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS from config
var allowedOrigins = builder.Configuration.GetSection("AllowedCorsOrigins").Get<string[]>() ?? Array.Empty<string>();
builder.Services.AddCors(p => p.AddPolicy("CorsPolicy", policy =>
    policy.WithOrigins(allowedOrigins)
          .AllowAnyHeader()
          .AllowAnyMethod()
          .AllowCredentials()));

// HealthChecks
builder.Services.AddHealthChecks().AddDbContextCheck<AppDbContext>("db");

var app = builder.Build();

/* Ensure database exists (create via master if missing), then initialize schema */
using (var scope = app.Services.CreateScope())
{
    var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    var defaultConn = configuration.GetConnectionString("Default");
    if (string.IsNullOrWhiteSpace(defaultConn))
        throw new Exception("ConnectionStrings:Default is not configured.");

    var csb = new SqlConnectionStringBuilder(defaultConn);
    var dbName = csb.InitialCatalog;
    if (string.IsNullOrWhiteSpace(dbName))
        throw new Exception("Connection string must include a Database/Initial Catalog.");

    // Connect to master and create DB if needed
    csb.InitialCatalog = "master";
    var masterConn = csb.ConnectionString;

    const int maxAttempts = 10;
    for (int attempt = 1; attempt <= maxAttempts; attempt++)
    {
        try
        {
            await using var conn = new SqlConnection(masterConn);
            await conn.OpenAsync();
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "IF DB_ID(@name) IS NULL EXEC('CREATE DATABASE [' + @name + ']')";
                cmd.Parameters.AddWithValue("@name", dbName);
                await cmd.ExecuteNonQueryAsync();
            }
            break;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "SQL Server not ready. Attempt {Attempt}/{MaxAttempts}. Retrying in 3s...", attempt, maxAttempts);
            if (attempt == maxAttempts) throw;
            await Task.Delay(TimeSpan.FromSeconds(3));
        }
    }

    // Apply pending EF Core migrations (preferred over EnsureCreated for prod-like flows)
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

app.UseSerilogRequestLogging();
app.UseCors("CorsPolicy");
app.UseAuthentication();
app.UseAuthorization();
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapControllers();
app.MapHub<DataHub>("/hubs/data");
app.MapHub<HealthHub>("/hubs/health");
app.MapHealthChecks("/health");
app.Run();

/* Minimal hub implementations to satisfy frontend subscriptions */
public class DataHub : Microsoft.AspNetCore.SignalR.Hub
{
    public System.Threading.Tasks.Task SubscribeToIngestionUpdates() => System.Threading.Tasks.Task.CompletedTask;
    public System.Threading.Tasks.Task SubscribeToHealthUpdates() => System.Threading.Tasks.Task.CompletedTask;

    public System.Threading.Tasks.Task SubscribeToDataSource(System.Guid dataSourceId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, $"ds:{dataSourceId}");

    public System.Threading.Tasks.Task UnsubscribeFromDataSource(System.Guid dataSourceId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, $"ds:{dataSourceId}");
}

public class HealthHub : Microsoft.AspNetCore.SignalR.Hub { }
