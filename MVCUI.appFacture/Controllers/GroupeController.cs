using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Domain.appFacture;
using appFacture.Models;
using System.Text;
using System.Web.Script.Serialization;

namespace appFacture.Controllers
{
    [Authorize(Roles = "GROUPE")]
    public class GroupeController : Controller
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
            GROUPEAdvFiltre advFiltre = new JavaScriptSerializer().Deserialize<GROUPEAdvFiltre>(advSearch);
            if (advFiltre == null)
            {
                advFiltre = new GROUPEAdvFiltre();
            }
            FACTGROUPE FACTGROUPE = new FACTGROUPE();
            GROUPEPaginationRes result = FACTGROUPE.getAll(page, pageSize, search, sortby, isasc, advFiltre);
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
            ViewBag.listInterfaces = getInterfaces2();
            return View(new GroupeListView() { PagingInfo = pagingInfo, Groupes = result.listGROUPE, Search = search, SortBy = sortby, IsAsc = isasc, AdvSearch = advSearch, AdvSearchFilters = advFiltre });
        }

        public List<FACTINTERFACE> getInterfaces()
        {
            FACTINTERFACE FACTINTERFACE = new FACTINTERFACE();
            List<FACTINTERFACE> listInterfaces = FACTINTERFACE.getAll();
            return listInterfaces;
        }
        public List<FACTINTERFACE> getInterfaces2()
        {
            FACTINTERFACE FACTINTERFACE = new FACTINTERFACE();
            List<FACTINTERFACE> listInterfaces = FACTINTERFACE.getAll();
            listInterfaces.Insert(0, new FACTINTERFACE(-1, "Sélectionner une FACTINTERFACE"));
            return listInterfaces;
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
            FACTGROUPE FACTGROUPE = new FACTGROUPE();
            ViewBag.listInterfaces = getInterfaces();
            return View("Edit", FACTGROUPE);
        }

        public ViewResult View(int id)
        {
            keepFilters(); 
            FACTGROUPE FACTGROUPE = new FACTGROUPE();
            return View(FACTGROUPE.getOne(id));
        }

        [NoCache]
        public ViewResult Edit(int id)
        {
            keepFilters(); 
            FACTGROUPE FACTGROUPE = new FACTGROUPE();
            ViewBag.listInterfaces = getInterfaces();
            return View(FACTGROUPE.getOne(id));
        }

        [HttpPost]
        public ActionResult Edit(int IDGROUPE, string LIBELLEGROUPE, ICollection<long> checkedInterfaces)
        {
            bool isCreated = IDGROUPE == 0 ? true : false;
            if (isCreated)
                TempData["state"] = "1";
            else
                TempData["state"] = "2";

            //try
            //{
                FACTGROUPE FACTGROUPE = new FACTGROUPE();
                FACTGROUPE.update(IDGROUPE, LIBELLEGROUPE, checkedInterfaces);
                TempData["success"] = "true";
                if (isCreated)
                    TempData["message"] = string.Format("Le FACTGROUPE {0} a bien été crée !", FACTGROUPE.LIBELLEGROUPE);
                else
                    TempData["message"] = string.Format("Le FACTGROUPE {0} a bien été modifié !", FACTGROUPE.LIBELLEGROUPE);
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
                FACTGROUPE FACTGROUPE = new FACTGROUPE();
                FACTGROUPE.delete(id);
                TempData["success"] = "true";
                TempData["message"] = string.Format("Le FACTGROUPE {0} a bien été supprimé !", FACTGROUPE.LIBELLEGROUPE);
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
