using Banner.Models;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Admin.Controllers
{
    public class IncludeController : PublicClass
    {
        #region 選單
        // GET: Admin/Shared
        public PartialViewResult _LeftMenu()
        {
            string sURL = GetShortURL().Replace("_Edit", "_List");
            cMenu Ms = new cMenu();
            int ACID = GetACID();
            if (CheckAdmin(ACID))//此使用者擁有系統管理者權限
            {
                Ms.Items = GetMenu(null, sURL, 0);//選單全開
            }
            else
            {
                var Rs = from q in DC.Rool.Where(q => q.ActiveFlag && !q.DeleteFlag && (q.RoolType == 3 || q.RoolType == 4))
                         join p in DC.M_Rool_Account.Where(q => q.ACID == ACID && !q.DeleteFlag && q.ActiveFlag && (q.JoinDate >= q.CreDate && q.JoinDate.Date <= DT.Date) && (q.LeaveDate == q.CreDate || q.LeaveDate.Date >= DT.Date))
                         on q.RID equals p.RID
                         select q;

                Ms.Items = GetMenu(Rs.ToList(), sURL, 0);
            }
            return PartialView(Ms);
        }
        //取得選單可以查的網址
        private string GetShortURL()
        {
            string sURL = Request.RawUrl;//目前完整網址
            if (sURL.ToLower().Contains("/admin/"))
            {
                string[] NewURL = sURL.Split('/');
                sURL = "";
                if (NewURL.Length >= 4)
                    for (int i = 0; i < 4; i++)
                        sURL += NewURL[i] + "/";
            }
            return sURL;
        }

        private List<cMenu> GetMenu(List<Rool> Rs, string sURL, int MID)
        {
            List<cMenu> Ms = new List<cMenu>();
            var Ns = DC.Menu.Where(q => q.ActiveFlag && !q.DeleteFlag && q.MenuType == 0 && q.ParentID == MID).ToList();
            if (Rs != null)
            {
                var MPs = from q in (from q in DC.M_Rool_Menu.Where(q => q.ShowFlag && q.Menu.MenuType == 0).ToList()
                                     join p in Rs
                                     on q.RID equals p.RID
                                     select new { q.MID })
                          group q by new { q.MID } into g
                          select new { g.Key.MID };

                Ns = (from q in MPs.ToList()
                      join p in Ns
                      on q.MID equals p.MID
                      select p).ToList();
            }
            Ns = Ns.OrderBy(q => q.SortNo).ToList();
            if (Ns.Count() > 0)
            {
                foreach (var N in Ns)
                {
                    cMenu cM = new cMenu();
                    cM.MenuID = N.MID;
                    cM.Title = N.Title;
                    cM.Url = N.URL;
                    cM.SortNo = N.SortNo;
                    cM.ImgUrl = string.IsNullOrEmpty(N.ImgURL) ? "" : N.ImgURL;
                    cM.Items = GetMenu(Rs, sURL, N.MID);
                    if (!string.IsNullOrEmpty(N.URL))
                        cM.SelectFlag = N.URL.StartsWith(sURL);
                    else if (cM.Items.Find(q => q.SelectFlag) != null)
                        cM.SelectFlag = true;
                    else
                        cM.SelectFlag = false;
                    Ms.Add(cM);
                }
            }
            return Ms;
        }
        #endregion
        #region Alert控制
        public PartialViewResult _AlertMsg()
        {
            return PartialView();
        }
        #endregion
        #region 列表顯示
        public PartialViewResult _TableList(cTableList cTL)
        {
            return PartialView(cTL);
        }
        #endregion
        #region 取SiteMap
        public PartialViewResult _SiteMap()
        {
            int iSort = 0;
            List<cMenu> Ms = new List<cMenu>();
            string sURL = GetShortURL();
            string sID = Request.Url.Segments[Request.Url.Segments.Length - 1];
            if (sURL.Contains("_Edit"))
            {
                cMenu cM = new cMenu();
                cM.MenuID = 0;
                cM.Title = (sID == "0" ? "新增" : "編輯");
                cM.Url = "";
                cM.SortNo = iSort;
                cM.ImgUrl = "";
                cM.SelectFlag = true;
                cM.Items = null;
                Ms.Add(cM);

                iSort++;
            }

            sURL = sURL.Replace("_Edit", "_List");
            var N = DC.Menu.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.MenuType == 0 && q.URL.StartsWith(sURL));
            while (N != null)
            {
                cMenu cM = new cMenu();
                cM.MenuID = N.MID;
                cM.Title = N.Title;
                cM.Url = iSort == 0 ? "" : N.URL;
                cM.SortNo = iSort;
                cM.ImgUrl = "";
                cM.SelectFlag = true;
                cM.Items = null;
                Ms.Add(cM);

                iSort++;
                N = DC.Menu.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.MenuType == 0 && q.MID == N.ParentID);
            };

            return PartialView(Ms);
        }

        #endregion
        public PartialViewResult _HeadInclude()
        {
            return PartialView();
        }

    }
}