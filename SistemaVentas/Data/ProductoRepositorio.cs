using Dapper;
using Microsoft.Data.SqlClient;
using SistemaVentas.Models;
using SistemaVentas.Services;

namespace SistemaVentas.Data
{
    public class ProductoRepositorio : IProductoRepositorio
    {
        private readonly string _connectionString;

        public ProductoRepositorio(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BDContexto");
        }

        public async Task<Paginador<Producto>> ObtenerTodos(int numeroPagina, int tamanoPagina, string cadenaBusqueda = null, bool? estado = null)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new
            {
                CadenaBusqueda = $"%{cadenaBusqueda ?? ""}%",
                Estado = estado,
                Desplazamiento = (numeroPagina - 1) * tamanoPagina,
                TamanoPagina = tamanoPagina
            };

            string sql = @"
                SELECT 
                    p.IdProducto, p.Nombre, p.CodigoBarras, p.PrecioCosto, p.PrecioVenta, p.Stock, p.Marca, p.IdCategoria AS ProdIdCategoria, p.Estado,
                    c.IdCategoria, c.Nombre
                FROM Producto p
                LEFT JOIN Categoria c ON p.IdCategoria = c.IdCategoria";

            string countSql = "SELECT COUNT(*) FROM Producto p";

            var whereClauses = new List<string>();
            if (!string.IsNullOrEmpty(cadenaBusqueda))
            {
                whereClauses.Add("(p.Nombre LIKE @CadenaBusqueda OR p.CodigoBarras LIKE @CadenaBusqueda)");
            }
            if (estado.HasValue)
            {
                whereClauses.Add("p.Estado = @Estado");
            }

            if (whereClauses.Any())
            {
                sql += " WHERE " + string.Join(" AND ", whereClauses);
                countSql += " WHERE " + string.Join(" AND ", whereClauses);
            }

            sql += " ORDER BY p.IdProducto OFFSET @Desplazamiento ROWS FETCH NEXT @TamanoPagina ROWS ONLY";

            var productos = (await connection.QueryAsync<Producto, Categoria, Producto>(
                sql,
                (producto, categoria) =>
                {
                    if (categoria != null && categoria.IdCategoria != 0 && !string.IsNullOrEmpty(categoria.Nombre))
                    {
                        producto.Categoria = categoria;
                    }
                    else
                    {
                        producto.Categoria = new Categoria { Nombre = "Sin categoría" };
                    }
                    return producto;
                },
                parameters,
                splitOn: "IdCategoria")).AsList();

            var totalRegistros = await connection.ExecuteScalarAsync<int>(countSql, parameters);

            return new Paginador<Producto>
            {
                Elementos = productos,
                TotalRegistros = totalRegistros,
                NumeroPagina = numeroPagina,
                TamanoPagina = tamanoPagina
            };
        }

        public async Task<Producto> ObtenerPorId(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT 
                    p.IdProducto, p.Nombre, p.CodigoBarras, p.PrecioCosto, p.PrecioVenta, p.Stock, p.Marca, p.IdCategoria AS ProdIdCategoria, p.Estado,
                    c.IdCategoria, c.Nombre
                FROM Producto p
                LEFT JOIN Categoria c ON p.IdCategoria = c.IdCategoria
                WHERE p.IdProducto = @Id";

            var producto = (await connection.QueryAsync<Producto, Categoria, Producto>(
                sql,
                (p, c) =>
                {
                    if (c != null && c.IdCategoria != 0 && !string.IsNullOrEmpty(c.Nombre))
                    {
                        p.Categoria = c;
                        p.IdCategoria = c.IdCategoria;
                    }
                    else
                    {
                        p.Categoria = new Categoria { Nombre = "Sin categoría" };
                        p.IdCategoria = 0;
                    }
                    return p;
                },
                new { Id = id },
                splitOn: "IdCategoria")).FirstOrDefault();

            return producto ?? new Producto { Categoria = new Categoria { Nombre = "Sin categoría" }, IdCategoria = 0 };
        }

        public async Task<Producto> ObtenerPorCodigoBarras(string codigoBarras)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                SELECT 
                    p.IdProducto, p.Nombre, p.CodigoBarras, p.PrecioCosto, p.PrecioVenta, p.Stock, p.Marca, p.IdCategoria AS ProdIdCategoria, p.Estado,
                    c.IdCategoria, c.Nombre
                FROM Producto p
                LEFT JOIN Categoria c ON p.IdCategoria = c.IdCategoria
                WHERE p.CodigoBarras = @CodigoBarras";

            var producto = (await connection.QueryAsync<Producto, Categoria, Producto>(
                sql,
                (p, c) =>
                {
                    if (c != null && c.IdCategoria != 0 && !string.IsNullOrEmpty(c.Nombre))
                    {
                        p.Categoria = c;
                        p.IdCategoria = c.IdCategoria;
                    }
                    else
                    {
                        p.Categoria = new Categoria { Nombre = "Sin categoría" };
                        p.IdCategoria = 0;
                    }
                    return p;
                },
                new { CodigoBarras = codigoBarras },
                splitOn: "IdCategoria")).FirstOrDefault();

            return producto;
        }

        public async Task Crear(Producto producto)
        {
            using var connection = new SqlConnection(_connectionString);
            var categoriaExiste = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Categoria WHERE IdCategoria = @IdCategoria",
                new { IdCategoria = producto.IdCategoria });
            if (categoriaExiste == 0)
            {
                throw new InvalidOperationException("La categoría seleccionada no existe.");
            }

            var existe = await connection.QuerySingleOrDefaultAsync<Producto>(
                "SELECT * FROM Producto WHERE CodigoBarras = @CodigoBarras",
                new { producto.CodigoBarras });
            if (existe != null)
            {
                throw new InvalidOperationException("El código de barras ya está registrado.");
            }

            var id = await connection.QuerySingleAsync<int>(
                @"INSERT INTO Producto (Nombre, CodigoBarras, PrecioCosto, PrecioVenta, Stock, Marca, IdCategoria, Estado)
                  VALUES (@Nombre, @CodigoBarras, @PrecioCosto, @PrecioVenta, @Stock, @Marca, @IdCategoria, @Estado);
                  SELECT SCOPE_IDENTITY()", producto);
            producto.IdProducto = id;
        }

        public async Task Actualizar(Producto producto)
        {
            using var connection = new SqlConnection(_connectionString);
            var categoriaExiste = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Categoria WHERE IdCategoria = @IdCategoria",
                new { IdCategoria = producto.IdCategoria });
            if (categoriaExiste == 0)
            {
                throw new InvalidOperationException("La categoría seleccionada no existe.");
            }

            var existe = await connection.QuerySingleOrDefaultAsync<Producto>(
                "SELECT * FROM Producto WHERE CodigoBarras = @CodigoBarras AND IdProducto != @IdProducto",
                new { producto.CodigoBarras, producto.IdProducto });
            if (existe != null)
            {
                throw new InvalidOperationException("El código de barras ya está registrado.");
            }

            await connection.ExecuteAsync(
                @"UPDATE Producto 
                  SET Nombre = @Nombre, CodigoBarras = @CodigoBarras, PrecioCosto = @PrecioCosto, 
                      PrecioVenta = @PrecioVenta, Stock = @Stock, Marca = @Marca, IdCategoria = @IdCategoria, Estado = @Estado
                  WHERE IdProducto = @IdProducto", producto);
        }

        public async Task Eliminar(int id)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync("DELETE FROM Producto WHERE IdProducto = @Id", new { Id = id });
        }

        public async Task<IEnumerable<Categoria>> ObtenerCategorias()
        {
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryAsync<Categoria>("SELECT IdCategoria, Nombre FROM Categoria ORDER BY Nombre");
        }
    }
}