using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RentalManager.Api.Common;
using RentalManager.Api.Data;
using RentalManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<DbSeedService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<AdminUserService>();
builder.Services.AddScoped<TenantService>();
builder.Services.AddScoped<PropertyService>();
builder.Services.AddScoped<RoomService>();
builder.Services.AddScoped<ContractService>();
builder.Services.AddScoped<ChargeService>();
builder.Services.AddScoped<ExpenseService>();
builder.Services.AddScoped<ElectricityBillingService>();
builder.Services.AddScoped<MeterReadingService>();
builder.Services.AddScoped<ReportService>();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "super-secret-key-change-me";
var issuer = builder.Configuration["Jwt:Issuer"] ?? "RentalManager";
var audience = builder.Configuration["Jwt:Audience"] ?? "RentalManagerClient";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddCors(options =>
{
    options.AddPolicy("frontend", p => p.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var seedService = scope.ServiceProvider.GetRequiredService<DbSeedService>();
    db.Database.Migrate();

    var exportSeedPath = args.SkipWhile(x => x != "--export-seed").Skip(1).FirstOrDefault();
    if (args.Contains("--export-seed"))
    {
        await seedService.ExportAsync(exportSeedPath);
        return;
    }

    await seedService.ApplyTemplateIfEmptyAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    // OpenAPI 文件（開發環境）：GET /openapi/v1.json
    app.MapOpenApi();
}

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
