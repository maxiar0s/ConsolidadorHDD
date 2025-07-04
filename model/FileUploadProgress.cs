using System.ComponentModel; 
using System.IO; 

namespace ConsolidadorHDD.model
{
    public class FileUploadProgress : INotifyPropertyChanged
    {
        private string _fileName;
        private string _filePath;
        private string _status;
        private double _progressPercentage;
        private long _uploadedBytes;
        private long _totalBytes;

        // Nombre del archivo (solo el nombre, sin ruta)
        public string FileName
        {
            get => _fileName;
            set
            {
                if (_fileName != value)
                {
                    _fileName = value;
                    OnPropertyChanged(nameof(FileName));
                }
            }
        }

        // Ruta completa del archivo en el sistema local
        public string FilePath
        {
            get => _filePath;
            set
            {
                if (_filePath != value)
                {
                    _filePath = value;
                    OnPropertyChanged(nameof(FilePath));
                }
            }
        }

        // Estado actual de la subida (ej. "En cola", "Subiendo...", "Completado", "Error")
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged(nameof(Status));
                }
            }
        }

        // Porcentaje de progreso de la subida (0-100)
        public double ProgressPercentage
        {
            get => _progressPercentage;
            set
            {
                if (_progressPercentage != value)
                {
                    _progressPercentage = value;
                    OnPropertyChanged(nameof(ProgressPercentage));
                }
            }
        }

        // Número de bytes subidos hasta el momento
        public long UploadedBytes
        {
            get => _uploadedBytes;
            set
            {
                if (_uploadedBytes != value)
                {
                    _uploadedBytes = value;
                    OnPropertyChanged(nameof(UploadedBytes));
                    OnPropertyChanged(nameof(UploadedMB)); // También notificar cambio en MB
                }
            }
        }

        // Tamaño total del archivo en bytes
        public long TotalBytes
        {
            get => _totalBytes;
            set
            {
                if (_totalBytes != value)
                {
                    _totalBytes = value;
                    OnPropertyChanged(nameof(TotalBytes));
                    OnPropertyChanged(nameof(TotalMB)); // También notificar cambio en MB
                }
            }
        }

        // Propiedad calculada para mostrar bytes subidos en MegaBytes
        public double UploadedMB => UploadedBytes / (1024.0 * 1024.0);
        // Propiedad calculada para mostrar el tamaño total en MegaBytes
        public double TotalMB => TotalBytes / (1024.0 * 1024.0);

        // Evento requerido por INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        // Método para invocar el evento PropertyChanged
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

}
