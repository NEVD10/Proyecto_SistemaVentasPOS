using Microsoft.EntityFrameworkCore;
using SistemaVentas.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Configure DbContext
builder.Services.AddDbContext<DBContexto>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("BDContexto") ??
        throw new InvalidOperationException("Cadena de conexión 'BDContexto' no existe.")));

// Add repository
builder.Services.AddScoped<ClienteRepositorio>();

var app = builder.Build();

// Crear base de datos y sembrar datos iniciales
using (var scope = app.Services.CreateScope())
{
    var servicio = scope.ServiceProvider;
    var contexto = servicio.GetRequiredService<DBContexto>();
    contexto.Database.EnsureCreated();

}

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();