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
    
    public partial class FACTASSOGFACTTYPEENSEIGNE
    {
        public int IDFACTTYPE { get; set; }
        public int IDENSEIGNE { get; set; }
        public string COMPTEVENDEUR { get; set; }
        public string COMPTEVENDTVA20 { get; set; }
        public string COMPTEVENDTVA14 { get; set; }
        public string COMPTEVENDTVA7 { get; set; }
        public string COMPTEVENDTVAIPPRF { get; set; }
        public string COMPTEACHETEUR { get; set; }
        public string COMPTEACHETTVA20 { get; set; }
        public string COMPTEACHETTVA14 { get; set; }
        public string COMPTEACHETTVA7 { get; set; }
        public string COMPTEACHETTVAIPPRF { get; set; }
        public string COMPTEVENDCLIENT { get; set; }
        public string COMPTEACHETFRS { get; set; }
        public string COMPTEVENDTVA10 { get; set; }
        public string COMPTEACHETTVA10 { get; set; }
        public string COMPTEVENDTSC { get; set; }
        public string COMPTEVENDTSCTAX { get; set; }
    
        public virtual FACTENSEIGNE FACTENSEIGNE { get; set; }
        public virtual FACTTYPE FACTTYPE { get; set; }
    }
}