using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Web.Controllers
{
    public class ClassStoreController : PublicClass
    {
        // GET: Web/ClassStore
        public ActionResult Index()
        {
            GetViewBag();
            return View();
        }
    }
}