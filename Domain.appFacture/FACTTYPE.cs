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
    
    public partial class FACTTYPE
    {
        public FACTTYPE()
        {
            this.FACTASSOGFACTTYPEENSEIGNE = new HashSet<FACTASSOGFACTTYPEENSEIGNE>();
            this.FACTDEMANDE = new HashSet<FACTDEMANDE>();
            this.FACTFACTURE = new HashSet<FACTFACTURE>();
            this.OTOPFACTURE = new HashSet<OTOPFACTURE>();
            this.FACTUSER = new HashSet<FACTUSER>();
        }
    
        public int IDFACTTYPE { get; set; }
        public string LIBELLE { get; set; }
        public string COMPTE { get; set; }
        public Nullable<int> DERNIERUTILISATEUR { get; set; }
        public Nullable<System.DateTime> DATECREATION { get; set; }
        public Nullable<System.DateTime> DATEMODIFICATION { get; set; }
        public Nullable<short> IDCATEGORIE { get; set; }
    
        public virtual ICollection<FACTASSOGFACTTYPEENSEIGNE> FACTASSOGFACTTYPEENSEIGNE { get; set; }
        public virtual ICollection<FACTDEMANDE> FACTDEMANDE { get; set; }
        public virtual ICollection<FACTFACTURE> FACTFACTURE { get; set; }
        public virtual FACTTYPECATEGORIE FACTTYPECATEGORIE { get; set; }
        public virtual ICollection<OTOPFACTURE> OTOPFACTURE { get; set; }
        public virtual ICollection<FACTUSER> FACTUSER { get; set; }
    }
}