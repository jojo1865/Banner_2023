using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Web.Controllers
{
    public class AccountAddController : PublicClass
    {
        // GET: Web/AccountAdd
        public ActionResult Step1()
        {
            return View();
        }
    }
}