using System.Text;
using System.Text.Json.Serialization;
using Domain.Entities.Identity;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using WebApi.Services;
using WebApi.Services.Identity;

// Create builder
var builder = WebApplication.CreateBuilder(args);

// Controllers + enums as strings in JSON
builder.Services.AddControllers().AddJsonOptions(o =>
{
    o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext via DI using the connection string from apsettings.json
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing 'DefaultConnection'.");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connStr));

// Identity Core
builder.Services.AddIdentityCore<ApplicationUser>(opt =>
    {
        // Reasonable dev defaults, harden later
        opt.Password.RequiredLength = 6;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequireUppercase = false;
        opt.Lockout.MaxFailedAccessAttempts = 5;
    })
    .AddRoles<IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddSignInManager();

// JWT Bearer Authentication
var jwt = builder.Configuration.GetSection("Jwt");
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Validate typical token fields
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwt["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(2)
        };
    });

builder.Services.AddAuthorization();

// App services
builder.Services.AddScoped<TagSyncService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>(); // token generator for AuthController

// CORS: allow your local front-end dev servers
builder.Services.AddCors(opts =>
{
    opts.AddPolicy("Frontend", p => p
        .WithOrigins(
            "http://localhost:5500",    // VS Code Live Server
            "http://127.0.0.1:5500",
            "http://localhost:5173",    // Vite/React
            "https://localhost:5173",
            "http://localhost:3000",
            "https://localhost:3000")
        .AllowAnyHeader()
        .AllowAnyMethod());
});

var app = builder.Build();

// Dev tools
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// CORS must be before auth for preflights
app.UseCors("Frontend");

// AuthN / AuthZ
app.UseAuthentication();
app.UseAuthorization();

// quick health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok", timeUtc = DateTime.UtcNow }));

// Controllers
app.MapControllers();

app.Run();
