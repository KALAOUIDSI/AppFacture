using Domain.appFacture;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Security.Claims;
using System.IdentityModel.Tokens;
using System.IdentityModel.Services;
using System.Web.Helpers;

using System.Security.Principal;

namespace MVCUI.appFacture
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            string l4net = Server.MapPath("~/log4net.config");
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new System.IO.FileInfo(l4net));
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.Name;
            AreaRegistration.RegisterAllAreas();
            ModelBinders.Binders.Add(typeof(decimal), new DecimalModelBinder());
            ModelBinders.Binders.Add(typeof(decimal?), new DecimalModelBinder());
            WebApiConfig.Register(GlobalConfiguration.Configuration);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
        }

        protected void FormsAuthentication_OnAuthenticate(Object sender, FormsAuthenticationEventArgs e)
        {
            if ((!(Request.Path.EndsWith("ico") | Request.Path.EndsWith("js") | Request.Path.EndsWith("css") | Request.Path.EndsWith("png") | Request.Path.EndsWith("gif") | Request.Path.EndsWith("jpg"))) && FormsAuthentication.CookiesSupported == true)
            {
              
                if (Request.Cookies[FormsAuthentication.FormsCookieName] != null)
                {

                    //let us take out the username now                
                    string login = FormsAuthentication.Decrypt(Request.Cookies[FormsAuthentication.FormsCookieName].Value).Name;
                    FACTUSER user = new FACTUSER();
                    user = user.getOne(login.Split('_')[0]);
                    if (user != null)
                    {
                        string IDSITE = login.Split('_')[1];
                        FACTSITE site = new FACTSITE();
                        bool goodSite = false;
                        List<FACTSITE> listeSite = site.getAll(user.IDUTILISATEUR);
                        if (IDSITE != null && IDSITE != "-1")
                        {
                            //on vérifie que le site fait bien partie des sites associé à l'utilisateur
    
                            foreach (FACTSITE s in listeSite)
                            {
                                if (s.IDSITE.ToString() == IDSITE)
                                {
                                    goodSite = true;
                                    break;
                                }
                            }

                        }
                        else
                        {
                            if (listeSite.Count() == 1)
                            {
                                goodSite = true;
                                IDSITE = listeSite.First().IDSITE.ToString();
                            }
                        }

                        IList<Claim> claimCollection;

                        if (goodSite)
                        {
                            site = site.getOne(int.Parse(IDSITE));
                            claimCollection = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Name, login.Split('_')[0]),
                                    new Claim(ClaimTypes.NameIdentifier, user.IDUTILISATEUR.ToString()),
                                    new Claim(ClaimTypes.SerialNumber, (site.IDSITE).ToString()),
                                    new Claim(ClaimTypes.Actor, (site.LIBELLESITE).ToString())
                                };
                        }
                        else
                            claimCollection = new List<Claim>
                                {
                                    new Claim(ClaimTypes.Name, login.Split('_')[0]),
                                    new Claim(ClaimTypes.NameIdentifier, user.IDUTILISATEUR.ToString()),
                                    new Claim(ClaimTypes.SerialNumber, (user.IDSITE.HasValue?user.IDSITE.Value:0).ToString()),
                                    new Claim(ClaimTypes.Actor, (user.IDSITE.HasValue?user.FACTSITE.LIBELLESITE:"").ToString())
                                };
                        List<string> roles = new List<string>();
                        FACTGROUPE grp = new FACTGROUPE();
                        List<FACTGROUPE> listeGroupe = grp.getAll(user.IDUTILISATEUR);
                        List<long> liste = new List<long>();
                        foreach (FACTGROUPE item in listeGroupe) {
                            liste.Add(item.IDGROUPE);
                            if (item.LIBELLEGROUPE.Equals("Siege Comptable") || item.LIBELLEGROUPE.Equals("Admin"))
                            {
                                claimCollection.Add(new Claim(ClaimTypes.Role, "SIEGE"));
                            }
                        }

                        OTOPGROUPE Otopgrp = new OTOPGROUPE();
                        List<OTOPGROUPE> listeOtopGroupe = Otopgrp.getAll(user.IDUTILISATEUR);
                        foreach (OTOPGROUPE itemOt in listeOtopGroupe)
                        {
                            claimCollection.Add(new Claim(ClaimTypes.Role, itemOt.LIBELLEGRP));
                        }

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

                        e.User = cp;
                    }


                }
            }
        }

        //public override string GetVaryByCustomString(HttpContext context, string arg)
        //{
        //    if (arg == "Login")
        //    {
        //        return Request.Cookies[FormsAuthentication.FormsCookieName].Value;
        //    }
        //    else
        //    {
        //        return base.GetVaryByCustomString(context, arg);
        //    }

        //}


        //protected void FormsAuthentication_PostAuthenticateRequest()
        //{
        //    if ( Request.Path.ToLower() == ("/Home/Login").ToLower() && Request.IsAuthenticated)
        //    {

        //        WindowsIdentity iisIdentity =   Request.LogonUserIdentity;
        //        if (iisIdentity != null)
        //        {
        //            //if (iisIdentity.IsAnonymous)
        //            //{
        //            //    this.Context.User = new WindowsPrincipal(WindowsIdentity.GetAnonymous());
        //            //}
        //            //else
        //            //{
        //            //    this.Context.User = new WindowsPrincipal(iisIdentity);
        //            //}
        //        }
        //    }
        //}
    }
}