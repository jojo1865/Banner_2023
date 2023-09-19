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
        #region 首頁
        public class cIndex
        {
            public List<Product> Ps_Classical = new List<Product>();//經典課程
            public List<Product> Ps_News = new List<Product>();//最新課程
            public List<Product> Ps_Close = new List<Product>();//即將結束報名課程
        }
        public class c_TempProduct
        {
            public Product P = null;
            public int iDay = 0;
        }
        public cIndex GetIndex()
        {
            var N = new cIndex();
            var Ps = DC.Product.Where(q => q.ActiveFlag &&
            !q.DeleteFlag &&
            q.Course.ActiveFlag &&
            !q.Course.DeleteFlag &&
            q.ShowFlag
            );
            //經典課程
            var Ps_ = Ps.Where(q => q.Course.ClassicalFlag).OrderByDescending(q => q.UpdDate).Take(12);
            foreach (var P_ in Ps_)
                N.Ps_Classical.Add(P_);

            //最新課程
            Ps_ = Ps.OrderByDescending(q => q.UpdDate).Take(12);
            foreach (var P_ in Ps_)
                N.Ps_News.Add(P_);

            //即將結束報名課程
            List<c_TempProduct> c_TP = new List<c_TempProduct>();
            //找臨櫃
            Ps_ = Ps.Where(q => (q.EDate_Signup_OnSite >= q.CreDate && q.EDate_Signup_OnSite >= DT.Date)
            ).OrderBy(q => (DT.Date - q.EDate_Signup_OnSite)).Take(12);
            foreach (var P_ in Ps_)
            {
                c_TP.Add(new c_TempProduct
                {
                    P = P_,
                    iDay = (DT.Date - P_.EDate_Signup_OnSite).Days
                });
            }
            //找線上
            Ps_ = Ps.Where(q => (q.EDate_Signup_OnLine >= q.CreDate && q.EDate_Signup_OnLine >= DT.Date)
            ).OrderBy(q => (DT.Date - q.EDate_Signup_OnLine)).Take(12);
            foreach (var P_ in Ps_)
            {
                if(!c_TP.Any(q=>q.P.PID == P_.PID))//排除重複
                {
                    c_TP.Add(new c_TempProduct
                    {
                        P = P_,
                        iDay = (DT.Date - P_.EDate_Signup_OnLine).Days
                    });
                }
            }
            //排序剩餘天數後塞入物件
            foreach (var P_ in c_TP.OrderBy(q=>q.iDay).Take(12))
                N.Ps_Close.Add(P_.P);
            return N;
        }
        [HttpGet]
        public ActionResult Index()
        {
            GetViewBag();
            return View(GetIndex());
        }
        #endregion

    }
}