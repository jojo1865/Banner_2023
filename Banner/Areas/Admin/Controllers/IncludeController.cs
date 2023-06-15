using Banner.Models;
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
            string sURL = Request.RawUrl;//目前完整網址
            cMenu Ms = new cMenu();
            int ACID = GetACID();
            var PA = DC.M_Permissions_Account.FirstOrDefault(q => q.ACID == ACID && q.PID == 1 && !q.DeleteFlag);
            if (PA != null)//此使用者擁有系統管理者權限
            {
                Ms.Items = GetAdminMenu(null, sURL, 0);
            }
            else
            {
                var Ps = DC.Permissions.Where(q => q.M_Permissions_Account.Count(p => p.ACID == ACID && !p.DeleteFlag) > 0);
                Ms.Items = GetAdminMenu(Ps.ToList(), sURL, 0);
            }
            return PartialView(Ms);
        }

        private List<cMenu> GetAdminMenu(List<Permissions> Ps, string sURL, int MID)
        {
            List<cMenu> Ms = new List<cMenu>();
            var Ns = DC.Menu.Where(q => q.ActiveFlag && !q.DeleteFlag && q.MenuType == 0 && q.ParentID == MID).ToList();
            if (Ps != null)
            {
                var MPs = from q in (from q in DC.M_Permissions_Menu.Where(q => q.ShowFlag).ToList()
                                     join p in Ps
                                     on q.PID equals p.PID
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
                    cM.Title = N.Title;
                    cM.Url = N.URL;
                    cM.SortNo = N.SortNo;
                    if (!string.IsNullOrEmpty(N.URL))
                        cM.SelectFlag = sURL.StartsWith(N.URL);
                    else
                        cM.SelectFlag = false;
                    cM.Items = GetAdminMenu(Ps, sURL, N.MID);

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

        public PartialViewResult _HeadInclude()
        {
            return PartialView();
        }
    }
}