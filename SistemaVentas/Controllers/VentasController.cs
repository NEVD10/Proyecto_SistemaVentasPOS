using Microsoft.AspNetCore.Mvc;
using SistemaVentas.Data;
using SistemaVentas.Models;
using SistemaVentas.Services;
using System.Text.Json;

namespace SistemaVentas.Controllers
{
    public class VentasController : Controller
    {
        private readonly VentaRepositorio _ventaRepositorio;
        private readonly ProductoRepositorio _productoRepositorio;
        private readonly ClienteRepositorio _clienteRepositorio;
        private readonly FacturaPdfGenerator _facturaPdfGenerator;
        private readonly BoletaPdfGenerator _boletaPdfGenerator;
        private readonly ILogger<VentasController> _logger;

        public VentasController(
            VentaRepositorio ventaRepositorio,
            ProductoRepositorio productoRepositorio,
            ClienteRepositorio clienteRepositorio,
            FacturaPdfGenerator facturaPdfGenerator,
            BoletaPdfGenerator boletaPdfGenerator,
            ILogger<VentasController> logger)
        {
            _ventaRepositorio = ventaRepositorio;
            _productoRepositorio = productoRepositorio;
            _clienteRepositorio = clienteRepositorio;
            _facturaPdfGenerator = facturaPdfGenerator;
            _boletaPdfGenerator = boletaPdfGenerator;
            _logger = logger;
        }

        public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin, int? clienteId, int pagina = 1, int tamanoPagina = 10)
        {
            //fechaInicio ??= DateTime.Today.AddMonths(-1);
            //fechaFin ??= DateTime.Today;
            tamanoPagina = tamanoPagina is 5 or 10 or 25 or 50 ? tamanoPagina : 10;
            pagina = pagina > 0 ? pagina : 1;

            var paginador = await _ventaRepositorio.ObtenerTodos(pagina, tamanoPagina, fechaInicio, fechaFin, clienteId);
            var ventas = paginador.Elementos.ToList();

            var totalVentasGeneral = await _ventaRepositorio.ContarTotal(fechaInicio, fechaFin, clienteId);
            var montoTotalGeneral = await _ventaRepositorio.SumarMontoTotal(fechaInicio, fechaFin, clienteId);
            var promedioGeneral = totalVentasGeneral > 0 ? montoTotalGeneral / totalVentasGeneral : 0;

            ViewBag.FechaInicio = fechaInicio;
            ViewBag.FechaFin = fechaFin;
            ViewBag.ClienteId = clienteId;
            ViewBag.PaginaActual = pagina;
            ViewBag.TamanoPagina = tamanoPagina;
            ViewBag.TotalPaginas = paginador.TotalPaginas;
            ViewBag.TotalRegistros = paginador.TotalRegistros;
            ViewBag.TieneAnterior = paginador.TienePaginaAnterior;
            ViewBag.TieneSiguiente = paginador.TienePaginaSiguiente;
            ViewBag.TotalVentasGeneral = totalVentasGeneral;
            ViewBag.MontoTotalGeneral = montoTotalGeneral;
            ViewBag.PromedioGeneral = promedioGeneral;
            ViewBag.TotalVentasPagina = ventas.Count;
            ViewBag.Clientes = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewBag.ProductosBusqueda = new List<Producto>();
            ViewBag.ClientesBusqueda = new List<Cliente>();

            return View(paginador);
        }

        public async Task<IActionResult> Details(int id)
        {
            var venta = await _ventaRepositorio.ObtenerPorId(id);
            if (venta == null)
            {
                TempData["Error"] = "Venta no encontrada.";
                return RedirectToAction("Index");
            }

            ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewBag.ProductosBusqueda = new List<Producto>();
            ViewBag.ClientesBusqueda = new List<Cliente>();
            return View(venta);
        }

        public async Task<IActionResult> VerFactura(int id)
        {
            var venta = await _ventaRepositorio.ObtenerPorId(id);
            if (venta == null)
            {
                TempData["Error"] = "Venta no encontrada.";
                return RedirectToAction("Index");
            }

            var productos = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            byte[] pdfBytes;

            if (venta.TipoComprobante == "Factura")
            {
                pdfBytes = _facturaPdfGenerator.GeneratePdf(venta, productos);
                return File(pdfBytes, "application/pdf", $"Factura_F001-{venta.IdVenta:D6}.pdf");
            }
            else
            {
                pdfBytes = _boletaPdfGenerator.GeneratePdf(venta, productos);
                return File(pdfBytes, "application/pdf", $"Boleta_B001-{venta.IdVenta:D6}.pdf");
            }
        }

        public async Task<IActionResult> Crear()
        {
            var viewModel = ObtenerVentaDeTempData() ?? new Venta
            {
                DetalleVentas = new List<DetalleVenta>(),
                FechaVenta = DateTime.Now,
                TipoComprobante = "Boleta",
                MetodoPago = "Efectivo"
            };
            ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewBag.ProductosBusqueda = new List<Producto>();
            ViewBag.ClientesBusqueda = new List<Cliente>();
            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> BuscarProducto(string buscarProducto, string tipoComprobante, string metodoPago, int? idCliente)
        {
            var viewModel = ObtenerVentaDeTempData() ?? new Venta { DetalleVentas = new List<DetalleVenta>() };
            viewModel.TipoComprobante = tipoComprobante;
            viewModel.MetodoPago = metodoPago;
            viewModel.IdCliente = idCliente;
            RecalcularTotales(viewModel);

            var productos = new List<Producto>();
            if (!string.IsNullOrEmpty(buscarProducto))
            {
                var productoExacto = await _productoRepositorio.ObtenerPorCodigoBarras(buscarProducto);
                if (productoExacto != null && productoExacto.IdProducto != 0)
                {
                    productos.Add(productoExacto);
                }
                else
                {
                    productos = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue, buscarProducto)).Elementos.ToList();
                }
                _logger.LogInformation("Productos encontrados: {Count}", productos.Count);
            }
            ViewBag.ProductosBusqueda = productos;
            ViewBag.ClientesBusqueda = new List<Cliente>();

            GuardarVentaEnTempData(viewModel);
            ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            return View("Crear", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> BuscarCliente(string buscarCliente, string tipoComprobante, string metodoPago, int? idCliente)
        {
            var viewModel = ObtenerVentaDeTempData() ?? new Venta { DetalleVentas = new List<DetalleVenta>() };
            viewModel.TipoComprobante = tipoComprobante;
            viewModel.MetodoPago = metodoPago;
            viewModel.IdCliente = idCliente;
            RecalcularTotales(viewModel);

            var clientes = new List<Cliente>();
            if (!string.IsNullOrEmpty(buscarCliente))
            {
                clientes = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue, buscarCliente)).Elementos.ToList();
                _logger.LogInformation("Clientes encontrados: {Count}", clientes.Count);
            }
            ViewBag.ClientesBusqueda = clientes;
            ViewBag.ProductosBusqueda = new List<Producto>();

            GuardarVentaEnTempData(viewModel);
            ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            return View("Crear", viewModel);
        }

        public async Task<IActionResult> AgregarProducto(int id, int cantidad, string tipoComprobante, string metodoPago, int? idCliente)
        {
            var viewModel = ObtenerVentaDeTempData() ?? new Venta { DetalleVentas = new List<DetalleVenta>() };
            viewModel.TipoComprobante = tipoComprobante;
            viewModel.MetodoPago = metodoPago;
            viewModel.IdCliente = idCliente;

            var producto = await _productoRepositorio.ObtenerPorId(id);
            if (producto != null && cantidad > 0 && producto.Stock >= cantidad)
            {
                var detalleExistente = viewModel.DetalleVentas.FirstOrDefault(d => d.IdProducto == id);
                if (detalleExistente != null)
                {
                    detalleExistente.Cantidad += cantidad;
                    detalleExistente.SubtotalLinea = detalleExistente.Cantidad * detalleExistente.PrecioUnitario;
                }
                else
                {
                    viewModel.DetalleVentas.Add(new DetalleVenta
                    {
                        IdProducto = producto.IdProducto,
                        Cantidad = cantidad,
                        PrecioUnitario = producto.PrecioVenta,
                        SubtotalLinea = cantidad * producto.PrecioVenta
                    });
                }
                RecalcularTotales(viewModel);
            }
            else
            {
                ModelState.AddModelError("", "Producto no encontrado, sin stock suficiente o cantidad inválida.");
            }

            GuardarVentaEnTempData(viewModel);
            ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewBag.ProductosBusqueda = new List<Producto>();
            ViewBag.ClientesBusqueda = new List<Cliente>();
            return View("Crear", viewModel);
        }

        public async Task<IActionResult> QuitarProducto(int id, string tipoComprobante, string metodoPago, int? idCliente)
        {
            var viewModel = ObtenerVentaDeTempData() ?? new Venta { DetalleVentas = new List<DetalleVenta>() };
            viewModel.TipoComprobante = tipoComprobante;
            viewModel.MetodoPago = metodoPago;
            viewModel.IdCliente = idCliente;

            var detalle = viewModel.DetalleVentas.FirstOrDefault(d => d.IdProducto == id);
            if (detalle != null)
            {
                viewModel.DetalleVentas.Remove(detalle);
                RecalcularTotales(viewModel);
            }

            GuardarVentaEnTempData(viewModel);
            ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewBag.ProductosBusqueda = new List<Producto>();
            ViewBag.ClientesBusqueda = new List<Cliente>();
            return View("Crear", viewModel);
        }

        public async Task<IActionResult> SeleccionarCliente(int id, string tipoComprobante, string metodoPago)
        {
            var viewModel = ObtenerVentaDeTempData() ?? new Venta { DetalleVentas = new List<DetalleVenta>() };
            viewModel.TipoComprobante = tipoComprobante;
            viewModel.MetodoPago = metodoPago;
            viewModel.IdCliente = id;
            RecalcularTotales(viewModel);

            GuardarVentaEnTempData(viewModel);
            ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewBag.ProductosBusqueda = new List<Producto>();
            ViewBag.ClientesBusqueda = new List<Cliente>();
            return View("Crear", viewModel);
        }

        public async Task<IActionResult> QuitarCliente(string tipoComprobante, string metodoPago)
        {
            var viewModel = ObtenerVentaDeTempData() ?? new Venta { DetalleVentas = new List<DetalleVenta>() };
            viewModel.TipoComprobante = tipoComprobante;
            viewModel.MetodoPago = metodoPago;
            viewModel.IdCliente = null;
            RecalcularTotales(viewModel);

            GuardarVentaEnTempData(viewModel);
            ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewBag.ProductosBusqueda = new List<Producto>();
            ViewBag.ClientesBusqueda = new List<Cliente>();
            return View("Crear", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> RegistrarCliente(string nuevoClienteNombre, string nuevoClienteDNI, string tipoComprobante, string metodoPago)
        {
            var viewModel = ObtenerVentaDeTempData() ?? new Venta { DetalleVentas = new List<DetalleVenta>() };
            viewModel.TipoComprobante = tipoComprobante;
            viewModel.MetodoPago = metodoPago;

            if (!string.IsNullOrEmpty(nuevoClienteNombre) && !string.IsNullOrEmpty(nuevoClienteDNI))
            {
                var nuevoCliente = new Cliente
                {
                    TipoDocumento = "DNI",
                    NumeroDocumento = nuevoClienteDNI,
                    Nombres = nuevoClienteNombre,
                    Apellidos = "",
                    Telefono = "",
                    Email = "",
                    Direccion = "",
                    FechaRegistro = DateTime.Now
                };
                try
                {
                    await _clienteRepositorio.Crear(nuevoCliente);
                    viewModel.IdCliente = nuevoCliente.IdCliente;
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError("", ex.Message);
                }
            }
            else
            {
                ModelState.AddModelError("", "Debe proporcionar nombre y DNI para registrar un nuevo cliente.");
            }

            RecalcularTotales(viewModel);
            GuardarVentaEnTempData(viewModel);
            ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewBag.ProductosBusqueda = new List<Producto>();
            ViewBag.ClientesBusqueda = new List<Cliente>();
            return View("Crear", viewModel);
        }

        public async Task<IActionResult> LimpiarCarrito(string tipoComprobante, string metodoPago, int? idCliente)
        {
            var viewModel = new Venta
            {
                DetalleVentas = new List<DetalleVenta>(),
                TipoComprobante = tipoComprobante,
                MetodoPago = metodoPago,
                IdCliente = idCliente
            };
            RecalcularTotales(viewModel);
            GuardarVentaEnTempData(viewModel);
            ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewBag.ProductosBusqueda = new List<Producto>();
            ViewBag.ClientesBusqueda = new List<Cliente>();
            return View("Crear", viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> ProcesarVenta(string tipoComprobante, string metodoPago, int? idCliente, int idUsuario)
        {
            var viewModel = ObtenerVentaDeTempData() ?? new Venta { DetalleVentas = new List<DetalleVenta>() };
            viewModel.TipoComprobante = tipoComprobante;
            viewModel.MetodoPago = metodoPago;
            viewModel.IdCliente = idCliente;
            viewModel.IdUsuario = idUsuario;
            viewModel.FechaVenta = DateTime.Now;

            if (viewModel.DetalleVentas == null || viewModel.DetalleVentas.Count == 0)
            {
                ModelState.AddModelError("", "Debe agregar al menos un producto.");
                GuardarVentaEnTempData(viewModel);
                ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
                ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
                ViewBag.ProductosBusqueda = new List<Producto>();
                ViewBag.ClientesBusqueda = new List<Cliente>();
                return View("Crear", viewModel);
            }

            try
            {
                RecalcularTotales(viewModel);
                int idVenta = await _ventaRepositorio.CrearVentaConDetalles(viewModel);

                foreach (var detalle in viewModel.DetalleVentas)
                {
                    var producto = await _productoRepositorio.ObtenerPorId(detalle.IdProducto);
                    if (producto != null)
                    {
                        producto.Stock -= detalle.Cantidad;
                        await _productoRepositorio.Actualizar(producto);
                    }
                }

                TempData["Success"] = "Venta procesada exitosamente.";
                TempData.Clear();
                ViewBag.VentaExitosa = true;
                ViewBag.VentaId = idVenta;
                ViewBag.TotalVenta = viewModel.MontoTotal;
                ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
                ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
                ViewBag.ProductosBusqueda = new List<Producto>();
                ViewBag.ClientesBusqueda = new List<Cliente>();
                return View("Crear", new Venta { DetalleVentas = new List<DetalleVenta>() });
            }
            catch (InvalidOperationException ex)
            {
                ModelState.AddModelError("", ex.Message);
                GuardarVentaEnTempData(viewModel);
                ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
                ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
                ViewBag.ProductosBusqueda = new List<Producto>();
                ViewBag.ClientesBusqueda = new List<Cliente>();
                return View("Crear", viewModel);
            }
        }

        public async Task<IActionResult> CancelarVenta()
        {
            TempData.Clear();
            var viewModel = new Venta { DetalleVentas = new List<DetalleVenta>() };
            ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewBag.ProductosBusqueda = new List<Producto>();
            ViewBag.ClientesBusqueda = new List<Cliente>();
            return View("Crear", viewModel);
        }

        public async Task<IActionResult> CerrarModal()
        {
            TempData.Clear();
            var viewModel = new Venta { DetalleVentas = new List<DetalleVenta>() };
            ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewBag.ProductosBusqueda = new List<Producto>();
            ViewBag.ClientesBusqueda = new List<Cliente>();
            return View("Crear", viewModel);
        }

        private Venta ObtenerVentaDeTempData()
        {
            if (TempData["VentaEnProgreso"] != null)
            {
                return JsonSerializer.Deserialize<Venta>((string)TempData["VentaEnProgreso"]);
            }
            return null;
        }

        private void GuardarVentaEnTempData(Venta venta)
        {
            TempData["VentaEnProgreso"] = JsonSerializer.Serialize(venta);
        }

        private void RecalcularTotales(Venta venta)
        {
            venta.Subtotal = venta.DetalleVentas?.Sum(d => d.SubtotalLinea) ?? 0;
            venta.MontoIGV = venta.Subtotal * 0.18m;
            venta.MontoTotal = venta.Subtotal + venta.MontoIGV;
        }
    }
}