using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AGRESSOAPI.Models
{
    public class Lettrage
    {
        public string client { get; set; }
        public string journal { get; set; }
        public string account { get; set; }
        public string nfact { get; set; }
        public int numpc { get; set; }
        public int sequence_no { get; set; }
        public int numpc_ref { get; set; }
        public int sequence_ref { get; set; }
        public int period { get; set; }

        public string toJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
