using ConsolidadorHDD.model;
using ConsolidadorHDD.utils;
using Microsoft.Win32;
using MS.WindowsAPICodePack.Internal;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq; 
using System.Text;
using System.Threading; 
using System.Threading.Tasks;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Path = System.IO.Path;



namespace ConsolidadorHDD
{
    /// <summary>
    /// Lógica de interacción para Win_upload.xaml
    /// </summary>
    public partial class Win_upload : Window
    {
        // ObservableCollection para almacenar la lista de archivos a subir
        // y permitir que la UI (ListView) se actualice automáticamente
        public ObservableCollection<FileUploadProgress> FilesToUpload { get; set; }

        public static ConcurrentBag<string> CreatedDirectory { get; set; }

        // CancellationTokenSource para gestionar la cancelación de las operaciones de subida
        private CancellationTokenSource _cancellationTokenSource;

        private static SemaphoreSlim _semaphore = new SemaphoreSlim(4);
        private static List<FileData> HDDDataWin = new List<FileData>();
        private static List<SelectedExtencion> _extencionSelection = new List<SelectedExtencion>();

        private static int Intentos = 1;

        public Win_upload()
        {
            InitializeComponent();
            FilesToUpload = new ObservableCollection<FileUploadProgress>();
            // Asignar la colección como fuente de datos para el ListView
            lvFiles.ItemsSource = FilesToUpload;
        }

        public Win_upload(List<FileData> HDDData)
        {
            InitializeComponent();
            FilesToUpload = new ObservableCollection<FileUploadProgress>();
            // Asignar la colección como fuente de datos para el ListView
            HDDDataWin = HDDData;



            lvFiles.ItemsSource = FilesToUpload;


            var extencionList = HDDDataWin
                .GroupBy(x => x.Extension).ToList();

            extencionList.ForEach(x => {
                _extencionSelection.Add(new SelectedExtencion
                {
                     Selected = true,
                     Extencion = x.Key
                });
            });
            

            ListadoExtenciones.ItemsSource = _extencionSelection;
        }

        // Evento Click del botón "Select Files"
        private void SelectFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = true; // Permitir la selección de múltiples archivos

            // Mostrar el diálogo y procesar los archivos seleccionados si el usuario hace clic en OK
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (string filePath in openFileDialog.FileNames)
                {
                    FilesToUpload.Add(new FileUploadProgress
                    {
                        FileName = Path.GetFileName(filePath), // Obtener solo el nombre del archivo
                        FilePath = filePath, // Guardar la ruta completa
                        Status = Estados.WAITING, // Estado inicial
                        StatusMSG = "Listo...",
                        ProgressPercentage = 0, // Progreso inicial
                        UploadedBytes = 0, // Bytes subidos iniciales
                        TotalBytes = new FileInfo(filePath).Length, // Obtener el tamaño total del archivo

                    });
                }
            }
        }

        private async Task UploadFilesProcess(bool reintentar = false) {
            if (!FilesToUpload.Any())
            {
                MessageBox.Show("Please select files to upload first.", "No Files Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            int num;
            if (txtMaxThread.Text != "" && int.TryParse(txtMaxThread.Text, out num))
            {
                _semaphore = new SemaphoreSlim(num);
                Console.WriteLine("La cadena es un número entero. El valor es: " + num);
            }
            else
            {
                _semaphore = new SemaphoreSlim(4);
                Console.WriteLine("La cadena no es un número entero.");
            }

            // Obtener la configuración del NAS de los campos de texto de la UI
            string nasIp = txtNasIp.Text;
            string sessionId = txtSessionId.Text;
            string destinationPath = txtDestinationPath.Text;
            string diskPath = txtSubDestinationPath.Text;

            // Validar que los campos no estén vacíos
            if (string.IsNullOrWhiteSpace(destinationPath) || string.IsNullOrWhiteSpace(nasIp))// || string.IsNullOrWhiteSpace(sessionId) )
            {
                MessageBox.Show("Please enter NAS IP, Session ID, and Destination Path.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            txtOverallStatus.Text = "Empesando subida...";
            _cancellationTokenSource = new CancellationTokenSource();


            var uploadTasks = new ObservableCollection<Task>();
            CreatedDirectory = new ConcurrentBag<string>();


            var archivosUpload = new ObservableCollection<FileUploadProgress>();
            if (reintentar) {
                var listadoFiltrado = FilesToUpload.Where(x =>
                            x.Status == Estados.CANCEL ||
                            x.Status == Estados.SIZEERROR ||
                            x.Status == Estados.NAMEERROR ||
                            x.Status == Estados.ERROR ||
                            x.Status == Estados.NETERROR);
                foreach (var item in listadoFiltrado)
                {
                    try
                    {
                        archivosUpload.Add(item);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("error ", item.FileName, ex.Message);
                    }

                }

            } else {
                archivosUpload = FilesToUpload;
            }
            txtTodos.Text = FilesToUpload.Count().ToString();

            foreach (var fileProgress in archivosUpload)
                {
                    fileProgress.Status = Estados.QUEUED;
                    fileProgress.StatusMSG = "En cola";
                    fileProgress.ProgressPercentage = 0;
                    fileProgress.UploadedBytes = 0;

                    FileUploadService uploader = new FileUploadService(nasIp, sessionId, destinationPath, diskPath, CreatedDirectory);
                    uploadTasks.Add(Task.Run(async () =>
                    {
                        await _semaphore.WaitAsync();
                        try
                        {
                            await uploader.UploadFileAsync(fileProgress, _cancellationTokenSource.Token, (progress) =>
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    // Las propiedades de fileProgress ya notifican a la UI (INotifyPropertyChanged),
                                    // así que no necesitamos hacer nada más aquí explícitamente para actualizar la fila.
                                    var result = FilesToUpload.Where(x => x.Status == Estados.SUCCESS).Count();
                                    var resultERROR = FilesToUpload.Where(x => x.Status == Estados.ERROR).Count();
                                    var resultNET = FilesToUpload.Where(x => x.Status == Estados.NETERROR).Count();
                                    var resultNAME = FilesToUpload.Where(x => x.Status == Estados.NAMEERROR).Count();
                                    var resultSIZE = FilesToUpload.Where(x => x.Status == Estados.SIZEERROR).Count();
                                    var resultCANCEL = FilesToUpload.Where(x => x.Status == Estados.CANCEL).Count();

                                    
                                    txtSubidos.Text = result.ToString();
                                    txtError.Text = resultERROR.ToString();
                                    txtErrornet.Text = resultNET.ToString();
                                    txtErrorName.Text = resultNAME.ToString();
                                    txtErrorSize.Text = resultSIZE.ToString();
                                    txtCancelados.Text = resultCANCEL.ToString();
                                    /*
                                    txtmsgSubidos.Text = $"{result} subidos exitosos," +
                                                         $" {resultERROR} con error" +
                                                         $" {resultNET} con Error de red" +
                                                         $" {resultNAME} con Mal nombre" +
                                                         $" {resultSIZE} con Mal tamaño" +
                                                         $" {resultCANCEL} Cancelados" ;
                                    */
                                });
                            });

                        }
                        catch (Exception)
                        {


                        }
                        finally
                        {
                            _semaphore.Release();
                        }

                    }, _cancellationTokenSource.Token)); // Pasar el CancellationToken
                }
            //);

            try
            {
                // Esperar a que todas las tareas de subida se completen
                await Task.WhenAll(uploadTasks);
                txtOverallStatus.Text = "Todos los archivos Subidos.";
            }
            catch (OperationCanceledException)
            {
                txtOverallStatus.Text = "Alguno de los archivos fue cancelado.";
            }
            catch (Exception ex)
            {
                txtOverallStatus.Text = $"Un error ocurrio cuando estaba subiendo: {ex.Message}";
            }
            finally
            {
                // Limpiar y liberar los recursos del CancellationTokenSource
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                var total = FilesToUpload.Count();
                var result = FilesToUpload.Where(x => x.Status == Estados.SUCCESS).Count();
                if (total != result)
                {
                    btnReSubir.Visibility = Visibility.Visible;
                }
                else
                {
                    btnReSubir.Visibility = Visibility.Hidden;
                }
                Debug.WriteLine("--------------------------------------------------");
                Debug.WriteLine("Finalizar proceso");
                Debug.WriteLine($"total:{total}");
                Debug.WriteLine($"result:{result}");
                Debug.WriteLine("--------------------------------------------------");
            }
        }



        // Método opcional para cancelar todas las subidas.
        // Podrías añadir un botón en el XAML para llamar a este método.
        private void CancelUploads()
        {
            _cancellationTokenSource?.Cancel(); // Solicitar la cancelación de todas las tareas
            txtOverallStatus.Text = "Cancelando las subidas...";
            Debug.WriteLine("--------------------------------------------------");
            Debug.WriteLine("Cancelando proceso");
            Debug.WriteLine("--------------------------------------------------");
        }

        private void ListadoExtenciones_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnFiltrar_Click(object sender, RoutedEventArgs e)
        {
            Filtrar();
        }

        private void Filtrar() {

            var DiscLeter = txtLetraDisco.Text;

            var filterFilter = HDDDataWin
                        .Join(_extencionSelection,
                            o1 => o1.Extension,
                            o2 => o2.Extencion,
                            (o1, o2) => new { o1, o2 })
                        .Where(x => x.o2.Selected == true && x.o1.isNas != true && !x.o1.Nombre.StartsWith(txtStartWithOut.Text))
                        .OrderBy(x => x.o1.Hash)
                        .GroupBy(x => x.o1.Hash)
                        .Select(g => g.First().o1) // solo el primer Objeto1 por hash
                        .ToList();
            FilesToUpload.Clear();
            foreach (var item in filterFilter)
            {
                try
                {
                    string fullPath = item.Directorios;
                    string driveRoot = Path.GetPathRoot(fullPath);
                    if (DiscLeter.ToUpper() != driveRoot.ToUpper()) {
                        item.Directorios = item.Directorios.Replace(driveRoot, DiscLeter);
                    }


                    FilesToUpload.Add(new FileUploadProgress
                    {
                        FileName = item.Nombre, // hdd.Nombre, // Obtener solo el nombre del archivo
                        FilePath = item.Directorios + @"\" + item.Nombre, // Guardar la ruta completa
                        Status = Estados.STARTED, // Estado inicial
                        StatusMSG = "LISTO",
                        ProgressPercentage = 0, // Progreso inicial
                        UploadedBytes = 0, // Bytes subidos iniciales
                        TotalBytes = new FileInfo(item.Directorios + @"\" + item.Nombre).Length, // Obtener el tamaño total del archivo
                        Tamaño = item.Tamaño,
                        //Extension = hdd.Extension,
                        Hash = item.Hash,
                        IsNAS = item.isNas,
                        IsRepited = item.isRepited
                    });
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("error ", item.Nombre, ex.Message);
                }

            }
            var cuantosSeleccionados = FilesToUpload.Count();
            var pesoTotal = FilesToUpload.Sum(x => x.TotalBytes);
            txtMensaje.Text = $"{cuantosSeleccionados} Archivos seleccionados, {FileSizeExtension.ToHumanReadableString(pesoTotal)}.";
            txtTodos.Text = cuantosSeleccionados.ToString();
        
        }


        private void lvFiles_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            Debug.WriteLine(e);
        }



        // Evento Click del botón "Upload Files"
        private async void UploadFiles_Click(object sender, RoutedEventArgs e)
        {
            btnUpload.IsEnabled = false;
            await UploadFilesProcess();
            btnUpload.IsEnabled = true;
        }

        private async void btnSubir_Click(object sender, RoutedEventArgs e)
        {
            Intentos = 0;
            btnSubir.IsEnabled = false;
            btnSubir.Visibility = Visibility.Hidden;
            btnCancelarSubir.Visibility = Visibility.Visible;
            int maxRetry = 0;
            if (!int.TryParse(txtMaxRetry.Text, out maxRetry)) {
                MessageBox.Show("El maximo numero de intentos tiene que ser un numero entero", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            //int intento = 1;
            await UploadFilesProcess();
            
            while (Intentos < maxRetry)
            {
                txtOverallStatus.Text = $"Reintentando intento {Intentos}";
                await UploadFilesProcess(true);
                Intentos++;
            }
            txtOverallStatus.Text = $"Proceso terminado intentos:{Intentos}";

            btnSubir.IsEnabled = true;
            btnSubir.Visibility = Visibility.Visible;
            btnCancelarSubir.Visibility = Visibility.Hidden;


        }

        private void btnCancelarSubir_Click(object sender, RoutedEventArgs e)
        {
            Intentos = 200000000;
            CancelUploads();
            var total = FilesToUpload.Count();
            var result = FilesToUpload.Where(x => x.Status == Estados.SUCCESS).Count();
            if (total != result)
            {
                btnReSubir.Visibility = Visibility.Visible;
            }
            else
            {
                btnReSubir.Visibility = Visibility.Hidden;
            }
            txtOverallStatus.Text = $"Proceso cancelado";

            Debug.WriteLine("--------------------------------------------------");
            Debug.WriteLine("Cancelar proceso");
            Debug.WriteLine($"total:{total}");
            Debug.WriteLine($"result:{result}");
            Debug.WriteLine("--------------------------------------------------");
        }

        private async void btnReSubir_Click(object sender, RoutedEventArgs e)
        {
            btnReSubir.IsEnabled = false;
            btnSubir.Visibility = Visibility.Hidden;
            btnCancelarSubir.Visibility = Visibility.Visible;
            await UploadFilesProcess(true);
            btnReSubir.IsEnabled = true;
            btnSubir.IsEnabled = true;
            btnSubir.Visibility = Visibility.Visible;
            btnCancelarSubir.Visibility = Visibility.Hidden;

        }
    }

}
