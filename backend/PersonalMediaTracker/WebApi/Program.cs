using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext via DI using the connection string
var connStr = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Missing 'DefaultConnection'.");
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseSqlServer(connStr));
builder.Services.AddScoped<TagSyncService>();

// DEV: wide-open CORS so the frontend on another port can call the API.
// TODO: PROD: replae AllowAnyOrigin() with WthOrigins("https://your-frontend.com")
builder.Services.AddCors(p => p.AddDefaultPolicy(policy => policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

// quick health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok", timeUtc = DateTime.UtcNow }));

app.UseAuthorization();

app.MapControllers();
app.Run();
