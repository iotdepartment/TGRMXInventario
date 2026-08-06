using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TGRMXInventario.Models; // Asegúrate de que apunte a tu namespace real

namespace TGRMXInventario.Controllers
{
    public class DescontarController : Controller
    {
        private readonly AppDbContext _appContext;
        private readonly UserContext _userContext;

        public DescontarController(AppDbContext appContext, UserContext userContext)
        {
            _appContext = appContext;
            _userContext = userContext;
        }

        // GET: Descontar
        public IActionResult Index()
        {
            return View();
        }

        // GET: /Descontar/ObtenereMpleado?idEmpleado=002887
        [HttpGet]
        public async Task<IActionResult> ObtenereMpleado(string idEmpleado)
        {
            if (string.IsNullOrWhiteSpace(idEmpleado))
            {
                return Json(new { success = false, message = "Número de empleado no proporcionado." });
            }

            // Convertimos a entero para quitar ceros a la izquierda (ej. "002887" se vuelve 2887)
            if (!int.TryParse(idEmpleado, out int numEmpleado))
            {
                return Json(new { success = false, message = "El número de empleado debe ser un valor numérico." });
            }

            // 1. Buscar primero el registro en la tabla rh4 usando el UserContext
            var empRH = await _userContext.rh4
                .FirstOrDefaultAsync(e => e.EMPLEADO == numEmpleado);

            if (empRH == null)
            {
                return Json(new { success = false, message = "El número de empleado no existe en el sistema de Recursos Humanos." });
            }

            // 2. Validar de forma cruzada si ese Id de rh4 está registrado en tu tabla de Usuarios local (AppDbContext)
            // Buscamos comparando EmpleadoID (de tu tabla Usuarios) con el Id único (de la tabla rh4)
            var usuarioSistema = await _appContext.Usuarios
                .FirstOrDefaultAsync(u => u.EmpleadoID == empRH.Id);

            if (usuarioSistema == null)
            {
                return Json(new { success = false, message = "El empleado está dado de alta en RH, pero no tiene una cuenta de usuario activa en este sistema." });
            }

            // 3. Si pasa ambas validaciones, retornamos la información unificada
            return Json(new
            {
                success = true,
                empleado = new
                {
                    id = empRH.Id, // Se mantiene el Id original para el guardado de la transacción
                    numero = empRH.EMPLEADO,
                    nombre = empRH.NOMBRECOMPLETO,
                    puesto = empRH.PUESTO_DESCRIPCION ?? "Sin puesto",
                    departamento = empRH.DEPTO_DESCRIPCION ?? "Sin departamento"
                }
            });
        }


        // GET: /Descontar/ObtenerProducto?codigo=15
        [HttpGet]
        public async Task<IActionResult> ObtenerProducto(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return Json(new { success = false, message = "Código de material vacío." });
            }

            // El escáner arrojará el ID numérico del producto (ej. "15"). Lo validamos y convertimos.
            if (!int.TryParse(codigo.Trim(), out int productoId))
            {
                return Json(new { success = false, message = "El código escaneado debe ser un ID numérico válido." });
            }

            // Buscamos directamente por la llave primaria 'Id' en la base de datos local (AppDbContext)
            var producto = await _appContext.Productos
                .Include(p => p.Categoria)
                .FirstOrDefaultAsync(p => p.Id == productoId);

            if (producto == null)
            {
                return Json(new { success = false, message = $"El material con ID #{productoId} no está registrado en el inventario." });
            }

            return Json(new
            {
                success = true,
                producto = new
                {
                    id = producto.Id, // Enviamos el Id real de vuelta al JavaScript
                    nombre = producto.NombreProducto ?? "Sin nombre",
                    categoria = producto.Categoria != null ? producto.Categoria.NombreCategoria : "General"
                }
            });
        }

        // POST: /Descontar/ProcesarDescuento
        [HttpPost]
        public async Task<IActionResult> ProcesarDescuento([FromBody] TransaccionSalidaViewModel modelo)
        {
            if (modelo == null || modelo.Materiales == null || !modelo.Materiales.Any())
            {
                return Json(new { success = false, message = "La solicitud no contiene materiales para procesar." });
            }

            // Iniciamos una transacción para asegurar consistencia atómica en el inventario
            using var dbTransaction = await _appContext.Database.BeginTransactionAsync();

            try
            {
                // 1. Obtener el departamento real del empleado desde el UserContext (rh4)
                var empleadoRH = await _userContext.rh4.FindAsync(modelo.SolicitanteID);
                string deptoEmpleado = empleadoRH?.DEPTO_DESCRIPCION ?? "Sin Departamento";

                // 2. Determinar si el flujo es una suma (AGREGAR) o una resta (DESCUENTO)
                bool esModoAgregar = !string.IsNullOrEmpty(modelo.Modo) && modelo.Modo.ToUpper() == "AGREGAR";
                string tipoMovimiento = esModoAgregar ? "Agregar" : "Descuento";

                // 3. Procesar cada material del carrito escaneado
                foreach (var item in modelo.Materiales)
                {
                    var producto = await _appContext.Productos.FindAsync(item.ProductoID);

                    if (producto == null)
                    {
                        await dbTransaction.RollbackAsync();
                        return Json(new { success = false, message = $"El producto con ID {item.ProductoID} ya no existe." });
                    }

                    int stockActual = producto.Cantidad ?? 0;

                    if (esModoAgregar)
                    {
                        // MODO ENTRADA: Sumar piezas directamente al stock físico
                        producto.Cantidad = stockActual + item.Cantidad;
                    }
                    else
                    {
                        // MODO SALIDA: Validar existencias disponibles y restar
                        if (stockActual < item.Cantidad)
                        {
                            await dbTransaction.RollbackAsync();
                            return Json(new { success = false, message = $"Stock insuficiente para '{producto.NombreProducto}'. Disponible: {stockActual}, Solicitado: {item.Cantidad}" });
                        }
                        producto.Cantidad = stockActual - item.Cantidad;
                    }

                    // Actualizar stock del producto
                    _appContext.Productos.Update(producto);

                    // 4. Generar la fila de auditoría para la tabla de Movimientos
                    var nuevoMovimiento = new Movimientos
                    {
                        EmpleadoID = modelo.SolicitanteID,
                        ProductoID = item.ProductoID,
                        Tipo = tipoMovimiento, // Guardará de forma limpia "Descuento" o "Agregar"
                        Departamento = deptoEmpleado,
                        Fecha = DateTime.Now,
                        Cantidad = item.Cantidad
                    };

                    _appContext.Movimientos.Add(nuevoMovimiento);
                }

                // 5. Guardar el lote completo en SQL Server de manera segura
                await _appContext.SaveChangesAsync();

                // Confirmar la transacción
                await dbTransaction.CommitAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // En caso de cualquier error imprevisto (ej. pérdida de conexión), deshacer todos los movimientos
                await dbTransaction.RollbackAsync();
                return Json(new { success = false, message = "Error interno en el servidor: " + ex.Message });
            }
        }


    }

    // ================= VIEWMODELS PARA RECEPCIÓN DE DATOS JSON =================

    public class TransaccionSalidaViewModel
    {
        public int SolicitanteID { get; set; }
        public int ReceptorID { get; set; }
        public string? Modo { get; set; } // <--- Esta propiedad es obligatoria para capturar "AGREGAR" o "DESCUENTO"
        public List<MaterialItemViewModel> Materiales { get; set; } = new List<MaterialItemViewModel>();
    }


    public class MaterialItemViewModel
    {
        public int ProductoID { get; set; }
        public int Cantidad { get; set; }
    }
}
