using ConsolidadorHDD.model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO; 
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading; 
using System.Threading.Tasks;


namespace ConsolidadorHDD.utils
{
    public class FileUploadService
    {
        private readonly string _nasIp;
        private readonly string _sessionId;
        private readonly string _destinationPath;
        private readonly string _diskPath;

        private readonly ConcurrentBag<string> listaDirectorios;


        // Constructor que inicializa el servicio con la configuración del NAS
        public FileUploadService(string nasIp, string sessionId, string destinationPath, string diskPath, ConcurrentBag<string> Directorios)
        {
            _nasIp = nasIp;
            _sessionId = sessionId;
            _destinationPath = destinationPath;
            _diskPath = diskPath;
            listaDirectorios = Directorios;
        }

        public const string CREATEDIR = "createdir";
        public const string UPLOADFILE = "upload";


        public async Task<bool> RenameFile(string directory, string sourceName, string destName)
        {
            string sid = _sessionId;

            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (HttpRequestMessage message, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors) =>
                {
                    return true; // Siempre devuelve true para aceptar cualquier certificado
                };

            string Url = $"https://nas.filantropiacortessolari.cl:1443/cgi-bin/filemanager/utilRequest.cgi?func=rename&sid={sid}&path={directory}&source_name={sourceName}&dest_name={destName}";
            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Get, Url);
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseStr = await response.Content.ReadAsStringAsync();
            Debug.WriteLine(responseStr);
            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                return true;
            }
            else
            {
                return false;
            }
            
        }





        public async Task<bool> CreateDir(string BaseNASDir, string directory) {
            string sid = _sessionId;
            
            List<string> directorios = directory.Split("/").ToList();
            string NewBaseDir = BaseNASDir;
            //https://nas.filantropiacortessolari.cl:1443/cgi-bin/filemanager/utilRequest.cgi?func=v&sid=xsdsmbg3&dest_folder=HDDExt&dest_path=/Discos
            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (HttpRequestMessage message, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors) =>
                {
                    return true; // Siempre devuelve true para aceptar cualquier certificado
                };

            bool rerturnValue = true;
            foreach (var item in directorios)
            {
                string newDir = item;
                if (!buscaDirectorio(NewBaseDir + "/" + item))
                {
                    string Url = $"https://nas.filantropiacortessolari.cl:1443/cgi-bin/filemanager/utilRequest.cgi?sid={sid}&func={CREATEDIR}&dest_folder={newDir}&dest_path={NewBaseDir}";
                    var client = new HttpClient(handler);
                    var request = new HttpRequestMessage(HttpMethod.Get, Url);
                    var response = await client.SendAsync(request);
                    response.EnsureSuccessStatusCode();
                    var responseStr = await response.Content.ReadAsStringAsync();
                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        NewBaseDir += "/" + item;
                        listaDirectorios.Add(NewBaseDir);
                    }
                    else
                    {

                        rerturnValue = false;
                        break;
                    }
                    Debug.WriteLine(responseStr);
                }
                else {
                    NewBaseDir += "/" + item;
                }
                
            }
            return rerturnValue;
        }

        public bool buscaDirectorio(string dir) {
            var encontro = listaDirectorios.Where(x => x == dir);
            if (encontro.Any()) {
                return true;
            }
            return false;
        }


        public async Task UploadFileAsync() //string directory, string fileName)
        {
            var BaseDir = "/Multimedia";
            var DestPath = _destinationPath;// "HDD/SAMPLE_DISK/ejemplo1/salpledir";
            var AllDestPAth = BaseDir + "/" + DestPath;

            var responseDir = await CreateDir(BaseDir, DestPath);


            string sid = _sessionId;
            string filePath = "D:\\directoriopdf\\nas\\ejemplo3.txt";
            string uploadUrl= $"https://nas.filantropiacortessolari.cl:1443/cgi-bin/filemanager/utilRequest.cgi?sid={sid}&func=upload&type=standard&dest_path={AllDestPAth}&overwrite=1&progress=-ejemplo3.txt";


            var handler = new HttpClientHandler();
            handler.ServerCertificateCustomValidationCallback =
                (HttpRequestMessage message, X509Certificate2 cert, X509Chain chain, SslPolicyErrors errors) =>
                {
                    return true; // Siempre devuelve true para aceptar cualquier certificado
                };

            var client = new HttpClient(handler);
            var request = new HttpRequestMessage(HttpMethod.Post, uploadUrl);
            var content = new MultipartFormDataContent();
            content.Add(new StreamContent(File.OpenRead("D:\\directoriopdf\\nas\\ejemplo3.txt")), "file", "D:\\directoriopdf\\nas\\ejemplo3.txt");
            request.Content = content;
            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
            var responseStr = await response.Content.ReadAsStringAsync();

            Console.WriteLine(responseStr);


        }

        public async Task UploadFileAsync(FileUploadProgress fileProgress, CancellationToken cancellationToken, Action<FileUploadProgress> progressCallback) {
            fileProgress.Status = Estados.STARTED;
            fileProgress.StatusMSG = "Inicializando...";

            progressCallback?.Invoke(fileProgress); // Notificar estado inicial
            var BaseDir = _destinationPath;// "/Multimedia";
            var onlyPath = Path.GetDirectoryName(fileProgress.FilePath);
            var DestPath = _diskPath + "/" + Path.GetRelativePath(Path.GetPathRoot(onlyPath), onlyPath).Replace("\\", "/");
            var AllDestPAth = BaseDir + "/" + DestPath;// + "/" + fileProgress.FileName;
            string encodedStringPath = AllDestPAth; // Uri.EscapeDataString(AllDestPAth);
            var responseDir = await CreateDir(BaseDir, DestPath);

            string sid = _sessionId;
            string filePath = fileProgress.FilePath;// "D:\\directoriopdf\\nas\\ejemplo3.txt";
            string fileName = "-" + Uri.EscapeDataString(fileProgress.FileName);// "ejemplo3.txt";
            string uploadUrl = $"https://nas.filantropiacortessolari.cl:1443/cgi-bin/filemanager/utilRequest.cgi?sid={sid}&func=upload&type=standard&dest_path={encodedStringPath}&overwrite=1&progress={fileName}";

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
                        var fileStream = new FileStream(fileProgress.FilePath, FileMode.Open, FileAccess.Read);
                        fileProgress.TotalBytes = fileStream.Length; // Establecer el tamaño total del archivo
                        fileProgress.Status = Estados.UPLOADING;
                        fileProgress.StatusMSG = "Subiendo...";
                        
                        content.Add(
                            new StreamContent(fileStream),
                            "file",
                            filePath);

                        progressCallback?.Invoke(fileProgress);

                        HttpResponseMessage response = await httpClient.PostAsync(uploadUrl, content, cancellationToken);
                        if (response.IsSuccessStatusCode)
                        {
                            string responseBody = await response.Content.ReadAsStringAsync();
                            ResponseUpload responseJSON = JsonSerializer.Deserialize<ResponseUpload>(responseBody);
                            if (responseJSON != null && responseJSON.status == 1)
                            {
                                if (fileProgress.FileName == responseJSON.name && fileProgress.TotalBytes == long.Parse(responseJSON.size)) {
                                    fileProgress.Status = Estados.SUCCESS;
                                } else if (fileProgress.FileName != responseJSON.name)
                                {

                                    bool result = await RenameFile(encodedStringPath, responseJSON.name, fileProgress.FileName);
                                    if (result)
                                    {
                                        fileProgress.Status = Estados.SUCCESS;
                                        fileProgress.StatusMSG += "Error de Nombre del archivo, reparado!!";

                                    }
                                    else {
                                        fileProgress.Status = Estados.NAMEERROR;
                                        fileProgress.StatusMSG += "Error de Nombre del archivo::";
                                    }

                                } else if (fileProgress.TotalBytes != long.Parse(responseJSON.size))
                                {
                                    fileProgress.Status = Estados.SIZEERROR;
                                    fileProgress.StatusMSG += "Error de Tamaño de archivo::";
                                }
                            }
                            else if (responseJSON != null && responseJSON.status == 3) {
                                fileProgress.StatusMSG += "Error de sesion verifica tu token::";
                                fileProgress.Status = Estados.ERROR;
                            }
                            else
                            {
                                fileProgress.Status = Estados.ERROR;
                            }
                            fileProgress.StatusMSG += responseBody;

                            fileProgress.ProgressPercentage = 100; // Asegurar que el progreso sea 100%
                        }
                        else
                        {
                            fileProgress.Status = Estados.ERROR;
                            fileProgress.StatusMSG = $"Upload Failed: HTTP {response.StatusCode} - {response.ReasonPhrase}";
                        }

                    }
                }
                // Manejo de excepciones comunes
                catch (HttpRequestException ex)
                {
                    fileProgress.Status = Estados.NETERROR;
                    fileProgress.StatusMSG = $"Network Error: {ex.Message}";
                }
                catch (OperationCanceledException)
                {
                    fileProgress.Status = Estados.CANCEL;
                    fileProgress.StatusMSG = "Upload Canceled.";

                }
                catch (Exception ex)
                {
                    fileProgress.Status = Estados.ERROR;
                    fileProgress.StatusMSG = $"Error: {ex.Message}";
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
