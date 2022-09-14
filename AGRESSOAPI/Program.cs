using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APIAGRESSO.AgressoControllers;
using APIAGRESSO.Models;

namespace AGRESSOAPI
{
    class Program
    {
        static void Main(string[] args)
        {
            AgressoControllers AgrAPI = new AgressoControllers();
            Console.WriteLine("TESTING CONNEXION AGRESSO API !");

            

            fournisseur r = AgrAPI.GetClient("CF", "8035");
            Console.WriteLine(r.toJson());

            Console.WriteLine("END ==>");
            //c.GetProrata();
            Console.ReadLine();
        }
    }
}
