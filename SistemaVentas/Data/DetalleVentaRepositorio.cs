using Dapper;
using Microsoft.Data.SqlClient;
using SistemaVentas.Models;
using System.Data;

namespace SistemaVentas.Data
{
    public class DetalleVentaRepositorio
    {
        private readonly string _connectionString;

        public DetalleVentaRepositorio(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BDContexto");
        }

        public async Task<int> Crear(DetalleVenta detalle)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                INSERT INTO DetalleVenta (IdVenta, IdProducto, Cantidad, PrecioUnitario, SubtotalLinea)
                VALUES (@IdVenta, @IdProducto, @Cantidad, @PrecioUnitario, @SubtotalLinea);
                SELECT SCOPE_IDENTITY();";
            return await connection.ExecuteScalarAsync<int>(sql, detalle);
        }

        public async Task<IEnumerable<DetalleVenta>> ObtenerPorVenta(int idVenta)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM DetalleVenta WHERE IdVenta = @IdVenta";
            return await connection.QueryAsync<DetalleVenta>(sql, new { IdVenta = idVenta });
        }
    }
}