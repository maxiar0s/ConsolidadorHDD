using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsolidadorHDD.model
{
    public class ResponseUpload
    {
       public int status { get; set; }
       public string size { get; set; }
       public string name { get; set; }
    }
}
