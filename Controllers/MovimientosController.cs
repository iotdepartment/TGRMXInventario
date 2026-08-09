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
            // 1. Registrar estampa de tiempo real para mantener vivo el temporizador de 30 minutos
            HttpContext.Session.SetString("UltimaActividadReal", DateTime.Now.ToString());

            // 2. Recuperar el Rol y el Área del usuario autenticado desde la Sesión
            string? userRol = HttpContext.Session.GetString("UsuarioRol");
            string? userArea = HttpContext.Session.GetString("UsuarioArea");

            // 3. Cargar el catálogo completo de empleados de recursos humanos en memoria para el cruce de nombres
            var listaEmpleados = await _userContext.rh4.ToListAsync();

            // 4. Inicializar las consultas base de la base de datos local
            IQueryable<Movimientos> consultaMovimientos = _appContext.Movimientos;
            IQueryable<Productos> consultaProductos = _appContext.Productos;

            // 5. APLICAR FILTRO DE AUDITORÍA SI EL OPERADOR ES REQUISITOR
            if (!string.IsNullOrEmpty(userRol) && userRol.Equals("Requisitor", StringComparison.OrdinalIgnoreCase))
            {
                // Si el área de la sesión llega vacía por error, devolvemos una lista vacía por protección
                if (string.IsNullOrEmpty(userArea))
                {
                    return View(new List<MovimientoViewModel>());
                }

                // A) Obtener los IDs de todos los productos que pertenecen al área del Requisitor
                var idsProductosArea = await consultaProductos
                    .Where(p => p.Area != null && p.Area.ToLower() == userArea.ToLower())
                    .Select(p => p.Id)
                    .ToListAsync();

                // B) Filtrar los movimientos para que solo traiga los que correspondan a esos productos
                consultaMovimientos = consultaMovimientos.Where(m => m.ProductoID.HasValue && idsProductosArea.Contains(m.ProductoID.Value));
            }

            // 6. Ejecutar las consultas finales de forma asíncrona hacia SQL Server
            var listaMovimientosFinal = await consultaMovimientos.OrderByDescending(m => m.Fecha).ToListAsync();
            var listaProductosFinal = await consultaProductos.ToListAsync();

            // 7. Mapear y cruzar la información en memoria para construir el ViewModel que requiere la tabla
            var modeloVista = listaMovimientosFinal.Select(mov =>
            {
                var emp = listaEmpleados.FirstOrDefault(e => e.Id == mov.EmpleadoID);
                var prod = listaProductosFinal.FirstOrDefault(p => p.Id == mov.ProductoID);

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
