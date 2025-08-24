using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using SistemaVentas.Data;
using SistemaVentas.Models;


namespace SistemaVentas.Controllers
{
    public class ProductosController : Controller
    {
        private readonly ProductoRepositorio _productoRepositorio;
        private readonly ILogger<ProductosController> _logger; // Para depuracion

        public ProductosController(ProductoRepositorio productoRepositorio, ILogger<ProductosController> logger)
        {
            _productoRepositorio = productoRepositorio;
            _logger = logger; // Inyectar ILogger para depuracion
        }

        public async Task<IActionResult> Index(int numeroPagina = 1, int tamanoPagina = 10, string cadenaBusqueda = null, bool? estado = null)
        {
            var paginador = await _productoRepositorio.ObtenerTodos(numeroPagina, tamanoPagina, cadenaBusqueda, estado);
            return View(paginador);
        }

        public async Task<IActionResult> Crear()
        {
            var categorias = await _productoRepositorio.ObtenerCategorias();
            var categoriasList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Seleccione una categoría", Selected = true }
            };
            categoriasList.AddRange(categorias.Select(c => new SelectListItem
            {
                Value = c.IdCategoria.ToString(),
                Text = c.Nombre
            }));
            ViewBag.Categorias = categoriasList;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Crear(Producto producto)
        {
            if (ModelState.IsValid)
            {
                if (producto.IdCategoria == 0)
                {
                    ModelState.AddModelError("IdCategoria", "Debe seleccionar una categoría.");
                }
                else
                {
                    try
                    {
                        await _productoRepositorio.Crear(producto);
                        return RedirectToAction(nameof(Index));
                    }
                    catch (InvalidOperationException ex)
                    {
                        ModelState.AddModelError("", ex.Message);
                    }
                }
            }
            var categorias = await _productoRepositorio.ObtenerCategorias();
            var categoriasList = new List<SelectListItem>
            {
                new SelectListItem { Value = "", Text = "Seleccione una categoría", Selected = true }
            };
            categoriasList.AddRange(categorias.Select(c => new SelectListItem
            {
                Value = c.IdCategoria.ToString(),
                Text = c.Nombre
            }));
            ViewBag.Categorias = categoriasList;
            return View(producto);
        }

        public async Task<IActionResult> Editar(int id)
        {
            var producto = await _productoRepositorio.ObtenerPorId(id);
            if (producto == null)
            {
                return NotFound();
            }
            _logger.LogInformation("Editar GET - IdProducto: {Id}, IdCategoria: {IdCategoria}", id, producto.IdCategoria); // Depuración
            var categorias = await _productoRepositorio.ObtenerCategorias();
            var categoriasList = new List<SelectListItem>
    {
        new SelectListItem { Value = "", Text = "Seleccione una categoría" }
    };
            categoriasList.AddRange(categorias.Select(c => new SelectListItem
            {
                Value = c.IdCategoria.ToString(),
                Text = c.Nombre,
                Selected = c.IdCategoria == producto.IdCategoria // Selecciona la categoria actual
            }));
            ViewBag.Categorias = categoriasList;
            return View(producto);
        }

        [HttpPost]
        public async Task<IActionResult> Editar(Producto producto)
        {
            if (ModelState.IsValid)
            {
                if (producto.IdCategoria == 0)
                {
                    ModelState.AddModelError("IdCategoria", "Debe seleccionar una categoría.");
                }
                else
                {
                    try
                    {
                        await _productoRepositorio.Actualizar(producto);
                        return RedirectToAction(nameof(Index));
                    }
                    catch (InvalidOperationException ex)
                    {
                        ModelState.AddModelError("", ex.Message);
                    }
                }
            }
            _logger.LogInformation("Editar POST - IdProducto: {Id}, IdCategoria: {IdCategoria}", producto.IdProducto, producto.IdCategoria); // Depuración
            var categorias = await _productoRepositorio.ObtenerCategorias();
            var categoriasList = new List<SelectListItem>
    {
        new SelectListItem { Value = "", Text = "Seleccione una categoría" }
    };
            categoriasList.AddRange(categorias.Select(c => new SelectListItem
            {
                Value = c.IdCategoria.ToString(),
                Text = c.Nombre,
                Selected = c.IdCategoria == producto.IdCategoria // Selecciona la categoría actual en POST
            }));
            ViewBag.Categorias = categoriasList;
            return View(producto);
        }

        // GET: Mostrar la vista de eliminación
        public async Task<IActionResult> Eliminar(int id)
        {
            var producto = await _productoRepositorio.ObtenerPorId(id);
            if (producto == null)
            {
                return NotFound();
            }
            return View(producto);
        }

        // POST: Procesar la eliminación
        [HttpPost]
        [ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            try
            {
                await _productoRepositorio.Eliminar(id);
                return RedirectToAction(nameof(Index));
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 547) // Violación de restricción de clave foránea
            {
                ViewBag.ErrorMessage = "No se puede eliminar el producto porque está asociado a ventas en DetalleVenta.";
                var producto = await _productoRepositorio.ObtenerPorId(id);
                if (producto == null)
                {
                    return NotFound();
                }
                return View(producto); // Regresar a la vista con el mensaje de error
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Ocurrió un error inesperado: {ex.Message}";
                var producto = await _productoRepositorio.ObtenerPorId(id);
                if (producto == null)
                {
                    return NotFound();
                }
                return View(producto);
            }
        }
    }
}