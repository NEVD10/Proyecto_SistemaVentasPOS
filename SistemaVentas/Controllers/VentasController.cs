using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVentas.Data;
using SistemaVentas.Models;
using SistemaVentas.Services;
using System.Text.Json;

namespace SistemaVentas.Controllers
{
    public class VentasController : Controller
    {
        private readonly IClienteRepositorio _clienteRepositorio;
        private readonly IServicioFacturacion _servicioFacturacion;
        private readonly IVentaRepositorio _ventaRepositorio;
        private readonly IProductoRepositorio _productoRepositorio;
        private readonly ILogger<VentasController> _logger;
        private readonly IServicioCorreo _servicioCorreo;

        public VentasController(
            IClienteRepositorio clienteRepositorio,
            IServicioFacturacion servicioFacturacion,
            IVentaRepositorio ventaRepositorio,
            IProductoRepositorio productoRepositorio,
            ILogger<VentasController> logger,
            IServicioCorreo servicioCorreo)
        {
            _clienteRepositorio = clienteRepositorio;
            _servicioFacturacion = servicioFacturacion;
            _ventaRepositorio = ventaRepositorio;
            _productoRepositorio = productoRepositorio;
            _logger = logger;
            _servicioCorreo = servicioCorreo;
        }


        public async Task<IActionResult> Index(DateTime? fechaInicio, DateTime? fechaFin, int? clienteId, int pagina = 1, int tamanoPagina = 10)
        {
            //fechaInicio ??= DateTime.Today.AddMonths(-1);
            //fechaFin ??= DateTime.Today;
            tamanoPagina = tamanoPagina is 5 or 10 or 25 or 50 ? tamanoPagina : 10;
            pagina = pagina > 0 ? pagina : 1;

            var paginador = await _ventaRepositorio.ObtenerTodos(pagina, tamanoPagina, fechaInicio, fechaFin, clienteId);
            var ventas = paginador.Elementos.ToList();
            //var num_ventasTotales = await _ventaRepositorio.ObtenerTotalVentas();
            var totalVentasGeneral = await _ventaRepositorio.ContarTotal(fechaInicio, fechaFin, clienteId);
            var montoTotalGeneral = await _ventaRepositorio.SumarMontoTotal(fechaInicio, fechaFin, clienteId);
            var promedioGeneral = totalVentasGeneral > 0 ? montoTotalGeneral / totalVentasGeneral : 0;

            ViewBag.FechaInicio = fechaInicio;
            ViewBag.FechaFin = fechaFin;
            ViewBag.ClienteId = clienteId;
            ViewBag.PaginaActual = pagina;
            ViewBag.TamanoPagina = tamanoPagina;
            ViewBag.TotalPaginas = (int)Math.Ceiling((double)paginador.TotalRegistros / tamanoPagina); // Calcular manualmente
            ViewBag.TotalRegistros = paginador.TotalRegistros;
            ViewBag.TieneAnterior = pagina > 1;
            ViewBag.TieneSiguiente = pagina * tamanoPagina < paginador.TotalRegistros;
            ViewBag.TotalVentasGeneral = totalVentasGeneral;
            ViewBag.MontoTotalGeneral = montoTotalGeneral;
            ViewBag.PromedioGeneral = promedioGeneral;
            ViewBag.TotalVentasPagina = ventas.Count;
            ViewBag.Clientes = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewBag.ProductosBusqueda = new List<Producto>();
            ViewBag.ClientesBusqueda = new List<Cliente>();
            //ViewBag["VentasTotales"] = num_ventasTotales;

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
            if (venta == null) return NotFound();

            var productos = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList(); // Ajusta a int.MaxValue para obtener todos los productos
            try
            {
                byte[] pdfBytes = _servicioFacturacion.GenerarComprobante(venta, productos);
                return File(pdfBytes, "application/pdf", $"{venta.TipoComprobante}_{venta.NumeroComprobante}.pdf");
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
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
        [Authorize]
        public async Task<IActionResult> ProcesarVenta(string tipoComprobante, string metodoPago, int? idCliente, bool enviarCorreo = false)
        {
            // Paso 1: Verificar usuario autenticado
            _logger.LogInformation("Comenzando a procesar una nueva venta.");
            int? idUsuario = HttpContext.Session.GetInt32("IdUsuario");
            if (idUsuario == null)
            {
                _logger.LogWarning("No se encontró un usuario autenticado.");
                ModelState.AddModelError("", "Por favor, inicia sesión para continuar.");
                return RedirectToAction("IniciarSesion", "Usuarios");
            }

            // Paso 2: Cargar la venta actual o crear una nueva
            var venta = ObtenerVentaDeTempData() ?? new Venta { DetalleVentas = new List<DetalleVenta>() };
            _logger.LogInformation("Venta cargada. Productos en el carrito: {0}", venta.DetalleVentas?.Count ?? 0);

            // Paso 3: Validar y asignar datos de la venta
            if (string.IsNullOrEmpty(tipoComprobante))
            {
                _logger.LogWarning("No se proporcionó el tipo de comprobante.");
                ModelState.AddModelError("TipoComprobante", "Debes seleccionar un tipo de comprobante.");
                await CargarDatosVista(venta);
                return View("Crear", venta);
            }
            venta.TipoComprobante = tipoComprobante;

            venta.MetodoPago = string.IsNullOrEmpty(metodoPago) ? "Efectivo" : metodoPago;
            venta.IdCliente = idCliente;
            venta.IdUsuario = idUsuario.Value;
            venta.FechaVenta = DateTime.Now;

            // Paso 4: Verificar que haya productos en el carrito
            if (venta.DetalleVentas == null || venta.DetalleVentas.Count == 0)
            {
                _logger.LogWarning("El carrito está vacío.");
                ModelState.AddModelError("", "Debes agregar al menos un producto.");
                GuardarVentaEnTempData(venta);
                await CargarDatosVista(venta);
                return View("Crear", venta);
            }

            // Paso 5: Validar cliente si se solicita enviar correo
            if (enviarCorreo && !idCliente.HasValue)
            {
                _logger.LogWarning("No se seleccionó un cliente para enviar el correo.");
                ModelState.AddModelError("IdCliente", "Debes seleccionar un cliente para enviar el comprobante por correo.");
                GuardarVentaEnTempData(venta);
                await CargarDatosVista(venta);
                return View("Crear", venta);
            }

            try
            {
                // Paso 6: Calcular totales
                RecalcularTotales(venta);
                _logger.LogInformation("Total calculado: S/. {0}", venta.MontoTotal);
                _logger.LogInformation("Detalles de la venta - Usuario: {0}, Cliente: {1}, Tipo: {2}, Productos: {3}",
                    venta.IdUsuario, venta.IdCliente, venta.TipoComprobante, venta.DetalleVentas?.Count ?? 0);

                // Paso 7: Guardar la venta
                int idVenta = await _ventaRepositorio.CrearVentaConDetalles(venta);
                _logger.LogInformation("Venta guardada con ID: {0}", idVenta);

                // Paso 8: Actualizar stock
                foreach (var detalle in venta.DetalleVentas)
                {
                    var producto = await _productoRepositorio.ObtenerPorId(detalle.IdProducto);
                    if (producto != null)
                    {
                        if (producto.Stock < detalle.Cantidad)
                        {
                            _logger.LogWarning("No hay suficiente stock para el producto ID: {0}", detalle.IdProducto);
                            throw new InvalidOperationException($"No hay stock suficiente para el producto ID {detalle.IdProducto}.");
                        }
                        producto.Stock -= detalle.Cantidad;
                        await _productoRepositorio.Actualizar(producto);
                        _logger.LogInformation("Stock actualizado para el producto ID: {0}", detalle.IdProducto);
                    }
                }

                // Paso 9: Enviar correo si se solicita
                if (enviarCorreo && idCliente.HasValue)
                {
                    var ventaGuardada = await _ventaRepositorio.ObtenerPorId(idVenta);
                    var listaProductos = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
                    byte[] pdfBytes = _servicioFacturacion.GenerarComprobante(ventaGuardada, listaProductos);

                    var cliente = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos
                        .FirstOrDefault(c => c.IdCliente == idCliente);
                    if (cliente?.Email != null)
                    {
                        try
                        {
                            _logger.LogInformation("Intentando enviar correo a: {0} para venta ID: {1}", cliente.Email, idVenta);
                            await _servicioCorreo.EnviarComprobantePorCorreo(cliente.Email, ventaGuardada, pdfBytes);
                            ViewBag.CorreoEnviado = true;
                            _logger.LogInformation("Correo enviado exitosamente para la venta ID: {0}", idVenta);
                        }
                        catch (Exception ex)
                        {
                            ViewBag.CorreoEnviado = false;
                            ViewBag.ErrorCorreo = $"Fallo al enviar el correo: {ex.Message}";
                            _logger.LogError(ex, "Error al enviar el correo para la venta ID: {0}", idVenta);
                        }
                    }
                    else
                    {
                        ViewBag.CorreoEnviado = false;
                        ViewBag.ErrorCorreo = "No se puede enviar el correo porque el cliente no tiene un email registrado.";
                        _logger.LogWarning("Cliente sin email para la venta ID: {0}", idVenta);
                    }
                }

                // Paso 10: Finalizar y mostrar resultado
                TempData["Success"] = "¡Venta procesada con éxito!";
                TempData.Remove("VentaEnProgreso");
                ViewBag.VentaExitosa = true;
                ViewBag.VentaId = idVenta;
                ViewBag.TotalVenta = venta.MontoTotal;
                await CargarDatosVista(new Venta { DetalleVentas = new List<DetalleVenta>(), TipoComprobante = "Boleta", MetodoPago = "Efectivo" });
                return View("Crear", new Venta { DetalleVentas = new List<DetalleVenta>(), TipoComprobante = "Boleta", MetodoPago = "Efectivo" });
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError("Error al procesar la venta: {0}", ex.Message);
                ModelState.AddModelError("", ex.Message);
                GuardarVentaEnTempData(venta);
                await CargarDatosVista(venta);
                return View("Crear", venta);
            }
            catch (Exception ex)
            {
                _logger.LogError("Error inesperado al procesar la venta: {0}", ex.Message);
                ModelState.AddModelError("", "Ocurrió un problema. Intenta de nuevo.");
                GuardarVentaEnTempData(venta);
                await CargarDatosVista(venta);
                return View("Crear", venta);
            }
        }

        // Método auxiliar para reducir duplicación
        private async Task CargarDatosVista(Venta viewModel)
        {
            ViewData["Productos"] = (await _productoRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewData["Clientes"] = (await _clienteRepositorio.ObtenerTodos(1, int.MaxValue)).Elementos.ToList();
            ViewBag.ProductosBusqueda = new List<Producto>();
            ViewBag.ClientesBusqueda = new List<Cliente>();
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