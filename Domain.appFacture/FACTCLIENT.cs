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
    
    public partial class FACTCLIENT
    {
        public FACTCLIENT()
        {
            this.FACTDEMANDE = new HashSet<FACTDEMANDE>();
            this.FACTFACTURE = new HashSet<FACTFACTURE>();
        }
    
        public int IDCLIENT { get; set; }
        public string REFERENCECLIENT { get; set; }
        public string DESIGNATIONCLIENT { get; set; }
        public string ADRFACTCLIENT { get; set; }
        public string ICECLIENT { get; set; }
        public Nullable<short> STATUS { get; set; }
        public Nullable<int> DERNIERUTILISATEUR { get; set; }
        public Nullable<System.DateTime> DATECREATION { get; set; }
        public Nullable<System.DateTime> DATEMODIFICATION { get; set; }
    
        public virtual ICollection<FACTDEMANDE> FACTDEMANDE { get; set; }
        public virtual ICollection<FACTFACTURE> FACTFACTURE { get; set; }
    }
}
