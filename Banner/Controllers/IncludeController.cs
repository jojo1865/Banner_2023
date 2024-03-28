using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Banner.Controllers
{
    public class IncludeController : PublicClass
    {
        //改小組
        public PartialViewResult _ChangeGroup()
        {
            return PartialView();
        }
    }
}