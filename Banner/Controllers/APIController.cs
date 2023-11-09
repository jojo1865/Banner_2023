using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;


namespace Banner.Controllers
{
    public class APIController : Controller
    {
        // GET: API
        public ActionResult Index()
        {
            return View();
        }

        public string GetAPI_1()//牧養組織(三層)
        {
            return "";
        }

        public string GetAPI_2()//牧養身份(三層)
        {
            return "";
        }

        public string GetAPI_3()//聚會點組織(三層)
        {
            return "";
        }

        public string GetAPI_4()//事工團身份
        {
            return "";
        }

        public string GetAPI_5()//小組出缺席紀錄
        {
            return "";
        }

        public string GetAPI_6()//事工團出席紀錄
        {
            return "";
        }

        public string GetAPI_7()//課程歷程
        {
            return "";
        }

        public string GetAPI_8()//屬靈健檢表
        {
            return "";
        }


    }
}