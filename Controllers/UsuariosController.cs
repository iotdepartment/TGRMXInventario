using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TGRMXInventario.Models;

namespace TGRMXInventario.Controllers
{
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _appContext;
        private readonly UserContext _userContext;

        public UsuariosController(AppDbContext appContext, UserContext userContext)
        {
            _appContext = appContext;
            _userContext = userContext;
        }

        // GET: Usuarios
        public async Task<IActionResult> Index()
        {
            HttpContext.Session.SetString("UltimaActividadReal", DateTime.Now.ToString());
            // 1. Cargar el catálogo completo de RH y de usuarios
            var listaEmpleados = await _userContext.rh4.ToListAsync();
            var listaUsuarios = await _appContext.Usuarios.ToListAsync();

            // Enviar a los ViewBag la lista completa para los modales
            ViewBag.Empleados = listaEmpleados.OrderBy(e => e.NOMBRECOMPLETO).ToList();

            // 2. Cruzar datos en memoria para armar la lista de la tabla
            var modeloVista = listaUsuarios.Select(usr =>
            {
                // Buscamos al empleado en rh4 cuyo Id sea igual al EmpleadoID del usuario
                var emp = listaEmpleados.FirstOrDefault(e => e.Id == usr.EmpleadoID);

                return new UsuarioListaViewModel
                {
                    Id = usr.Id,
                    EmpleadoID_PK = usr.EmpleadoID, // Pasamos la PK de rh4 para que sigan funcionando los modales
                    NumeroEmpleado = emp?.EMPLEADO?.ToString() ?? "Sin asignar",
                    NombreCompleto = emp?.NOMBRECOMPLETO ?? "Empleado no identificado",
                    Area = usr.Area,
                    Correo = usr.Correo,
                    Password = usr.Password,
                    Rol = usr.Rol
                };
            }).ToList();

            return View(modeloVista);
        }


        // POST: Usuarios/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("EmpleadoID,Area,Correo,Password,Rol")] Usuarios usuario)
        {
            if (ModelState.IsValid)
            {
                // Hashear la contraseña de forma segura con BCrypt antes de guardar
                if (!string.IsNullOrEmpty(usuario.Password))
                {
                    usuario.Password = BCrypt.Net.BCrypt.HashPassword(usuario.Password);
                }

                _appContext.Add(usuario);
                await _appContext.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Empleados = await _userContext.rh4.OrderBy(e => e.NOMBRECOMPLETO).ToListAsync();
            var usuarios = await _appContext.Usuarios.ToListAsync();
            return View("Index", usuarios);
        }

        // POST: Usuarios/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,EmpleadoID,Area,Correo,Password,Rol")] Usuarios usuario)
        {
            if (id != usuario.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Comportamiento de Contraseña en Edición:
                    // Buscamos el registro actual para saber si la contraseña cambió o se mantiene igual
                    var usuarioExistente = await _appContext.Usuarios.AsNoTracking().FirstOrDefaultAsync(u => u.Id == id);

                    if (usuarioExistente != null)
                    {
                        // Si el usuario escribió una contraseña nueva (distinta a la vieja hasheada), la hasheamos
                        if (!string.IsNullOrEmpty(usuario.Password) && usuario.Password != usuarioExistente.Password)
                        {
                            usuario.Password = BCrypt.Net.BCrypt.HashPassword(usuario.Password);
                        }
                        else
                        {
                            // Si no la modificó, conservamos el hash que ya existía en la BD
                            usuario.Password = usuarioExistente.Password;
                        }
                    }

                    _appContext.Update(usuario);
                    await _appContext.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(usuario.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Empleados = await _userContext.rh4.OrderBy(e => e.NOMBRECOMPLETO).ToListAsync();
            var usuarios = await _appContext.Usuarios.ToListAsync();
            return View("Index", usuarios);
        }

        // POST: Usuarios/Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var usuario = await _appContext.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _appContext.Usuarios.Remove(usuario);
                await _appContext.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool UsuarioExists(int id)
        {
            return _appContext.Usuarios.Any(e => e.Id == id);
        }


        // GET: Usuarios/BuscarEmpleados?query=carlos
        [HttpGet]
        public async Task<IActionResult> BuscarEmpleados(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Json(new List<object>());
            }

            var queryClean = query.ToLower().Trim();

            // Filtramos directamente en la base de datos por número de empleado o nombre completo
            var resultados = await _userContext.rh4
                .Where(e => (e.EMPLEADO.HasValue && e.EMPLEADO.Value.ToString().Contains(queryClean)) ||
                            (e.NOMBRECOMPLETO != null && e.NOMBRECOMPLETO.ToLower().Contains(queryClean)))
                .OrderBy(e => e.NOMBRECOMPLETO)
                .Take(10) // Limitamos a las 10 mejores coincidencias para mayor velocidad
                .Select(e => new
                {
                    id = e.Id,
                    empleado = e.EMPLEADO,
                    nombre = e.NOMBRECOMPLETO
                })
                .ToListAsync();

            return Json(resultados);
        }

    }
}
