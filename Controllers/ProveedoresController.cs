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
        // Muestra la tabla con el listado completo de proveedores
        public async Task<IActionResult> Index()
        {
            HttpContext.Session.SetString("UltimaActividadReal", DateTime.Now.ToString());
            var proveedores = await _context.Proveedores.ToListAsync();
            return View(proveedores);
        }

        // POST: Proveedores/Create
        // Procesa el formulario del modal "Agregar Proveedor"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NombreProveedor,Contacto,Correo,Telefono")] Proveedores proveedor)
        {
            if (ModelState.IsValid)
            {
                _context.Add(proveedor);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Si el estado del modelo no es válido, vuelve a cargar la lista con los datos actuales
            var proveedores = await _context.Proveedores.ToListAsync();
            return View("Index", proveedores);
        }

        // POST: Proveedores/Edit
        // Procesa el formulario del modal "Editar Proveedor"
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NombreProveedor,Contacto,Correo,Telefono")] Proveedores proveedor)
        {
            if (id != proveedor.Id)
            {
                return NotFound();
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
                    if (!ProveedorExists(proveedor.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }

            var proveedores = await _context.Proveedores.ToListAsync();
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
