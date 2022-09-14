using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Configuration;
using Domain.appFacture;
using appFacture.Models;
using System.Text;
using System.Web.Script.Serialization;
using System.Security.Claims;
using System.Threading;
using log4net;

namespace appFacture.Controllers
{
    [Authorize(Roles = "IMPORTFACTURE")]
    public class ImportFactController : Controller
    {
        public int pageSize = 10;
        ILog logger = log4net.LogManager.GetLogger("EramGoldLogger");

        public ActionResult Index(Boolean keepFilters = false, int page = 1, string search = "", int sortby = 0, Boolean isasc = false, string advSearch = "")
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
            FACTIMPORTXLSXAdvFiltre advFiltre = new JavaScriptSerializer().Deserialize<FACTIMPORTXLSXAdvFiltre>(advSearch);
            if (advFiltre == null)
            {
                advFiltre = new FACTIMPORTXLSXAdvFiltre();
            }
            FACTIMPORTXLSX Fichierxlsx = new FACTIMPORTXLSX();
            FACTIMPORTXLSXPaginationRes result = Fichierxlsx.getAll(page, pageSize, search, sortby, isasc, advFiltre);
            TempData["page"] = page;
            TempData["search"] = search;
            TempData["sortby"] = sortby;
            TempData["isasc"] = isasc;
            TempData["advSearch"] = advSearch;
            var pagingInfo = new InfosPagination()
            {
                CurrentPage = page,
                ItemsPerPage = pageSize,
                TotalItems = result.count
            };
            //ViewBag.listEnseignes = getEnseignes();
            return View(new ImportCmdListView() { PagingInfo = pagingInfo, fichiers = result.listIMPORTXLSX, Search = search, SortBy = sortby, IsAsc = isasc, AdvSearch = advSearch, AdvSearchFilters = advFiltre });
        }

        public List<FACTENSEIGNE> getEnseignes()
        {
            FACTENSEIGNE ens = new FACTENSEIGNE();
            List<FACTENSEIGNE> listEnseignes = ens.getAll();
            listEnseignes.Insert(0, new FACTENSEIGNE(-1, "Sélectionner une enseigne"));
            return listEnseignes;
        }

        public void keepFilters()
        {
            TempData.Keep("page");
            TempData.Keep("search");
            TempData.Keep("sortby");
            TempData.Keep("isasc");
            TempData.Keep("advSearch");
        }

        [NoCache]
        public ViewResult Create()
        {
            keepFilters();
            FACTSITE Site = new FACTSITE();
            ViewBag.listEnseignes = getEnseignes();
            return View("Edit", Site);
        }

        public ViewResult View(int id)
        {
            keepFilters();
            FACTSITE Site = new FACTSITE();
            return View(Site.getOne(id));
        }

        [NoCache]
        public ViewResult Edit(int id)
        {
            keepFilters();
            FACTSITE Site = new FACTSITE();
            Site = Site.getOne(id);
            ViewBag.listEnseignes = getEnseignes();
            return View(Site);
        }


        [HttpPost]
        public ActionResult Delete(int id)
        {
            TempData["state"] = "3";
            FACTIMPORTXLSX fichier = new FACTIMPORTXLSX();
            try
            {
                fichier=fichier.getOne(id);

                if (fichier.NOMFICHINTERNE!=null)
                {
                    var path = Path.Combine(ConfigurationManager.AppSettings["targetFolderImpExcel"], fichier.NOMFICHINTERNE);
                    //logger.Error("path a deleter =" + path);
                    if (System.IO.File.Exists(path))
                    {
                        System.IO.File.Delete(path);
                    }
                }
                
                fichier.delete(id);
                TempData["success"] = "true";
                TempData["message"] = string.Format("Le fichier {0} a bien été supprimé !", fichier.NOMFICHIER);
            }
            catch (Exception ex)
            {
                TempData["success"] = "false";
                TempData["message"] = string.Format("Erreur system : {0}", ex.Message);
            }
            return RedirectToAction("Index", new { keepFilters = true });
        }

        //[HttpPost]
        //public ActionResult Annuler(int id)
        //{
        //    COMMANDEACHAT cmd = new COMMANDEACHAT();
        //    List<COMMANDEACHAT> cmds=cmd.getAllByfichier(id);
        //    bool annuler = true;
        //    foreach(COMMANDEACHAT item in cmds){
        //        if (item.STATUS != 1 || item.DATEENVOIERAM.HasValue)
        //        {
        //            annuler = false;
        //            break;
        //        }
        //    }
        //    if (annuler)
        //    {
        //        try
        //        {
        //            IMPORTCMDINTEG imp = new IMPORTCMDINTEG();
        //            imp.deleteByFichier(id);
        //            cmd.deleteByFich(id);
        //            IMPORTXLSX impxlsx = new IMPORTXLSX();
        //            impxlsx.updateStatus(id,3);

        //            TempData["success"] = "true";
        //            TempData["message"] = string.Format("L'integration des commandes a bien été annulée !");
        //        }
        //        catch (Exception ex)
        //        {
        //            TempData["success"] = "false";
        //            TempData["message"] = string.Format("Impossible d'annuler l'integration, erreur system", ex.Message);
        //        }
        //    }
        //    else {
        //        TempData["success"] = "false";
        //        TempData["message"] = string.Format("Impossible d'annuler l'integration commande déja validée !!!");
        //    }
        //    return RedirectToAction("Index", new { keepFilters = true });
        //}

        [HttpPost]
        public ActionResult upload(HttpPostedFileBase file)
        {
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int IDUTILISATEUR = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            
            // Verify that the user selected a file
            if (file != null && file.ContentLength > 0)
            {
                // extract only the filename
                var fileName = Path.GetFileName(file.FileName);
                //logger.Error("fileName=" + fileName);
                // store the file inside ~/App_Data/uploads folder
                //var path = Path.Combine(Server.MapPath(@"~/App_Data/uploads"), fileName);

                FACTIMPORTXLSX Fichier = new FACTIMPORTXLSX();
               Fichier=Fichier.update(0, fileName,"", false,0,0, IDUTILISATEUR);
               
               logger.Error("fileName=" + Fichier.IDFICHIER);
               if (Fichier.IDFICHIER != 0) { 
                   var fileNameInterne = Fichier.IDFICHIER + Path.GetExtension(file.FileName);
                   var path = Path.Combine(ConfigurationManager.AppSettings["targetFolderImpExcel"], fileNameInterne);
                   Fichier.update(Fichier.IDFICHIER, fileName, fileNameInterne, false, 0, 0, IDUTILISATEUR);
                   file.SaveAs(path);
               }
            }
            else
            {
                logger.Error(" Pas de fichier. ");
            }
            // redirect back to the index action to show the form once again
            return RedirectToAction("Index", new { keepFilters = true });
        }
        
        public void ExportExcel(long idFichier)
        {
            FACTIMPORTXLSX fichier = new FACTIMPORTXLSX();
                fichier = fichier.getOne(idFichier);
                var fileName = fichier.NOMFICHIER;
                if (fichier.NOMFICHINTERNE == null) {
                    return;
                }
                var path = Path.Combine(ConfigurationManager.AppSettings["targetFolderImpExcel"], fichier.NOMFICHINTERNE);
                if (!System.IO.File.Exists(path)) {
                    return;
                }
                using (FileStream file = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    byte[] bytes = new byte[file.Length];
                    file.Read(bytes, 0, (int)file.Length);
                    //exportData.Write(bytes, 0, (int)file.Length);
                    //byte[] bytes = exportData.ToArray();
                    Response.Clear();
                    Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    Response.AddHeader("Content-Length", bytes.Length.ToString());
                    Response.AddHeader("Content-disposition", "attachment; filename=" + fileName);
                    Response.BinaryWrite(bytes);
                    Response.Flush();
                    Response.End();
                }
        }
        //public ActionResult ArticleRejetee(int idFichier = 0, Boolean keepFilters = false, int page = 1, string search = "", int sortby = 0, Boolean isasc = true, string advSearch = "")
        //{


        //    if (idFichier != 0)
        //    {
        //        TempData["idFichier"] = idFichier;
        //    }
        //    else
        //    {
        //        int idFichierout;
        //        int.TryParse(TempData["idFichier"].ToString(), out idFichierout);
        //        idFichier = idFichierout;
        //        TempData["idFichier"] = idFichier;
        //    }

        //    if (keepFilters)
        //    {
        //        //vérifier si on a une donnée TempData
        //        if (TempData["page"] != null)
        //        {
        //            int pageTemp;
        //            int.TryParse(TempData["page"].ToString(), out pageTemp);
        //            page = pageTemp;
        //        }
        //        if (TempData["search"] != null)
        //        {
        //            search = TempData["search"].ToString();
        //        }
        //        if (TempData["sortby"] != null)
        //        {
        //            int sortbyTemp;
        //            int.TryParse(TempData["sortby"].ToString(), out sortbyTemp);
        //            sortby = sortbyTemp;
        //        }
        //        if (TempData["isasc"] != null)
        //        {
        //            Boolean isascTemp;
        //            Boolean.TryParse(TempData["isasc"].ToString(), out isascTemp);
        //            isasc = isascTemp;
        //        }
        //        if (TempData["advSearch"] != null)
        //        {
        //            advSearch = TempData["advSearch"].ToString();
        //        }
        //    }
        //    INTDETCMDAdvFiltre advFiltre = new JavaScriptSerializer().Deserialize<INTDETCMDAdvFiltre>(advSearch);
        //    if (advFiltre == null)
        //    {
        //        advFiltre = new INTDETCMDAdvFiltre();
        //    }
        //    INTDETCMD lingeRejetee = new INTDETCMD();
        //    INTDETCMDPaginationRes result = FACTIMPORTXLSX.getAllArticleRejete(idFichier, page, pageSize, search, sortby, isasc, advFiltre);
        //    TempData["page"] = page;
        //    TempData["search"] = search;
        //    TempData["sortby"] = sortby;
        //    TempData["isasc"] = isasc;
        //    TempData["advSearch"] = advSearch;
        //    var pagingInfo = new InfosPagination()
        //    {
        //        CurrentPage = page,
        //        ItemsPerPage = pageSize,
        //        TotalItems = result.count
        //    };
        //    FACTIMPORTXLSX entete = new FACTIMPORTXLSX();
        //    entete = entete.getOne(idFichier);
        //    //ViewBag.listEnseignes = getEnseignes();
        //    //if (result.listINTDETCMD.Any())
        //    //{
        //    //    ViewBag.nomFichier = result.listINTDETCMD.First().IMPORTXLSX.NOMFICHIER;
        //    //}

        //    return View(new IntDetCmdListView() { PagingInfo = pagingInfo, LignesRejetees = result.listINTDETCMD,Entete=entete, Search = search, SortBy = sortby, IsAsc = isasc, AdvSearch = advSearch, AdvSearchFilters = advFiltre });
        //}
   
    }
}
