using System.ComponentModel;

namespace ConsolidadorHDD.model
{
    public enum Estados
    {
        [Description("subida exitosa")]
        SUCCESS,
        [Description("Esperando a subir")]
        WAITING,
        [Description("Subiendo")]
        UPLOADING,
        [Description("Inicializando subida")]
        STARTED,
        [Description("Error")]
        ERROR,
        [Description("Error de red")]
        NETERROR,
        [Description("Error de Nombre")]
        NAMEERROR,
        [Description("Error de Tamaño")]
        SIZEERROR,
        [Description("Subida cancelada")]
        CANCEL,
        [Description("En cola")]
        QUEUED,

    }
}
