using Asp.Versioning;
using StemCellsPro.Api.Middlewares;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Infrastructure.Data;
using StemCellsPro.Infrastructure.Repositories;
using StemCellsPro.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
builder.Services.AddHealthChecks()
    .AddSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? "");

// 3. Redis Distributed Caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("RedisConnection");
    options.InstanceName = "ERP_";
});

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
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Global Exception Handler
app.UseMiddleware<ExceptionMiddleware>();

// Tenant Resolver Middleware
app.UseMiddleware<TenantMiddleware>();

// Custom Authentication Middleware
app.UseMiddleware<CustomAuthMiddleware>();

app.UseAuthorization();

// Health Check Endpoint
app.MapHealthChecks("/api/health");

// Hangfire Dashboard (typically protected by auth in production)
//app.UseHangfireDashboard("/hangfire");

app.MapControllers();

app.Run();
