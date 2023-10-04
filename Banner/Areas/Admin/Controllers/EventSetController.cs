using Banner.Models;
using OfficeOpenXml.ConditionalFormatting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Admin.Controllers
{
    public class EventSetController : PublicClass
    {
        // GET: Admin/EventSet
        public ActionResult Index()
        {
            return View();
        }
        #region 活動列表
        public class cEvent_List
        {
            public cTableList cTL = new cTableList();
            public int ActiveType = -1;
            public string sKey = "";
            public int CID = 1;
            public List<SelectListItem> EC_SL = new List<SelectListItem>();
        }
        public cEvent_List GetEvent_List(FormCollection FC)
        {
            cEvent_List c = new cEvent_List();
            int CatID = GetQueryStringInInt("CID");
            if (CatID <= 0)
                CatID = 1;
            c.CID = CatID;
            #region 物件初始化
            //聚會點初始化
            int MID = 0;
            c.EC_SL.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            var Ms = from q in DC.M_Location_Set.Where(q => q.SetType == 0 && q.ActiveFlag && !q.DeleteFlag)
                     join p in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag)
                     on q.OIID equals p.OIID
                     select new { p.OIID, OITitle = p.Title, OTitle = p.Organize.Title, q.MID, ML_Title = q.Meeting_Location.Title };
            ACID = GetACID();
            ACID = 4;
            if (ACID != 1)//非管理者
            {
                Ms = from q in Ms
                     join p in DC.v_GetAC_O2_OI.Where(q => q.ACID == ACID)
                     on q.OIID equals p.OIID
                     select q;
            }

            foreach (var M in Ms)
                c.EC_SL.Add(new SelectListItem { Text = M.OITitle + M.OTitle + "-" + M.ML_Title, Value = M.MID.ToString() });

            #region 前端資料帶入
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.ActiveType = Convert.ToInt32(FC != null ? FC.Get("rbl_ActiveType") : "-1");
            c.sKey = FC != null ? FC.Get("txb_Key") : "";
            if (FC != null)
            {
                MID = Convert.ToInt32(FC.Get("ddl_M"));
                c.EC_SL.ForEach(q => q.Selected = false);
                c.EC_SL.First(q => q.Value == MID.ToString()).Selected = true;
            }
            #endregion


            c.cTL = new cTableList();
            c.cTL.Title = "";
            c.cTL.NowPage = iNowPage;
            c.cTL.ItemID = "";
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            #endregion
            #region 資料庫資料帶入
            var Ns = DC.Event.Where(q => !q.DeleteFlag && q.ECID == CatID);
            if (c.sKey != "")
                Ns = Ns.Where(q => q.Title.Contains(c.sKey));
            if (c.ActiveType >= 0)
                Ns = Ns.Where(q => q.ActiveFlag == (c.ActiveType == 1));
            if (CatID == 1)//主日聚會
            {
                if (MID > 0)
                    Ns = Ns.Where(q => q.TargetLocationType == 0 && q.TargetLocationID == MID);
                else if (ACID != 1)//非管理者
                {
                    var MLs = from q in DC.M_Location_Set.Where(q => q.SetType == 0 && !q.DeleteFlag && q.ActiveFlag && q.OIID > 0)
                              join p in DC.Meeting_Location.Where(q => !q.DeleteFlag && q.ActiveFlag)
                              on q.MLID equals p.MLID
                              select new { q.OIID };

                    var OIs = from q in MLs
                              join p in DC.v_GetAC_O2_OI.Where(q => q.ACID == ACID)
                              on q.OIID equals p.OIID
                              select q;
                }
            }

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "活動名稱" });
            TopTitles.Add(new cTableCell { Title = "舉辦日期" });
            TopTitles.Add(new cTableCell { Title = "舉辦時間" });
            TopTitles.Add(new cTableCell { Title = "報名管理" });
            TopTitles.Add(new cTableCell { Title = "狀態" });
            TopTitles.Add(new cTableCell { Title = "備註" });

            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.EID).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/EventSet/Event_Edit/" + N_.EID + "?CID=" + CatID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = N_.Title });//活動名稱
                if (N_.CircleFlag)//為循環活動
                    cTR.Cs.Add(new cTableCell { Value = sWeeks[N_.WeeklyNo] });//舉辦日期
                else
                    cTR.Cs.Add(new cTableCell { Value = N_.EventDate.ToString(DateFormat) });//舉辦日期
                cTR.Cs.Add(new cTableCell { Value = N_.STime.Hours + ":" + N_.STime.Minutes + " ~ " + N_.ETime.Hours + ":" + N_.ETime.Minutes });//舉辦時間
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/EventSet/Event_Join_List/0?CID=" + CatID + "&EID=" + N_.EID, Target = "_self", Value = "管理" });//報名管理
                cTR.Cs.Add(new cTableCell { Value = N_.ActiveFlag ? "啟用" : "停用" });//狀態
                cTR.Cs.Add(new cTableCell { Value = N_.Note });//備註

                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion

            return c;
        }
        [HttpGet]
        public ActionResult Event_List()
        {
            GetViewBag();
            return View(GetEvent_List(null));
        }

        [HttpPost]
        public ActionResult Event_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetEvent_List(FC));
        }
        #endregion
        #region 活動編輯
        public class cEvent_Edit
        {
            public Event N = new Event();

        }

        public cEvent_Edit GetEvent_Edit(FormCollection FC)
        {
            cEvent_Edit c = new cEvent_Edit();
            int EID = GetQueryStringInInt("EID");
            int CID = GetQueryStringInInt("CID");


            return c;
        }
        [HttpGet]
        public ActionResult Event_Edit()
        {
            GetViewBag();
            return View(GetEvent_Edit(null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Event_Edit(FormCollection FC)
        {
            GetViewBag();
            var c = GetEvent_Edit(FC);
            return View(c);
        }
        #endregion
    }
}