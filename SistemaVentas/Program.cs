using Microsoft.EntityFrameworkCore;
using SistemaVentas.Data;
using SistemaVentas.Services;

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

builder.Services.AddScoped<IClienteRepositorio, ClienteRepositorio>();
builder.Services.AddScoped<IProductoRepositorio, ProductoRepositorio>();
builder.Services.AddScoped<IServicioFacturacion, ServicioFacturacion>();
builder.Services.AddScoped<IVentaRepositorio, VentaRepositorio>();
builder.Services.AddScoped<IServicioCorreo, ServicioCorreo>();
builder.Services.AddScoped<IServicioReporte, ServicioReporte>();
builder.Services.Configure<ConfiguracionCorreo>(builder.Configuration.GetSection("ConfiguracionCorreo"));

// Registro de generadores de PDF (asegúrate de que existan)
builder.Services.AddScoped<BoletaPdfGenerator>();
builder.Services.AddScoped<FacturaPdfGenerator>();

var app = builder.Build();

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
        // Registrar el error para depuración
        Console.WriteLine($"Error al crear la base de datos: {ex.Message}");
        throw;
    }
}


// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();