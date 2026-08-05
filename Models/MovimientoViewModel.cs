namespace TGRMXInventario.Models
{
    public class MovimientoViewModel
    {
        public int Id { get; set; }
        public string? NumeroEmpleado { get; set; }
        public string? NombreEmpleado { get; set; }
        public string? NombreProducto { get; set; }
        public string? Tipo { get; set; }
        public string? Departamento { get; set; }
        public DateTime? Fecha { get; set; }
        public int? Cantidad { get; set; }
    }
}
