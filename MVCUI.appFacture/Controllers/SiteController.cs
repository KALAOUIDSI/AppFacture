using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Domain.appFacture;
using appFacture.Models;
using System.Text;
using System.Web.Script.Serialization;
using System.Security.Claims;
using System.Threading;

namespace appFacture.Controllers
{
    [Authorize(Roles = "SITE")]
    public class SiteController : Controller
    {
        public int pageSize = 10;

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
            SITEAdvFiltre advFiltre = new JavaScriptSerializer().Deserialize<SITEAdvFiltre>(advSearch);
            if (advFiltre == null)
            {
                advFiltre = new SITEAdvFiltre();
            }
            FACTSITE Site = new FACTSITE();
            SITEPaginationRes result = Site.getAll(page, pageSize, search, sortby, isasc, advFiltre);
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
            ViewBag.listEnseignes = getEnseignes();
            return View(new SiteListView() { PagingInfo = pagingInfo, Sites = result.listSITE, Search = search, SortBy = sortby, IsAsc = isasc, AdvSearch = advSearch, AdvSearchFilters = advFiltre });
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
        public ActionResult Edit(int IDSITE, string LIBELLESITE, int IDENSEIGNE, int? CODEGOLD, string DIM1, string DIM4, string ADRLINE1, string ADRLINE2, string CODEAGRESSO)
        {
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int IDUTILISATEUR = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            CODEGOLD = -1;
            bool isCreated = IDSITE == 0 ? true : false;
            if (isCreated)
                TempData["state"] = "1";
            else
                TempData["state"] = "2";

            try
            {
                FACTSITE Site = new FACTSITE();
                Site.update(IDSITE, LIBELLESITE, IDENSEIGNE,CODEGOLD.Value, CODEAGRESSO,DIM1,DIM4, ADRLINE1,ADRLINE2,"", IDUTILISATEUR);
                TempData["success"] = "true";
                if (isCreated)
                    TempData["message"] = string.Format("Le magasin {0} a bien été crée !", Site.LIBELLESITE);
                else
                    TempData["message"] = string.Format("Le magasin {0} a bien été modifié !", Site.LIBELLESITE);
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
                FACTSITE Site = new FACTSITE();
                Site.delete(id);
                TempData["success"] = "true";
                TempData["message"] = string.Format("Le magasin {0} a bien été supprimé !", Site.LIBELLESITE);
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
