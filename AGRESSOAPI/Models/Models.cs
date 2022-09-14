using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace APIAGRESSO.Models
{
    class Models
    {

    }

    public class fournisseur
    {
        public string name { get; set; }
        public string pay_method { get; set; }
        public string currency { get; set; }
        public string description { get; set; }
        public DateTime dt_up { get; set; }
        public string status { get; set; }
        public string toJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    class loginacess
    {
        public string login { get; set; }
        public string password { get; set; }

        public string toJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class Token
    {
        public string token { get; set; }
        public DateTime dtexp { get; set; }
    }

    public class PrdOuverte
    {
        public int period { get; set; }
        public string client { get; set; }

        public string toJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class TauxTVA
    {
        public string account { get; set; }
        public DateTime dt { get; set; }
        public string client { get; set; }

        public string toJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

    public class clientFOU
    {
        public string client { get; set; }
        public string cnuf { get; set; }

        public string toJson()
        {
            return JsonConvert.SerializeObject(this);
        }
    }

   
    
}
