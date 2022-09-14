using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Domain.appFacture;
using appFacture.Models;
using System.Text;
using System.Web.Script.Serialization;
using System.Web.Security;
using System.Security.Claims;
using System.Threading;
using log4net;

namespace appFacture.Controllers
{
    [Authorize(Roles = "UTILISATEUR")]
    public class UtilisateurController : Controller
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

            UTILISATEURAdvFiltre advFiltre = new JavaScriptSerializer().Deserialize<UTILISATEURAdvFiltre>(advSearch);
            if (advFiltre == null)
            {
                advFiltre = new UTILISATEURAdvFiltre();
            }
            KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            //appel de méthode de lecture des données
            FACTUSER FACTUSER = new FACTUSER();
            UTILISATEURPaginationRes result = FACTUSER.getAll(page, pageSize, search, sortby, isasc, advFiltre, res.Key, res.Value);
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
            ViewBag.listSites = getSites();
            List<FACTGROUPE> listGroupes = getGroupes();
            listGroupes.Insert(0, new FACTGROUPE(-1, "Sélectionner un groupe"));
            ViewBag.listGroupes = listGroupes;
            return View(new UtilisateurListView() { PagingInfo = pagingInfo, Utilisateurs = result.listUTILISATEUR, Search = search, SortBy = sortby, IsAsc = isasc, AdvSearch = advSearch, AdvSearchFilters = advFiltre });
        }

        //Liste déroulante des chapitres
        public List<FACTTYPE> getChapitres()
        {
            FACTTYPE fcttype = new FACTTYPE();
            List<FACTTYPE> listCahpitres = fcttype.getAll();
            listCahpitres.Insert(0, new FACTTYPE(-1, "Sélectionner un chapitre"));
            return listCahpitres;
        }
        
        //Liste déroulante des magasins
        public List<FACTSITE> getSites()
        {
            FACTSITE ens = new FACTSITE();
            List<FACTSITE> listSites = ens.getAll();
            listSites.Insert(0, new FACTSITE(-1, "Sélectionner un site"));
            return listSites;
        }
        //Liste déroulante des groupes
        public List<FACTGROUPE> getGroupes()
        {
            FACTGROUPE FACTGROUPE = new FACTGROUPE();
            return FACTGROUPE.getAll();
        }
        //Liste déroulante des groupes
        public List<FACTGROUPE> getRestrictedGroupes()
        {
            FACTGROUPE FACTGROUPE = new FACTGROUPE();
            return FACTGROUPE.getRestricted();
        }

        //Liste déroulante des groupes
        public List<FACTSITE> getAllSites()
        {
            FACTSITE FACTSITE = new FACTSITE();
            return FACTSITE.getAll();
        }

        //Liste déroulante des magasins
        public List<FACTSITE> getRestrictedSites(List<long> ids)
        {
            FACTSITE ens = new FACTSITE();
            List<FACTSITE> listSites = ens.getRestricted(ids);
            listSites.Insert(0, new FACTSITE(-1, "Sélectionner un site"));
            return listSites;
        }
        [NoCache]
        public ViewResult Create()
        {
            keepFilters();
            FACTUSER FACTUSER = new FACTUSER();
            FACTUSER.PASSWORD = "123456";
            KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            if (!res.Key)
            {
                ViewBag.isAdmin = false;
                ViewBag.listSites = getRestrictedSites(res.Value);
                ViewBag.listGroupes = getRestrictedGroupes();
                ViewBag.listChapitres = getChapitres();
            }
            else
            {
                ViewBag.isAdmin = false;
                ViewBag.listSites = getSites();
                ViewBag.listGroupes = getGroupes();
                ViewBag.listChapitres = getChapitres();
            }
            //ViewBag.listesites = getAllSites();
            return View("Edit", FACTUSER);
        }

        public ActionResult View(int id)
        {
            keepFilters();
            FACTUSER FACTUSER = new FACTUSER();
            KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            return View(FACTUSER.getOne(id));
        }

        [NoCache]
        [HttpPost]
        public ActionResult Edit(int id)
        {
            keepFilters();
            FACTUSER FACTUSER = new FACTUSER();
            
            KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            if (!res.Key)
            {
                ViewBag.isAdmin = false;
                FACTSITE site = new FACTSITE();
                ViewBag.listSites = site.getAll(id);
                //ViewBag.listGroupes = getGroupes();
                ViewBag.listGroupes = getRestrictedGroupes();
            }
            else
            {
                ViewBag.isAdmin = false;
                ViewBag.listSites = getSites();
                ViewBag.listGroupes = getGroupes();
            }
            ViewBag.listChapitres = getChapitres();
            //ViewBag.listSites = getSites();
            //ViewBag.listesites=getAllSites();
            FACTUSER = FACTUSER.getOne(id);
            return View(FACTUSER);
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
        public ActionResult ValidateEdit(int IDUTILISATEUR, string NOM, string PRENOM, string EMAIL, string OLDLOGIN, string LOGIN, string PASSWORD, string selectedChapitres, string selectedSites, bool ACTIF, ICollection<long> checkedGroupes)
        {
            FACTUSER FACTUSER = new FACTUSER();
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            bool isCreated = IDUTILISATEUR == 0 ? true : false;
            if (isCreated)
                TempData["state"] = "1";
            else
                TempData["state"] = "2";
            KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            //on commence par vérifier si le login est unique
            if (isCreated || OLDLOGIN!=LOGIN)
            {
                if (FACTUSER.exists(LOGIN))
                {
                    //erreur
                    keepFilters();
                    TempData["success"] = "false";
                    TempData["message"] = "Erreur : L'identifiant existe déjà, merci de choisir un autre !";
                    if (res.Key) {
                        ViewBag.isAdmin = true;
                        ViewBag.listSites = getRestrictedSites(res.Value);
                        ViewBag.listGroupes = getRestrictedGroupes();
                        }
                    else
                    {
                        ViewBag.isAdmin = false;
                        ViewBag.listSites = getSites();
                        ViewBag.listGroupes = getGroupes();
                        ViewBag.listesites = getAllSites();
                    }
                    ViewBag.listChapitres = getChapitres();
                    return View("Edit", FACTUSER);
                }
            }
            int idSite = -1;
            if (selectedSites == null || selectedSites.Length <= 0 || (int.TryParse(selectedSites, out idSite) && idSite <= -1))
            {
                //erreur lors de la création d'un compte commercial par un profil Administrateur
                keepFilters();
                TempData["success"] = "false";
                TempData["message"] = "Erreur : Vous devez choisir un magasin dans la liste déroulante";
                //ViewBag.listSites = getRestrictedSites(res.Value);
                //ViewBag.listGroupes = getRestrictedGroupes();
                if (res.Key)
                {
                    ViewBag.isAdmin = true;
                    ViewBag.listSites = getRestrictedSites(res.Value);
                    ViewBag.listGroupes = getRestrictedGroupes();
                }
                else
                {
                    ViewBag.isAdmin = false;
                    ViewBag.listSites = getSites();
                    ViewBag.listGroupes = getGroupes();
                    ViewBag.listesites = getAllSites();
                }
                ViewBag.listChapitres = getChapitres();
                return View("Edit", FACTUSER);
            }
            //try
            //{

            FACTUSER.update(IDUTILISATEUR, NOM, PRENOM, EMAIL, LOGIN, PASSWORD, ACTIF, selectedChapitres, selectedSites, checkedGroupes, UTILISATEURCONNECTE);
                TempData["success"] = "true";
                if (isCreated)
                    TempData["message"] = string.Format("L'utilisateur {0} {1} a bien été crée !", FACTUSER.PRENOM, FACTUSER.NOM);
                else
                    TempData["message"] = string.Format("L'utilisateur {0} {1} a bien été modifié !", FACTUSER.PRENOM, FACTUSER.NOM);
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
        public ActionResult Delete(int id)
        {
            TempData["state"] = "3";
            try
            {
                FACTUSER FACTUSER = new FACTUSER();
                //KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
                //if (res.Key)
                //{
                //    if (!FACTUSER.isIdValid(id, res.Value))
                //        return RedirectToAction("Index");
                //}
                FACTUSER.delete(id);
                TempData["success"] = "true";
                TempData["message"] = string.Format("L'utilisateur {0} {1} a bien été supprimé !", FACTUSER.PRENOM, FACTUSER.NOM);
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
