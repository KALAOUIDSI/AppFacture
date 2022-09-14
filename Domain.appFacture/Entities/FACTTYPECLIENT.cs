using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Data.Entity;

namespace Domain.appFacture
{
    public partial class FACTTYPECLIENT
    {
        void OnCreated()
        {
        }

        public FACTTYPECLIENT(int id, string value)
        {
            this.TYPE = (short)id;
            this.LIBELLE = value;
        }

        public List<FACTTYPECLIENT> getAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                IQueryable<FACTTYPECLIENT> result = from c in DB.FACTTYPECLIENT
                                            orderby c.TYPE ascending 
                                            select c;
                return result.Include("FACTDEMANDE").ToList();
            }
        }


        public List<FACTTYPECLIENT> getAll(long IdDemande)
        {
            using (EramEntities DB = new EramEntities())
            {
                IQueryable<FACTTYPECLIENT> result = from c in DB.FACTTYPECLIENT
                                              where c.FACTDEMANDE.Any(u => u.IDDEMANDE == IdDemande)
                                            select c;
                return result.ToList();
            }
        }

        public int countAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = from c in DB.FACTTYPECLIENT
                             select c;
                return result.Count();
            }
        }
        public FACTTYPEPaginationRes getAll(int page, int pageSize, string search, int sortby, Boolean isasc, FACTTYPEAdvFiltre AdvFiltre)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTTYPEPaginationRes res = new FACTTYPEPaginationRes();
                IQueryable<FACTTYPE> result = (from c in DB.FACTTYPE
                                                 select c).Include("FACTDEMANDE");
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    int searchid;
                    if (int.TryParse(search, out searchid))
                        result = from c in result where c.LIBELLE.ToLower().Contains(search) || c.IDFACTTYPE == searchid select c;
                    else
                        result = from c in result where c.LIBELLE.ToLower().Contains(search) select c;
                }

                //recherche avancée
                if (!string.IsNullOrWhiteSpace(AdvFiltre.libelle))
                    result = result.Where(c => c.LIBELLE.ToLower().Contains(AdvFiltre.libelle.ToLower()));

                if (!string.IsNullOrWhiteSpace(AdvFiltre.compte))
                    result = result.Where(c => c.COMPTE.ToLower().Contains(AdvFiltre.compte.ToLower()));

                switch (sortby)
                {
                    case 1:
                        result = isasc ? result.OrderBy(c => c.LIBELLE) : result.OrderByDescending(c => c.LIBELLE);
                        break;
                    case 0:
                        result = result.OrderByDescending(c => c.IDFACTTYPE);
                        break;
                }
                res.count = result.Count();
                res.listTYPE = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize).Include("FACTDEMANDE").ToList();

                return res;
            }
        }

        public FACTTYPECLIENT getOne(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTTYPECLIENT FACTTYPECLIENT = (from c in DB.FACTTYPECLIENT
                                 where c.TYPE == id
                                     select c).Include("FACTDEMANDE").First();
                return FACTTYPECLIENT;
            }
        }

        public void update(int IDFACTTYPE, string LIBELLEFACTTYPE,string COMPTE)
        {
            if (IDFACTTYPE != 0)
            {
                //modification
                using (EramEntities DB = new EramEntities())
                {
                    FACTTYPE FACTTYPE = (from c in DB.FACTTYPE
                                             where c.IDFACTTYPE == IDFACTTYPE
                                     select c).First();
                    FACTTYPE.LIBELLE = LIBELLEFACTTYPE;
                    FACTTYPE.COMPTE = COMPTE;
                    
                    DB.Entry(FACTTYPE).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }

            }
            else
            {
                using (EramEntities DB = new EramEntities())
                {
                    //DB.Configuration.ValidateOnSaveEnabled = false;
                    FACTTYPE FACTTYPE = new FACTTYPE();

                    FACTTYPE.LIBELLE = LIBELLEFACTTYPE;
                    FACTTYPE.COMPTE = COMPTE;

                    DB.FACTTYPE.Add(FACTTYPE);
                    DB.SaveChanges();
                }
            }
        }

        public void delete(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTTYPE FACTTYPE = new FACTTYPE();
                FACTTYPE = (from c in DB.FACTTYPE
                          where c.IDFACTTYPE == id
                          select c).First();
                if (FACTTYPE.FACTDEMANDE.Count == 0)
                {
                    DB.Entry(FACTTYPE).State = System.Data.EntityState.Deleted;
                    DB.SaveChanges();
                }
                else
                {
                    throw new Exception("Suppression impossible, des demandes utilisent ce TYPE FACTURE !");
                }

            }
        }
    }
}
