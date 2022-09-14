using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using APIAGRESSO.HTTP;
using APIAGRESSO.Models;
using AGRESSOAPI.Models;
using Newtonsoft.Json;
using System.Configuration;

namespace APIAGRESSO.AgressoControllers
{
    public class AgressoControllers
    {

        private Token token;
        private API_CNX AGRAPI = new API_CNX();
        
     
       
        /*
        Ce WS permet d’injecter des données sur la nouvelle version Agresso à déployer sur les
        prochains mois
        /api/InsAgr
        --------------
        0 : intégration succès.
        Différent de zéro, erreur lors de
        l’intégration :
         -1 : Erreur données pièce Non équilibrée.
         -2 : Erreur compte comptable.
         -3 : Erreur période clôturée.
         -4 : Erreur unicité des données.
         -99 : Erreur interne API.
        -----------------
        */
        

        /*public List<AgrInjectOUT> InsAgr(AgrInjectIN InsAgr)
        {
            string JSON = AGRAPI.HTTPAGRESSO("POST", InsAgr.toJson(), "api/InsAcr");
            return JsonConvert.DeserializeObject<List<AgrInjectOUT>>(JSON);
        }*/

        /*
        Ce WS permet d’injecter des données sur la version actuelle de Agresso:
        /api/InsAcr
        ------------------
        0 : intégration succès.
        Différent de zéro, erreur lors de l’intégration :
         -1 : Erreur données pièce Non équilibrée.
         -2 : Erreur compte comptable.
         -3 : Erreur période clôturée.
         -4 : Erreur unicité des données
         -99 : Erreur interne API.
         */
        public List<AgrInjectOUT> InsAcr(AgrInjectIN AgrIn)
        {
            string JSON = AGRAPI.HTTPAGRESSO("POST", AgrIn.toJson(), "api/InsAcr");
            return JsonConvert.DeserializeObject<List<AgrInjectOUT>>(JSON);
        }
        /*
        Ce WS permet de récupérer le prorata d’un compte TVA pour une enseigne donnée :
        /api/GetProrata
       ------------------
       Différent de -1 : Valeur de prorata de compte.
       -1 : prorata n’est pas encore paramétré sur
       Agresso
       ------------------
       */
        public string GetProrata()
        {
            Prorata prorata = new Prorata();
            prorata.client = "CF";
            prorata.account = "34551000";
            prorata.dt = DateTime.Parse("2022-02-16T09:29:09.161Z");
            string JSON = AGRAPI.HTTPAGRESSO("POST", prorata.toJson(), "/api/GetProrata");
            //Code
            return JSON;
        }

        /*
        Ce WS permet de récupérer l’état de la période sur Agresso pour une enseigne :
        /api/IsPrdOuverte
        -------------------
        True : Ouverte
        False : Clôturée
        -------------------
        */
        public bool IsPrdOuverte(string client, int period)
        {
            PrdOuverte pr = new PrdOuverte();
            pr.client = client;
            pr.period = period;
            return bool.Parse(AGRAPI.HTTPAGRESSO("POST", pr.toJson(), "/api/IsPrdOuverte"));
        }

        /*
        Il permet de récupérer le taux TVA d’un compte TVA :
        /api/GetTauxTVA
        */

        public int GetTauxTVA(string account, string client, DateTime dt)
        {
            TauxTVA TTVA = new TauxTVA();
            TTVA.account = account;
            TTVA.client = client;
            TTVA.dt = dt;
            return int.Parse(AGRAPI.HTTPAGRESSO("POST", TTVA.toJson(), "/api/GetTauxTVA"));
        }


        /*
        Il permet de récupérer le coefficient d’un compte TVA pour une enseigne : 
        /api/GetCoefProrata
        --------------------
        Différent de -1 : Valeur de coefficient prorata de
        compte.
        -1 : prorata n’est pas encore paramétré sur
        Agresso
        */
        public int GetCoefProrata(string account, string client, DateTime dt)
        {
            Prorata pr = new Prorata();
            pr.account = account;
            pr.client = client;
            pr.dt = dt;
            return int.Parse(AGRAPI.HTTPAGRESSO("POST", pr.toJson(), "/api/GetCoefProrata") );
        }



        /*
        Il permet de récupérer les informations fournisseur : 
        /api/GetFrn
        */
        public fournisseur GetFrn(string client , string cnuf)
        {
            clientFOU c = new clientFOU();
            c.client = client;
            c.cnuf = cnuf;
            string JSON = AGRAPI.HTTPAGRESSO("POST", c.toJson(), "/api/GetFrn");
            return JsonConvert.DeserializeObject<fournisseur>(JSON);
        }



        /*
        Il permet de récupérer les informations client : 
        /api/GetClient
        */
        public fournisseur GetClient(string client, string cnuf)
        {
            clientFOU c = new clientFOU();
            c.client = client;
            c.cnuf = cnuf;
            string JSON = AGRAPI.HTTPAGRESSO("POST", c.toJson(), "/api/GetClient");
            return JsonConvert.DeserializeObject<fournisseur>(JSON);
        }


        /*
        Ce WS permet de lettre des données sur Agresso:
         /api/lettrage
        --------------------
        0 : Intégration succès.
        -99 : Erreur interne API.
        ---------------------
        */
        public List<Lettrage> lettrage(Lettrage letr)
        {
            string JSON = AGRAPI.HTTPAGRESSO("POST", letr.toJson(), "/api/lettrage");
            List<Lettrage> listLettrage = new List<Lettrage>();
            return JsonConvert.DeserializeObject<List<Lettrage>>(JSON);
        }


    }
}
