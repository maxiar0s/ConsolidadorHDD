
namespace ConsolidadorHDD
{
    public class FileData
    {
        public int? id { get; set; }
        public string? Nombre { get; set; }
        public string? Directorios { get; set; }
        public long? Tamaño { get; set; }
        public string? Tamañostr { get; set; }

        public string? Extension { get; set; }
        public string? Hash { get; set; }
        public bool? isNas { get; set; }
        public bool? isRepited { get;set; }
    }
}
