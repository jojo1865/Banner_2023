using Banner.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Admin.Controllers
{
    public class MeetingLocationController : PublicClass
    {
        // GET: Admin/MeetingLocation
        #region 主日聚會列表
        public class cJoinCt_List
        {
            public string sKey = "";
            public string sDate = "";
            public int MLSID = 0;
            public cTableList cTL = new cTableList();
            public List<SelectListItem> MLS_SL = new List<SelectListItem>();
        }
        public cJoinCt_List GetJoinCt_List(FormCollection FC)
        {
            cJoinCt_List c = new cJoinCt_List();
            #region 物件初始化
            //聚會點初始化
            c.MLS_SL.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            var Ms = from q in DC.Meeting_Location_Set.Where(q => q.SetType == 0 && q.ActiveFlag && !q.DeleteFlag)
                     join p in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag)
                     on q.OIID equals p.OIID
                     select new { p.OIID, p.OID, OITitle = p.Title, OTitle = p.Organize.Title, q.MLSID, ML_Title = q.Meeting_Location.Title };
            ACID = GetACID();
            if (ACID != 1)//非管理者
            {
                Ms = from q in Ms
                     join p in DC.M_OI2_Account.Where(q => q.ACID == ACID)
                     on q.OIID equals p.OIID
                     select q;
            }
            Ms = Ms.OrderBy(q => q.OID).ThenBy(q => q.OIID);
            foreach (var M in Ms)
                c.MLS_SL.Add(new SelectListItem { Text = M.OITitle + M.OTitle + ":" + M.ML_Title, Value = M.MLSID.ToString() });

            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.cTL = new cTableList();
            c.cTL.Title = "";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();
            #endregion
            #region 前端資料載入
            if (FC != null)
            {
                c.sKey = FC.Get("txb_Key");
                c.sDate = FC.Get("txb_Date");
                string sMLSID = FC.Get("ddl_MLS");

                if (Ms.Any(q => q.MLSID.ToString() == sMLSID))
                {
                    c.MLS_SL.ForEach(q => q.Selected = false);
                    c.MLS_SL.Find(q => q.Value == sMLSID).Selected = true;

                    c.MLSID = Convert.ToInt32(sMLSID);
                }
            }
            #endregion
            #region 資料庫載入
            var Ns = DC.Meeting_Location_Used.Where(q => !q.DeleteFlag && q.Meeting_Location_Set.SetType == 0 && q.Meeting_Location_Set.OIID > 0);
            if (ACID != 1)//非管理者
            {
                Ns = from q in Ns
                     join p in Ms
                     on q.Meeting_Location_Set.OIID equals p.OIID
                     select q;
            }
            else
            {
                Ns = from q in Ns
                     join p in DC.OrganizeInfo.Where(q => !q.DeleteFlag)
                     on q.Meeting_Location_Set.OIID equals p.OIID
                     select q;
            }
            if (c.MLSID > 0)
                Ns = Ns.Where(q => q.MLSID == c.MLSID);
            if (c.sKey != "")
                Ns = Ns.Where(q => q.Title.Contains(c.sKey));
            if (c.sDate != "")
            {
                DateTime DT_ = DT;
                if (DateTime.TryParse(c.sDate, out DT_))
                    Ns = Ns.Where(q => q.S_DateTime.Date == DT_);
            }

            #endregion
            #region 組成表單
            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "旌旗" });
            TopTitles.Add(new cTableCell { Title = "聚會點" });
            TopTitles.Add(new cTableCell { Title = "名稱" });
            TopTitles.Add(new cTableCell { Title = "舉辦日期" });
            TopTitles.Add(new cTableCell { Title = "舉辦時間" });
            TopTitles.Add(new cTableCell { Title = "兒童人數" });
            TopTitles.Add(new cTableCell { Title = "青年人數" });
            TopTitles.Add(new cTableCell { Title = "成人人數" });
            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.S_DateTime).ThenByDescending(q => q.CreDate).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);
            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == N_.Meeting_Location_Set.OIID);
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/MeetingLocation/JoinCt_Edit/" + N_.MLUID, Target = "_self", Value = "編輯" });//操作
                cTR.Cs.Add(new cTableCell { Value = OI.Title + OI.Organize.Title });//旌旗
                cTR.Cs.Add(new cTableCell { Value = N_.Meeting_Location_Set.Meeting_Location.Title });//聚會點
                cTR.Cs.Add(new cTableCell { Value = N_.Title });//名稱
                cTR.Cs.Add(new cTableCell { Value = N_.S_DateTime.ToString(DateFormat) });//舉辦日期
                cTR.Cs.Add(new cTableCell { Value = N_.S_DateTime.ToString("HH:mm") + "~" + N_.E_DateTime.ToString("HH:mm") });//舉辦時間
                cTR.Cs.Add(new cTableCell { Value = N_.JoinCt_Child.ToString() });//兒童人數
                cTR.Cs.Add(new cTableCell { Value = N_.JoinCt_Juiner.ToString() });//青年人數
                cTR.Cs.Add(new cTableCell { Value = N_.JoinCt_Aldult.ToString() });//成人人數

                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion

            return c;
        }
        [HttpGet]
        public ActionResult JoinCt_List()
        {
            GetViewBag();
            return View(GetJoinCt_List(null));
        }
        [HttpPost]
        public ActionResult JoinCt_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetJoinCt_List(FC));
        }
        #endregion
        #region 主日聚會紀錄編輯
        public class cJoinCt_Edit
        {
            public Meeting_Location_Used ML = new Meeting_Location_Used();
            public List<SelectListItem> MLS_SL = new List<SelectListItem>();
        }
        public cJoinCt_Edit GetJoinCt_Edit(int ID, FormCollection FC)
        {
            cJoinCt_Edit c = new cJoinCt_Edit();
            #region 物件初始化
            Error = "";
            var Ms = from q in DC.Meeting_Location_Set.Where(q => q.SetType == 0 && q.ActiveFlag && !q.DeleteFlag)
                     join p in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag)
                     on q.OIID equals p.OIID
                     select new { p.OIID, p.OID, OITitle = p.Title, OTitle = p.Organize.Title, q.MLSID, ML_Title = q.Meeting_Location.Title };
            ACID = GetACID();
            if (ACID != 1)//非管理者
            {
                Ms = from q in Ms
                     join p in DC.M_OI2_Account.Where(q => q.ACID == ACID)
                     on q.OIID equals p.OIID
                     select q;
            }
            Ms = Ms.OrderBy(q => q.OID).ThenBy(q => q.OIID);
            foreach (var M in Ms)
                c.MLS_SL.Add(new SelectListItem { Text = M.OITitle + M.OTitle + ":" + M.ML_Title, Value = M.MLSID.ToString() });

            if (Ms.Count() == 0)
                Error += "您所屬旌旗下沒有聚會點資料<br/>";
            #endregion
            #region 資料庫載入
            if (ID > 0)
            {
                c.ML = DC.Meeting_Location_Used.FirstOrDefault(q => q.MLUID == ID);
                if (c.ML != null)
                    if (c.ML.DeleteFlag)
                        Error += "此紀錄已被刪除<br/>";
            }
            else
            {
                c.ML = new Meeting_Location_Used
                {
                    MLSID = Ms.Any() ? Ms.First().MLSID : 1,
                    Title = "",
                    S_DateTime = DT,
                    E_DateTime = DT,
                    JoinCt_Child = 0,
                    JoinCt_Juiner = 0,
                    JoinCt_Aldult = 0,
                    Note = "",
                    DeleteFlag = false,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID
                };
            }
            #endregion
            #region 前端參數輸入
            if (FC != null)
            {
                c.ML.Title = FC.Get("txb_Title");
                c.ML.MLSID = Convert.ToInt32(FC.Get("ddl_ML"));
                c.ML.S_DateTime = Convert.ToDateTime(FC.Get("txb_S_Date") + " " + FC.Get("txb_S_Time"));
                c.ML.E_DateTime = Convert.ToDateTime(FC.Get("txb_S_Date") + " " + FC.Get("txb_E_Time"));
                c.ML.JoinCt_Child = Convert.ToInt32(FC.Get("txb_JoinCt_Child"));
                c.ML.JoinCt_Juiner = Convert.ToInt32(FC.Get("txb_JoinCt_Juiner"));
                c.ML.JoinCt_Aldult = Convert.ToInt32(FC.Get("txb_JoinCt_Aldult"));
                c.ML.Note = FC.Get("txb_Note");
                c.ML.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                c.ML.UpdDate = DT;
                c.ML.SaveACID = ACID;

                c.MLS_SL.ForEach(q => q.Selected = false);
                c.MLS_SL.First(q => q.Value == c.ML.MLSID.ToString()).Selected = true;
            }
            #endregion
            if (Error != "")
                SetAlert(Error, 2, "/Admin/MeetingLocation/JoinCt_List");
            return c;
        }
        [HttpGet]
        public ActionResult JoinCt_Edit(int ID)
        {
            GetViewBag();
            return View(GetJoinCt_Edit(ID, null));
        }
        [HttpPost]
        
        public ActionResult JoinCt_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            var c = GetJoinCt_Edit(ID, FC);
            if (Error == "")
            {
                if (c.ML.MLUID == 0)
                    DC.Meeting_Location_Used.InsertOnSubmit(c.ML);
                DC.SubmitChanges();

                SetAlert("存檔完成", 1, "/Admin/MeetingLocation/JoinCt_List");
            }
            return View(c);
        }
        #endregion
        #region 主日聚會匯出Excel
        [HttpGet]
        public ActionResult JoinCt_Print()
        {
            GetViewBag();
            string Key = GetQueryStringInString("Key");
            string MLSID = GetQueryStringInString("MLSID");
            string sDate = GetQueryStringInString("Date");

            var Ms = from q in DC.Meeting_Location_Set.Where(q => q.SetType == 0 && q.ActiveFlag && !q.DeleteFlag)
                     join p in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag)
                     on q.OIID equals p.OIID
                     select new { p.OIID, p.OID, OITitle = p.Title, OTitle = p.Organize.Title, q.MLSID, ML_Title = q.Meeting_Location.Title };
            ACID = GetACID();

            var Ns = DC.Meeting_Location_Used.Where(q => !q.DeleteFlag && q.Meeting_Location_Set.SetType == 0 && q.Meeting_Location_Set.OIID > 0);
            if (ACID != 1)//非管理者
            {
                Ns = from q in Ns
                     join p in Ms
                     on q.Meeting_Location_Set.OIID equals p.OIID
                     select q;
            }
            else
            {
                Ns = from q in Ns
                     join p in DC.OrganizeInfo.Where(q => !q.DeleteFlag)
                     on q.Meeting_Location_Set.OIID equals p.OIID
                     select q;
            }
            if (MLSID != "" && MLSID != "0")
                Ns = Ns.Where(q => q.MLSID.ToString() == MLSID);
            if (Key != "")
                Ns = Ns.Where(q => q.Title.Contains(Key));
            if (sDate != "")
            {
                DateTime DT_ = DT;
                if (DateTime.TryParse(sDate, out DT_))
                    Ns = Ns.Where(q => q.S_DateTime.Date == DT_);
            }
            Ns = Ns.OrderByDescending(q => q.S_DateTime).ThenByDescending(q => q.CreDate);

            ArrayList AL = new ArrayList();
            ArrayList ALS = new ArrayList();
            ALS.Add("聚會日期");
            ALS.Add("聚會時間");
            ALS.Add("旌旗");
            ALS.Add("聚會點");
            ALS.Add("標題");
            ALS.Add("兒童人數");
            ALS.Add("青年人數");
            ALS.Add("成人人數");
            ALS.Add("備註");
            AL.Add((string[])ALS.ToArray(typeof(string)));
            var OIs = DC.OrganizeInfo.Where(q => !q.DeleteFlag).ToList();
            foreach (var N in Ns)
            {
                var OI = OIs.FirstOrDefault(q => q.OIID == N.Meeting_Location_Set.OIID);
                ALS = new ArrayList();
                ALS.Add(N.S_DateTime.ToString(DateFormat));//聚會日期
                ALS.Add(N.S_DateTime.ToString("HH:mm") + "~" + N.E_DateTime.ToString("HH:mm"));//聚會時間
                ALS.Add(OI != null ? OI.Title + OI.Organize.Title : "");//旌旗
                ALS.Add(N.Meeting_Location_Set.Meeting_Location.Title);//聚會點
                ALS.Add(N.Title);//標題
                ALS.Add(N.JoinCt_Child.ToString());//兒童人數
                ALS.Add(N.JoinCt_Juiner.ToString());//青年人數
                ALS.Add(N.JoinCt_Aldult.ToString());//成人人數
                ALS.Add(N.Note);//備註
                AL.Add((string[])ALS.ToArray(typeof(string)));
            }
            WriteExcelFromString("主日聚會參加紀錄", AL);
            SetAlert("已完成匯出", 1, "");
            return View();
        }
        #endregion
    }
}