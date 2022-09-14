using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;

namespace Domain.appFacture
{
    public partial class FACTENSEIGNE
    {
        void OnCreated()
        {
        }

        public FACTENSEIGNE(int id, string value)
        {
            this.IDENSEIGNE = id;
            this.LIBELLEENSEIGNE = value;
        }

        public List<FACTENSEIGNE> getAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                return (from c in DB.FACTENSEIGNE select c).ToList();
            }
        }

        public int countAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTENSEIGNE
                             select c;
                return result.Count();
            }
        }

        public FACTENSEIGNE getOne(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTENSEIGNE
                              where c.IDENSEIGNE == id 
                              select c).First();
                return result;
            }
        }

        public bool verifUniciteICE(string ice,int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTENSEIGNE
                             where c.ICE == ice && c.IDENSEIGNE != id
                             select c;
                return result.Count()==0;
            }
        }

        public FACTASSOGFACTTYPEENSEIGNE getOneAssoFactEns(int idtypefact,int idens)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTASSOGFACTTYPEENSEIGNE
                              where c.IDENSEIGNE == idens && c.IDFACTTYPE == idtypefact
                              select c);
                if (result != null && result.Count()>0)
                {
                    return result.First();
                }else 
                    return null;
            }
        }

        public ENSEIGNEPaginationRes getAll(int page, int pageSize, string search, int sortby, Boolean isasc, ENSEIGNEAdvFiltre AdvFiltre)
        {
            using (EramEntities DB = new EramEntities())
            {
                ENSEIGNEPaginationRes res = new ENSEIGNEPaginationRes();
                IQueryable<FACTENSEIGNE> result = (from c in DB.FACTENSEIGNE
                                                 select c);
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    int searchid;
                    if (int.TryParse(search, out searchid))
                        result = from c in result where c.LIBELLEENSEIGNE.ToLower().Contains(search.Trim()) || c.IDENSEIGNE == searchid select c;
                    else
                        result = from c in result where c.LIBELLEENSEIGNE.ToLower().Contains(search.Trim()) || c.CODE.ToLower().Contains(search.Trim()) select c;
                }

                //recherche avancée
                if (!string.IsNullOrWhiteSpace(AdvFiltre.libelle))
                    result = result.Where(c => c.LIBELLEENSEIGNE.ToLower().Contains(AdvFiltre.libelle.ToLower()));

                if (!string.IsNullOrWhiteSpace(AdvFiltre.code))
                    result = result.Where(c => c.CODE.ToLower().Contains(AdvFiltre.code.ToLower()));

                if (!string.IsNullOrWhiteSpace(AdvFiltre.ice))
                    result = result.Where(c => c.ICE.ToLower().Contains(AdvFiltre.ice.ToLower()));


                switch (sortby)
                {
                    case 2:
                        result = isasc ? result.OrderBy(c => c.CODE) : result.OrderByDescending(c => c.CODE);
                        break;
                    case 1:
                        result = isasc ? result.OrderBy(c => c.LIBELLEENSEIGNE) : result.OrderByDescending(c => c.LIBELLEENSEIGNE);
                        break;
                    case 3:
                        result = isasc ? result.OrderBy(c => c.ICE) : result.OrderByDescending(c => c.ICE);
                        break;
                    case 0:
                        result = result.OrderByDescending(c => c.IDENSEIGNE);
                        break;
                }
                res.count = result.Count();
                res.listENSEIGNE = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToList();

                return res;
            }
        }

        public void update(int IDENSEIGNE, string LIBELLEENSEIGNE, string CODE, string ICE, string PLINE1, string PLINE2, string PLINE3, string PLINE4, int UTILISATEURCONNECTE)
        {
            if (IDENSEIGNE != 0)
            {
                //modification
                using (EramEntities DB = new EramEntities())
                {
                    FACTENSEIGNE ens = new FACTENSEIGNE();
                    ens = ens.getOne(IDENSEIGNE);
                    ens.DATEMODIFICATION = DateTime.Now;
                    ens.LIBELLEENSEIGNE = LIBELLEENSEIGNE;
                    ens.CODE = CODE;
                    ens.ICE = ICE;
                    ens.PLINE1 = PLINE1;
                    ens.PLINE2 = PLINE2;
                    ens.PLINE3 = PLINE3;
                    ens.PLINE4 = PLINE4;
                    ens.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                    DB.Entry(ens).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }

            }
            else
            {

                using (EramEntities DB = new EramEntities())
                {
                    DB.Configuration.ValidateOnSaveEnabled = false;
                    FACTENSEIGNE ens = new FACTENSEIGNE();
                    ens.DATECREATION = DateTime.Now;
                    ens.DATEMODIFICATION = DateTime.Now;
                    ens.LIBELLEENSEIGNE = LIBELLEENSEIGNE;
                    ens.CODE = CODE;
                    ens.ICE = ICE;
                    ens.PLINE1 = PLINE1;
                    ens.PLINE2 = PLINE2;
                    ens.PLINE3 = PLINE3;
                    ens.PLINE4 = PLINE4;
                    ens.DERNIERUTILISATEUR = UTILISATEURCONNECTE;
                    DB.FACTENSEIGNE.Add(ens);
                    DB.SaveChanges();
                }
            }
        }

        public void delete(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTENSEIGNE Ens = new FACTENSEIGNE();
                Ens = (from c in DB.FACTENSEIGNE
                        where c.IDENSEIGNE == id
                        select c).FirstOrDefault();
                //on vérifie que le site n'est pas utilisé ailleurs
                if (!(Ens.FACTSITE.Count > 0 | Ens.FACTSITE.Count > 0) && !(Ens.FACTASSOGFACTTYPEENSEIGNE.Count > 0 | Ens.FACTASSOGFACTTYPEENSEIGNE.Count > 0))
                {
                    DB.Entry(Ens).State = System.Data.EntityState.Deleted;
                    DB.SaveChanges();
                }
                else
                {
                    throw new Exception("Impossible de supprimer cet enseigne, il est utilisé sur d'autres éléments du site ou chapitre !");
                }
            }
        }


    }
}
