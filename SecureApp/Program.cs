using SecureApp.Data;
using SecureApp.Helpers;
using SecureApp.Services;
using Microsoft.EntityFrameworkCore;
using AspNetCoreRateLimit;

var builder = WebApplication.CreateBuilder(args);

// Configuración de la base de datos
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrar servicios
builder.Services.AddScoped<JwtHelper>();
builder.Services.AddScoped<ValidationService>();

// Configuración de límites de solicitudes
builder.Services.AddMemoryCache();
builder.Services.AddInMemoryRateLimiting();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();

// Configuración de reglas específicas para rutas críticas
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/register",
            Period = "10m",
            Limit = 3
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/auth/login",
            Period = "5m",
            Limit = 5
        }
    };
});


builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowSpecificOrigins", policy =>
        policy.WithOrigins("https://chascamain.shop/'") 
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // Permite cookies/autenticación
});


// Configuración de Swagger/OpenAPI
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Middleware para manejar excepciones globales
app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        context.Response.StatusCode = 500;
        await context.Response.WriteAsJsonAsync(new { message = "Ocurrió un error en el servidor. Inténtalo más tarde." });
    });
});

// Crear la base de datos si no existe
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    context.Database.EnsureCreated();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}


// Middleware de CORS
app.UseCors("AllowSpecificOrigins");

app.UseHttpsRedirection();

// Middleware de límites de solicitudes
app.UseIpRateLimiting();

app.UseAuthorization();

app.MapControllers();

app.Run();
