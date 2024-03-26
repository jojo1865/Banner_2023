using Banner.Models;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using static Banner.Areas.Admin.Controllers.StaffSetController;

namespace Banner.Areas.Web.Controllers
{
    public class StaffPlaceController : PublicClass
    {
        #region 取事工團ID

        private int SID = 0;
        private void GetSID()
        {
            SID = 0;
            if (GetQueryStringInInt("SID") > 0)
            {
                SID = GetQueryStringInInt("SID");
                SetBrowserData("SID", SID.ToString());
            }
            else if (GetBrowserData("SID") != "")
                SID = Convert.ToInt32(GetBrowserData("SID"));

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

        #endregion
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
            GetSID();
            var TopTitles = new List<cTableCell>();

            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 300 });
            TopTitles.Add(new cTableCell { Title = "活動類型", WidthPX = 80 });
            TopTitles.Add(new cTableCell { Title = "標題" });
            TopTitles.Add(new cTableCell { Title = "活動日期", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "活動時間", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "活動內容" });
            TopTitles.Add(new cTableCell { Title = "參加人數", WidthPX = 100 });

            cTL.Rs.Add(SetTableRowTitle(TopTitles));
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";

            var Ns = DC.Event_Join_Header.Where(q => q.TargetType == 1 && q.TargetID == SID && !q.Event.DeleteFlag);
            cTL.TotalCt = Ns.Count();
            cTL.MaxNum = GetMaxNum(cTL.TotalCt, cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.Event.EventDate).ThenByDescending(q => q.Event.STime).Skip((iNowPage - 1) * cTL.NumCut).Take(cTL.NumCut);
            int iTotal = DC.M_Staff_Account.Count(q => q.Account.ActiveFlag && !q.Account.DeleteFlag && q.SID == SID && q.ActiveFlag && !q.DeleteFlag);
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                //操作
                cTableCell cTC = new cTableCell();
                if (N.Event.EventDate.Date >= DT.Date || N.Event.CircleFlag)//過期後就不能編輯
                    cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Web/StaffPlace/Event_Edit?SID=" + SID + "&EHID=" + N.EJHID, Target = "_self", Value = "編輯" });
                cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Web/StaffPlace/Event_QRCode?SID=" + SID + "&EHID=" + N.EJHID, Target = "_black", Value = "報到QR-Code" });
                cTR.Cs.Add(cTC);
                //活動類型
                cTR.Cs.Add(new cTableCell { Value = sCourseType[N.Event.EventType] });
                //標題
                cTR.Cs.Add(new cTableCell { Value = N.Event.Title });
                //活動日期
                cTR.Cs.Add(new cTableCell { Value = N.Event.EventDate.ToString(DateFormat) });
                //活動時間
                cTR.Cs.Add(new cTableCell { Value = GetTimeSpanToString(N.Event.STime) + "~" + GetTimeSpanToString(N.Event.ETime) });
                //活動內容
                cTR.Cs.Add(new cTableCell { Value = N.Event.EventInfo });
                //參加人數
                cTR.Cs.Add(new cTableCell { Type = "link", URL = "/Web/StaffPlace/EventJoin_List?SID=" + SID + "&EHID=" + N.EJHID, Target = "_black", Value = N.Event_Join_Detail.Count(q => !q.DeleteFlag) + "/" + iTotal });

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
        public class cEvent_Edit
        {
            public int OIID { get; set; }
            public int EHID { get; set; }
            public string Title { get; set; }
            public string Info { get; set; }
            public DateTime EventDate { get; set; }
            public TimeSpan STime { get; set; }
            public TimeSpan ETime { get; set; }
            public List<SelectListItem> ddl_EventType { get; set; }

            public Location L { get; set; }
            public Contect C { get; set; }
            public bool ActiveFlag { get; set; }
            public bool DeleteFlag { get; set; }
        }
        public cEvent_Edit GetEvent_Edit(FormCollection FC)
        {
            cEvent_Edit N = new cEvent_Edit();
            N.EHID = GetQueryStringInInt("EHID");
            N.OIID = GetQueryStringInInt("OIID");
            GetSID();

            #region 資料庫讀取
            var EH = DC.Event_Join_Header.FirstOrDefault(q => q.TargetType == 1 && q.TargetID == SID && q.EJHID == N.EHID && !q.Event.DeleteFlag);
            if (EH == null && N.EHID > 0)
                Error += "此活動已被移除<br/>";
            else if (EH == null)
            {
                N.Title = "";
                N.Info = "";
                N.EventDate = DT;
                N.STime = TimeSpan.Parse("09:00");
                N.ETime = TimeSpan.Parse("12:00");
                N.ActiveFlag = true;
                N.DeleteFlag = false;
                N.ddl_EventType = new List<SelectListItem> {
                    new SelectListItem{ Text=sCourseType[1],Value="1",Selected=true},
                    new SelectListItem{ Text=sCourseType[2],Value="2",Selected=false}
                };
                N.L = new Location
                {
                    LID = 0,
                    TargetType = 5,
                    TargetID = 0,
                    ZID = 318,
                    Address = ""
                };
                N.C = new Contect
                {
                    CID = 0,
                    TargetType = 5,
                    TargetID = 0,
                    ZID = 10,
                    ContectType = 1,
                    ContectValue = "",
                    CheckFlag = false,
                    CreDate = DT,
                    CheckDate = DT
                };
            }
            else
            {
                N.Title = EH.Event.Title;
                N.Info = EH.Event.EventInfo;
                N.EventDate = EH.Event.EventDate;
                N.STime = EH.Event.STime;
                N.ETime = EH.Event.ETime;
                N.ActiveFlag = EH.Event.ActiveFlag;
                N.DeleteFlag = EH.Event.DeleteFlag;
                N.ddl_EventType = new List<SelectListItem> {
                    new SelectListItem{ Text=sCourseType[1],Value="1",Selected=true},
                    new SelectListItem{ Text=sCourseType[2],Value="2",Selected=false}
                };
                var L = DC.Location.FirstOrDefault(q => q.TargetType == 5 && q.TargetID == EH.EID);
                if (L != null)
                    N.L = L;
                else
                {
                    N.L = new Location
                    {
                        LID = 0,
                        TargetType = 5,
                        TargetID = 0,
                        ZID = 318,
                        Address = ""
                    };
                }
                var C = DC.Contect.FirstOrDefault(q => q.TargetType == 5 && q.TargetID == EH.EID);
                if (C != null)
                    N.C = C;
                else
                {
                    N.C = new Contect
                    {
                        CID = 0,
                        TargetType = 5,
                        TargetID = 0,
                        ZID = 10,
                        ContectType = 1,
                        ContectValue = "",
                        CheckFlag = false,
                        CreDate = DT,
                        CheckDate = DT
                    };
                }
            }
            #endregion
            #region 前端匯入
            if (FC != null)
            {
                N.Title = FC.Get("txb_Title");
                N.Info = FC.Get("txb_Info");
                DateTime DT_ = DT;
                if (DateTime.TryParse(FC.Get("txb_EventDate"), out DT_))
                    N.EventDate = DT_;
                else
                    Error += "活動日期輸入錯誤<br/>";
                TimeSpan TS = new TimeSpan();
                if (TimeSpan.TryParse(FC.Get("txb_STime"), out TS))
                    N.STime = TS;
                else
                    Error += "起始時間輸入錯誤<br/>";
                if (TimeSpan.TryParse(FC.Get("txb_ETime"), out TS))
                    N.ETime = TS;
                else
                    Error += "結束時間輸入錯誤<br/>";
                if (N.STime >= N.ETime)
                    Error += "活動起始與結束時間輸入錯誤<br/>";

                N.ddl_EventType.ForEach(q => q.Selected = false);
                N.ddl_EventType.First(q => q.Value == FC.Get("ddl_MeetType")).Selected = true;
                N.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                N.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                N.C.ContectValue = FC.Get("txb_PhoneNo");
                N.C.ZID = Convert.ToInt32(FC.Get("ddl_PhoneZip"));
                if (FC.Get("ddl_Zip0") == "10")
                {
                    N.L.ZID = Convert.ToInt32(FC.Get("ddl_Zip2"));
                    N.L.Address = FC.Get("txb_Address0");
                }
                else if (FC.Get("ddl_Zip0") == "2")
                {
                    N.L.ZID = Convert.ToInt32(FC.Get("ddl_Zip3"));
                    N.L.Address = FC.Get("txb_Address1_1") + "\n" + FC.Get("txb_Address1_2");
                }
                else
                {
                    N.L.ZID = Convert.ToInt32(FC.Get("ddl_Zip0"));
                    N.L.Address = FC.Get("txb_Address2");
                }
            }
            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult Event_Edit()
        {
            GetViewBag();
            var N = GetEvent_Edit(null);
            if (Error != "")
                SetAlert(Error, 2, "/Web/Home/Index");

            return View(N);
        }
        [HttpPost]
        public ActionResult Event_Edit(FormCollection FC)
        {
            GetViewBag();
            ACID = GetACID();
            var N = GetEvent_Edit(FC);
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (N.EHID == 0)//新增活動
                {
                    #region 活動本體
                    Event E = new Event();
                    E.ECID = 3;
                    E.Title = N.Title;
                    if (N.ddl_EventType.Any(q => q.Selected))
                        E.EventType = Convert.ToInt32(N.ddl_EventType.First(q => q.Selected).Value);
                    else
                        E.EventType = 1;
                    E.EventInfo = N.Info;
                    E.CircleFlag = false;
                    E.EventDate = N.EventDate;
                    E.WeeklyNo = 0;
                    E.STime = N.STime;
                    E.ETime = N.ETime;
                    E.SDate_AllowJoin = DT;
                    E.EDate_AllowJoin = N.EventDate;
                    E.STime_AllowJoin = N.STime;
                    E.ETime_AllowJoin = N.ETime;
                    E.PhoneNo = N.C.ContectValue;
                    E.Location_MID = 0;
                    E.Location_URL = "";
                    E.Location_Note = "";
                    E.Note = "";
                    E.ActiveFlag = N.ActiveFlag;
                    E.DeleteFlag = N.DeleteFlag;
                    E.CreDate = DT;
                    E.UpdDate = DT;
                    E.SaveACID = ACID;
                    DC.Event.InsertOnSubmit(E);
                    DC.SubmitChanges();
                    #endregion
                    #region 活動-事工團連結
                    Event_Join_Header EH = new Event_Join_Header();
                    EH.Event = E;
                    EH.TargetType = 1;
                    EH.TargetID = SID;
                    EH.OIID = N.OIID;
                    EH.EventDate = N.EventDate;
                    EH.Note = "";
                    EH.CreDate = DT;
                    EH.UpdDate = DT;
                    EH.SaveACID = ACID;
                    DC.Event_Join_Header.InsertOnSubmit(EH);
                    DC.SubmitChanges();
                    #endregion
                    #region 活動-地址
                    Location L = new Location();
                    N.L.TargetID = E.EID;
                    DC.Location.InsertOnSubmit(N.L);
                    DC.SubmitChanges();
                    #endregion
                    #region 活動-電話
                    N.C.TargetID = E.EID;
                    DC.Contect.InsertOnSubmit(N.C);
                    DC.SubmitChanges();
                    #endregion
                    SetAlert("新增活動完成", 1, "/Web/StaffPlace/Event_List?SID=" + SID);
                }
                else
                {
                    var EH = DC.Event_Join_Header.FirstOrDefault(q => q.EJHID == N.EHID);
                    if (EH == null)
                        SetAlert("更新活動失敗,遺失原始活動資料,請建立新活動", 2, "/Web/StaffPlace/Event_Edit?SID=" + SID);
                    else
                    {
                        EH.EventDate = N.EventDate;
                        EH.UpdDate = DT;
                        EH.SaveACID = ACID;
                        EH.Event.Title = N.Title;
                        if (N.ddl_EventType.Any(q => q.Selected))
                            EH.Event.EventType = Convert.ToInt32(N.ddl_EventType.First(q => q.Selected).Value);
                        else
                            EH.Event.EventType = 1;
                        EH.Event.EventInfo = N.Info;
                        EH.Event.EventDate = N.EventDate;
                        EH.Event.STime = N.STime;
                        EH.Event.ETime = N.ETime;
                        EH.Event.EDate_AllowJoin = N.EventDate;
                        EH.Event.STime_AllowJoin = N.STime;
                        EH.Event.ETime_AllowJoin = N.ETime;
                        EH.Event.PhoneNo = N.C.ContectValue;
                        EH.Event.ActiveFlag = N.ActiveFlag;
                        EH.Event.DeleteFlag = N.DeleteFlag;
                        EH.Event.UpdDate = DT;
                        EH.Event.SaveACID = ACID;


                        DC.SubmitChanges();
                        SetAlert("更新活動完成", 1, "/Web/StaffPlace/Event_List?SID=" + SID);


                    }
                }
            }

            return View(N);
        }
        #endregion
        #region 小組聚會登記QRCode
        [HttpGet]
        public ActionResult Event_QRCode()
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";
            GetSID();
            int EHID = GetQueryStringInInt("EHID");
            string sQRCode_URL = Create_QRCode("/Web/AccountPlace/StaffEvent_Join/" + EHID);
            TempData["QRCode_URL"] = sQRCode_URL;
            TempData["JoinGroup_URL"] = ("/Web/AccountPlace/StaffEvent_Join/" + EHID);

            TempData["QRCode_Show"] = false;
            var EH = DC.Event_Join_Header.FirstOrDefault(q => q.EJHID == EHID && !q.Event.DeleteFlag && q.TargetID == SID && q.TargetType == 1);
            if (EH != null)
            {
                if (EH.EventDate.Date == DT.Date)
                {
                    DateTime STime = Convert.ToDateTime(EH.EventDate.ToString(DateFormat) + " " + GetTimeSpanToString(EH.Event.STime));
                    DateTime ETime = Convert.ToDateTime(EH.EventDate.ToString(DateFormat) + " " + GetTimeSpanToString(EH.Event.ETime));
                    if (DT < STime.AddMinutes(EventQrCodeSTimeAdd) || DT > ETime.AddMinutes(EventQrCodeETimeAdd))
                        TempData["QRCode_Msg"] = "本事工團活動可打卡時間為" + STime.AddMinutes(EventQrCodeSTimeAdd).ToString(DateTimeFormat) + " 至 " + ETime.AddMinutes(EventQrCodeETimeAdd).ToString(DateTimeFormat);
                    else
                    {
                        TempData["QRCode_Show"] = true;
                        TempData["QRCode_Msg"] = "活動時間:" + GetTimeSpanToString(EH.Event.STime) + " - " + GetTimeSpanToString(EH.Event.ETime);
                    }
                }
                else
                    TempData["QRCode_Msg"] = "今天不是活動日,不能打卡";
            }
            else
                TempData["QRCode_Msg"] = "目前非事工團可打卡時間...";


            return View();
        }
        #endregion
        #region 活動報名列表
        public class cGetEventJoin_List
        {
            public cTableList cTL = new cTableList();
        }
        public cGetEventJoin_List GetEventJoin_List(FormCollection FC)
        {
            cGetEventJoin_List c = new cGetEventJoin_List();

            c.cTL.Title = "團員簽到列表";
            c.cTL.NowPage = 1;
            c.cTL.NumCut = 0;
            c.cTL.Rs = new List<cTableRow>();
            GetSID();
            int EHID = GetQueryStringInInt("EHID");
            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "序號", WidthPX = 80 });
            TopTitles.Add(new cTableCell { Title = "ID", WidthPX = 80 });
            TopTitles.Add(new cTableCell { Title = "姓名" });
            TopTitles.Add(new cTableCell { Title = "簽到時間", WidthPX = 160 });
            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";

            var Ns = DC.M_Staff_Account.Where(q => !q.DeleteFlag && q.SID == SID);
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            //Ns = Ns.OrderBy(q => q.Account.Name_First).ThenBy(q=>q.Account.Name_Last).Skip((iNowPage - 1) * cTL.NumCut).Take(cTL.NumCut);
            Ns = Ns.OrderBy(q => q.Account.Name_First).ThenBy(q => q.Account.Name_Last);

            var EDs = DC.Event_Join_Detail.Where(q => q.EJHID == EHID && !q.DeleteFlag).ToList();
            var EH = DC.Event_Join_Header.FirstOrDefault(q => q.EJHID == EHID);
            if (EH != null)
                ViewBag._StaffTitle = ViewBag._StaffTitle + " " + EH.Event.Title;
            int i = 1;
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                //序號
                cTR.Cs.Add(new cTableCell { Value = (i++).ToString() });
                //ID
                cTR.Cs.Add(new cTableCell { Value = N.ACID.ToString() });
                //姓名
                cTR.Cs.Add(new cTableCell { Value = N.Account.Name_First + N.Account.Name_Last });
                //簽到時間
                var ED = EDs.FirstOrDefault(q => q.ACID == N.ACID);
                if (ED == null)
                    cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "javascript:JoinEvent(" + EHID + "," + N.ACID + ");", Target = "_self", Value = "補打卡" });
                else if (ED.ACID == ED.SaveACID)//本人打卡
                    cTR.Cs.Add(new cTableCell { Value = ED.JoinDate.ToString(DateTimeFormat) });
                else
                {
                    cTableCell cT = new cTableCell();
                    cT.cTCs.Add(new cTableCell { Value = ED.JoinDate.ToString(DateTimeFormat) });
                    cT.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "javascript:RemoveEvent(" + ED.EJDID + ");", Target = "_self", Value = "移除打卡" });
                    cTR.Cs.Add(cT);
                }
                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }

            return c;
        }
        public ActionResult EventJoin_List()
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            return View(GetEventJoin_List(null));
        }
        [HttpPost]
        public ActionResult EventJoin_List(FormCollection FC)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            return View(GetEventJoin_List(FC));
        }
        #endregion
        #region 補打卡API
        [HttpGet]
        public string StaffEvent_LeaderJoin(int EHID, int ACID)
        {
            int LeaderID = GetACID();
            var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID && !q.DeleteFlag && q.ActiveFlag);
            var EH = DC.Event_Join_Header.FirstOrDefault(q => q.EJHID == EHID && !q.Event.DeleteFlag && q.TargetType == 1);
            string Error = "";
            if (AC == null)
                Error += "查無此會員<br/>";
            else if (EH == null)
                Error += "缺少事工團活動資訊,無法補打卡<br/>";
            else if (EH.Event.DeleteFlag)
                Error += "該活動已被移除,無法補打卡<br/>";
            else if (!EH.Event.ActiveFlag)
                Error += "該活動未被啟用,無法補打卡<br/>";
            else
            {
                var MSA = DC.M_Staff_Account.FirstOrDefault(q => q.SID == EH.TargetID && q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag);
                if (MSA == null)
                    Error += "此員並非本事工團成員,無法補打卡<br/>";
                else if (EH.Event_Join_Detail.Any(q => !q.DeleteFlag && q.ACID == ACID))
                    Error += "此員已打卡過了,請勿重複打卡<br/>";
                else
                {
                    Event_Join_Detail EJD = DC.Event_Join_Detail.FirstOrDefault(q => q.EJHID == EH.EJHID && q.ACID == ACID);
                    if (EJD == null)
                    {
                        EJD = new Event_Join_Detail();
                        EJD.Event_Join_Header = EH;
                        EJD.ACID = ACID;
                        EJD.Name = AC.Name_Last + AC.Name_Last;
                        EJD.PhoneNo = "";
                        EJD.DeleteFlag = false;
                        EJD.JoinDate = DT;
                        EJD.CreDate = DT;
                        EJD.UpdDate = DT;
                        EJD.SaveACID = LeaderID;
                        DC.Event_Join_Detail.InsertOnSubmit(EJD);
                        DC.SubmitChanges();
                    }
                    else
                    {
                        EJD.Name = AC.Name_Last + AC.Name_Last;
                        EJD.DeleteFlag = false;
                        EJD.JoinDate = DT;
                        EJD.UpdDate = DT;
                        EJD.SaveACID = LeaderID;
                        DC.SubmitChanges();
                    }
                }
            }
            return Error;
        }
        #endregion
        #region 移除打卡API
        [HttpGet]
        public string StaffEvent_LeaderRemove(int EDID)
        {
            int ACID = GetACID();
            var ED = DC.Event_Join_Detail.FirstOrDefault(q => q.EJDID == EDID);
            var MACs = DC.M_Staff_Account.Where(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag && q.LeaderFlag);
            string Error = "";

            if (ED == null)
                Error += "缺少事工團活動資訊,無法移除打卡<br/>";
            else if (ED.DeleteFlag)
                Error += "該打卡紀錄已被移除<br/>";
            else if (ED.SaveACID == ED.ACID)
                Error += "本人的打卡紀錄不能被移除<br/>";
            else if (!MACs.Any(q => q.SID == ED.Event_Join_Header.TargetID && q.OIID == ED.Event_Join_Header.OIID))
                Error += "您並非本團主責,不能進行移除作業<br/>";
            else
            {
                ED.DeleteFlag = true;
                ED.UpdDate = DT;
                ED.SaveACID = ACID;
                DC.SubmitChanges();
            }
            return Error;
        }
        #endregion
        #region 我的事工團活動歷程
        public class cGetMyStaffEvnet_List
        {
            public ListSelect LS = new ListSelect();
            public string SDate = "";
            public string EDate = "";
            public cTableList cTL = new cTableList();
        }
        public cGetMyStaffEvnet_List GetMyStaffEvnet_List(FormCollection FC)
        {
            cGetMyStaffEvnet_List c = new cGetMyStaffEvnet_List();
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.cTL.Title = "事工團活動列表";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();
            ACID = GetACID();
            #region 物件初始化
            c.LS = new ListSelect();
            c.LS.Title = "所屬事工團";
            c.LS.ControlName = "ddl_StaffTitle";
            c.LS.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            c.LS.ddlList.AddRange(from q in DC.M_Staff_Account.Where(q => q.ACID == ACID && !q.DeleteFlag && q.ActiveFlag).OrderBy(q => q.OIID).ThenBy(q => q.Staff.SCID).ThenBy(q => q.SID)
                                  select new SelectListItem { Text = "[" + q.OrganizeInfo.Title + q.OrganizeInfo.Organize.Title + "]" + q.Staff.Staff_Category.Title + " " + q.Staff.Title, Value = q.OIID + "," + q.SID });

            #endregion

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "活動類型", WidthPX = 80 });
            TopTitles.Add(new cTableCell { Title = "標題" });
            TopTitles.Add(new cTableCell { Title = "活動日期", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "活動時間", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "活動內容" });
            TopTitles.Add(new cTableCell { Title = "簽到時間", WidthPX = 160 });

            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";

            var Ns = DC.Event_Join_Detail.Where(q => q.ACID == ACID && q.Event_Join_Header.TargetType == 1 && !q.Event_Join_Header.Event.DeleteFlag);
            #region 篩選
            if (FC != null)
            {
                string IDs = FC.Get("ddl_StaffTitle");
                string[] sIDs = IDs.Split(',');
                if (sIDs.Length == 2)
                {
                    c.LS.ddlList.ForEach(q => q.Selected = false);
                    c.LS.ddlList.First(q => q.Value == IDs).Selected = true;

                    Ns = Ns.Where(q => q.Event_Join_Header.OIID.ToString() == sIDs[0] && q.Event_Join_Header.TargetID.ToString() == sIDs[1]);
                }

                DateTime DT_ = DateTime.Now;
                if (DateTime.TryParse(FC.Get("txb_SDate"), out DT_))
                    Ns = Ns.Where(q => q.Event_Join_Header.EventDate.Date >= DT.Date);
                if (DateTime.TryParse(FC.Get("txb_EDate"), out DT_))
                    Ns = Ns.Where(q => q.Event_Join_Header.EventDate.Date <= DT.Date);
            }
            #endregion

            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.Event_Join_Header.Event.EventDate).ThenByDescending(q => q.Event_Join_Header.Event.STime).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                //活動類型
                cTR.Cs.Add(new cTableCell { Value = sCourseType[N.Event_Join_Header.Event.EventType] });
                //標題
                cTR.Cs.Add(new cTableCell { Value = N.Event_Join_Header.Event.Title });
                //活動日期
                cTR.Cs.Add(new cTableCell { Value = N.Event_Join_Header.Event.EventDate.ToString(DateFormat) });
                //活動時間
                cTR.Cs.Add(new cTableCell { Value = GetTimeSpanToString(N.Event_Join_Header.Event.STime) + "~" + GetTimeSpanToString(N.Event_Join_Header.Event.ETime) });
                //活動內容
                cTR.Cs.Add(new cTableCell { Value = N.Event_Join_Header.Event.EventInfo });
                //簽到時間
                cTR.Cs.Add(new cTableCell { Value = N.JoinDate.ToString(DateTimeFormat) });

                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }

            return c;
        }
        [HttpGet]
        public ActionResult MyStaffEvnet_List()
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            return View(GetMyStaffEvnet_List(null));
        }
        [HttpPost]
        public ActionResult MyStaffEvnet_List(FormCollection FC)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            return View(GetMyStaffEvnet_List(FC));
        }

        #endregion
        #region 團員管理
        [HttpGet]
        public ActionResult StaffAccount_List(int SID,int OIID)
        {
            GetViewBag();
            return View(GetStaffAccount_List(SID));
        }

        #endregion
    }
}