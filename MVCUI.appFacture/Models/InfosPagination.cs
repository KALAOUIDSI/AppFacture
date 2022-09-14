using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace appFacture.Models
{

    /// <summary>
    /// This class contains the information required to page across the products.
    /// </summary>
    public class InfosPagination
    {
        public int TotalItems { get; set; }
        public int ItemsPerPage { get; set; }
        public int CurrentPage { get; set; }

        public int TotalPages
        {
            get
            {
                return (int)Math.Ceiling((decimal)TotalItems / ItemsPerPage);
            }
        }
    }

}
