using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Domain.appFacture
{

    static class Extensions
    {
        /// <summary>
        /// Get substring of specified number of characters on the right.
        /// </summary>
        //public static string Right(this string value, int length)
        //{
        //    return value.Substring(value.Length - length);
        //}

        public static KeyValuePair<bool, List<long>> isCompteJustAdmin(string cookieValue)
        {
            //vérification du compte : SuperAdmin ou Admin
            string login = FormsAuthentication.Decrypt(cookieValue).Name.Split('_')[0];
            FACTUSER user = new FACTUSER();
            user = user.getOne(login);
            FACTGROUPE grp = new FACTGROUPE();
            List<FACTGROUPE> listeGroupe = grp.getAll(user.IDUTILISATEUR);
            List<long> liste = new List<long>();
            bool isAdmin = true;
            if ((from c in listeGroupe where (c.LIBELLEGROUPE == "Siege Comptable" || c.LIBELLEGROUPE == "Admin") select c).Count() == 0)
            {
                isAdmin = false;
                //le compte n est pas uniquement Admin, on restreint l'affichage des profils à ceux des magasins que gère cet utilisateur
                FACTSITE site = new FACTSITE();
                List<FACTSITE> listeSite = site.getAll(user.IDUTILISATEUR);
                foreach (FACTSITE item in listeSite)
                    liste.Add(item.IDSITE);
            }
            return new KeyValuePair<bool, List<long>>(isAdmin, liste);

        }

        
    }

    public class NoCacheAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            filterContext.HttpContext.Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            filterContext.HttpContext.Response.Cache.SetValidUntilExpires(false);
            filterContext.HttpContext.Response.Cache.SetRevalidation(HttpCacheRevalidation.AllCaches);
            filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            filterContext.HttpContext.Response.Cache.SetNoStore();

            base.OnResultExecuting(filterContext);
        }
    }
}
