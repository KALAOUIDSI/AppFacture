using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;

namespace Domain.appFacture
{
    public partial class FACRAPPORTGEN
    {
        void OnCreated()
        {
        }

        public List<String> MESSAGES = new List<String>();

        public void insererFichier(int util,string message = "")
        {
            using (EramEntities DB = new EramEntities())
            {

                //foreach(FACRAPPORTGEN rapp in getAll()){
                //    DB.Entry(rapp).State = System.Data.EntityState.Deleted;
                //}
                DB.Database.ExecuteSqlCommand("DELETE FROM FACRAPPORTGEN WHERE DERNIERUTILISATEUR="+util);

                FACRAPPORTGEN f = new FACRAPPORTGEN();
                f.CONTENURAPPORT = message;
                f.DATECREATION = DateTime.Now;
                f.DATEMODIFICATION = DateTime.Now;
                f.DERNIERUTILISATEUR = util;
                DB.FACRAPPORTGEN.Add(f);
                DB.SaveChanges();
            }
        }

        public List<FACRAPPORTGEN> getAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                IQueryable<FACRAPPORTGEN> result = from c in DB.FACRAPPORTGEN
                                            select c;
                return result.ToList();
            }
        }

        public FACRAPPORTGEN getOne(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACRAPPORTGEN f = (from c in DB.FACRAPPORTGEN
                                 where c.IDRAPPORT == id
                                   select c).FirstOrDefault();
                return f;
            }
        }

        public FACRAPPORTGEN getOneByUser(int util)
        {
            using (EramEntities DB = new EramEntities())
            {
                FACRAPPORTGEN f = (from c in DB.FACRAPPORTGEN
                                   where c.DERNIERUTILISATEUR == util
                                   select c).FirstOrDefault();
                return f;
            }
        }

        public int? getMaxRapports()
        {
            using (EramEntities DB = new EramEntities())
            {
                try {
                    return DB.FACRAPPORTGEN.Select(p => p.IDRAPPORT).DefaultIfEmpty(0).Max();
                }catch(ArgumentNullException e){
                    return -1;
                }
            }
        }

    }
}
