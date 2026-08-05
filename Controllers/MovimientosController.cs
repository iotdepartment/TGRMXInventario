using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TGRMXInventario.Models; // Asegúrate de que apunte a tu namespace real

namespace TGRMXInventario.Controllers
{
    public class MovimientosController : Controller
    {
        private readonly AppDbContext _appContext;
        private readonly UserContext _userContext;

        // Inyectamos ambos contextos de datos de forma segura
        public MovimientosController(AppDbContext appContext, UserContext userContext)
        {
            _appContext = appContext;
            _userContext = userContext;
        }

        // GET: Movimientos
        public async Task<IActionResult> Index()
        {
            // 1. ELIMINADO EL .Include() QUE REVENTABA LA PÁGINA
            var listaMovimientos = await _appContext.Movimientos
                .OrderByDescending(m => m.Fecha) // Los registros más recientes aparecen arriba
                .ToListAsync();

            // 2. Traer el catálogo de empleados de recursos humanos y productos para el cruce en memoria
            var listaEmpleados = await _userContext.rh4.ToListAsync();
            var listaProductos = await _appContext.Productos.ToListAsync();

            // 3. Mapear y cruzar la información para construir el ViewModel
            var modeloVista = listaMovimientos.Select(mov =>
            {
                // Buscar los datos del empleado en rh4 cruzando por el EmpleadoID guardado
                var emp = listaEmpleados.FirstOrDefault(e => e.Id == mov.EmpleadoID);

                // Buscar los datos del producto cruzando por el ProductoID guardado
                var prod = listaProductos.FirstOrDefault(p => p.Id == mov.ProductoID);

                return new MovimientoViewModel
                {
                    Id = mov.Id,
                    NumeroEmpleado = emp?.EMPLEADO?.ToString() ?? "N/A",
                    NombreEmpleado = emp?.NOMBRECOMPLETO ?? "Usuario no identificado",
                    NombreProducto = prod?.NombreProducto ?? "Material eliminado/no identificado",
                    Tipo = mov.Tipo ?? "Descuento",
                    Departamento = mov.Departamento ?? emp?.DEPTO_DESCRIPCION ?? "Sin asignar",
                    Fecha = mov.Fecha,
                    Cantidad = mov.Cantidad ?? 0
                };
            }).ToList();

            return View(modeloVista);
        }

    }
}
