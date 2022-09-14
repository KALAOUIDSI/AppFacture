using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Data.Entity;

namespace Domain.appFacture
{
    public partial class FACTDEMANDESTATUS
    {
        void OnCreated()
        {
        }

        public FACTDEMANDESTATUS(int id, string value)
        {
            this.STATUS = (short)id;
            this.LIBELLE = value;
        }
        public List<FACTDEMANDESTATUS> getAllStatus()
        {
            using (EramEntities DB = new EramEntities())
            {
                IQueryable<FACTDEMANDESTATUS> result = from c in DB.FACTDEMANDESTATUS
                                                       select c;
                return result.ToList();
            }
        }

        //public List<FACTGROUPE> getAll()
        //{
        //    using (EramEntities DB = new EramEntities())
        //    {
        //        IQueryable<FACTGROUPE> result = from c in DB.FACTGROUPE
        //                                    orderby c.LIBELLEGROUPE 
        //                                    select c;
        //        return result.Include("FACTINTERFACE").ToList();
        //    }
        //}
        //public List<FACTGROUPE> getRestricted()
        //{
        //    using (EramEntities DB = new EramEntities())
        //    {
        //        IQueryable<FACTGROUPE> result = from c in DB.FACTGROUPE
        //                                    where c.LIBELLEGROUPE != "SuperAdmin" && c.LIBELLEGROUPE != "Commercial" && c.LIBELLEGROUPE != "Administrateur"
        //                                    orderby c.LIBELLEGROUPE
        //                                    select c;
        //        return result.Include("FACTINTERFACE").ToList();
        //    }
        //}

    //    public List<FACTGROUPE> getAll(long IdUtilisateur)
    //    {
    //        using (EramEntities DB = new EramEntities())
    //        {
    //            IQueryable<FACTGROUPE> result = from c in DB.FACTGROUPE
    //                                            where c.FACTUSER.Any(u => u.IDUTILISATEUR == IdUtilisateur)
    //                                        select c;
    //            return result.ToList();
    //        }
    //    }

    //    public GROUPEPaginationRes getAll(int page, int pageSize, string search, int sortby, Boolean isasc, GROUPEAdvFiltre AdvFiltre)
    //    {
    //        using (EramEntities DB = new EramEntities())
    //        {
    //            GROUPEPaginationRes res = new GROUPEPaginationRes();
    //            IQueryable<FACTGROUPE> result = (from c in DB.FACTGROUPE
    //                                         select c).Include("FACTINTERFACE");
    //            if (!string.IsNullOrEmpty(search))
    //            {
    //                search = search.ToLower();
    //                int searchid;
    //                if (int.TryParse(search, out searchid))
    //                    result = from c in result where c.LIBELLEGROUPE.ToLower().Contains(search) || c.IDGROUPE == searchid select c;
    //                else
    //                    result = from c in result where c.LIBELLEGROUPE.ToLower().Contains(search) select c;
    //            }

    //            //recherche avancée
    //            if (!string.IsNullOrWhiteSpace(AdvFiltre.libellegroupe))
    //                result = result.Where(c => c.LIBELLEGROUPE.ToLower().Contains(AdvFiltre.libellegroupe.ToLower()));
    //            if (!string.IsNullOrWhiteSpace(AdvFiltre.idinterface))
    //            {
    //                int idInterface = 0;
    //                int.TryParse(AdvFiltre.idinterface, out idInterface);
    //                if (idInterface > 0)
    //                    result = result.Where(c => c.FACTINTERFACE.Any(d => d.IDINTERFACE == idInterface));
    //            }

    //            switch (sortby)
    //            {
    //                case 1:
    //                    result = isasc ? result.OrderBy(c => c.LIBELLEGROUPE) : result.OrderByDescending(c => c.LIBELLEGROUPE);
    //                    break;
    //                case 0:
    //                    result = result.OrderByDescending(c => c.IDGROUPE);
    //                    break;
    //            }
    //            res.count = result.Count();
    //            res.listGROUPE = result
    //            .Skip((page - 1) * pageSize)
    //            .Take(pageSize).Include("FACTINTERFACE").ToList();

    //            return res;
    //        }
    //    }

    //    public FACTGROUPE getOne(int id)
    //    {
    //        using (EramEntities DB = new EramEntities())
    //        {
    //            FACTGROUPE FACTGROUPE = (from c in DB.FACTGROUPE
    //                             where c.IDGROUPE == id
    //                             select c).Include("FACTINTERFACE").First();
    //            return FACTGROUPE;
    //        }
    //    }

    //    public void update(int IDGROUPE, string LIBELLEGROUPE, ICollection<long> checkedInterfaces)
    //    {
    //        if (IDGROUPE != 0)
    //        {
    //            //modification
    //            using (EramEntities DB = new EramEntities())
    //            {
    //                FACTGROUPE FACTGROUPE = (from c in DB.FACTGROUPE
    //                                 where c.IDGROUPE == IDGROUPE
    //                                 select c).First();
    //                FACTGROUPE.LIBELLEGROUPE = LIBELLEGROUPE;
    //                FACTGROUPE.FACTINTERFACE.Clear();
    //                if (checkedInterfaces != null)
    //                {
    //                    FACTGROUPE.FACTINTERFACE = (from c in DB.FACTINTERFACE
    //                                        where checkedInterfaces.Contains(c.IDINTERFACE)
    //                                        select c).ToList();
    //                }
    //                DB.Entry(FACTGROUPE).State = System.Data.EntityState.Modified;
    //                DB.SaveChanges();
    //            }

    //        }
    //        else
    //        {

    //            using (EramEntities DB = new EramEntities())
    //            {
    //                //DB.Configuration.ValidateOnSaveEnabled = false;
    //                FACTGROUPE FACTGROUPE = new FACTGROUPE();

    //                FACTGROUPE.LIBELLEGROUPE = LIBELLEGROUPE;
    //                if (checkedInterfaces != null)
    //                {
    //                    FACTGROUPE.FACTINTERFACE = (from c in DB.FACTINTERFACE
    //                                        where checkedInterfaces.Contains(c.IDINTERFACE)
    //                                        select c).ToList();
    //                }
    //                DB.FACTGROUPE.Add(FACTGROUPE);
    //                DB.SaveChanges();
                    

    //            }
    //        }
    //    }

    //    public void delete(int id)
    //    {
    //        using (EramEntities DB = new EramEntities())
    //        {
    //            FACTGROUPE FACTGROUPE = new FACTGROUPE();
    //            FACTGROUPE = (from c in DB.FACTGROUPE
    //                      where c.IDGROUPE == id
    //                      select c).First();
    //            if (FACTGROUPE.FACTUSER.Count == 0)
    //            {
    //                FACTGROUPE.FACTINTERFACE.Clear();
    //                DB.Entry(FACTGROUPE).State = System.Data.EntityState.Deleted;
    //                DB.SaveChanges();
    //            }
    //            else
    //            {
    //                throw new Exception("Suppression impossible, des utilisateurs utilisent ce FACTGROUPE !");
    //            }

    //        }
    //    }
    }
}
