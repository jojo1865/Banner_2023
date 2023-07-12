using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Web.Controllers
{
    public class AccountPlaceController : PublicClass
    {
        // GET: Web/AccountPlace
        public ActionResult Index()
        {
            GetViewBag();
            return View();
        }
        #region 小組資訊-首頁
        public ActionResult GroupLeaderIndex()
        {
            GetViewBag();
            return View();
        }
        #endregion

    }
}

