using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;
using System.Data.Entity;

namespace Domain.appFacture
{
    public partial class OTOPGROUPE
    {
        void OnCreated()
        {
        }

        public OTOPGROUPE(int id, string value)
        {
            this.IDGRP = id;
            this.LIBELLEGRP = value;
        }

        public List<OTOPGROUPE> getAll()
        {
            using (EramEntities DB = new EramEntities())
            {
                IQueryable<OTOPGROUPE> result = from c in DB.OTOPGROUPE
                                            orderby c.LIBELLEGRP 
                                            select c;
                return result.ToList();
            }
        }

        public List<OTOPGROUPE> getAll(long IdUtilisateur)
        {
            using (EramEntities DB = new EramEntities())
            {
                IQueryable<OTOPGROUPE> result = from c in DB.OTOPGROUPE
                                                where c.FACTUSER.Any(u => u.IDUTILISATEUR == IdUtilisateur)
                                                select c;
                return result.ToList();
            }
        }

        public OTOPGROUPE getOne(int id)
        {
            using (EramEntities DB = new EramEntities())
            {
                OTOPGROUPE OTOPGROUPE = (from c in DB.OTOPGROUPE
                                         where c.IDGRP == id
                                 select c).First();
                return OTOPGROUPE;
            }
        }



    }
}
