using Newtonsoft.Json;

namespace ExtractorFacturasAzure.Models
{
    public class FacturaDto
    {
        [JsonProperty("emisor")] public string? Emisor { get; set; }
        [JsonProperty("nit_o_id")] public string? NitOId { get; set; }
        [JsonProperty("fecha")] public string? Fecha { get; set; }
        [JsonProperty("total_pagar")] public decimal TotalPagar { get; set; }
        [JsonProperty("moneda")] public string? Moneda { get; set; }
    }
}
