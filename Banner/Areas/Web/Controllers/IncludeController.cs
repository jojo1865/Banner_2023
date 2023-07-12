using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace Banner.Areas.Web.Controllers
{
    public class IncludeController : PublicClass
    {
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
        #region 上選單
        public PartialViewResult _TopMenu()
        {
            var Ms = DC.Menu.Where(q => q.ParentID == 0 && q.ActiveFlag && !q.DeleteFlag);
            int ACID = GetACID();
            var MOIs = GetMOIAC(0, 0, ACID);
            if (MOIs.Count() > 0)//是否已有小組
            {
                if (MOIs.Count(q => q.OrganizeInfo.OID == 8) > 0)//是否為小組長
                    Ms = Ms.Where(q => q.MenuType == 1 || q.MenuType == 2);
                else
                    Ms = Ms.Where(q => q.MenuType == 1);
            }
            else
                Ms = Ms.Where(q => q.MenuType == 1);
            Ms = Ms.OrderBy(q => q.SortNo);
            string[] ThisPath = Request.Url.Segments;
            string ThisController = "";
            for (int i = 0; i < ThisPath.Length; i++)
            {
                if (ThisPath[i].ToLower() == "web/")
                {
                    ThisController = "/Web/" + ThisPath[i + 1].Replace("/", "");
                    break;
                }
            }

            List<cMenu> cMs = new List<cMenu>();
            foreach (var M in Ms)
            {
                cMenu cM = new cMenu
                {
                    MenuID = M.MID,
                    Title = M.Title,
                    Url = M.URL,
                    ImgUrl = M.ImgURL,
                    SortNo = M.SortNo,
                    SelectFlag = M.URL.StartsWith(ThisController)
                };
                cMs.Add(cM);
            }
            if (cMs.FirstOrDefault(q => q.SelectFlag) == null)
                cMs[0].SelectFlag = true;

            return PartialView(cMs);
        }
        #endregion
        #region 上選單
        public PartialViewResult _LeftMenu()
        {
            List<cMenu> cMs = new List<cMenu>();
            string[] ThisPath = Request.Url.Segments;
            string ThisController = "";
            for (int i = 0; i < ThisPath.Length; i++)
            {
                if (ThisPath[i].ToLower() == "web/")
                {
                    ThisController = "/Web/" + ThisPath[i + 1].Replace("/", "");
                    break;
                }
            }
            var PM = DC.Menu.FirstOrDefault(q => q.ParentID == 0 && q.ActiveFlag && !q.DeleteFlag && q.URL.StartsWith(ThisController));
            if (PM != null)
            {
                var Ms = DC.Menu.Where(q => q.ParentID == PM.MID && q.ActiveFlag && !q.DeleteFlag);
                int ACID = GetACID();
                var MOIs = GetMOIAC(0, 0, ACID);
                if (MOIs.Count() > 0)//是否已有小組
                {
                    if (MOIs.Count(q => q.OrganizeInfo.OID == 8) > 0)//是否為小組長
                        Ms = Ms.Where(q => q.MenuType == 1 || q.MenuType == 2);
                    else
                        Ms = Ms.Where(q => q.MenuType == 1);
                }
                else
                    Ms = Ms.Where(q => q.MenuType == 1);
                Ms = Ms.OrderBy(q => q.SortNo);
                // /Web/AccountPlace/Index/123
                string NowShortPath = Request.FilePath.Replace("_Edit", "").Replace("_Info", "").Replace("_Index", "");
                foreach (var M in Ms)
                {
                    cMenu cM = new cMenu
                    {
                        MenuID = M.MID,
                        Title = M.Title,
                        Url = M.URL,
                        ImgUrl = M.ImgURL,
                        SortNo = M.SortNo,
                        SelectFlag = M.URL.StartsWith(NowShortPath)
                    };
                    cMs.Add(cM);
                }
                if (cMs.FirstOrDefault(q => q.SelectFlag) == null)
                    cMs[0].SelectFlag = true;
            }


            return PartialView(cMs);
        }
        #endregion
        public PartialViewResult _HeadInclude()
        {
            return PartialView();
        }
    }
}