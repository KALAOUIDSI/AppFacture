using System;
using System.Collections.Generic;
using System.Configuration;
using System.Web;
using System.Web.Mvc;
using System.IO;
using Domain.appFacture;
using appFacture.Models;
using System.Web.Script.Serialization;
using System.Security.Claims;
using System.Threading;
using Newtonsoft.Json;
using log4net;
using Domain.appFacture.Rapports;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.SS.Util;
using System.Text;
using System.Globalization;

namespace appFacture.Controllers
{
    [Authorize(Roles = "OTOPFACT")]
    public class OtopFactureController : Controller
    {
        public int pageSize = 10;
        ILog logger = log4net.LogManager.GetLogger("KassagrWEBLogger");

        public ActionResult Index(Boolean keepFilters = false, int page = 1, string search = "", int sortby = 0, Boolean isasc = true, string advSearch = "")
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
            
            OTOPFACTUREAdvFiltre advFiltre = new JavaScriptSerializer().Deserialize<OTOPFACTUREAdvFiltre>(advSearch);
            if (advFiltre == null)
            {
                advFiltre = new OTOPFACTUREAdvFiltre();
            }
            //KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            //appel de méthode de lecture des données
            advFiltre.site = IdSiteConnecte;
            if (User.IsInRole("SIEGE"))
            {
                advFiltre.site = -1;
            }
            OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();
            OTOPFACTUREPaginationRes result = OTOPFACTURE.getAll(page, pageSize, search, sortby, isasc, advFiltre);
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

            return View(new OtopFactureListView() { PagingInfo = pagingInfo, Demandes = result.listDEMANDE, Search = search, SortBy = sortby, IsAsc = isasc, AdvSearch = advSearch, AdvSearchFilters = advFiltre });
        }

        //Liste déroulante des type de facture
        public List<FACTTYPE> getTypefacture()
        {
            FACTTYPE FACTTYPE = new FACTTYPE();
            return FACTTYPE.getAll();
        }
        public List<FACTTYPECLIENT> getTypeclient()
        {
            FACTTYPECLIENT FACTTYPECLIENT = new FACTTYPECLIENT();
            return FACTTYPECLIENT.getAll();
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

        public List<OTOPACHETEUR> getlistclientsinterne()
        {
            return OTOPFACTURE.getAllAcheteur();
        }

        [NoCache]
        public ViewResult Create()
        {
            keepFilters();
            OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();

            List<OTOPACHETEUR> lsclientsinterne = getlistclientsinterne();
            lsclientsinterne.Insert(0, new OTOPACHETEUR(-1, "Sélectionner un client"));
            ViewBag.listclientsinterne = lsclientsinterne;

            return View("Edit", OTOPFACTURE);
        }

        [NoCache]
        public ViewResult Avoir(int id)
        {
            keepFilters();
            OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();
            OTOPFACTURE = OTOPFACTURE.getOne(id);
            return View(OTOPFACTURE);
        }

        public ViewResult CrAvoir(int id)
        {
            keepFilters();
            OTOPFACTURE OTOPFACTURE3 = new OTOPFACTURE();
            OTOPFACTURE3 = OTOPFACTURE3.getOne(id);
            OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();
            OTOPFACTURE.IDFACTTYPE = OTOPFACTURE3.IDFACTTYPE;
            OTOPFACTURE.FACTTYPE = OTOPFACTURE3.FACTTYPE;
            OTOPFACTURE.IDDEMANDE = OTOPFACTURE3.IDDEMANDE;
            OTOPFACTURE.REFERENCEFACT = "";
            OTOPFACTURE.LIBELLEDEMANDE = "Avoir sur fct N°:" + OTOPFACTURE3.REFERENCEFACT + " - lib:" + OTOPFACTURE3.LIBELLEDEMANDE;
            OTOPFACTURE.FLAGAVOIR = 0;

            return View("Avoir",OTOPFACTURE);
        }

        public ActionResult View(int id)
        {
            keepFilters();
            OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();
            OTOPFACTURE=OTOPFACTURE.getOne(id);
            OTOPFACTURE.TVATOTAUX = OTOPFACTURE.calcultotauxtv(id);
            //KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            return View(OTOPFACTURE);
        }

        [NoCache]
        [HttpPost]
        public ActionResult Edit(int id)
        {
            keepFilters();
            OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();
            OTOPFACTURE = OTOPFACTURE.getOne(id);

            if (OTOPFACTURE.DATEIMPRESSION.HasValue)
            {
                TempData["state"] = "2";
                TempData["success"] = "false";
                TempData["message"] = string.Format("Impossible de modifier la demande car elle est déja imprimée !", OTOPFACTURE.REFERENCEFACT, OTOPFACTURE.LIBELLEDEMANDE);
                return RedirectToAction("Index", new { keepFilters = true });
            }
            
            ViewBag.isAdmin = true;

            List<OTOPACHETEUR> lsclientsinterne = getlistclientsinterne();
            lsclientsinterne.Insert(0, new OTOPACHETEUR(-1, "Sélectionner un client"));
            ViewBag.listclientsinterne = lsclientsinterne;


            return View(OTOPFACTURE);
        }

        [NoCache]
        public ActionResult Edit2(int id)
        {
            keepFilters();
            OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();
            OTOPFACTURE = OTOPFACTURE.getOne(id);

            ICollection<OTOPFACTUREDETAIL> ezee = OTOPFACTURE.OTOPFACTUREDETAIL;

            if (OTOPFACTURE.DATEIMPRESSION.HasValue)
            {
                TempData["state"] = "2";
                TempData["success"] = "false";
                TempData["message"] = string.Format("Impossible de modifier la demande car elle est déja imprimée !", OTOPFACTURE.REFERENCEFACT, OTOPFACTURE.LIBELLEDEMANDE);
                return RedirectToAction("Index", new { keepFilters = true });
            }

            ViewBag.isAdmin = true;

            List<OTOPACHETEUR> lsclientsinterne = getlistclientsinterne();
            lsclientsinterne.Insert(0, new OTOPACHETEUR(-1, "Sélectionner un client"));
            ViewBag.listclientsinterne = lsclientsinterne;


            return View(OTOPFACTURE);
        }

        public ViewResult View2()
        {
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

            FACRAPPORTGEN rapport = new FACRAPPORTGEN();
            List<String> autres = new List<String>();
            rapport = rapport.getOneByUser(UTILISATEURCONNECTE);
            if (rapport != null && !string.IsNullOrWhiteSpace(rapport.CONTENURAPPORT))
            {
                foreach (var ligne in rapport.CONTENURAPPORT.Split(new[] { "-NEWLINE-" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    autres.Add(ligne);
                }

                rapport.MESSAGES = autres;
            }
            else
            {
                rapport = new FACRAPPORTGEN();
                autres.Add("Rapport vide");
                rapport.MESSAGES = autres;
            }
            return View(rapport);
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
        public ActionResult ValidateEdit(int IDDEMANDE, string LIBELLEDEMANDE, int IDACHETEUR,string PRIODEDEBUT,string PRIODEFIN,string selectedDetails)
        {
                OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();
                bool isCreated = IDDEMANDE == 0 ? true : false;
                TempData["state"] = "1";

                ///KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
                //on commence par vérifier si le login est unique
                List<OTOPACHETEUR> lsclientsinterne = getlistclientsinterne();
                lsclientsinterne.Insert(0, new OTOPACHETEUR(-1, "Sélectionner un vendeur"));
                ViewBag.listclientsinterne = lsclientsinterne;

                if (IDACHETEUR <= 0)
                {
                    //erreur lors de la création d'un compte commercial par un profil Administrateur
                    keepFilters();
                    TempData["success"] = "false";
                    TempData["message"] = "Erreur : Vous devez choisir un vendeur dans la liste déroulante";
                    return RedirectToAction("Create");
                }

                try { 
                    Convert.ToDateTime(DateTime.ParseExact(PRIODEDEBUT, "dd/MM/yyyy", CultureInfo.InvariantCulture));
                    Convert.ToDateTime(DateTime.ParseExact(PRIODEFIN, "dd/MM/yyyy", CultureInfo.InvariantCulture));
                }
                catch (Exception e)
                {
                    //erreur lors de la création d'un compte commercial par un profil Administrateur
                    keepFilters();
                    TempData["success"] = "false";
                    TempData["message"] = "Erreur : Le format de la date est invalide merci de saisir des dates valides";
                    return RedirectToAction("Create");
                }

                string datedeb = PRIODEDEBUT;// "12/02/2019";
                string datefin = PRIODEFIN;// "12/02/2019";

                if (!getListArticles(ref IDDEMANDE, LIBELLEDEMANDE, IDACHETEUR, datedeb, datefin))
                {
                    //erreur lors de la création d'un compte commercial par un profil Administrateur
                    keepFilters();
                    TempData["success"] = "false";
                    TempData["message"] = "Erreur : Aucune ligne a selectionner";
                    return RedirectToAction("Create");
                }
                else {
                    if (IDDEMANDE > 0)
                    {
                        OTOPFACTURE otopf = new OTOPFACTURE();
                        otopf = otopf.getOne(IDDEMANDE);
                        return View("Edit2", otopf);
                    }
                
                    //TempData["success"] = "true" + IDDEMANDE;
                    //TempData["message"] = string.Format("La demande {0} {1} a bien été crée !", OTOPFACTURE.REFERENCEFACT, OTOPFACTURE.LIBELLEDEMANDE);
                }
                return RedirectToAction("Index", new { keepFilters = true });
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ValidateEdit2(int IDDEMANDE, string LIBELLEDEMANDE,  string selectedDetails)
        {

            string datedebut=""; 
            string datefin = "";

            OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();
            OTOPFACTURE = OTOPFACTURE.getOne(IDDEMANDE);

            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

            TempData["state"] = "2";

            string[][] details = null;
            if (selectedDetails != null && selectedDetails.Length > 0)
                details = JsonConvert.DeserializeObject<string[][]>(selectedDetails);

            OTOPFACTURE = OTOPFACTURE.update(IDDEMANDE, LIBELLEDEMANDE, -1, UTILISATEURCONNECTE, datedebut, datefin, details);
            OTOPFACTURE.TVATOTAUX = OTOPFACTURE.calcultotauxtv(OTOPFACTURE.IDDEMANDE);
            IDDEMANDE = OTOPFACTURE.IDDEMANDE;

            TempData["success"] = "true";
                TempData["message"] = string.Format("La demande {0} {1} a bien été modifié !", OTOPFACTURE.REFERENCEFACT, OTOPFACTURE.LIBELLEDEMANDE);
            
            return RedirectToAction("Index", new { keepFilters = true });
        }

        [HttpPost]
        [ValidateInput(false)]
        public ActionResult ValidateEditAv(int IDDEMANDE, string LIBELLEDEMANDE, string PRIODEDEBUT, string PRIODEFIN, string selectedDetails)
        {
            OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();
            bool isCreated = IDDEMANDE == 0 ? true : false;
            TempData["state"] = "1";

            try
            {
                Convert.ToDateTime(DateTime.ParseExact(PRIODEDEBUT, "dd/MM/yyyy", CultureInfo.InvariantCulture));
                Convert.ToDateTime(DateTime.ParseExact(PRIODEFIN, "dd/MM/yyyy", CultureInfo.InvariantCulture));
            }
            catch (Exception e)
            {
                //erreur lors de la création d'un compte commercial par un profil Administrateur
                keepFilters();
                TempData["success"] = "false";
                TempData["message"] = "Erreur : Le format de la date est invalide merci de saisir des dates valides";
                return RedirectToAction("Index", new { keepFilters = true });// RedirectToAction("CrAvoir", IDDEMANDE);
            }

            string datedeb = PRIODEDEBUT;// "12/02/2019";
            string datefin = PRIODEFIN;// "12/02/2019";

            OTOPFACTURE fact = new OTOPFACTURE();
            fact = fact.getOne(IDDEMANDE);

            if (!getListArticlesAv(ref IDDEMANDE, LIBELLEDEMANDE, fact.OTOPACHETEUR.IDACHETEUR, datedeb, datefin))
            {
                //erreur lors de la création d'un compte commercial par un profil Administrateur
                keepFilters();
                TempData["success"] = "false";
                TempData["message"] = "Erreur : Aucune ligne a selectionner";
                return RedirectToAction("Index", new { keepFilters = true });
                //return RedirectToAction("CrAvoir",IDDEMANDE);
            }

            return RedirectToAction("Index", new { keepFilters = true });
        }

        public void ExportPdf(int iddemande)
        {
            OTOPFACTURE dem = new OTOPFACTURE();
            dem = dem.getOne(iddemande);

            if (dem.MNTTTC != 0 && dem.STATUS < 1 && !dem.DATEIMPRESSION.HasValue)
            {
                dem.majRef(iddemande);
            }

            //CrystalReportFacture crp = new CrystalReportFacture();
            int idenseinge = Int16.Parse(ConfigurationManager.AppSettings["IDENTIFENSEIGNE"]);
            string logoenseigne = ConfigurationManager.AppSettings["LOGOOTOP"];


            CrystalReportfact2 crp = dem.ExportPdf(iddemande, idenseinge, logoenseigne);

                //logger.Info(" Nombre rows facture ds=" + ds.facture.Rows.Count);
                var fileName = "Facture_" + iddemande + ".pdf";

                using (Stream file = crp.ExportToStream(CrystalDecisions.Shared.ExportFormatType.PortableDocFormat))
                {
                    byte[] bytes = new byte[file.Length];
                    file.Read(bytes, 0, (int)file.Length);

                    if (dem.MNTTTC != 0 && dem.STATUS < 1 && !dem.DATEIMPRESSION.HasValue)
                    {
                        ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
                        int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
                        dem.imprimer(1, iddemande, UTILISATEURCONNECTE);
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
            OTOPFACTURE dem = new OTOPFACTURE();
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
            short status = 2;
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int iduser = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            if (commentaire == null)
                commentaire = "";
            if (Request.Form["valider"] != null)
            {
                status = 1;
            }
            else if (Request.Form["refuser"] != null)
            {
                status = 2;
            }
            OTOPFACTURE demande = new OTOPFACTURE();
            demande.validerOuRefuser(id, status, commentaire, iduser);
            return RedirectToAction("Index", new { keepFilters = true });
        }

        public ActionResult Genbrouillard()
        {
            keepFilters();

            ComptabiliserOtop.genererBrouillard();

            return RedirectToAction("Index", new { keepFilters = true });
        }

        [HttpPost]
        public ActionResult generer()
        {
            ComptabiliserOtop.integrer();
            return RedirectToAction("Index", new { keepFilters = true });
        }

        [HttpPost]
        public ActionResult Reporter(int id)
        {
            TempData["state"] = "3";
            try
            {
                ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
                int iduser = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
                OTOPFACTURE FACTFACTURE = new OTOPFACTURE();
                FACTFACTURE.reporter(id, iduser);
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

        [HttpPost]
        public ActionResult Delete(int id)
        {
            TempData["state"] = "3";
            try
            {
                OTOPFACTURE OTOPFACTURE = new OTOPFACTURE();
                OTOPFACTURE = OTOPFACTURE.getOne(id);
                if (OTOPFACTURE.DATEIMPRESSION.HasValue)
                {
                    TempData["success"] = "false";
                    TempData["message"] = string.Format("Impossible de supprimer la demande car elle est déja imprimée !", OTOPFACTURE.REFERENCEFACT, OTOPFACTURE.LIBELLEDEMANDE);
                }else {
                    OTOPFACTURE.delete(id);
                    TempData["success"] = "true";
                    TempData["message"] = string.Format("La demande {0} {1} a bien été supprimé !", OTOPFACTURE.REFERENCEFACT, OTOPFACTURE.LIBELLEDEMANDE);
                }
            }
            catch (Exception ex)
            {
                TempData["success"] = "false";
                TempData["message"] = string.Format("Erreur system : {0}", ex.Message);
            }
            return RedirectToAction("Index", new { keepFilters = true });
        }

        public bool getListArticles(ref int IDDEMANDE, string LIBELLEDEMANDE, int IDACHETEUR, string datedeb, string datefin)
        {
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            OTOPACHETEUR client = new OTOPACHETEUR();
            client=client.getOne(IDACHETEUR);

            int cntLines = getCountLine(client.CODEGOLD.Value, datedeb, datefin);
            //cntLines = 0;
            if (cntLines == 0)
                return false;
            string[][] details = new string[cntLines][];
            OTOPFACTURE otopfacture = new OTOPFACTURE();
            int i = 0;
            //for (int j = 0; j < 10; j++)
            //{
                string OracleConnectionString = ConfigurationManager.ConnectionStrings["Gold"].ConnectionString;
                using (OracleConnection conn = new OracleConnection(OracleConnectionString))
                {
                    String sql = OTOPFACTURE.getLinesfacturesFromGOLD(client.CODEGOLD.Value, datedeb, datefin);
                   // logger.Info("sql="+sql);
                    conn.Open();

                    using (OracleCommand cmd = new OracleCommand(sql, conn))
                    {
                        cmd.Parameters.Add(new OracleParameter("datedeb", datedeb));
                        cmd.Parameters.Add(new OracleParameter("datefin", datefin));

                        using (OracleDataReader rd = cmd.ExecuteReader())
                        {
     
                            while (rd.Read())
                            {
                                try
                                {
                                    string[] det = new string[7];
                                    det[0] = Convert.ToString(rd.GetValue(0));
                                    string ean = Convert.ToString(rd.GetValue(1));
                                    string ecode = Convert.ToString(rd.GetValue(2));
                                    det[1] = Convert.ToDouble(rd.GetValue(3)).ToString();
                                    det[2] = Convert.ToDouble(rd.GetValue(4)).ToString();
                                    det[3] = Convert.ToDouble(rd.GetValue(5)).ToString();
                                    det[4] = Convert.ToString(rd.GetValue(6)).ToString(); //Rayon
                                    det[5] = ecode;
                                    det[6] = Convert.ToString(rd.GetValue(7)).ToString(); //Lib_Rayon

                                    details[i] = det;
                                    i++;
                                }
                                catch (Exception ex)
                                {
                                    logger.Info(ex.ToString());
                                }
                            }
                        }
                    }
                }
            //}

            otopfacture = otopfacture.update(IDDEMANDE, LIBELLEDEMANDE, IDACHETEUR, UTILISATEURCONNECTE, datedeb, datefin, details);
            otopfacture.TVATOTAUX = otopfacture.calcultotauxtv(otopfacture.IDDEMANDE);
            IDDEMANDE = otopfacture.IDDEMANDE;
            return true;
        }

        public bool getListArticlesAv(ref int IDDEMANDE, string LIBELLEDEMANDE, int IDACHETEUR, string datedeb, string datefin)
        {
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            OTOPACHETEUR client = new OTOPACHETEUR();
            client = client.getOne(IDACHETEUR);

            int cntLines = getCountLineAv(client.CODEGOLD.Value, datedeb, datefin);
            //cntLines = 0;
            if (cntLines == 0)
                return false;
            string[][] details = new string[cntLines][];
            OTOPFACTURE otopfacture = new OTOPFACTURE();
            int i = 0;
            //for (int j = 0; j < 10; j++)
            //{
            string OracleConnectionString = ConfigurationManager.ConnectionStrings["Gold"].ConnectionString;
            using (OracleConnection conn = new OracleConnection(OracleConnectionString))
            {
                String sql = OTOPFACTURE.getLinesfacturesAvFromGOLD(client.CODEGOLD.Value, datedeb, datefin);
                // logger.Info("sql="+sql);
                conn.Open();

                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("datedeb", datedeb));
                    cmd.Parameters.Add(new OracleParameter("datefin", datefin));

                    using (OracleDataReader rd = cmd.ExecuteReader())
                    {

                        while (rd.Read())
                        {
                            try
                            {
                                string[] det = new string[7];
                                det[0] = Convert.ToString(rd.GetValue(0));
                                string ean = Convert.ToString(rd.GetValue(1));
                                string ecode = Convert.ToString(rd.GetValue(2));
                                det[1] = Convert.ToDouble(rd.GetValue(3)).ToString();
                                det[2] = Convert.ToDouble(rd.GetValue(4)).ToString();
                                det[3] = Convert.ToDouble(rd.GetValue(5)).ToString();
                                det[4] = Convert.ToString(rd.GetValue(6)).ToString(); //Rayon
                                det[5] = ecode;
                                det[6] = Convert.ToString(rd.GetValue(7)).ToString(); //Lib_Rayon

                                details[i] = det;
                                i++;
                            }
                            catch (Exception ex)
                            {
                                logger.Info(ex.ToString());
                                return false;
                            }
                        }
                    }
                }
            }
            //}

            otopfacture = otopfacture.createAvoir(IDDEMANDE, LIBELLEDEMANDE, IDACHETEUR, UTILISATEURCONNECTE, datedeb, datefin, details);
            otopfacture.TVATOTAUX = otopfacture.calcultotauxtv(otopfacture.IDDEMANDE);
            IDDEMANDE = otopfacture.IDDEMANDE;
            return true;
        }
        private int getCountLineAv(int site, string datedeb, string datefin)
        {
            Int16 count = 0;
            string OracleConnectionString = ConfigurationManager.ConnectionStrings["Gold"].ConnectionString;
            using (OracleConnection conn = new OracleConnection(OracleConnectionString))
            {
                String sql = OTOPFACTURE.getCountLinesfacturesAvFromGOLD(site, datedeb, datefin);
                // logger.Info("sql count=" + sql);
                conn.Open();
                using (OracleCommand cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add(new OracleParameter("datedeb", datedeb));
                    cmd.Parameters.Add(new OracleParameter("datefin", datefin));

                    using (OracleDataReader rd = cmd.ExecuteReader())
                    {
                        while (rd.Read())
                        {
                            try
                            {
                                count = Convert.ToInt16(rd.GetValue(0));
                            }
                            catch (Exception ex)
                            {
                                logger.Info("sql count=" + sql);
                                logger.Info(ex.ToString());
                            }
                        }
                    }
                }
            }

            return count;
        }

        private int getCountLine(int site, string datedeb, string datefin)
        {
                    Int16 count = 0;
                    string OracleConnectionString = ConfigurationManager.ConnectionStrings["Gold"].ConnectionString;
                    using (OracleConnection conn = new OracleConnection(OracleConnectionString))
                    {
                        String sql = OTOPFACTURE.getCountLinesfacturesFromGOLD(site,datedeb,datefin);
                       // logger.Info("sql count=" + sql);
                        conn.Open();
                        using (OracleCommand cmd = new OracleCommand(sql, conn))
                        {
                            cmd.Parameters.Add(new OracleParameter("datedeb", datedeb));
                            cmd.Parameters.Add(new OracleParameter("datefin", datefin));

                            using (OracleDataReader rd = cmd.ExecuteReader())
                            {
                                while (rd.Read())
                                {
                                    try
                                    {
                                        count = Convert.ToInt16(rd.GetValue(0));
                                    }
                                    catch (Exception ex)
                                    {
                                        logger.Info("sql count=" + sql);
                                        logger.Info(ex.ToString());
                                    }
                                }
                            }
                        }
                    }

                    return count;
        }



        public ActionResult ExportExcelConsulter(Boolean keepFilters = false, int page = 1, string search = "", int sortby = 0, Boolean isasc = true, string advSearch = "")
        {
            OTOPFACTURE facture = new OTOPFACTURE();
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int iduser = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

            ICollection<INTEGBROUILLARD> listINTEGRER_BROUILLARD = facture.consulterBrouillard(0, iduser);
            if (listINTEGRER_BROUILLARD.Count <= 0)
            {
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

                // creation de le tableau des donnees
                DataTable aDt = new DataTable();

                //remplir de l entete de tableau des donnees
                aDt.Columns.Add(new DataColumn("N° lot", typeof(Double)));
                aDt.Columns.Add(new DataColumn("Num. Pièce", typeof(Double)));
                aDt.Columns.Add(new DataColumn("Réf. facture", typeof(String)));
                aDt.Columns.Add(new DataColumn("Client", typeof(string)));
                aDt.Columns.Add(new DataColumn("Cnuf", typeof(string)));
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
                        aDt.Rows.Add(item.NO_LOT, item.CROID, item.REF_FACT, item.CLIENT, item.CNUF, item.ANNEE_FISCALE, item.PERIOD, item.DATE_FACTURE,
                            item.MNT_NEG, item.MNT_POS, Decimal.Round(item.EQUILIBRE,1), detitem.SEQUENCE, detitem.COMPTE, detitem.MONTANT,
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
                                int nbreCellulesMergee = Convert.ToInt32((double)aDt.Rows[(i - ligneDetail) - 1][18]);
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

                            if ((encienCROID != (double)aDt.Rows[i - ligneDetail][1]) || (ancienIdfacture != (double)aDt.Rows[i - ligneDetail][19]) || (ancienclient != (string)aDt.Rows[i - ligneDetail][20]))
                            {
                                if (encienCROID != 0)
                                {
                                    int nbreCellulesMergee = Convert.ToInt32((double)aDt.Rows[(i - ligneDetail) - 1][18]);
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
                                ancienIdfacture = (double)aDt.Rows[i - ligneDetail][19];
                                ancienclient = (string)aDt.Rows[i - ligneDetail][20];
                            }
                            else
                            {
                                row.Cells[row.Cells.Count - 1].SetCellValue("");
                                //row.Cells[row.Cells.Count - 2].SetCellValue("");
                                j = 8;
                                continue;
                            }
                        }

                        if (j != 18 && j != 19 && j != 20)
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


    }
}
