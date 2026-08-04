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


        // 2. API para buscar el material por su código/SKU al ser escaneado
        // GET: /Descontar/ObtenerProducto?codigo=ABC-123
        [HttpGet]
        public async Task<IActionResult> ObtenerProducto(string codigo)
        {
            if (string.IsNullOrWhiteSpace(codigo))
            {
                return Json(new { success = false, message = "Código de material vacío." });
            }

            // Buscamos el producto por su Nombre o puedes adaptar el campo si tienes una columna 'Codigo'
            // Nota: Aquí asumo que buscas por NombreProducto o un identificador único en AppDbContext
            var producto = await _appContext.Productos
                .Include(p => p.Categoria) // Incluye la relación de categorías
                .FirstOrDefaultAsync(p => p.NombreProducto != null && p.NombreProducto.ToUpper() == codigo.ToUpper() || p.Id.ToString() == codigo);

            if (producto == null)
            {
                return Json(new { success = false, message = "El material escaneado no está registrado en el inventario." });
            }

            return Json(new
            {
                success = true,
                producto = new
                {
                    id = producto.Id,
                    codigo = producto.Id, // Usamos el ID como código visual o ajusta a tu columna código
                    nombre = producto.NombreProducto,
                    categoria = producto.Categoria != null ? producto.Categoria.NombreCategoria : "General"
                }
            });
        }

        // 3. API POST para procesar el lote completo, restar stock y guardar los cambios
        // POST: /Descontar/ProcesarDescuento
        [HttpPost]
        public async Task<IActionResult> ProcesarDescuento([FromBody] TransaccionSalidaViewModel modelo)
        {
            if (modelo == null || modelo.Materiales == null || !modelo.Materiales.Any())
            {
                return Json(new { success = false, message = "La solicitud no contiene materiales para descontar." });
            }

            // Iniciamos una transacción de base de datos para asegurar consistencia (si falla uno, no se descuenta ninguno)
            using var dbTransaction = await _appContext.Database.BeginTransactionAsync();

            try
            {
                foreach (var item in modelo.Materiales)
                {
                    var producto = await _appContext.Productos.FindAsync(item.ProductoID);

                    if (producto == null)
                    {
                        await dbTransaction.RollbackAsync();
                        return Json(new { success = false, message = $"El producto con ID {item.ProductoID} ya no existe." });
                    }

                    // Validación de Stock disponible
                    int stockActual = producto.Cantidad ?? 0;
                    if (stockActual < item.Cantidad)
                    {
                        await dbTransaction.RollbackAsync();
                        return Json(new { success = false, message = $"Stock insuficiente para '{producto.NombreProducto}'. Disponible: {stockActual}, Solicitado: {item.Cantidad}" });
                    }

                    // Restamos la cantidad del inventario de forma segura
                    producto.Cantidad = stockActual - item.Cantidad;
                    _appContext.Productos.Update(producto);
                }

                // Guardamos los cambios de stock en la base de datos
                await _appContext.SaveChangesAsync();

                // Confirmamos la transacción con éxito
                await dbTransaction.CommitAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                // Si ocurre cualquier error inesperado, revertimos los cambios aplicados en este lote
                await dbTransaction.RollbackAsync();
                return Json(new { success = false, message = "Error interno del servidor: " + ex.Message });
            }
        }
    }

    // ================= VIEWMODELS PARA RECEPCIÓN DE DATOS JSON =================

    public class TransaccionSalidaViewModel
    {
        public int SolicitanteID { get; set; }
        public int ReceptorID { get; set; }
        public List<MaterialItemViewModel> Materiales { get; set; } = new List<MaterialItemViewModel>();
    }

    public class MaterialItemViewModel
    {
        public int ProductoID { get; set; }
        public int Cantidad { get; set; }
    }
}
