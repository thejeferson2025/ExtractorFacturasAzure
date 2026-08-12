using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using ExtractorFacturasAzure.Services;
using ExtractorFacturasAzure.Models;
using System.Net;
using HttpMultipartParser;
using Newtonsoft.Json;

namespace ExtractorFacturasAzure.Functions
{
    public class ProcesarFactura
    {
        private readonly IFacturaService _service;
        private readonly ILogger<ProcesarFactura> _logger;

        public ProcesarFactura(IFacturaService service, ILogger<ProcesarFactura> logger)
        {
            _service = service;
            _logger = logger;
        }

        // CREATE: POST api/Facturas/update
        [Function("CrearFactura")]
        public async Task<HttpResponseData> Crear(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "Facturas/update")] HttpRequestData req)
        {
            _logger.LogInformation("Ejecutando Crear Factura (POST).");

            try
            {
                var parsedFormBody = await MultipartFormDataParser.ParseAsync(req.Body);
                var archivo = parsedFormBody.Files.FirstOrDefault(f => f.Name == "archivo");

                if (archivo == null)
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Debe subir un archivo PDF.");
                    return badResponse;
                }

                var extension = Path.GetExtension(archivo.FileName).ToLower();
                if (extension != ".pdf")
                {
                    var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                    await badResponse.WriteStringAsync("Solo se admiten archivos PDF.");
                    return badResponse;
                }

                var factura = await _service.ProcesarYCrearFacturaAsync(archivo.Data, archivo.FileName);

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(new { Mensaje = "Éxito", Id = factura.Id });
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Crear: {ex.Message}");
                var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
                await errorResponse.WriteStringAsync($"Error: {ex.Message}");
                return errorResponse;
            }
        }

        // READ (Todas): GET api/Facturas
        [Function("ObtenerTodasFacturas")]
        public async Task<HttpResponseData> ObtenerTodas(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Facturas")] HttpRequestData req)
        {
            _logger.LogInformation("Ejecutando Obtener Todas (GET).");
            
            try
            {
                var facturas = await _service.ObtenerTodasAsync();
                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(facturas);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Obtener Todas: {ex.Message}");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        // READ (Por ID): GET api/Facturas/{id}
        [Function("ObtenerFacturaPorId")]
        public async Task<HttpResponseData> Obtener(
            [HttpTrigger(AuthorizationLevel.Function, "get", Route = "Facturas/{id}")] HttpRequestData req, int id)
        {
            _logger.LogInformation($"Ejecutando Obtener por ID (GET): {id}");
            
            try
            {
                var factura = await _service.ObtenerPorIdAsync(id);
                if (factura == null)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(factura);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Obtener por ID: {ex.Message}");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        // UPDATE: PUT api/Facturas/{id}
        [Function("ActualizarFactura")]
        public async Task<HttpResponseData> Actualizar(
            [HttpTrigger(AuthorizationLevel.Function, "put", Route = "Facturas/{id}")] HttpRequestData req, int id)
        {
            _logger.LogInformation($"Ejecutando Actualizar (PUT): {id}");
            
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var factura = JsonConvert.DeserializeObject<Factura>(requestBody);

                if (factura == null)
                {
                     var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                     await badResponse.WriteStringAsync("Cuerpo de la petición inválido.");
                     return badResponse;
                }

                var actualizado = await _service.ActualizarFacturaAsync(id, factura);
                
                if (actualizado == null)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                var response = req.CreateResponse(HttpStatusCode.OK);
                await response.WriteAsJsonAsync(actualizado);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Actualizar: {ex.Message}");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }

        // DELETE: DELETE api/Facturas/{id}
        [Function("EliminarFactura")]
        public async Task<HttpResponseData> Eliminar(
            [HttpTrigger(AuthorizationLevel.Function, "delete", Route = "Facturas/{id}")] HttpRequestData req, int id)
        {
            _logger.LogInformation($"Ejecutando Eliminar (DELETE): {id}");
            
            try
            {
                var exito = await _service.EliminarFacturaAsync(id);
                if (!exito)
                {
                    return req.CreateResponse(HttpStatusCode.NotFound);
                }

                return req.CreateResponse(HttpStatusCode.NoContent);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error en Eliminar: {ex.Message}");
                return req.CreateResponse(HttpStatusCode.InternalServerError);
            }
        }
    }
}