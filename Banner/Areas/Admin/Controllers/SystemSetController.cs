using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace Banner.Areas.Admin.Controllers
{
    public class SystemSetController : PublicClass
    {
        // GET: Admin/SystemSet
        public ActionResult Index()
        {
            return View();
        }


    }
}