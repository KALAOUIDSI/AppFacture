using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace AGRESSOAPI.Models
{
    public class Prorata
    {
        public string account { get; set; }
        public DateTime dt { get; set; }
        public string client { get; set; }

        public string toJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }
}
