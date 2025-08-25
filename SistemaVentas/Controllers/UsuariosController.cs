// Controllers/UsuariosController.cs
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SistemaVentas.Data;
using SistemaVentas.Models;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SistemaVentas.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly UsuarioRepositorio _usuarioRepositorio;

        public UsuariosController(IConfiguration configuration)
        {
            _usuarioRepositorio = new UsuarioRepositorio(configuration);
        }

        [HttpGet]
        public IActionResult IniciarSesion()
        {
            // Forzar login al reiniciar, ignorando la cookie si la aplicación se reinicia
            if (HttpContext.Session.GetInt32("IdUsuario") == null)
            {
                return View();
            }
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        public async Task<IActionResult> IniciarSesion(Usuario model)
        {
            ModelState.Remove("Rol");
            ModelState.Remove("NombreCompleto");

            if (ModelState.IsValid)
            {
                if (await _usuarioRepositorio.VerificarCredenciales(model.NombreUsuario, model.PasswordHash))
                {
                    var usuario = await _usuarioRepositorio.ObtenerPorNombreUsuario(model.NombreUsuario);
                    var claims = new[]
                    {
                        new Claim(ClaimTypes.Name, usuario.NombreUsuario),
                        new Claim(ClaimTypes.Role, usuario.Rol)
                    };
                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

                    HttpContext.Session.SetInt32("IdUsuario", usuario.IdUsuario);
                    HttpContext.Session.SetString("NombreUsuario", usuario.NombreUsuario);
                    HttpContext.Session.SetString("Rol", usuario.Rol);

                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("","");
            }
            else
            {
                foreach (var error in ModelState.Values.SelectMany(v => v.Errors))
                {
                    // No se imprimen errores, pero se mantienen para validación
                }
            }
            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = "Administrador")]
        public IActionResult Registrar()
        {
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public async Task<IActionResult> Registrar(Usuario model)
        {
            if (ModelState.IsValid)
            {
                var usuarioExistente = await _usuarioRepositorio.ObtenerPorNombreUsuario(model.NombreUsuario);
                if (usuarioExistente != null)
                {
                    ModelState.AddModelError("NombreUsuario", "El nombre de usuario ya está en uso.");
                    return View(model);
                }

                await _usuarioRepositorio.CrearUsuario(model);
                return RedirectToAction("IniciarSesion");
            }
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("IniciarSesion");
        }

        [HttpGet]
        public IActionResult AccesoDenegado()
        {
            return View();
        }
    }
}