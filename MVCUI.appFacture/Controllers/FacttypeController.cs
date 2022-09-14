using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Domain.appFacture;
using appFacture.Models;
using System.Security.Claims;
using System.Threading;
using System.Text;
using System.Web.Script.Serialization;
using log4net;

namespace appFacture.Controllers
{
    [Authorize(Roles = "TYPEFACTURE")]
    public class FacttypeController : Controller
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
            FACTTYPEAdvFiltre advFiltre = new JavaScriptSerializer().Deserialize<FACTTYPEAdvFiltre>(advSearch);
            if (advFiltre == null)
            {
                advFiltre = new FACTTYPEAdvFiltre();
            }
            FACTTYPE FACTTYPE = new FACTTYPE();
            FACTTYPEPaginationRes result = FACTTYPE.getAll(page, pageSize, search, sortby, isasc, advFiltre);
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
            //ViewBag.listInterfaces = getInterfaces2();
            return View(new FacttypeListView() { PagingInfo = pagingInfo, Types = result.listTYPE, Search = search, SortBy = sortby, IsAsc = isasc, AdvSearch = advSearch, AdvSearchFilters = advFiltre });
        }

        //public List<FACTINTERFACE> getInterfaces()
        //{
        //    FACTINTERFACE FACTINTERFACE = new FACTINTERFACE();
        //    List<FACTINTERFACE> listInterfaces = FACTINTERFACE.getAll();
        //    return listInterfaces;
        //}
        //public List<FACTINTERFACE> getInterfaces2()
        //{
        //    FACTINTERFACE FACTINTERFACE = new FACTINTERFACE();
        //    List<FACTINTERFACE> listInterfaces = FACTINTERFACE.getAll();
        //    listInterfaces.Insert(0, new FACTINTERFACE(-1, "Sélectionner une FACTINTERFACE"));
        //    return listInterfaces;
        //}

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
            FACTTYPE FACTTYPE = new FACTTYPE();
            //ViewBag.listInterfaces = getInterfaces();
            ViewBag.listenseigne = getlistenseigne();
            ViewBag.listcategories = getCategories();

            return View("Edit", FACTTYPE);
        }

        public ViewResult View(int id)
        {
            keepFilters();
            FACTTYPE FACTTYPE = new FACTTYPE();
            ViewBag.listenseigne   = getlistenseigne();
            ViewBag.listcategories = getCategories();

            return View(FACTTYPE.getOne(id));
        }

        [NoCache]
        public ViewResult Edit(int id)
        {
            keepFilters();
            FACTTYPE FACTTYPE = new FACTTYPE();
            //ViewBag.listInterfaces = getInterfaces();
            ViewBag.listenseigne   = getlistenseigne();
            ViewBag.listcategories = getCategories();

            return View(FACTTYPE.getOne(id));
        }

        public List<FACTENSEIGNE> getlistenseigne()
        {
            FACTENSEIGNE FACTENSEIGNE = new FACTENSEIGNE();
            return FACTENSEIGNE.getAll();
        }

        public List<FACTTYPECATEGORIE> getCategories()
        {
            FACTTYPE facttype = new FACTTYPE();
            return facttype.getAllCategories();
        }

        [HttpPost]
        public ActionResult Edit(int IDFACTTYPE, string LIBELLE, int IDCATEGORIE, string COMPTE)
        {
            string vcompte="";
            string vtva20 = "";
            string vtva14 = "";
            string vtva10 = "";
            string vtva7 = "";
            string vipprf = "";
            string vcomptecli = "";
            string vtsc = "";
            string vtsctaxe = "";

            string acompte = "";
            string atva20 = "";
            string atva14 = "";
            string atva10 = "";
            string atva7 = "";
            string aipprf = "";
            string acomptefrs = "";

            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            //int IdSiteConnecte = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.SerialNumber).Value);

            FACTTYPE FACTTYPE = new FACTTYPE();


            bool isCreated = IDFACTTYPE == 0 ? true : false;
            if (isCreated)
                TempData["state"] = "1";
            else
                TempData["state"] = "2";

            //try
            //{

            FACTTYPE=FACTTYPE.update(IDFACTTYPE, LIBELLE, IDCATEGORIE, COMPTE, UTILISATEURCONNECTE);
            if (isCreated)
                IDFACTTYPE = FACTTYPE.IDFACTTYPE;
            foreach (FACTENSEIGNE ens in getlistenseigne())
            {
                vcompte    = Request.Form["v1_" + ens.IDENSEIGNE];
                vtva20     = Request.Form["v2_" + ens.IDENSEIGNE];
                vtva14     = Request.Form["v3_" + ens.IDENSEIGNE];
                vtva10     = Request.Form["v4_" + ens.IDENSEIGNE];
                vtva7      = Request.Form["v5_" + ens.IDENSEIGNE];
                vipprf     = Request.Form["v6_" + ens.IDENSEIGNE];
                vcomptecli = Request.Form["v7_" + ens.IDENSEIGNE];
                vtsc       = Request.Form["v8_" + ens.IDENSEIGNE];
                vtsctaxe   = Request.Form["v9_" + ens.IDENSEIGNE];

                acompte =    Request.Form["a1_" + ens.IDENSEIGNE];
                atva20 =     Request.Form["a2_" + ens.IDENSEIGNE];
                atva14 =     Request.Form["a3_" + ens.IDENSEIGNE];
                atva10 =     Request.Form["a4_" + ens.IDENSEIGNE];
                atva7 =      Request.Form["a5_" + ens.IDENSEIGNE];
                aipprf =     Request.Form["a6_" + ens.IDENSEIGNE];
                acomptefrs = Request.Form["a7_" + ens.IDENSEIGNE];

                FACTTYPE.updateLienEnsFactTypeV(IDFACTTYPE, ens.IDENSEIGNE, vcompte, vtva20, vtva14, vtva10, vtva7, vipprf, vcomptecli,vtsc,vtsctaxe, acompte, atva20, atva14,atva10, atva7, aipprf, acomptefrs);
            }


                TempData["success"] = "true";
                if (isCreated)
                    TempData["message"] = string.Format("Le type facture {0} a bien été crée !", FACTTYPE.LIBELLE);
                else
                    TempData["message"] = string.Format("Le type facture {0} a bien été modifié !", FACTTYPE.LIBELLE);
            //}
            //catch (Exception ex)
            //{
            //    TempData["success"] = "false";
            //    TempData["message"] = string.Format("Erreur system : {0}", ex.Message);
            //}

            return RedirectToAction("Index", new { keepFilters = true });
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            TempData["state"] = "3";
            try
            {
                FACTTYPE FACTTYPE = new FACTTYPE();
                FACTTYPE.delete(id);
                TempData["success"] = "true";
                TempData["message"] = string.Format("Le type de facture {0} a bien été supprimé !", FACTTYPE.LIBELLE);
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
