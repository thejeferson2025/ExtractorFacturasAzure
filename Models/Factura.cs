using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ExtractorFacturasAzure.Models
{
    public class Factura
    {
        [Key]
        public int Id { get; set; }

        public string? Emisor { get; set; }
        public string? NitOId { get; set; }
        public string? Fecha { get; set; } 

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalPagar { get; set; }

        public string? Moneda { get; set; }

        // Campos de auditoría
        public DateTime FechaCreacion { get; set; } = DateTime.Now;
    }
}
    