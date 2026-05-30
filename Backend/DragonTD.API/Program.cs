using DragonTD.API.Data;
using DragonTD.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<DragonService>();
builder.Services.AddScoped<GachaService>();
builder.Services.AddScoped<JwtTokenService>();
builder.Services.AddSingleton<IIapPlatformReceiptValidator, GooglePlayReceiptValidator>();
builder.Services.AddSingleton<IIapPlatformReceiptValidator, AppleReceiptValidator>();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

string jwtSecret = builder.Configuration["Jwt:Secret"] ?? string.Empty;
if (Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    jwtSecret = "dragon-dominion-dev-secret-change-me-32";
var jwtOptions = new JwtTokenOptions(
    jwtSecret,
    builder.Configuration["Jwt:Issuer"] ?? "DragonTD.API",
    builder.Configuration["Jwt:Audience"] ?? "DragonTD.Client");
builder.Services.AddSingleton(jwtOptions);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.Secret))
        };
    });
builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Auto-migrate on startup in Production — EF migrations are idempotent
if (app.Environment.IsProduction())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();

public partial class Program { }
