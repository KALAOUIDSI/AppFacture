using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AGRESSOAPI.Models
{
   

    public class AgrInjectIN
    {
       public int period { get; set; }
        public int numpc { get; set; }
        public int sequence_no { get; set; }
        public string journal { get; set; }
        public string client { get; set; }
        public string account { get; set; }
        public string ana_1 { get; set; } // <-- DIM 1
        public string ana_2 { get; set; } // <-- DIM 2
        public string ana_3 { get; set; }
        public string ana_4 { get; set; }
        public string ana_5 { get; set; }
        public string ana_6 { get; set; }
        public string ana_7 { get; set; }
        public int mnt { get; set; }
        public string description { get; set; }
        public DateTime dtpc { get; set; } // <-- Vochar _ date
        public DateTime dtech { get; set; }
        public string nfact { get; set; }
        public string cnuf { get; set; } // <-- Apart_id
        public string lblfrn { get; set; } // <-- apart name 
        public string toJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class AgrInjectOUT
    {
        public string client { get; set; }
        public int numpc { get; set; }
        public string journal { get; set; }
        public int sequence_no { get; set; }
        public int period { get; set; }
        public DateTime dtpc { get; set; }
        public DateTime dtech { get; set; }
        public string nfact { get; set; }
        public string account { get; set; }
        public int mnt { get; set; }
        public string cnuf { get; set; }
        public string lblfrn { get; set; }
        public string description { get; set; }
        public string ana_1 { get; set; }
        public string ana_2 { get; set; }
        public string ana_3 { get; set; }
        public string ana_4 { get; set; }
        public string ana_5 { get; set; }
        public string ana_6 { get; set; }
        public string ana_7 { get; set; }

        public string toJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }


}
