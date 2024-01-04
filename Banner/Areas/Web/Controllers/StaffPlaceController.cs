using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Web.Controllers
{
    public class StaffPlaceController : PublicClass
    {
        private int SID = 0;
        private void GetID(int ID = 0)
        {
            if (ID > 0)
            {
                SetBrowserData("SID", ID.ToString());
                SID = ID;
            }
            else if (GetBrowserData("SID") != "")
                SID = Convert.ToInt32(GetBrowserData("SID"));
            else if (GetQueryStringInInt("SID") > 0)
            {
                SID = GetQueryStringInInt("SID");
                SetBrowserData("SID", SID.ToString());
            }
            ViewBag._StaffTitle = "";
            if (SID == 0)
                SetAlert("事工團ID查詢失敗", 2, "Web/Home/Index");
            else
            {
                ACID = GetACID();
                var S = DC.Staff.FirstOrDefault(q => q.SID == SID && !q.DeleteFlag && q.ActiveFlag);
                if (S == null)
                    SetAlert("此事工團不存在或已被關閉", 2, "Web/Home/Index");
                else
                {
                    var M = DC.M_Staff_Account.FirstOrDefault(q => q.SID == S.SID && q.ACID == ACID && !q.DeleteFlag);
                    if (M == null)
                        SetAlert("您並非此事工團的成員", 2, "Web/Home/Index");
                    else
                        ViewBag._StaffTitle = "[" + M.OrganizeInfo.Title + M.OrganizeInfo.Organize.Title + "]" + S.Staff_Category.Title + "-" + S.Title;
                }
            }
            ViewBag._SID = SID;

        }

        #region 活動列表

        public cTableList GetEvent_List(FormCollection FC)
        {
            cTableList cTL = new cTableList();
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            cTL.Title = "事工團活動列表";
            cTL.NowPage = iNowPage;
            cTL.NumCut = iNumCut;
            cTL.Rs = new List<cTableRow>();
            GetID();
            var TopTitles = new List<cTableCell>();

            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 200 });
            TopTitles.Add(new cTableCell { Title = "活動類型", WidthPX = 150 });
            TopTitles.Add(new cTableCell { Title = "標題" });
            TopTitles.Add(new cTableCell { Title = "活動日期", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "參加人數", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "備註" });
            cTL.Rs.Add(SetTableRowTitle(TopTitles));
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";

            var Ns = DC.Event_Join_Header.Where(q => q.TargetType == 1 && q.TargetID == SID && !q.Event.DeleteFlag);
            cTL.TotalCt = Ns.Count();
            cTL.MaxNum = GetMaxNum(cTL.TotalCt, cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.Event.EventDate).ThenByDescending(q => q.Event.STime).Skip((iNowPage - 1) * cTL.NumCut).Take(cTL.NumCut);

            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                //操作
                cTableCell cTC = new cTableCell();
                cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Web/StaffPlace/Event_Edit?SID=" + SID + "&EHID=" + N.EJHID, Target = "_self", Value = "編輯" });
                cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Web/StaffPlace/JoinCode?SID=" + SID + "&EHID=" + N.EJHID, Target = "_black", Value = "報到QR-Code" });
                cTR.Cs.Add(cTC);
                //活動類型
                cTR.Cs.Add(new cTableCell { Value = sCourseType[N.Event.EventType] });
                //標題
                cTR.Cs.Add(new cTableCell { Value = N.Event.Title });
                //活動日期
                cTR.Cs.Add(new cTableCell { Value = N.Event.EventDate.ToString(DateFormat) });
                //參加人數
                cTR.Cs.Add(new cTableCell { Type = "link", URL = "/Web/StaffPlace/Join_List?SID=" + SID + "&EHID=" + N.EJHID, Target = "_black", Value = "(" + N.Event_Join_Detail.Count(q => !q.DeleteFlag) + ")" });
                //備註
                cTR.Cs.Add(new cTableCell { Value = N.Note });
                cTL.Rs.Add(SetTableCellSortNo(cTR));
            }

            return cTL;
        }
        [HttpGet]
        public ActionResult Event_List()
        {

            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            return View(GetEvent_List(null));
        }
        [HttpPost]
        public ActionResult Event_List(FormCollection FC)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            return View(GetEvent_List(FC));
        }
        #endregion
        #region 建立活動
        public ActionResult Event_Edit()
        {
            
            return View();
        }
        #endregion
        #region 活動內容
        public ActionResult Event_Info()
        {
            return View();
        }
        #endregion
    }
}