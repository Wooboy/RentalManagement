using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using RentalManager.Api.Data;
using RentalManager.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<DbSeedService>();

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

app.UseCors("frontend");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
