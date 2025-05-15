using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsolidadorHDD
{
    public class StorageDrop : ObservableCollection<Storage>
    {
        public string? ID;
        public TimeSpan last_update;
        public string? name;
        public string? external_id;

    }

    public class Storage
    {
        public string? ID;
        public TimeSpan last_update;
        public string? name;
        public string? external_id;

    }
}
