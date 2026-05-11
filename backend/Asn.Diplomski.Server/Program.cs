using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Application.UseCases.ConnectDevice;
using Asn.Diplomski.Application.UseCases.ConnectTenant;
using Asn.Diplomski.Application.UseCases.CreateDeviceWithSensors;
using Asn.Diplomski.Application.UseCases.CreateTenant;
using Asn.Diplomski.Application.UseCases.CompleteDeviceProvisioning;
using Asn.Diplomski.Application.UseCases.ConfirmMqttConnection;
using Asn.Diplomski.Application.UseCases.HandleDeviceStatus;
using Asn.Diplomski.Application.UseCases.RequestReprovision;
using Asn.Diplomski.Application.UseCases.SetDeviceProvisioningReady;
using Asn.Diplomski.Application.UseCases.GetProvisioningToken;
using Asn.Diplomski.Application.UseCases.GetSoilMoistureReadings;
using Asn.Diplomski.Application.UseCases.GetTemperatureReadings;
using Asn.Diplomski.Application.UseCases.GetTenantById;
using Asn.Diplomski.Application.UseCases.HandleSoilMoisture;
using Asn.Diplomski.Application.UseCases.HandleTemperature;
using Asn.Diplomski.Application.UseCases.HandleWaterLevel;
using Asn.Diplomski.Application.UseCases.RefreshToken;
using Asn.Diplomski.Application.UseCases.SignIn;
using Asn.Diplomski.Rdbm;
using Asn.Diplomski.Rdbm.Repositories;
using Asn.Diplomski.Server;
using Asn.Diplomski.Server.Mqtt;
using Asn.Diplomski.Server.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using System.Reflection;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ── CORS ────────────────────────────────────────────────────
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins,
        policy => policy
            .WithOrigins("*")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// ── RDBM / EF + Repositories ────────────────────────────────
builder.Services.AddRdbm(builder.Configuration);

// ── MQTT Infrastructure ──────────────────────────────────────
builder.Services.AddSingleton<MqttIncomingChannel>();
builder.Services.AddSingleton<MqttOutgoingChannel>();
builder.Services.AddSingleton<MqttService>();
builder.Services.AddSingleton<IMqttSubscriber>(sp => sp.GetRequiredService<MqttService>());
builder.Services.AddSingleton<IMqttPublisher, MqttPublisher>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<MqttService>());
builder.Services.AddHostedService<MqttIncomingWorker>();
builder.Services.AddHostedService<MqttOutgoingWorker>();

// ── Application Use Cases ────────────────────────────────────
builder.Services.AddScoped<CreateDeviceWithSensorsHandler>();
builder.Services.AddScoped<CreateTenantHandler>();
builder.Services.AddScoped<GetTenantByIdHandler>();
builder.Services.AddScoped<ConnectTenantHandler>();
builder.Services.AddScoped<ConnectDeviceHandler>();
builder.Services.AddScoped<HandleSoilMoistureHandler>();
builder.Services.AddScoped<HandleWaterLevelHandler>();
builder.Services.AddScoped<HandleTemperatureHandler>();
builder.Services.AddScoped<GetTemperatureReadingsHandler>();
builder.Services.AddScoped<GetSoilMoistureReadingsHandler>();
builder.Services.AddScoped<SignInHandler>();
builder.Services.AddScoped<RefreshTokenHandler>();
builder.Services.AddScoped<GetProvisioningTokenHandler>();
builder.Services.AddScoped<CompleteDeviceProvisioningHandler>();
builder.Services.AddScoped<ConfirmMqttConnectionHandler>();
builder.Services.AddScoped<HandleDeviceStatusHandler>();
builder.Services.AddScoped<RequestReprovisionHandler>();
builder.Services.AddScoped<SetDeviceProvisioningReadyHandler>();

// ── Authentication / Authorization ──────────────────────────
builder.Services.AddScoped<ITokenService, TokenService>();

var jwtSecret = builder.Configuration["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("Jwt:Audience not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// ── Controllers ─────────────────────────────────────────────
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();

// ── Repositories ─────────────────────────────────────────────
builder.Services.AddScoped<ITemperatureReadingRepository, TemperatureReadingRepository>();
builder.Services.AddScoped<ISoilMoistureReadingRepository, SoilMoistureReadingRepository>();

// ── Swagger ─────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Asn.Diplomski API",
        Version = "v1",
        Description = "IoT navodnjavanje — SaaS backend"
    });

    // JWT Authorization
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter JWT token",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    };
    c.AddSecurityDefinition("bearer", securityScheme);

    c.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("bearer", document)] = []
    });

    // XML komentari (opcionalno, ali preporučeno)
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

// ── Auto-migracija + seed pri startu ────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AsnDbContext>();
    db.Database.Migrate();

    var createTenantHandler = scope.ServiceProvider.GetRequiredService<CreateTenantHandler>();
    var tenantRepository = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
    var loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var seederLogger = loggerFactory.CreateLogger(nameof(DbSeeder));

    await DbSeeder.SeedAsync(createTenantHandler, tenantRepository, seederLogger);
}

// ── Middleware ───────────────────────────────────────────────
app.UseCors(MyAllowSpecificOrigins);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Asn.Diplomski API v1");
        c.RoutePrefix = string.Empty; // Swagger na root /
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
