// Data/UsuarioRepositorio.cs
using Dapper;
using Microsoft.Data.SqlClient;
using SistemaVentas.Models;
using System;
using System.Threading.Tasks;

namespace SistemaVentas.Data
{
    public class UsuarioRepositorio
    {
        private readonly string _connectionString;

        public UsuarioRepositorio(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("BDContexto");
        }

        public async Task<Usuario> ObtenerPorNombreUsuario(string nombreUsuario)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = "SELECT * FROM Usuario WHERE NombreUsuario = @NombreUsuario AND Estado = 1";
            return await connection.QueryFirstOrDefaultAsync<Usuario>(sql, new { NombreUsuario = nombreUsuario });
        }

        public async Task<int> CrearUsuario(Usuario usuario)
        {
            using var connection = new SqlConnection(_connectionString);
            var sql = @"
                INSERT INTO Usuario (NombreUsuario, PasswordHash, NombreCompleto, Rol, Estado)
                VALUES (@NombreUsuario, @PasswordHash, @NombreCompleto, @Rol, @Estado);
                SELECT SCOPE_IDENTITY()";
            try
            {
                var id = await connection.ExecuteScalarAsync<int>(sql, usuario);
                Console.WriteLine($"Usuario creado con ID: {id}");
                return id;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al crear usuario: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> VerificarCredenciales(string nombreUsuario, string password)
        {
            var usuario = await ObtenerPorNombreUsuario(nombreUsuario);
            if (usuario == null) return false;
            return BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash);
        }
    }
}