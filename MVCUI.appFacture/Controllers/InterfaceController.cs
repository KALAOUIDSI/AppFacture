using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Domain.appFacture;
using appFacture.Models;
using System.Text;

namespace appFacture.Controllers
{
    [Authorize(Roles = "INTERFACE")]
    public class InterfaceController : Controller
    {
        public int pageSize = 10;

        public ActionResult Index(Boolean keepFilters = false, int page = 1, string search = "", int sortby = 0, Boolean isasc = true)
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
            }
            FACTINTERFACE FACTINTERFACE = new FACTINTERFACE();
            INTERFACEPaginationRes result = FACTINTERFACE.getAll(page, pageSize, search, sortby, isasc);
            TempData["page"] = page;
            TempData["search"] = search;
            TempData["sortby"] = sortby;
            TempData["isasc"] = isasc;
            var pagingInfo = new InfosPagination()
            {
                CurrentPage = page,
                ItemsPerPage = pageSize,
                TotalItems = result.count
            };
            return View(new InterfaceListView() { PagingInfo = pagingInfo, Interfaces = result.listINTERFACE, Search = search, SortBy = sortby, IsAsc = isasc });
        }

        [NoCache]
        public ViewResult Create()
        {
            FACTINTERFACE FACTINTERFACE = new FACTINTERFACE();
            TempData.Keep("page");
            TempData.Keep("search");
            TempData.Keep("sortby");
            TempData.Keep("isasc");
            return View("Edit", FACTINTERFACE);
        }

        public ViewResult View(int id)
        {
            FACTINTERFACE FACTINTERFACE = new FACTINTERFACE();
            TempData.Keep("page");
            TempData.Keep("search");
            TempData.Keep("sortby");
            TempData.Keep("isasc");
            return View(FACTINTERFACE.getOne(id));
        }

        [NoCache]
        public ViewResult Edit(int id)
        {
            FACTINTERFACE FACTINTERFACE = new FACTINTERFACE();
            TempData.Keep("page");
            TempData.Keep("search");
            TempData.Keep("sortby");
            TempData.Keep("isasc");
            return View(FACTINTERFACE.getOne(id));
        }

        [HttpPost]
        public ActionResult Edit(int IDINTERFACE, string LIBELLEINTERFACE)
        {
            bool isCreated = IDINTERFACE == 0 ? true : false;
            if (isCreated)
                TempData["state"] = "1";
            else
                TempData["state"] = "2";

            try
            {
                FACTINTERFACE FACTINTERFACE = new FACTINTERFACE();
                FACTINTERFACE.update(IDINTERFACE, LIBELLEINTERFACE);
                TempData["success"] = "true";
                if (isCreated)
                    TempData["message"] = string.Format("L'FACTINTERFACE {0} a bien été crée !", FACTINTERFACE.LIBELLEINTERFACE);
                else
                    TempData["message"] = string.Format("L'FACTINTERFACE {0} a bien été modifié !", FACTINTERFACE.LIBELLEINTERFACE);
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
                FACTINTERFACE FACTINTERFACE = new FACTINTERFACE();
                FACTINTERFACE.delete(id);
                TempData["success"] = "true";
                TempData["message"] = string.Format("L'FACTINTERFACE {0} a bien été supprimé !", FACTINTERFACE.LIBELLEINTERFACE);
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
