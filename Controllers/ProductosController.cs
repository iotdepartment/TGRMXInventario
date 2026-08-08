using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TGRMXInventario.Models;

namespace TGRMXInventario.Controllers
{
    public class ProductosController : Controller
    {
        // Reemplaza 'ApplicationDbContext' por el nombre real de tu clase de contexto de BD
        private readonly AppDbContext _context;

        public ProductosController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Productos
        public async Task<IActionResult> Index()
        {
            HttpContext.Session.SetString("UltimaActividadReal", DateTime.Now.ToString());
            // 1. Cargar las categorías para el modal
            ViewBag.Categorias = await _context.Categorias
                .OrderBy(c => c.NombreCategoria)
                .ToListAsync();

            // 2. Cargar los proveedores para el modal
            ViewBag.Proveedores = await _context.Proveedores
                .OrderBy(p => p.NombreProveedor)
                .ToListAsync();

            // 3. Cargar los productos incluyendo sus datos relacionados (JOIN)
            var productos = await _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor)
                .ToListAsync();

            return View(productos);
        }

        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NombreProducto,CategoriaID,ProveedorID,Costo,Unidad,Moneda,Cantidad,Min,Max")] Productos producto)
        {
            if (ModelState.IsValid)
            {
                _context.Add(producto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            ViewBag.Categorias = await _context.Categorias.OrderBy(c => c.NombreCategoria).ToListAsync();
            ViewBag.Proveedores = await _context.Proveedores.OrderBy(p => p.NombreProveedor).ToListAsync();
            var productos = await _context.Productos.Include(p => p.Categoria).Include(p => p.Proveedor).ToListAsync();
            return View("Index", productos);
        }

        // POST: Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NombreProducto,CategoriaID,ProveedorID,Costo,Unidad,Moneda,Cantidad,Min,Max")] Productos producto)
        {
            if (id != producto.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.Id)) return NotFound();
                    else throw;
                }
            }

            ViewBag.Categorias = await _context.Categorias.OrderBy(c => c.NombreCategoria).ToListAsync();
            ViewBag.Proveedores = await _context.Proveedores.OrderBy(p => p.NombreProveedor).ToListAsync();
            var productos = await _context.Productos.Include(p => p.Categoria).Include(p => p.Proveedor).ToListAsync();
            return View("Index", productos);
        }


        // GET: Productos/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var producto = await _context.Productos
                .FirstOrDefaultAsync(m => m.Id == id);

            if (producto == null) return NotFound();

            return View(producto);
        }

        // POST: Productos/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var producto = await _context.Productos.FindAsync(id);
            if (producto != null)
            {
                _context.Productos.Remove(producto);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        private bool ProductoExists(int id)
        {
            return _context.Productos.Any(e => e.Id == id);
        }
    }
}
