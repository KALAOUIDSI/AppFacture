using appFacture.Models;
using CrystalDecisions.CrystalReports.Engine;
using Domain.appFacture;
using log4net;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net.Mail;
using System.Security.Claims;
using System.Text;
using System.Threading;
using System.Web;
using System.Web.Mvc;
using System.Web.Script.Serialization;
using APIAGRESSO.AgressoControllers;
using APIAGRESSO.Models;

namespace appFacture.Controllers
{
    [Authorize(Roles = "DEMANDE")]
    public class DemandeController : Controller
    {
        public int pageSize = 10;
        public static ILog logger = log4net.LogManager.GetLogger("KassagrWEBLogger");
        public static string agrDbLink = ConfigurationManager.AppSettings["AGRDBLINK"];
        public static string OracleConnectionString = ConfigurationManager.ConnectionStrings["Factdb"].ConnectionString;
        //public static string OracleConnectionAgresso = ConfigurationManager.ConnectionStrings["Agrprod"].ConnectionString;
        public static string OracleConnectionAgresso = ConfigurationManager.ConnectionStrings["Agrtest"].ConnectionString;

        public static string clientStation = ConfigurationManager.AppSettings["CLIENTSTATION"];

        public ActionResult Index(Boolean keepFilters = false, int page = 1, string search = "", int sortby = -1, Boolean isasc = true, string advSearch = "")
        {

            if (keepFilters)
            {
                //vérifier si on a une donnée TempData
                if (TempData["page"] != null)
                {
                    int pageTemp;
                    int.TryParse(TempData["page"].ToString(), out pageTemp);
                    page = pageTemp;
                }
                if (TempData["search"] != null)
                {
                    search = TempData["search"].ToString();
                }
                if (TempData["sortby"] != null)
                {
                    int sortbyTemp;
                    int.TryParse(TempData["sortby"].ToString(), out sortbyTemp);
                    sortby = sortbyTemp;
                }
                if (TempData["isasc"] != null)
                {
                    Boolean isascTemp;
                    Boolean.TryParse(TempData["isasc"].ToString(), out isascTemp);
                    isasc = isascTemp;
                }
                if (TempData["advSearch"] != null)
                {
                    advSearch = TempData["advSearch"].ToString();
                }
            }

            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            int IdSiteConnecte = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.SerialNumber).Value);
            
            DEMANDEAdvFiltre advFiltre = new JavaScriptSerializer().Deserialize<DEMANDEAdvFiltre>(advSearch);
            if (advFiltre == null)
            {
                advFiltre = new DEMANDEAdvFiltre();
            }
           // KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            //appel de méthode de lecture des données
            advFiltre.site = IdSiteConnecte;
            if (User.IsInRole("SIEGE"))
            {
                advFiltre.site = -1;
            }

            advFiltre.user = UTILISATEURCONNECTE;

            FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();
            DEMANDEPaginationRes result = FACTDEMANDE.getAll(page, pageSize, search, sortby, isasc, advFiltre);
            //sauvegarde des paramètres
            TempData["page"] = page;
            TempData["search"] = search;
            TempData["sortby"] = sortby;
            TempData["isasc"] = isasc;
            TempData["advSearch"] = advSearch;
            //informations pour la pagination
            var pagingInfo = new InfosPagination()
            {
                CurrentPage = page,
                ItemsPerPage = pageSize,
                TotalItems = result.count
            };
            //ViewBag.listSites = getSites();
            List<FACTTYPE> listTypes = getTypefacture(UTILISATEURCONNECTE);
            bool isexport = false;
            bool queexport = false;
            foreach (FACTTYPE typef in listTypes) {
                if (typef.IDCATEGORIE == 6)
                    isexport = true;
            }
            if (isexport && listTypes.Count == 1)
                queexport = true;
            
            listTypes.Insert(0, new FACTTYPE(-1, "Sélectionner un type facture"));
            ViewBag.listTypes = listTypes;


            List<FACTDEMANDESTATUS> listStatus = getstatus();
            listStatus.Insert(0, new FACTDEMANDESTATUS(-1, "Sélectionner un status"));
            ViewBag.listStatus = listStatus;

            return View(new DemandeListView() { PagingInfo = pagingInfo, Demandes = result.listDEMANDE, Search = search, SortBy = sortby, IsAsc = isasc, AdvSearch = advSearch, AdvSearchFilters = advFiltre, isExport = isexport, queExport = queexport });
        }

        
        //Liste déroulante des magasins
        //public List<SITE> getSites()
        //{
        //    SITE ens = new SITE();
        //    List<SITE> listSites = ens.getAll();
        //    listSites.Insert(0, new SITE(-1, "Sélectionner un site"));
        //    return listSites;
        //}
        //Liste déroulante des type de facture

        public List<FACTTYPE> getTypefactureExport(int idUser)
        {
            FACTTYPE FACTTYPE = new FACTTYPE();
            return FACTTYPE.getTypeExport(idUser);//getAll();
        }

        public List<FACTTYPE> getTypefacture(int idUser)
        {
            FACTTYPE FACTTYPE = new FACTTYPE();
            return FACTTYPE.getAllOfUser(idUser);//getAll();
        }
        public List<FACTTYPECLIENT> getTypeclient()
        {
            FACTTYPECLIENT FACTTYPECLIENT = new FACTTYPECLIENT();
            return FACTTYPECLIENT.getAll();
        }
        //Liste déroulante des status demandes
        public List<FACTDEMANDESTATUS> getstatus()
        {
            FACTDEMANDESTATUS FACTDEMANDESTATUS = new FACTDEMANDESTATUS();
            return FACTDEMANDESTATUS.getAllStatus();
        }
        public List<FACTCLIENT> getlistclients()
        {
            FACTCLIENT FACTCLIENT = new FACTCLIENT();
            return FACTCLIENT.getAllNonRepeat();//getAll();
        }

        public List<FACTSITE> getlistclientsinterne()
        {
            FACTSITE FACTSITE = new FACTSITE();
            return FACTSITE.getAll();
        }

        public List<TAUXTVA> getlisttauxtva()
        {
            List<TAUXTVA> newlist = new List<TAUXTVA>();
            newlist.Add(new TAUXTVA(20, "20"));
            newlist.Add(new TAUXTVA(14, "14"));
            newlist.Add(new TAUXTVA(10, "10"));
            newlist.Add(new TAUXTVA(7, "7"));
            newlist.Add(new TAUXTVA(0, "0"));
            return newlist;
        }

        public List<TAUXTVA> getlisttauxtvaipprf()
        {
            List<TAUXTVA> newlist = new List<TAUXTVA>();
            newlist.Add(new TAUXTVA(20, "20"));
            newlist.Add(new TAUXTVA(14, "14"));
            newlist.Add(new TAUXTVA(10, "10"));
            newlist.Add(new TAUXTVA(7, "7"));
            newlist.Add(new TAUXTVA(0, "0"));
            return newlist;
        }

        public List<FACTSITE> getlistsites()
        {
            FACTSITE FACTSITE = new FACTSITE();
            List<FACTSITE> newlist=new List<FACTSITE>();
            foreach (FACTSITE site in FACTSITE.getAllOrdonnes())
            {
                site.ADRLINE1 = site.IDSITE + "-" + site.LIBELLESITE;
                newlist.Add(site);
            }
            return newlist;
        }

        //public List<FACTENSEIGNE> getlistenseigne()
        //{
        //    FACTENSEIGNE FACTENSEIGNE = new FACTENSEIGNE();
        //    return FACTENSEIGNE.getAll();
        //}

        //Liste déroulante des magasins
        //public List<SITE> getRestrictedSites(List<long> ids)
        //{
        //    SITE ens = new SITE();
        //    List<SITE> listSites = ens.getRestricted(ids);
        //    listSites.Insert(0, new SITE(-1, "Sélectionner un site"));
        //    return listSites;
        //}
        [NoCache]
        public ViewResult Create()
        {
            keepFilters();
            FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();

            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

            List<FACTTYPE> listTypesfacture = getTypefacture(UTILISATEURCONNECTE);
            if (listTypesfacture.Count != 1)
                listTypesfacture.Insert(0, new FACTTYPE(-1, "Sélectionner un type facture"));
            ViewBag.listTypes = listTypesfacture;

            List<FACTTYPECLIENT> listTypesclient = getTypeclient();
            ViewBag.listTypesclient = listTypesclient;

            List<FACTCLIENT> lsclients = getlistclients();
            lsclients.Insert(0, new FACTCLIENT(-1, "Sélectionner un client"));
            ViewBag.listclients = lsclients;

            List<FACTSITE> lsclientsinterne = getlistclientsinterne();
            lsclientsinterne.Insert(0, new FACTSITE(-1, "Sélectionner un client"));
            ViewBag.listclientsinterne = lsclientsinterne;

            List<FACTSITE> listsite = getlistsites();
            ViewBag.listsite = listsite;

            ViewBag.listtva = getlisttauxtva();
            ViewBag.listtvaipprf = getlisttauxtvaipprf();

            return View("Edit", FACTDEMANDE);
        }

        [NoCache]
        public ViewResult Create2()
        {
            keepFilters();
            FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();

            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

            List<FACTTYPE> listTypesfacture = getTypefactureExport(UTILISATEURCONNECTE);
            if (listTypesfacture.Count != 1)
                listTypesfacture.Insert(0, new FACTTYPE(-1, "Sélectionner un type facture"));
            ViewBag.listTypes = listTypesfacture;

            List<FACTTYPECLIENT> listTypesclient = getTypeclient();
            ViewBag.listTypesclient = listTypesclient;

            List<FACTCLIENT> lsclients = getlistclients();
            lsclients.Insert(0, new FACTCLIENT(-1, "Sélectionner un client"));
            ViewBag.listclients = lsclients;

            List<FACTSITE> lsclientsinterne = getlistclientsinterne();
            lsclientsinterne.Insert(0, new FACTSITE(-1, "Sélectionner un client"));
            ViewBag.listclientsinterne = lsclientsinterne;

            List<FACTSITE> listsite = getlistsites();
            ViewBag.listsite = listsite;

            ViewBag.listtva = getlisttauxtva();
            ViewBag.listtvaipprf = getlisttauxtvaipprf();

            return View("Export", FACTDEMANDE);
        }

        [NoCache]
        [HttpPost]
        public ActionResult Export(int id)
        {
            keepFilters();
            FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();
            FACTDEMANDE = FACTDEMANDE.getOne(id);


            ViewBag.isAdmin = true;

            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

            List<FACTTYPE> listTypesfacture = getTypefactureExport(UTILISATEURCONNECTE);
            if (listTypesfacture.Count != 1)
                listTypesfacture.Insert(0, new FACTTYPE(-1, "Sélectionner un type facture"));
            ViewBag.listTypes = listTypesfacture;

            List<FACTTYPECLIENT> listTypesclient = getTypeclient();
            ViewBag.listTypesclient = listTypesclient;

            List<FACTCLIENT> lsclients = getlistclients();
            lsclients.Insert(0, new FACTCLIENT(-1, "Sélectionner un client"));
            ViewBag.listclients = lsclients;

            List<FACTSITE> lsclientsinterne = getlistclientsinterne();
            lsclientsinterne.Insert(0, new FACTSITE(-1, "Sélectionner un client"));
            ViewBag.listclientsinterne = lsclientsinterne;

            List<FACTSITE> listsite = getlistsites();
            ViewBag.listsite = listsite;

            ViewBag.listtva = getlisttauxtva();
            ViewBag.listtvaipprf = getlisttauxtvaipprf();

            return View(FACTDEMANDE);
        }


        [HttpPost]
        public bool isCnufClientExist(string cnuf, string chapitre)
        {
            //Ici on passe le categorie au lieu de chapitre 2==>client-station
            if (chapitre == "2")
                return true;
            else
                return verifCnufFrs(cnuf);
        }

        [HttpPost]
        public bool isCnufFrsExist(string cnuf,string chapitre)
        {
            //Ici on passe le categorie au lieu de chapitre 2==>client-station
            if (chapitre == "2")
                return true;
            else
                return verifCnufClient(cnuf);
        }

        public bool verifCnufFrs(String cnuf)
        {
            int nbrCnuf = 0;
            //using (EramEntities DB = new EramEntities())
            //{
                StringBuilder requette = new StringBuilder();
                requette.Append("select count(*) from asuheader" /*+ agrDbLink*/);
                requette.Append(" where apar_id='" + cnuf + "'");

                try
                {
                    //using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                    using (OracleConnection conn = new OracleConnection(OracleConnectionAgresso))
                    {
                        conn.Open();


                        using (OracleCommand cmd = new OracleCommand(requette.ToString(), conn))
                        {
                            using (OracleDataReader rd = cmd.ExecuteReader())
                            {
                                if (rd.Read())
                                    nbrCnuf = Int32.Parse(rd.GetValue(0).ToString());
                                else
                                {
                                    conn.Close();
                                    conn.Dispose();
                                }
                            }
                        }
                        conn.Close();
                        conn.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    logger.Error("Probleme verification CNUF fournisseur; requette=" + requette.ToString());
                    logger.Error("erreur=" + ex.Message);
                }
            //}
            return (nbrCnuf !=0);
        }

        public bool verifCnufClient(String cnuf)
        {
            int nbrCnuf = 0;
            clientFOU client = new clientFOU();
            client.cnuf = "0";
            AgressoControllers AgrAPI = new AgressoControllers();

            AgrAPI.GetClient("CF", cnuf);

            //------ ZJADDY : deprecatedCNX DATABASE
            /*
            using (EramEntities DB = new EramEntities())
            {
                StringBuilder requette = new StringBuilder();
                requette.Append("select to_char(count(*)) from acuheader" );
                requette.Append(" where apar_id='" + cnuf + "'");
                try
                {

                    

                   
                    //using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                   using (OracleConnection conn = new OracleConnection(OracleConnectionAgresso))
                    {
                        conn.Open();


                        using (OracleCommand cmd = new OracleCommand(requette.ToString(), conn))
                        {
                            using (OracleDataReader rd = cmd.ExecuteReader())
                            {
                                if (rd.Read())
                                    nbrCnuf = Int32.Parse(rd.GetValue(0).ToString());
                                else
                                {
                                    conn.Close();
                                    conn.Dispose();
                                }
                            }
                        }
                        conn.Close();
                        conn.Dispose();
                    }
                }
                catch (Exception ex)
                {

                    logger.Error("Probleme verification CNUF client; requette=" + requette.ToString());
                    logger.Error("erreur=" + ex.Message);
                }
            }
            */
            return (nbrCnuf != 0);
        }


        public ActionResult View(int id)
        {
            keepFilters();
            FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();
            FACTDEMANDE=FACTDEMANDE.getOne(id);
            FACTDEMANDE.TVATOTAUX = FACTDEMANDE.calcultotauxtv(id);
            //KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            return View(FACTDEMANDE);
        }

        [NoCache]
        [HttpPost]
        public ActionResult Edit(int id)
        {
            keepFilters();
            FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();
            FACTDEMANDE = FACTDEMANDE.getOne(id);

            if (FACTDEMANDE.FACTTYPE.FACTTYPECATEGORIE.LIBELLE == clientStation && FACTDEMANDE.DATEIMPRESSION.HasValue)
            {
                TempData["state"] = "2";
                TempData["success"] = "false";
                TempData["message"] = string.Format("Impossible de modifier la demande car elle est déja imprimée !", FACTDEMANDE.REFERENCEFACT, FACTDEMANDE.LIBELLEDEMANDE);
                return RedirectToAction("Index", new { keepFilters = true });
            }
            
            ViewBag.isAdmin = true;

            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

            List<FACTTYPE> listTypesfacture = getTypefacture(UTILISATEURCONNECTE);
            if (listTypesfacture.Count != 1)
                listTypesfacture.Insert(0, new FACTTYPE(-1, "Sélectionner un type facture"));
            ViewBag.listTypes = listTypesfacture;

            List<FACTTYPECLIENT> listTypesclient = getTypeclient();
            ViewBag.listTypesclient = listTypesclient;

            List<FACTCLIENT> lsclients = getlistclients();
            lsclients.Insert(0, new FACTCLIENT(-1, "Sélectionner un client"));
            ViewBag.listclients = lsclients;

            List<FACTSITE> lsclientsinterne = getlistclientsinterne();
            lsclientsinterne.Insert(0, new FACTSITE(-1, "Sélectionner un client"));
            ViewBag.listclientsinterne = lsclientsinterne;

            List<FACTSITE> listsite = getlistsites();
            ViewBag.listsite = listsite;

            ViewBag.listtva = getlisttauxtva();
            ViewBag.listtvaipprf = getlisttauxtvaipprf();

            return View(FACTDEMANDE);
        }

        [HttpPost]
        public bool LoginExist(string login)
        {
            FACTUSER FACTUSER = new FACTUSER();
            return FACTUSER.exists(login);
        }

        public void keepFilters()
        {
            TempData.Keep("page");
            TempData.Keep("search");
            TempData.Keep("sortby");
            TempData.Keep("isasc");
            TempData.Keep("advSearch");
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ValidateEdit(int IDDEMANDE, string LIBELLEDEMANDE, string CHAPITRE, int? RAYON, string CNUFVENDEUR, int TYPECLIENT, int IDCLIENT, int IDCLIENTINTERNE, String CNUFACHETEUR, int? RAYONACHETEUR, string IDCAT, int? IDSITE, string IDENTIFCLIENT, string ANNEEPRESTATION, string selectedDetails, string DESIGNATIONCLIENT, HttpPostedFileBase file)
        {
            int IDFACTTYPE = int.Parse(IDCAT.Split(',')[0]);
            int IDCATEGORIE = int.Parse(IDCAT.Split(',')[1]);
            FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            int IdSiteConnecte = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.SerialNumber).Value);
            if (User.IsInRole("SIEGE") && IDSITE != -1)
            {
                IdSiteConnecte = IDSITE.Value;
            }
            string fileName = "";
            string fileNameInterne = "";
            string contenttype = "";
            // Verify that the user selected a file
            if (file != null && file.ContentLength > 0)
            {
                fileName = Path.GetFileName(file.FileName);
                fileNameInterne = FACTDEMANDE.seqFile() + Path.GetExtension(file.FileName);
                contenttype = file.ContentType;
            }

            bool isCreated = IDDEMANDE == 0 ? true : false;
            if (isCreated)
                TempData["state"] = "1";
            else
                TempData["state"] = "2";
            ///KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            //on commence par vérifier si le login est unique
            string MNTTVAREV = "0";
            string MNTTVAIPPRF ="0";
            if (IDCATEGORIE == 4)
            {
                if (Request.Form["MNTTVAREV"] != null && Request.Form["MNTTVAREV"].Trim().Length > 0)
                    MNTTVAREV = Request.Form["MNTTVAREV"];
            }
            if (IDCATEGORIE == 5)
            {
                MNTTVAREV = Request.Form["TAUXTVA"];
                MNTTVAIPPRF = Request.Form["TAUXTVAIPPRF"];

            }

            if (IDFACTTYPE <= 0)
            {
                //erreur lors de la création d'un compte commercial par un profil Administrateur
                keepFilters();
                TempData["success"] = "false";
                TempData["message"] = "Erreur : Vous devez choisir un type de facture dans la liste déroulante";

                List<FACTTYPE> listTypesfacture = getTypefacture(UTILISATEURCONNECTE);
                if (listTypesfacture.Count != 1)
                    listTypesfacture.Insert(0, new FACTTYPE(-1, "Sélectionner un type facture"));
                ViewBag.listTypes = listTypesfacture;

                List<FACTTYPECLIENT> listTypesclient = getTypeclient();
                ViewBag.listTypesclient = listTypesclient;

                List<FACTCLIENT> lsclients = getlistclients();
                lsclients.Insert(0, new FACTCLIENT(-1, "Sélectionner un client"));
                ViewBag.listclients = lsclients;

                List<FACTSITE> lsclientsinterne = getlistclientsinterne();
                lsclientsinterne.Insert(0, new FACTSITE(-1, "Sélectionner un client"));
                ViewBag.listclientsinterne = lsclientsinterne;

                List<FACTSITE> listsite = getlistsites();
                ViewBag.listsite = listsite;

                ViewBag.listtva = getlisttauxtva();
                ViewBag.listtvaipprf = getlisttauxtvaipprf();

                return View("Edit", FACTDEMANDE);
            }
            string[][] details = null;
            if (selectedDetails != null && selectedDetails.Length > 0)
                details = JsonConvert.DeserializeObject<string[][]>(selectedDetails);
            //try
            //{
            if (IDCATEGORIE == 5)
            {
                FACTDEMANDE = FACTDEMANDE.updateMutiSites(IDDEMANDE, LIBELLEDEMANDE, IDFACTTYPE, CHAPITRE, RAYON, CNUFVENDEUR, TYPECLIENT, IDCLIENT, IDCLIENTINTERNE, CNUFACHETEUR, RAYONACHETEUR, ANNEEPRESTATION, MNTTVAREV, MNTTVAIPPRF, UTILISATEURCONNECTE, IdSiteConnecte, details, fileName, fileNameInterne, contenttype);
            }
            else
            {
                FACTDEMANDE = FACTDEMANDE.update(IDDEMANDE, LIBELLEDEMANDE, IDFACTTYPE, CHAPITRE, RAYON, CNUFVENDEUR, TYPECLIENT, IDCLIENT, IDCLIENTINTERNE, CNUFACHETEUR, RAYONACHETEUR, ANNEEPRESTATION, MNTTVAREV, UTILISATEURCONNECTE, IdSiteConnecte, details, fileName, fileNameInterne, contenttype);
            }
            if (file != null && file.ContentLength > 0)
            {
                var path = Path.Combine(ConfigurationManager.AppSettings["targetFolderImp"], fileNameInterne);
                file.SaveAs(path);
            }
            FACTDEMANDE.TVATOTAUX = FACTDEMANDE.calcultotauxtv(FACTDEMANDE.IDDEMANDE);


            TempData["success"] = "true";
            if (isCreated)
            {
                TempData["message"] = string.Format("La demande {0} {1} a bien été crée !", FACTDEMANDE.REFERENCEFACT, FACTDEMANDE.LIBELLEDEMANDE);
                if (IDCATEGORIE != 2)
                {
                    sendmailSM(FACTDEMANDE.IDDEMANDE);//FACTDEMANDE.REFERENCEFACT, FACTDEMANDE.LIBELLEDEMANDE, new FACTTYPE().getOne(IDFACTTYPE).LIBELLE);
                }
            }
            else
                TempData["message"] = string.Format("La demande {0} {1} a bien été modifié !", FACTDEMANDE.REFERENCEFACT, FACTDEMANDE.LIBELLEDEMANDE);
            //}
            //catch (Exception ex)
            //{
            //    TempData["success"] = "false";
            //    TempData["message"] = string.Format("Erreur system : {0}", ex.Message);
            //    logger.Error(string.Format("Erreur system : {0}", ex.Message));
            //}
            return RedirectToAction("Index", new { keepFilters = true });
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ValidateEditExport(int IDDEMANDE, string LIBELLEDEMANDE, string CHAPITRE, int? RAYON, string CNUFVENDEUR, int TYPECLIENT, int IDCLIENT, int IDCLIENTINTERNE, String CNUFACHETEUR, int? RAYONACHETEUR, string IDCAT, int? IDSITE, string IDENTIFCLIENT, string ANNEEPRESTATION, string selectedDetails, string DESIGNATIONCLIENT, HttpPostedFileBase file)
        {
            int IDFACTTYPE = int.Parse(IDCAT.Split(',')[0]);
            int IDCATEGORIE = int.Parse(IDCAT.Split(',')[1]);
            FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            int IdSiteConnecte = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.SerialNumber).Value);
            if (User.IsInRole("SIEGE") && IDSITE != -1)
            {
                IdSiteConnecte = IDSITE.Value;
            }
            string fileName = "";
            string fileNameInterne = "";
            string contenttype = "";
            // Verify that the user selected a file
            if (file != null && file.ContentLength > 0)
            {
                fileName = Path.GetFileName(file.FileName);
                fileNameInterne = FACTDEMANDE.seqFile() + Path.GetExtension(file.FileName);
                contenttype = file.ContentType;
            }

            bool isCreated = IDDEMANDE == 0 ? true : false;
            if (isCreated)
                TempData["state"] = "1";
            else
                TempData["state"] = "2";
            ///KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            //on commence par vérifier si le login est unique
            //string MNTTVAREV = "0";
            //if (IDCATEGORIE == 4)
            //{
            //    if (Request.Form["MNTTVAREV"] != null && Request.Form["MNTTVAREV"].Trim().Length > 0)
            //        MNTTVAREV = Request.Form["MNTTVAREV"];
            //}
            //if (IDCATEGORIE == 5)
            //{
            //    MNTTVAREV = Request.Form["TAUXTVA"];
            //}

            if (IDFACTTYPE <= 0)
            {
                //erreur lors de la création d'un compte commercial par un profil Administrateur
                keepFilters();
                TempData["success"] = "false";
                TempData["message"] = "Erreur : Vous devez choisir un type de facture dans la liste déroulante";

                List<FACTTYPE> listTypesfacture = getTypefactureExport(UTILISATEURCONNECTE);
                if (listTypesfacture.Count != 1)
                    listTypesfacture.Insert(0, new FACTTYPE(-1, "Sélectionner un type facture"));
                ViewBag.listTypes = listTypesfacture;

                List<FACTTYPECLIENT> listTypesclient = getTypeclient();
                ViewBag.listTypesclient = listTypesclient;

                List<FACTCLIENT> lsclients = getlistclients();
                lsclients.Insert(0, new FACTCLIENT(-1, "Sélectionner un client"));
                ViewBag.listclients = lsclients;

                List<FACTSITE> lsclientsinterne = getlistclientsinterne();
                lsclientsinterne.Insert(0, new FACTSITE(-1, "Sélectionner un client"));
                ViewBag.listclientsinterne = lsclientsinterne;

                List<FACTSITE> listsite = getlistsites();
                ViewBag.listsite = listsite;

                ViewBag.listtva = getlisttauxtva();
                ViewBag.listtvaipprf = getlisttauxtvaipprf();

                return View("Export", FACTDEMANDE);
            }
            string[][] details = null;
            if (selectedDetails != null && selectedDetails.Length > 0)
                details = JsonConvert.DeserializeObject<string[][]>(selectedDetails);
            //try
            //{

                if (file != null && file.ContentLength > 0 && isCreated)
                {
                    Object[] retour = processImport.importProcess(file);
                    if (retour !=null && retour[0] == "OK")
                    {
                        List<string[]> detailss = (List<string[]>)retour[1];
                        FACTDEMANDE = FACTDEMANDE.updateExport(IDDEMANDE, LIBELLEDEMANDE, IDFACTTYPE, RAYON, CNUFVENDEUR, TYPECLIENT, IDCLIENT, IDCLIENTINTERNE, CNUFACHETEUR, RAYONACHETEUR, ANNEEPRESTATION, UTILISATEURCONNECTE, IdSiteConnecte, detailss, fileName, fileNameInterne, contenttype);
                    }
                    else
                    {
                        string msgerror = "";
                        if (retour == null)
                            msgerror = "Le fichier ne correspond pas à un fichier import facture Export";
                        else
                            msgerror = (string)retour[0];

                        //erreur lors de la création d'un compte commercial par un profil Administrateur
                        keepFilters();
                        TempData["success"] = "false";
                        TempData["message"] = msgerror;

                        List<FACTTYPE> listTypesfacture = getTypefactureExport(UTILISATEURCONNECTE);
                        if (listTypesfacture.Count != 1)
                            listTypesfacture.Insert(0, new FACTTYPE(-1, "Sélectionner un type facture"));
                        ViewBag.listTypes = listTypesfacture;

                        List<FACTTYPECLIENT> listTypesclient = getTypeclient();
                        ViewBag.listTypesclient = listTypesclient;

                        List<FACTCLIENT> lsclients = getlistclients();
                        lsclients.Insert(0, new FACTCLIENT(-1, "Sélectionner un client"));
                        ViewBag.listclients = lsclients;

                        List<FACTSITE> lsclientsinterne = getlistclientsinterne();
                        lsclientsinterne.Insert(0, new FACTSITE(-1, "Sélectionner un client"));
                        ViewBag.listclientsinterne = lsclientsinterne;

                        List<FACTSITE> listsite = getlistsites();
                        ViewBag.listsite = listsite;

                        ViewBag.listtva = getlisttauxtva();
                        ViewBag.listtvaipprf = getlisttauxtvaipprf();

                    return View("Export", FACTDEMANDE);
                    }
                }
                else
                {
                    if (!isCreated)
                    {
                        List<string[]> detailss = null;
                        if (selectedDetails != null && selectedDetails.Length > 0)
                            detailss = JsonConvert.DeserializeObject<List<string[]>>(selectedDetails);
                        FACTDEMANDE = FACTDEMANDE.updateExport(IDDEMANDE, LIBELLEDEMANDE, IDFACTTYPE, RAYON, CNUFVENDEUR, TYPECLIENT, IDCLIENT, IDCLIENTINTERNE, CNUFACHETEUR, RAYONACHETEUR, ANNEEPRESTATION, UTILISATEURCONNECTE, IdSiteConnecte, detailss, fileName, fileNameInterne, contenttype);
                    }
                }
            
            if (file != null && file.ContentLength > 0)
            {
                var path = Path.Combine(ConfigurationManager.AppSettings["targetFolderImp"], fileNameInterne);
                file.SaveAs(path);
            }
            FACTDEMANDE.TVATOTAUX = FACTDEMANDE.calcultotauxtv(FACTDEMANDE.IDDEMANDE);


            TempData["success"] = "true";
            if (isCreated)
            {
                TempData["message"] = string.Format("La demande {0} {1} a bien été crée !", FACTDEMANDE.REFERENCEFACT, FACTDEMANDE.LIBELLEDEMANDE);
                if (IDCATEGORIE != 2)
                {
                    sendmailSM(FACTDEMANDE.IDDEMANDE);//FACTDEMANDE.REFERENCEFACT, FACTDEMANDE.LIBELLEDEMANDE, new FACTTYPE().getOne(IDFACTTYPE).LIBELLE);
                }
            }
            else
                TempData["message"] = string.Format("La demande {0} {1} a bien été modifié !", FACTDEMANDE.REFERENCEFACT, FACTDEMANDE.LIBELLEDEMANDE);

            return RedirectToAction("Index", new { keepFilters = true });
        }

        public static void sendmailSM(int id) //string reference,string libelle,string chapitre
        {
            try {
                    FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();
                    FACTDEMANDE = FACTDEMANDE.getOne(id);
                    ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
                    int iduser = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
                    int IdSiteConnecte = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.SerialNumber).Value);
                    FACTUSER user = new FACTUSER();
                    user = user.getOne(iduser);

                    FACTSITE site = new FACTSITE();
                    site = site.getOne(IdSiteConnecte);

                    FACTGROUPE grp = new FACTGROUPE();
                    List<FACTGROUPE> listeGroupe = grp.getAllDunInterface("VALIDATOR");
                    List<long> liste = new List<long>();
                    foreach (FACTGROUPE item in listeGroupe){
                        liste.Add(item.IDGROUPE);
                    }
                    List<FACTUSER> listeUsers = user.getAllFromGroupsForMail(liste, FACTDEMANDE.FACTTYPE.IDCATEGORIE.Value);//user.getAllFromGroups(liste);

                    MailMessage mail = new MailMessage("noreply@appfacture.com", ConfigurationManager.AppSettings["MAIL_CREATION_TO"]);
                    mail.IsBodyHtml = true;
                    SmtpClient client = new SmtpClient();
                    client.Port = 25;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = true;
                    client.Host = ConfigurationManager.AppSettings["MAIL_SMTP"];
                    foreach (FACTUSER usr in listeUsers) {
                        if (!string.IsNullOrWhiteSpace(usr.EMAIL) && usr.EMAIL.Contains("@") && usr.EMAIL.Contains(".co.ma"))
                            mail.To.Add(new MailAddress(usr.EMAIL.Trim()));
                    }
                    mail.Subject = "App. Facturation : Notification creation demande facture";
                    List<string> msg = new List<string>();
                    msg.Add("Bonjour,");
                    msg.Add("Une demande facture a été crée par l'utilisateur : " + user.PRENOM+" "+user.NOM +" ("+site.LIBELLESITE+").");
                    msg.Add("   ");
                    msg.Add("Référence demande: " + FACTDEMANDE.REFERENCEFACT);
                    msg.Add("Libellé demande: " + FACTDEMANDE.LIBELLEDEMANDE);
                    msg.Add("Chapitre: " + FACTDEMANDE.FACTTYPE.LIBELLE);
                    msg.Add("   ");
                    msg.Add("Excellente journée.");
                 
                    string body = string.Join("<br />", msg.ToArray()); //System.Environment.NewLine
                    mail.Body = body;
                    client.Send(mail);
            }catch(Exception e){
                logger.Info("problème envoie email");
            }
        }
        //public void ExportPdf(int iddemande)
        //{
        //    FACTDEMANDE dem = new FACTDEMANDE();
        //    dem = dem.getOne(iddemande);
        //    CrystalReportFacture crp = new CrystalReportFacture();
        //    using (EramEntities DB = new EramEntities())
        //    {
        //        StringBuilder requette = new StringBuilder();

        //        requette.Append("select e.referencefact nofacture,d.referenceproduit code,d.libelleproduit designation, ");
        //        requette.Append(" d.quantite qte,d.prixunitaire prix,d.tauxtva txtva,d.mnttva mnttva,d.mntttc mntttc ");
        //        requette.Append(" from factdemande e,factdemandedetail d ");
        //        requette.Append("where e.iddemande=");
        //        requette.Append(iddemande);
        //        requette.Append(" and e.iddemande = d.iddemande ");
        //        string SQL = requette.ToString();
        //        //string ConnectionString = DB.Database.Connection.ConnectionString;
        //        OracleConnection conn = (OracleConnection)DB.Database.Connection;
        //        OracleDataAdapter da = new OracleDataAdapter();
        //        //SqlCommand cmd = conn.CreateCommand();
        //        OracleCommand cmd = new OracleCommand(SQL, conn);
        //        //cmd.CommandText = SQL;
        //        da.SelectCommand = cmd;
        //        DataSetFacture ds = new DataSetFacture();
        //        conn.Open();
        //        da.Fill(ds.facture);
        //        ds.AcceptChanges();

        //        crp.SetDataSource(ds);

        //    logger.Info(" Nombre table ds=" + ds.Tables.Count);
        //    //logger.Info(" ds.Tables[0].TableName=" + ds.Tables[0].TableName);
        //    //logger.Info(" ds.Tables[1].TableName=" + ds.Tables[1].TableName);
        //    //logger.Info(" ds.Tables[0].Rows.Count=" + ds.Tables[0].Rows.Count);
        //    //logger.Info(" ds.Tables[1].Rows.Count=" + ds.Tables[1].Rows.Count);
        //    logger.Info(" Nombre rows facture ds=" + ds.facture.Rows.Count);
        //    var fileName = "Facture_"+iddemande + ".pdf";

        //    using (Stream file = crp.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat))
        //    {
        //        byte[] bytes = new byte[file.Length];
        //        file.Read(bytes, 0, (int)file.Length);
        //        Response.Clear();
        //        Response.ContentType = "application/pdf";
        //        Response.AddHeader("Content-Length", bytes.Length.ToString());
        //        Response.AddHeader("Content-disposition", "attachment; filename=" + fileName);
        //        Response.BinaryWrite(bytes);
        //        Response.Flush();
        //        Response.End();
        //    }

        //    conn.Close();
        //    }
        //}


        public void ExportPdf(int iddemande)
        {
            FACTDEMANDE dem = new FACTDEMANDE();
            dem = dem.getOne(iddemande);
            //CrystalReportFacture crp = new CrystalReportFacture();
            ReportDocument  crp = dem.ExportPdf(iddemande);

            //CrystalReportfact crp = null;
            //CrystalReportfactlg crp2 = null;
            //if (dem.FACTTYPE.IDCATEGORIE == 3)
            //    crp2 = new CrystalReportfactlg();
            //else
            //    crp = new CrystalReportfact();

                //logger.Info(" Nombre rows facture ds=" + ds.facture.Rows.Count);
                var fileName = "Facture_" + iddemande + ".pdf";

                using (Stream file = crp.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat))
                {
                    byte[] bytes = new byte[file.Length];
                    file.Read(bytes, 0, (int)file.Length);

                    if (dem.FACTTYPE.FACTTYPECATEGORIE.LIBELLE != clientStation && dem.MNTTTC > 0)
                    { 
                        FACTFACTURE facture = new FACTFACTURE();
                        facture.imprimer(1, iddemande);
                    }
                    else
                    {
                        if (dem.STATUS == 0 && !dem.DATEIMPRESSION.HasValue && dem.MNTTTC > 0) { 
                            dem.imprimer(1, iddemande);
                            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
                            int iduser = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
                            FACTENSEIGNE ens = new FACTENSEIGNE();
                            ens = ens.getOne(dem.FACTSITE1.IDENSEIGNE.Value);
                            String periode = CommonFacturation.periodeOuverte(ens.CODE.Trim()).ToString();
                            dem.validerOuRefuser(iddemande, 1, "client station: validation automatique apres impression", iduser, periode);
                        }
                    }

                    Response.Clear();
                    Response.ContentType = "application/pdf";
                    Response.AddHeader("Content-Length", bytes.Length.ToString());
                    Response.AddHeader("Content-disposition", "attachment; filename=" + fileName);
                    Response.BinaryWrite(bytes);
                    Response.Flush();
                    Response.End();
                }
        }


        public void DownloadPJ(int idFichier)
        {
            FACTDEMANDE dem = new FACTDEMANDE();
            FACTPIECEJOINTE pj = dem.getOnePJ(idFichier);
            var fileName = pj.NOM;
            if (pj.NOMINTERNE == null)
            {
                return;
            }
            var path = Path.Combine(ConfigurationManager.AppSettings["targetFolderImp"], pj.NOMINTERNE);
            if (!System.IO.File.Exists(path))
            {
                return;
            }
            using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[file.Length];
                file.Read(bytes, 0, (int)file.Length);
                //exportData.Write(bytes, 0, (int)file.Length);
                //byte[] bytes = exportData.ToArray();
                Response.Clear();
                if (pj.CONTENTTYPE != null && pj.CONTENTTYPE.Length > 0)
                    Response.ContentType = pj.CONTENTTYPE;
                else
                    Response.ContentType = "multipart/form-data";
                Response.AddHeader("Content-Length", bytes.Length.ToString());
                Response.AddHeader("Content-disposition", "attachment; filename=" + fileName);
                Response.BinaryWrite(bytes);
                Response.Flush();
                Response.End();
            }
        }

        
        [HttpPost]
        public ActionResult Valider(int id, string commentaire)
        {
            keepFilters();
            short status=2;
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int iduser = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (commentaire==null)
                commentaire="";
            if (Request.Form["valider"] != null)
            {
                status=1;
            }
            else if (Request.Form["refuser"] != null)
            {
                 status=2;
            }
            FACTDEMANDE demande = new FACTDEMANDE();
            demande = demande.getOne(id);
            if (demande.MNTTTC > 0){
                    FACTENSEIGNE ens = new FACTENSEIGNE();
                    ens = ens.getOne(demande.FACTSITE1.IDENSEIGNE.Value);
                String periode = CommonFacturation.periodeOuverte(ens.CODE.Trim()).ToString();
                demande.validerOuRefuser(id, status, commentaire, iduser, periode);
            }
            else
            {
                TempData["state"] = "1";
                TempData["success"] = "true";
                TempData["message"] = string.Format("La demande {0} {1} ne peux pas être validée, le montant est nul !", demande.REFERENCEFACT, demande.LIBELLEDEMANDE);
            }
            return RedirectToAction("Index", new { keepFilters = true });
        }

        [HttpPost]
        public ActionResult AjoutClient(int id, string returnUrl, string refclient, string libclient, string iceclient, string adrclient)
        {
            keepFilters();
            //ILog logger = log4net.LogManager.GetLogger("EramGoldLogger");
            //logger.Error("RECLID=" + id);
            bool erreur = false;
            if (refclient.Length > 30) {
                TempData["success"] = "false";
                TempData["message"] = string.Format("La référence client ne doit pas dépasser 30 caractères");
                erreur = true;
            }
            if (libclient.Length > 50)
            {
                TempData["success"] = "false";
                TempData["message"] = string.Format("La désignation client ne doit pas dépasser 50 caractères");
                erreur = true;
            }
            if (iceclient.Length != 15)
            {
                TempData["success"] = "false";
                TempData["message"] = string.Format("L'ICE client doit être exactement 15 caractères");
                erreur = true;
            }
            if (adrclient.Length > 50)
            {
                TempData["success"] = "false";
                TempData["message"] = string.Format("L'adresse facturation client ne doit pas dépasser 50 caractères");
                erreur = true;
            }

            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            FACTCLIENT client = new FACTCLIENT();
            if (!erreur)
                client.update(0, refclient, libclient, iceclient, adrclient, UTILISATEURCONNECTE);
            
            FACTDEMANDE savarticle = new FACTDEMANDE();
            List<FACTCLIENT> lsclients = getlistclients();
            lsclients.Insert(0, new FACTCLIENT(-1, "Sélectionner un client"));
            ViewBag.listclients = lsclients;

            List<FACTSITE> lsclientsinterne = getlistclientsinterne();
            lsclientsinterne.Insert(0, new FACTSITE(-1, "Sélectionner un client"));
            ViewBag.listclientsinterne = lsclientsinterne;

            ViewBag.listtva = getlisttauxtva();
            ViewBag.listtvaipprf = getlisttauxtvaipprf();

            List<FACTTYPE> listTypesfacture = getTypefacture(UTILISATEURCONNECTE);
            if (listTypesfacture.Count != 1)
                listTypesfacture.Insert(0, new FACTTYPE(-1, "Sélectionner un type facture"));
            ViewBag.listTypes = listTypesfacture;

            List<FACTTYPECLIENT> listTypesclient = getTypeclient();
            ViewBag.listTypesclient = listTypesclient;


            List<FACTSITE> listsite = getlistsites();
            ViewBag.listsite = listsite;

            if (id > 0) {
                savarticle = savarticle.getOne(id);
            }
            //ModelState.Clear();
            //return Redirect(Request.UrlReferrer.PathAndQuery);
            //return Redirect(returnUrl);
            return View("Edit", savarticle);

            //SAVRECL Reclamation = new SAVRECL();
            //Reclamation = Reclamation.getOne(id);
            //TempData["state"] = "4";

            //if (IDDESCRIPT == null || IDDESCRIPT.Length <= 0)
            //{
            //    TempData["success"] = "false";
            //    TempData["message"] = string.Format("La description est obligatoire");
            //}
            //else
            //{
            //    try
            //    {
            //        //bool isValidated = true;
            //        Reclamation.validate(id, USERID, 2, IDDESCRIPT, UTILISATEURCONNECTE);

            //        TempData["success"] = "true";
            //        TempData["message"] = string.Format("La réclamation {0} a bien été validée !", Reclamation.RECLLIB);

            //    }
            //    catch (Exception ex)
            //    {
            //        TempData["success"] = "false";
            //        TempData["message"] = string.Format("Erreur system : {0}", ex.Message);
            //    }
            //}
            //return RedirectToAction("Index", new { keepFilters = true });
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            TempData["state"] = "3";
            try
            {
                FACTDEMANDE FACTDEMANDE = new FACTDEMANDE();
                FACTDEMANDE = FACTDEMANDE.getOne(id);
                if (FACTDEMANDE.FACTTYPE.FACTTYPECATEGORIE.LIBELLE == clientStation && FACTDEMANDE.DATEIMPRESSION.HasValue)
                {
                    TempData["success"] = "false";
                    TempData["message"] = string.Format("Impossible de supprimer la demande car elle est déja imprimée !", FACTDEMANDE.REFERENCEFACT, FACTDEMANDE.LIBELLEDEMANDE);
                }else {
                    FACTDEMANDE.delete(id);
                    TempData["success"] = "true";
                    TempData["message"] = string.Format("La demande {0} {1} a bien été supprimé !", FACTDEMANDE.REFERENCEFACT, FACTDEMANDE.LIBELLEDEMANDE);
                }
            }
            catch (Exception ex)
            {
                TempData["success"] = "false";
                TempData["message"] = string.Format("Erreur system : {0}", ex.Message);
            }
            return RedirectToAction("Index", new { keepFilters = true });
        }


    }
}
