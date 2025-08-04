using System.ComponentModel; 
using System.IO; 

namespace ConsolidadorHDD.model
{ 

    public class FileUploadProgress : INotifyPropertyChanged
    {
        private string _fileName;
        private string _filePath;
        private Estados _status;
        private string _statusMSG;

        private double _progressPercentage;
        private long _uploadedBytes;
        private long _totalBytes;

        private long?   _tamaño;
        //private string? _tamañostr;
        private string? _extension;
        private string? _hash;
        private bool?   _isNas;
        private bool?   _isRepited;




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
        public Estados Status
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

        public string StatusMSG
        {
            get => _statusMSG;
            set
            {
                if (_statusMSG != value)
                {
                    _statusMSG = value;
                    OnPropertyChanged(nameof(StatusMSG));
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

        public long? Tamaño
        {
            get => _tamaño;
            set
            {
                if (_tamaño != value)
                {
                    _tamaño = value;
                    OnPropertyChanged(nameof(Tamaño));
                }
            }
        }

        public string? Hash
        {
            get => _hash;
            set
            {
                if (_hash != value)
                {
                    _hash = value;
                    OnPropertyChanged(nameof(Hash));
                }
            }
        }

        public bool? IsNAS
        {
            get => _isNas;
            set
            {
                if (_isNas != value)
                {
                    _isNas = value;
                    OnPropertyChanged(nameof(IsNAS));
                }
            }
        }

        public bool? IsRepited
        {
            get => _isRepited;
            set
            {
                if (_isRepited != value)
                {
                    _isRepited = value;
                    OnPropertyChanged(nameof(IsRepited));
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
