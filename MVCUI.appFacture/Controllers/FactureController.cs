using System;
using System.Globalization;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Web.Mvc;
using System.Text;
using System.IO;
using Domain.appFacture;
using appFacture.Models;
using System.Web.Script.Serialization;
using System.Security.Claims;
using System.Threading;
using Newtonsoft.Json;
using log4net;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Data;
using NPOI.SS.Util;
using Domain.appFacture.Rapports;
using CrystalDecisions.CrystalReports.Engine;
using Oracle.ManagedDataAccess.Client;



namespace appFacture.Controllers
{
    [Authorize(Roles = "FACTURE")]
    public class FactureController : Controller
    {
        
        public int pageSize = 10;
        ILog logger = log4net.LogManager.GetLogger("KassagrWEBLogger");
        public static string agrDbLink = ConfigurationManager.AppSettings["AGRDBLINK"];
        //public static string OracleConnectionAgresso = ConfigurationManager.ConnectionStrings["Agrprod"].ConnectionString;
        public static string OracleConnectionAgresso = ConfigurationManager.ConnectionStrings["Agrtest"].ConnectionString;
        public static string OracleConAppFact = ConfigurationManager.ConnectionStrings["Factdb"].ConnectionString;
        public static string tauxtsc = ConfigurationManager.AppSettings["TAUXTSC"];

        private const string compteBidon = "99999999";

        List<String> rapportGereration = new List<String>();

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

            FACTUREAdvFiltre advFiltre = new JavaScriptSerializer().Deserialize<FACTUREAdvFiltre>(advSearch);
            if (advFiltre == null)
            {
                advFiltre = new FACTUREAdvFiltre();
            }
            // KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            int IdSiteConnecte = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.SerialNumber).Value);
            advFiltre.site = IdSiteConnecte;
            if (User.IsInRole("SIEGE"))
            {
                advFiltre.site = -1;
            }
            advFiltre.user = UTILISATEURCONNECTE;
            //appel de méthode de lecture des données
            FACTFACTURE FACTFACTURE = new FACTFACTURE();
            FACTUREPaginationRes result = FACTFACTURE.getAll(page, pageSize, search, sortby, isasc, advFiltre);
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
            List<FACTTYPE> listTypes = getTypefacture();
            listTypes.Insert(0, new FACTTYPE(-1, "Sélectionner un type facture"));
            ViewBag.listTypes = listTypes;


            List<FACTFACTURESTATUS> listStatus = getstatus();
            listStatus.Insert(0, new FACTFACTURESTATUS(-1, "Sélectionner un status"));
            ViewBag.listStatus = listStatus;

            return View(new FactureListView() { PagingInfo = pagingInfo, Factures = result.listFACTURE, Search = search, SortBy = sortby, IsAsc = isasc, AdvSearch = advSearch, AdvSearchFilters = advFiltre });
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
        public List<FACTTYPE> getTypefacture()
        {
            FACTTYPE FACTTYPE = new FACTTYPE();
            return FACTTYPE.getAll();
        }
        //Liste déroulante des status demandes
        public List<FACTFACTURESTATUS> getstatus()
        {
            FACTFACTURESTATUS FACTFACTURESTATUS = new FACTFACTURESTATUS();
            return FACTFACTURESTATUS.getAllStatus();
        }
        public List<FACTCLIENT> getlistclients()
        {
            FACTCLIENT FACTCLIENT = new FACTCLIENT();
            return FACTCLIENT.getAll();
        }


        [NoCache]
        public ViewResult Create(int id)
        {
            keepFilters();
            FACTFACTURE FACTFACTURE = new FACTFACTURE();
            ViewBag.listTypes = getTypefacture();
            List<FACTCLIENT> lsclients = getlistclients();
            //lsclients.Insert(0, new FACTCLIENT(-1, "Sélectionner un client"));
            ViewBag.listclients = lsclients;

            FACTFACTURE FACTFACTURE3 = new FACTFACTURE();
            FACTFACTURE3 = FACTFACTURE3.getOne(id);

            FACTFACTURE.IDFACTTYPE = FACTFACTURE3.IDFACTTYPE;
            FACTFACTURE.FACTTYPE = FACTFACTURE3.FACTTYPE;
            FACTFACTURE.IDFACTURE = FACTFACTURE3.IDFACTURE;
            FACTFACTURE.REFERENCEFACT = FACTFACTURE3.REFERENCEFACT;
            FACTFACTURE.LIBELLEDEMANDE = "Avoir : "+FACTFACTURE3.LIBELLEDEMANDE;
            FACTFACTURE.FLAGAVOIR = 0;
            //FACTFACTURE.MNTTVAREV = 0;

            ViewBag.listtva = getlisttauxtva();

            return View("Edit", FACTFACTURE);
        }

        [NoCache]
        public ViewResult Create2(int id)
        {
            keepFilters();
            FACTFACTURE FACTFACTURE = new FACTFACTURE();
            ViewBag.listTypes = getTypefacture();
            List<FACTCLIENT> lsclients = getlistclients();
            //lsclients.Insert(0, new FACTCLIENT(-1, "Sélectionner un client"));
            ViewBag.listclients = lsclients;

            FACTFACTURE FACTFACTURE3 = new FACTFACTURE();
            FACTFACTURE3 = FACTFACTURE3.getOne(id);

            FACTFACTURE.IDFACTTYPE = FACTFACTURE3.IDFACTTYPE;
            FACTFACTURE.FACTTYPE = FACTFACTURE3.FACTTYPE;
            FACTFACTURE.IDFACTURE = FACTFACTURE3.IDFACTURE;
            FACTFACTURE.REFERENCEFACT = FACTFACTURE3.REFERENCEFACT;
            FACTFACTURE.LIBELLEDEMANDE = "Avoir : " + FACTFACTURE3.LIBELLEDEMANDE;
            FACTFACTURE.FLAGAVOIR = 0;
            //FACTFACTURE.MNTTVAREV = 0;

            List<long> sites = new List<long>();
            foreach (FACTFACTUREDETAIL det in FACTFACTURE3.FACTFACTUREDETAIL)
            {
                sites.Add((det.LIBELLEPRODUIT.Length > 0 ? int.Parse(det.LIBELLEPRODUIT.Substring(0, det.LIBELLEPRODUIT.IndexOf("-"))) : -1));
            }

            ViewBag.listsite = getlistsites(sites);
            ViewBag.listtva = getlisttauxtva();

            return View("Edit2", FACTFACTURE);
        }

        public ActionResult View(int id)
        {
            keepFilters();
            FACTFACTURE FACTFACTURE = new FACTFACTURE();
            FACTFACTURE = FACTFACTURE.getOne(id);
            FACTFACTURE.TVATOTAUX = FACTFACTURE.calcultotauxtv(id);
            //KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            return View(FACTFACTURE);
        }

        [NoCache]
        [HttpPost]
        public ActionResult Edit(int id)
        {
            keepFilters();
            FACTFACTURE FACTFACTURE = new FACTFACTURE();
            //ViewBag.isAdmin = true;
            //ViewBag.listTypes = getTypefacture();
            //List<FACTCLIENT> lsclients = getlistclients();
            //lsclients.Insert(0, new FACTCLIENT(-1, "Sélectionner un client"));
            //ViewBag.listclients = lsclients;
            FACTFACTURE = FACTFACTURE.getOne(id);
            ViewBag.listtva = getlisttauxtva();

            return View(FACTFACTURE);
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
        public ActionResult ValidateEdit(int IDFACTURE,int FLAGAVOIR, string LIBELLEDEMANDE, string selectedDetails)
        {
            FACTFACTURE FACTFACTURE = new FACTFACTURE();
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            int IdSiteConnecte = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.SerialNumber).Value);

            string MNTTVAREV = "0";
            if (Request.Form["MNTTVAREV"] != null && Request.Form["MNTTVAREV"].Trim().Length > 0)
                MNTTVAREV = Request.Form["MNTTVAREV"];

            try {
                    bool isCreated = false;
                    if (FLAGAVOIR == 0)
                        isCreated = true;

                    if (isCreated)
                        TempData["state"] = "1";
                    else
                        TempData["state"] = "2";

                    string[][] details=null;

                    if (selectedDetails != null && selectedDetails.Length > 0)
                    {
                        details = JsonConvert.DeserializeObject<string[][]>(selectedDetails);
                        string[] det=(details.Length>0?details[0]:null);
                        if (det != null && det[2] != null && det[2].Length > 0 && decimal.Parse(det[2].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." }) > 0)
                        {
                            if (FLAGAVOIR == 0)
                                FACTFACTURE.createavoir(IDFACTURE, LIBELLEDEMANDE, MNTTVAREV, UTILISATEURCONNECTE, details);
                            else
                                FACTFACTURE.updateavoir(IDFACTURE, LIBELLEDEMANDE, MNTTVAREV, UTILISATEURCONNECTE, details);

                            FACTFACTURE.TVATOTAUX = FACTFACTURE.calcultotauxtv(FACTFACTURE.IDFACTURE);
                            TempData["success"] = "true";
                            if (isCreated)
                                TempData["message"] = string.Format("L'avoir {0} {1} a bien été crée !", FACTFACTURE.REFERENCEFACT, FACTFACTURE.LIBELLEDEMANDE);
                            else
                                TempData["message"] = string.Format("L'avoir {0} {1} a bien été modifié !", FACTFACTURE.REFERENCEFACT, FACTFACTURE.LIBELLEDEMANDE);
                        }else
                        {
                            TempData["success"] = "false";
                            TempData["message"] = "Impossible de créer/modifier : Vous avez essayer de créer un avoir avec un montant null ou négatif !!!";
                            logger.Error("Impossible de créer/modifier : Vous avez essayé de créer un avoir avec un montant null ou négatif !!!");
                        }
                    }
                    else {
                        TempData["success"] = "false";
                        TempData["message"] ="Impossible de créer/modifier : Vous avez essayer de créer un avoir vide !!!";
                        logger.Error("Impossible de créer/modifier : Vous avez essayer de créer un avoir vide !!!");
                    }
            }
            catch (Exception ex)
            {
                TempData["success"] = "false";
                TempData["message"] = string.Format("Erreur system : {0}", ex.Message);
                logger.Error(string.Format("Erreur system : {0}", ex.Message));
            }
            return RedirectToAction("Index", new { keepFilters = true });
        }

        
        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ValidateEditAvMS(int IDFACTURE, int FLAGAVOIR, string TAUXTVA,string LIBELLEDEMANDE, string selectedDetails)
        {
            FACTFACTURE FACTFACTURE = new FACTFACTURE();
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            int IdSiteConnecte = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.SerialNumber).Value);


            try { 
                    bool isCreated = false;
                    if (FLAGAVOIR == 0)
                        isCreated = true;

                    if (isCreated)
                        TempData["state"] = "1";
                    else
                        TempData["state"] = "2";

                    string[][] details=null;

                    if (selectedDetails != null && selectedDetails.Length > 0)
                    {
                        details = JsonConvert.DeserializeObject<string[][]>(selectedDetails);
                        string[] det=(details.Length>0?details[0]:null);
                        if (det != null && det[2] != null && det[2].Length > 0 && decimal.Parse(det[2].Replace(",", "."), new NumberFormatInfo() { NumberDecimalSeparator = "." }) > 0)
                        {
                            if (FLAGAVOIR == 0)
                                FACTFACTURE.createavoirMS(IDFACTURE, LIBELLEDEMANDE, TAUXTVA, UTILISATEURCONNECTE, details);
                            else
                                FACTFACTURE.updateavoirMS(IDFACTURE, LIBELLEDEMANDE, TAUXTVA, UTILISATEURCONNECTE, details);

                            FACTFACTURE.TVATOTAUX = FACTFACTURE.calcultotauxtv(FACTFACTURE.IDFACTURE);
                            TempData["success"] = "true";
                            if (isCreated)
                                TempData["message"] = string.Format("L'avoir {0} {1} a bien été crée !", FACTFACTURE.REFERENCEFACT, FACTFACTURE.LIBELLEDEMANDE);
                            else
                                TempData["message"] = string.Format("L'avoir {0} {1} a bien été modifié !", FACTFACTURE.REFERENCEFACT, FACTFACTURE.LIBELLEDEMANDE);
                        }else
                        {
                            TempData["success"] = "false";
                            TempData["message"] = "Impossible de créer/modifier : Vous avez essayer de créer un avoir avec un montant null ou négatif !!!";
                            logger.Error("Impossible de créer/modifier : Vous avez essayé de créer un avoir avec un montant null ou négatif !!!");
                        }
                    }
                    else {
                        TempData["success"] = "false";
                        TempData["message"] ="Impossible de créer/modifier : Vous avez essayer de créer un avoir vide !!!";
                        logger.Error("Impossible de créer/modifier : Vous avez essayer de créer un avoir vide !!!");
                    }
            }
            catch (Exception ex)
            {
                TempData["success"] = "false";
                TempData["message"] = string.Format("Erreur system : {0}", ex.Message);
                logger.Error(string.Format("Erreur system : {0}", ex.Message));
            }
            return RedirectToAction("Index", new { keepFilters = true });
        }

        [NoCache]
        [HttpPost]
        public ActionResult Edit2(int id)
        {
            keepFilters();
            FACTFACTURE FACTFACTURE = new FACTFACTURE();
            //ViewBag.isAdmin = true;
            //ViewBag.listTypes = getTypefacture();
            //List<FACTCLIENT> lsclients = getlistclients();
            //lsclients.Insert(0, new FACTCLIENT(-1, "Sélectionner un client"));
            //ViewBag.listclients = lsclients;
            FACTFACTURE = FACTFACTURE.getOne(id);

            List<long> sites = new List<long>();
            foreach (FACTFACTUREDETAIL det in FACTFACTURE.getOne(FACTFACTURE.getOnebyDemande(FACTFACTURE.IDDEMANDE.Value).IDFACTURE).FACTFACTUREDETAIL)
            {
                sites.Add((det.LIBELLEPRODUIT.Length > 0 ? int.Parse(det.LIBELLEPRODUIT.Substring(0, det.LIBELLEPRODUIT.IndexOf("-"))) : -1));
            }

            ViewBag.listsite = getlistsites(sites);
            ViewBag.listtva = getlisttauxtva();

            return View(FACTFACTURE);
        }

        public List<FACTSITE> getlistsites(List<long> sites)
        {
            FACTSITE FACTSITE = new FACTSITE();
            List<FACTSITE> newlist = new List<FACTSITE>();
            foreach (FACTSITE site in FACTSITE.getRestricted(sites))
            {
                site.ADRLINE1 = site.IDSITE + "-" + site.LIBELLESITE;
                newlist.Add(site);
            }
            return newlist;
        }

        public List<TAUXTVA> getlisttauxtva()
        {
            List<TAUXTVA> newlist = new List<TAUXTVA>();
            newlist.Add(new TAUXTVA(20,"20"));
            newlist.Add(new TAUXTVA(14, "14"));
            newlist.Add(new TAUXTVA(10, "10"));
            newlist.Add(new TAUXTVA(7, "7"));
            newlist.Add(new TAUXTVA(0, "0"));
            return newlist;
        }

        public void ExportPdf(int idfacture)
        {
            FACTFACTURE facture = new FACTFACTURE();
            facture = facture.getOne(idfacture);
            ReportDocument crp = facture.ExportPdf(idfacture);
            var fileName = "Facture_" + idfacture + ".pdf";

            using (Stream file = crp.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat))
            {
                byte[] bytes = new byte[file.Length];
                file.Read(bytes, 0, (int)file.Length);
                facture.imprimer(2, idfacture);
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
            FACTFACTURE fact = new FACTFACTURE();
            FACTFACTPIECEJOINTE pj = fact.getOnePJ(idFichier);
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
                Response.Clear();
                if (pj.CONTENTTYPE != null && pj.CONTENTTYPE.Length>0)
                    Response.ContentType = pj.CONTENTTYPE;
                else
                    Response.ContentType = "multipart/form-data";//"multipart/form-data";//"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
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
            FACTFACTURE demande = new FACTFACTURE();
            demande.validerOuRefuser(id, status, commentaire, iduser);
            return RedirectToAction("Index", new { keepFilters = true });
        }

        [HttpPost]
        public ActionResult AjoutClient(int id, string refclient, string libclient, string iceclient, string adrclient)
        {
            keepFilters();
            //logger.Error("RECLID=" + id);
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            FACTCLIENT client = new FACTCLIENT();
            client.update(0, refclient, libclient, iceclient, adrclient, UTILISATEURCONNECTE);
            FACTFACTURE facture = new FACTFACTURE();
            List<FACTCLIENT> lsclients = getlistclients();
            ViewBag.listclients = lsclients;
            ViewBag.listTypes = getTypefacture();
            if (id > 0) {
                facture = facture.getOne(id);
            }
            return View("Edit", facture);
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            TempData["state"] = "3";
            try
            {
                ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
                int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

                FACTFACTURE FACTFACTURE = new FACTFACTURE();
                FACTFACTURE.delete(id, UTILISATEURCONNECTE);
                TempData["success"] = "true";
                TempData["message"] = string.Format("La facture {0} {1} a bien été supprimé !", FACTFACTURE.REFERENCEFACT, FACTFACTURE.LIBELLEDEMANDE);
            }
            catch (Exception ex)
            {
                TempData["success"] = "false";
                TempData["message"] = string.Format("Erreur system : {0}", ex.Message);
            }
            return RedirectToAction("Index", new { keepFilters = true });
        }

        [HttpPost]
        public ActionResult Reporter(int id)
        {
            TempData["state"] = "3";
            try
            {
                FACTFACTURE FACTFACTURE = new FACTFACTURE();
                FACTFACTURE.reporter(id);
                TempData["success"] = "true";
                TempData["message"] = string.Format("La facture {0} {1} a bien été reportée/dreportée !", FACTFACTURE.REFERENCEFACT, FACTFACTURE.LIBELLEDEMANDE);
            }
            catch (Exception ex)
            {
                TempData["success"] = "false";
                TempData["message"] = string.Format("Erreur system : {0}", ex.Message);
            }
            return RedirectToAction("Index", new { keepFilters = true });
        }

        public ActionResult Genbrouillard()
        {
            keepFilters();
            //try
            //{
                logger.Info("Début génération brouillard");
                rapportGereration.Clear();
                rapportGereration.Add("Début génération brouillard");
                FACTFACTURE ff = new FACTFACTURE();

                long nolot = ff.getSequence("NLOT_SEQ");
                ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
                int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

                List<FACTFACTURE> factures = ff.facturesAGenerer(UTILISATEURCONNECTE);
                foreach (FACTFACTURE facture in factures) {

                    logger.Info("Début traitement facture N°=" + facture.REFERENCEFACT + " - id=" + facture.IDFACTURE);
                    rapportGereration.Add("***** Début traitement facture N°=" + facture.REFERENCEFACT + " - id=" + facture.IDFACTURE + " ***** ");
                    int seq=-1;

                    if (!inserVendeur(facture.IDFACTURE, out seq, nolot))
                    {
                        facture.deleteAcrtrans(facture.IDFACTURE);
                        continue;
                    }
                    if (facture.TYPECLIENT == 1)
                    {
                        seq = -1;
                        if (!inserAcheteur(facture.IDFACTURE, seq, nolot))
                        {
                            facture.deleteAcrtrans(facture.IDFACTURE);
                            continue;
                        }
                    }


                    facture.acomptabiliser(facture.IDFACTURE, UTILISATEURCONNECTE);

                    logger.Info("Fin traitement facture N°=" + facture.REFERENCEFACT + " - id=" + facture.IDFACTURE);
                    rapportGereration.Add("***** Fin traitement facture N°=" + facture.REFERENCEFACT + " - id=" + facture.IDFACTURE + " ***** ");

                }

 
                logger.Info("Fin génération brouillard");
                rapportGereration.Add("Fin génération brouillard");

                string body = string.Join("-NEWLINE-", rapportGereration.ToArray());
                FACRAPPORTGEN rapport = new FACRAPPORTGEN();
                rapport.insererFichier(UTILISATEURCONNECTE, body);
            //}catch (Exception ex)
            //{
            //    logger.Info(string.Format("Erreur system : {0}", ex.Message));
            //    TempData["success"] = "false";
            //    TempData["message"] = string.Format("Erreur system : {0}", ex.Message);
            //}

            return RedirectToAction("Index", new { keepFilters = true });
        }



        public bool inserVendeur(int id, out int seq,long nolot)
        {
            seq = -1;
            FACTFACTURE facture = new FACTFACTURE();
            facture = facture.getOne(id);
            AGR bc = new AGR();
            FACTENSEIGNE ens = new FACTENSEIGNE();
            ens = ens.getOne(facture.FACTSITE1.IDENSEIGNE.Value);

            bc.IDFACTURE = facture.IDFACTURE;
            bc.NOLOT = (int)nolot;


            bc.DIM_1 = facture.FACTSITE1.DIM1; //CODEAGRESSO;//N° site
            //bc.DIM_2 = "DIM_2"; //TO DO
            string rayonvendeur=(facture.RAYON.HasValue ? facture.RAYON.Value.ToString() : " ");
            bc.DIM_4           = rayonvendeur;// (facture.RAYON.HasValue ? facture.RAYON.Value.ToString() : " ");

            //construire libelle facture
            if (facture.TYPECLIENT == 1)
            {
                FACTENSEIGNE ens2 = new FACTENSEIGNE();
                ens2 = ens2.getOne(facture.FACTSITE.IDENSEIGNE.Value);

                bc.DESCRIPTION = ens.LIBELLEENSEIGNE + "-" + ens2.LIBELLEENSEIGNE +"-"+facture.REFERENCEFACT+" (" + facture.LIBELLEDEMANDE + ")"; 
            }
            else {
                bc.DESCRIPTION = ens.LIBELLEENSEIGNE + "-" + facture.REFERENCEFACT + " (" + facture.LIBELLEDEMANDE + ")"; 
            }

            //bc.DESCRIPTION = ens.LIBELLEENSEIGNE + "-" + " (" + facture.LIBELLEDEMANDE + ")"; //a construire
            bc.CLIENT = ens.CODE;

            int anneeFiscal = -1;
            if (facture.ANNEEPRESTATION != null && facture.ANNEEPRESTATION.Length > 0)
            {
                bc.FISCAL_YEAR = Int32.Parse(facture.ANNEEPRESTATION);
                anneeFiscal = bc.FISCAL_YEAR;
            }
            else
            {
                bc.FISCAL_YEAR = Int32.Parse(facture.DATECREATION.Value.ToString("yyyy"));
            }

            bc.LAST_UPDATE = DateTime.Now;
            bc.PERIOD = CommonFacturation.periodeOuverte(bc.CLIENT); //gePeriodeOuverte(bc.CLIENT, anneeFiscal); // Int32.Parse(facture.DATECREATION.Value.ToString("yyyyMM"));
            if (bc.PERIOD == -1)
            {
                logger.Info("Problème communication avec Agresso ou aucune période ouverte pour la société :" + ens.CODE);
                rapportGereration.Add("Ko : Problème communication avec Agresso ou aucune période ouverte pour la société :" + ens.CODE);
                return false;
            }


            bc.TRANS_DATE = facture.DATECREATION.Value;

            bc.VOUCHER_DATE = DateTime.Now;
            bc.VOUCHER_TYPE = "GF";

            Int64 counter = 0;
            int trans_id = 0;

            if (!getListInfosClient(ens.CODE, out counter, out trans_id))
            {
                logger.Info("Ko: Probleme recuperation infos client :" + ens.CODE + " depuis Agresso, merci de vérifier la communication avec Agresso");
                rapportGereration.Add("Ko: Probleme recuperation infos client :" + ens.CODE + " depuis Agresso, merci de vérifier la communication avec Agresso");
                return false;
            }

            bc.VOUCHER_NO = counter;
            bc.TRANS_ID = trans_id;
            bc.EXT_INV_REF = facture.REFERENCEFACT;

            if (counter == 0) {
                logger.Info("Merci de parametrer le voucher_no sur Agresso pour la société :" + ens.CODE);
                rapportGereration.Add("Ko : Le voucher_no n'est pas paramétré sur Agresso pour la société :" + ens.CODE);
                return false;
            }

            FACTASSOGFACTTYPEENSEIGNE assoFE = ens.getOneAssoFactEns(facture.IDFACTTYPE.Value, ens.IDENSEIGNE);
            if (assoFE == null )
            {
                logger.Info("Merci de parametrer les comptes de comptabilisation !!!");
                rapportGereration.Add("Ko : Merci de parametrer les comptes de comptabilisation pour le chapitre de cette facture !!!");
                return false;
            }
            bool isok = true;
            /*****************************************Début Vendeur*******************************************************/
            bc.APAR_ID = facture.CNUFACHETEUR;

            if (!(ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC")))
            {
                bc.DIM_2 = CommonFacturation.getDim2Coquille(ens.CODE, rayonvendeur, facture.FACTSITE1.CODEAGRESSO);
                bc.DIM_4 = facture.FACTSITE1.DIM4;
            }

            if (facture.FACTTYPE.FACTTYPECATEGORIE != null && facture.FACTTYPE.FACTTYPECATEGORIE.LIBELLE == "location gerance")
            {

                isok = vendLigneTvaLG(facture, ref  bc, assoFE, isok, ens, ref seq);
               if (!isok) return false;

               isok = vendLigneTscLG(facture, ref  bc, assoFE, isok, ens, ref seq);
               if (!isok) return false;

               isok = vendLigneTscTaxesLG(facture, ref  bc, assoFE, isok, ens, ref seq);
               if (!isok) return false;

               isok = vendLigneTscPrdLG(facture, ref  bc, assoFE, isok, ens, ref seq);
               if (!isok) return false;

               isok = vendLigneHtLG(facture, ref  bc, assoFE, isok, ens, ref seq);
               if (!isok) return false;

               isok = vendLigneTtcLG(facture, ref  bc, assoFE, isok, ens, ref seq);
               if (!isok) return false;
               return true;
           }

            if (facture.FACTTYPE.FACTTYPECATEGORIE != null && facture.FACTTYPE.IDCATEGORIE == 5 )
            {
                isok = vendLigneTvaMS(facture, ref  bc, assoFE, isok, ens, ref seq);
                if (!isok) return false;

                isok = vendLigneHtMS(facture, ref  bc, assoFE, isok, ens, ref seq);
                if (!isok) return false;

                isok = vendLigneTtcMS(facture, ref  bc, assoFE, isok, ens, ref seq);
                if (!isok) return false;

                return true;
            }

            string compteipprrf = assoFE.COMPTEVENDTVAIPPRF;

            //La ligne TVA
            if (facture.MNTTVA != 0)
            {
                List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
                TOTAUXTVA ligneTva = totauxtva[0];
                if (ligneTva.TAUXTVA == 20)
                {
                    if (assoFE.COMPTEVENDTVA20 == null || assoFE.COMPTEVENDTVA20.Length <= 7)
                    {
                        logger.Info("Compte TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        rapportGereration.Add("Ko : Compte TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                        isok = false;
                        return isok;
                    }
                    bc.ACCOUNT = assoFE.COMPTEVENDTVA20;
                }
                if (ligneTva.TAUXTVA == 10)
                {
                    if (assoFE.COMPTEVENDTVA10 == null || assoFE.COMPTEVENDTVA10.Length <= 7)
                    {
                        logger.Info("Compte TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        rapportGereration.Add("Ko : Compte TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                        isok = false;
                        return isok;
                    }
                    bc.ACCOUNT = assoFE.COMPTEVENDTVA10;
                }
                if (ligneTva.TAUXTVA == 14)
                {
                    if (assoFE.COMPTEVENDTVA14 == null || assoFE.COMPTEVENDTVA14.Length <= 7)
                    {
                        logger.Info("Compte TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        rapportGereration.Add("Ko : Compte TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                        isok = false;
                        return isok;
                    }
                    bc.ACCOUNT = assoFE.COMPTEVENDTVA14;
                }

                if (ligneTva.TAUXTVA == 7)
                {
                    if (assoFE.COMPTEVENDTVA7 == null || assoFE.COMPTEVENDTVA7.Length <= 7)
                    {
                        logger.Info("Compte TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        rapportGereration.Add("Ko : Compte TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                        isok = false;
                        return isok;
                    }
                    bc.ACCOUNT = assoFE.COMPTEVENDTVA7;
                }

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.AMOUNT = -1 * Convert.ToDouble(ligneTva.MNTVA);
                    bc.DC_FLAG = 1;
                }
                else
                {
                    bc.AMOUNT = -1 * Convert.ToDouble(ligneTva.MNTVA);
                    bc.DC_FLAG = -1;
                }

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;

                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);

                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];

                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                }
                if (isok){
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! "+e.ToString());
                        return false;
                    }
                }
            }


            //La ligne HT
            if (facture.MNTHT != 0) {
                if (assoFE.COMPTEVENDEUR == null || assoFE.COMPTEVENDEUR.Length <= 7)
                {
                    logger.Info("Compte client est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte client est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEVENDEUR;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1){
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT);
                    bc.DC_FLAG = 1;
                }else
                {
                    bc.DC_FLAG = -1;
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT);
                }
               
                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                }
                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }
            //Manage Stupid Null Exception 
            var NullExcep = compteipprrf; 
            double mntIpprrf = 0.0;
            //La ligne IPPRRF
            if (NullExcep != null)
            {
                if (compteipprrf != null && compteipprrf.Length > 0 && compteipprrf != compteBidon && compteipprrf != "")
                {
                    if (facture.MNTHT != 0)
                    {
                        bc.ACCOUNT = assoFE.COMPTEVENDTVAIPPRF;

                        if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                        {
                            bc.AMOUNT = Convert.ToDouble(facture.MNTHT) * 0.2;
                            bc.DC_FLAG = -1;
                        }
                        else
                        {
                            bc.DC_FLAG = 1;
                            bc.AMOUNT = Convert.ToDouble(facture.MNTHT) * 0.2;
                        }

                        mntIpprrf = bc.AMOUNT;

                        seq = seq + 1;
                        bc.SEQUENCE_NO = seq;
                        bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                        String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                        bc.ATT_1_ID = att_ids[0];
                        bc.ATT_2_ID = att_ids[1];
                        bc.ATT_3_ID = att_ids[2];
                        bc.ATT_4_ID = att_ids[3];
                        bc.ATT_5_ID = att_ids[4];
                        bc.ATT_6_ID = att_ids[5];
                        bc.ATT_7_ID = att_ids[6];
                        if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                        {
                            bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                        }
                        if (isok)
                        {
                            try
                            {
                                if (!facture.insert_temp(bc, "ACRTRANS"))
                                    return false;
                            }
                            catch (Exception e)
                            {
                                logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                                rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                                return false;
                            }
                        }
                    }
                }
            }
            //La ligne TTC
            if (facture.MNTTTC != 0)
            {
                if (assoFE.COMPTEVENDCLIENT == null || assoFE.COMPTEVENDCLIENT.Length <= 7)
                {
                    logger.Info("Compte vendeur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte vendeur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEVENDCLIENT;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.AMOUNT =  Convert.ToDouble(facture.MNTTTC);
                    bc.DC_FLAG = -1;
                }
                else
                {
                    bc.DC_FLAG = 1;
                    bc.AMOUNT =  Convert.ToDouble(facture.MNTTTC);
                }

                bc.AMOUNT = bc.AMOUNT - mntIpprrf;

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, bc.DIM_1);
                }

                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }

            return isok;
        }

        //Mutisites
        private bool vendLigneTvaMS(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens, ref int seq)
        {
            List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
            TOTAUXTVA ligneTva = totauxtva[0];
            int TAUXTVA = Convert.ToInt32(ligneTva.TAUXTVA);
            if (facture.MNTTVA != 0)
            {
                //List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
                //TOTAUXTVA ligneTva = totauxtva[0];
                if (TAUXTVA == 20)
                {
                    if (assoFE.COMPTEVENDTVA20 == null || assoFE.COMPTEVENDTVA20.Length <= 7)
                    {
                        logger.Info("Compte TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        rapportGereration.Add("Ko : Compte TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                        isok = false;
                        return isok;
                    }
                    bc.ACCOUNT = assoFE.COMPTEVENDTVA20;
                }
                if (TAUXTVA == 10)
                {
                    if (assoFE.COMPTEVENDTVA10 == null || assoFE.COMPTEVENDTVA10.Length <= 7)
                    {
                        logger.Info("Compte TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        rapportGereration.Add("Ko : Compte TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                        isok = false;
                        return isok;
                    }
                    bc.ACCOUNT = assoFE.COMPTEVENDTVA10;
                }
                if (TAUXTVA == 14)
                {
                    if (assoFE.COMPTEVENDTVA14 == null || assoFE.COMPTEVENDTVA14.Length <= 7)
                    {
                        logger.Info("Compte TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        rapportGereration.Add("Ko : Compte TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                        isok = false;
                        return isok;
                    }
                    bc.ACCOUNT = assoFE.COMPTEVENDTVA14;
                }

                if (TAUXTVA == 7)
                {
                    if (assoFE.COMPTEVENDTVA7 == null || assoFE.COMPTEVENDTVA7.Length <= 7)
                    {
                        logger.Info("Compte TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        rapportGereration.Add("Ko : Compte TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                        isok = false;
                        return isok;
                    }
                    bc.ACCOUNT = assoFE.COMPTEVENDTVA7;
                }

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTTVA);
                    bc.DC_FLAG = 1;
                }
                else
                {
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTTVA);
                    bc.DC_FLAG = -1;
                }

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);

                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];

                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                }
                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }
            return isok;
        }

        //Multisites
        private bool vendLigneHtMS(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens, ref int seq)
        {
            string compteipprrf = assoFE.COMPTEVENDTVAIPPRF;
            //La ligne HT
            if (facture.MNTHT != 0)
            {
                if (assoFE.COMPTEVENDEUR == null || assoFE.COMPTEVENDEUR.Length <= 7)
                {
                    logger.Info("Compte client est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte client est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEVENDEUR;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT);
                    bc.DC_FLAG = 1;
                }
                else
                {
                    bc.DC_FLAG = -1;
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT);
                }

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                }
                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }
            double mntIpprrf = 0.0;
            //La ligne IPPRRF
            if (compteipprrf != null && compteipprrf.Length > 0 && compteipprrf != compteBidon)
            {
                if (facture.MNTHT != 0)
                {
                    bc.ACCOUNT = assoFE.COMPTEVENDTVAIPPRF;

                    if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                    {
                        //bc.AMOUNT = Convert.ToDouble(facture.MNTHT) * 0.2;
                        bc.AMOUNT = Convert.ToDouble(facture.MNTTVAPPRF);
                        bc.DC_FLAG = -1;
                    }
                    else
                    {
                        bc.DC_FLAG = 1;
                        //bc.AMOUNT = Convert.ToDouble(facture.MNTHT) * 0.2;
                        bc.AMOUNT = Convert.ToDouble(facture.MNTTVAPPRF);
                    }

                    mntIpprrf = bc.AMOUNT;

                    seq = seq + 1;
                    bc.SEQUENCE_NO = seq;
                    bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                    String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                    bc.ATT_1_ID = att_ids[0];
                    bc.ATT_2_ID = att_ids[1];
                    bc.ATT_3_ID = att_ids[2];
                    bc.ATT_4_ID = att_ids[3];
                    bc.ATT_5_ID = att_ids[4];
                    bc.ATT_6_ID = att_ids[5];
                    bc.ATT_7_ID = att_ids[6];
                    if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                    {
                        bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                    }
                    if (isok)
                    {
                        try
                        {
                            if (!facture.insert_temp(bc, "ACRTRANS"))
                                return false;
                        }
                        catch (Exception e)
                        {
                            logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                            rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                            return false;
                        }
                    }
                }
            }
            return isok;
        }

        //Multisites
        private bool vendLigneTtcMS(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens, ref int seq)
        {
            //La ligne TTC
            if (facture.MNTTTC != 0)
            {
                if (assoFE.COMPTEVENDCLIENT == null || assoFE.COMPTEVENDCLIENT.Length <= 7)
                {
                    logger.Info("Compte vendeur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte vendeur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEVENDCLIENT;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.AMOUNT = Convert.ToDouble(facture.MNTTTC);
                    bc.DC_FLAG = -1;
                }
                else
                {
                    bc.DC_FLAG = 1;
                    bc.AMOUNT = Convert.ToDouble(facture.MNTTTC);
                }
                string compteipprrf = assoFE.COMPTEVENDTVAIPPRF;
                double mntIpprrf = 0.0;
                //if (compteipprrf != null && compteipprrf.Length > 0 && compteipprrf != compteBidon && facture.MNTHT != 0)
                //{
                //    mntIpprrf = Convert.ToDouble(facture.MNTHT) * 0.2;
                //}

                //bc.AMOUNT = bc.AMOUNT - mntIpprrf;

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, bc.DIM_1);
                }

                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }
            return isok;
        }

        //location gerance
        private bool vendLigneTvaLG(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens, ref int seq)
        {
            if (facture.MNTTVA != 0)
            {
                List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
                TOTAUXTVA ligneTva = totauxtva[0];
                if (ligneTva.TAUXTVA == 20)
                {
                    if (assoFE.COMPTEVENDTVA20 == null || assoFE.COMPTEVENDTVA20.Length <= 7)
                    {
                        logger.Info("Compte TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        rapportGereration.Add("Ko : Compte TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                        isok = false;
                        return isok;
                    }
                    bc.ACCOUNT = assoFE.COMPTEVENDTVA20;
                }
                if (ligneTva.TAUXTVA == 10)
                {
                    if (assoFE.COMPTEVENDTVA10 == null || assoFE.COMPTEVENDTVA10.Length <= 7)
                    {
                        logger.Info("Compte TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        rapportGereration.Add("Ko : Compte TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                        isok = false;
                        return isok;
                    }
                    bc.ACCOUNT = assoFE.COMPTEVENDTVA10;
                }
                if (ligneTva.TAUXTVA == 14)
                {
                    if (assoFE.COMPTEVENDTVA14 == null || assoFE.COMPTEVENDTVA14.Length <= 7)
                    {
                        logger.Info("Compte TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        rapportGereration.Add("Ko : Compte TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                        isok = false;
                        return isok;
                    }
                    bc.ACCOUNT = assoFE.COMPTEVENDTVA14;
                }

                if (ligneTva.TAUXTVA == 7)
                {
                    if (assoFE.COMPTEVENDTVA7 == null || assoFE.COMPTEVENDTVA7.Length <= 7)
                    {
                        logger.Info("Compte TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        rapportGereration.Add("Ko : Compte TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                        isok = false;
                        return isok;
                    }
                    bc.ACCOUNT = assoFE.COMPTEVENDTVA7;
                }

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.AMOUNT = -1 * Convert.ToDouble(ligneTva.MNTVA);
                    bc.DC_FLAG = 1;
                }
                else
                {
                    bc.AMOUNT = -1 * Convert.ToDouble(ligneTva.MNTVA);
                    bc.DC_FLAG = -1;
                }

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);

                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];

                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                }
                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }
            return isok;
        }

        //location gerance
        private bool vendLigneTscLG(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens,ref int seq)
        {
            if (facture.MNTTVA != 0)
            {
               // List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
                //TOTAUXTVA ligneTva = totauxtva[0];

                if (assoFE.COMPTEVENDTSC == null || assoFE.COMPTEVENDTSC.Length <= 7)
                {
                    logger.Info("Compte TSC est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte TSC est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEVENDTSC;

                double dtauxtsc = double.Parse(tauxtsc, new NumberFormatInfo() { NumberDecimalSeparator = "." });
                double mnttsc = Convert.ToDouble(facture.MNTHT) * (dtauxtsc * 0.01);

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.AMOUNT =  Convert.ToDouble(mnttsc);
                    bc.DC_FLAG = -1;
                }
                else
                {
                    bc.AMOUNT = Convert.ToDouble(mnttsc);
                    bc.DC_FLAG = 1;
                }

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);

                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];

                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                }
                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }
            return isok;
        }

        //location gerance
        private bool vendLigneTscTaxesLG(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens,ref int seq)
        {
            if (facture.MNTHT != 0)
            {
                // List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
                //TOTAUXTVA ligneTva = totauxtva[0];

                if (assoFE.COMPTEVENDTSCTAX == null || assoFE.COMPTEVENDTSCTAX.Length <= 7)
                {
                    logger.Info("Compte TSC TAXES est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte TSC TAXES est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEVENDTSCTAX;

                double dtauxtsc = double.Parse(tauxtsc, new NumberFormatInfo() { NumberDecimalSeparator = "." });
                double mnttsc = Convert.ToDouble(facture.MNTHT) * (dtauxtsc * 0.01);

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.AMOUNT = -1 * Convert.ToDouble(mnttsc);
                    bc.DC_FLAG = 1;
                }
                else
                {
                    bc.AMOUNT = -1 * Convert.ToDouble(mnttsc);
                    bc.DC_FLAG = -1;
                }

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);

                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];

                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                }
                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }
            return isok;
        }

        //location gerance
        private bool vendLigneTscPrdLG(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens,ref int seq)
        {
            if (facture.MNTHT != 0)
            {
                // List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
                //TOTAUXTVA ligneTva = totauxtva[0];

                if (assoFE.COMPTEVENDEUR == null || assoFE.COMPTEVENDEUR.Length <= 7)
                {
                    logger.Info("Compte client est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte client est non défini !!! Merci de le saisir sur l'ecran des chapites !!!");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEVENDEUR;

                double dtauxtsc = double.Parse(tauxtsc, new NumberFormatInfo() { NumberDecimalSeparator = "." });
                double mnttsc = Convert.ToDouble(facture.MNTHT) * (dtauxtsc * 0.01);

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.AMOUNT = -1 * Convert.ToDouble(mnttsc);
                    bc.DC_FLAG = 1;
                }
                else
                {
                    bc.AMOUNT = -1 * Convert.ToDouble(mnttsc);
                    bc.DC_FLAG = -1;
                }

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;

                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);

                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];

                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                }
                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }
            return isok;
        }

        //location gerance
        private bool vendLigneHtLG(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens, ref int seq)
        {
            string compteipprrf = assoFE.COMPTEVENDTVAIPPRF;
            //La ligne HT
            if (facture.MNTHT != 0)
            {
                if (assoFE.COMPTEVENDEUR == null || assoFE.COMPTEVENDEUR.Length <= 7)
                {
                    logger.Info("Compte client est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte client est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEVENDEUR;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT);
                    bc.DC_FLAG = 1;
                }
                else
                {
                    bc.DC_FLAG = -1;
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT);
                }

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                }
                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }
            double mntIpprrf = 0.0;
            //La ligne IPPRRF
            if (compteipprrf != null && compteipprrf.Length > 0 && compteipprrf != compteBidon)
            {
                if (facture.MNTHT != 0)
                {
                    bc.ACCOUNT = assoFE.COMPTEVENDTVAIPPRF;

                    if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                    {
                        bc.AMOUNT = Convert.ToDouble(facture.MNTHT) * 0.2;
                        bc.DC_FLAG = -1;
                    }
                    else
                    {
                        bc.DC_FLAG = 1;
                        bc.AMOUNT = Convert.ToDouble(facture.MNTHT) * 0.2;
                    }

                    mntIpprrf = bc.AMOUNT;

                    seq = seq + 1;
                    bc.SEQUENCE_NO = seq;
                    bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                    String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                    bc.ATT_1_ID = att_ids[0];
                    bc.ATT_2_ID = att_ids[1];
                    bc.ATT_3_ID = att_ids[2];
                    bc.ATT_4_ID = att_ids[3];
                    bc.ATT_5_ID = att_ids[4];
                    bc.ATT_6_ID = att_ids[5];
                    bc.ATT_7_ID = att_ids[6];
                    if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                    {
                        bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE1.CODEAGRESSO);
                    }
                    if (isok)
                    {
                        try
                        {
                            if (!facture.insert_temp(bc, "ACRTRANS"))
                                return false;
                        }
                        catch (Exception e)
                        {
                            logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                            rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                            return false;
                        }
                    }
                }
            }
            return isok;
        }

        //Location gérance
        private bool vendLigneTtcLG(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens, ref int seq)
        {
            //La ligne TTC
            if (facture.MNTTTC != 0)
            {
                if (assoFE.COMPTEVENDCLIENT == null || assoFE.COMPTEVENDCLIENT.Length <= 7)
                {
                    logger.Info("Compte vendeur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte vendeur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEVENDCLIENT;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.AMOUNT = Convert.ToDouble(facture.MNTTTC);
                    bc.DC_FLAG = -1;
                }
                else
                {
                    bc.DC_FLAG = 1;
                    bc.AMOUNT = Convert.ToDouble(facture.MNTTTC);
                }
                string compteipprrf = assoFE.COMPTEVENDTVAIPPRF;
                double mntIpprrf = 0.0;
                if (compteipprrf != null && compteipprrf.Length > 0 && compteipprrf != compteBidon && facture.MNTHT != 0)
                {
                    mntIpprrf = Convert.ToDouble(facture.MNTHT) * 0.2;
                }
                
                bc.AMOUNT = bc.AMOUNT - mntIpprrf;

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, bc.DIM_1);
                }

                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }
            return isok;
        }

        public bool inserAcheteur(int id, int seq, long nolot)
        {
            FACTFACTURE facture = new FACTFACTURE();
            facture = facture.getOne(id);

            AGR bc = new AGR();
            FACTENSEIGNE ens = new FACTENSEIGNE();
            ens = ens.getOne(facture.FACTSITE.IDENSEIGNE.Value); //l acheteur

            bc.IDFACTURE = facture.IDFACTURE;
            bc.NOLOT = (int)nolot;
            string comptetva = "";

            if (ens.CODE.Equals("AC"))
            {
                bc.ATT_1_ID = "0I";
                bc.ATT_2_ID = "0O";
                bc.ATT_4_ID = "0J";
            }
            else
            {
                bc.ATT_1_ID = "0P";
                bc.ATT_2_ID = "0O";
                bc.ATT_4_ID = "0G";
            }

            //bc.DIM_1 = facture.FACTSITE.CODEAGRESSO;//N° site
            bc.DIM_1 = facture.FACTSITE.DIM1;

            //bc.DIM_4 = (facture.RAYON.HasValue ? facture.RAYON.Value.ToString() : "");
            string rayonacheteur = (facture.RAYONACHETEUR.HasValue ? facture.RAYONACHETEUR.Value.ToString() : " ");
            bc.DIM_4 = rayonacheteur;//(facture.RAYONACHETEUR.HasValue ? facture.RAYONACHETEUR.Value.ToString() : " ");


            //construire libelle facture
            FACTENSEIGNE ens2 = new FACTENSEIGNE();
            ens2 = ens2.getOne(facture.FACTSITE1.IDENSEIGNE.Value); //le vendeur
            bc.DESCRIPTION = ens2.LIBELLEENSEIGNE + "-" + ens.LIBELLEENSEIGNE + "-" + facture.REFERENCEFACT + " (" + facture.LIBELLEDEMANDE + ")";
            

            //bc.DESCRIPTION = facture.LIBELLEDEMANDE; //a construire
            bc.CLIENT = ens.CODE;
            int anneeFiscal = -1;
            if (facture.ANNEEPRESTATION != null && facture.ANNEEPRESTATION.Length > 0)
            {
                bc.FISCAL_YEAR = Int32.Parse(facture.ANNEEPRESTATION);
                anneeFiscal = bc.FISCAL_YEAR;
            }
            else
            {
                bc.FISCAL_YEAR = Int32.Parse(facture.DATECREATION.Value.ToString("yyyy"));
            }

            bc.LAST_UPDATE = DateTime.Now;

            bc.PERIOD = CommonFacturation.periodeOuverte(bc.CLIENT); //gePeriodeOuverte(bc.CLIENT, anneeFiscal); // Int32.Parse(facture.DATECREATION.Value.ToString("yyyyMM"));
            if (bc.PERIOD == -1) {
                logger.Info("Problème communication avec Agresso ou aucune période ouverte pour la société :" + ens.CODE);
                rapportGereration.Add("Ko : Problème communication avec Agresso ou aucune période ouverte pour la société :" + ens.CODE);
                return false;
            }


            bc.TRANS_DATE = facture.DATECREATION.Value;
            bc.VOUCHER_DATE = DateTime.Now;

            bc.VOUCHER_TYPE = "GF";

            Int64 counter = 0;
            int trans_id = 0;
            if (!getListInfosClient(ens.CODE, out counter, out trans_id))
            {
                logger.Info("Ko: Probleme recuperation infos client :" + ens.CODE + " depuis Agresso, merci de vérifier la communication avec Agresso");
                rapportGereration.Add("Ko: Probleme recuperation infos client :" + ens.CODE + " depuis Agresso, merci de vérifier la communication avec Agresso");
                return false;
            }
            bc.VOUCHER_NO = counter;
            bc.TRANS_ID = trans_id;
            bc.EXT_INV_REF = facture.REFERENCEFACT;

            if (counter == 0)
            {
                logger.Info("Merci de parametrer le voucher_no sur Agresso pour la société :" + ens.CODE);
                rapportGereration.Add("Ko : Merci de parametrer le voucher_no sur Agresso pour la société :" + ens.CODE);
                return false;
            }

            FACTASSOGFACTTYPEENSEIGNE assoFE = ens.getOneAssoFactEns(facture.IDFACTTYPE.Value, ens.IDENSEIGNE);
            if (assoFE == null)
            {
                logger.Info("Merci de parametrer les comptes de comptabilisation !!!");
                rapportGereration.Add("Ko : Merci de parametrer les comptes de comptabilisation !!!");
                return false;
            }
            bool isok = true;
            /*****************************************Début Vendeur*******************************************************/
            bc.APAR_ID = facture.CNUFVENDEUR;


            if (!(ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC")))
            {
                bc.DIM_2 = CommonFacturation.getDim2Coquille(ens.CODE, rayonacheteur, facture.FACTSITE.CODEAGRESSO);
                bc.DIM_4 = facture.FACTSITE.DIM4;
            }

            if (facture.FACTTYPE.FACTTYPECATEGORIE != null && facture.FACTTYPE.FACTTYPECATEGORIE.LIBELLE == "location gerance")
            {
                isok = achLigneTvaLg(facture, ref  bc, assoFE, isok, ens, ref seq);
                if (!isok) return false;

                isok = achLigneHtLg(facture, ref  bc, assoFE, isok, ens, ref seq);
                if (!isok) return false;

                isok = achLigneTtcLg(facture, ref  bc, assoFE, isok, ens, ref seq);
                if (!isok) return false;

                return true;
            }
            
            if (facture.FACTTYPE.FACTTYPECATEGORIE != null && facture.FACTTYPE.IDCATEGORIE == 5 )
            {
                try
                {
                    isok = achLigneTvaMS(facture, ref  bc, assoFE, isok, ens, ref seq);
                }
                catch (Exception e)
                {
                    logger.Info("Probleme dans achLigneTvaMS " + e.ToString());
                    rapportGereration.Add("Ko : Probleme dans achLigneTvaMS " + e.ToString());
                    return false;
                }

                if (!isok) return false;

                try
                {
                    isok = achLigneHtMS(facture, ref  bc, assoFE, isok, ens, ref seq);
                }
                catch (Exception e)
                {
                    logger.Info("Probleme dans achLigneHtMS "+e.ToString());
                    rapportGereration.Add("Ko : Probleme dans achLigneHtMS "+e.ToString());
                    return false;
                }

                if (!isok) return false;

                try
                {
                    isok = achLigneTtcMS(facture, ref  bc, assoFE, isok, ens, ref seq);
                }
                catch (Exception e)
                {
                    logger.Info("Probleme dans achLigneTtcMS " + e.ToString());
                    rapportGereration.Add("Ko : Probleme dans achLigneTtcMS " + e.ToString());
                    return false;
                }
                if (!isok) return false;

                return true;
          }

            string compteIpprrf = assoFE.COMPTEACHETTVAIPPRF;
            double mntipprrf = 0.0;
            //La ligne TVA
            if (facture.MNTTVA != 0)
            {
                List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
                TOTAUXTVA ligneTva = totauxtva[0];
                if (ligneTva.TAUXTVA == 20)
                {
                    //if (assoFE.COMPTEACHETTVA20 == null || assoFE.COMPTEACHETTVA20.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA20;
                }
                if (ligneTva.TAUXTVA == 10)
                {
                    //if (assoFE.COMPTEACHETTVA10 == null || assoFE.COMPTEACHETTVA10.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA10;
                }
                if (ligneTva.TAUXTVA == 14)
                {
                    //if (assoFE.COMPTEACHETTVA14 == null || assoFE.COMPTEACHETTVA14.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA14;
                }
                if (ligneTva.TAUXTVA == 7)
                {
                    //if (assoFE.COMPTEACHETTVA7 == null || assoFE.COMPTEACHETTVA7.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA7;
                }


                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.DC_FLAG = -1;
                    //montant = ((Convert.ToSingle(row["gfamtht"]) * Convert.ToInt32(intM.TVA1)) / 100) * p.Prorata_Tva(site, intM.Compte, intM.Date_piec.ToString());
                    float prtrta = CommonFacturation.Prorata_Tva(bc.CLIENT, bc.ACCOUNT,bc.PERIOD+"01");// facture.DATECREATION.Value.ToString("dd/MM/yyyy"));
                    if (prtrta == -1)
                    {
                        logger.Info("Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        rapportGereration.Add("Ko : Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        return false;
                    }
                    bc.AMOUNT = ((Convert.ToDouble(facture.MNTHT) * (double)ligneTva.TAUXTVA) / 100) * prtrta;
                   //bc.AMOUNT = Convert.ToDouble(ligneTva.MNTVA);
                }
                else
                {
                    bc.DC_FLAG = 1;
                    //bc.AMOUNT = Convert.ToDouble(ligneTva.MNTVA);
                    float prtrta = CommonFacturation.Prorata_Tva(bc.CLIENT, bc.ACCOUNT,bc.PERIOD+"01");// facture.DATECREATION.Value.ToString("dd/MM/yyyy"));
                    if (prtrta == -1)
                    {
                        logger.Info("Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        rapportGereration.Add("Ko : Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        return false;
                    }
                    bc.AMOUNT = ((Convert.ToDouble(facture.MNTHT) * (double)ligneTva.TAUXTVA) / 100) * prtrta;
                }
                seq = seq + 1;
                bc.SEQUENCE_NO = seq;

                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                }
                bc.DIM_1 = facture.FACTSITE.DIM1;//N° site
                if (ens.CODE.Equals("CF")) { 
                    bc.DIM_1="01";
                }

                if (isok)
                {
                    if (bc.AMOUNT != 0) {
                        try
                        {
                            if (!facture.insert_temp(bc, "ACRTRANS"))
                                return false;
                        }
                        catch (Exception e)
                        {
                            logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                            rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                            return false;
                        }
                    }
                }
                comptetva = bc.ACCOUNT;
            }


            //La ligne HT            
            if (facture.MNTHT != 0)
            {
                if (assoFE.COMPTEACHETEUR == null || assoFE.COMPTEACHETEUR.Length <= 7)
                {
                    logger.Info("Compte acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEACHETEUR;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.DC_FLAG = -1;
                    //bc.AMOUNT = Convert.ToDouble(facture.MNTHT);
                }
                else
                {
                    bc.DC_FLAG = 1;
                    //bc.AMOUNT = Convert.ToDouble(facture.MNTHT);
                }

                //montant = Convert.ToSingle(row["gfamtht"]) * p.Prorata(site, p.Compte_FG_TVA(site, intM.TVA1), intM.Date_piec.ToString());
                if (comptetva != null && !comptetva.Equals(" ") && comptetva.Length > 0)
                {
                    float prtrtah = CommonFacturation.Prorata(bc.CLIENT, comptetva, bc.PERIOD + "01");// facture.DATECREATION.Value.ToString("dd/MM/yyyy"));
                    if (prtrtah == -1)
                    {
                        logger.Info("Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        rapportGereration.Add("Ko : Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        return false;
                    }
                    bc.AMOUNT = Convert.ToDouble(facture.MNTHT) * prtrtah;
                }
                else {
                    bc.AMOUNT = Convert.ToDouble(facture.MNTHT);
                }
           
                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                }
                bc.DIM_1 = facture.FACTSITE.DIM1;//N° site
                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }

            //La ligne IPPRRF
            if (compteIpprrf != null && compteIpprrf.Length > 0 && compteIpprrf != compteBidon)
            {
                if (facture.MNTHT != 0)
                {
                    bc.ACCOUNT = assoFE.COMPTEACHETTVAIPPRF;

                    if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                    {
                        bc.DC_FLAG = 1;
                        bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT) * 0.2;
                    }
                    else
                    {
                        bc.DC_FLAG = -1;
                        bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT) * 0.2;
                    }
                    mntipprrf = bc.AMOUNT;

                    seq = seq + 1;
                    bc.SEQUENCE_NO = seq;
                    bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                    String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                    bc.ATT_1_ID = att_ids[0];
                    bc.ATT_2_ID = att_ids[1];
                    bc.ATT_3_ID = att_ids[2];
                    bc.ATT_4_ID = att_ids[3];
                    bc.ATT_5_ID = att_ids[4];
                    bc.ATT_6_ID = att_ids[5];
                    bc.ATT_7_ID = att_ids[6];
                    if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                    {
                        bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                    }
                    bc.DIM_1 = facture.FACTSITE.DIM1;//N° site
                    if (ens.CODE.Equals("CF"))
                    {
                        bc.DIM_1 = "01";
                    }

                    if (isok)
                    {
                        try
                        {
                            if (!facture.insert_temp(bc, "ACRTRANS"))
                                return false;
                        }
                        catch (Exception e)
                        {
                            logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                            rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                            return false;
                        }
                    }
                }
            }
            //La ligne TTC
            if (facture.MNTTTC != 0)
            {
                if (assoFE.COMPTEACHETFRS == null || assoFE.COMPTEACHETFRS.Length <= 7)
                {
                    logger.Info("Compte fournisseur de l'acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte fournisseur de l'acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEACHETFRS;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.DC_FLAG = 1;
                    bc.AMOUNT =-1 * Convert.ToDouble(facture.MNTTTC);
                }
                else
                {
                    bc.DC_FLAG = -1;
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTTTC);
                }
                
                bc.AMOUNT = bc.AMOUNT - mntipprrf;

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                }
                bc.DIM_1 = facture.FACTSITE.DIM1;//N° site
                if (ens.CODE.Equals("CF"))
                {
                    bc.DIM_1 = "01";
                }

                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }



            return isok;
        }

        //Mutisites acheteur
        private bool achLigneTvaMS(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens, ref int seq)
        {
            List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
            TOTAUXTVA ligneTva = totauxtva[0];
            int TAUXTVA = Convert.ToInt32(ligneTva.TAUXTVA);
            
            if (facture.MNTTVA != 0)
            {
                //List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
                //TOTAUXTVA ligneTva = totauxtva[0];
                if (TAUXTVA == 20)
                {
                    //if (assoFE.COMPTEACHETTVA20 == null || assoFE.COMPTEACHETTVA20.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA20;
                }
                if (TAUXTVA == 10)
                {
                    //if (assoFE.COMPTEACHETTVA10 == null || assoFE.COMPTEACHETTVA10.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA10;
                }
                if (TAUXTVA == 14)
                {
                    //if (assoFE.COMPTEACHETTVA14 == null || assoFE.COMPTEACHETTVA14.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA14;
                }
                if (TAUXTVA == 7)
                {
                    //if (assoFE.COMPTEACHETTVA7 == null || assoFE.COMPTEACHETTVA7.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA7;
                }


                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.DC_FLAG = -1;
                    //montant = ((Convert.ToSingle(row["gfamtht"]) * Convert.ToInt32(intM.TVA1)) / 100) * p.Prorata_Tva(site, intM.Compte, intM.Date_piec.ToString());
                    float prtrta = CommonFacturation.Prorata_Tva(bc.CLIENT, bc.ACCOUNT, bc.PERIOD+"01");// facture.DATECREATION.Value.ToString("dd/MM/yyyy"));
                    if (prtrta == -1)
                    {
                        logger.Info("Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        rapportGereration.Add("Ko : Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        return false;
                    }
                    bc.AMOUNT = Convert.ToDouble(facture.MNTTVA) * prtrta;
                    //bc.AMOUNT = Convert.ToDouble(ligneTva.MNTVA);
                }
                else
                {
                    bc.DC_FLAG = 1;
                    //bc.AMOUNT = Convert.ToDouble(ligneTva.MNTVA);
                    float prtrta = CommonFacturation.Prorata_Tva(bc.CLIENT, bc.ACCOUNT, bc.PERIOD+"01");// facture.DATECREATION.Value.ToString("dd/MM/yyyy"));
                    if (prtrta == -1)
                    {
                        logger.Info("Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        rapportGereration.Add("Ko : Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        return false;
                    }
                    bc.AMOUNT = Convert.ToDouble(facture.MNTTVA) * prtrta;
                }
                seq = seq + 1;
                bc.SEQUENCE_NO = seq;

                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                }
                bc.DIM_1 = facture.FACTSITE.DIM1;//N° site
                if (ens.CODE.Equals("CF"))
                {
                    bc.DIM_1 = "01";
                }

                if (isok)
                {
                    if (bc.AMOUNT != 0)
                    {
                        try
                        {
                            if (!facture.insert_temp(bc, "ACRTRANS"))
                                return false;
                        }
                        catch (Exception e)
                        {
                            logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                            rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                            return false;
                        }
                    }
                }
            }
            return isok;
        }

        //Multisites
        private bool achLigneHtMS(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens, ref int seq)
        {
            

            string compteIpprrf = assoFE.COMPTEACHETTVAIPPRF;
            List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
            TOTAUXTVA ligneTva = totauxtva[0];
            int TAUXTVA = Convert.ToInt32(ligneTva.TAUXTVA);
            int idsite = -1;
            //if (compteIpprrf != null && compteIpprrf.Length > 0 && compteIpprrf != compteBidon)
            //    TAUXTVA = Convert.ToInt32(ligneTva.TAUXTVAIPPRF);
                

                foreach (FACTFACTUREDETAIL item in facture.FACTFACTUREDETAIL)
                {
                FACTSITE site = new FACTSITE();

                idsite = (item.LIBELLEPRODUIT.Length > 0 ? int.Parse(item.LIBELLEPRODUIT.Substring(0, item.LIBELLEPRODUIT.IndexOf("-"))) : -1);

                if (idsite == -1)
                    continue;
                site = site.getOne(idsite);
                
                double mntipprrf = 0.0;
                string comptetva = "";
                //La ligne HT            
                if (item.MNTHT != 0)
                {
                    if (TAUXTVA == 20)
                    {
                        comptetva = assoFE.COMPTEACHETTVA20;
                    }
                    if (TAUXTVA == 10)
                    {
                        comptetva = assoFE.COMPTEACHETTVA10;
                    }
                    if (TAUXTVA == 14)
                    {
                        comptetva = assoFE.COMPTEACHETTVA14;
                    }
                    if (TAUXTVA == 7)
                    {
                        comptetva = assoFE.COMPTEACHETTVA7;
                    }

                    if (assoFE.COMPTEACHETEUR == null || assoFE.COMPTEACHETEUR.Length <= 7)
                    {
                        logger.Info("Compte acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        rapportGereration.Add("Ko : Compte acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                        isok = false;
                        return isok;
                    }
                    bc.ACCOUNT = assoFE.COMPTEACHETEUR;

                    if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                    {
                        bc.DC_FLAG = -1;
                        //bc.AMOUNT = Convert.ToDouble(facture.MNTHT);
                    }
                    else
                    {
                        bc.DC_FLAG = 1;
                        //bc.AMOUNT = Convert.ToDouble(facture.MNTHT);
                    }

                    double mntht = Convert.ToDouble(item.MNTHT);

                    //montant = Convert.ToSingle(row["gfamtht"]) * p.Prorata(site, p.Compte_FG_TVA(site, intM.TVA1), intM.Date_piec.ToString());
                    if (comptetva != null && !comptetva.Equals(" ") && comptetva.Length > 0)
                    {
                        float prtrtah = CommonFacturation.Prorata(bc.CLIENT, comptetva, bc.PERIOD + "01");// facture.DATECREATION.Value.ToString("dd/MM/yyyy"));
                        if (prtrtah == -1)
                        {
                            logger.Info("Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                            rapportGereration.Add("Ko : Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                            return false;
                        }
                        bc.AMOUNT = mntht * prtrtah;
                    }
                    else
                    {
                        bc.AMOUNT = mntht;
                    }

                    seq = seq + 1;
                    bc.SEQUENCE_NO = seq;
                    bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                    String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                    bc.ATT_1_ID = att_ids[0];
                    bc.ATT_2_ID = att_ids[1];
                    bc.ATT_3_ID = att_ids[2];
                    bc.ATT_4_ID = att_ids[3];
                    bc.ATT_5_ID = att_ids[4];
                    bc.ATT_6_ID = att_ids[5];
                    bc.ATT_7_ID = att_ids[6];
                    if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                    {
                        bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, site.CODEAGRESSO);// facture.FACTSITE.CODEAGRESSO
                    }
                    bc.DIM_1 = site.DIM1;
                    bc.DIM_4 = item.REFERENCEPRODUIT; //multisites referenceproduit est le rayon

                    if (isok)
                    {
                        try
                        {
                            if (!facture.insert_temp(bc, "ACRTRANS"))
                                return false;
                        }
                        catch (Exception e)
                        {
                            logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                            rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                            return false;
                        }
                    }
                }

                //La ligne IPPRRF
                if (compteIpprrf != null && compteIpprrf.Length > 0 && compteIpprrf != compteBidon)
                {
                    if (item.MNTHT != 0)
                    {
                        bc.ACCOUNT = assoFE.COMPTEACHETTVAIPPRF;

                        if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                        {
                            bc.DC_FLAG = 1;
                            //bc.AMOUNT = -1 * Convert.ToDouble(item.MNTHT) * 0.2;
                            bc.AMOUNT = -1 * Convert.ToDouble(item.MNTTVAIPPRF);
                        }
                        else
                        {
                            bc.DC_FLAG = -1;
                            //bc.AMOUNT = -1 * Convert.ToDouble(item.MNTHT) * 0.2;
                            bc.AMOUNT = -1 * Convert.ToDouble(item.MNTTVAIPPRF);
                        }
                        mntipprrf = bc.AMOUNT;

                        seq = seq + 1;
                        bc.SEQUENCE_NO = seq;
                        bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                        String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                        bc.ATT_1_ID = att_ids[0];
                        bc.ATT_2_ID = att_ids[1];
                        bc.ATT_3_ID = att_ids[2];
                        bc.ATT_4_ID = att_ids[3];
                        bc.ATT_5_ID = att_ids[4];
                        bc.ATT_6_ID = att_ids[5];
                        bc.ATT_7_ID = att_ids[6];
                        if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                        {
                            bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, site.CODEAGRESSO);// facture.FACTSITE.CODEAGRESSO
                        }

                        bc.DIM_1 = site.DIM1;
                        bc.DIM_4 = item.REFERENCEPRODUIT;

                        if (isok)
                        {
                            try
                            {
                                if (!facture.insert_temp(bc, "ACRTRANS"))
                                    return false;
                            }
                            catch (Exception e)
                            {
                                logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                                rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                                return false;
                            }
                        }
                    }
                }
            }
            return isok;
        }

        //Multisites
        private bool achLigneTtcMS(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens, ref int seq)
        {
            //La ligne TTC
            if (facture.MNTTTC != 0)
            {
                if (assoFE.COMPTEACHETFRS == null || assoFE.COMPTEACHETFRS.Length <= 7)
                {
                    logger.Info("Compte fournisseur de l'acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte fournisseur de l'acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEACHETFRS;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.DC_FLAG = 1;
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTTTC);
                }
                else
                {
                    bc.DC_FLAG = -1;
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTTTC);
                }
                string compteIpprrf = assoFE.COMPTEACHETTVAIPPRF;
                double mntipprrf = 0.0;

                //Recuperation ttc depuis la facture 

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                }
                bc.DIM_1 = facture.FACTSITE.DIM1;//N° site
                if (ens.CODE.Equals("CF"))
                {
                    bc.DIM_1 = "01";
                }
                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }
            return isok;
        }




        //location gerance
        private bool achLigneTvaLg(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens,ref int seq)
        {
            if (facture.MNTTVA != 0)
            {
                List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
                TOTAUXTVA ligneTva = totauxtva[0];
                if (ligneTva.TAUXTVA == 20)
                {
                    //if (assoFE.COMPTEACHETTVA20 == null || assoFE.COMPTEACHETTVA20.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA20 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA20;
                }
                if (ligneTva.TAUXTVA == 10)
                {
                    //if (assoFE.COMPTEACHETTVA10 == null || assoFE.COMPTEACHETTVA10.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA10 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA10;
                }
                if (ligneTva.TAUXTVA == 14)
                {
                    //if (assoFE.COMPTEACHETTVA14 == null || assoFE.COMPTEACHETTVA14.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA14 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA14;
                }
                if (ligneTva.TAUXTVA == 7)
                {
                    //if (assoFE.COMPTEACHETTVA7 == null || assoFE.COMPTEACHETTVA7.Length <= 7)
                    //{
                    //    logger.Info("Compte acheteur TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    rapportGereration.Add("Ko : Compte acheteur TVA7 est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    //    isok = false;
                    //    return isok;
                    //}
                    bc.ACCOUNT = assoFE.COMPTEACHETTVA7;
                }


                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.DC_FLAG = -1;
                    //montant = ((Convert.ToSingle(row["gfamtht"]) * Convert.ToInt32(intM.TVA1)) / 100) * p.Prorata_Tva(site, intM.Compte, intM.Date_piec.ToString());
                    float prtrta = CommonFacturation.Prorata_Tva(bc.CLIENT, bc.ACCOUNT, bc.PERIOD+"01");// facture.DATECREATION.Value.ToString("dd/MM/yyyy"));
                    if (prtrta == -1)
                    {
                        logger.Info("Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        rapportGereration.Add("Ko : Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        return false;
                    }
                    bc.AMOUNT = Convert.ToDouble(facture.MNTTVA) * prtrta;
                   //bc.AMOUNT = Convert.ToDouble(ligneTva.MNTVA);
                }
                else
                {
                    bc.DC_FLAG = 1;
                    //bc.AMOUNT = Convert.ToDouble(ligneTva.MNTVA);
                    float prtrta = CommonFacturation.Prorata_Tva(bc.CLIENT, bc.ACCOUNT, bc.PERIOD + "01");// facture.DATECREATION.Value.ToString("dd/MM/yyyy"));
                    if (prtrta == -1)
                    {
                        logger.Info("Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        rapportGereration.Add("Ko : Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        return false;
                    }
                    bc.AMOUNT = Convert.ToDouble(facture.MNTTVA) * prtrta;
                }
                seq = seq + 1;
                bc.SEQUENCE_NO = seq;

                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                }
                bc.DIM_1 = facture.FACTSITE.DIM1;//N° site
                if (ens.CODE.Equals("CF"))
                {
                    bc.DIM_1 = "01";
                }
                
                if (isok)
                {
                    if (bc.AMOUNT != 0) {
                        try
                        {
                            if (!facture.insert_temp(bc, "ACRTRANS"))
                                return false;
                        }
                        catch (Exception e)
                        {
                            logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                            rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                            return false;
                        }
                    }
                }
            }
            return isok;
        }

        //location gerance
        private bool achLigneHtLg(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens, ref int seq)
        {

            string compteIpprrf = assoFE.COMPTEACHETTVAIPPRF;
            double mntipprrf = 0.0;
            string comptetva = "";
            //La ligne HT            
            if (facture.MNTHT != 0)
            {
                List<TOTAUXTVA> totauxtva = facture.calcultotauxtv(facture.IDFACTURE);
                TOTAUXTVA ligneTva = totauxtva[0];
                if (ligneTva.TAUXTVA == 20)
                {
                    comptetva = assoFE.COMPTEACHETTVA20;
                }
                if (ligneTva.TAUXTVA == 10)
                {
                    comptetva = assoFE.COMPTEACHETTVA10;
                }
                if (ligneTva.TAUXTVA == 14)
                {
                    comptetva = assoFE.COMPTEACHETTVA14;
                }
                if (ligneTva.TAUXTVA == 7)
                {
                    comptetva = assoFE.COMPTEACHETTVA7;
                }

                if (assoFE.COMPTEACHETEUR == null || assoFE.COMPTEACHETEUR.Length <= 7)
                {
                    logger.Info("Compte acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEACHETEUR;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.DC_FLAG = -1;
                    //bc.AMOUNT = Convert.ToDouble(facture.MNTHT);
                }
                else
                {
                    bc.DC_FLAG = 1;
                    //bc.AMOUNT = Convert.ToDouble(facture.MNTHT);
                }

                double dtauxtsc = double.Parse(tauxtsc, new NumberFormatInfo() { NumberDecimalSeparator = "." });
                double mnttsc = Convert.ToDouble(facture.MNTHT) * (dtauxtsc * 0.01);
                double mntht = Convert.ToDouble(facture.MNTHT) + mnttsc;

                //montant = Convert.ToSingle(row["gfamtht"]) * p.Prorata(site, p.Compte_FG_TVA(site, intM.TVA1), intM.Date_piec.ToString());
                if (comptetva != null && !comptetva.Equals(" ") && comptetva.Length > 0)
                {
                    float prtrtah = CommonFacturation.Prorata(bc.CLIENT, comptetva, bc.PERIOD + "01");//facture.DATECREATION.Value.ToString("dd/MM/yyyy"));
                    if (prtrtah == -1)
                    {
                        logger.Info("Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        rapportGereration.Add("Ko : Merci de parametrer le prorata tva sur Agresso pour la société :" + bc.CLIENT + " et le compte :" + bc.ACCOUNT);
                        return false;
                    }
                    bc.AMOUNT = mntht * prtrtah;
                }
                else
                {
                    bc.AMOUNT = mntht;
                }

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                }
                bc.DIM_1 = facture.FACTSITE.DIM1;//N° site

                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }

            //La ligne IPPRRF
            if (compteIpprrf != null && compteIpprrf.Length > 0 && compteIpprrf != compteBidon)
            {
                if (facture.MNTHT != 0)
                {
                    bc.ACCOUNT = assoFE.COMPTEACHETTVAIPPRF;

                    if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                    {
                        bc.DC_FLAG = 1;
                        bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT) * 0.2;
                    }
                    else
                    {
                        bc.DC_FLAG = -1;
                        bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTHT) * 0.2;
                    }
                    mntipprrf = bc.AMOUNT;

                    seq = seq + 1;
                    bc.SEQUENCE_NO = seq;
                    bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                    String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                    bc.ATT_1_ID = att_ids[0];
                    bc.ATT_2_ID = att_ids[1];
                    bc.ATT_3_ID = att_ids[2];
                    bc.ATT_4_ID = att_ids[3];
                    bc.ATT_5_ID = att_ids[4];
                    bc.ATT_6_ID = att_ids[5];
                    bc.ATT_7_ID = att_ids[6];
                    if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                    {
                        bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                    }

                    bc.DIM_1 = facture.FACTSITE.DIM1;//N° site

                    if (ens.CODE.Equals("CF"))
                    {
                        bc.DIM_1 = "01";
                    }

                    if (isok)
                    {
                        try
                        {
                            if (!facture.insert_temp(bc, "ACRTRANS"))
                                return false;
                        }
                        catch (Exception e)
                        {
                            logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                            rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                            return false;
                        }
                    }
                }
            }
            return isok;
        }

        private bool achLigneTtcLg(FACTFACTURE facture, ref AGR bc, FACTASSOGFACTTYPEENSEIGNE assoFE, bool isok, FACTENSEIGNE ens, ref int seq){
                        //La ligne TTC
            if (facture.MNTTTC != 0)
            {
                if (assoFE.COMPTEACHETFRS == null || assoFE.COMPTEACHETFRS.Length <= 7)
                {
                    logger.Info("Compte fournisseur de l'acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    rapportGereration.Add("Ko : Compte fournisseur de l'acheteur est non défini !!! Merci de le saisir sur l'ecran des chapites");
                    isok = false;
                    return isok;
                }
                bc.ACCOUNT = assoFE.COMPTEACHETFRS;

                if (facture.FLAGAVOIR.HasValue && facture.FLAGAVOIR == 1)
                {
                    bc.DC_FLAG = 1;
                    bc.AMOUNT =-1 * Convert.ToDouble(facture.MNTTTC);
                }
                else
                {
                    bc.DC_FLAG = -1;
                    bc.AMOUNT = -1 * Convert.ToDouble(facture.MNTTTC);
                }
                string compteIpprrf = assoFE.COMPTEACHETTVAIPPRF;
                double mntipprrf = 0.0;

                if (compteIpprrf != null && compteIpprrf.Length > 0 && compteIpprrf != compteBidon && facture.MNTHT!=0)
                {
                    mntipprrf = -1 * Convert.ToDouble(facture.MNTHT) * 0.2;
                }

                bc.AMOUNT = bc.AMOUNT - mntipprrf;

                seq = seq + 1;
                bc.SEQUENCE_NO = seq;
                bc.TRANS_TYPE = CommonFacturation.getTrans_type(ens.CODE, bc.ACCOUNT);
                String[] att_ids = CommonFacturation.getListATT_ID(ens.CODE, bc.ACCOUNT);
                bc.ATT_1_ID = att_ids[0];
                bc.ATT_2_ID = att_ids[1];
                bc.ATT_3_ID = att_ids[2];
                bc.ATT_4_ID = att_ids[3];
                bc.ATT_5_ID = att_ids[4];
                bc.ATT_6_ID = att_ids[5];
                bc.ATT_7_ID = att_ids[6];
                if (ens.CODE.Equals("AC") || ens.CODE.Equals("CF") || ens.CODE.Equals("EM") || ens.CODE.Equals("VB") || ens.CODE.Equals("TB") || ens.CODE.Equals("AL") || ens.CODE.Equals("TC"))
                {
                    bc.DIM_2 = CommonFacturation.getDim2(ens.CODE, bc.ATT_1_ID, bc.ATT_2_ID, facture.FACTSITE.CODEAGRESSO);
                }
                bc.DIM_1 = facture.FACTSITE.DIM1;//N° site

                if (ens.CODE.Equals("CF"))
                {
                    bc.DIM_1 = "01";
                }
                if (isok)
                {
                    try
                    {
                        if (!facture.insert_temp(bc, "ACRTRANS"))
                            return false;
                    }
                    catch (Exception e)
                    {
                        logger.Info("Probleme insertion Brouillard !!! " + e.ToString());
                        rapportGereration.Add("Ko : Probleme insertion Brouillard !!! " + e.ToString());
                        return false;
                    }
                }
            }
            return isok;
        }

        public ActionResult ExportExcelConsulter(Boolean keepFilters = false, int page = 1, string search = "", int sortby = 0, Boolean isasc = true, string advSearch = "")
        {
            FACTFACTURE facture = new FACTFACTURE();
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int iduser = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            //long nolot=facture.getMaxNolot();
            //if (nolot <= 0)
            //    return;
            ICollection<INTEGBROUILLARD> listINTEGRER_BROUILLARD = facture.consulterBrouillard(0, iduser);
            if (listINTEGRER_BROUILLARD.Count <= 0) { 
                TempData["success"] = "false";
                TempData["message"] = string.Format("Acucune facture à l'état << A comptabiliée >>");
                logger.Error("Acucune facture à l'état << A comptabiliée >>");
                return RedirectToAction("Index", new { keepFilters = true });
            }


            string titreSheet = "Consulter brouillards integrés";

            if (listINTEGRER_BROUILLARD != null && listINTEGRER_BROUILLARD.Count > 0)
            {

                //***********************create the Excel Sheet************************
                IWorkbook workbook = new XSSFWorkbook();
                ISheet sheet1 = workbook.CreateSheet(titreSheet);

                //setting styles
                XSSFCellStyle titleStyle = (XSSFCellStyle)workbook.CreateCellStyle();
                var titleFont = workbook.CreateFont();
                XSSFColor myColor = new XSSFColor(System.Drawing.ColorTranslator.FromHtml("#35aa47"));
                titleStyle.SetFillForegroundColor(myColor);
                titleStyle.FillPattern = FillPattern.SolidForeground;
                titleFont.FontHeightInPoints = 14;
                titleFont.Boldweight = (short)FontBoldWeight.Bold;
                titleFont.IsItalic = true;
                titleFont.Color = IndexedColors.White.Index;
                titleStyle.SetFont(titleFont);

                var subTitleStyle = workbook.CreateCellStyle();
                var subTitleFont = workbook.CreateFont();
                subTitleFont.FontHeightInPoints = 14;
                subTitleFont.Boldweight = (short)FontBoldWeight.Bold;
                subTitleFont.IsItalic = true;
                subTitleStyle.SetFont(subTitleFont);

                var headerStyle = workbook.CreateCellStyle();
                var headerFont = workbook.CreateFont();
                headerStyle.FillForegroundColor = IndexedColors.DarkBlue.Index;
                headerStyle.FillPattern = FillPattern.SolidForeground;
                headerFont.FontHeightInPoints = 11;
                headerFont.Boldweight = (short)FontBoldWeight.Bold;
                headerFont.Color = IndexedColors.White.Index;
                headerStyle.SetFont(headerFont);
                IDataFormat dataFormatCustom = workbook.CreateDataFormat();
                var rowStyle = workbook.CreateCellStyle();

                //setting Title                
                //IRow TitleRow = sheet1.CreateRow(0);
                //ICell titleRow = TitleRow.CreateCell(0);
                //titleRow.SetCellValue("Export des brouillands intégrés dans AGRESSO ");
                //titleRow.CellStyle = titleStyle;
                //NPOI.SS.Util.CellRangeAddress cra = new NPOI.SS.Util.CellRangeAddress(0, 0, 0, 5);
                //sheet1.AddMergedRegion(cra);
                //IRow subTitleRow1 = sheet1.CreateRow(1);
                //ICell subtitleRow1 = subTitleRow1.CreateCell(0);
                //subtitleRow1.SetCellValue("");
                //subtitleRow1.CellStyle = subTitleStyle;

                //entête de la facture
                //IRow hr1 = sheet1.CreateRow(2);
                //ICell hr1cel1 = hr1.CreateCell(1);
                //hr1cel1.SetCellValue("Numéro de facture");
                //hr1cel1.CellStyle = headerStyle;

                //IRow hr2 = sheet1.CreateRow(3);
                //ICell hr2cel1 = hr2.CreateCell(1);
                //hr2cel1.CellStyle = subTitleStyle;
                //hr2cel1.SetCellValue(noFacture);

                // creation de le tableau des donnees
                DataTable aDt = new DataTable();

                //remplir de l entete de tableau des donnees
                aDt.Columns.Add(new DataColumn("N° lot", typeof(Double)));
                aDt.Columns.Add(new DataColumn("Réf. facture", typeof(Double)));
                aDt.Columns.Add(new DataColumn("Client", typeof(string)));
                aDt.Columns.Add(new DataColumn("A. fiscale", typeof(Double)));
                aDt.Columns.Add(new DataColumn("Période", typeof(string)));
                //aDt.Columns.Add(new DataColumn("Date Compta.", typeof(DateTime)));
                aDt.Columns.Add(new DataColumn("Date Fact.", typeof(DateTime)));
                aDt.Columns.Add(new DataColumn("Négatif", typeof(Double)));
                aDt.Columns.Add(new DataColumn("Positif", typeof(Double)));
                aDt.Columns.Add(new DataColumn("Equilibre", typeof(Double)));
                aDt.Columns.Add(new DataColumn("Séq.", typeof(Double)));
                aDt.Columns.Add(new DataColumn("Compte", typeof(string)));
                aDt.Columns.Add(new DataColumn("Montant", typeof(Double)));
                aDt.Columns.Add(new DataColumn("DIM_1", typeof(string)));
                aDt.Columns.Add(new DataColumn("DIM_2", typeof(string)));
                aDt.Columns.Add(new DataColumn("DIM_4", typeof(string)));
                aDt.Columns.Add(new DataColumn("Description", typeof(string)));
                aDt.Columns.Add(new DataColumn("", typeof(Double)));
                aDt.Columns.Add(new DataColumn("", typeof(Double)));
                aDt.Columns.Add(new DataColumn("", typeof(string)));


                ////remplir de le body de tableau des donnees
                foreach (INTEGBROUILLARD item in listINTEGRER_BROUILLARD)
                {
                    List<INTEGBROUILLARDDET> details = facture.chercherDetailsBrouillard(item.CROID, item.CLIENT, item.IDFACTURE);
                    foreach (INTEGBROUILLARDDET detitem in details)
                    {
                        aDt.Rows.Add(item.NO_LOT, item.CROID, item.CLIENT, item.ANNEE_FISCALE, item.PERIOD, item.DATE_FACTURE,
                            item.MNT_NEG, item.MNT_POS, item.EQUILIBRE, detitem.SEQUENCE, detitem.COMPTE, detitem.MONTANT,
                            detitem.DIM1, detitem.DIM2, detitem.DIM4, detitem.DESCRIPTION,
                            item.NBRE_SEQUENCE, item.IDFACTURE, item.CLIENT);
                    }
                }
                int ligneEntete = 0;
                int colDecaler = 0;
                int colDebut = 0 + colDecaler;
                int ligneDetail = 1;

                //ajouter le style entete et les éléments entete de tableau des donnees au sheet
                IRow headerRow = sheet1.CreateRow(ligneEntete);
                for (int j = 0; j < aDt.Columns.Count - 3; j++)
                {
                    headerRow.CreateCell(j + colDecaler).SetCellValue(aDt.Columns[j].ColumnName);
                    headerRow.Cells[j].CellStyle = headerStyle;
                }

                var celluleStyle = workbook.CreateCellStyle();
                //celluleStyle.Alignment = HorizontalAlignment.Center;
                celluleStyle.VerticalAlignment = VerticalAlignment.Center;


                double encienCROID = 0; double ancienIdfacture = 0; string ancienclient = "";
                //ajouter les éléments body de tableau des donnees au sheet
                for (int i = ligneDetail; i < aDt.Rows.Count + ligneDetail; i++)
                {
                    IRow row = sheet1.CreateRow(i);
                    for (int j = 0; j < aDt.Columns.Count; j++)
                    {
                        if (j == 1)
                        {
                            if (i == (aDt.Rows.Count - 1 + ligneDetail))
                            {
                                int nbreCellulesMergee = Convert.ToInt32((double)aDt.Rows[(i - ligneDetail) - 1][16]);
                                sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee + 1), i, colDebut, colDebut));
                                sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee + 1), i, colDebut + 1, colDebut + 1));
                                sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee + 1), i, colDebut + 2, colDebut + 2));
                                sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee + 1), i, colDebut + 3, colDebut + 3));
                                sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee + 1), i, colDebut + 4, colDebut + 4));
                                sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee + 1), i, colDebut + 5, colDebut + 5));
                                sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee + 1), i, colDebut + 6, colDebut + 6));
                                sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee + 1), i, colDebut + 7, colDebut + 7));
                                sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee + 1), i, colDebut + 8, colDebut + 8));
                            }

                            if ((encienCROID != (double)aDt.Rows[i - ligneDetail][1]) || (ancienIdfacture != (double)aDt.Rows[i - ligneDetail][17]) || (ancienclient != (string)aDt.Rows[i - ligneDetail][18]))
                            {
                                if (encienCROID != 0)
                                {
                                    int nbreCellulesMergee = Convert.ToInt32((double)aDt.Rows[(i - ligneDetail) - 1][16]);
                                    sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee), i - 1, colDebut, colDebut));
                                    sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee), i - 1, colDebut + 1, colDebut + 1));
                                    sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee), i - 1, colDebut + 2, colDebut + 2));
                                    sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee), i - 1, colDebut + 3, colDebut + 3));
                                    sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee), i - 1, colDebut + 4, colDebut + 4));
                                    sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee), i - 1, colDebut + 5, colDebut + 5));
                                    sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee), i - 1, colDebut + 6, colDebut + 6));
                                    sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee), i - 1, colDebut + 7, colDebut + 7));
                                    sheet1.AddMergedRegion(new CellRangeAddress((i - nbreCellulesMergee), i - 1, colDebut + 8, colDebut + 8));
                                }

                                encienCROID = (double)aDt.Rows[i - ligneDetail][1];
                                ancienIdfacture = (double)aDt.Rows[i - ligneDetail][17];
                                ancienclient = (string)aDt.Rows[i - ligneDetail][18];
                            }
                            else
                            {
                                row.Cells[row.Cells.Count - 1].SetCellValue("");
                                //row.Cells[row.Cells.Count - 2].SetCellValue("");
                                j = 8;
                                continue;
                            }
                        }

                        if (j != 16 && j != 17 && j != 18)
                        {
                            switch (Type.GetTypeCode(aDt.Rows[i - ligneDetail][j].GetType()))
                            {
                                case TypeCode.Double:
                                    ICell cell2 = row.CreateCell(j + colDecaler, CellType.Numeric);
                                    cell2.SetCellValue((double)aDt.Rows[i - ligneDetail][j]);
                                    cell2.CellStyle = celluleStyle;
                                    break;
                                default:
                                    ICell cell3 = row.CreateCell(j + colDecaler, CellType.String);
                                    cell3.SetCellValue(aDt.Rows[i - ligneDetail][j].ToString());
                                    cell3.CellStyle = celluleStyle;
                                    break;
                            }
                        }
                    }

                }

                for (int i = 0; i <= aDt.Columns.Count; i++)
                {
                    sheet1.AutoSizeColumn(i);
                    GC.Collect(); // Add this line
                }

                using (var exportData = new MemoryStream())
                {
                    workbook.Write(exportData);
                    byte[] bytes = exportData.ToArray();
                    //exportData.Capacity = bytes.Length;
                    Response.Clear();
                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    Response.AddHeader("Content-Length", bytes.Length.ToString());
                    Response.AddHeader("Content-disposition", "attachment; filename=" + titreSheet + ".xlsx");
                    Response.BinaryWrite(bytes);
                    Response.Flush();
                    Response.End();
                }

            }
            return RedirectToAction("Index", new { keepFilters = true });
        }

        public bool executerRequette(String req) {
            using (EramEntities DB = new EramEntities())
            {
                using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                {
                    conn.Open();
                    using (OracleCommand cmd = new OracleCommand(req, conn))
                    {
                        try
                        {
                            cmd.ExecuteNonQuery();
                        }
                        catch (Exception ex)
                        {
                            logger.Error("La requette est :" + req + "; Error =" + ex.ToString());
                            return false;
                        }
                    }
                    conn.Close();
                    conn.Dispose();
                }
            }
            return true;
        }

        public bool getListInfosClient(String client, out Int64 counter, out int trans_id)
        {
            counter = 0;
            trans_id = 0;
            StringBuilder requette = new StringBuilder();
            using (EramEntities DB = new EramEntities())
            {
                try
                {
                    requette.Append("select counter,trans_id from acrtransgr" + agrDbLink + ", acrvouchtype" + agrDbLink + " ");
                    requette.Append(" where ACRTRANSGR.VOUCH_SERIES=ACRVOUCHTYPE.VOUCH_SERIES and ACRTRANSGR.client=ACRVOUCHTYPE.client ");
                    requette.Append(" and ACRTRANSGR.client='" + client + "' and ACRVOUCHTYPE.VOUCHER_TYPE='GF' and rownum=1");

                    //logger.Error("La requette est =" + requette);

                    using (OracleConnection conn = (OracleConnection)DB.Database.Connection)
                    {
                        conn.Open();
                        using (OracleCommand cmd = new OracleCommand(requette.ToString(), conn))
                        {
                            using (OracleDataReader rd = cmd.ExecuteReader())
                            {
                                if (rd.Read())
                                {
                                    counter = Convert.ToInt64(rd.GetValue(0));
                                    trans_id = Convert.ToInt32(rd.GetValue(1));
                                }
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
                    logger.Error("Probleme recuperation infos client getListInfosClient =" + ex.Message + "La requette est =" + requette.ToString());
                    return false;
                }
            }
            //FACTFACTURE facture = new FACTFACTURE();
            if (counter != 0) { 
                if (!executerRequette((new FACTFACTURE()).updateCounter(client, counter + 1)))
                {
                    logger.Info("Impossible de mettre à jour le counter(voucher_no) sur Agresso, merci de vérifier la plage !!!");
                    rapportGereration.Add("Ko : Impossible de mettre à jour le counter(voucher_no) sur Agresso, merci de vérifier la plage !!!");
                    return false;
                }
            }
            return true;
        }


        public ViewResult View2()
        {
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

            FACRAPPORTGEN rapport = new FACRAPPORTGEN();
            List<String> autres = new List<String>();
            //int? idrapp = rapport.getMaxRapports();
            //if (idrapp != null && idrapp != -1) {
                rapport = rapport.getOneByUser(UTILISATEURCONNECTE);
                if (rapport != null && !string.IsNullOrWhiteSpace(rapport.CONTENURAPPORT))
                {
                    foreach (var ligne in rapport.CONTENURAPPORT.Split(new[] { "-NEWLINE-" }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        autres.Add(ligne);
                    }

                    rapport.MESSAGES = autres;
                }
                else {
                    rapport = new FACRAPPORTGEN();
                    autres.Add("Rapport vide");
                    rapport.MESSAGES = autres;
                }
            //}

            return View(rapport);
        }

        [HttpPost]
        public ActionResult generer()
        {

            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

            FACTFACTURE fact = new FACTFACTURE();
            List<FACTFACTURE> listAcomptabilise = fact.facturesAIntegrer(UTILISATEURCONNECTE);
            rapportGereration.Clear();
            rapportGereration.Add("Début intégration brouillard");

            foreach (FACTFACTURE facture in listAcomptabilise) {
                string reqInsert = requetteInsertAgr(facture.IDFACTURE);
                if (CommonFacturation.verifEquilibre(facture.IDFACTURE))
                {
                    facture.updateAcrtransLastUpdate(facture.IDFACTURE);
                    if (CommonFacturation.insererAgresso(reqInsert))
                    {

                        facture.comptabiliser(facture.IDFACTURE, UTILISATEURCONNECTE);
                    }
                    else {
                        rapportGereration.Add("Problème communication avec Agresso ou Problème d'insertion dans agresso, la requette=" + reqInsert);
                    }
                }
                else
                    rapportGereration.Add("Une pièce comptable de la facture " + facture.REFERENCEFACT + " est non équilibrée !");
            }

            rapportGereration.Add("Fin intégration brouillard");

            string body = string.Join("-NEWLINE-", rapportGereration.ToArray());
            FACRAPPORTGEN rapport = new FACRAPPORTGEN();
            rapport.insererFichier(UTILISATEURCONNECTE, body);

            return RedirectToAction("Index", new { keepFilters = true });
        }

        //public int gePeriodeOuverte(string client, int anneeFiscal)
        //{
        //    int periode = -1;
        //    StringBuilder requette = new StringBuilder();
        //    requette.Append(" SELECT MAX(PERIOD)  FROM ACRPERIOD ");
        //    requette.Append(" where client=:client  ");
        //    requette.Append(" and STATUS='N' ");
        //    if (anneeFiscal!=-1)
        //        requette.Append(" and FISCAL_YEAR=:anneeFiscal  ");

        //    try
        //    {
        //        using (OracleConnection conn = new OracleConnection(OracleConnectionAgresso))
        //        {
        //            conn.Open();
        //            using (OracleCommand cmd = new OracleCommand(requette.ToString(), conn))
        //            {
        //                cmd.Parameters.Add(new OracleParameter("client", client.Trim()));
        //                if (anneeFiscal != -1)
        //                    cmd.Parameters.Add(new OracleParameter("anneeFiscal", anneeFiscal));

        //                using (OracleDataReader rd = cmd.ExecuteReader())
        //                {
        //                    if (rd.Read())
        //                        periode =  Int32.Parse(rd.GetValue(0).ToString());
        //                }
        //            }
        //            conn.Close();
        //            conn.Dispose();
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        logger.Error("Probleme récupération periode comptabilisation; requette=" + requette.ToString());
        //        logger.Error("erreur=" + ex.Message);
        //    }
        //    return periode;
        //}


        private string requetteInsertAgr(int idfacture)
        {
            StringBuilder requette = new StringBuilder();

            requette.Append("insert into acrtrans" + agrDbLink + " ( "); //acrtrans@AGRPROD
            requette.Append("  accept_status  , ");
            requette.Append("  account        , ");
            requette.Append("  account2       , ");
            requette.Append("  address        , ");
            requette.Append("  allocation_key , ");
            requette.Append("  amount         , ");
            requette.Append("  apar_id        , ");
            requette.Append("  apar_name      , ");
            requette.Append("  apar_type      , ");
            requette.Append("  arrival_date   , ");
            requette.Append("  arrive_id      , ");
            requette.Append("  att_1_id       , ");
            requette.Append("  att_2_id       , ");
            requette.Append("  att_3_id       , ");
            requette.Append("  att_4_id       , ");
            requette.Append("  att_5_id       , ");
            requette.Append("  att_6_id       , ");
            requette.Append("  att_7_id       , ");
            requette.Append("  bank_account   , ");
            requette.Append("  base_amount    , ");
            requette.Append("  base_curr      , ");
            requette.Append("  clearing_code  , ");
            requette.Append("  client         , ");
            requette.Append("  client_ref     , ");
            requette.Append("  collection     , ");
            requette.Append("  commitment     , ");
            requette.Append("  complaint      , ");
            requette.Append("  compress_flag  , ");
            requette.Append("  contract_order , ");
            requette.Append("  cur_amount     , ");
            requette.Append("  curr_doc       , ");
            requette.Append("  curr_licence   , ");
            requette.Append("  currency       , ");
            requette.Append("  dc_flag        , ");
            requette.Append("  description    , ");
            requette.Append("  dim_1          , ");
            requette.Append("  dim_2          , ");
            requette.Append("  dim_3          , ");
            requette.Append("  dim_4          , ");
            requette.Append("  dim_5          , ");
            requette.Append("  dim_6          , ");
            requette.Append("  dim_7          , ");
            requette.Append("  disc_date      , ");
            requette.Append("  disc_percent   , ");
            requette.Append("  discount       , ");
            requette.Append("  due_date       , ");
            requette.Append("  exch_rate      , ");
            requette.Append("  exch_rate2     , ");
            requette.Append("  exch_rate3     , ");
            requette.Append("  ext_inv_ref    , ");
            requette.Append("  factor_short   , ");
            requette.Append("  fiscal_year    , ");
            requette.Append("  header_flag    , ");
            requette.Append("  int_status     , ");
            requette.Append("  intrule_id     , ");
            requette.Append("  kid            , ");
            requette.Append("  last_update    , ");
            requette.Append("  line_no        , ");
            requette.Append("  number_1       , ");
            requette.Append("  order_id       , ");
            requette.Append("  pay_currency   , ");
            requette.Append("  pay_flag       , ");
            requette.Append("  pay_method     , ");
            requette.Append("  pay_transfer   , ");
            requette.Append("  period         , ");
            requette.Append("  period_no      , ");
            requette.Append("  place          , ");
            requette.Append("  province       , ");
            requette.Append("  pseudo_id      , ");
            requette.Append("  reg_amount     , ");
            requette.Append("  rem_level      , ");
            requette.Append("  responsible    , ");
            requette.Append("  rev_period     , ");
            requette.Append("  sequence_no    , ");
            requette.Append("  sequence_ref   , ");
            requette.Append("  sequence_ref2  , ");
            requette.Append("  status         , ");
            requette.Append("  swift          , ");
            requette.Append("  tax_code       , ");
            requette.Append("  tax_id         , ");
            requette.Append("  tax_system     , ");
            requette.Append("  template_type  , ");
            requette.Append("  terms_id       , ");
            requette.Append("  trans_date     , ");
            requette.Append("  trans_id       , ");
            requette.Append("  trans_type     , ");
            requette.Append("  treat_code     , ");
            requette.Append("  user_id        , ");
            requette.Append("  value_1        , ");
            requette.Append("  value_2        , ");
            requette.Append("  value_3        , ");
            requette.Append("  vat_amount     , ");
            requette.Append("  vat_reg_no     , ");
            requette.Append("  vouch_stat     , ");
            requette.Append("  voucher_date   , ");
            requette.Append("  voucher_no     , ");
            requette.Append("  voucher_ref    , ");
            requette.Append("  voucher_ref2   , ");
            requette.Append("  voucher_type   , ");
            requette.Append("  zip_code       , ");
            requette.Append("  auth_code      , ");
            requette.Append("  base_value_2   , ");
            requette.Append("  base_value_3   , ");
            requette.Append("  contract_id    , ");
            requette.Append("  orig_reference ) ");
            requette.Append("  select  ");
            requette.Append("    m.accept_status  , ");
            requette.Append("  m.account        , ");
            requette.Append("  m.account2       , ");
            requette.Append("  m.address        , ");
            requette.Append("  m.allocation_key , ");
            requette.Append("  m.amount         , ");
            requette.Append("  m.apar_id        , ");
            requette.Append("  m.apar_name      , ");
            requette.Append("  m.apar_type      , ");
            requette.Append("  m.arrival_date   , ");
            requette.Append("  m.arrive_id      , ");
            requette.Append("  m.att_1_id       , ");
            requette.Append("  m.att_2_id       , ");
            requette.Append("  m.att_3_id       , ");
            requette.Append("  m.att_4_id       , ");
            requette.Append("  m.att_5_id       , ");
            requette.Append("  m.att_6_id       , ");
            requette.Append("  m.att_7_id       , ");
            requette.Append("  m.bank_account   , ");
            requette.Append("  m.base_amount    , ");
            requette.Append("  m.base_curr      , ");
            requette.Append("  m.clearing_code  , ");
            requette.Append("  m.client         , ");
            requette.Append("  m.client_ref     , ");
            requette.Append("  m.collection     , ");
            requette.Append("  m.commitment     , ");
            requette.Append("  m.complaint      , ");
            requette.Append("  m.compress_flag  , ");
            requette.Append("  m.contract_order , ");
            requette.Append("  m.cur_amount     , ");
            requette.Append("  m.curr_doc       , ");
            requette.Append("  m.curr_licence   , ");
            requette.Append("  m.currency       , ");
            requette.Append("  m.dc_flag        , ");
            requette.Append("  m.description    , ");
            requette.Append("  m.dim_1          , ");
            requette.Append("  m.dim_2          , ");
            requette.Append("  m.dim_3          , ");
            requette.Append("  m.dim_4          , ");
            requette.Append("  m.dim_5          , ");
            requette.Append("  m.dim_6          , ");
            requette.Append("  m.dim_7          , ");
            requette.Append("  m.disc_date      , ");
            requette.Append("  m.disc_percent   , ");
            requette.Append("  m.discount       , ");
            requette.Append("  m.due_date       , ");
            requette.Append("  m.exch_rate      , ");
            requette.Append("  m.exch_rate2     , ");
            requette.Append("  m.exch_rate3     , ");
            requette.Append("  m.ext_inv_ref    , ");
            requette.Append("  m.factor_short   , ");
            requette.Append("  m.fiscal_year    , ");
            requette.Append("  m.header_flag    , ");
            requette.Append("  m.int_status     , ");
            requette.Append("  m.intrule_id     , ");
            requette.Append("  m.kid            , ");
            requette.Append("  m.last_update    , ");
            requette.Append("  m.line_no        , ");
            requette.Append("  m.number_1       , ");
            requette.Append("  m.order_id       , ");
            requette.Append("  m.pay_currency   , ");
            requette.Append("  m.pay_flag       , ");
            requette.Append("  m.pay_method     , ");
            requette.Append("  m.pay_transfer   , ");
            requette.Append("  m.period         , ");
            requette.Append("  m.period_no      , ");
            requette.Append("  m.place          , ");
            requette.Append("  m.province       , ");
            requette.Append("  m.pseudo_id      , ");
            requette.Append("  m.reg_amount     , ");
            requette.Append("  m.rem_level      , ");
            requette.Append("  m.responsible    , ");
            requette.Append("  m.rev_period     , ");
            requette.Append("  m.sequence_no    , ");
            requette.Append("  m.sequence_ref   , ");
            requette.Append("  m.sequence_ref2  , ");
            requette.Append("  m.status         , ");
            requette.Append("  m.swift          , ");
            requette.Append("  m.tax_code       , ");
            requette.Append("  m.tax_id         , ");
            requette.Append("  m.tax_system     , ");
            requette.Append("  m.template_type  , ");
            requette.Append("  m.terms_id       , ");
            requette.Append("  m.trans_date     , ");
            requette.Append("  m.trans_id       , ");
            requette.Append("  m.trans_type     , ");
            requette.Append("  m.treat_code     , ");
            requette.Append("  m.user_id        , ");
            requette.Append("  m.value_1        , ");
            requette.Append("  m.value_2        , ");
            requette.Append("  m.value_3        , ");
            requette.Append("  m.vat_amount     , ");
            requette.Append("  m.vat_reg_no     , ");
            requette.Append("  m.vouch_stat     , ");
            requette.Append("  m.voucher_date   , ");
            requette.Append("  m.voucher_no     , ");
            requette.Append("  m.voucher_ref    , ");
            requette.Append("  m.voucher_ref2   , ");
            requette.Append("  m.voucher_type   , ");
            requette.Append("  m.zip_code       , ");
            requette.Append("  m.auth_code      , ");
            requette.Append("  base_value_2   , ");
            requette.Append("  m.base_value_3   , ");
            requette.Append("  m.contract_id    , ");
            requette.Append("  m.orig_reference  ");
            requette.Append("  from acrtrans m,factfacture f ");
            requette.Append("where f.idfacture ='" + idfacture + "'");
            requette.Append(" AND m.idfacture=f.idfacture ");
            requette.Append(" AND f.status = 2 ");

            logger.Error("requetteInsertAgr =" + requette.ToString());
            return requette.ToString();
        }
	 
    }
}
