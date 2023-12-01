using Banner.Models;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using static Banner.Areas.Admin.Controllers.OrganizeSetController;

namespace Banner.Areas.Web.Controllers
{
    public class GroupPlaceController : PublicClass
    {
        // GET: Web/GroupPlace
        private int OIID = 0;
        private void GetID(int ID = 0)
        {
            if (ID > 0)
            {
                SetBrowserData("OIID", ID.ToString());
                OIID = ID;
            }
            else if (GetBrowserData("OIID") != "")
                OIID = Convert.ToInt32(GetBrowserData("OIID"));
            else if (GetQueryStringInInt("OIID") > 0)
                OIID = GetQueryStringInInt("OIID");

            ViewBag._OIID = OIID;
        }
        #region 小組資訊-首頁-聚會資料
        public class cIndex
        {
            public Meeting_Location_Set MLS = new Meeting_Location_Set();
            public List<SelectListItem> SLIs = new List<SelectListItem>();
            public Location L = new Location();
            public Account AC = new Account();
            public Contect C = new Contect();
        }

        [HttpGet]
        public ActionResult Index(int ID = 0)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";
            GetID(ID);
            cIndex N = new cIndex();
            N.AC = DC.Account.FirstOrDefault(q => q.ACID == ACID);
            if (N.AC != null)
            {
                var Ms = GetMOIAC(8, 0, ACID);
                if (Ms != null)
                {
                    if (Ms.Count() > 0)
                    {
                        var M = Ms.OrderByDescending(q => q.JoinDate).First();
                        OIID = M.OIID;
                        SetBrowserData("OIID", OIID.ToString());
                    }
                }

                for (int i = 1; i < sWeeks.Length; i++)
                    N.SLIs.Add(new SelectListItem { Text = sWeeks[i], Value = (i).ToString(), Selected = i == 0 });
                var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ACID == ACID && q.OID == 8 && !q.DeleteFlag && q.ActiveFlag && q.OIID == OIID);
                if (OI == null)
                    SetAlert("您尚未被指定為某小組的小組長,無法進行設定", 2, "/Web/GroupPlace/Index");
                else
                {
                    N.MLS = DC.Meeting_Location_Set.FirstOrDefault(q => q.SetType == 1 && q.OIID == OI.OIID && !q.DeleteFlag);
                    if (N.MLS == null)
                    {
                        N.MLS = new Meeting_Location_Set
                        {
                            Meeting_Location = new Meeting_Location { Code = OI.OIID.ToString().PadLeft(6, '0'), Title = OI.Title + OI.Organize.Title + "聚會點" },
                            SetType = 1,
                            OIID = OI.OIID,
                            WeeklyNo = 7,
                            TimeNo = 0,
                            S_hour = 9,
                            S_minute = 0,
                            E_hour = 12,
                            E_minute = 0
                        };
                        N.L = new Location { ZID = 10, Address = "", LID = 0 };
                        N.C = new Contect { ZID = 10, ContectType = 0 };
                    }
                    else
                    {
                        N.L = DC.Location.FirstOrDefault(q => q.TargetID == N.MLS.MLSID && q.TargetType == 3);
                        if (N.L == null)
                            N.L = new Location { ZID = 10, Address = "", LID = 0 };
                        N.C = DC.Contect.FirstOrDefault(q => q.TargetID == N.MLS.MLSID && q.TargetType == 3);
                        if (N.C == null)
                            N.C = new Contect { ZID = 10, ContectType = 0 };
                    }

                    N.SLIs.ForEach(q => q.Selected = false);
                    N.SLIs.First(q => q.Value == N.MLS.WeeklyNo.ToString()).Selected = true;
                }
            }
            return View(N);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(int ID, FormCollection FC)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";
            GetID(ID);
            cIndex N = new cIndex();
            N.AC = DC.Account.FirstOrDefault(q => q.ACID == ACID);
            for (int i = 1; i < sWeeks.Length; i++)
                N.SLIs.Add(new SelectListItem { Text = sWeeks[i], Value = (i + 1).ToString(), Selected = i == 0 });

            var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ACID == ACID && q.OID == 8 && !q.DeleteFlag && q.ActiveFlag && q.OIID == OIID);
            if (OI != null)
            {
                N.MLS = DC.Meeting_Location_Set.FirstOrDefault(q => q.SetType == 1 && q.OIID == OI.OIID && !q.DeleteFlag);
                if (N.MLS == null)
                {
                    N.MLS = new Meeting_Location_Set
                    {
                        Meeting_Location = new Meeting_Location
                        {
                            Code = OI.OIID.ToString().PadLeft(6, '0'),
                            Title = OI.Title + OI.Organize.Title + "聚會點",
                            ActiveFlag = true,
                            DeleteFlag = false,
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = ACID
                        },
                        SetType = 1,
                        OIID = OI.OIID,
                        WeeklyNo = 7,
                        TimeNo = 0,
                        S_hour = 9,
                        S_minute = 0,
                        E_hour = 12,
                        E_minute = 0,
                        ActiveFlag = true,
                        DeleteFlag = false,
                        CreDate = DT,
                        UpdDate = DT,
                        SaveACID = ACID
                    };
                    N.L = new Location { ZID = 10, Address = "", LID = 0 };
                    N.C = new Contect { ZID = 10, ContectType = 0 };
                }
                else
                {
                    N.L = DC.Location.FirstOrDefault(q => q.TargetID == N.MLS.MLSID && q.TargetType == 3);
                    if (N.L == null)
                        N.L = new Location { ZID = 10, Address = "", LID = 0 };
                    N.C = DC.Contect.FirstOrDefault(q => q.TargetID == N.MLS.MLSID && q.TargetType == 3);
                    if (N.C == null)
                        N.C = new Contect { ZID = 10, ContectType = 0 };
                }
                if (FC != null)
                {
                    N.MLS.WeeklyNo = Convert.ToInt32(FC.Get("ddl_WeeklyNo"));
                    string[] STime = FC.Get("txb_STime").Split(':');
                    string[] ETime = FC.Get("txb_ETime").Split(':');
                    N.MLS.S_hour = Convert.ToInt32(STime[0]);
                    N.MLS.S_minute = Convert.ToInt32(STime[1]);
                    N.MLS.E_hour = Convert.ToInt32(ETime[0]);
                    N.MLS.E_minute = Convert.ToInt32(ETime[1]);
                    N.MLS.TimeNo = N.MLS.S_hour < 12 ? 1 : (N.MLS.S_hour < 18 ? 2 : 3);
                    if (N.MLS.S_hour > N.MLS.E_hour)
                        Error += "聚會起始時間應小於結束時間</br>";
                    else if (N.MLS.S_minute > N.MLS.E_minute)
                        Error += "聚會起始時間應小於結束時間</br>";


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


                    N.C.ZID = Convert.ToInt32(FC.Get("ddl_PhoneZip"));
                    N.C.ContectValue = FC.Get("txb_PhoneNo");
                }

                N.SLIs.ForEach(q => q.Selected = false);
                N.SLIs.First(q => q.Value == N.MLS.WeeklyNo.ToString()).Selected = true;
            }
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (N.MLS.MLSID == 0)
                    DC.Meeting_Location_Set.InsertOnSubmit(N.MLS);
                DC.SubmitChanges();

                if (N.MLS.MLSID == 0)
                {
                    N.L.TargetID = N.MLS.MLID;
                    DC.Location.InsertOnSubmit(N.L);

                    N.C.TargetID = N.MLS.MLID;
                    N.C.CheckFlag = false;
                    N.C.CreDate = DT;
                    N.C.CheckDate = DT;
                    DC.Contect.InsertOnSubmit(N.C);
                }
                DC.SubmitChanges();
                SetAlert("存檔完成", 1);
            }
            return View(N);
        }
        #endregion
        #region 小組資訊-組員名單管理-小組名單

        public cTableList GetAldult_List(FormCollection FC)
        {
            cTableList cTL = new cTableList();
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            cTL.Title = "小組名單";
            cTL.NowPage = iNowPage;
            cTL.NumCut = iNumCut;
            cTL.Rs = new List<cTableRow>();
            GetID();
            var TopTitles = new List<cTableCell>();

            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 200 });
            TopTitles.Add(new cTableCell { Title = "職分", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "姓名", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "性別", WidthPX = 50 });
            TopTitles.Add(new cTableCell { Title = "生日", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "手機", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "受洗狀態", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "備註" });
            cTL.Rs.Add(SetTableRowTitle(TopTitles));
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID && q.OID == 8 && q.OIID == OIID);
            if (OI != null)
            {
                var Ns = GetMOIAC(OI.OID, OI.OIID, 0).Where(q => q.JoinDate != q.CreDate && q.LeaveDate == q.CreDate && q.ActiveFlag);
                cTL.TotalCt = Ns.Count();
                cTL.MaxNum = GetMaxNum(cTL.TotalCt, cTL.NumCut);
                Ns = Ns.OrderByDescending(q => q.ACID == OI.ACID).ThenByDescending(q => q.CreDate).Skip((iNowPage - 1) * cTL.NumCut).Take(cTL.NumCut);
                foreach (var N in Ns)
                {
                    cTableRow cTR = new cTableRow();
                    //操作
                    cTableCell cTC = new cTableCell();
                    if (N.ACID != ACID)//小組長不能移除自己
                        cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Web/GroupPlace/Aldult_Remove/" + N.MID, Target = "_self", Value = "移除" });
                    cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Web/GroupPlace/Aldult_Edit/" + N.MID, Target = "_self", Value = "編輯" });
                    cTR.Cs.Add(cTC);
                    //職分
                    cTR.Cs.Add(new cTableCell { Value = N.ACID == OI.ACID ? OI.Organize.JobTitle : "小組員" });
                    //姓名
                    cTR.Cs.Add(new cTableCell { Value = N.Account.Name_First + N.Account.Name_Last });
                    //性別
                    cTR.Cs.Add(new cTableCell { Value = N.Account.ManFlag ? "男" : "女" });
                    //生日
                    cTR.Cs.Add(new cTableCell { Value = N.Account.Birthday == N.Account.CreDate ? "--" : N.Account.Birthday.ToString(DateFormat) });
                    //手機
                    var Cons = DC.Contect.Where(q => q.TargetType == 2 && q.ContectType == 1 && q.TargetID == N.ACID).OrderByDescending(q => q.CID);
                    cTR.Cs.Add(new cTableCell { Value = (Cons.Count() > 0 ? string.Join(",", Cons.Select(q => q.ContectValue)) : "--") });
                    //受洗狀態
                    var B = DC.Baptized.Where(q => q.ACID == N.ACID && !q.DeleteFlag).OrderByDescending(q => q.BID).FirstOrDefault();
                    if (B == null)
                        cTR.Cs.Add(new cTableCell { Value = BaptizedType[N.Account.BaptizedType] });//受洗狀態
                    else if (!B.ImplementFlag)
                        cTR.Cs.Add(new cTableCell { Value = "預計於" + B.BaptismDate.ToString(DateFormat) + "受洗" });//受洗狀態
                    else
                        cTR.Cs.Add(new cTableCell { Value = "已於" + B.BaptismDate.ToString(DateFormat) + "受洗" });//受洗狀態
                    //備註
                    var cNote = N.Account.Account_Note.Where(q => q.NoteType == 0 && q.OIID == OI.OIID && !q.DeleteFlag).FirstOrDefault();
                    if (cNote != null)
                        cTR.Cs.Add(new cTableCell { Value = cNote.Note });
                    else
                        cTR.Cs.Add(new cTableCell { Value = "--" });
                    cTL.Rs.Add(SetTableCellSortNo(cTR));
                }
            }
            return cTL;
        }
        [HttpGet]
        public ActionResult Aldult_List(int ID)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            SetBrowserData("OIID", ID.ToString());
            return View(GetAldult_List(null));
        }
        [HttpPost]
        public ActionResult Aldult_List(int ID, FormCollection FC)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            SetBrowserData("OIID", ID.ToString());
            return View(GetAldult_List(FC));
        }
        #endregion
        #region 小組資訊-組員名單管理-小組名單-更新備註
        [HttpGet]
        public ActionResult Aldult_Edit(int ID)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";
            GetID();
            Account_Note N = new Account_Note();
            var M = DC.M_OI_Account.FirstOrDefault(q => !q.DeleteFlag && q.MID == ID && q.OrganizeInfo.ACID == ACID);
            if (M == null)
                SetAlert("查無此小組成員資料", 2, "/Web/GroupPlace/Aldult_List/" + OIID);
            else
            {
                N = DC.Account_Note.FirstOrDefault(q => q.ACID == M.ACID && q.OIID == M.OIID && q.NoteType == 0);
                if (N == null)
                {
                    N = new Account_Note
                    {
                        Account = M.Account,
                        NoteType = 0,
                        OIID = M.OIID,
                        Note = ""
                    };
                }
            }
            return View(N);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Aldult_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";
            GetID();
            Account_Note N = new Account_Note();
            var M = DC.M_OI_Account.FirstOrDefault(q => !q.DeleteFlag && q.MID == ID && q.OrganizeInfo.ACID == ACID && q.OIID == OIID);
            if (M != null)
            {
                N = DC.Account_Note.FirstOrDefault(q => q.ACID == M.ACID && q.OIID == M.OIID && q.NoteType == 0);
                if (N == null)
                {
                    N = new Account_Note();
                    N.Account = M.Account;
                    N.NoteType = 0;
                    N.OIID = M.OIID;
                    N.Note = "";
                }
                if (FC != null)
                {
                    N.Note = FC.Get("txb_Note");
                }


                if (N.ANID == 0)
                {
                    N.DeleteFlag = false;
                    N.CreDate = N.UpdDate = DT;
                    N.SaveACID = ACID;
                    DC.Account_Note.InsertOnSubmit(N);
                    DC.SubmitChanges();
                    SetAlert("新增備註完成", 1, "/Web/GroupPlace/Aldult_List/" + OIID);
                }
                else
                {
                    N.UpdDate = DT;
                    N.SaveACID = ACID;
                    DC.SubmitChanges();
                    SetAlert("更新備註完成", 1, "/Web/GroupPlace/Aldult_List/" + OIID);
                }
            }
            return View(N);
        }
        #endregion
        #region 小組資訊-組員名單管理-小組名單-移除
        [HttpGet]
        public ActionResult Aldult_Remove(int ID)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";
            GetID();
            Account_Note N = new Account_Note();
            var M = DC.M_OI_Account.FirstOrDefault(q => !q.DeleteFlag && q.MID == ID && q.OrganizeInfo.ACID == ACID && q.OIID == OIID);
            if (M == null)
                SetAlert("查無此小組成員資料", 2, "/Web/GroupPlace/Aldult_List/" + OIID);
            else
            {
                N.Account = M.Account;
                N.NoteType = 3;
                N.OIID = M.OIID;
                N.Note = "";
            }
            return View(N);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Aldult_Remove(int ID, FormCollection FC)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";
            GetID();
            ViewBag._OIID = OIID;
            Account_Note N = new Account_Note();
            var M = DC.M_OI_Account.FirstOrDefault(q => !q.DeleteFlag && q.MID == ID && q.OrganizeInfo.ACID == ACID && q.OIID == OIID);
            if (M != null)
            {
                N = new Account_Note();
                N.Account = M.Account;
                N.NoteType = 3;
                N.OIID = M.OIID;
                N.DeleteFlag = false;
                N.CreDate = N.UpdDate = DT;
                N.SaveACID = ACID;
                if (FC != null)
                    N.Note = FC.Get("txb_Note");
                else
                    N.Note = "";

                if (N.Note == "")
                    SetAlert("請填寫移除組員的理由", 4);
                else
                {
                    DC.Account_Note.InsertOnSubmit(N);
                    DC.SubmitChanges();

                    M.Account.GroupType = "有意願";
                    M.Account.UpdDate = DT;
                    N.Account.SaveACID = ACID;

                    M.ActiveFlag = false;
                    M.LeaveDate = DT;
                    M.SaveACID = ACID;
                    DC.SubmitChanges();



                    SetAlert("已將組員移出此小組", 1, "/Web/GroupPlace/Aldult_List/" + M.OIID);

                }

            }
            return View(N);
        }
        #endregion

        #region 小組資訊-組員名單管理-新人名單

        public cTableList GetNew_List(FormCollection FC)
        {
            cTableList cTL = new cTableList();

            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            cTL.Title = "小組名單";
            cTL.NowPage = iNowPage;
            cTL.NumCut = iNumCut;
            cTL.Rs = new List<cTableRow>();
            GetID();

            var TopTitles = new List<cTableCell>();

            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 200 });
            TopTitles.Add(new cTableCell { Title = "姓名", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "性別", WidthPX = 50 });
            TopTitles.Add(new cTableCell { Title = "生日", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "手機", WidthPX = 160 });
            cTL.Rs.Add(SetTableRowTitle(TopTitles));
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID && q.OID == 8 && q.OIID == OIID);
            if (OI != null)
            {
                var Ns = GetMOIAC(OI.OID, OI.OIID, 0).Where(q => q.JoinDate == q.CreDate && q.LeaveDate == q.CreDate && q.ActiveFlag);
                cTL.TotalCt = Ns.Count();
                cTL.MaxNum = GetMaxNum(cTL.TotalCt, cTL.NumCut);
                Ns = Ns.OrderByDescending(q => q.ACID == OI.ACID).ThenByDescending(q => q.CreDate).Skip((iNowPage - 1) * cTL.NumCut).Take(cTL.NumCut);
                foreach (var N in Ns)
                {
                    cTableRow cTR = new cTableRow();
                    //操作
                    cTableCell cTC = new cTableCell();
                    cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Web/GroupPlace/New_Remove/" + N.MID, Target = "_self", Value = "駁回" });
                    cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Web/GroupPlace/New_Edit/" + N.MID, Target = "_self", Value = "落戶" });
                    cTR.Cs.Add(cTC);
                    //姓名
                    cTR.Cs.Add(new cTableCell { Value = N.Account.Name_First + N.Account.Name_Last });
                    //性別
                    cTR.Cs.Add(new cTableCell { Value = N.Account.ManFlag ? "男" : "女" });
                    //生日
                    cTR.Cs.Add(new cTableCell { Value = N.Account.Birthday == N.Account.CreDate ? "--" : N.Account.Birthday.ToString(DateFormat) });
                    //手機
                    var Cons = DC.Contect.Where(q => q.TargetType == 2 && q.ContectType == 1 && q.TargetID == N.ACID).OrderByDescending(q => q.CID);
                    cTR.Cs.Add(new cTableCell { Value = (Cons.Count() > 0 ? string.Join(",", Cons.Select(q => q.ContectValue)) : "--") });
                    cTL.Rs.Add(SetTableCellSortNo(cTR));
                }
            }
            return cTL;
        }
        [HttpGet]
        public ActionResult New_List()
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            return View(GetNew_List(null));
        }
        [HttpPost]
        public ActionResult New_List(FormCollection FC)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            return View(GetNew_List(FC));
        }
        #endregion
        #region 小組資訊-組員名單管理-新人名單-落戶
        [HttpGet]
        public ActionResult New_Edit(int ID)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";
            GetID();
            var M = DC.M_OI_Account.FirstOrDefault(q => !q.DeleteFlag && q.MID == ID && q.OrganizeInfo.ACID == ACID && q.OIID == OIID);
            if (M == null)
                SetAlert("查無此新人資料", 2, "/Web/GroupPlace/New_List/" + OIID);
            else
            {
                M.JoinDate = DT;
                M.UpdDate = DT;
                M.ActiveFlag = true;
                M.SaveACID = ACID;
                DC.SubmitChanges();
                SetAlert("已完成落戶", 1, "/Web/GroupPlace/New_List/" + OIID);
            }
            return View();
        }
        #endregion
        #region 小組資訊-組員名單管理-新人名單-駁回
        [HttpGet]
        public ActionResult New_Remove(int ID)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";
            GetID();
            Account_Note N = new Account_Note();
            var M = DC.M_OI_Account.FirstOrDefault(q => !q.DeleteFlag && q.MID == ID && q.OrganizeInfo.ACID == ACID && q.OIID == OIID);
            if (M == null)
                SetAlert("查無此會員資料", 2, "/Web/GroupPlace/New_List/" + OIID);
            else
            {
                N.Account = M.Account;
                N.NoteType = 1;
                N.OIID = M.OIID;
                N.Note = "";
            }
            return View(N);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult New_Remove(int ID, FormCollection FC)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";
            GetID();
            Account_Note N = new Account_Note();
            var M = DC.M_OI_Account.FirstOrDefault(q => !q.DeleteFlag && q.MID == ID && q.OrganizeInfo.ACID == ACID && q.OIID == OIID);
            if (M != null)
            {
                N = new Account_Note();
                N.Account = M.Account;
                N.NoteType = 1;
                N.OIID = M.OIID;
                N.DeleteFlag = false;
                N.CreDate = N.UpdDate = DT;
                N.SaveACID = ACID;
                if (FC != null)
                    N.Note = FC.Get("txb_Note");
                else
                    N.Note = "";

                if (N.Note == "")
                    SetAlert("請填寫駁回原因", 4);
                else
                {
                    DC.Account_Note.InsertOnSubmit(N);
                    DC.SubmitChanges();

                    M.ActiveFlag = false;
                    M.UpdDate = DT;
                    M.SaveACID = ACID;
                    DC.SubmitChanges();

                    SetAlert("已駁回入組申請", 1, "/Web/GroupPlace/New_List/" + OIID);

                }

            }
            return View(N);
        }
        #endregion

        #region 小組資訊-組員名單管理-待受洗名單

        public cTableList GetBaptized_List(FormCollection FC)
        {
            cTableList cTL = new cTableList();
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            cTL.Title = "小組名單";
            cTL.NowPage = iNowPage;
            cTL.NumCut = iNumCut;
            cTL.Rs = new List<cTableRow>();
            GetID();
            var TopTitles = new List<cTableCell>();

            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 200 });
            TopTitles.Add(new cTableCell { Title = "姓名", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "性別", WidthPX = 50 });
            TopTitles.Add(new cTableCell { Title = "生日", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "手機", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "預計受洗日期" });
            cTL.Rs.Add(SetTableRowTitle(TopTitles));
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID && q.OID == 8 && q.OIID == OIID);
            if (OI != null)
            {
                /*var Ns = from q in GetMOIAC(OI.OID, OI.OIID, 0).Where(q => q.JoinDate != q.CreDate && q.LeaveDate == q.CreDate && q.ActiveFlag)
                         join p in DC.Baptized.Where(q => !q.DeleteFlag && !q.ImplementFlag)
                         on new { q.OIID, q.ACID } equals new { p.OIID, p.ACID }
                         select q;*/
                var Ns = GetMOIAC(OI.OID, OI.OIID, 0).Where(q => q.JoinDate != q.CreDate && q.LeaveDate == q.CreDate && q.ActiveFlag && q.Account.BaptizedType == 0);
                cTL.TotalCt = Ns.Count();
                cTL.MaxNum = GetMaxNum(cTL.TotalCt, cTL.NumCut);
                Ns = Ns.OrderByDescending(q => q.ACID == OI.ACID).ThenByDescending(q => q.CreDate).Skip((iNowPage - 1) * cTL.NumCut).Take(cTL.NumCut);
                foreach (var N in Ns)
                {
                    cTableRow cTR = new cTableRow();
                    //操作
                    cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Web/GroupPlace/Baptized_Edit/" + N.MID, Target = "_self", Value = "設定受洗日期" });
                    //姓名
                    cTR.Cs.Add(new cTableCell { Value = N.Account.Name_First + N.Account.Name_Last });
                    //性別
                    cTR.Cs.Add(new cTableCell { Value = N.Account.ManFlag ? "男" : "女" });
                    //生日
                    cTR.Cs.Add(new cTableCell { Value = N.Account.Birthday == N.Account.CreDate ? "--" : N.Account.Birthday.ToString(DateFormat) });
                    //手機
                    var Cons = DC.Contect.Where(q => q.TargetType == 2 && q.ContectType == 1 && q.TargetID == N.ACID).OrderByDescending(q => q.CID);
                    cTR.Cs.Add(new cTableCell { Value = (Cons.Count() > 0 ? string.Join(",", Cons.Select(q => q.ContectValue)) : "--") });
                    //預計受洗日期
                    var B = DC.Baptized.FirstOrDefault(q => q.OIID == N.OIID && q.ACID == N.ACID && !q.ImplementFlag && !q.DeleteFlag && q.BaptismDate != q.CreDate);
                    cTR.Cs.Add(new cTableCell { Value = (B != null ? B.BaptismDate.ToString(DateFormat) : "--") });
                    cTL.Rs.Add(SetTableCellSortNo(cTR));
                }
            }
            return cTL;
        }
        [HttpGet]
        public ActionResult Baptized_List()
        {
            GetViewBag();
            return View(GetBaptized_List(null));
        }
        [HttpPost]
        public ActionResult Baptized_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetBaptized_List(FC));
        }
        #endregion
        #region 小組資訊-組員名單管理-待受洗名單-設定受洗日期
        [HttpGet]
        public ActionResult Baptized_Edit(int ID)
        {
            GetViewBag();
            DateTime DT_ = Convert.ToDateTime("2000/1/1");
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";
            GetID();
            Baptized N = new Baptized();
            var M = DC.M_OI_Account.FirstOrDefault(q => !q.DeleteFlag && q.MID == ID && q.OrganizeInfo.ACID == ACID && q.OIID == OIID);
            if (M == null)
                SetAlert("查無此小組成員資料", 2, "/Web/GroupPlace/Baptized_List/" + OIID);
            else
            {
                N = DC.Baptized.FirstOrDefault(q => q.ACID == M.ACID && q.OIID == M.OIID && !q.DeleteFlag && !q.ImplementFlag);
                if (N == null)
                {
                    N = new Baptized
                    {
                        Account = M.Account,
                        OIID = M.OIID,
                        BaptismDate = DT_,
                        ImplementFlag = false,
                        CreDate = DT_
                    };
                }
            }
            return View(N);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Baptized_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            DateTime DT_ = Convert.ToDateTime("2000/1/1");
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";
            GetID();
            Baptized N = new Baptized();
            var M = DC.M_OI_Account.FirstOrDefault(q => !q.DeleteFlag && q.MID == ID && q.OrganizeInfo.ACID == ACID && q.OIID == OIID);
            if (M != null)
            {
                N = DC.Baptized.FirstOrDefault(q => q.ACID == M.ACID && q.OIID == M.OIID && !q.DeleteFlag && !q.ImplementFlag);
                if (N == null)
                {
                    N = new Baptized
                    {
                        Account = M.Account,
                        OIID = M.OIID,
                        BaptismDate = DT_,
                        ImplementFlag = false
                    };
                }

                if (FC != null)
                {
                    DT_ = DT;
                    try
                    {
                        DT_ = Convert.ToDateTime(FC.Get("txb_BaptismDate"));
                        if (DT_.Date <= DT.Date)
                            Error += "預計受洗日請輸入含今天以後的日期";
                        else
                            N.BaptismDate = DT_;
                    }
                    catch { }
                }

                if (Error != "")
                    SetAlert(Error);
                else if (N.BID == 0)
                {
                    N.DeleteFlag = false;
                    N.CreDate = N.UpdDate = DT;
                    N.SaveACID = ACID;
                    DC.Baptized.InsertOnSubmit(N);
                    DC.SubmitChanges();
                    SetAlert("新增預計受洗日完成", 1, "/Web/GroupPlace/Baptized_List/" + OIID);
                }
                else
                {
                    N.UpdDate = DT;
                    N.SaveACID = ACID;
                    DC.SubmitChanges();
                    SetAlert("更新預計受洗日完成", 1, "/Web/GroupPlace/Baptized_List/" + OIID);
                }
            }
            return View(N);
        }
        #endregion

        #region 小組聚會登記
        [HttpGet]
        public ActionResult GroupMeet_QRCode(int ID)
        {
            GetViewBag();
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";
            GetID();
            if (ID > 0)
                SetBrowserData("OIID", ID.ToString());
            else
                ID = Convert.ToInt32(GetBrowserData("OIID"));

            string sQRCode_URL = Create_QRCode("/Web/AccountPlace/GroupMeet_Join/" + ID);
            TempData["QRCode_URL"] = sQRCode_URL;
            TempData["JoinGroup_URL"] = ("/Web/AccountPlace/GroupMeet_Join/" + ID);
            ViewBag._OIID = ID;

            var MLS = DC.Meeting_Location_Set.FirstOrDefault(q => q.SetType == 1 && q.OIID == ID && !q.DeleteFlag);
            if (MLS != null)
            {
                if (MLS.WeeklyNo > 0)
                {
                    int ixx = (int)DT.DayOfWeek;
                    if ((int)DT.DayOfWeek == MLS.WeeklyNo || ((int)DT.DayOfWeek == 0 && MLS.WeeklyNo == 7))
                    {
                        TempData["QRCode_Show"] = true;
                        string str = "小組聚會時間:" + sWeeks[MLS.WeeklyNo];
                        if (MLS.TimeNo > 0)
                            str += " " + sTimeSpans[MLS.TimeNo];
                        str += " " + MLS.S_hour.ToString().PadLeft(2, '0') + ":" + MLS.S_minute.ToString().PadLeft(2, '0') + " ~ ";
                        str += " " + MLS.E_hour.ToString().PadLeft(2, '0') + ":" + MLS.E_minute.ToString().PadLeft(2, '0');
                        TempData["QRCode_Msg"] = str;
                    }
                    else
                    {
                        TempData["QRCode_Show"] = false;
                        TempData["QRCode_Msg"] = "今日並非小組聚會時間...";
                    }

                }
                else
                {
                    TempData["QRCode_Show"] = false;
                    TempData["QRCode_Msg"] = "查無小組聚會時間...";
                }

            }
            else
            {
                TempData["QRCode_Show"] = false;
                TempData["QRCode_Msg"] = "查無小組聚會時間...";
            }

            return View();
        }
        #endregion
        #region 小組聚會名單維護
        public class cGroupMeet_List
        {
            public cTableList cTL = new cTableList();
            public string sDate = "";
            public string sName = "";

            public string[] GroupCt = new string[5] { "", "", "", "", "" };
        }
        public cGroupMeet_List GetGroupMeet_List(FormCollection FC)
        {
            cGroupMeet_List c = new cGroupMeet_List();
            GetID();
            #region 物件初始化


            string sSubmitType = "Filter";
            c.cTL = new cTableList();
            c.cTL.Title = "";
            c.cTL.NowPage = 1;
            c.cTL.NumCut = 0;
            c.cTL.Rs = new List<cTableRow>();
            #endregion
            #region 前端帶入
            if (FC != null)
            {
                sSubmitType = FC.Get("hid_SubmitType");
                c.sDate = FC.Get("txb_Date");
                c.sName = FC.Get("txb_Name");
            }
            #endregion
            #region 資料庫帶入

            var Ns = DC.Event_Join_Header.Where(q => q.EID == 2 && q.OIID == OIID);
            if (c.sDate != "")
            {
                DateTime dt_ = DT;
                if (DateTime.TryParse(c.sDate, out dt_))
                    Ns = Ns.Where(q => q.EventDate.Date == dt_.Date);
            }

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "聚會日期", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "會友名稱" });
            TopTitles.Add(new cTableCell { Title = "報到狀況" });
            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            c.cTL.MaxNum = c.cTL.TotalCt = Ns.Count();
            Ns = Ns.OrderByDescending(q => q.EventDate).Take(5);//只抓最近5次集會

            var MOIs = GetMOIAC(8, OIID, 0).OrderBy(q => q.ACID).ToList();
            int i = 0;
            int j = 0;
            foreach (var N in Ns)
            {
                int k = 0;
                foreach (var MOI in MOIs)
                {
                    bool bShow = true;
                    if (c.sName != "")
                        if (MOI.Account.Name_First + MOI.Account.Name_Last != c.sName)
                            bShow = false;

                    if (bShow)
                    {
                        cTableRow cTR = new cTableRow();
                        cTR.ID = i;
                        cTR.Cs.Add(new cTableCell { Value = N.EventDate.ToString(DateFormat) });//聚會日期
                        cTR.Cs.Add(new cTableCell { Value = MOI.Account.Name_First + MOI.Account.Name_Last });//會友名稱
                        var EJD = N.Event_Join_Detail.FirstOrDefault(q => q.ACID == MOI.ACID);
                        if (sSubmitType == "Save")
                        {
                            if (GetViewCheckBox(FC.Get("cbox_Join" + i)) && EJD == null)
                            {
                                EJD = new Event_Join_Detail
                                {
                                    Event_Join_Header = N,
                                    ACID = MOI.ACID,
                                    Name = MOI.Account.Name_First + MOI.Account.Name_Last,
                                    PhoneNo = "",
                                    DeleteFlag = false,
                                    JoinDate = DT,
                                    CreDate = DT
                                };
                                DC.Event_Join_Detail.InsertOnSubmit(EJD);
                                DC.SubmitChanges();
                            }
                        }

                        if (EJD != null)
                        {
                            if (EJD.JoinDate.Date == N.EventDate.Date)
                                cTR.Cs.Add(new cTableCell { Value = "已於" + EJD.JoinDate.ToString("HH:mm") + "報到" });//報到狀況
                            else
                                cTR.Cs.Add(new cTableCell { Value = "已於" + EJD.JoinDate.ToString("yyyy/MM/dd HH:mm") + "報到" });//報到狀況
                            k++;
                        }
                        else
                        {
                            cTR.Cs.Add(new cTableCell { Type = "checkbox", ControlName = "cbox_Join", Disabled = false, Title = "手動報到" });
                        }

                        c.cTL.Rs.Add(SetTableCellSortNo(cTR));
                        i++;
                    }
                }

                string sColor = "";
                if (MOIs.Count() == k)
                    sColor = "Green";
                else if (k == 0)
                    sColor = "Red";
                else
                    sColor = "Orange";
                c.GroupCt[j] = N.EventDate.ToString(DateFormat) + " 的集會 應到 " + MOIs.Count() + " 人，實到 <span style='color:" + sColor + "'>" + k + "</span> 人";
                j++;
            }
            if (sSubmitType == "Save")
                SetAlert("已存檔完成", 1);
            #endregion

            return c;
        }
        [HttpGet]
        public ActionResult GroupMeet_List()
        {
            GetViewBag();
            return View(GetGroupMeet_List(null));
        }
        [HttpPost]
        public ActionResult GroupMeet_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetGroupMeet_List(FC));
        }
        #endregion
    }
}