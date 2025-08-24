using Dapper;
using Microsoft.Data.SqlClient;
using SistemaVentas.Models;
using SistemaVentas.Services;

namespace SistemaVentas.Data
{
    public class VentaRepositorio : IVentaRepositorio
    {
        private readonly string _connectionString;

        public VentaRepositorio(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BDContexto");
        }

        public async Task<int> CrearVentaConDetalles(Venta venta)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();

            try
            {
                var idVenta = await connection.ExecuteScalarAsync<int>(
                    @"INSERT INTO Venta (IdCliente, IdUsuario, FechaVenta, TipoComprobante, MetodoPago, Subtotal, MontoIGV, MontoTotal)
                      VALUES (@IdCliente, @IdUsuario, @FechaVenta, @TipoComprobante, @MetodoPago, @Subtotal, @MontoIGV, @MontoTotal);
                      SELECT SCOPE_IDENTITY()",
                    venta, transaction);

                foreach (var detalle in venta.DetalleVentas)
                {
                    detalle.IdVenta = idVenta;
                    await connection.ExecuteAsync(
                        @"INSERT INTO DetalleVenta (IdVenta, IdProducto, Cantidad, PrecioUnitario, SubtotalLinea)
                          VALUES (@IdVenta, @IdProducto, @Cantidad, @PrecioUnitario, @SubtotalLinea)",
                        detalle, transaction);
                }

                transaction.Commit();
                return idVenta;
            }
            catch
            {
                transaction.Rollback();
                throw new InvalidOperationException("Error al crear la venta.");
            }
        }

        public async Task<Venta> ObtenerPorId(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT 
                    v.IdVenta, v.IdCliente, v.IdUsuario, v.FechaVenta, v.TipoComprobante, v.MetodoPago, v.Subtotal, v.MontoIGV, v.MontoTotal,
                    c.IdCliente, c.Nombres, c.Apellidos, c.TipoDocumento, c.NumeroDocumento, c.Email, c.Direccion, c.Telefono, c.FechaRegistro,
                    u.IdUsuario, u.NombreUsuario,
                    dv.IdVenta, dv.IdProducto, dv.Cantidad, dv.PrecioUnitario, dv.SubtotalLinea
                FROM Venta v
                LEFT JOIN Cliente c ON v.IdCliente = c.IdCliente
                LEFT JOIN Usuario u ON v.IdUsuario = u.IdUsuario
                LEFT JOIN DetalleVenta dv ON v.IdVenta = dv.IdVenta
                WHERE v.IdVenta = @Id";

            Venta venta = null;
            await connection.QueryAsync<Venta, Cliente, Usuario, DetalleVenta, Venta>(
                sql,
                (v, c, u, dv) =>
                {
                    if (venta == null)
                    {
                        venta = v;
                        venta.Cliente = c;
                        venta.Usuario = u;
                        venta.DetalleVentas = new List<DetalleVenta>();
                    }
                    if (dv != null && dv.IdVenta != 0)
                    {
                        venta.DetalleVentas.Add(dv);
                    }
                    return venta;
                },
                new { Id = id },
                splitOn: "IdCliente,IdUsuario,IdVenta");

            return venta;
        }

        public async Task<Paginador<Venta>> ObtenerTodos(int numeroPagina = 1, int tamanoPagina = 10, DateTime? fechaInicio = null, DateTime? fechaFin = null, int? clienteId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin?.AddDays(1).AddTicks(-1),
                ClienteId = clienteId,
                Desplazamiento = (numeroPagina - 1) * tamanoPagina,
                TamanoPagina = tamanoPagina
            };

            string sql = @"
                SELECT 
                    v.IdVenta, v.IdCliente, v.IdUsuario, v.FechaVenta, v.TipoComprobante, v.MetodoPago, v.Subtotal, v.MontoIGV, v.MontoTotal,
                    c.IdCliente, c.Nombres, c.Apellidos, c.TipoDocumento, c.NumeroDocumento, c.Email, c.Direccion, c.Telefono, c.FechaRegistro,
                    u.IdUsuario, u.NombreUsuario
                FROM Venta v
                LEFT JOIN Cliente c ON v.IdCliente = c.IdCliente
                LEFT JOIN Usuario u ON v.IdUsuario = u.IdUsuario";

            string countSql = "SELECT COUNT(*) FROM Venta v";

            var whereClauses = new List<string>();
            if (fechaInicio.HasValue)
            {
                whereClauses.Add("v.FechaVenta >= @FechaInicio");
            }
            if (fechaFin.HasValue)
            {
                whereClauses.Add("v.FechaVenta <= @FechaFin");
            }
            if (clienteId.HasValue)
            {
                whereClauses.Add("v.IdCliente = @ClienteId");
            }

            if (whereClauses.Any())
            {
                var whereClause = " WHERE " + string.Join(" AND ", whereClauses);
                sql += whereClause;
                countSql += whereClause;
            }

            sql += " ORDER BY v.FechaVenta DESC OFFSET @Desplazamiento ROWS FETCH NEXT @TamanoPagina ROWS ONLY";

            var ventas = (await connection.QueryAsync<Venta, Cliente, Usuario, Venta>(
                sql,
                (venta, cliente, usuario) =>
                {
                    venta.Cliente = cliente;
                    venta.Usuario = usuario;
                    return venta;
                },
                parameters,
                splitOn: "IdCliente,IdUsuario")).AsList();

            var totalRegistros = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            return new Paginador<Venta>
            {
                Elementos = ventas,
                TotalRegistros = totalRegistros,
                NumeroPagina = numeroPagina,
                TamanoPagina = tamanoPagina
            };
        }

        public async Task<int> ContarTotal(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? clienteId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin?.AddDays(1).AddTicks(-1),
                ClienteId = clienteId
            };

            string countSql = "SELECT COUNT(*) FROM Venta v";

            var whereClauses = new List<string>();
            if (fechaInicio.HasValue)
            {
                whereClauses.Add("v.FechaVenta >= @FechaInicio");
            }
            if (fechaFin.HasValue)
            {
                whereClauses.Add("v.FechaVenta <= @FechaFin");
            }
            if (clienteId.HasValue)
            {
                whereClauses.Add("v.IdCliente = @ClienteId");
            }

            if (whereClauses.Any())
            {
                countSql += " WHERE " + string.Join(" AND ", whereClauses);
            }

            return await connection.ExecuteScalarAsync<int>(countSql, parameters);
        }

        public async Task<decimal> SumarMontoTotal(DateTime? fechaInicio = null, DateTime? fechaFin = null, int? clienteId = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new
            {
                FechaInicio = fechaInicio,
                FechaFin = fechaFin?.AddDays(1).AddTicks(-1),
                ClienteId = clienteId
            };

            string sumSql = "SELECT COALESCE(SUM(v.MontoTotal), 0) FROM Venta v";

            var whereClauses = new List<string>();
            if (fechaInicio.HasValue)
            {
                whereClauses.Add("v.FechaVenta >= @FechaInicio");
            }
            if (fechaFin.HasValue)
            {
                whereClauses.Add("v.FechaVenta <= @FechaFin");
            }
            if (clienteId.HasValue)
            {
                whereClauses.Add("v.IdCliente = @ClienteId");
            }

            if (whereClauses.Any())
            {
                sumSql += " WHERE " + string.Join(" AND ", whereClauses);
            }

            return await connection.ExecuteScalarAsync<decimal>(sumSql, parameters);
        }

        public async Task<int?> ObtenerUltimoNumeroComprobante(string tipoComprobante)
        {
            using var connection = new SqlConnection(_connectionString);
            var ultimoNumero = await connection.QueryFirstOrDefaultAsync<int?>(
                "SELECT MAX(CAST(RIGHT(NumeroComprobante, 6) AS INT)) FROM Venta WHERE TipoComprobante = @TipoComprobante",
                new { TipoComprobante = tipoComprobante });
            return ultimoNumero;
        }

        public async Task Actualizar(Venta venta)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                "UPDATE Venta SET NumeroComprobante = @NumeroComprobante WHERE IdVenta = @IdVenta",
                new { venta.NumeroComprobante, venta.IdVenta });
        }
    }
}