using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using SistemaVentas.Data;
using SistemaVentas.Models;
using SistemaVentas.Services;
using BCrypt.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure DbContext
builder.Services.AddDbContext<DBContexto>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BDContexto") ??
        throw new InvalidOperationException("Cadena de conexión 'BDContexto' no existe.")));

// Añadir repository
builder.Services.AddScoped<VentaRepositorio>();
builder.Services.AddScoped<ProductoRepositorio>();
builder.Services.AddScoped<ClienteRepositorio>();
builder.Services.AddScoped<UsuarioRepositorio>();

builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<IProductoRepositorio, ProductoRepositorio>();
builder.Services.AddScoped<IServicioFacturacion, ServicioFacturacion>();
builder.Services.AddScoped<IVentaRepositorio, VentaRepositorio>();
builder.Services.AddScoped<IServicioCorreo, ServicioCorreo>();
builder.Services.AddScoped<IServicioReporte, ServicioReporte>();
builder.Services.AddScoped<ServicioUsuario, ServicioUsuario>();
builder.Services.Configure<ConfiguracionCorreo>(builder.Configuration.GetSection("ConfiguracionCorreo"));

// Registro de generadores de PDF
builder.Services.AddScoped<BoletaPdfGenerator>();
builder.Services.AddScoped<FacturaPdfGenerator>();

// Añadir autenticación basada en cookies sin persistencia entre reinicios
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Usuarios/IniciarSesion";
        options.AccessDeniedPath = "/Usuarios/AccesoDenegado";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(15); // Tiempo de vida de la cookie
        options.SlidingExpiration = true; // Renueva el tiempo si hay actividad
        options.Cookie.IsEssential = true; // Acepta cookies sin consentimiento
        options.Cookie.MaxAge = TimeSpan.FromMinutes(15); // Limita la vida máxima de la cookie
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Requiere HTTPS (ajusta según tu entorno)
        options.Cookie.HttpOnly = true; // Solo accesible por el servidor
        options.Events.OnValidatePrincipal = context =>
        {
            // Forzar expiración si pasa el tiempo de inactividad
            if (context.Properties.IssuedUtc.HasValue &&
                context.Properties.IssuedUtc.Value.AddMinutes(15) < DateTimeOffset.UtcNow)
            {
                context.RejectPrincipal(); // Expira la sesión
            }
            return Task.CompletedTask;
        };
    });

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(15); // Tiempo de inactividad antes de expirar
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.MaxAge = TimeSpan.FromMinutes(15); // Limita la vida de la cookie de sesión
});

// Inicializar la aplicación
var app = builder.Build();

// Inicializar usuarios predeterminados de forma síncrona
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var usuarioRepositorio = services.GetRequiredService<UsuarioRepositorio>();

    var adminTask = usuarioRepositorio.ObtenerPorNombreUsuario("admin1");
    var vendedorTask = usuarioRepositorio.ObtenerPorNombreUsuario("vendedor1");
    Task.WaitAll(adminTask, vendedorTask);
    var admin = adminTask.Result;
    var vendedor = vendedorTask.Result;

    if (admin == null)
    {
        var nuevoAdmin = new Usuario
        {
            NombreUsuario = "admin1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"),
            NombreCompleto = "Administrador Principal",
            Rol = "Administrador",
            Estado = true
        };
        usuarioRepositorio.CrearUsuario(nuevoAdmin).Wait();
    }

    if (vendedor == null)
    {
        var nuevoVendedor = new Usuario
        {
            NombreUsuario = "vendedor1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("vendedor123"),
            NombreCompleto = "Vendedor Ejemplo",
            Rol = "Vendedor",
            Estado = true
        };
        usuarioRepositorio.CrearUsuario(nuevoVendedor).Wait();
    }
}

// Crear base de datos y sembrar datos iniciales
using (var scope = app.Services.CreateScope())
{
    var contexto = scope.ServiceProvider.GetRequiredService<DBContexto>();
    try
    {
        contexto.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error al crear la base de datos: {ex.Message}");
        throw;
    }
}

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Usuarios}/{action=IniciarSesion}/{id?}");

app.Run();