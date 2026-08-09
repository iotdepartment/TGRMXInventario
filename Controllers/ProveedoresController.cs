using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TGRMXInventario.Models; // Asegúrate de que coincida con tu namespace real

namespace TGRMXInventario.Controllers
{
    public class ProveedoresController : Controller
    {
        // Reemplaza 'ApplicationDbContext' por el nombre real de tu clase DbContext
        private readonly AppDbContext _context;

        public ProveedoresController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Proveedores
        public async Task<IActionResult> Index()
        {
            // 1. Registrar estampa de tiempo real para mantener vivo el temporizador de 30 minutos
            HttpContext.Session.SetString("UltimaActividadReal", DateTime.Now.ToString());

            // 2. Recuperar el Rol y el Área del usuario autenticado desde la Sesión
            string? userRol = HttpContext.Session.GetString("UsuarioRol");
            string? userArea = HttpContext.Session.GetString("UsuarioArea") ?? HttpContext.Session.GetString("Area");
            // Nota: Si no guardaste el Área al loguearte, abajo te enseño cómo añadirla al LoginController.

            // 3. Inicializar la consulta base de Entity Framework sobre la tabla Proveedores
            IQueryable<Proveedores> consultaProveedores = _context.Proveedores;

            // 4. APLICAR FILTRO ESTRICTO SI EL ROL ES REQUISITOR
            if (!string.IsNullOrEmpty(userRol) && userRol.Equals("Requisitor", StringComparison.OrdinalIgnoreCase))
            {
                // Si el área de la sesión llega vacía por error, forzamos una lista en blanco por seguridad
                if (string.IsNullOrEmpty(userArea))
                {
                    return View(new List<Proveedores>());
                }

                // Filtramos en SQL Server para que solo traiga proveedores de la misma área
                consultaProveedores = consultaProveedores.Where(p => p.Area != null && p.Area.ToLower() == userArea.ToLower());
            }

            // 5. Ejecutar la consulta de forma asíncrona y enviar el listado final a la vista
            var resultadoProveedores = await consultaProveedores.OrderBy(p => p.NombreProveedor).ToListAsync();
            return View(resultadoProveedores);
        }

        // POST: Proveedores/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NombreProveedor,Area,Contacto,Correo,Telefono")] Proveedores proveedor)
        {
            // 1. Mantener actualizado el contador real de actividad de 30 minutos
            HttpContext.Session.SetString("UltimaActividadReal", DateTime.Now.ToString());

            // 2. Extraer el rol y el área de la cuenta logueada
            string? userRol = HttpContext.Session.GetString("UsuarioRol");
            string? userArea = HttpContext.Session.GetString("UsuarioArea");

            // 3. BLINDAJE INTERNO: Forzar el área de la sesión si es Requisitor
            if (!string.IsNullOrEmpty(userRol) && userRol.Equals("Requisitor", StringComparison.OrdinalIgnoreCase))
            {
                proveedor.Area = userArea ?? "Sin Área";

                // Removemos el campo de las reglas de validación automáticas de .NET
                ModelState.Remove("Area");
            }

            if (ModelState.IsValid)
            {
                _context.Add(proveedor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si ocurre un error, recargar el catálogo manteniendo las restricciones de seguridad
            var proveedores = await _context.Proveedores.ToListAsync();

            if (!string.IsNullOrEmpty(userRol) && userRol.Equals("Requisitor", StringComparison.OrdinalIgnoreCase))
            {
                proveedores = proveedores.Where(p => p.Area != null && p.Area.ToLower() == userArea!.ToLower()).ToList();
            }

            return View("Index", proveedores);
        }


        // POST: Proveedores/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NombreProveedor,Area,Contacto,Correo,Telefono")] Proveedores proveedor)
        {
            if (id != proveedor.Id)
            {
                return NotFound();
            }

            // 1. Renovar estampa de tiempo real de 30 minutos
            HttpContext.Session.SetString("UltimaActividadReal", DateTime.Now.ToString());

            // 2. Extraer rol y área actuales de la sesión
            string? userRol = HttpContext.Session.GetString("UsuarioRol");
            string? userArea = HttpContext.Session.GetString("UsuarioArea");

            // 3. CANDADO DE BACKEND: Si es Requisitor, forzar el área de su sesión de forma estricta
            if (!string.IsNullOrEmpty(userRol) && userRol.Equals("Requisitor", StringComparison.OrdinalIgnoreCase))
            {
                proveedor.Area = userArea ?? "Sin Área";
                ModelState.Remove("Area"); // Excluir de validaciones del modelo
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(proveedor);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProveedorExists(proveedor.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // Si el formulario falla, recargar con el filtrado correspondiente
            var proveedores = await _context.Proveedores.ToListAsync();
            if (!string.IsNullOrEmpty(userRol) && userRol.Equals("Requisitor", StringComparison.OrdinalIgnoreCase))
            {
                proveedores = proveedores.Where(p => p.Area != null && p.Area.ToLower() == userArea!.ToLower()).ToList();
            }

            return View("Index", proveedores);
        }



        // POST: Proveedores/Delete
        // Procesa la confirmación del modal "¿Eliminar?"
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);
            if (proveedor != null)
            {
                _context.Proveedores.Remove(proveedor);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // Método auxiliar para verificar si un proveedor existe por su ID
        private bool ProveedorExists(int id)
        {
            return _context.Proveedores.Any(e => e.Id == id);
        }
    }
}
