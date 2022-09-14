using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Data.Entity;
using System.Reflection;

namespace Domain.appFacture
{
    public partial class FACTUSER
    {
        void OnCreated()
        {
        }

        public List<FACTUSER> getAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTUSER
                             select c;
                return result.Include("FACTGROUPE").Include("FACTSITE").Include("FACTSITE1").ToList();
            }
        }

        public List<FACTUSER> getAll(int idGroupe)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTUSER
                             where c.FACTGROUPE.Any(u => u.IDGROUPE == idGroupe)
                             select c;
                return result.Include("FACTGROUPE").Include("FACTSITE").Include("FACTSITE1").ToList();
            }
        }
        public int countAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTUSER
                             select c;
                return result.Count();
            }
        }

        public int countAll(List<long> SiteRestriction)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTUSER
                             where !c.FACTGROUPE.Any(u => u.LIBELLEGROUPE == "SuperAdmin" | u.LIBELLEGROUPE == "Commercial" | u.LIBELLEGROUPE == "Administrateur") 
                             select c;
                return result.Count();
            }
        }

        public int countAllUser(List<long> SiteRestriction)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTUSER
                             where c.FACTSITE1.Any(u => SiteRestriction.Contains(u.IDSITE)) 
                             select c;
                return result.Count();
            }
        }

        public List<FACTUSER> getAllFromGroups(List<long> listGroups)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTUSER
                             where c.FACTGROUPE.Any(u => listGroups.Contains(u.IDGROUPE))
                             select c;
                return result.ToList();
            }
        }

        public List<FACTUSER> getAllFromGroupsForMail(List<long> listGroups, short idcategorie)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTUSER
                             where c.FACTGROUPE.Any(u => listGroups.Contains(u.IDGROUPE))
                             && c.FACTTYPE.Any(w => w.IDCATEGORIE == idcategorie)
                             select c;
                return result.ToList();
            }
        }


        public bool isIdValid(long id, List<long> SiteRestriction)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTUSER
                              select c).Include("FACTGROUPE").Include("FACTSITE").Include("FACTSITE1");
                if (SiteRestriction != null)
                    result = from c in result where !c.FACTGROUPE.Any(u => u.LIBELLEGROUPE == "SuperAdmin" | u.LIBELLEGROUPE == "Commercial" | u.LIBELLEGROUPE == "Administrateur") select c;
                else
                    result = from c in result where 1 != 1 select c;
                result = from c in result where c.IDUTILISATEUR == id select c;
                if (result.Count() > 0)
                    return true;
                else
                    return false;

            }
        }

        public UTILISATEURPaginationRes getAll(int page, int pageSize, string search, int sortby, Boolean isasc, UTILISATEURAdvFiltre AdvFiltre, bool adminRestriction, List<long> SiteRestriction)
        {
            using (EramEntities DB = new EramEntities())
            {
                UTILISATEURPaginationRes res = new UTILISATEURPaginationRes();

                var result = (from c in DB.FACTUSER
                              select c).Include("FACTGROUPE").Include("FACTSITE").Include("FACTSITE1");
                if (!adminRestriction)
                {
                    //ids.Contains(c.IDSITE)
                        //result = from c in result where !c.FACTGROUPE.Any(u => u.LIBELLEGROUPE == "SuperAdmin" | u.LIBELLEGROUPE == "Commercial" | u.LIBELLEGROUPE == "Administrateur") select c;
                    if (SiteRestriction != null)
                        result = from c in result where (c.FACTSITE1.Any(d => SiteRestriction.Contains(d.IDSITE)) ) select c;
                    else
                        result = from c in result where 1 != 1 select c;
                }
                //recherche normale
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    int searchid;
                    if (int.TryParse(search, out searchid))
                        result = from c in result where c.NOM.ToLower().Contains(search.Trim()) || c.PRENOM.ToLower().Contains(search.Trim()) || c.EMAIL.ToLower().Contains(search.Trim()) || c.LOGIN.ToLower().Contains(search.Trim()) || c.PASSWORD.ToLower().Contains(search.Trim()) || c.IDUTILISATEUR == searchid select c;
                    else
                        result = from c in result where c.NOM.ToLower().Contains(search.Trim()) || c.PRENOM.ToLower().Contains(search.Trim()) || c.EMAIL.ToLower().Contains(search.Trim()) || c.LOGIN.ToLower().Contains(search.Trim()) || c.PASSWORD.ToLower().Contains(search.Trim()) select c;
                }

                //recherche avancée
                if (!string.IsNullOrWhiteSpace(AdvFiltre.nom))
                    result = result.Where(c => c.NOM.ToLower().Contains(AdvFiltre.nom.ToLower()));

                if (!string.IsNullOrWhiteSpace(AdvFiltre.prenom))
                    result = result.Where(c => c.PRENOM.ToLower().Contains(AdvFiltre.prenom.ToLower()));

                if (!string.IsNullOrWhiteSpace(AdvFiltre.email))
                    result = result.Where(c => c.EMAIL.ToLower().Contains(AdvFiltre.email.ToLower()));

                if (!string.IsNullOrWhiteSpace(AdvFiltre.login))
                    result = result.Where(c => c.LOGIN.ToLower().Contains(AdvFiltre.login.ToLower()));

                if (!string.IsNullOrWhiteSpace(AdvFiltre.site))
                    result = result.Where(c => c.FACTSITE.LIBELLESITE.ToLower().Contains(AdvFiltre.site.ToLower()));

                if (!string.IsNullOrWhiteSpace(AdvFiltre.password))
                    result = result.Where(c => c.PASSWORD.ToLower().Contains(AdvFiltre.password.ToLower()));


                if (!string.IsNullOrWhiteSpace(AdvFiltre.actif))
                {
                    if (AdvFiltre.actif == "0")
                        result = result.Where(c => c.ACTIF == 0);
                    if (AdvFiltre.actif == "1")
                        result = result.Where(c => c.ACTIF == 1);
                }

                if (!string.IsNullOrWhiteSpace(AdvFiltre.groupe))
                {
                    int idGroupe = 0;
                    int.TryParse(AdvFiltre.groupe, out idGroupe);
                    if (idGroupe > 0)
                        result = result.Where(c => c.FACTGROUPE.Any(d => d.IDGROUPE == idGroupe));
                }

                // tri
                switch (sortby)
                {
                    case 1:
                        result = isasc ? result.OrderBy(c => c.NOM) : result.OrderByDescending(c => c.NOM);
                        break;
                    case 2:
                        result = isasc ? result.OrderBy(c => c.PRENOM) : result.OrderByDescending(c => c.PRENOM);
                        break;
                    case 3:
                        result = isasc ? result.OrderBy(c => c.EMAIL) : result.OrderByDescending(c => c.EMAIL);
                        break;
                    case 4:
                        result = isasc ? result.OrderBy(c => c.LOGIN) : result.OrderByDescending(c => c.LOGIN);
                        break;
                    //case 5:
                    //    result = isasc ? result.OrderBy(c => c.PASSWORD) : result.OrderByDescending(c => c.PASSWORD);
                    //    break;
                    case 6:
                        result = isasc ? result.OrderBy(c => c.FACTSITE.LIBELLESITE) : result.OrderByDescending(c => c.FACTSITE.LIBELLESITE);
                        break;
                    case 7:
                        result = isasc ? result.OrderBy(c => c.ACTIF) : result.OrderByDescending(c => c.ACTIF);
                        break;
                    case 0:
                        result = result.OrderByDescending(c => c.IDUTILISATEUR);
                        break;
                }

                res.count = result.Count();
                res.listUTILISATEUR = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToList();

                return res;
            }
        }

        public FACTUSER getOne(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTUSER
                              where c.IDUTILISATEUR == id
                              select c).Include("FACTGROUPE").Include("FACTSITE").Include("FACTSITE1").Include("FACTTYPE").First();
                return result;

            }
        }
        public FACTUSER getOne(string login)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTUSER
                              where c.LOGIN.ToLower() == login.ToLower()
                              select c).Include("FACTGROUPE").Include("FACTSITE").Include("FACTSITE1").Include("FACTTYPE").FirstOrDefault();
                return result;

            }
        }
        public bool exists(string login)
        {
            using (EramEntities DB = new EramEntities())
            {
                var count = (from c in DB.FACTUSER
                             where c.LOGIN.ToLower() == login.ToLower()
                             select c).Count();
                if (count > 0)
                    return true;
                else
                    return false;

            }
        }

        public FACTUSER getOne(string login, string password)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTUSER
                              where c.LOGIN.ToLower() == login.ToLower() && c.PASSWORD == password && c.ACTIF == 1
                              select c).Include("FACTGROUPE").Include("FACTSITE").Include("FACTSITE1").FirstOrDefault();
                return result;

            }
        }

        public void update(int IDUTILISATEUR, string NOM, string PRENOM, string EMAIL, string LOGIN, string PASSWORD, bool ACTIF, string listChapitres, string listSites, ICollection<long> checkedGroupes, int UTILISATEURCONNECTE)
        {
            if (IDUTILISATEUR != 0)
            {

                //modification
                using (EramEntities DB = new EramEntities())
                {
                    FACTUSER FACTUSER = new FACTUSER();
                    FACTUSER = (from c in DB.FACTUSER
                                   where c.IDUTILISATEUR == IDUTILISATEUR
                                   select c).First();
                    FACTUSER.FACTGROUPE.Clear();
                    if (checkedGroupes != null)
                    {
                        FACTUSER.FACTGROUPE = (from c in DB.FACTGROUPE
                                              where checkedGroupes.Contains(c.IDGROUPE)
                                              select c).ToList();
                    }
                    FACTUSER.DATEMODIFICATION = DateTime.Now;
                    FACTUSER.NOM = NOM;
                    FACTUSER.PRENOM = PRENOM;
                    FACTUSER.EMAIL = EMAIL;
                    FACTUSER.LOGIN = LOGIN;
                    FACTUSER.IDSITE = IDSITE;
                    FACTUSER.PASSWORD = PASSWORD;
                    if (ACTIF)
                        FACTUSER.ACTIF = 1;
                    else
                        FACTUSER.ACTIF = 0;

                    FACTUSER.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                    int idSite;
                    if (int.TryParse(listSites, out idSite) && idSite > -1)
                        FACTUSER.IDSITE = idSite;
                    else
                        FACTUSER.IDSITE = null;
                    FACTUSER.FACTSITE1.Clear();
                    if (listSites != null && !string.IsNullOrEmpty(listSites))
                    {
                        List<int> listeSite = listSites.Split(',').Select(n => int.Parse(n)).ToList();
                        FACTUSER.FACTSITE1 = (from c in DB.FACTSITE
                                          where listeSite.Contains(c.IDSITE)
                                          select c).ToList();
                    }
                    //////////**************Chapitres **/////////////
                    FACTUSER.FACTTYPE.Clear();
                    if (listChapitres != null && !string.IsNullOrEmpty(listChapitres))
                    {
                        List<int> listeChaps = listChapitres.Split(',').Select(n => int.Parse(n)).ToList();
                        FACTUSER.FACTTYPE = (from c in DB.FACTTYPE
                                             where listeChaps.Contains(c.IDFACTTYPE)
                                             select c).ToList();
                    }
                    //////////**************Chapitres **/////////////
                    DB.Entry(FACTUSER).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }

            }
            else
            {

                using (EramEntities DB = new EramEntities())
                {
                    FACTUSER FACTUSER = new FACTUSER();
                    FACTUSER.DATECREATION = DateTime.Now;
                    FACTUSER.NOM = NOM;
                    FACTUSER.PRENOM = PRENOM;
                    FACTUSER.EMAIL = EMAIL;
                    FACTUSER.LOGIN = LOGIN;
                    FACTUSER.IDSITE = IDSITE;
                    FACTUSER.PASSWORD = PASSWORD;
                    if (ACTIF)
                        FACTUSER.ACTIF = 1;
                    else
                        FACTUSER.ACTIF = 0;

                    FACTUSER.DERNIERUTILISATEUR = UTILISATEURCONNECTE;

                    int idSite;
                    if (int.TryParse(listSites, out idSite) && idSite > -1)
                        FACTUSER.IDSITE = idSite;
                    else
                        FACTUSER.IDSITE = null;
                    if (listSites != null && !string.IsNullOrEmpty(listSites))
                    {
                        List<int> listeSite = listSites.Split(',').Select(n => int.Parse(n)).ToList();
                        FACTUSER.FACTSITE1 = (from c in DB.FACTSITE
                                          where listeSite.Contains(c.IDSITE)
                                          select c).ToList();
                    }
                    if (checkedGroupes != null)
                    {
                        FACTUSER.FACTGROUPE = (from c in DB.FACTGROUPE
                                              where checkedGroupes.Contains(c.IDGROUPE)
                                              select c).ToList();
                    }
                    //////////**************Chapitres **/////////////
                    if (listChapitres != null && !string.IsNullOrEmpty(listChapitres))
                    {
                        List<int> listeChaps = listChapitres.Split(',').Select(n => int.Parse(n)).ToList();
                        FACTUSER.FACTTYPE = (from c in DB.FACTTYPE
                                             where listeChaps.Contains(c.IDFACTTYPE)
                                             select c).ToList();
                    }
                    //////////**************Chapitres **/////////////
                    DB.FACTUSER.Add(FACTUSER);
                    DB.SaveChanges();

                }
            }
        }

        public void delete(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTUSER FACTUSER = new FACTUSER();
                FACTUSER = (from c in DB.FACTUSER
                               where c.IDUTILISATEUR == id
                               select c).FirstOrDefault();
                //on supprime la relation avec les groupes
                FACTUSER.FACTGROUPE.Clear();
                //on supprime la relation avec les magasins
                FACTUSER.FACTSITE1.Clear();
                //on supprime la relation avec les chapitres
                FACTUSER.FACTTYPE.Clear();
                DB.Entry(FACTUSER).State = System.Data.EntityState.Deleted;
                DB.SaveChanges();

            }
        }
    }
}
