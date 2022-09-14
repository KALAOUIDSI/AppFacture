using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;

namespace Domain.appFacture
{
    public partial class FACTINTERFACE
    {
        void OnCreated()
        {
        }

        public FACTINTERFACE(int id, string value)
        {
            this.IDINTERFACE = id;
            this.LIBELLEINTERFACE = value;
        }

        public List<FACTINTERFACE> getAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                IQueryable<FACTINTERFACE> result = from c in DB.FACTINTERFACE
                                               orderby c.LIBELLEINTERFACE
                                            select c;
                return result.ToList();
            }
        }

        public List<FACTINTERFACE> getAll(List<long> listGroupe)
        {
            using (EramEntities DB = new EramEntities())
            {
                IQueryable<FACTINTERFACE> result = from c in DB.FACTINTERFACE
                                                   where c.FACTGROUPE.Any(u => listGroupe.Contains(u.IDGROUPE))
                                               select c;
                return result.ToList();
            }
        }

        public FACTINTERFACE getOneByLibelle(string libelle)
        {
            using (EramEntities DB = new EramEntities())
            {
                var result = (from c in DB.FACTINTERFACE
                              where c.LIBELLEINTERFACE == libelle
                              select c).First();
                return result;
            }
        }

        public INTERFACEPaginationRes getAll(int page, int pageSize, string search, int sortby, Boolean isasc)
        {
            using (EramEntities DB = new EramEntities())
            {
                INTERFACEPaginationRes res = new INTERFACEPaginationRes();
                IQueryable<FACTINTERFACE> result = from c in DB.FACTINTERFACE
                                                             select c;
                if (!string.IsNullOrEmpty(search))
                {
                    search = search.ToLower();
                    int searchid;
                    if (int.TryParse(search, out searchid))
                        result = from c in result where c.LIBELLEINTERFACE.ToLower().Contains(search) || c.IDINTERFACE == searchid select c;
                    else
                        result = from c in result where c.LIBELLEINTERFACE.ToLower().Contains(search) select c;
                }

                switch (sortby)
                {
                    case 1:
                        result = isasc ? result.OrderBy(c => c.LIBELLEINTERFACE) : result.OrderByDescending(c => c.LIBELLEINTERFACE);
                        break;
                    case 0:
                        result = result.OrderByDescending(c => c.IDINTERFACE);
                        break;
                }
                res.count = result.Count();
                res.listINTERFACE = result
                .Skip((page - 1) * pageSize)
                .Take(pageSize).ToList();

                return res;
            }
        }

        public FACTINTERFACE getOne(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTINTERFACE FACTINTERFACE = (from c in DB.FACTINTERFACE
                                 where c.IDINTERFACE == id
                                 select c).FirstOrDefault();
                return FACTINTERFACE;
            }
        }

        public void update(int IDINTERFACE, string LIBELLEINTERFACE)
        {
            if (IDINTERFACE != 0)
            {
                //modification
                using (EramEntities DB = new EramEntities())
                {
                    FACTINTERFACE FACTINTERFACE = new FACTINTERFACE();
                    FACTINTERFACE = FACTINTERFACE.getOne(IDINTERFACE);
                    FACTINTERFACE.LIBELLEINTERFACE = LIBELLEINTERFACE;
                    DB.Entry(FACTINTERFACE).State = System.Data.EntityState.Modified;
                    DB.SaveChanges();
                }

            }
            else
            {

                using (EramEntities DB = new EramEntities())
                {
                    FACTINTERFACE FACTINTERFACE = new FACTINTERFACE();

                    FACTINTERFACE.LIBELLEINTERFACE = LIBELLEINTERFACE;

                    DB.FACTINTERFACE.Add(FACTINTERFACE);
                    DB.SaveChanges();

                }
            }
        }

        public void delete(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACTINTERFACE FACTINTERFACE = new FACTINTERFACE();
                FACTINTERFACE = (from c in DB.FACTINTERFACE
                          where c.IDINTERFACE == id
                          select c).FirstOrDefault();
                if (FACTINTERFACE.FACTGROUPE.Count == 0)
                {
                    DB.Entry(FACTINTERFACE).State = System.Data.EntityState.Deleted;
                    DB.SaveChanges();
                }
                else
                    throw new Exception("Suppression impossible, des groupes sont en relation avec cette FACTINTERFACE !");

            }
        }
    }
}
