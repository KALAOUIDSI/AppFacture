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
    
    public partial class FACTUSER
    {
        public FACTUSER()
        {
            this.FACTGROUPE = new HashSet<FACTGROUPE>();
            this.FACTTYPE = new HashSet<FACTTYPE>();
            this.FACTSITE1 = new HashSet<FACTSITE>();
            this.OTOPGROUPE = new HashSet<OTOPGROUPE>();
        }
    
        public int IDUTILISATEUR { get; set; }
        public string NOM { get; set; }
        public string PRENOM { get; set; }
        public string EMAIL { get; set; }
        public string LOGIN { get; set; }
        public string PASSWORD { get; set; }
        public short ACTIF { get; set; }
        public Nullable<int> IDSITE { get; set; }
        public Nullable<int> DERNIERUTILISATEUR { get; set; }
        public Nullable<System.DateTime> DATECREATION { get; set; }
        public Nullable<System.DateTime> DATEMODIFICATION { get; set; }
    
        public virtual FACTSITE FACTSITE { get; set; }
        public virtual ICollection<FACTGROUPE> FACTGROUPE { get; set; }
        public virtual ICollection<FACTTYPE> FACTTYPE { get; set; }
        public virtual ICollection<FACTSITE> FACTSITE1 { get; set; }
        public virtual ICollection<OTOPGROUPE> OTOPGROUPE { get; set; }
    }
}
