using Microsoft.AspNetCore.Mvc;
using SistemaVentas.Data;
using SistemaVentas.Models;

namespace SistemaVentas.Controllers
{
    public class ClientesController : Controller
    {
        private readonly ClienteRepositorio _repositorio;

        public ClientesController(ClienteRepositorio repositorio)
        {
            _repositorio = repositorio;
        }

        // GET: Clientes
        public async Task<IActionResult> Index(int numeroPagina = 1, string cadenaBusqueda = null, string tipoDocumento = null, int tamanoPagina = 15)
        {
            var paginador = await _repositorio.ObtenerTodos(numeroPagina, tamanoPagina, cadenaBusqueda, tipoDocumento);
            ViewData["CadenaBusquedaActual"] = cadenaBusqueda;
            ViewData["TipoDocumentoActual"] = tipoDocumento; // Para mantener el valor seleccionado
            return View(paginador);
        }

        // GET: Clientes/Crear
        public IActionResult Crear()
        {
            return View();
        }

        // POST: Clientes/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(Cliente cliente)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _repositorio.Crear(cliente);
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("NumeroDocumento", ex.Message);
                }
            }
            return View(cliente);
        }

        // GET: Clientes/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var cliente = await _repositorio.ObtenerPorId(id);
            if (cliente == null)
            {
                return NotFound();
            }
            return View(cliente);
        }

        // POST: Clientes/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, Cliente cliente)
        {
            if (id != cliente.IdCliente)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _repositorio.Actualizar(cliente);
                    return RedirectToAction(nameof(Index));
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("NumeroDocumento", ex.Message);
                }
            }
            return View(cliente);
        }

        // GET: Clientes/Eliminar/5
        public async Task<IActionResult> Eliminar(int id)
        {
            var cliente = await _repositorio.ObtenerPorId(id);
            if (cliente == null)
            {
                return NotFound();
            }
            return View(cliente);
        }

        // POST: Clientes/Eliminar/5
        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            await _repositorio.Eliminar(id);
            return RedirectToAction(nameof(Index));
        }
    }
}