using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.appFacture
{
    public class FACTUREAdvFiltre
    {
        public string reference { get; set; }
        public string designation { get; set; }
        public string chapitre { get; set; }
        public string typefacture { get; set; }
        public string status { get; set; }
        public string mntht { get; set; }
        public string mntttc { get; set; }
        public string client { get; set; }
        public int site { get; set; }
        public int user { get; set; }
   }
}
