namespace TGRMXInventario.Models
{
    public class UsuarioListaViewModel
    {
        public int Id { get; set; }
        public int? EmpleadoID_PK { get; set; } // El Id primario de rh4 necesario para los modales
        public string? NumeroEmpleado { get; set; } // El campo EMPLEADO (nómina)
        public string? NombreCompleto { get; set; } // NOMBRECOMPLETO de rh4
        public string? Jerarquia { get; set; }
        public string? Correo { get; set; }
        public string? Password { get; set; }
        public string? Rol { get; set; }
    }
}
