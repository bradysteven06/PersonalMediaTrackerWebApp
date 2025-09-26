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

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();


app.UseCors("Frontend");   // <-- CORS must run before MapControllers()

// quick health endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok", timeUtc = DateTime.UtcNow }));

app.UseAuthorization();

app.MapControllers();
app.Run();
