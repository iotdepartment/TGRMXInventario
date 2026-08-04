namespace TGRMXInventario.Models
{
    public class Movimientos
    {
        public int Id { get; set; }
        public int? EmpleadoID { get; set; }
        public int? ProductoID { get; set; }
        public string? Tipo { get; set; }
        public string? Departamento { get; set; }
        public DateTime? Fecha { get; set; }
        public int? Cantidad { get; set; }
    }
}
