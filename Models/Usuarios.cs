namespace TGRMXInventario.Models
{
    public class Usuarios
    {
        public int Id { get; set; }
        public int? EmpleadoID { get; set; }
        public string? Jerarquia { get; set; }
        public string? Correo { get; set; }
        public string? Password { get; set; }
        public string? Rol { get; set; }

    }
}
