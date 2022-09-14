using Elmah;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

public class ElmahHandleErrorAttribute : HandleErrorAttribute
{
    public override void OnException(ExceptionContext filterContext)
    {
        base.OnException(filterContext);

        // signal ELMAH to log the exception
        if (filterContext.ExceptionHandled)
            ErrorSignal.FromCurrentContext().Raise(filterContext.Exception);
    }
}