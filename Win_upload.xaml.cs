using ConsolidadorHDD.model;
using ConsolidadorHDD.utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq; 
using System.Threading; 
using System.Threading.Tasks;
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
        // CancellationTokenSource para gestionar la cancelación de las operaciones de subida
        private CancellationTokenSource _cancellationTokenSource;

        public Win_upload()
        {
            InitializeComponent();
            FilesToUpload = new ObservableCollection<FileUploadProgress>();
            // Asignar la colección como fuente de datos para el ListView
            lvFiles.ItemsSource = FilesToUpload;
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
                        Status = "Ready", // Estado inicial
                        ProgressPercentage = 0, // Progreso inicial
                        UploadedBytes = 0, // Bytes subidos iniciales
                        TotalBytes = new FileInfo(filePath).Length // Obtener el tamaño total del archivo
                    });
                }
            }
        }

        // Evento Click del botón "Upload Files"
        private async void UploadFiles_Click(object sender, RoutedEventArgs e)
        {
            FileUploadService uploader2 = new FileUploadService("", "", "");
            await uploader2.UploadFileAsync();

            // Verificar si hay archivos seleccionados para subir
            if (!FilesToUpload.Any())
            {
                MessageBox.Show("Please select files to upload first.", "No Files Selected", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Obtener la configuración del NAS de los campos de texto de la UI
            string nasIp = txtNasIp.Text;
            string sessionId = txtSessionId.Text;
            string destinationPath = txtDestinationPath.Text;

            // Validar que los campos no estén vacíos
            if (string.IsNullOrWhiteSpace(nasIp) || string.IsNullOrWhiteSpace(sessionId) || string.IsNullOrWhiteSpace(destinationPath))
            {
                MessageBox.Show("Please enter NAS IP, Session ID, and Destination Path.", "Missing Information", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            txtOverallStatus.Text = "Starting uploads...";
            _cancellationTokenSource = new CancellationTokenSource(); // Crear un nuevo token para esta operación

            // Lista para mantener todas las tareas de subida individuales
            var uploadTasks = new ObservableCollection<Task>();

            // Iterar sobre cada archivo en la colección
            foreach (var fileProgress in FilesToUpload)
            {
                // Reiniciar el estado del archivo para una posible re-subida
                fileProgress.Status = "Queued";
                fileProgress.ProgressPercentage = 0;
                fileProgress.UploadedBytes = 0;

                // Crear una nueva instancia del servicio de subida para cada archivo.
                // Esto es útil si las configuraciones del NAS (IP, SID, path) pudieran variar por archivo,
                // aunque en este caso se asumen constantes para la sesión de subida.
                FileUploadService uploader = new FileUploadService(nasIp, sessionId, destinationPath);

                // Iniciar cada subida en una tarea separada utilizando Task.Run
                // Esto permite que las subidas se realicen en paralelo en hilos de fondo.
                uploadTasks.Add(Task.Run(async () =>
                {
                    await uploader.UploadFileAsync(fileProgress, _cancellationTokenSource.Token, (progress) =>
                    {
                        // Este callback es invocado desde un hilo de fondo.
                        // Todas las actualizaciones de la UI deben hacerse en el hilo principal de la UI.
                        // Usamos Application.Current.Dispatcher.Invoke para asegurar esto.
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Las propiedades de fileProgress ya notifican a la UI (INotifyPropertyChanged),
                            // así que no necesitamos hacer nada más aquí explícitamente para actualizar la fila.
                        });
                    });
                }, _cancellationTokenSource.Token)); // Pasar el CancellationToken
            }

            try
            {
                // Esperar a que todas las tareas de subida se completen
                await Task.WhenAll(uploadTasks);
                txtOverallStatus.Text = "All uploads completed.";
            }
            catch (OperationCanceledException)
            {
                txtOverallStatus.Text = "Some uploads were cancelled.";
            }
            catch (Exception ex)
            {
                txtOverallStatus.Text = $"An error occurred during uploads: {ex.Message}";
            }
            finally
            {
                // Limpiar y liberar los recursos del CancellationTokenSource
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
            }
        }

        // Método opcional para cancelar todas las subidas.
        // Podrías añadir un botón en el XAML para llamar a este método.
        private void CancelUploads_Click(object sender, RoutedEventArgs e)
        {
            _cancellationTokenSource?.Cancel(); // Solicitar la cancelación de todas las tareas
            txtOverallStatus.Text = "Cancelling uploads...";
        }
    }

}
