using System;
using System.Collections.Generic;
using System.Data.Objects;
using System.Linq;

namespace Domain.appFacture
{
    public  class TAUXTVA
    {

        public int IDTAUXTVA { get; set; }
        public string LIBELLETAUXTVA { get; set; }

        public TAUXTVA(int id, string value)
        {
            this.IDTAUXTVA = id;
            this.LIBELLETAUXTVA = value;
        }
   
    }
}
