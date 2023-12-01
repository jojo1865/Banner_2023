using Banner.Models;
using OfficeOpenXml.ConditionalFormatting;
using OfficeOpenXml.Drawing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
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
            var Ms = from q in DC.Meeting_Location_Set.Where(q => q.SetType == 0 && q.ActiveFlag && !q.DeleteFlag)
                     join p in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag)
                     on q.OIID equals p.OIID
                     select new { p.OIID, OITitle = p.Title, OTitle = p.Organize.Title, q.MLSID, ML_Title = q.Meeting_Location.Title };
            ACID = GetACID();
            if (ACID != 1)//非管理者
            {
                Ms = from q in Ms
                     join p in DC.v_GetAC_O2_OI.Where(q => q.ACID == ACID)
                     on q.OIID equals p.OIID
                     select q;
            }

            foreach (var M in Ms)
                c.EC_SL.Add(new SelectListItem { Text = M.OITitle + M.OTitle + "-" + M.ML_Title, Value = M.MLSID.ToString() });

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
                    Ns = Ns.Where(q => q.Location_MID == MID);
                else if (ACID != 1)//非管理者
                {
                    var MLs = from q in DC.Meeting_Location_Set.Where(q => q.SetType == 0 && !q.DeleteFlag && q.ActiveFlag && q.OIID > 0)
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
            //TopTitles.Add(new cTableCell { Title = "報名管理" });
            TopTitles.Add(new cTableCell { Title = "狀態" });
            TopTitles.Add(new cTableCell { Title = "備註" });

            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.EID).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/EventSet/Event_Edit/" + N_.EID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = N_.Title });//活動名稱
                if (N_.CircleFlag)//為循環活動
                    cTR.Cs.Add(new cTableCell { Value = sWeeks[N_.WeeklyNo] });//舉辦日期
                else
                    cTR.Cs.Add(new cTableCell { Value = N_.EventDate.ToString(DateFormat) });//舉辦日期
                cTR.Cs.Add(new cTableCell { Value = N_.STime.Hours + ":" + N_.STime.Minutes + " ~ " + N_.ETime.Hours + ":" + N_.ETime.Minutes });//舉辦時間
                //cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/EventSet/Event_Join_List_" + N_.ECID + "/?EID=" + N_.EID, Target = "_self", Value = "管理" });//報名管理
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
        #region 活動編輯-通用物件
        public class cEvent_Edit
        {
            public Event E = new Event();
            public int ECID = 0;
            public string CTitle = "";
            public List<SelectListItem> WeeklyNoList = new List<SelectListItem>();
            public List<SelectListItem> ML0List = new List<SelectListItem>();
        }

        public cEvent_Edit GetEvent_Edit(FormCollection FC)
        {
            cEvent_Edit c = new cEvent_Edit();
            int EID = GetQueryStringInInt("EID");
            int ECID = GetQueryStringInInt("ECID");
            #region 物件初始化

            var EC = DC.Event_Category.FirstOrDefault(q => q.ECID == ECID && !q.DeleteFlag);
            if (EC == null)
                SetAlert("缺少類別資料...", 3, "/Admin/EventSet/Event_List?CID=1");
            else
            {
                c.ECID = EC.ECID;
                c.CTitle = EC.Title;
            }
            switch (ECID)
            {
                case 1://主日
                    c.E = new Event
                    {
                        ECID = 1,
                        Title = "",
                        EventType = 0,
                        EventInfo = "",
                        CircleFlag = false,
                        EventDate = DT,
                        WeeklyNo = 0,
                        STime = new TimeSpan(9, 0, 0),
                        ETime = new TimeSpan(12, 0, 0),
                        SDate_AllowJoin = DT,
                        EDate_AllowJoin = DT,
                        STime_AllowJoin = new TimeSpan(0, 0, 0),
                        ETime_AllowJoin = new TimeSpan(23, 59, 59),
                        PhoneNo = "",
                        Location_MID = 0,
                        Location_URL = "",
                        Location_Note = "",
                        Note = "",
                        ActiveFlag = true,
                        DeleteFlag = false,
                        CreDate = DT,
                        UpdDate = DT,
                        SaveACID = ACID
                    };
                    break;

                case 2://小組
                    break;
                case 3://事工團
                    break;

                default://其他活動
                    break;
            }

            //每周下拉
            c.WeeklyNoList = new List<SelectListItem>();
            for (int i = 0; i < sWeeks.Length; i++)
                c.WeeklyNoList.Add(new SelectListItem { Text = sWeeks[i], Value = i.ToString(), Selected = i == 0 });
            //主日聚會點下拉
            int SetType = ECID == 1 ? 0 : (ECID == 1 ? 1 : (ECID == 2 ? 2 : 0));
            c.ML0List = new List<SelectListItem>();
            var MLs = from q in DC.OrganizeInfo.Where(q => !q.DeleteFlag)
                      join p in DC.Meeting_Location_Set.Where(q => q.SetType == SetType && !q.DeleteFlag)
                      on q.OIID equals p.OIID
                      select new
                      {
                          MLSID = p.MLSID,
                          OID = q.OID,
                          OIID = q.OIID,
                          OTitle = q.Organize.Title,
                          OITitle = q.Title,
                          MLID = p.MLID,
                          MLTitle = p.Meeting_Location.Title
                      };
            foreach (var ML in MLs.OrderBy(q => q.OID).ThenBy(q => q.OIID))
                c.ML0List.Add(new SelectListItem { Text = ML.OITitle + ML.OTitle + ":" + ML.MLTitle, Value = ML.MLSID.ToString() });
            if (MLs.Count() == 0)
                c.ML0List.Add(new SelectListItem { Text = "無主日聚會點可供選擇", Value = "0", Selected = true });
            else
                c.ML0List[0].Selected = true;
            #endregion
            #region 資料庫載入
            if (c.ECID > 0)
            {
                if (EID > 0)
                {
                    c.E = DC.Event.FirstOrDefault(q => q.ECID == c.ECID && q.EID == EID && !q.DeleteFlag);

                    c.WeeklyNoList.ForEach(q => q.Selected = false);
                    c.WeeklyNoList.First(q => q.Value == c.E.WeeklyNo.ToString()).Selected = true;

                    if (c.E.ECID == 1 && c.E.Location_MID > 0)//主日聚會+實體
                    {
                        c.ML0List.ForEach(q => q.Selected = false);
                        c.ML0List.First(q => q.Value == c.E.Location_MID.ToString()).Selected = true;
                    }
                }
            }


            #endregion
            #region 前端資料載入
            if (FC != null)
                if (FC.AllKeys.Count() == 0)
                    FC = null;
            if (FC != null)
            {
                c.E.Title = GetStringValue(c.E.Title, FC, "txb_Title");
                if (c.E.Title == "" && c.ML0List.Any(q => q.Selected))
                    c.E.Title = c.ML0List.First(q => q.Selected).Text + " 的主日聚會";
                c.E.EventInfo = GetStringValue(c.E.Title, FC, "txb_EventInfo");
                c.E.EventType = Convert.ToInt32(FC.Get("rbl_EventType"));
                if (c.E.ECID == 1)//主日聚會欄位
                {
                    if (c.E.EventType == 0 || c.E.EventType == 1)//不限制或實體
                    {
                        string sMID = GetStringValue("0", FC, "ddl_ML");
                        var ML = DC.Meeting_Location_Set.FirstOrDefault(q => q.MLSID.ToString() == sMID);
                        if (ML != null)
                            c.E.Location_MID = ML.MLSID;
                    }
                    else
                        c.E.Location_MID = 0;

                    if (c.E.EventType == 0 || c.E.EventType == 2)//不限制或線上
                        c.E.Location_URL = GetStringValue(c.E.Location_URL, FC, "txb_URL");
                    else
                        c.E.Location_URL = "";
                    c.E.CircleFlag = true;
                    c.E.WeeklyNo = Convert.ToInt32(FC.Get("ddl_Weekly"));
                    c.E.STime = TimeSpan.Parse(FC.Get("txb_STime"));
                    c.E.ETime = TimeSpan.Parse(FC.Get("txb_ETime"));
                }

                c.E.Location_Note = GetStringValue(c.E.Location_Note, FC, "txb_LocationNote");
                c.E.Note = GetStringValue(c.E.Note, FC, "txb_Note");
                c.E.ActiveFlag = GetViewCheckBox("cbox_ActiveFlag");
                if (c.E.EID > 0)
                    c.E.DeleteFlag = GetViewCheckBox("cbox_DeleteFlag");
                c.E.UpdDate = DT;
                c.E.SaveACID = ACID;


            }
            #endregion
            return c;
        }
        

        #endregion
        #region 活動編輯-主日聚會
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
            Error = "";
            if (c.E.STime >= c.E.ETime)
                Error += "聚會起始時間與結束時間有誤";
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (c.E.EID == 0)
                    DC.Event.InsertOnSubmit(c.E);
                DC.SubmitChanges();

                SetAlert("存檔完成", 1, "/Admin/EventSet/Event_List?CID=1");
            }
            return View(c);
        }
        #endregion

    }
}