using Asp.Versioning;
using Microsoft.AspNetCore.Authentication;
using Microsoft.OpenApi;
using StemCellsPro.Api.Authentication;
using StemCellsPro.Api.Middlewares;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Infrastructure.Data;
using StemCellsPro.Infrastructure.Repositories;
using StemCellsPro.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "Opaque",
        In = ParameterLocation.Header,
        Description = "Enter the APICallDB token as: Bearer {token}"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
});

builder.Services
    .AddAuthentication(ApiTokenAuthenticationHandler.SchemeName)
    .AddScheme<AuthenticationSchemeOptions, ApiTokenAuthenticationHandler>(
        ApiTokenAuthenticationHandler.SchemeName,
        options => { });

builder.Services.AddAuthorization();

// Enable CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// --- ENTERPRISE FEATURES ---

// 0. Mayan EDMS Configuration
builder.Services.Configure<StemCellsPro.Application.Configuration.MayanEdmsSettings>(builder.Configuration.GetSection("MayanEdms"));
builder.Services.AddHttpClient<IMayanEdmsService, MayanEdmsService>();

// 1. API Versioning
builder.Services.AddApiVersioning(options =>
{
    options.DefaultApiVersion = new ApiVersion(1, 0);
    options.AssumeDefaultVersionWhenUnspecified = true;
    options.ReportApiVersions = true;
}).AddMvc().AddApiExplorer(options =>
{
    options.GroupNameFormat = "'v'VVV";
    options.SubstituteApiVersionInUrl = true;
});

// 2. Health Checks
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrEmpty(defaultConnection))
{
    throw new InvalidOperationException("The connection string 'DefaultConnection' was not found or is empty. Please check your configuration.");
}
builder.Services.AddHealthChecks()
    .AddSqlServer(defaultConnection);

// 3. In-Memory Distributed Caching (Replaced Redis for local development)
builder.Services.AddDistributedMemoryCache();

// 4. Hangfire Background Jobs
//builder.Services.AddHangfire(config => config
//    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
//    .UseSimpleAssemblyNameTypeSerializer()
//    .UseRecommendedSerializerSettings()
//    .UseSqlServerStorage(builder.Configuration.GetConnectionString("DefaultConnection")));
//builder.Services.AddHangfireServer();

// --- DI REGISTRATION ---
builder.Services.AddScoped<DapperContext>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
builder.Services.AddScoped<IFormDataRepository, FormDataRepository>();
builder.Services.AddScoped<IAttachmentsRepository, AttachmentsRepository>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
var enableSwagger = builder.Configuration.GetValue<bool>("EnableSwagger");
if (enableSwagger)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Enable CORS before custom middlewares
app.UseCors("AllowAll");

// Global Exception Handler
app.UseMiddleware<ExceptionMiddleware>();

// Tenant Resolver Middleware
app.UseMiddleware<TenantMiddleware>();

app.UseAuthentication();
app.UseAuthorization();

// Health Check Endpoint
app.MapHealthChecks("/api/health");

// Hangfire Dashboard (typically protected by auth in production)
//app.UseHangfireDashboard("/hangfire");

app.MapControllers();

app.Run();


