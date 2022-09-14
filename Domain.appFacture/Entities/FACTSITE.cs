using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Data.Entity;

namespace Domain.appFacture
{
    public partial class FACTSITE
    {
        void OnCreated()
        {
        }

        public FACTSITE(int id, string value)
        {
            this.IDSITE = id;
            this.LIBELLESITE = value;
        }

        public List<FACTSITE> getAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTSITE
                             select c;
                return result.Include("FACTENSEIGNE").ToList();
            }
        }

        public List<FACTSITE> getAllOrdonnes()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTSITE
                             select c;
                return result.Include("FACTENSEIGNE").OrderBy(c => c.LIBELLESITE).ToList();
            }
        }

        public List<FACTSITE> getRestricted(List<long> ids)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTSITE
                             where ids.Contains(c.IDSITE)
                             select c;
                return result.Include("FACTENSEIGNE").ToList();
            }
        }
        public List<FACTSITE> getAll(long IdUtilisateur)
        {
            using (EramEntities DB = new EramEntities())
            {
                IQueryable<FACTSITE> result = from c in DB.FACTSITE
                                          where c.FACTUSER1.Any(u => u.IDUTILISATEUR == IdUtilisateur)
                                          select c;
                return result.ToList();
            }
        }

        public int countAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTSITE
                             select c;
                return result.Count();
            }
        }

        public SITEPaginationRes getAll(int page, int pageSize, string search, int sortby, Boolean isasc, SITEAdvFiltre AdvFiltre)
        {
            using (EramEntities DB = new EramEntities())
            {
                SITEPaginationRes res = new SITEPaginationRes();
                var result = (from c in DB.FACTSITE
                              select c).Include("FACTENSEIGNE");
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    int searchid;
                    if (int.TryParse(search, out searchid))
                        result = from c in result where c.LIBELLESITE.ToLower().Contains(search.Trim()) || c.FACTENSEIGNE.LIBELLEENSEIGNE.ToLower().Contains(search.Trim()) || c.CODEAGRESSO.ToLower().Contains(search.Trim()) || c.IDSITE == searchid || c.CODEGOLD == searchid select c;
                    else
                        result = from c in result where c.LIBELLESITE.ToLower().Contains(search.Trim()) || c.FACTENSEIGNE.LIBELLEENSEIGNE.ToLower().Contains(search.Trim()) || c.CODEAGRESSO.ToLower().Contains(search.Trim()) select c;
                }

                //recherche avancée
                if (!string.IsNullOrWhiteSpace(AdvFiltre.libellesite))
                    result = result.Where(c => c.LIBELLESITE.ToLower().Contains(AdvFiltre.libellesite.ToLower()));
                if (!string.IsNullOrWhiteSpace(AdvFiltre.idenseigne))
                {
                    int idenseigne = 0;
                    int.TryParse(AdvFiltre.idenseigne, out idenseigne);
                    if (idenseigne > 0)
                        result = result.Where(c => c.IDENSEIGNE == idenseigne);
                }

                if (!string.IsNullOrWhiteSpace(AdvFiltre.adrfact))
                    result = result.Where(c => (c.ADRLINE1.ToLower().Contains(AdvFiltre.adrfact.ToLower()) || c.ADRLINE2.ToLower().Contains(AdvFiltre.adrfact.ToLower())));
                if (!string.IsNullOrWhiteSpace(AdvFiltre.codegold))
                {
                    int codegold;
                    if (int.TryParse(AdvFiltre.codegold, out codegold))
                        result = result.Where(c => c.CODEGOLD == codegold);
                }
                if (!string.IsNullOrWhiteSpace(AdvFiltre.codeagresso))
                    result = result.Where(c => c.CODEAGRESSO.ToLower().Contains(AdvFiltre.codeagresso.ToLower()));
                //if (!string.IsNullOrWhiteSpace(AdvFiltre.ice))
                //    result = result.Where(c => c.ICE.ToLower().Contains(AdvFiltre.ice.ToLower()));
                switch (sortby)
                {
                    case 1:
                        result = isasc ? result.OrderBy(c => c.LIBELLESITE) : result.OrderByDescending(c => c.LIBELLESITE);
                        break;
                    case 2:
                        result = isasc ? result.OrderBy(c => c.FACTENSEIGNE.LIBELLEENSEIGNE) : result.OrderByDescending(c => c.FACTENSEIGNE.LIBELLEENSEIGNE);
                        break;
                    case 3:
                        result = isasc ? result.OrderBy(c => c.CODEGOLD) : result.OrderByDescending(c => c.CODEGOLD);
                        break;
                    case 4:
                        result = isasc ? result.OrderBy(c => c.CODEAGRESSO) : result.OrderByDescending(c => c.CODEAGRESSO);
                        break;
                    case 5:
                        result = isasc ? result.OrderBy(c => c.ADRLINE1) : result.OrderByDescending(c => c.ADRLINE1);
                        break;
                    case 6:
                        result = isasc ? result.OrderBy(c => c.DIM1) : result.OrderByDescending(c => c.DIM1);
                        break;
                    case 7:
                        result = isasc ? result.OrderBy(c => c.DIM4) : result.OrderByDescending(c => c.DIM4);
                        break;
                    case 0:
                        result = result.OrderByDescending(c => c.IDSITE);
                        break;
                }
                res.count = result.Count();
                res.listSITE = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToList();

                return res;
            }
        }

        public FACTSITE getOne(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTSITE
                             where c.IDSITE == id
                             select c;

                return result.Include("FACTENSEIGNE").First();

            }
        }

        public FACTSITE getOneSsEns(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTSITE
                             where c.IDSITE == id
                             select c;

                return result.First();

            }
        }

        public FACTSITE getFromCodeGold(int codeGold)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTSITE
                             where c.CODEGOLD == codeGold
                             select c;
                if (result.Count() == 0)
                    return null;
                return result.Include("FACTENSEIGNE").First();

            }
        }

        public void update(int IDSITE, string LIBELLESITE, int IDENSEIGNE, int CODEGOLD, string CODEAGRESSO,string DIM1,string DIM4, string ADRLINE1, string ADRLINE2, string ICE, int? IDUTILISATEUR = null)
        {
            if (IDSITE != 0)
            {

                //modification
                using (EramEntities DB = new EramEntities())
                {
                    FACTSITE Site = new FACTSITE();
                    Site = Site.getOneSsEns(IDSITE);
                    Site.DATEMODIFICATION = DateTime.Now;
                    Site.LIBELLESITE = LIBELLESITE;
                    Site.IDENSEIGNE = IDENSEIGNE;
                    Site.CODEGOLD = CODEGOLD;
                    Site.CODEAGRESSO = CODEAGRESSO;
                    Site.DIM1=DIM1;
                    Site.DIM4=DIM4;
                    Site.DERNIERUTILISATEUR = IDUTILISATEUR;
                    Site.ADRLINE1 = ADRLINE1;
                    Site.ADRLINE2 = ADRLINE2;
                    //Site.ICE = ICE;
                    DB.Entry(Site).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }

            }
            else
            {

                using (EramEntities DB = new EramEntities())
                {
                    DB.Configuration.ValidateOnSaveEnabled = false;
                    FACTSITE Site = new FACTSITE();
                    Site.DATECREATION = DateTime.Now;
                    Site.LIBELLESITE = LIBELLESITE;                    
                    Site.IDENSEIGNE = IDENSEIGNE;
                    Site.CODEGOLD = CODEGOLD;
                    Site.CODEAGRESSO = CODEAGRESSO;
                    Site.DIM1=DIM1;
                    Site.DIM4=DIM4;
                    Site.DERNIERUTILISATEUR = IDUTILISATEUR;
                    Site.ADRLINE1 = ADRLINE1;
                    Site.ADRLINE2 = ADRLINE2;
                    //Site.ICE = ICE;
                    DB.FACTSITE.Add(Site);
                    DB.SaveChanges();
                }
            }
        }

        public void updateDerniereExec(int IDSITE,DateTime Dernieredate)
        {
                using (EramEntities DB = new EramEntities())
                {
                    FACTSITE Site = new FACTSITE();
                    Site = Site.getOne(IDSITE);
                    Site.DATEDERNIEREEXECUTION = Dernieredate;
                    DB.Entry(Site).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }
        }


        public void delete(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTSITE Site = new FACTSITE();
                Site = (from c in DB.FACTSITE
                        where c.IDSITE == id
                        select c).FirstOrDefault();
                //on vérifie que le site n'est pas utilisé ailleurs
                if (!(Site.FACTUSER.Count > 0 | Site.FACTUSER1.Count > 0))
                {
                    DB.Entry(Site).State = System.Data.EntityState.Deleted;
                    DB.SaveChanges();
                }
                else
                {
                    throw new Exception("Impossible de supprimer ce magasin, il est utilisé sur d'autres éléments du site !");
                }
            }
        }
    }
}
