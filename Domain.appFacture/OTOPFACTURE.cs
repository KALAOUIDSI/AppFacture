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
    using System.Collections.Generic;
    
    public partial class OTOPFACTURE
    {
        public OTOPFACTURE()
        {
            this.OTOPFACTUREDETAIL = new HashSet<OTOPFACTUREDETAIL>();
        }
    
        public int IDDEMANDE { get; set; }
        public string REFERENCEFACT { get; set; }
        public string LIBELLEDEMANDE { get; set; }
        public string CHAPITRE { get; set; }
        public Nullable<int> IDFACTTYPE { get; set; }
        public Nullable<short> STATUS { get; set; }
        public Nullable<decimal> MNTHT { get; set; }
        public Nullable<decimal> MNTTVA { get; set; }
        public Nullable<decimal> MNTTTC { get; set; }
        public Nullable<int> IDVENDEUR { get; set; }
        public Nullable<int> IDACHETEUR { get; set; }
        public string COMMENTAIRE { get; set; }
        public Nullable<int> DERNIERUTILISATEUR { get; set; }
        public Nullable<System.DateTime> DATECREATION { get; set; }
        public Nullable<System.DateTime> DATEMODIFICATION { get; set; }
        public Nullable<System.DateTime> DATEREFUS { get; set; }
        public Nullable<System.DateTime> DATEVALIDATION { get; set; }
        public Nullable<System.DateTime> DATEIMPRESSION { get; set; }
        public Nullable<System.DateTime> DATECOMPTABILISATION { get; set; }
        public string ANNEEPRESTATION { get; set; }
        public Nullable<short> FLAGAVOIR { get; set; }
        public string CNUFVENDEUR { get; set; }
        public string CNUFACHETEUR { get; set; }
        public Nullable<short> RAYON { get; set; }
        public Nullable<short> RAYONACHETEUR { get; set; }
        public Nullable<System.DateTime> PRIODEDEBUT { get; set; }
        public Nullable<System.DateTime> PRIODEFIN { get; set; }
        public Nullable<int> USERINTEGAGR { get; set; }
        public Nullable<int> USERREPORT { get; set; }
        public Nullable<int> USERGENERE { get; set; }
        public Nullable<int> USERIMPRIM { get; set; }
        public Nullable<System.DateTime> DATEGENERATION { get; set; }
    
        public virtual FACTTYPE FACTTYPE { get; set; }
        public virtual OTOPACHETEUR OTOPACHETEUR { get; set; }
        public virtual ICollection<OTOPFACTUREDETAIL> OTOPFACTUREDETAIL { get; set; }
        public virtual OTOPVENDEUR OTOPVENDEUR { get; set; }
        public virtual FACTFACTURESTATUS FACTFACTURESTATUS { get; set; }
    }
}
