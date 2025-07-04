using ConsolidadorHDD.model;
using System;
using System.Collections.Generic;
using System.IO; 
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading; 
using System.Threading.Tasks;


namespace ConsolidadorHDD.utils
{
    public class FileUploadService
    {
        private readonly string _nasIp;
        private readonly string _sessionId;
        private readonly string _destinationPath;

        // Constructor que inicializa el servicio con la configuración del NAS
        public FileUploadService(string nasIp, string sessionId, string destinationPath)
        {
            _nasIp = nasIp;
            _sessionId = sessionId;
            _destinationPath = destinationPath;
        }


        public async Task UploadFileAsync()
        {
            string filePath = "D:\\directoriopdf\\nas\\ejemplo3.txt";
            string uploadUrl= "https://nas.filantropiacortessolari.cl:1443/cgi-bin/filemanager/utilRequest.cgi?sid=wx0pai54&func=upload&type=standard&dest_path=/Multimedia&overwrite=1&progress=-ejemplo3.txt";


            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (HttpRequestMessage message, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors) =>
                {
                    return true; // Siempre devuelve true para aceptar cualquier certificado
                };
            //TODO: lo que hay que cambiar para subir
            //dest_path = directorio destino
            //progress = 
            //id de sesion = sid


            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, "https://nas.filantropiacortessolari.cl:1443/cgi-bin/filemanager/utilRequest.cgi?sid=wx0pai54&func=upload&type=standard&dest_path=/Multimedia&overwrite=1&progress=-ejemplo2.txt");
            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(File.OpenRead("D:\\directoriopdf\\nas\\ejemplo3.txt")), "file", "D:\\directoriopdf\\nas\\ejemplo3.txt");
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseStr = await response.Content.ReadAsStringAsync();

            Console.WriteLine(responseStr);




            /*
            using var httpClient = new HttpClient(handler);
            using var form = new MultipartFormDataContent();

            var fileStream = File.OpenRead(filePath);
            var streamContent = new StreamContent(fileStream);
            streamContent.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");

            form.Add(streamContent, "file", Path.GetFileName(filePath));

            var response = await httpClient.PostAsync(uploadUrl, form);
            response.EnsureSuccessStatusCode();

            string responseBody = await response.Content.ReadAsStringAsync();
            Console.WriteLine("Upload successful: " + responseBody);
            */
        }



        // Método asíncrono para subir un único archivo
        // fileProgress: Objeto que contiene la información del archivo y el estado de progreso
        // cancellationToken: Token para permitir la cancelación de la operación
        // progressCallback: Un Action que se invoca para actualizar el progreso en la UI
        public async Task UploadFileAsync(FileUploadProgress fileProgress, CancellationToken cancellationToken, Action<FileUploadProgress> progressCallback)
        {
            fileProgress.Status = "Initializing...";
            progressCallback?.Invoke(fileProgress); // Notificar estado inicial

            

            //https://nas.filantropiacortessolari.cl:1443/cgi-bin/filemanager/utilRequest.cgi?sid=wx0pai54&func=upload&type=standard&dest_path=/Multimedia&overwrite=1&progress=-ejemplo2.txt
            // Construir la URL de subida. 'func=upload' es una suposición.
            // Uri.EscapeDataString se usa para codificar correctamente la ruta de destino.
            string uploadUrl = $"https://{_nasIp}:1443/cgi-bin/filemanager/utilRequest.cgi?sid={_sessionId}&func=upload&type=standard&dest_path={_destinationPath}&overwrite=1&progress=-{Uri.EscapeDataString(fileProgress.FileName)}";

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (HttpRequestMessage message, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors) =>
                {
                    return true; // Siempre devuelve true para aceptar cualquier certificado
                };


            using (var httpClient = new HttpClient(handler))
            {
                try
                {
                    using (var content = new MultipartFormDataContent())
                    {
                        // Abrir el archivo para lectura
                        var fileStream = new FileStream(fileProgress.FilePath, FileMode.Open, FileAccess.Read);
                        fileProgress.TotalBytes = fileStream.Length; // Establecer el tamaño total del archivo

                        // Crear un ProgressableStreamContent para envolver el FileStream
                        // Esto nos permite reportar el progreso a medida que se lee y se envía el stream.
                        var streamContent = new ProgressableStreamContent(fileStream, (uploaded, total) =>
                        {
                            fileProgress.UploadedBytes = uploaded; // Actualizar bytes subidos
                            fileProgress.ProgressPercentage = (double)uploaded / total * 100; // Calcular porcentaje
                            fileProgress.Status = "Uploading..."; // Actualizar estado
                            progressCallback?.Invoke(fileProgress); // Invocar el callback para actualizar la UI
                        });

                        // Añadir el contenido del archivo a la parte 'file' del formulario multipart/form-data
                        // "file" es el nombre del campo esperado por el servidor para los datos del archivo.
                        content.Add(streamContent, "file");

                        fileProgress.Status = "Starting upload...";
                        progressCallback?.Invoke(fileProgress);

                        // Enviar la solicitud POST asíncronamente
                        HttpResponseMessage response = await httpClient.PostAsync(uploadUrl, content, cancellationToken);

                        // Verificar si la solicitud fue exitosa (código de estado 2xx)
                        if (response.IsSuccessStatusCode)
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            // Realizar una verificación básica del cuerpo de la respuesta JSON del NAS
                            // (ej. { "status": 1, "success": "true" })
                            if (responseBody.Contains("\"status\": 1"))
                            {
                                fileProgress.Status = "Upload Complete!";
                                fileProgress.ProgressPercentage = 100; // Asegurar que el progreso sea 100%
                            }
                            else
                            {
                                fileProgress.Status = $"Upload Failed: {responseBody}"; // Mostrar respuesta si no es exitosa
                            }
                        }
                        else
                        {
                            fileProgress.Status = $"Upload Failed: HTTP {response.StatusCode} - {response.ReasonPhrase}";
                        }
                    }
                }
                // Manejo de excepciones comunes
                catch (HttpRequestException ex)
                {
                    fileProgress.Status = $"Network Error: {ex.Message}";
                }
                catch (OperationCanceledException)
                {
                    fileProgress.Status = "Upload Canceled.";
                }
                catch (Exception ex)
                {
                    fileProgress.Status = $"Error: {ex.Message}";
                }
                finally
                {
                    progressCallback?.Invoke(fileProgress); // Asegurar una última actualización del estado
                }
            }
        }
    }

    // Clase auxiliar que extiende StreamContent para reportar el progreso de la escritura del stream
    public class ProgressableStreamContent : StreamContent
    {
        private const int DefaultBufferSize = 4096; // Tamaño del buffer para la lectura del stream
        private readonly Stream _content; // El stream subyacente (ej. FileStream)
        private readonly Action<long, long> _progress; // Callback para reportar el progreso

        // Constructor que toma el stream a enviar y el callback de progreso
        public ProgressableStreamContent(Stream content, Action<long, long> progress) : base(content)
        {
            _content = content;
            _progress = progress;
        }

        // Sobrescribe el método para serializar el contenido del stream
        protected override async Task SerializeToStreamAsync(Stream stream, System.Net.TransportContext context)
        {
            var buffer = new byte[DefaultBufferSize];
            long uploaded = 0; // Bytes subidos
            long total = _content.Length; // Bytes totales del stream

            using (_content) // Asegurarse de que el stream se cierre correctamente
            {
                while (true)
                {
                    // Leer un bloque de datos del stream de origen
                    var length = await _content.ReadAsync(buffer, 0, buffer.Length);
                    if (length <= 0) // Si no se leen más bytes, terminar
                        break;

                    uploaded += length; // Incrementar bytes subidos
                    _progress?.Invoke(uploaded, total); // Invocar el callback con el progreso actual

                    // Escribir el bloque de datos en el stream de destino (el stream de la solicitud HTTP)
                    await stream.WriteAsync(buffer, 0, length);
                }
            }
        }
    }

}
