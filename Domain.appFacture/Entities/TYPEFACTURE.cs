using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;

namespace Domain.appFacture
{
    public  class TYPEFACTURE
    {

        public string IDTYPEFACT { get; set; }
        public string LIBELLETYPEFACT { get; set; }

        public TYPEFACTURE(string id, string value)
        {
            this.IDTYPEFACT = id;
            this.LIBELLETYPEFACT = value;
        }
   
    }
}
