using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace appFacture.Controllers
{
    public class BugController : Controller
    {
        [ElmahHandleErrorAttribute()]
        public ActionResult Index()
        {
            return View();
        }
        [ElmahHandleErrorAttribute()]
        public ActionResult Index2()
        {
            return View();
        }
    }
}
