namespace TGRMXInventario.Models
{
    public class Productos
    {
        public int Id { get; set; }
        public string? NombreProducto { get; set; }
        public int? CategoriaID { get; set; }
        // Propiedad de navegación para Categoría
        public virtual Categorias? Categoria { get; set; }
        public int? ProveedorID { get; set; }
        // Propiedad de navegación para Proveedor
        public virtual Proveedores? Proveedor { get; set; }
        public int? Costo { get; set; }
        public string? Unidad { get; set; }
        public string? Moneda { get; set; }
        public int? Cantidad { get; set; }
        public int? Min { get; set; }
        public int? Max { get; set; }
        public string? Area { get; set; }
    }
}
