using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsolidadorHDD
{
    public static class FileSizeExtension
    {
        public static string ToHumanReadableString(this long bytes)
        {
            if (bytes < 1024) return $"{bytes} bytes";

            double kb = bytes / 1024.0;
            if (kb < 1024) return $"{kb:0.##} KB";

            double mb = kb / 1024.0;
            if (mb < 1024) return $"{mb:0.##} MB";

            double gb = mb / 1024.0;
            return $"{gb:0.##} GB";
        }
    }
}
