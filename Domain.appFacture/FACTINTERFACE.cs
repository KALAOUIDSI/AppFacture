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
    
    public partial class FACTINTERFACE
    {
        public FACTINTERFACE()
        {
            this.FACTGROUPE = new HashSet<FACTGROUPE>();
        }
    
        public int IDINTERFACE { get; set; }
        public string LIBELLEINTERFACE { get; set; }
    
        public virtual ICollection<FACTGROUPE> FACTGROUPE { get; set; }
    }
}