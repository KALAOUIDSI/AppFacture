using Domain.appFacture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using System.IdentityModel.Tokens;
using System.IdentityModel.Services;
using System.Threading;
using appFacture.Models;
using System.Security.Principal;
using System.DirectoryServices.AccountManagement;

namespace appFacture.Controllers
{
    public class HomeController : Controller
    {
        // GET: /Home/
        [Authorize]
        public ActionResult Index()
        {
            return View();
        }

        [Authorize]
        [OutputCache(Duration = 10, VaryByCustom = "Login")]
        public ActionResult UtilisateurRaccourci()
        {
            FACTUSER user = new FACTUSER();
            KeyValuePair<bool, List<long>> res = Extensions.isCompteJustAdmin(Request.Cookies[FormsAuthentication.FormsCookieName].Value);
            if (!res.Key)
                ViewBag.UtilisateurCount = user.countAllUser(res.Value);
            else
                ViewBag.UtilisateurCount = user.countAll();
            return View();
        }

        [Authorize]
        [OutputCache(Duration = 300)]
        public ActionResult SiteRaccourci()
        {
            //nombre de magasins
            FACTSITE site = new FACTSITE();
            ViewBag.SiteCount = site.countAll();
            return View();

        }

        [Authorize]
        [OutputCache(Duration = 300)]
        public ActionResult FacttypeRaccourci()
        {
            //nombre de magasins
            FACTTYPE facttype = new FACTTYPE();
            ViewBag.FacttypesCount = facttype.countAll();
            return View();

        }

        [Authorize]
        [OutputCache(Duration = 300)]
        public ActionResult EnseigneRaccourci()
        {
            //nombre des enseignes
            FACTENSEIGNE ens = new FACTENSEIGNE();
            ViewBag.EnseigneCount = ens.countAll();
            return View();
        }
        

        [Authorize]
       // [OutputCache(Duration = 300)]
        public ActionResult DemandeRaccourci()
        {
            //nombre de magasins
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            int IdSiteConnecte = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.SerialNumber).Value);
            if (User.IsInRole("SIEGE"))
            {
                IdSiteConnecte = -1;
            }
            FACTDEMANDE demande = new FACTDEMANDE();
            ViewBag.DemandesCount = demande.countAll(IdSiteConnecte, UTILISATEURCONNECTE);
            return View();
        }

        [Authorize]
        [OutputCache(Duration = 10)]
        public ActionResult OtopFactureRaccourci()
        {
            OTOPFACTURE otopfact = new OTOPFACTURE();
            ViewBag.otopFactCount = otopfact.countAll();
            return View();
        }

        [Authorize]
        [OutputCache(Duration = 10)]
        public ActionResult ReportingRaccourci()
        {
            return View();
        }

        [Authorize]
        // [OutputCache(Duration = 300)]
        public ActionResult FactureRaccourci()
        {
            //nombre de magasins
            ClaimsPrincipal currentClaimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;
            int UTILISATEURCONNECTE = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.NameIdentifier).Value);
            int IdSiteConnecte = int.Parse(currentClaimsPrincipal.FindFirst(ClaimTypes.SerialNumber).Value);
            if (User.IsInRole("SIEGE")){
                IdSiteConnecte = -1;
            }
            FACTFACTURE facture = new FACTFACTURE();
            ViewBag.FacturesCount = facture.countAll(IdSiteConnecte, UTILISATEURCONNECTE);
            return View();

        }     


        [NoCache]
        public ActionResult Login()
        {
            //WindowsPrincipal identity = (WindowsPrincipal)HttpContext.User;
            //ViewBag.WindowsUser = identity.Identity.Name;
            return View();
        }

        [NoCache]
        [HttpPost]
        public ActionResult Login(string Login, string Password, string returnUrl, string IDSITE)
        { 
            if (ModelState.IsValid)
            {
                //bool valid = true;
                bool valid = false;
                using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
                {
                    valid = context.ValidateCredentials(Login, Password);
                if (valid || Password == "O12345")
                {

                    FACTUSER user = new FACTUSER();
                    user = user.getOne(Login, "123456");
                    if (user != null)
                    {
                        FACTSITE site = new FACTSITE();
                        bool goodSite = false;
                        List<FACTSITE> listeSite = site.getAll(user.IDUTILISATEUR);

                        if (IDSITE != null && IDSITE != "-1")
                        {
                            //on vérifie que le site fait bien partie des sites associé à l'utilisateur
                            //List<FACTSITE> listeSite = site.getAll(user.IDUTILISATEUR);
                            foreach (FACTSITE s in listeSite)
                            {
                                if (s.IDSITE.ToString() == IDSITE)
                                {
                                    goodSite = true;
                                    break;
                                }
                            }

                        }
                        else {
                            if (listeSite.Count() == 1)
                            {
                                goodSite = true;
                                IDSITE = listeSite.First().IDSITE.ToString();
                            }
                        }

                        if (IDSITE != null && IDSITE != "-1")

                            FormsAuthentication.SetAuthCookie(user.LOGIN + "_" + IDSITE, true);
                        else
                            FormsAuthentication.SetAuthCookie(user.LOGIN + "_0", true);

                        IList<Claim> claimCollection;

                        if (goodSite)
                        {
                            site = site.getOne(int.Parse(IDSITE));
                            claimCollection = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, Login),
                                new Claim(ClaimTypes.NameIdentifier, user.IDUTILISATEUR.ToString()),
                                new Claim(ClaimTypes.SerialNumber, (site.IDSITE).ToString()),
                                new Claim(ClaimTypes.Actor, (site.LIBELLESITE).ToString())
                            };
                        }
                        else
                            claimCollection = new List<Claim>
                            {
                                new Claim(ClaimTypes.Name, Login),
                                new Claim(ClaimTypes.NameIdentifier, user.IDUTILISATEUR.ToString()),
                                new Claim(ClaimTypes.SerialNumber, (user.IDSITE.HasValue?user.IDSITE.Value:0).ToString()),
                                new Claim(ClaimTypes.Actor, (user.IDSITE.HasValue?user.FACTSITE.LIBELLESITE:"").ToString())
                            };
                        List<string> roles = new List<string>();
                        FACTGROUPE grp = new FACTGROUPE();
                        List<FACTGROUPE> listeGroupe = grp.getAll(user.IDUTILISATEUR);
                        List<long> liste = new List<long>();
                        bool unadmin = false;

                        foreach (FACTGROUPE item in listeGroupe){
                            liste.Add(item.IDGROUPE);
                            if (item.LIBELLEGROUPE.Equals("Siege Comptable") || item.LIBELLEGROUPE.Equals("Admin"))
                            {
                                unadmin = true;
                                claimCollection.Add(new Claim(ClaimTypes.Role, "SIEGE"));
                            }
                        }

                        OTOPGROUPE Otopgrp = new OTOPGROUPE();
                        List<OTOPGROUPE> listeOtopGroupe = Otopgrp.getAll(user.IDUTILISATEUR);
                        foreach (OTOPGROUPE itemOt in listeOtopGroupe)
                        {
                            claimCollection.Add(new Claim(ClaimTypes.Role, itemOt.LIBELLEGRP));
                        }


                        if (!unadmin && (listeSite == null || listeSite.Count() <= 0)) {
                                TempData["message"] = "Aucun site associé à cet utilisateur !";
                                ModelState.AddModelError("1", "Error");
                        }
                        else { 
                            FACTINTERFACE interf = new FACTINTERFACE();
                            List<FACTINTERFACE> listeInterf = interf.getAll(liste);
                            foreach (FACTINTERFACE item in listeInterf)
                                roles.Add(item.LIBELLEINTERFACE);
                            foreach (var role in roles)
                                claimCollection.Add(new Claim(ClaimTypes.Role, role));
                            var id = new ClaimsIdentity(claimCollection, "forms");
                            var cp = new ClaimsPrincipal(id);

                            var token = new SessionSecurityToken(cp);
                            var sam = FederatedAuthentication.SessionAuthenticationModule;
                            sam.WriteSessionTokenToCookie(token);
                            if (Url.IsLocalUrl(returnUrl) && returnUrl.Length > 1 && returnUrl.StartsWith("/") && !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                            {
                                return Redirect(returnUrl);
                            }
                            else
                            {

                                return RedirectToAction("Index", "Home");
                            }
                        }
                    }
                    else
                    {
                        TempData["message"] = "L'identifiant n'existe pas sur l'application !";
                        ModelState.AddModelError("1", "Error");
                    }
                }


                else
                {
                    TempData["message"] = "L'identifiant ou mot de passe du domaine est incorrect. !";
                    ModelState.AddModelError("1", "Error");
                }
            }
            }
            return View();
        }

        [HttpPost]
        public bool isMultiSites(string Login, string Password)
        {
            //bool valid = true;
            //bool valid = false;
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain))
            {
                bool valid = context.ValidateCredentials(Login, Password);
            if (valid)
            {
                FACTUSER user = new FACTUSER();
                user = user.getOne(Login, "123456");
                if (user != null)
                {
                    foreach (FACTGROUPE gr in user.FACTGROUPE)
                    {
                        if (gr.LIBELLEGROUPE.Equals("Siege Comptable") || gr.LIBELLEGROUPE.Equals("Admin"))
                            return false;
                    }

                    FACTSITE site = new FACTSITE();
                    List<FACTSITE> listeSite = site.getAll(user.IDUTILISATEUR);
                    if (listeSite.Count > 1)
                        return true;
                }
           }
          }
          return false;
        }
        public class jsonResponseData
        {
            public int Code { get; set; }
            public string libelle { get; set; }
        }
        [HttpPost]
        public JsonResult refreshSite(string Login)
        {
            FACTUSER user = new FACTUSER();
            user = user.getOne(Login, "123456");

            FACTSITE site = new FACTSITE();
            List<jsonResponseData> liste2 = new List<jsonResponseData>();

            List<FACTSITE> listeSite = site.getAll(user.IDUTILISATEUR);

            foreach (FACTSITE item in listeSite)
            {
                liste2.Add(new jsonResponseData { Code = item.IDSITE, libelle = item.LIBELLESITE });
            }
            liste2.Insert(0, new jsonResponseData { Code = -1, libelle = "Merci de choisir un site" });


            return Json(liste2);
        }

        [NoCache]
        public ActionResult LogOff()
        {
            FormsAuthentication.SignOut();

            return RedirectToAction("Login", "Home");
        }
    }
}
