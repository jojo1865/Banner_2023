using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using static Banner.Areas.Admin.Controllers.OrganizeSetController;

namespace Banner.Areas.Admin.Controllers
{
    public class AccountSetController : PublicClass
    {
        // GET: Admin/AccountSet
        public ActionResult Index()
        {
            return View();
        }

        #region 牧養名單成人-列表
        public class cAccount_List : PublicClass
        {
            public cTableList cTL = new cTableList();
            public string sAddURL = "/Admin/AccountSet/Account_Aldult_Edit/0";
            public string Name = "";
            public string CellPhone = "";
            public ListSelect LS_Sex = new ListSelect();
            public ListSelect LS_Baptized = new ListSelect();
            public ListSelect LS_Group = new ListSelect();

            public cAccount_List()
            {
                if (LS_Sex == null)
                    LS_Sex = new ListSelect();
                LS_Sex.ControlName = "ddl_Sex";
                LS_Sex.Title = "性別";
                LS_Sex.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
                LS_Sex.ddlList.Add(new SelectListItem { Text = "男", Value = "1" });
                LS_Sex.ddlList.Add(new SelectListItem { Text = "女", Value = "2" });

                if (LS_Baptized == null)
                    LS_Baptized = new ListSelect();
                LS_Baptized.ControlName = "ddl_Baptized";
                LS_Baptized.Title = "受洗狀態";
                LS_Baptized.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
                LS_Baptized.ddlList.Add(new SelectListItem { Text = "已受洗", Value = "1" });
                LS_Baptized.ddlList.Add(new SelectListItem { Text = "未受洗", Value = "2" });

                if (LS_Group == null)
                    LS_Group = new ListSelect();
                LS_Group.ControlName = "ddl_Group";
                LS_Group.Title = "入組狀態";
                LS_Group.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
                for (int i = 0; i < JoinTitle.Length; i++)
                    LS_Group.ddlList.Add(new SelectListItem { Text = JoinTitle[i], Value = (i + 1).ToString(), Disabled = i == 2 });
            }

        }

        public cAccount_List sAccount_Aldult_List(FormCollection FC)
        {
            cAccount_List cAL = new cAccount_List();
            cAL.cTL = new cTableList();
            if (FC != null)
            {
                cAL.Name = FC.Get("txb_Name");
                cAL.CellPhone = FC.Get("txb_CellPhone");

                cAL.LS_Sex.ddlList.ForEach(q => q.Selected = false);
                cAL.LS_Sex.ddlList.First(q => q.Value == FC.Get("ddl_Sex")).Selected = true;

                if (FC.Get("ddl_Baptized") != null)
                {
                    cAL.LS_Baptized.ddlList.ForEach(q => q.Selected = false);
                    cAL.LS_Baptized.ddlList.First(q => q.Value == FC.Get("ddl_Baptized")).Selected = true;
                }

                if (FC.Get("ddl_Group") != null)
                {
                    cAL.LS_Group.ddlList.ForEach(q => q.Selected = false);
                    cAL.LS_Group.ddlList.First(q => q.Value == FC.Get("ddl_Group")).Selected = true;
                }

            }

            return cAL;
        }
        public cTableList GetAccountTable(int iType, FormCollection FC)
        {
            cTableList cTL = new cTableList();
            int iNumCut = GetQueryStringInInt("NumCut");
            int iNowPage = GetQueryStringInInt("PageNo");
            if (iNumCut <= 0)
            {
                if (GetCookie("NumCut") == "")
                {
                    iNumCut = 10;
                    SetCookie("NumCut", iNumCut.ToString());
                }
                else
                    iNumCut = Convert.ToInt32(GetCookie("NumCut"));
            }
            else
                SetCookie("NumCut", iNumCut.ToString());
            if (iNowPage <= 0) iNowPage = 1;
            cTL = new cTableList();
            cTL.Title = "";
            cTL.NowPage = iNowPage;
            cTL.ItemID = "";

            cTL.NumCut = iNumCut;
            cTL.Rs = new List<cTableRow>();

            var Ns = DC.Account.Where(q => !q.DeleteFlag);
            switch (iType)
            {
                case 1://成人
                    cTL.NowURL = "/Admin/AccountSet/Account_Aldult_List";
                    Ns = Ns.Where(q => DT.Year - q.Birthday.Year > 13);
                    break;

                case 2://兒童
                    cTL.NowURL = "/Admin/AccountSet/Account_Childen_List";
                    Ns = Ns.Where(q => DT.Year - q.Birthday.Year <= 13);
                    break;

                case 3://新人
                    cTL.NowURL = "/Admin/AccountSet/Account_New_List";
                    var M_OI_AGs = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag)
                                   group q by new { q.ACID } into g
                                   select new { g.Key.ACID, MaxID = g.Max(q => q.MID), Ct = g.Count() };
                    var M_OI_As = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag)
                                  join p in M_OI_AGs
                                  on q.MID equals p.MaxID
                                  select q;
                    var AIDs = (Ns.Select(q => q.ACID)).Except(M_OI_As.Where(q => q.OIID > 1 && q.JoinDate != q.CreDate).Select(q => q.ACID));
                    Ns = (from q in AIDs
                          join p in Ns
                         on q equals p.ACID
                          select p);

                    break;

                case 4://受洗
                    cTL.NowURL = "/Admin/AccountSet/Account_Baptized_List";
                    Ns = from q in DC.Baptized.Where(q => !q.ImplementFlag && !q.DeleteFlag).GroupBy(q => q.ACID).Select(q => q.Key)
                         join p in Ns
                         on q equals p.ACID
                         select p;

                    break;
            }

            if (FC != null)
            {
                if (!string.IsNullOrEmpty(FC.Get("txb_Name")))
                    Ns = Ns.Where(q => q.Name.Contains(FC.Get("txb_Name")));

                if (!string.IsNullOrEmpty(FC.Get("txb_CellPhone")))
                    Ns = from q in Ns
                         join p in DC.Contect.Where(q => q.TargetType == 2 && q.ContectType == 1 && q.ContectValue.Contains(FC.Get("txb_CellPhone"))).GroupBy(q => q.TargetID)
                         on q.ACID equals p.Key
                         select q;
                string sSix = FC.Get("ddl_Sex");
                if (FC.Get("ddl_Sex") != "0")
                    Ns = Ns.Where(q => q.ManFlag == (FC.Get("ddl_Sex") == "1"));

                switch (iType)
                {
                    case 1://成人
                        {
                            if (FC.Get("ddl_Baptized") != "0")
                                Ns = from q in Ns
                                     join p in DC.Baptized.Where(q => q.ImplementFlag == (FC.Get("ddl_Baptized") == "1") && !q.DeleteFlag).GroupBy(q => q.ACID).Select(q => q.Key)
                                     on q.ACID equals p
                                     select q;
                        }
                        break;

                    case 2://兒童
                        break;

                    case 3://新人
                        if (FC.Get("ddl_Group") != "0")
                        {
                            var M_OI_AGs = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag)
                                           group q by new { q.ACID } into g
                                           select new { g.Key.ACID, MaxID = g.Max(q => q.MID), Ct = g.Count() };
                            var M_OI_As = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag)
                                          join p in M_OI_AGs
                                          on q.MID equals p.MaxID
                                          select new { Ms = q, p.Ct };

                            switch (FC.Get("ddl_Group"))
                            {
                                case "1"://無意願
                                    {
                                        //不存在M_OI_As的名單中
                                        var AIDs = (Ns.Select(q => q.ACID)).Except(M_OI_As.Select(q => q.Ms.ACID));
                                        Ns = from q in Ns
                                             join p in AIDs
                                             on q.ACID equals p
                                             select q;
                                    }
                                    break;

                                case "2"://已入組未落戶
                                    {
                                        //存在M_OI_As的名單中
                                        Ns = from q in Ns
                                             join p in M_OI_As.Where(q => q.Ms.OIID > 1 && q.Ms.JoinDate == q.Ms.CreDate)
                                             on q.ACID equals p.Ms.ACID
                                             select q;
                                    }
                                    break;

                                case "4"://被退回(未分發)
                                    {
                                        Ns = from q in Ns
                                             join p in M_OI_As.Where(q => q.Ms.OIID == 1 && q.Ms.JoinDate == q.Ms.CreDate && q.Ct > 1)
                                             on q.ACID equals p.Ms.ACID
                                             select q;
                                    }
                                    break;

                                case "5"://跟進中(未分發)
                                    {
                                        Ns = from q in Ns
                                             join p in M_OI_As.Where(q => q.Ms.OIID == 1 && q.Ms.JoinDate == q.Ms.CreDate && q.Ct == 1)
                                             on q.ACID equals p.Ms.ACID
                                             select q;
                                    }
                                    break;
                            }
                        }
                        break;

                    case 4://受洗
                        break;
                }

                int iOID = Convert.ToInt32(FC.Get("ddl_O"));
                string OITitle = FC.Get("txb_OITitle");
                if (iOID > 0 || !string.IsNullOrEmpty(OITitle))
                {
                    var OIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag);
                    if (!string.IsNullOrEmpty(OITitle))
                        OIs = OIs.Where(q => q.Title.Contains(OITitle));
                    if (iOID > 0)
                        OIs = OIs.Where(q => q.OID == iOID);
                    var IDs = OIs.GroupBy(q => q.OIID).Select(q => q.Key);
                    var Ms = from q in GetMOIAC()
                             join p in IDs
                             on q.OIID equals p
                             select q;

                    Ns = from q in Ns
                         join p in Ms.GroupBy(q => q.ACID)
                         on q.ACID equals p.Key
                         select q;
                }
            }
            var TopTitles = new List<cTableCell>();
            switch (iType)
            {
                case 1://成人
                    TopTitles.Add(new cTableCell { Title = "選擇", WidthPX = 50 });
                    TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
                    TopTitles.Add(new cTableCell { Title = "牧區" });
                    TopTitles.Add(new cTableCell { Title = "督區" });
                    TopTitles.Add(new cTableCell { Title = "區" });
                    TopTitles.Add(new cTableCell { Title = "小組" });
                    TopTitles.Add(new cTableCell { Title = "姓名" });
                    TopTitles.Add(new cTableCell { Title = "性別", WidthPX = 50 });
                    TopTitles.Add(new cTableCell { Title = "受洗狀態" });
                    TopTitles.Add(new cTableCell { Title = "行動電話" });
                    break;

                case 2://兒童
                    break;

                case 3://新人
                    TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
                    TopTitles.Add(new cTableCell { Title = "姓名" });
                    TopTitles.Add(new cTableCell { Title = "性別", WidthPX = 50 });
                    TopTitles.Add(new cTableCell { Title = "生日" });
                    TopTitles.Add(new cTableCell { Title = "手機" });
                    TopTitles.Add(new cTableCell { Title = "入組狀態" });
                    TopTitles.Add(new cTableCell { Title = "備註" });
                    break;

                case 4://受洗
                    break;
            }


            cTL.Rs.Add(SetTableRowTitle(TopTitles));

            cTL.TotalCt = Ns.Count();
            cTL.MaxNum = GetMaxNum(cTL.TotalCt, cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.ACID).Skip((iNowPage - 1) * cTL.NumCut).Take(cTL.NumCut);
            foreach (var N in Ns)
            {
                //手機
                var Cons = DC.Contect.Where(q => q.TargetType == 2 && q.ContectType == 1 && q.TargetID == N.ACID).OrderByDescending(q => q.CID);
                string CellPhone = Cons.Count() > 0 ? string.Join(",", Cons.Select(q => q.ContectValue)) : "--";
                cTableRow cTR = new cTableRow();
                switch (iType)
                {
                    case 1://成人
                        {
                            cTR.Cs.Add(new cTableCell { Type = "checkbox", Value = "false", ControlName = "cbox_S" + N.ACID, CSS = "form-check-input cbox_S" });//選擇
                            cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_Aldult_Edit/" + N.ACID, Target = "_black", Value = "編輯" });//
                            var OI_8 = GetMOIAC(8, 0, N.ACID).FirstOrDefault();
                            if (OI_8 != null)
                            {
                                var OI_7 = DC.OrganizeInfo.FirstOrDefault(q => !q.DeleteFlag && q.OID == 7 && q.OIID == OI_8.OrganizeInfo.ParentID);
                                if (OI_7 != null)
                                {
                                    var OI_6 = DC.OrganizeInfo.FirstOrDefault(q => !q.DeleteFlag && q.OID == 6 && q.OIID == OI_7.ParentID);
                                    if (OI_6 != null)
                                    {
                                        var OI_5 = DC.OrganizeInfo.FirstOrDefault(q => !q.DeleteFlag && q.OID == 5 && q.OIID == OI_6.ParentID);
                                        if (OI_5 != null)
                                        {
                                            cTR.Cs.Add(new cTableCell { Value = OI_5.Title });//牧區
                                        }
                                        else
                                            cTR.Cs.Add(new cTableCell { Value = "--" });//牧區
                                        cTR.Cs.Add(new cTableCell { Value = OI_6.Title });//督區
                                    }
                                    else
                                    {
                                        cTR.Cs.Add(new cTableCell { Value = "--" });//牧區
                                        cTR.Cs.Add(new cTableCell { Value = "--" });//督區
                                    }
                                    cTR.Cs.Add(new cTableCell { Value = OI_7.Title });//區
                                }
                                else
                                {
                                    cTR.Cs.Add(new cTableCell { Value = "--" });//牧區
                                    cTR.Cs.Add(new cTableCell { Value = "--" });//督區
                                    cTR.Cs.Add(new cTableCell { Value = "--" });//區
                                }
                                cTR.Cs.Add(new cTableCell { Value = OI_8.OrganizeInfo.Title });//小組
                            }
                            else
                            {
                                cTR.Cs.Add(new cTableCell { Value = "" });//牧區
                                cTR.Cs.Add(new cTableCell { Value = "" });//督區
                                cTR.Cs.Add(new cTableCell { Value = "" });//區
                                cTR.Cs.Add(new cTableCell { Value = "" });//小組
                            }

                            cTR.Cs.Add(new cTableCell { Value = N.Name.ToString() });//姓名
                            cTR.Cs.Add(new cTableCell { Value = N.ManFlag ? "男" : "女" });//性別

                            var B = DC.Baptized.Where(q => q.ACID == N.ACID && !q.DeleteFlag).OrderByDescending(q => q.BID).FirstOrDefault();
                            if (B == null)
                                cTR.Cs.Add(new cTableCell { Value = "--" });//受洗狀態
                            else if (!B.ImplementFlag)
                                cTR.Cs.Add(new cTableCell { Value = "預計於" + B.BaptismDate.ToShortDateString() + "受洗" });//受洗狀態
                            else
                                cTR.Cs.Add(new cTableCell { Value = "已於" + B.BaptismDate.ToShortDateString() + "受洗" });//受洗狀態


                            cTR.Cs.Add(new cTableCell { Value = CellPhone });//行動電話
                        }
                        break;

                    case 2://兒童
                        break;

                    case 3://新人
                        {
                            var Ms = DC.M_OI_Account.Where(q => q.ACID == N.ACID);
                            int iJoinType = 0;//"無意願", "已入組未落戶", "跟進中(已分發)", "被退回(未分發)","跟進中(未分發)"
                            string Note = "";
                            if (Ms.Any())
                            {
                                var M = Ms.OrderByDescending(q => q.MID).First();
                                if (M.OIID > 1)
                                {
                                    if (M.JoinDate == M.CreDate)
                                    {
                                        iJoinType = 1;//已入組未落戶
                                        Note = "已於"+M.CreDate.ToShortDateString() + "分派至 (" + M.OIID + ")"+M.OrganizeInfo.Title;
                                    }
                                }
                                else
                                {
                                    if (Ms.Count() > 1)
                                    {
                                        iJoinType = 3;//被退回

                                        var A_Notes = DC.Account_Note.Where(q => q.ACID == N.ACID && !q.DeleteFlag && q.NoteType == 1 && q.OIID == M.OIID).OrderByDescending(q=>q.CreDate);
                                        foreach (var A_N in A_Notes)
                                            Note += (Note != "" ? "</br>" : "") + A_N.CreDate.ToString(DateTimeFormat) + ":" + A_N.Note;
                                    }
                                    else
                                        iJoinType = 4;//跟進中(未分發)
                                }
                            }
                            if (iJoinType == 3 || iJoinType == 4)
                                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_New_Edit/" + N.ACID, Target = "_black", Value = "分發" });//操作
                            else
                                cTR.Cs.Add(new cTableCell { Value = "" });//操作
                            cTR.Cs.Add(new cTableCell { Value = N.Name });//姓名
                            cTR.Cs.Add(new cTableCell { Value = N.ManFlag ? "男" : "女" });//性別
                            cTR.Cs.Add(new cTableCell { Value = N.Birthday != N.CreDate ? N.Birthday.ToShortDateString() : "" });//生日
                            cTR.Cs.Add(new cTableCell { Value = CellPhone });//手機
                            cTR.Cs.Add(new cTableCell { Value = JoinTitle[iJoinType] });//入組狀態
                            cTR.Cs.Add(new cTableCell { Value = Note });//備註

                        }

                        break;

                    case 4://受洗
                        break;
                }
                cTL.Rs.Add(SetTableCellSortNo(cTR));

            }

            return cTL;
        }
        public ActionResult Account_Aldult_List()
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(null);
            cAL.cTL = GetAccountTable(1, null);

            return View(cAL);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Account_Aldult_List(FormCollection FC)
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(FC);
            cAL.cTL = GetAccountTable(1, FC);

            return View(cAL);
        }
        #endregion
        #region 牧養名單新人-列表
        [HttpGet]
        public ActionResult Account_New_List()
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(null);
            cAL.cTL = GetAccountTable(3, null);

            return View(cAL);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Account_New_List(FormCollection FC)
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(FC);
            cAL.cTL = GetAccountTable(3, FC);

            return View(cAL);
        }
        #endregion
        #region 牧養名單新人-編輯
        public class cAccount_New_Edit
        {
            public int ACID = 0;
            public string Name = "";
            public string PhonoNo = "";
            public ListSelect OIddl = new ListSelect();
            public string InputOIData = "";
            public string InputControlName = "txb_OI";
        }
        public cAccount_New_Edit GetAccount_New_Edit(int ID, FormCollection FC)
        {
            cAccount_New_Edit N = new cAccount_New_Edit();
            var AC = DC.Account.FirstOrDefault(q => q.ACID == ID && !q.DeleteFlag);
            if (AC == null)
                SetAlert("查無此會友,請重新操作", 2, "/Admin/AccountSet/Account_New_List/0");
            else
            {
                N.ACID = AC.ACID;
                N.Name = AC.Name;
                var Con = DC.Contect.Where(q => q.TargetID == AC.ACID && q.TargetType == 2 && q.ContectType == 1 && q.ContectValue != "").OrderByDescending(q => q.CID).FirstOrDefault();
                if (Con != null)
                    N.PhonoNo = Con.ContectValue;
                else
                    N.PhonoNo = "--";
                N.OIddl = new ListSelect() { Title = "1.", SortNo = 0, ControlName = "ddl_OI", ddlList = new List<SelectListItem>() };
                //會友可接受的聚會時間
                //實體
                var JGWs_1 = DC.JoinGroupWish.Where(q => q.ACID == AC.ACID && q.WeeklyNo > 0 && q.TimeNo > 0 && q.JoinType == 1);
                //線上
                var JGWs_2 = DC.JoinGroupWish.Where(q => q.ACID == AC.ACID && q.WeeklyNo > 0 && q.TimeNo > 0 && q.JoinType == 2);
                if (JGWs_1.Count() > 0)
                {
                    //小組聚會點
                    //實體
                    var MLs_1 = from q in DC.M_Location_Set.Where(q => q.SetType == 1 && q.ActiveFlag && !q.DeleteFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag)
                                join p in DC.Location.Where(q => q.TargetType == 3 && q.ZipCode.GroupName != "網路")
                                on q.MLID equals p.TargetID
                                select q;
                    //目前符合的小組名單
                    //實體
                    var MLSs_1 = from q in MLs_1
                                 join p in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 8)
                                 on q.TargetID equals p.OIID
                                 select new { q.MID, q.Meeting_Location.Title, q.WeeklyNo, q.TimeNo, OITitle = p.Title, p.OIID };
                    var OIs = from q in JGWs_1
                              join p in MLSs_1
                              on new { q.WeeklyNo, q.TimeNo } equals new { p.WeeklyNo, p.TimeNo }
                              select p;

                    foreach (var OI in OIs.OrderByDescending(q => q.OIID))
                        N.OIddl.ddlList.Add(new SelectListItem { Text = "[實體]" + OI.OITitle, Value = OI.OIID.ToString() });
                }
                else if (JGWs_2.Count() > 0)
                {
                    //小組聚會點
                    //線上
                    var MLs_2 = from q in DC.M_Location_Set.Where(q => q.SetType == 1 && q.ActiveFlag && !q.DeleteFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag)
                                join p in DC.Location.Where(q => q.TargetType == 3 && q.ZipCode.GroupName == "網路")
                                on q.MLID equals p.TargetID
                                select q;

                    //目前符合的小組名單
                    //線上
                    var MLSs_2 = from q in MLs_2
                                 join p in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 8)
                                 on q.TargetID equals p.OIID
                                 select new { q.MID, q.Meeting_Location.Title, q.WeeklyNo, q.TimeNo, OITitle = p.Title, p.OIID };

                    var OIs = from q in JGWs_2
                              join p in MLSs_2
                              on new { q.WeeklyNo, q.TimeNo } equals new { p.WeeklyNo, p.TimeNo }
                              select p;

                    foreach (var OI in OIs.OrderByDescending(q => q.OIID))
                        N.OIddl.ddlList.Add(new SelectListItem { Text = "[線上]" + OI.OITitle, Value = OI.OIID.ToString() });
                }

                if (N.OIddl.ddlList.Count > 0)
                {
                    N.OIddl.ddlList = (List<SelectListItem>)N.OIddl.ddlList.OrderByDescending(q => q.Value);//比較新的小組放前面
                    if (FC != null)
                    {
                        N.OIddl.ddlList.ForEach(q => q.Selected = false);
                        N.OIddl.ddlList.Find(q => q.Value == FC.Get(N.OIddl.ControlName)).Selected = true;
                    }
                    else
                        N.OIddl.ddlList[0].Selected = true;
                }
                else
                    N.OIddl.ddlList.Add(new SelectListItem { Text = "無推薦名單", Value = "0", Selected = true });

                if (FC != null)
                {
                    N.InputOIData = FC.Get(N.InputControlName);
                    string sddlID = FC.Get(N.OIddl.ControlName);
                    if ((sddlID == "0" && N.InputOIData=="") || (sddlID != "0" && N.InputOIData != ""))
                        SetAlert("請從建議選單選擇小組 或 輸入關鍵字 以指定小組", 2);
                    else if (sddlID != "0")
                    {
                        var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 8 && q.OIID.ToString() == sddlID);
                        if (OI != null)
                        {
                            M_OI_Account M = new M_OI_Account
                            {
                                OIID = OI.OIID,
                                ACID = AC.ACID,
                                JoinDate = DT,
                                LeaveDate = DT,
                                ActiveFlag = true,
                                DeleteFlag = false,
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = GetACID()
                            };
                            DC.M_OI_Account.InsertOnSubmit(M);
                            DC.SubmitChanges();

                            SetAlert("分發完成", 1, "/Admin/AccountSet/Account_New_List/0");
                        }
                        else
                            SetAlert("查不到要被分發的小組,請重新指定", 2);
                    }
                    else if (N.InputOIData.Contains(')'))
                    {
                        string[] str = N.InputOIData.Split(')');
                        if (str.Length == 2)
                        {
                            var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 8 && q.OIID.ToString() == str[0].Replace("(", ""));
                            if (OI != null)
                            {
                                M_OI_Account M = new M_OI_Account
                                {
                                    OIID = OI.OIID,
                                    ACID = AC.ACID,
                                    JoinDate = DT,
                                    LeaveDate = DT,
                                    ActiveFlag = true,
                                    DeleteFlag = false,
                                    CreDate = DT,
                                    UpdDate = DT,
                                    SaveACID = GetACID()
                                };
                                DC.M_OI_Account.InsertOnSubmit(M);
                                DC.SubmitChanges();

                                SetAlert("分發完成", 1, "/Admin/AccountSet/Account_New_List/0");
                            }
                            else
                                SetAlert("查不到要被分發的小組,請重新指定", 2);
                        }
                        else
                            SetAlert("請輸入關鍵字後從下拉選單選擇分發的小組後重新送出", 2);
                    }
                    else
                        SetAlert("請輸入關鍵字後從下拉選單選擇分發的小組後重新送出", 2);
                }
            }

            return N;
        }
        [HttpGet]
        public ActionResult Account_New_Edit(int ID)
        {
            GetViewBag();
            return View(GetAccount_New_Edit(ID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Account_New_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            return View(GetAccount_New_Edit(ID, FC));
        }
        #endregion

        #region 牧養名單成人-新增/修改/刪除
        public class cAccount_Aldult_Edit
        {
            public Account AC = new Account();
            public List<SelectListItem> EducationTypes = ddl_EducationTypes;//教育程度
            public List<SelectListItem> JobTypes = ddl_JobTypes;//職業
            //受洗狀態
            public List<SelectListItem> BaptizedTypes = new List<SelectListItem> {
                new SelectListItem{ Text="旌旗受洗",Value="1",Selected=true},
                new SelectListItem{ Text="非旌旗受洗",Value="2"}
            };
            public List<SelectListItem> MLs = new List<SelectListItem>();//主日聚會點
            public List<ListInput> Coms = new List<ListInput>();//社群帳號
            public List<cContect> Cons = new List<cContect>();//通訊資料
            public Location L = new Location(); //通信地址

            public int JoinGroupType = 0;//入組意願調查選擇
            public List<cJoinGroupWish> cJGWs = new List<cJoinGroupWish>();//加入小組有意願選項
            public string GroupNo = "";//想加入小組的編號

        }
        public class cContect
        {
            public string Title = "";
            public string ControlName1 = "";
            public string ControlName2 = "";
            public bool RequiredFlag = false;
            public int SortNo = 0;
            public Contect C = new Contect();
            public List<SelectListItem> Zips = new List<SelectListItem>();
        }
        public class cJoinGroupWish
        {
            public bool SelectFalg = false;
            public int JoinType = 0;
            public int SortNo = 0;
            public string GroupTitle = "";
            public JoinGroupWish JGW = new JoinGroupWish();
            public ListSelect ddl_Weekly = new ListSelect();
            public ListSelect ddl_Time = new ListSelect();
        }
        public cAccount_Aldult_Edit GerAccountData(int ID, FormCollection FC)
        {
            var N = new cAccount_Aldult_Edit();
            #region 物件初始化

            //主日聚會點初始化
            N.MLs.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            N.MLs.AddRange((from q in DC.M_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag && q.SetType == 0)
                            select new SelectListItem { Text = q.Meeting_Location.Title, Value = q.MLID.ToString() }).ToList());
            //社群帳號初始化
            N.Coms = new List<ListInput>();
            for (int i = 0; i < CommunityTitle.Length; i++)
                N.Coms.Add(new ListInput { Title = CommunityTitle[i], SortNo = i, ControlName = "txb_Com_" + i, InputData = "" });
            //聯絡方式初始化
            var SLI = (from q in DC.ZipCode.Where(q => q.GroupName == "國" && q.ActiveFlag && q.Title != "線上").OrderBy(q => q.ParentID).ThenBy(q => q.Code)
                       select new SelectListItem { Text = q.Title + q.Code, Value = q.ZID.ToString(), Selected = q.ZID == 10 }).ToList();
            N.Cons = new List<cContect>
                {
                    new cContect{Title="手機號碼",ControlName1="ddl_Zip0",ControlName2 = "txb_Value0",RequiredFlag=true,SortNo=0,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 1, CID = 0, ContectValue = "",ZID=10 },Zips =SLI},//手機1
                    new cContect{Title="手機號碼2",ControlName1="ddl_Zip1",ControlName2 = "txb_Value1",SortNo=1,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 1, CID = 0, ContectValue = "",ZID=10 },Zips =SLI},//手機2
                    new cContect{Title="電話(市話)",ControlName1="ddl_Zip2",ControlName2 = "txb_Value2",SortNo=2,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 0, CID = 0, ContectValue = "",ZID=10 },Zips =SLI},//市話
                    new cContect{Title="Email",ControlName2 = "txb_Value3",RequiredFlag=true,SortNo=3,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 2, CID = 0, ContectValue = "",ZID=1 },Zips = null},//Email1
                    new cContect{Title="Email2",ControlName2 = "txb_Value4",SortNo=4,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 2, CID = 0, ContectValue = "",ZID=1 },Zips = null}//Email2
                };
            //加入小組有意願選項初始化
            N.cJGWs = new List<cJoinGroupWish>();
            for (int i = 0; i < 2; i++)
            {
                for (int j = 1; j < 4; j++)
                {
                    cJoinGroupWish cJ = new cJoinGroupWish();
                    cJ.JGW = new JoinGroupWish
                    {
                        ACID = ID,
                        JoinType = (i + 1),
                        SortNo = j,
                        WeeklyNo = 0,
                        TimeNo = 0
                    };
                    cJ.GroupTitle = i == 0 ? "實體" : "線上";
                    cJ.JoinType = (i + 1);
                    cJ.SortNo = j;
                    cJ.ddl_Weekly = new ListSelect
                    {
                        Title = "星期",
                        SortNo = j,
                        ControlName = "ddl_Join" + i + "_WeeklyNo_" + j,
                        ddlList = new List<SelectListItem>()
                    };
                    cJ.ddl_Time = new ListSelect
                    {
                        Title = "時段",
                        SortNo = j,
                        ControlName = "ddl_Join" + i + "_TimeNo_" + j,
                        ddlList = new List<SelectListItem>()
                    };
                    cJ.ddl_Weekly.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
                    cJ.ddl_Time.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
                    for (int k = 0; k < sWeeks.Length; k++)
                        cJ.ddl_Weekly.ddlList.Add(new SelectListItem { Text = sWeeks[k], Value = (k + 1).ToString() });
                    for (int k = 0; k < sTimeSpans.Length; k++)
                        cJ.ddl_Time.ddlList.Add(new SelectListItem { Text = sTimeSpans[k], Value = (k + 1).ToString() });
                    N.cJGWs.Add(cJ);
                }
            }

            #endregion
            #region 會員資料填入

            N.AC = DC.Account.FirstOrDefault(q => q.ACID == ID && !q.DeleteFlag);
            if (N.AC == null && ID > 0)
                Error += "此帳號已被移除";
            else if (ID == 0)
            {
                N.AC = new Account
                {
                    Birthday = DT,
                    ManFlag = true,
                    MarriageType = 0,
                };
                N.L = new Location();
            }
            else
            {
                #region 資料初始化
                //教育程度
                N.EducationTypes.ForEach(q => q.Selected = false);
                N.EducationTypes.First(q => q.Value == N.AC.EducationType.ToString()).Selected = true;
                //職務
                N.JobTypes.ForEach(q => q.Selected = false);
                N.JobTypes.First(q => q.Value == N.AC.JobType.ToString()).Selected = true;
                //受洗狀況
                if (N.AC.BaptizedType > 0)
                {
                    N.BaptizedTypes[0].Selected = N.AC.BaptizedType == 1;
                    N.BaptizedTypes[1].Selected = N.AC.BaptizedType == 2;
                }
                //主日聚會點
                var ML = (from q in DC.M_ML_Account.Where(q => !q.DeleteFlag && q.ACID == N.AC.ACID)
                          join p in DC.M_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.SetType == 0)
                          on q.MLID equals p.MLID
                          select q).FirstOrDefault();
                if (ML != null)
                {
                    if (N.MLs.FirstOrDefault(q => q.Value == ML.MLID.ToString()) != null)
                    {
                        N.MLs.ForEach(q => q.Selected = false);
                        N.MLs.First(q => q.Value == ML.MLID.ToString()).Selected = true;
                    }
                }
                //社群狀況
                var CMs = N.AC.Community.ToList();
                for (int i = 0; i < CommunityTitle.Length; i++)
                {
                    var CM = CMs.FirstOrDefault(q => q.CommunityType == i);
                    if (CM != null)
                        N.Coms[i].InputData = CM.CommunityValue;
                }

                //地址
                var L = DC.Location.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == ID);
                if (L != null)
                    N.L = L;
                else
                    N.L = new Location();

                #region 聯絡方式
                var Cons = DC.Contect.Where(q => q.TargetType == 2 && q.TargetID == ID).OrderBy(q => q.ContectType).ToList();

                if (Cons.Count() > 0)
                {
                    int iSort = 0;
                    foreach (var Con in Cons.Where(q => q.ContectType == 1))
                    {
                        var Con_ = N.Cons.FirstOrDefault(q => q.SortNo == iSort);
                        if (Con_ != null)
                        {
                            Con_.C = Con;
                            if (Con.ZID != 10 && Con.ZID != 1)
                            {
                                Con_.Zips.ForEach(q => q.Selected = false);
                                Con_.Zips.First(q => q.Value == Con.ZID.ToString()).Selected = true;
                            }
                        }

                        iSort++;
                        if (iSort >= 2) break;
                    }
                    foreach (var Con in Cons.Where(q => q.ContectType == 0))
                    {
                        var Con_ = N.Cons.FirstOrDefault(q => q.SortNo == iSort);
                        if (Con_ != null)
                        {
                            Con_.C = Con;
                            if (Con.ZID != 10 && Con.ZID != 1)
                            {
                                Con_.Zips.ForEach(q => q.Selected = false);
                                Con_.Zips.First(q => q.Value == Con.ZID.ToString()).Selected = true;
                            }
                        }

                        iSort++;
                        if (iSort >= 3) break;
                    }
                    foreach (var Con in Cons.Where(q => q.ContectType == 2))
                    {
                        var Con_ = N.Cons.FirstOrDefault(q => q.SortNo == iSort);
                        if (Con_ != null)
                        {
                            Con_.C = Con;
                            /*if (Con.ZID != 10 && Con.ZID != 1)
                            {
                                Con_.Zips.ForEach(q => q.Selected = false);
                                Con_.Zips.First(q => q.Value == Con.ZID.ToString()).Selected = true;
                            }*/
                        }
                        iSort++;
                        if (iSort >= 5) break;
                    }
                }
                #endregion
                #region 入組意願調查
                var MOIACs = GetMOIAC(0, 0, ID);
                if (MOIACs.Count() == 0)
                    N.JoinGroupType = 0;
                else
                {
                    var Js = DC.JoinGroupWish.Where(q => q.ACID == ID).ToList();
                    var MOIAC = MOIACs.OrderByDescending(q => q.MID).First();
                    if (MOIAC.OIID == 1)
                    {
                        N.JoinGroupType = 1;
                        foreach (var cJGW in N.cJGWs)
                        {
                            var JGW = Js.FirstOrDefault(q => q.JoinType == cJGW.JoinType && q.SortNo == cJGW.SortNo);
                            cJGW.JGW = JGW;
                            if (JGW != null)
                            {
                                if (!cJGW.SelectFalg)
                                    cJGW.SelectFalg = cJGW.JGW.WeeklyNo > 0 && cJGW.JGW.TimeNo > 0;
                                cJGW.ddl_Weekly.ddlList.ForEach(q => q.Selected = false);
                                cJGW.ddl_Weekly.ddlList.First(q => q.Value == cJGW.JGW.WeeklyNo.ToString()).Selected = true;
                                cJGW.ddl_Time.ddlList.ForEach(q => q.Selected = false);
                                cJGW.ddl_Time.ddlList.First(q => q.Value == cJGW.JGW.TimeNo.ToString()).Selected = true;
                            }
                        }
                    }
                    else
                    {
                        N.JoinGroupType = 2;
                        N.GroupNo = MOIAC.OrganizeInfo.OIID.ToString();
                    }

                }
                #endregion 
                #endregion
            }
            #endregion
            #region 介面資料填入
            if (FC != null)
            {

            }
            #endregion


            return N;
        }


        public ActionResult Account_Aldult_Edit(int ID)
        {
            GetViewBag();
            return View(GerAccountData(ID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Account_Aldult_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            var N = GerAccountData(ID, FC);
            if (Error != "")
                SetAlert(Error, 2);
            else if (FC.AllKeys.FirstOrDefault(q => q == "txb_Login") != null)//修改密碼
            {
                var AC = DC.Account.FirstOrDefault(q => q.ACID == ID);

            }
            else
            {

            }
            return View(N);
        }
        #endregion
    }
}