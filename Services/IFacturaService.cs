using ExtractorFacturasAzure.Models;

namespace ExtractorFacturasAzure.Services
{
    public interface IFacturaService
    {
        Task<Factura> ProcesarYCrearFacturaAsync(Stream pdfStream, string fileName); // CREATE 
        Task<List<Factura>> ObtenerTodasAsync();                        // READ ALL
        Task<Factura?> ObtenerPorIdAsync(int id);                       // READ ONE BY ID
        Task<Factura?> ActualizarFacturaAsync(int id, Factura factura); // UPDATE
        Task<bool> EliminarFacturaAsync(int id);                        // DELETE
    }
}
