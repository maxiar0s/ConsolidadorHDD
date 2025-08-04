using System.ComponentModel;

namespace ConsolidadorHDD.model
{

    public class SelectedExtencion : INotifyPropertyChanged
    {
        private bool _selected;
        private string _extencion;
        
        // Nombre del archivo (solo el nombre, sin ruta)
        public bool Selected
        {
            get => _selected;
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    OnPropertyChanged(nameof(Selected));
                }
            }
        }

        // Ruta completa del archivo en el sistema local
        public string Extencion
        {
            get => _extencion;
            set
            {
                if (_extencion != value)
                {
                    _extencion = value;
                    OnPropertyChanged(nameof(Extencion));
                }
            }
        }

        // Evento requerido por INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        // Método para invocar el evento PropertyChanged
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
