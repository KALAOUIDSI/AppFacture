//------------------------------------------------------------------------------
// <auto-generated>
//    Ce code a été généré à partir d'un modèle.
//
//    Des modifications manuelles apportées à ce fichier peuvent conduire à un comportement inattendu de votre application.
//    Les modifications manuelles apportées à ce fichier sont remplacées si le code est régénéré.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Domain.appFacture
{
    using System;
    using System.Data.Entity;
    using System.Data.Entity.Infrastructure;
    using System.Data.Objects;
    using System.Data.Objects.DataClasses;
    using System.Linq;
    
    public partial class EramEntities : DbContext
    {
        public EramEntities()
            : base("name=EramEntities")
        {
        }
    
        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            throw new UnintentionalCodeFirstException();
        }
    
        public DbSet<ACRTRANS> ACRTRANS { get; set; }
        public DbSet<FACRAPPORTGEN> FACRAPPORTGEN { get; set; }
        public DbSet<FACTASSOGFACTTYPEENSEIGNE> FACTASSOGFACTTYPEENSEIGNE { get; set; }
        public DbSet<FACTCLIENT> FACTCLIENT { get; set; }
        public DbSet<FACTDEMANDE> FACTDEMANDE { get; set; }
        public DbSet<FACTDEMANDEDETAIL> FACTDEMANDEDETAIL { get; set; }
        public DbSet<FACTENSEIGNE> FACTENSEIGNE { get; set; }
        public DbSet<FACTFACTPIECEJOINTE> FACTFACTPIECEJOINTE { get; set; }
        public DbSet<FACTFACTURE> FACTFACTURE { get; set; }
        public DbSet<FACTFACTUREDETAIL> FACTFACTUREDETAIL { get; set; }
        public DbSet<FACTGROUPE> FACTGROUPE { get; set; }
        public DbSet<FACTINTERFACE> FACTINTERFACE { get; set; }
        public DbSet<FACTPIECEJOINTE> FACTPIECEJOINTE { get; set; }
        public DbSet<FACTSITE> FACTSITE { get; set; }
        public DbSet<FACTTYPE> FACTTYPE { get; set; }
        public DbSet<FACTTYPECATEGORIE> FACTTYPECATEGORIE { get; set; }
        public DbSet<FACTTYPECLIENT> FACTTYPECLIENT { get; set; }
        public DbSet<FACTUSER> FACTUSER { get; set; }
        public DbSet<OTOPACHETEUR> OTOPACHETEUR { get; set; }
        public DbSet<OTOPFACTURE> OTOPFACTURE { get; set; }
        public DbSet<OTOPFACTUREDETAIL> OTOPFACTUREDETAIL { get; set; }
        public DbSet<OTOPGROUPE> OTOPGROUPE { get; set; }
        public DbSet<OTOPVENDEUR> OTOPVENDEUR { get; set; }
        public DbSet<FACTCLIENT_INUTILISEE> FACTCLIENT_INUTILISEE { get; set; }
        public DbSet<FACTIMPORTXLSX> FACTIMPORTXLSX { get; set; }
        public DbSet<OTOPACRTRANS> OTOPACRTRANS { get; set; }
        public DbSet<FACTDEMANDESTATUS> FACTDEMANDESTATUS { get; set; }
        public DbSet<FACTFACTURESTATUS> FACTFACTURESTATUS { get; set; }
    
        public virtual int GET_CA_INDEX(Nullable<decimal> yEARVALUE, Nullable<decimal> mONTHVALUE, ObjectParameter rETURNVALUE)
        {
            var yEARVALUEParameter = yEARVALUE.HasValue ?
                new ObjectParameter("YEARVALUE", yEARVALUE) :
                new ObjectParameter("YEARVALUE", typeof(decimal));
    
            var mONTHVALUEParameter = mONTHVALUE.HasValue ?
                new ObjectParameter("MONTHVALUE", mONTHVALUE) :
                new ObjectParameter("MONTHVALUE", typeof(decimal));
    
            return ((IObjectContextAdapter)this).ObjectContext.ExecuteFunction("GET_CA_INDEX", yEARVALUEParameter, mONTHVALUEParameter, rETURNVALUE);
        }
    }
}
