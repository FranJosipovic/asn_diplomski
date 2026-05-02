using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Application.UseCases.ConnectDevice;
using Microsoft.EntityFrameworkCore;
using Asn.Diplomski.Application.UseCases.CreateDeviceWithSensors;
using Asn.Diplomski.Application.UseCases.CreateTenant;
using Asn.Diplomski.Application.UseCases.GetTenantById;
using Asn.Diplomski.Application.UseCases.HandleSoilMoisture;
using Asn.Diplomski.Application.UseCases.HandleWaterLevel;
using Asn.Diplomski.Rdbm;
using Asn.Diplomski.Server;
using Asn.Diplomski.Server.Mqtt;
using Microsoft.OpenApi;
using System.Reflection;

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
builder.Services.AddScoped<ConnectDeviceHandler>();
builder.Services.AddScoped<HandleSoilMoistureHandler>();
builder.Services.AddScoped<HandleWaterLevelHandler>();

// ── Controllers ─────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// ── Swagger ─────────────────────────────────────────────────
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Asn.Diplomski API",
        Version = "v1",
        Description = "IoT navodnjavanje — SaaS backend"
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
    var tenantRepository    = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
    var loggerFactory       = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
    var seederLogger        = loggerFactory.CreateLogger(nameof(DbSeeder));

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
app.UseAuthorization();
app.MapControllers();

app.Run();
