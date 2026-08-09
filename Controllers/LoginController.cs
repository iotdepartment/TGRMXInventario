using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TGRMXInventario.Models;

namespace TGRMXInventario.Controllers
{
    public class LoginController : Controller
    {
        private readonly AppDbContext _appContext;
        private readonly UserContext _userContext;

        public LoginController(AppDbContext appContext, UserContext userContext)
        {
            _appContext = appContext;
            _userContext = userContext;
        }

        // GET: /Login
        public IActionResult Index()
        {
            return View();
        }

        // POST: /Login/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(int NumeroEmpleado, string Password)
        {
            if (NumeroEmpleado <= 0 || string.IsNullOrWhiteSpace(Password))
            {
                return Json(new { success = false, message = "Por favor, complete todos los campos requeridos." });
            }

            try
            {
                // 1. Validar si el número de nómina ingresado existe en el catálogo rh4 corporativo
                var empleadoRH = await _userContext.rh4
                    .FirstOrDefaultAsync(e => e.EMPLEADO == NumeroEmpleado);

                if (empleadoRH == null)
                {
                    return Json(new { success = false, message = "El número de empleado no está registrado en Recursos Humanos." });
                }

                // 2. Buscar si el Id asignado de rh4 tiene una cuenta creada en la tabla Usuarios local
                var usuarioSistema = await _appContext.Usuarios
                    .FirstOrDefaultAsync(u => u.EmpleadoID == empleadoRH.Id);

                if (usuarioSistema == null)
                {
                    return Json(new { success = false, message = "Este empleado no tiene un usuario de acceso asignado en el sistema." });
                }

                // 3. Validar el Hash de la contraseña guardada en base de datos mediante BCrypt
                bool passwordValida = BCrypt.Net.BCrypt.Verify(Password, usuarioSistema.Password);

                if (!passwordValida)
                {
                    return Json(new { success = false, message = "La contraseña ingresada es incorrecta." });
                }

                // 4. Inicializar y guardar las variables globales de estado dentro de la Sesión
                HttpContext.Session.SetString("UsuarioID", usuarioSistema.Id.ToString());
                HttpContext.Session.SetString("UsuarioCorreo", usuarioSistema.Correo ?? "");
                HttpContext.Session.SetString("UsuarioRol", usuarioSistema.Rol ?? "Consumidor");
                HttpContext.Session.SetString("EmpleadoNombre", empleadoRH.NOMBRECOMPLETO ?? "Usuario");
                HttpContext.Session.SetString("UsuarioArea", usuarioSistema.Area ?? "");
                // CORREGIDO: Se registra la estampa de tiempo ANTES de cerrar el método con el return
                HttpContext.Session.SetString("UltimaActividadReal", DateTime.Now.ToString());

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Ocurrió un error en el servidor al intentar validar su acceso: " + ex.Message });
            }
        }


        // GET: /Login/CheckSession
        [HttpGet]
        public IActionResult CheckSession()
        {
            bool tieneLlave = HttpContext.Session.GetString("UsuarioID") != null;

            if (tieneLlave)
            {
                var ultimaPeticionStr = HttpContext.Session.GetString("UltimaActividadReal");
                if (!string.IsNullOrEmpty(ultimaPeticionStr))
                {
                    var ultimaActividad = DateTime.Parse(ultimaPeticionStr);

                    // CAMBIADO: Ahora evalúa si han pasado más de 30 minutos desde la última acción real
                    if (DateTime.Now - ultimaActividad > TimeSpan.FromMinutes(30))
                    {
                        HttpContext.Session.Clear(); // Forzar la destrucción real de la sesión
                        return Json(new { active = false });
                    }
                }
            }
            else
            {
                return Json(new { active = false });
            }

            return Json(new { active = tieneLlave });
        }

        // GET: /Login/Logout
        public IActionResult Logout()
        {
            // 1. Limpiar y destruir todas las variables del perfil de la memoria del servidor
            HttpContext.Session.Clear();

            // 2. Redirigir explícitamente al controlador Descontar y su acción Index
            return RedirectToAction("Index", "Descontar");
        }
    }
}
