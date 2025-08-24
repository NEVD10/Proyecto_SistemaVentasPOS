using Dapper;
using Microsoft.Data.SqlClient;
using SistemaVentas.Models;
using SistemaVentas.Services;

namespace SistemaVentas.Data
{
    public class ClienteRepositorio : IClienteRepositorio
    {
        private readonly string _connectionString;

        public ClienteRepositorio(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BDContexto");
        }

        public async Task<Paginador<Cliente>> ObtenerTodos(int numeroPagina = 1, int tamanoPagina = 10, string filtro = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new { Filtro = $"%{filtro ?? ""}%", Desplazamiento = (numeroPagina - 1) * tamanoPagina, TamanoPagina = tamanoPagina };

            string sql = "SELECT * FROM Cliente";
            string countSql = "SELECT COUNT(*) FROM Cliente";

            var clausulasWhere = new List<string>();
            if (!string.IsNullOrEmpty(filtro))
            {
                clausulasWhere.Add("Nombres LIKE @Filtro OR NumeroDocumento LIKE @Filtro");
            }

            if (clausulasWhere.Any())
            {
                sql += " WHERE " + string.Join(" AND ", clausulasWhere);
                countSql += " WHERE " + string.Join(" AND ", clausulasWhere);
            }

            sql += " ORDER BY IdCliente OFFSET @Desplazamiento ROWS FETCH NEXT @TamanoPagina ROWS ONLY";

            var clientes = await connection.QueryAsync<Cliente>(sql, parameters);
            var totalRegistros = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            return new Paginador<Cliente>
            {
                Elementos = clientes,
                TotalRegistros = totalRegistros,
                NumeroPagina = numeroPagina,
                TamanoPagina = tamanoPagina
            };
        }

        public async Task<int> ObtenerTotalClientes()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Cliente");
        }

        public async Task<Cliente> ObtenerPorId(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<Cliente>("SELECT * FROM Cliente WHERE IdCliente = @Id", new { Id = id });
        }

        public async Task<int> Crear(Cliente cliente)
        {
            using var connection = new SqlConnection(_connectionString);
            var existe = await connection.QuerySingleOrDefaultAsync<Cliente>(
                "SELECT * FROM Cliente WHERE NumeroDocumento = @NumeroDocumento",
                new { cliente.NumeroDocumento });
            if (existe != null)
            {
                throw new InvalidOperationException("El número de documento ya está registrado.");
            }
            var id = await connection.QuerySingleAsync<int>(
                @"INSERT INTO Cliente (TipoDocumento, NumeroDocumento, Nombres, Apellidos, Telefono, Email, Direccion, FechaRegistro)
                  VALUES (@TipoDocumento, @NumeroDocumento, @Nombres, @Apellidos, @Telefono, @Email, @Direccion, @FechaRegistro);
                  SELECT SCOPE_IDENTITY()", cliente);
            cliente.IdCliente = id;
            return id;
        }

        public async Task Actualizar(Cliente cliente)
        {
            using var connection = new SqlConnection(_connectionString);
            var existe = await connection.QuerySingleOrDefaultAsync<Cliente>(
                "SELECT * FROM Cliente WHERE NumeroDocumento = @NumeroDocumento AND IdCliente != @IdCliente",
                new { cliente.NumeroDocumento, cliente.IdCliente });
            if (existe != null)
            {
                throw new InvalidOperationException("El número de documento ya está registrado.");
            }
            await connection.ExecuteAsync(
                @"UPDATE Cliente 
                  SET TipoDocumento = @TipoDocumento, NumeroDocumento = @NumeroDocumento, 
                      Nombres = @Nombres, Apellidos = @Apellidos, Telefono = @Telefono, 
                      Email = @Email, Direccion = @Direccion
                  WHERE IdCliente = @IdCliente", cliente);
        }

        public async Task Eliminar(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM Cliente WHERE IdCliente = @Id", new { Id = id });
        }
    }
}