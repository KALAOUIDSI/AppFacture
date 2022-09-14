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
    [Authorize(Roles = "ENSEIGNE")]
    public class EnseigneController : Controller
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
            ENSEIGNEAdvFiltre advFiltre = new JavaScriptSerializer().Deserialize<ENSEIGNEAdvFiltre>(advSearch);
            if (advFiltre == null)
            {
                advFiltre = new ENSEIGNEAdvFiltre();
            }
            FACTENSEIGNE ENSEIGNE = new FACTENSEIGNE();
            ENSEIGNEPaginationRes result = ENSEIGNE.getAll(page, pageSize, search, sortby, isasc, advFiltre);
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
            return View(new EnseigneListView() { PagingInfo = pagingInfo, Enseignes = result.listENSEIGNE, Search = search, SortBy = sortby, IsAsc = isasc, AdvSearch = advSearch, AdvSearchFilters = advFiltre });
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
            FACTENSEIGNE FACTENSEIGNE = new FACTENSEIGNE();
            return View("Edit", FACTENSEIGNE);
        }

        public ViewResult View(int id)
        {
            keepFilters();
            FACTENSEIGNE FACTENSEIGNE = new FACTENSEIGNE();
            return View(FACTENSEIGNE.getOne(id));
        }

        [NoCache]
        public ViewResult Edit(int id)
        {
            keepFilters();
            FACTENSEIGNE FACTENSEIGNE = new FACTENSEIGNE();
            return View(FACTENSEIGNE.getOne(id));
        }

        public List<FACTENSEIGNE> getlistenseigne()
        {
            FACTENSEIGNE FACTENSEIGNE = new FACTENSEIGNE();
            return FACTENSEIGNE.getAll();
        }

        [HttpPost]
        public ActionResult Edit(int IDENSEIGNE, string LIBELLEENSEIGNE, string CODE, string ICE, string PLINE1, string PLINE2, string PLINE3, string PLINE4)
        {
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);

            FACTENSEIGNE factenseigne = new FACTENSEIGNE();

            bool isCreated = IDENSEIGNE == 0 ? true : false;
            if (isCreated)
                TempData["state"] = "1";
            else
                TempData["state"] = "2";

            try
            {
                factenseigne.update(IDENSEIGNE, LIBELLEENSEIGNE, CODE,ICE,PLINE1,PLINE2,PLINE3,PLINE4, UTILISATEURCONNECTE);
                TempData["success"] = "true";
                if (isCreated)
                    TempData["message"] = string.Format("L'enseigne {0} a bien été crée !", factenseigne.LIBELLEENSEIGNE);
                else
                    TempData["message"] = string.Format("L'enseigne {0} a bien été modifié !", factenseigne.LIBELLEENSEIGNE);
            }
            catch (Exception ex)
            {
                TempData["success"] = "false";
                TempData["message"] = string.Format("Erreur system : {0}", ex.Message);
            }

            return RedirectToAction("Index", new { keepFilters = true });
        }

        [HttpPost]
        public bool isIceUnique(string ICE, int IDENSEIGNE)
        {
            FACTENSEIGNE fctens = new FACTENSEIGNE();
            return fctens.verifUniciteICE(ICE, IDENSEIGNE);
        }

        [HttpPost]
        public ActionResult Delete(int id)
        {
            TempData["state"] = "3";
            try
            {
                FACTENSEIGNE factenseigne = new FACTENSEIGNE();
                factenseigne.delete(id);
                TempData["success"] = "true";
                TempData["message"] = string.Format("L'enseigne {0} a bien été supprimé !", factenseigne.LIBELLEENSEIGNE);
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
