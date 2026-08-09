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
            // 1. Registrar estampa de tiempo real para mantener vivo el temporizador de 30 minutos
            HttpContext.Session.SetString("UltimaActividadReal", DateTime.Now.ToString());

            // 2. Recuperar el Rol y el Área del usuario autenticado desde la Sesión
            string? userRol = HttpContext.Session.GetString("UsuarioRol");
            string? userArea = HttpContext.Session.GetString("UsuarioArea");

            // 3. Inicializar la consulta base incluyendo las relaciones necesarias para la tabla
            IQueryable<Productos> consultaProductos = _context.Productos
                .Include(p => p.Categoria)
                .Include(p => p.Proveedor);

            // 4. APLICAR FILTRO DE SEGURIDAD SI EL OPERADOR ES REQUISITOR
            if (!string.IsNullOrEmpty(userRol) && userRol.Equals("Requisitor", StringComparison.OrdinalIgnoreCase))
            {
                // Si por un descuido el área de la sesión llega vacía, forzamos una lista en blanco por protección
                if (string.IsNullOrEmpty(userArea))
                {
                    ViewBag.Categorias = new List<Categorias>();
                    ViewBag.Proveedores = new List<Proveedores>();
                    return View(new List<Productos>());
                }

                // Filtramos directamente en SQL Server para que solo devuelva productos de su área corporativa
                consultaProductos = consultaProductos.Where(p => p.Area != null && p.Area.ToLower() == userArea.ToLower());
            }

            // 5. Cargar los catálogos en los ViewBag correspondientes para que los modales funcionen
            // Nota: Si el usuario es Requisitor, también filtramos los proveedores para que correspondan a su área
            if (!string.IsNullOrEmpty(userRol) && userRol.Equals("Requisitor", StringComparison.OrdinalIgnoreCase))
            {
                ViewBag.Categorias = await _context.Categorias.OrderBy(c => c.NombreCategoria).ToListAsync();
                ViewBag.Proveedores = await _context.Proveedores
                    .Where(p => p.Area != null && p.Area.ToLower() == userArea!.ToLower())
                    .OrderBy(p => p.NombreProveedor)
                    .ToListAsync();
            }
            else
            {
                ViewBag.Categorias = await _context.Categorias.OrderBy(c => c.NombreCategoria).ToListAsync();
                ViewBag.Proveedores = await _context.Proveedores.OrderBy(p => p.NombreProveedor).ToListAsync();
            }

            // 6. Ejecutar la consulta de forma asíncrona y enviar el listado seguro a la vista
            var resultadoProductos = await consultaProductos.OrderBy(p => p.NombreProducto).ToListAsync();
            return View(resultadoProductos);
        }


        // POST: Productos/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("NombreProducto,Area,CategoriaID,ProveedorID,Costo,Unidad,Moneda,Cantidad,Min,Max")] Productos producto)
        {
            // 1. Renovar el temporizador de actividad real de 30 minutos
            HttpContext.Session.SetString("UltimaActividadReal", DateTime.Now.ToString());

            // 2. Recuperar datos de control desde la sesión
            string? userRol = HttpContext.Session.GetString("UsuarioRol");
            string? userArea = HttpContext.Session.GetString("UsuarioArea");

            // 3. CANDADO DE SEGURIDAD: Si es Requisitor, sobreescribir el área de forma obligatoria con la de su sesión
            if (!string.IsNullOrEmpty(userRol) && userRol.Equals("Requisitor", StringComparison.OrdinalIgnoreCase))
            {
                producto.Area = userArea ?? "Sin Área";

                // Removemos "Area" de las validaciones del ModelState ya que no viene del HTML sino del servidor
                ModelState.Remove("Area");
            }

            if (ModelState.IsValid)
            {
                _context.Add(producto);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            // Recargar catálogos si el formulario llega con errores de validación
            ViewBag.Categorias = await _context.Categorias.ToListAsync();
            ViewBag.Proveedores = await _context.Proveedores.ToListAsync();

            return View("Index", await _context.Productos.Include(p => p.Categoria).ToListAsync());
        }

        // POST: Productos/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NombreProducto,Area,CategoriaID,ProveedorID,Costo,Unidad,Moneda,Cantidad,Min,Max")] Productos producto)
        {
            if (id != producto.Id)
            {
                return NotFound();
            }

            // 1. Renovar el temporizador de actividad real de 30 minutos
            HttpContext.Session.SetString("UltimaActividadReal", DateTime.Now.ToString());

            // 2. Recuperar el Rol y el Área desde la Sesión
            string? userRol = HttpContext.Session.GetString("UsuarioRol");
            string? userArea = HttpContext.Session.GetString("UsuarioArea");

            // 3. CANDADO DE SEGURIDAD: Si es Requisitor, sobreescribir forzosamente el área con la de su cuenta
            if (!string.IsNullOrEmpty(userRol) && userRol.Equals("Requisitor", StringComparison.OrdinalIgnoreCase))
            {
                producto.Area = userArea ?? "Sin Área";
                ModelState.Remove("Area"); // Quitar de las validaciones HTML
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(producto);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductoExists(producto.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            // Recargar componentes si el formulario falla al procesarse
            ViewBag.Categorias = await _context.Categorias.ToListAsync();
            ViewBag.Proveedores = await _context.Proveedores.ToListAsync();
            return View("Index", await _context.Productos.Include(p => p.Categoria).ToListAsync());
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
