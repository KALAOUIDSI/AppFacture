using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.appFacture
{
    public class FACTUREPaginationRes
    {
        public int count { get; set; }
        public ICollection<FACTFACTURE> listFACTURE { get; set; }
    }
}
