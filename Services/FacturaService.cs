using System.Text;
using UglyToad.PdfPig;
using Newtonsoft.Json;
using Microsoft.EntityFrameworkCore;
using ExtractorFacturasAzure.Data;
using Microsoft.Extensions.Configuration;
using ExtractorFacturasAzure.Models;

namespace ExtractorFacturasAzure.Services
{
    public class FacturaService : IFacturaService
    {
        private readonly ApplicationDbContext _context;
        private readonly string _apiKey;
        private readonly string _modelName;
        private readonly IHttpClientFactory _httpClientFactory;

        public FacturaService(ApplicationDbContext context, IConfiguration config, IHttpClientFactory httpClientFactory)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            
            _apiKey = config["Gemini:ApiKey"]!;
            _modelName = config["Gemini:Model"]!;

            if (string.IsNullOrWhiteSpace(_apiKey) || string.IsNullOrWhiteSpace(_modelName))
            {
                throw new InvalidOperationException("Faltan variables de configuración de Gemini en local.settings.json.");
            }
        }

        public async Task<Factura> ProcesarYCrearFacturaAsync(Stream pdfStream, string fileName)
        {
            Console.WriteLine($"--- INICIO PROCESO: {fileName} ---");
            
            using var initialStream = new MemoryStream();
            
            await pdfStream.CopyToAsync(initialStream); 
            initialStream.Position = 0;
            
            byte[] pdfBytes;
            
            using (var reader = new StreamReader(initialStream, leaveOpen: true))
            {
                string possibleBase64 = await reader.ReadToEndAsync();
                possibleBase64 = possibleBase64.Trim();

                if (IsBase64String(possibleBase64))
                {
                    Console.WriteLine("Detectado contenido en Base64. Decodificando...");
                    pdfBytes = Convert.FromBase64String(possibleBase64);
                }
                else
                {
                    Console.WriteLine("El archivo llegó en formato binario directo.");
                    pdfBytes = initialStream.ToArray();
                }
            }

            Console.WriteLine("1. Archivo procesado y listo para memoria.");

            var sb = new StringBuilder();
            try
            {
                using (var memoryStream = new MemoryStream(pdfBytes))
                {
                    using (var document = PdfDocument.Open(memoryStream))
                    {
                        foreach (var page in document.GetPages())
                        {
                            if (page.Text != null)
                            {
                                sb.Append(page.Text);
                                sb.Append(" ");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR FATAL LEYENDO PDF: {ex.Message}");
                throw;
            }

            string textoPdf = sb.ToString();
            Console.WriteLine($"2. Texto extraído. Longitud: {textoPdf.Length} caracteres.");

            if (string.IsNullOrWhiteSpace(textoPdf))
            {
                throw new Exception("No se pudo extraer texto del PDF. Puede ser una imagen escaneada.");
            }

            Console.WriteLine("3. Enviando a Gemini...");
            var datosExtraidos = await LlamarIA_Gemini(textoPdf);
            Console.WriteLine("4. Respuesta IA recibida correctamente.");

            var nuevaFactura = new Factura
            {
                Emisor = datosExtraidos.Emisor,
                NitOId = datosExtraidos.NitOId,
                Fecha = datosExtraidos.Fecha,
                TotalPagar = datosExtraidos.TotalPagar,
                Moneda = datosExtraidos.Moneda
            };

            _context.Facturas.Add(nuevaFactura);
            await _context.SaveChangesAsync();

            Console.WriteLine($"5. Factura guardada ID: {nuevaFactura.Id}");
            Console.WriteLine("--- FIN PROCESO ---");

            return nuevaFactura;
        }

        private bool IsBase64String(string base64)
        {
            if (string.IsNullOrEmpty(base64) || base64.Length % 4 != 0)
                return false;

            if (base64.StartsWith("JVBERi"))
                return true;

            Span<byte> buffer = new Span<byte>(new byte[base64.Length]);
            return Convert.TryFromBase64String(base64, buffer, out _);
        }

        private async Task<FacturaDto> LlamarIA_Gemini(string textoFactura)
        {
            string url = $"https://generativelanguage.googleapis.com/v1beta/models/{_modelName}:generateContent?key={_apiKey}";

            string instrucciones = @"
                Eres un experto en extracción de datos contables.
                Tu tarea es extraer la información de la factura y entregarla UNICAMENTE en formato JSON puro.
                Campos requeridos: emisor, nit_o_id, fecha, total_pagar (número decimal), moneda.
                Ejemplo JSON: { ""emisor"": ""ABC"", ""nit_o_id"": ""123"", ""fecha"": ""2023-01-01"", ""total_pagar"": 100.50, ""moneda"": ""USD"" }";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        parts = new[]
                        {
                            new { text = instrucciones + "\n\nTexto de la factura:\n" + textoFactura }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.1, 
                    responseMimeType = "application/json" 
                }
            };

            using var httpClient = _httpClientFactory.CreateClient();
            var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(url, content);
            
            if (!response.IsSuccessStatusCode)
            {
                var errorReal = await response.Content.ReadAsStringAsync();
                throw new Exception($"Gemini devolvió un error {response.StatusCode}: {errorReal}");
            }

            var responseString = await response.Content.ReadAsStringAsync();
            
            try
            {
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseString);
                string resText = jsonResponse.candidates[0].content.parts[0].text;
                resText = resText.Replace("```json", "").Replace("```", "").Trim();
                if (!resText.EndsWith("}"))
                {
                    resText += "\n}";
                }

                return JsonConvert.DeserializeObject<FacturaDto>(resText) ?? new FacturaDto();
            }
            catch (Exception ex)
            {
                throw new Exception("El JSON devuelto por Gemini no tiene el formato correcto.", ex);
            }
        }

        public async Task<List<Factura>> ObtenerTodasAsync()
        {
            return await _context.Facturas.ToListAsync();
        }

        public async Task<Factura?> ObtenerPorIdAsync(int id)
        {
            return await _context.Facturas.FindAsync(id);
        }

        public async Task<Factura?> ActualizarFacturaAsync(int id, Factura factura)
        {
            var existente = await _context.Facturas.FindAsync(id);
            if (existente == null) return null;

            existente.Emisor = factura.Emisor;
            existente.NitOId = factura.NitOId;
            existente.Fecha = factura.Fecha;
            existente.TotalPagar = factura.TotalPagar;
            existente.Moneda = factura.Moneda;

            await _context.SaveChangesAsync();
            return existente;
        }

        public async Task<bool> EliminarFacturaAsync(int id)
        {
            var factura = await _context.Facturas.FindAsync(id);
            if (factura == null) return false;

            _context.Facturas.Remove(factura);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}