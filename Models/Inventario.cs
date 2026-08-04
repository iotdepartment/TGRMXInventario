namespace TGRMXInventario.Models
{
    public class Inventario
    {
        public int Id { get; set; }
        public int? ProductoID { get; set; }
        public int? Stock { get; set; }
        public int? Min { get; set; }
        public int? Max { get; set; }
    }
}
