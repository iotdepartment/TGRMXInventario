namespace TGRMXInventario.Models
{
    public class rh4
    {
        public int Id { get; set; }
        public int? EMPLEADO { get; set; }
        public string? NOMBRECOMPLETO { get; set; }
        public DateTime? ALTA { get; set; }
        public string? PUESTO_DESCRIPCION { get; set; }
        public string? DEPTO_DESCRIPCION { get; set; }
        public int? EMPLEADO_SUPERVISOR { get; set; }
        public string? SUPERVISOR_EMPLEADO { get; set; }
        public string? HORARIO_COMBO { get; set; }
        public string? SINDCONF { get; set; }
        public string? COSTO_COMBO { get; set; }
    }
}
