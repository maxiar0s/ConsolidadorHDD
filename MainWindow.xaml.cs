using Microsoft.Win32;
using Npgsql;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ConsolidadorHDD
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static List<FileData> resultados = new List<FileData>();
        public static List<FileData> NasFileData = new List<FileData>();
        private static int _sharedCounter = 0;
        private static long _sharedFileSize = 0;
        private static int _counter = 0;

        private static Dictionary<string, List<FileData>> HashNasFiles = new Dictionary<string, List<FileData>>();


        public MainWindow()
        {
            InitializeComponent();
            getStorages();
            getFiles();



        }

        private async void getStorages() {
            List<Storage> storages = await DataBase.getAllStorages();

            foreach (var storage in storages)
            {
                CBStorageId.Items.Add(storage.name);
            }

        }

        private async void getFiles()
        {
            btnProcess.IsEnabled = false;
            btnProcess.Content = "Obteniendo de NAS...";
            NasFileData = await DataBase.getHashNas();

            HashNasFiles = (from x in NasFileData
                            group x by x.Hash into g
                            select new { hasdId = g.Key, files = g.ToList() })
                            .ToDictionary(g => g.hasdId, t => t.files);


            btnProcess.Content = "Procesar";
            btnProcess.IsEnabled = true;



        }



        private void BtnPath_Click(object sender, RoutedEventArgs e)
        {
            OpenFolderDialog openFolderDialog = new OpenFolderDialog()
            {
                Title = "Select folder to open ...",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            };

            //openFolderDialog.Multiselect = true;

            StringBuilder sb = new StringBuilder();
            if (openFolderDialog.ShowDialog() == true)
            {
                sb.AppendLine($"List of all subdirectories in the selected folders :");
                sb.AppendLine();

                foreach (var folder in openFolderDialog.FolderNames)
                {
                    sb.AppendLine($"Folder Name : {folder}");
                    foreach (var subfolder in Directory.EnumerateDirectories(folder))
                    {
                        sb.AppendLine($"\t{subfolder}");
                    }
                }
            }
            else
            {
                sb.AppendLine("No folder selected.");
                sb.AppendLine("Either cancel button was clicked or dialog was closed");
            }
            Console.WriteLine(sb.ToString());
            txtPath.Text = string.Join(",", openFolderDialog.FolderNames);
            if (txtPath.Text == "") {
                string messageBoxText = "Tiene que seleccionar primero un path";
                string caption = "No ha seleccionado un path para procesar";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Warning;
                MessageBoxResult result;

                MessageBox.Show(messageBoxText, caption, button, icon);
                return;
            }


            DriveInfo driveInfo = new DriveInfo(txtPath.Text);
            Debug.WriteLine(driveInfo);
            Debug.WriteLine("Drive {0}", driveInfo.Name);
            Debug.WriteLine("  Drive type: {0}", driveInfo.DriveType);
            if (driveInfo.IsReady)
            {
                Debug.WriteLine("  Volume label: {0}", driveInfo.VolumeLabel);
                Debug.WriteLine("  File system: {0}", driveInfo.DriveFormat);
                Debug.WriteLine(
                    "  Available space to current user:{0, 15} bytes",
                    driveInfo.AvailableFreeSpace);

                Debug.WriteLine(
                    "  Total available space:          {0, 15} bytes",
                    driveInfo.TotalFreeSpace);

                Debug.WriteLine(
                    "  Total size of drive:            {0, 15} bytes ",
                    driveInfo.TotalSize);

            }
            var serial = getDiskSerial(driveInfo.Name);

            txtDiskId.Text = $"Nombre:{driveInfo.Name}, Volumen:{driveInfo.VolumeLabel}, Formato:{driveInfo.DriveFormat}, Serial:{serial}:";
            

        }


        private string getDiskSerial(string path) {
            string driveletter = path.Replace("\\", "");

            var index = new ManagementObjectSearcher("SELECT * FROM Win32_LogicalDiskToPartition").Get().Cast<ManagementObject>();
            var disks = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive").Get().Cast<ManagementObject>();
            string serial = "";
            try
            {
                var drive = (from i in index where i["Dependent"].ToString().Contains(driveletter) select i).FirstOrDefault();
                var key = drive["Antecedent"].ToString().Split('#')[1].Split(',')[0];

                var disk = (from d in disks
                            where
                                d["Name"].ToString() == "\\\\.\\PHYSICALDRIVE" + key //&&
                                //d["InterfaceType"].ToString() == "USB"
                            select d).FirstOrDefault();
                if (disk != null) {
                    serial = disk["PNPDeviceID"].ToString().Split('\\').Last();
                }
            }
            catch
            {
                Debug.WriteLine("drive not found!!");
            }
            Debug.WriteLine("Serial: ",serial);
            return serial;
        }


        private void ChangeText(FileData archivo) {
            
            txtProgress.Text = $"Procesando : {_sharedCounter} de {_counter}";
            resultados.Add(archivo);
            //fileList.ItemsSource = resultados;

        }

        private void StartProgress(int max)
        {
            _counter = max;
            txtProgress.Text = $"{0} de {max}";
            ProcessProgress.Maximum = max;
        }


        private void ChangeProgress(int value, bool Repited) {
            ProcessProgress.Value = value;
            if (Repited) {
                txtProgress.Text = $"Buscando repetidos : {value} de {_counter}";
            }
        }


        private async void BtnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Title = "Seleccione el archivo a importar ...",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
            };

            //openFolderDialog.Multiselect = true;

            StringBuilder sb = new StringBuilder();
            if (openFileDialog.ShowDialog() == true)
            {
                sb.AppendLine($"List of all subdirectories in the selected folders :");
                sb.AppendLine();


                
                string file = openFileDialog.FileName;
                Debug.WriteLine(file);

                fileList.Items.Clear();
                List<FileData> datos = new List<FileData>();
                try
                {

                    // Crear un objeto StreamReader para leer el archivo
                    using (StreamReader sr = new StreamReader(file))
                    {

                        string linea;
                        sr.ReadLine();
                        // Leer el archivo línea por línea
                        while ((linea = sr.ReadLine()) != null)
                        {
                            var fileLineData = linea.Split(",");
                            long size = 0;
                            bool success = Int64.TryParse(fileLineData[6], out size);

                            var newFile = new FileData { 
                                isNas= fileLineData[0] == "True" ? true : false,
                                isRepited= fileLineData[1] == "True" ? true : false,
                                Nombre= fileLineData[2],
                                Extension= fileLineData[3],
                                Directorios = fileLineData[4],
                                Hash = fileLineData[5],
                                Tamaño = success ? size : 0,
                                Tamañostr = success ? FileSizeExtension.ToHumanReadableString(size): ""
                            };

                            datos.Add(newFile);
                            fileList.Items.Add(newFile);

                            Debug.WriteLine(linea); // Imprimir cada línea en la consola
                        }
                    } // El bloque using asegura que el archivo se cierre automáticamente
                }
                catch (Exception err)
                {
                    Console.WriteLine("Error al abrir o leer el archivo: " + err.Message);
                }

                if (datos.Count > 0) {
                    btnUpload.IsEnabled = true;
                }

                resultados = datos;

            }
            else
            {
                sb.AppendLine("No folder selected.");
                sb.AppendLine("Either cancel button was clicked or dialog was closed");
            }
            Console.WriteLine(sb.ToString());
            
        }


        private async void BtnProcess_Click(object sender, RoutedEventArgs e) {
            txtDetails.Text = "";
            btnProcess.IsEnabled = false;
            var processPath = txtPath.Text;
            if (processPath == "") {
                string messageBoxText = "Tiene que seleccionar primero un path";
                string caption = "No ha seleccionado un path para procesar";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Warning;
                MessageBoxResult result;

                MessageBox.Show(messageBoxText, caption, button, icon);
                return;
            }
            fileList.Items.Clear();
            await CaptureFilesAsync(txtPath.Text);

            txtDetails.Text += $"Total de archivos: {resultados.Count} tamaño {FileSizeExtension.ToHumanReadableString(_sharedFileSize)}\n";
            dynamic status = await FindRepited();

            int repetidosLocal = status.Local;
            int repetidosNAS = status.NAS;
            long repetidosLocalSize = status.LocalSize;
            long repetidosNASSize = status.NasSize;


            txtDetails.Text += $"  Repetidos en el disco: {repetidosLocal} tamaño {FileSizeExtension.ToHumanReadableString(repetidosLocalSize)}.\n";
            txtDetails.Text += $"  Repetidos NAS        : {repetidosNAS} tamaño {FileSizeExtension.ToHumanReadableString(repetidosNASSize)}.\n";
            btnProcess.IsEnabled = true;
            foreach (FileData file in resultados)
            {
                fileList.Items.Add(file);
            }
            btnUpload.IsEnabled = true;
            btnExportar.IsEnabled = true;
        }

        public async Task<object> FindRepited( ) {
            int repetidosLocal = 0;
            int repetidosNAS = 0;
            return await Task.Run(() =>
            {
                Dispatcher.Invoke(StartProgress, DispatcherPriority.Normal, resultados.Count);
                int count = 0;
                long NasSize = 0;
                long LocalSize = 0;

                ConcurrentBag<FileData> CBfileListResult = new ConcurrentBag<FileData>();

                //var HashNasFiles = new HashSet<FileData>();


                Dictionary<string, List<FileData>> HashLocalFiles = (from x in resultados
                                                                     group x by x.Hash into g
                                                                   select new { hasdId = g.Key, files = g.ToList() })
                                            .ToDictionary(g => g.hasdId, t => t.files);

                Parallel.ForEach(resultados,
                    new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 },
                    result =>
                {

                    FileData actualFile = result;
                    var Hash = result.Hash == null ? "XX" : result.Hash;


                    if (HashLocalFiles.ContainsKey(Hash) &&
                        HashLocalFiles[Hash].Count() > 1)
                    {
                        actualFile.isRepited = true;
                        Interlocked.Increment(ref repetidosLocal);
                        Interlocked.Add(ref LocalSize, (long)actualFile.Tamaño);
                    }


                    
                    if (HashNasFiles.ContainsKey(Hash) &&
                        HashNasFiles[Hash].Count() > 1) 
                    {
                        actualFile.isNas = true;
                        Interlocked.Increment(ref repetidosNAS);
                        Interlocked.Add(ref NasSize, (long)actualFile.Tamaño);
                    }

                    Interlocked.Increment(ref count);
                    Dispatcher.Invoke(ChangeProgress, DispatcherPriority.Normal, [count, true]);
                    CBfileListResult.Add(actualFile);

                });

                resultados = CBfileListResult.ToList();
                return new { Local = repetidosLocal, NAS = repetidosNAS, LocalSize = LocalSize, NasSize = NasSize };
            });
        }


        public async Task CaptureFilesAsync(string processPath)
        {
            // Create a task to run asynchronously
            resultados = new List<FileData>();
            await Task.Run(() =>
            {
                string rootPath = processPath;
                try
                {
                    // Recorre el directorio y procesa archivos
                    //var archivos = ObtenerInformacionArchivos(rootPath);
                    if (!Directory.Exists(rootPath))
                    {
                        throw new DirectoryNotFoundException("El directorio especificado no existe.");
                    }

                    //var resultados = new List<FileData>();

                    // Recorre todo el directorio y subdirectorios
                    var allFiles = Directory.GetFiles(rootPath, "*.*", SearchOption.AllDirectories);
                    var filesCount = allFiles.Length;
                    _sharedCounter = 1;
                    _sharedFileSize = 0;
                    Dispatcher.Invoke(StartProgress, DispatcherPriority.Normal, filesCount);
                    Parallel.ForEach(allFiles,
                        new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
                        archivo =>
                    {
                        var fileInfo = new FileInfo(archivo);
                        var hash = CalcularHashMD5(archivo);

                        var file = new FileData
                        {
                            Nombre = fileInfo.Name,
                            Directorios = fileInfo.Directory?.FullName,
                            Tamaño = fileInfo.Length,
                            Tamañostr = FileSizeExtension.ToHumanReadableString(fileInfo.Length),
                            Extension = fileInfo.Extension,
                            Hash = hash
                        };
                        //resultados.Add(file);
                        Dispatcher.Invoke(ChangeText, DispatcherPriority.Normal, file);
                        Interlocked.Increment(ref _sharedCounter);
                        Interlocked.Add(ref _sharedFileSize, (long)file.Tamaño);
                        Dispatcher.Invoke(ChangeProgress, DispatcherPriority.Normal, [_sharedCounter, false]);
                    });

                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error: {ex.Message}");
                }
            });
        }

        /*
        static string CalcularHashMD5(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = md5.ComputeHash(stream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }
        */

        static string CalcularHashMD5 (string filePath) //CalculateFileMd5
        {
            try
            {
                // Usar using para asegurar la liberación de recursos
                using (var md5 = MD5.Create())
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, false)) // false para useAsync porque MD5.ComputeHash es síncrono
                {
                    byte[] hashBytes = md5.ComputeHash(stream);
                    // Convertir el array de bytes a una cadena hexadecimal
                    var sb = new StringBuilder();
                    for (int i = 0; i < hashBytes.Length; i++)
                    {
                        sb.Append(hashBytes[i].ToString("x2"));
                    }
                    return sb.ToString();
                }
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"Error de I/O al procesar {filePath}: {ex.Message}");
                return ""; // Retorna null para el hash en caso de error
            }
            catch (UnauthorizedAccessException ex)
            {
                Console.Error.WriteLine($"Error de acceso al procesar {filePath}: {ex.Message}");
                return "";
            }
            catch (Exception ex) // Captura cualquier otra excepción inesperada para este archivo
            {
                Console.Error.WriteLine($"Error inesperado al procesar {filePath}: {ex.Message}");
                return "";
            }
        }






        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            //txtProgress.Text = Environment.ProcessorCount.ToString();
            SaveFileDialog saveFileDialog = new SaveFileDialog()
            {
                Title = "Nombre de archivo ...",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                CheckFileExists = false,
                FileName = "export.csv",
                DefaultExt = "csv"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                Debug.WriteLine($"{saveFileDialog.FileName}");
                TextWriter tw = new StreamWriter(saveFileDialog.FileName, false);
                tw.WriteLine("NAS,LOCAL,Name,Type,Path,Hash,Size");
                foreach (FileData item in resultados)
                {
                    tw.WriteLine($"{item.isNas},{item.isRepited},{item.Nombre},{item.Extension},{item.Directorios},{item.Hash},{item.Tamaño}");
                }
                tw.Close();

            }


                


        }

        private void BtnNewStorage_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Selecciono");
            Debug.WriteLine(CBStorageId.SelectedValue);
            Win_upload nuevaVentana = new Win_upload();
            nuevaVentana.Show();
        }

        private async void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = (sender as GridViewColumnHeader);
            string sortBy = column.Tag.ToString();
            Debug.WriteLine(sortBy);
            try
            {
                List<FileData> SortedList = new List<FileData>();
                if (sortBy == "isRepited")
                {
                    SortedList =
                        (from FileData file in resultados
                                         orderby file.isRepited descending
                                         select file).ToList<FileData>();
                    if (SortedList.Any())
                    {
                        fileList.Items.Clear();
                        foreach (FileData file in SortedList)
                        {
                            fileList.Items.Add(file);
                        }
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("error!!!", ex.Message);
            }

        }

        private void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            Debug.WriteLine("Selecciono");
            Debug.WriteLine(CBStorageId.SelectedValue);
            Win_upload nuevaVentana = new Win_upload(resultados);
            nuevaVentana.Show();
        }
    }



}