using Banner.Models;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace Banner.Areas.Web.Controllers
{
    public class AccountPlaceController : PublicClass
    {
        #region 會員中心-首頁
        public class cAccountPlace_Index
        {
            public string Name = "";
            public string Title_OI = "";
            public string Title_Staff = "";
            public string Title_Teacher = "";
            public List<cMenu> c_MyClass = new List<cMenu>();
            public bool bFriendFlag = false;
            public GroupData MyGroup = new GroupData();
            public List<GroupData> MyGroups = new List<GroupData>();
            public List<cMenu> c_Teacher = new List<cMenu>();

            public List<SelectListItem> SL_Spiritual = new List<SelectListItem>();
            public string sChangeOI = "";//申請更換小組進度
            public bool bShowChange = false;
        }
        public class GroupData
        {

            public string Title;
            public string Date;
            public string Time;
            public string Address;
        }
        public cAccountPlace_Index GetIndex(FormCollection FC)
        {
            cAccountPlace_Index N = new cAccountPlace_Index();
            ACID = GetACID();
            var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag);
            if (AC != null)
            {
                N.Name = AC.Name_First + AC.Name_Last;

                #region 區塊1=參與職分或團體
                //按立
                var MOA = DC.M_O_Account.Where(q => q.ACID == ACID &&
                                            q.ActiveFlag &&
                                            !q.DeleteFlag &&
                                            q.Organize.ActiveFlag &&
                                            !q.Organize.DeleteFlag &&
                                            q.Organize.JobTitle != ""
                                            ).OrderBy(q => q.OID).FirstOrDefault();
                if (MOA != null) { N.Title_OI = MOA.Organize.JobTitle; }
                //事工團
                var MSAs = DC.M_Staff_Account.Where(q => q.ACID == ACID &&
                                                q.ActiveFlag &&
                                                !q.DeleteFlag &&
                                                q.Staff.ActiveFlag &&
                                                !q.Staff.DeleteFlag
                                                ).OrderBy(q => q.JoinDate);
                if (MSAs.Count() > 0)
                    N.Title_Staff = string.Join("/", MSAs.Select(q => q.Staff.Title));
                //講師
                var MPTs = from q in DC.Teacher.Where(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag)
                           join p in DC.M_Product_Teacher.Where(q => q.Product.ActiveFlag && !q.Product.DeleteFlag && q.Product_Class.ActiveFlag && !q.Product_Class.DeleteFlag)
                           on q.TID equals p.TID
                           select p;
                if (AC.TeacherFlag && MPTs.Count() > 0)
                    N.Title_Teacher = string.Join("/", MPTs.Select(q => q.Product.Course.Title));
                #endregion
                #region 區塊2=上課中
                var MPCs = DC.Order_Product.Where(q => q.Order_Header.Order_Type == 2 && q.Order_Header.ACID == ACID && !q.Order_Header.DeleteFlag && !q.Product.DeleteFlag && q.Product_Class.GraduateDate.Date > DT.Date && !q.Graduation_Flag).OrderBy(q => q.Order_Header.CreDate);
                if (MPCs.Count() > 0)
                {
                    int i = 0;
                    foreach (var MPC in MPCs)
                    {
                        cMenu cM = new cMenu
                        {
                            MenuID = MPC.PID,
                            Title = MPC.Product.Title + " " + MPC.Product.SubTitle,
                            Url = "/Web/MyClass/MyClass_Info/" + MPC.PID,
                            SortNo = i,
                            SelectFlag = false,
                            Items = null
                        };
                        N.c_MyClass.Add(cM);
                        i++;
                    }
                }
                #endregion
                #region 我的小組
                var MOI8 = GetMOIAC(8, 0, ACID).FirstOrDefault();
                if (MOI8 != null)
                {
                    N.MyGroup = new GroupData();
                    List<string> sList = GetOITitles(MOI8.OIID);
                    sList = (sList.Take(5)).ToList();
                    sList.Reverse();
                    N.MyGroup.Title = string.Join("-", sList) + "(#" + MOI8.OIID + ")";
                    var Meet = DC.Meeting_Location_Set.FirstOrDefault(q => q.OIID == MOI8.OIID && q.SetType == 1 && q.ActiveFlag && !q.DeleteFlag);
                    if (Meet != null)
                    {
                        N.MyGroup.Date = sWeeks[Meet.WeeklyNo];
                        N.MyGroup.Time =sTimeSpans[ Meet.TimeNo] + " " + Meet.S_hour.ToString().PadLeft(2, '0') + ":" + Meet.S_minute.ToString().PadLeft(2, '0') + "~" + Meet.E_hour.ToString().PadLeft(2, '0') + ":" + Meet.E_minute.ToString().PadLeft(2, '0');
                        N.MyGroup.Address = Meet.Meeting_Location.Title;
                        var Loc = DC.Location.FirstOrDefault(q => q.TargetType == 3 && q.TargetID == Meet.MLSID);
                        if (Loc != null)
                        {
                            N.MyGroup.Address += " " + GetZipData(Loc.ZID) + Loc.Address;
                        }
                    }
                }
                #endregion
                #region 小組長園地
                var OI_Leaders = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID && q.OID == 8);
                foreach (var OI in OI_Leaders)
                {
                    GroupData G = new GroupData();
                    List<string> sList = GetOITitles(OI.OIID);
                    sList = (sList.Take(5)).ToList();
                    sList.Reverse();
                    G.Title = string.Join("-", sList) + "(#" + OI.OIID + ")";
                    var Meet = DC.Meeting_Location_Set.FirstOrDefault(q => q.OIID == OI.OIID && q.SetType == 1 && q.ActiveFlag && !q.DeleteFlag);
                    if (Meet != null)
                    {
                        G.Date = sWeeks[Meet.WeeklyNo];
                        G.Time = sTimeSpans[Meet.TimeNo] + " " + Meet.S_hour.ToString().PadLeft(2, '0') + ":" + Meet.S_minute.ToString().PadLeft(2, '0') + "~" + Meet.E_hour.ToString().PadLeft(2, '0') + ":" + Meet.E_minute.ToString().PadLeft(2, '0');
                        G.Address = Meet.Meeting_Location.Title;
                        var Loc = DC.Location.FirstOrDefault(q => q.TargetType == 3 && q.TargetID == Meet.MLSID);
                        if (Loc != null)
                        {
                            G.Address += " " + GetZipData(Loc.ZID) + Loc.Address;
                        }
                    }
                    N.MyGroups.Add(G);
                }
                #endregion
                #region 講師專區
                if (AC.TeacherFlag)
                {
                    int i = 0;
                    foreach (var MPT in MPTs.OrderBy(q => q.PID).ThenBy(q => q.Product_Class.Product_ClassTime.Min(p => p.ClassDate)))
                    {
                        cMenu cM = new cMenu
                        {
                            MenuID = MPT.PCID,
                            Title = MPT.Product.Title + " " + MPT.Product.SubTitle + " " + MPT.Product_Class.Title,
                            Url = "/Web/Teacher/Student_List/" + MPT.PCID,
                            SortNo = i,
                            SelectFlag = false,
                            Items = null
                        };
                        N.c_Teacher.Add(cM);
                        i++;
                    }
                }
                #endregion
                #region 屬靈健檢表
                /*屬靈=是否有參加主日聚會的紀錄
                 *1:抓主日聚會點的聚會星期幾(假設為每周日)
                 *2:由今天以前算10次主日聚會日 
                 *3.若今天是3/5 周二,則顯示列表為3/2 2/24 2/17....
                 *4:多一個送出存檔的按鈕
                 *5:當周靈修(QT)次數 為純數字,送出後不能改
                 *2024/3/28 改為無視主日聚會,就是固定每周日
                */
                //查資料前先更新
                if (FC != null)
                {
                    int iCheckCt = 0;
                    for (int i = 0; i < 10; i++)
                    {
                        if (!string.IsNullOrEmpty(FC.Get("hid_" + i)))
                        {
                            DateTime DT_ = Convert.ToDateTime(FC.Get("hid_" + i));
                            bool bCheck = GetViewCheckBox(FC.Get("cbox_" + i));
                            string sValue = FC.Get("txb_" + i);
                            int iCt = 0;
                            if (bCheck && int.TryParse(sValue, out iCt))//有打勾 且 有數字
                            {
                                Account_Spiritual AS = new Account_Spiritual();
                                AS.Account = AC;
                                AS.QTCt = iCt;
                                AS.QTDate = DT_;
                                AS.CreDate = DT;
                                DC.Account_Spiritual.InsertOnSubmit(AS);
                                DC.SubmitChanges();

                                iCheckCt++;
                            }
                        }
                    }
                    if (iCheckCt > 0)
                        SetAlert("有勾選主日的屬靈健檢表已記錄", 1);
                    else
                        SetAlert("屬靈健檢表紀錄失敗", 2);
                }


                /*DateTime MaxDate = Convert.ToDateTime("2020/1/1");//最後一次主日應該是哪天
                var ASs = DC.Account_Spiritual.Where(q => q.ACID == ACID);
                //DateTime OldDate = AS.Count() > 0 ? AS.Max(q => q.QTDate) : MaxDate;//有紀錄的屬靈表最後是哪天
                
                var MLS = (from q in DC.M_ML_Account.Where(q => q.ACID == ACID && !q.DeleteFlag)
                           join p in DC.Meeting_Location_Set.Where(q => q.SetType == 0 && q.ActiveFlag && !q.DeleteFlag)
                           on q.MLID equals p.MLID
                           select p).FirstOrDefault();
                if (MLS != null)
                {
                    DateTime DT_Max = DT;
                    for (int i = 0; i < 7; i++)//找到距離最近的主日聚會日
                    {
                        int iDay = Convert.ToInt32(DT.AddDays(i * -1).DayOfWeek);
                        if (iDay == MLS.WeeklyNo)
                        {
                            DT_Max = DT.AddDays(i * -1).Date;
                            break;
                        }
                    }
                    if (DT_Max != DT)
                    {
                        for (int i = 0; i < 10; i++)
                        {
                            DateTime DT_ = DT_Max.AddDays(i * -7).Date;
                            var AS = ASs.FirstOrDefault(q => q.QTDate == DT_);

                            SelectListItem SL = new SelectListItem();
                            SL.Text = DT_.ToString(DateFormat);
                            if (AS != null)
                            {
                                SL.Value = AS.QTCt.ToString();
                                SL.Selected = true;
                            }
                            else
                            {
                                SL.Value = "";
                                SL.Selected = false;
                            }

                            N.SL_Spiritual.Add(SL);
                        }
                    }
                }*/
                DateTime MaxDate = DT.AddDays(-1* ((int)DT.DayOfWeek));//最近一個星期天
                var ASs = DC.Account_Spiritual.Where(q => q.ACID == ACID).ToList();
                for (int i = 0; i < 10; i++)
                {
                    DateTime DT_ = MaxDate.AddDays(i * -7).Date;
                    var AS = ASs.FirstOrDefault(q => q.QTDate == DT_);

                    SelectListItem SL = new SelectListItem();
                    SL.Text = DT_.ToString(DateFormat);
                    if (AS != null)
                    {
                        SL.Value = AS.QTCt.ToString();
                        SL.Selected = true;
                    }
                    else
                    {
                        SL.Value = "";
                        SL.Selected = false;
                    }

                    N.SL_Spiritual.Add(SL);
                }
                #endregion
                //會友卡
                N.bFriendFlag = DC.M_Role_Account.Any(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID && q.RID == 2);
                //申請新小組進度
                N.bShowChange = false;
                /*N.bShowChange = AC.GroupType != "無意願";
                var COI = DC.Change_OI_Order.Where(q => q.ACID == AC.ACID && !q.DeleteFlag).OrderByDescending(q => q.CreDate).FirstOrDefault();
                if (COI != null)
                {
                    var OI_From = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == COI.From_OIID);
                    var OI_To = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == COI.To_OIID);
                    N.sChangeOI = "已於" + COI.CreDate.ToString(DateTimeFormat) + "提出" + (COI.From_OIID == 1 ? "" : "由" + OI_From.Title + OI_From.Organize.Title) + "轉換至" + OI_To.Title + OI_To.Organize.Title + "，目前狀態為：";
                    if (COI.Order_Type == 0)
                        N.sChangeOI += "審核中";
                    else if (COI.Order_Type == 1)
                        N.sChangeOI += "審核通過，等待落戶";
                    else if (COI.Order_Type == 2)
                        N.sChangeOI += "審核通過，已加入小組";
                    else
                        N.sChangeOI += "已駁回";

                    N.bShowChange = true;
                }*/
            }
            else
                SetAlert("遺失登入訊息,請重新登入", 4, "/Web/Home/Index");
            return N;
        }

        [HttpGet]
        public ActionResult Index()
        {
            GetViewBag();
            return View(GetIndex(null));
        }
        [HttpPost]
        public ActionResult Index(FormCollection FC)
        {
            GetViewBag();
            return View(GetIndex(FC));
        }
        #endregion
        #region 會員中心-基本資料
        public class cAccount_Basic
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
        public cAccount_Basic GerAccountData(int ID, FormCollection FC)
        {
            var N = new cAccount_Basic();
            ViewBag._CSS1 = "~/Areas/Web/Content/css/form.css";
            #region 物件初始化

            //主日聚會點初始化
            //N.MLs.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            N.MLs.AddRange((from q in DC.Meeting_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag && q.SetType == 0)
                            select new SelectListItem { Text = q.Meeting_Location.Title, Value = q.MLID.ToString() }).ToList());
            N.MLs[0].Selected = true;

            //社群帳號初始化
            N.Coms = new List<ListInput>();
            for (int i = 0; i < CommunityTitle.Length; i++)
                N.Coms.Add(new ListInput { Title = CommunityTitle[i], SortNo = i, ControlName = "txb_Com_" + i, InputData = "" });
            //聯絡方式初始化
            var SLI = (from q in DC.ZipCode.Where(q => q.GroupName == "國" && q.ActiveFlag && q.Title != "線上").OrderBy(q => q.ParentID).ThenBy(q => q.Code)
                       select new SelectListItem { Text = q.Title + q.Code, Value = q.ZID.ToString(), Selected = q.ZID == 10 }).ToList();
            string sSLI = JsonConvert.SerializeObject(SLI);

            //List<SelectListItem>_SLI = JsonConvert.DeserializeObject<List<SelectListItem>>(sSLI);
            N.Cons = new List<cContect>
                {
                    new cContect{Title="手機號碼",ControlName1="ddl_Zip_Con0",ControlName2 = "txb_Value_Con0",RequiredFlag=true,SortNo=0,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 1, CID = 0, ContectValue = "",ZID=10 },Zips = (JsonConvert.DeserializeObject<List<SelectListItem>>(sSLI))},//手機1
                    new cContect{Title="手機號碼2",ControlName1="ddl_Zip_Con1",ControlName2 = "txb_Value_Con1",SortNo=1,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 1, CID = 0, ContectValue = "",ZID=10 },Zips = (JsonConvert.DeserializeObject<List<SelectListItem>>(sSLI))},//手機2
                    new cContect{Title="電話(市話)",ControlName1="ddl_Zip_Con2",ControlName2 = "txb_Value_Con2",SortNo=2,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 0, CID = 0, ContectValue = "",ZID=10 },Zips =(JsonConvert.DeserializeObject<List<SelectListItem>>(sSLI))},//市話
                    new cContect{Title="Email",ControlName2 = "txb_Value_Con3",RequiredFlag=true,SortNo=3,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 2, CID = 0, ContectValue = "",ZID=1 },Zips = null},//Email1
                    new cContect{Title="Email2",ControlName2 = "txb_Value_Con4",SortNo=4,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 2, CID = 0, ContectValue = "",ZID=1 },Zips = null}//Email2
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
                    cJ.ddl_Time.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
                    for (int k = 1; k < sWeeks.Length; k++)
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
                N.GroupNo = "";

            }
            else
            {
                #region 基本資料

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
                if (FC == null)
                {
                    var ML = (from q in DC.M_ML_Account.Where(q => !q.DeleteFlag && q.ACID == N.AC.ACID)
                              join p in DC.Meeting_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.SetType == 0)
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
                }
                //社群狀況
                if (FC == null)
                {
                    var CMs = N.AC.Community.ToList();
                    for (int i = 0; i < CommunityTitle.Length; i++)
                    {
                        var CM = CMs.FirstOrDefault(q => q.CommunityType == i);
                        if (CM != null)
                            N.Coms[i].InputData = CM.CommunityValue;
                    }
                }
                //地址
                var L = DC.Location.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == ID);
                if (L != null)
                    N.L = L;
                else
                    N.L = new Location();
                #endregion
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
                            if (Con.ZID != 10 && Con.ZID > 1)
                            {
                                Con_.Zips.ForEach(q => q.Selected = false);
                                Con_.Zips.First(q => q.Value == Con.ZID.ToString()).Selected = true;
                            }
                        }

                        iSort++;
                        if (iSort >= 2) break;
                    }
                    iSort = 2;
                    foreach (var Con in Cons.Where(q => q.ContectType == 0))
                    {
                        var Con_ = N.Cons.FirstOrDefault(q => q.SortNo == iSort);
                        if (Con_ != null)
                        {
                            Con_.C = Con;
                            if (Con.ZID != 10 && Con.ZID > 1)
                            {
                                Con_.Zips.ForEach(q => q.Selected = false);
                                Con_.Zips.First(q => q.Value == Con.ZID.ToString()).Selected = true;
                            }
                        }

                        iSort++;
                        if (iSort >= 3) break;
                    }
                    iSort = 3;
                    foreach (var Con in Cons.Where(q => q.ContectType == 2))
                    {
                        var Con_ = N.Cons.FirstOrDefault(q => q.SortNo == iSort);
                        if (Con_ != null)
                        {
                            Con_.C = Con;
                            if (Con.ZID != 10 && Con.ZID > 1)
                            {
                                Con_.Zips.ForEach(q => q.Selected = false);
                                Con_.Zips.First(q => q.Value == Con.ZID.ToString()).Selected = true;
                            }
                        }
                        iSort++;
                        if (iSort >= 5) break;
                    }
                }
                #endregion
                #region 入組意願調查
                N.JoinGroupType = N.AC.GroupType == "有意願" ? 1 : (N.AC.GroupType == "無意願" ? 0 : 2);
                switch (N.AC.GroupType)
                {
                    case "有意願":
                        {
                            N.JoinGroupType = 1;
                            var Js = DC.JoinGroupWish.Where(q => q.ACID == ID).ToList();
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
                        break;

                    case "無意願":
                        {
                            N.JoinGroupType = 0;
                        }
                        break;

                    default:
                        {
                            N.GroupNo = N.AC.GroupType;
                            N.JoinGroupType = 2;
                        }
                        break;
                }



                /*var MOIACs = GetMOIAC(0, 0, ID);
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

                }*/
                #endregion
                #region 介面資料填入
                if (FC != null)
                {
                    N.AC.Name_First = FC.Get("txb_Name_First");
                    N.AC.Name_Last = FC.Get("txb_Name_Last");
                    /*if (ID == 0)
                    {
                        N.AC.Login = FC.Get("txb_Login");
                        if (!CheckPasswork(FC.Get("txb_New1")))
                            Error += "密碼必須為包含大小寫英文與數字的8碼以上字串</br>";
                        else if (FC.Get("txb_New2") != FC.Get("txb_New1"))
                            Error += "新密碼與重複輸入的不同</br>";
                        else
                            N.AC.Password = HSM.Enc_1(FC.Get("txb_New1"));
                    }*/

                    N.AC.ManFlag = GetViewCheckBox(FC.Get("cbox_Sex"));
                    N.AC.IDNumber = FC.Get("txb_IDNumber");
                    N.AC.IDType = CheckSSN(N.AC.IDNumber) ? 0 : 1;
                    DateTime BD_ = DT;
                    try
                    {
                        BD_ = Convert.ToDateTime(FC.Get("txb_Birthday"));
                        N.AC.Birthday = BD_;
                        if (BD_.Date > DT.Date)
                            Error += "生日填寫錯誤</br>";
                    }
                    catch { BD_ = DT; }
                    N.AC.EducationType = Convert.ToInt32(FC.Get("ddl_EducationTypes"));
                    N.AC.JobType = Convert.ToInt32(FC.Get("ddl_JobTypes"));
                    N.AC.MarriageType = Convert.ToInt32(FC.Get("rbl_MarriageType"));
                    if (N.AC.MarriageType == 2)
                        N.AC.MarriageNote = FC.Get("txb_MarriageNote");
                    else
                        N.AC.MarriageNote = "";
                    //受洗狀態
                    if (FC.Get("rbl_BaptizedType") == "1")
                        N.AC.BaptizedType = Convert.ToInt32(FC.Get("ddl_BaptizedType"));
                    else
                        N.AC.BaptizedType = 0;
                    //主日聚會點
                    N.MLs.ForEach(q => q.Selected = false);
                    N.MLs.First(q => q.Value == FC.Get("ddl_ML")).Selected = true;
                    //社群資料
                    foreach (var CM in N.Coms)
                    {
                        CM.InputData = FC.Get(CM.ControlName);
                    }
                    //通訊地址
                    N.L.LID = Convert.ToInt32(FC.Get("hid_LID"));
                    //手機電話
                    foreach (var Con in N.Cons)
                    {
                        Con.C.ContectValue = FC.Get(Con.ControlName2);
                        if (Con.ControlName1 != "")
                        {
                            Con.C.ZID = Convert.ToInt32(FC.Get(Con.ControlName1));
                            Con.Zips.ForEach(q => q.Selected = false);
                            Con.Zips.First(q => q.Value == Con.C.ZID.ToString()).Selected = true;
                        }
                    }

                    //入組意願
                    N.JoinGroupType = Convert.ToInt32(FC.Get("rbut_GroupFlag"));
                    for (int i = 0; i < 2; i++)
                    {
                        foreach (var _cJGW in N.cJGWs.Where(q => q.JoinType == (i + 1)).OrderBy(q => q.SortNo))
                        {
                            _cJGW.SelectFalg = GetViewCheckBox(FC.Get("cbox_JoinGroupWish"));
                            if (!string.IsNullOrEmpty(FC.Get(_cJGW.ddl_Weekly.ControlName)))
                            {
                                _cJGW.ddl_Weekly.ddlList.ForEach(q => q.Selected = false);
                                _cJGW.ddl_Weekly.ddlList.First(q => q.Value == FC.Get(_cJGW.ddl_Weekly.ControlName)).Selected = true;
                            }
                            if (!string.IsNullOrEmpty(FC.Get(_cJGW.ddl_Time.ControlName)))
                            {
                                _cJGW.ddl_Time.ddlList.ForEach(q => q.Selected = false);
                                _cJGW.ddl_Time.ddlList.First(q => q.Value == FC.Get(_cJGW.ddl_Time.ControlName)).Selected = true;
                            }

                        }
                    }
                    if (N.JoinGroupType == 2)
                    {
                        string sGT = FC.Get("txb_GroupNo");
                        if (sGT != null)
                        {
                            var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID.ToString() == sGT && !q.DeleteFlag);
                            if (OI != null)
                            {
                                var MOI = DC.M_OI_Account.FirstOrDefault(q => q.OIID == OI.OIID && q.ACID == ACID);
                                if (MOI != null)
                                    Error += "此小組您曾經加入過或仍在小組內,不能重複申請</br>";
                                else
                                    N.AC.GroupType = N.GroupNo = sGT;
                            }
                            else
                                Error += "您期望加入的小組編號輸入錯誤</br>";
                        }
                    }
                    else
                    {
                        N.AC.GroupType = N.JoinGroupType == 0 ? "無意願" : "有意願";
                    }
                }
                #endregion
            }
            #endregion
            return N;
        }
        [HttpGet]
        public ActionResult BasicData()
        {
            GetViewBag();
            return View(GerAccountData(ACID, null));
        }
        [HttpPost]

        public ActionResult BasicData(FormCollection FC)
        {
            GetViewBag();
            var N = GerAccountData(ACID, FC);


            //會員資料更新
            N.AC.UpdDate = DT;
            N.AC.SaveACID = ACID;
            DC.SubmitChanges();

            //主日聚會點
            if (N.MLs.FirstOrDefault(q => q.Selected) != null)
            {
                int MLID = Convert.ToInt32(N.MLs.First(q => q.Selected).Value);
                var ML = DC.M_ML_Account.FirstOrDefault(q => q.MLID == MLID && q.ACID == ACID);
                if (ML != null)
                {
                    ML.MLID = MLID;
                    ML.DeleteFlag = false;
                    ML.UpdDate = DT;
                    ML.SaveACID = N.AC.SaveACID;
                    DC.SubmitChanges();
                }
                else
                {
                    ML = new M_ML_Account();
                    ML.MLID = MLID;
                    ML.ACID = N.AC.ACID;
                    ML.DeleteFlag = false;
                    ML.UpdDate = ML.CreDate = DT;
                    ML.SaveACID = N.AC.SaveACID;
                    DC.M_ML_Account.InsertOnSubmit(ML);
                    DC.SubmitChanges();
                }
            }
            else
            {
                var MLs = from q in DC.M_ML_Account.Where(q => q.ACID == ACID && !q.DeleteFlag)
                          join p in DC.Meeting_Location_Set.Where(q => q.SetType == 0)
                          on q.MLID equals p.MLID
                          select q;
                foreach (var ML in MLs)
                {
                    ML.DeleteFlag = true;
                    ML.UpdDate = DT;
                    ML.SaveACID = ACID;
                    DC.SubmitChanges();
                }
            }
            //社群
            foreach (var CM in N.Coms)
            {
                if (!string.IsNullOrEmpty(CM.InputData))
                {
                    Community Com = new Community();
                    Com.ACID = N.AC.ACID;
                    Com.CommunityType = CM.SortNo;
                    Com.CommunityValue = CM.InputData;
                    DC.Community.InsertOnSubmit(Com);
                    DC.SubmitChanges();
                }
            }
            //入組意願調查
            var Js = DC.JoinGroupWish.Where(q => q.ACID == N.AC.ACID).ToList();
            foreach (var cJGW in N.cJGWs)
            {
                var WNo = cJGW.ddl_Weekly.ddlList.FirstOrDefault(q => q.Selected);
                var TNo = cJGW.ddl_Time.ddlList.FirstOrDefault(q => q.Selected);
                var JGW = Js.FirstOrDefault(q => q.JoinType == cJGW.JoinType && q.SortNo == cJGW.SortNo);
                if (WNo != null && TNo != null)
                {
                    if (JGW != null)
                    {
                        JGW.WeeklyNo = Convert.ToInt32(WNo.Value);
                        JGW.TimeNo = Convert.ToInt32(TNo.Value);
                    }
                    else
                    {
                        JGW = new JoinGroupWish
                        {
                            ACID = N.AC.ACID,
                            JoinType = cJGW.JoinType,
                            SortNo = cJGW.SortNo,
                            WeeklyNo = Convert.ToInt32(WNo.Value),
                            TimeNo = Convert.ToInt32(TNo.Value)
                        };
                        DC.JoinGroupWish.InsertOnSubmit(JGW);
                    }
                    DC.SubmitChanges();
                }
            }
            //所在小組資料檢查
            //2023.10.19改為若有填寫現有小組編號就直接派到該小組~但尚未落戶
            if (N.AC.GroupType != "無意願" && N.AC.GroupType != "有意願")
            {
                var MOI = DC.M_OI_Account.FirstOrDefault(q => q.ACID == N.AC.ACID && q.OIID.ToString() == N.AC.GroupType);
                if (MOI == null)
                {
                    var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID.ToString() == N.AC.GroupType && !q.DeleteFlag);
                    if (OI != null)
                    {
                        MOI = new M_OI_Account
                        {
                            OrganizeInfo = OI,
                            Account = N.AC,
                            JoinDate = DT,
                            LeaveDate = DT,
                            ActiveFlag = true,
                            DeleteFlag = false,
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = N.AC.ACID
                        };
                        DC.M_OI_Account.InsertOnSubmit(MOI);
                        DC.SubmitChanges();
                    }
                }
            }
            SetAlert("已完成儲存", 1);
            return View(N);
        }

        //更換密碼
        [HttpPut]
        public string ChangePW(int ACID, string Login, string PW)
        {
            Error = "OK";
            var AC_ = DC.Account.FirstOrDefault(q => q.ACID == ACID && q.Login == Login && !q.DeleteFlag);
            if (AC_ == null)
                Error = "帳號輸入錯誤";
            else if (!CheckPasswork(PW))
                Error = "密碼必須為包含大小寫英文與數字的8碼以上字串";
            else
            {

            }
            return Error;
        }
        #endregion
        #region 家庭狀況
        public class cFamilyData
        {
            public List<Family> Fs = new List<Family>();
            public Account AC = new Account();
            public bool ChildFlag = false;
            public bool ChangeWeddingDataFlag = false;
            public List<Contect> Cons = new List<Contect>();
            public List<Account> AC_F3s = new List<Account>();
        }
        public cFamilyData GetFamilyData(FormCollection FC)
        {
            cFamilyData N = new cFamilyData();
            #region 會員資料填入
            var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID && !q.DeleteFlag);
            if (AC != null)
            {
                N.AC = AC;
                N.Fs = AC.Family.Where(q => !q.DeleteFlag).ToList();
                N.ChildFlag = DT.Year - N.AC.Birthday.Year > iChildAge && N.AC.Birthday != N.AC.CreDate;

                if (N.Fs.FirstOrDefault(q => q.FamilyType == 2) == null)//沒有配偶~
                {
                    N.Fs.Add(new Family
                    {
                        ACID = N.AC.ACID,
                        Name = "",
                        IDNumber = "",
                        Login = "",
                        FamilyType = 2,
                        FamilyTitle = "配偶",
                        TargetACID = 0,
                        SortNo = 0,
                        DeleteFlag = false
                    });
                }

                N.AC_F3s = (from q in N.Fs.Where(q => q.FamilyType == 3)
                            join p in DC.Account.Where(q => !q.DeleteFlag)
                            on q.TargetACID equals p.ACID
                            select p).ToList();

                N.Cons = (from q in DC.Contect.Where(q => q.ContectType == 1 && q.TargetType == 4).ToList()
                          join p in N.Fs
                          on q.TargetID equals p.FID
                          select q).ToList();
            }

            #endregion
            #region 輸入資料填入
            if (FC != null)
            {
                string F0_Name = FC.Get("txb_Family_Name_0");//父
                string F1_Name = FC.Get("txb_Family_Name_1");//母
                #region 父親
                if (!string.IsNullOrEmpty(F0_Name))
                {
                    var F0 = N.Fs.FirstOrDefault(q => q.FamilyType == 0);
                    if (F0 != null)
                    {
                        F0.Name = F0_Name;
                    }
                    else
                    {
                        F0 = new Family
                        {
                            ACID = ACID,
                            Name = F0_Name,
                            IDNumber = "",
                            Login = "",
                            FamilyType = 0,
                            FamilyTitle = "父親",
                            TargetACID = 0,
                            SortNo = 0,
                            DeleteFlag = false
                        };
                        N.Fs.Add(F0);
                    }
                }
                #endregion
                #region 母親
                if (!string.IsNullOrEmpty(F1_Name))
                {
                    var F1 = N.Fs.FirstOrDefault(q => q.FamilyType == 1);
                    if (F1 != null)
                    {
                        F1.Name = F1_Name;
                    }
                    else
                    {
                        F1 = new Family
                        {
                            ACID = ACID,
                            Name = F1_Name,
                            IDNumber = "",
                            Login = "",
                            FamilyType = 1,
                            FamilyTitle = "母親",
                            TargetACID = 0,
                            SortNo = 0,
                            DeleteFlag = false
                        };
                        N.Fs.Add(F1);
                    }
                }
                #endregion
                #region 配偶
                //有配對到配偶~就不處理了
                //反之~若沒ACID表示目前沒配對到
                string F2_Name = FC.Get("txb_Family_Name_2");//配偶姓名
                if (!string.IsNullOrEmpty(F2_Name))
                {
                    string F2_IDNumber = FC.Get("txb_Family_IDNumber_2");//配偶身分證字號
                    string F2_Login = FC.Get("txb_Family_Login_2");//帳號

                    var F2 = N.Fs.FirstOrDefault(q => q.FamilyType == 2);
                    if (F2 != null)
                    {
                        N.ChangeWeddingDataFlag = F2.Name != F2_Name || F2.IDNumber != F2_IDNumber || F2.Login != F2_Login;
                        F2.Name = F2_Name;
                        F2.IDNumber = F2_IDNumber;
                        F2.Login = F2_Login;
                    }
                    else
                    {
                        N.ChangeWeddingDataFlag = true;
                        F2 = new Family
                        {
                            ACID = ACID,
                            Name = F2_Name,
                            IDNumber = F2_IDNumber,
                            Login = F2_Login,
                            FamilyType = 2,
                            FamilyTitle = "配偶",
                            TargetACID = 0,
                            SortNo = -1,
                            DeleteFlag = false
                        };
                        N.Fs.Add(F2);
                    }
                }
                #endregion
                #region 緊急連絡人
                for (int i = 0; i < 2; i++)
                {
                    string F4_Name = FC.Get("txb_Family_Name_4_" + i);
                    string F4_Title = FC.Get("txb_Family_Title_4_" + i);

                    string sPhone = FC.Get("txb_PhoneNo");
                    string sZID = FC.Get("ddl_PhoneZip");
                    string F4_PhoneNo = FC.Get("txb_PhoneNo").Split(',')[i];
                    int F4_Zip = Convert.ToInt32(FC.Get("ddl_PhoneZip").Split(',')[i]);

                    var F4 = N.Fs.FirstOrDefault(q => q.FamilyType == 4 && q.SortNo == i);
                    if (F4 != null)
                    {
                        F4.Name = F4_Name;
                        F4.FamilyTitle = F4_Title;

                        Contect Con = N.Cons.FirstOrDefault(q => q.TargetID == F4.FID);
                        if (Con != null)
                        {
                            Con.ZID = F4_Zip;
                            Con.ContectValue = F4_PhoneNo;
                        }
                        else
                        {
                            Con = new Contect
                            {
                                TargetType = 4,
                                TargetID = F4.FID,
                                ZID = F4_Zip,
                                ContectType = 1,
                                ContectValue = F4_PhoneNo,
                                CheckFlag = false,
                                CreDate = DT,
                                CheckDate = DT
                            };
                            N.Cons.Add(Con);
                        }

                    }
                    else
                    {
                        F4 = new Family
                        {
                            ACID = ACID,
                            Name = F4_Name,
                            IDNumber = "",
                            Login = F4_PhoneNo,
                            FamilyType = 4,
                            FamilyTitle = F4_Title,
                            TargetACID = F4_Zip,
                            SortNo = i,
                            DeleteFlag = false
                        };
                        N.Fs.Add(F4);

                        Contect Con = N.Cons.FirstOrDefault(q => q.TargetID == F4.FID && q.ContectType == i);
                        if (Con != null)
                        {
                            Con.ZID = F4_Zip;
                            Con.ContectValue = F4_PhoneNo;
                        }
                        else
                        {
                            Con = new Contect
                            {
                                TargetType = 4,
                                TargetID = 0,
                                ZID = F4_Zip,
                                ContectType = i,
                                ContectValue = F4_PhoneNo,
                                CheckFlag = false,
                                CreDate = DT,
                                CheckDate = DT
                            };
                            N.Cons.Add(Con);
                        }
                    }
                }
                #endregion
            }
            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult FamilyData()
        {
            GetViewBag();
            return View(GetFamilyData(null));
        }
        [HttpPost]

        public ActionResult FamilyData(FormCollection FC)
        {
            GetViewBag();
            var N = GetFamilyData(FC);
            foreach (var F in N.Fs)
            {
                switch (F.FamilyType)
                {
                    case 0://父
                    case 1://母
                        {
                            if (F.FID == 0)
                                DC.Family.InsertOnSubmit(F);
                        }
                        break;

                    case 2://配偶
                        {
                            if (F.FID == 0)
                                DC.Family.InsertOnSubmit(F);
                            if (N.ChangeWeddingDataFlag)
                            {
                                var AC_ = DC.Account.FirstOrDefault(q => !q.DeleteFlag &&
                                q.ActiveFlag &&
                                (q.Name_First + q.Name_Last) == F.Name &&
                                q.IDNumber == F.IDNumber &&
                                q.Login == F.Login
                                );

                                if (AC_ != null)
                                {
                                    string sMailData = "";
                                    sMailData += "<p>" + AC_.Name_First + "您好：</p>";
                                    sMailData += "<p>有位" + N.AC.Name_First + N.AC.Name_Last + (N.AC.ManFlag ? "先生" : "女士") + "想跟您確認配偶關係</p>";
                                    sMailData += "<p>若兩位確實為配偶,請按下";
                                    sMailData += "<a href='https://" + Request.Url.Host + "/Web/Home/CheckWedding?ID1=" + HSM.Enc_1(N.AC.ACID.ToString()) + "&ID2=" + HSM.Enc_1(AC_.ACID.ToString()) + "' target='_black'>確定</a>";
                                    sMailData += "以確認兩位互為配偶</p>";

                                    var Con = DC.Contect.FirstOrDefault(q => q.TargetID == AC_.ACID && q.TargetType == 2 && q.ContectType == 2 && q.CheckFlag);
                                    if (Con != null)
                                        Error = SendMail(Con.ContectValue, AC_.Name_First + AC_.Name_Last, "【全球旌旗資訊網】配偶認證", sMailData);
                                }
                            }
                        }
                        break;

                    case 4://緊急連絡人
                        {
                            if (F.FID == 0)
                            {
                                DC.Family.InsertOnSubmit(F);
                            }
                        }
                        break;
                }
            }
            foreach (var Con in N.Cons)
            {
                if (Con.CID == 0)
                    DC.Contect.InsertOnSubmit(Con);
            }
            DC.SubmitChanges();

            foreach (var F4 in N.Fs.Where(q => q.FamilyType == 4).OrderBy(q => q.SortNo))
            {
                var Con = N.Cons.FirstOrDefault(q => q.TargetID == 0 && q.TargetType == 4 && q.ContectValue == F4.Login && q.ZID == F4.TargetACID && q.ContectType == F4.SortNo);
                if (Con != null)
                {
                    Con.TargetID = F4.FID;
                    Con.ContectType = 1;
                    F4.Login = "";
                    F4.TargetACID = 0;
                    DC.SubmitChanges();
                }
            }
            SetAlert("存檔完成", 1);
            return View(N);
        }

        #endregion
        #region 退款費用專用帳戶
        public class cBankData
        {
            public Account_Bank AB = new Account_Bank();
            public List<SelectListItem> SLI = new List<SelectListItem>();
        }
        public cBankData GetBankData(FormCollection FC)
        {
            cBankData N = new cBankData();
            var Bs = DC.Bank.Where(q => q.ActiveFlag).OrderBy(q => q.BankNo);
            #region 物件初始化
            foreach (var B in Bs)
                N.SLI.Add(new SelectListItem { Text = B.BankNo.ToString().PadLeft(3, '0') + " " + B.Title, Value = B.BID.ToString() });
            N.SLI[0].Selected = true;
            #endregion
            #region 會員資料填入
            N.AB = DC.Account_Bank.FirstOrDefault(q => q.ACID == ACID && !q.DeleteFlag);
            if (N.AB == null)
            {
                N.AB = new Account_Bank
                {
                    ACID = ACID,
                    BID = Bs.Min(q => q.BID),
                    Title = "",
                    BankNo = "",
                    AccountNo = "",
                    DeleteFlag = false,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID
                };
            }
            else
            {
                N.SLI.ForEach(q => q.Selected = false);
                N.SLI.Find(q => q.Value == N.AB.BID.ToString()).Selected = true;
            }
            #endregion
            #region 輸入資料填入
            if (FC != null)
            {
                N.AB.Title = FC.Get("txb_Title");
                N.AB.BankNo = FC.Get("txb_BankNo");
                N.AB.AccountNo = FC.Get("txb_AccountNo");
                N.AB.BID = Convert.ToInt32(FC.Get("ddl_BID"));
                N.SLI.ForEach(q => q.Selected = false);
                N.SLI.Find(q => q.Value == N.AB.BID.ToString()).Selected = true;
            }
            #endregion
            return N;
        }

        [HttpGet]
        public ActionResult BankData()
        {
            GetViewBag();
            return View(GetBankData(null));
        }
        [HttpPost]

        public ActionResult BankData(FormCollection FC)
        {
            GetViewBag();
            var N = GetBankData(FC);
            if (N.AB.ABID == 0)
                DC.Account_Bank.InsertOnSubmit(N.AB);
            DC.SubmitChanges();
            SetAlert("已存檔完成", 1);
            return View(N);
        }
        #endregion
        #region 多元表現
        public class cPerformanceData
        {
            public List<Account_Performance> APs = new List<Account_Performance>();
        }
        public cPerformanceData GetPerformanceData(FormCollection FC)
        {
            cPerformanceData N = new cPerformanceData();

            #region 會員資料填入
            N.APs = DC.Account_Performance.ToList();

            #endregion
            #region 輸入資料填入
            if (FC != null)
            {
                for (int i = 0; i < 2; i++)
                {
                    int iSortNo_Max = 0;
                    foreach (var AP in N.APs.Where(q => q.PerformanceType == i).OrderBy(q => q.APID))
                    {
                        string sData = FC.Get("txb_Performance_DB_" + i + "_" + AP.APID);
                        if (!string.IsNullOrEmpty(sData))
                            AP.Performance = sData;
                        else
                            AP.Performance = "";
                        iSortNo_Max = iSortNo_Max < AP.SortNo ? AP.SortNo : iSortNo_Max;
                    }
                    int iMax = Convert.ToInt32(FC.Get("text_max_" + i));
                    for (int j = 0; j <= iMax; j++)
                    {
                        string sData = FC.Get("txb_Performance_" + i + "_" + j);
                        if (!string.IsNullOrEmpty(sData))
                        {
                            N.APs.Add(new Account_Performance
                            {
                                APID = 0,
                                ACID = ACID,
                                PerformanceType = i,
                                Performance = sData,
                                SortNo = iSortNo_Max + 1
                            });
                            iSortNo_Max++;
                        }
                    }
                }

            }
            #endregion
            return N;
        }

        [HttpGet]
        public ActionResult PerformanceData()
        {
            GetViewBag();
            return View(GetPerformanceData(null));
        }
        [HttpPost]

        //[HandleError(ExceptionType = typeof(HttpAntiForgeryException), View = "/Web/Home/CSRF")]
        public ActionResult PerformanceData(FormCollection FC)
        {
            GetViewBag();
            var N = GetPerformanceData(FC);
            foreach (var AP in N.APs.Where(q => q.APID == 0))
            {
                DC.Account_Performance.InsertOnSubmit(AP);
            }
            foreach (var AP in N.APs.Where(q => q.Performance == "" && q.APID > 0))
            {
                DC.Account_Performance.DeleteOnSubmit(AP);
            }
            DC.SubmitChanges();
            SetAlert("已存檔完成", 1, "/Web/AccountPlace/PerformanceData");
            return View(N);
        }
        #endregion
        #region 小組聚會報到
        [HttpGet]
        public ActionResult GroupMeet_Join(int ID)
        {
            GetViewBag();
            ACID = GetACID();
            var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID && !q.DeleteFlag && q.ActiveFlag);
            var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == ID);
            var MOIs = GetMOIAC(8, ID, ACID);
            if (AC == null)
                SetAlert("請先登入", 2, "/Web/Home/Login");
            else if (OI == null)
                SetAlert("缺少小組資訊,無法報到", 2, "/Web/Home/Index");
            else if (OI.DeleteFlag)
                SetAlert("該小組已被移除,無法報到", 2, "/Web/Home/Index");
            else if (!OI.ActiveFlag)
                SetAlert("該小組未被啟用,無法報到", 2, "/Web/Home/Index");
            else if (!MOIs.Any())
                SetAlert("您並非該小組成員,無法報到", 2, "/Web/Home/Index");
            else
            {
                var MLS = DC.Meeting_Location_Set.FirstOrDefault(q => q.SetType == 1 && q.OIID == OI.OIID && !q.DeleteFlag);
                if (MLS != null)
                {
                    if (MLS.WeeklyNo > 0)
                    {
                        DateTime SDT = Convert.ToDateTime(DT.ToString(DateFormat) + " " + MLS.S_hour.ToString().PadLeft(2, '0') + ":" + MLS.S_minute.ToString().PadLeft(2, '0') + ":00");
                        DateTime EDT = Convert.ToDateTime(DT.ToString(DateFormat) + " " + MLS.E_hour.ToString().PadLeft(2, '0') + ":" + MLS.E_minute.ToString().PadLeft(2, '0') + ":00");
                        if (((int)DT.DayOfWeek == 0 && MLS.WeeklyNo != 7) || ((int)DT.DayOfWeek != 0 && (int)DT.DayOfWeek != MLS.WeeklyNo))
                            SetAlert("今日並非小組聚會時間...", 3, "/Web/Home/Index");
                        else if (DT < SDT || DT > EDT)
                            SetAlert("小組聚會時間為" + SDT.ToString("HH:mm") + "~" + EDT.ToString("HH:mm") + ",請在這段時間內報到", 3, "/Web/Home/Index");
                        else
                        {
                            var EJH = DC.Event_Join_Header.FirstOrDefault(q => q.EID == 1 && q.TargetType == 0 && q.TargetID == OI.OIID && q.EventDate == DT.Date);
                            if (EJH == null)
                            {
                                EJH = new Event_Join_Header
                                {
                                    EID = 1,
                                    TargetType = 0,
                                    TargetID = OI.OIID,
                                    EventDate = DT.Date,
                                    Note = "",
                                    CreDate = DT,
                                    UpdDate = DT,
                                    SaveACID = ACID
                                };
                                DC.Event_Join_Header.InsertOnSubmit(EJH);
                                DC.SubmitChanges();
                            }

                            var EJD = DC.Event_Join_Detail.FirstOrDefault(q => q.EJHID == EJH.EJHID && q.ACID == ACID && !q.DeleteFlag);
                            if (EJD == null)
                            {
                                EJD = new Event_Join_Detail
                                {
                                    Event_Join_Header = EJH,
                                    ACID = ACID,
                                    Name = AC != null ? AC.Name_First + AC.Name_Last : "",
                                    PhoneNo = "",
                                    DeleteFlag = false,
                                    JoinDate = DT,
                                    CreDate = DT,
                                    UpdDate = DT,
                                    SaveACID = ACID
                                };
                                DC.Event_Join_Detail.InsertOnSubmit(EJD);
                                DC.SubmitChanges();
                            }
                            else
                            {
                                EJD.Name = AC != null ? AC.Name_First + AC.Name_Last : "";
                                EJD.DeleteFlag = false;
                                EJD.JoinDate = DT;
                                DC.SubmitChanges();
                            }
                            SetAlert("您已報到完成", 1, "/Web/AccountPlace/GroupMeet_List");
                        }
                    }
                    else
                        SetAlert("查無小組聚會時間...", 3, "/Web/Home/Index");
                }
                else
                    SetAlert("查無小組聚會時間...", 3, "/Web/Home/Index");
            }

            return View();
        }

        #endregion
        #region 我的小組聚會紀錄
        public class cGroupMeet_List
        {
            public cTableList cTL = new cTableList();
            public string SDate = "";
            public string EDate = "";

            public string sWeek = "";
            public string sTime = "";
            public string sLocation = "";
        }
        public cGroupMeet_List GetGroupMeet_List(FormCollection FC)
        {
            cGroupMeet_List c = new cGroupMeet_List();
            #region 物件初始化
            c.cTL = new cTableList();
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.cTL.Title = "";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();
            ACID = GetACID();
            #endregion
            #region 前端條件導入
            DateTime _sdt = Convert.ToDateTime("2010/1/1");
            DateTime _edt = Convert.ToDateTime("2010/1/1");
            if (FC != null)
            {
                string s_sdt = FC.Get("txb_SDate");
                string s_edt = FC.Get("txb_EDate");
                if (DateTime.TryParse(s_sdt, out _sdt))
                    c.SDate = s_sdt;
                if (DateTime.TryParse(s_edt, out _edt))
                    c.EDate = s_edt;
                if (c.SDate != "" && c.EDate != "" && _sdt > _edt)
                {
                    c.SDate = s_edt;
                    c.EDate = s_sdt;

                    DateTime dt_ = _sdt;
                    _sdt = _edt;
                    _edt = dt_;
                }
            }
            #endregion
            #region 資料庫納入
            var OIs = GetMOIAC(8, 0, ACID);
            var OI = OIs.OrderByDescending(q => q.JoinDate).FirstOrDefault();
            if (OI == null)
                SetAlert("您並未落戶,無法使用本功能", 3, "/Web/AccountPlace/Index");
            else
            {
                #region 聚會點資訊
                var M = DC.Meeting_Location_Set.FirstOrDefault(q => q.SetType == 1 && q.OIID == OI.OIID && !q.DeleteFlag);
                if (M != null)
                {
                    c.sWeek = sWeeks[M.WeeklyNo];
                    c.sTime = M.S_hour.ToString().PadLeft(2, '0') + ":" + M.S_minute.ToString().PadLeft(2, '0') + "~" + M.E_hour.ToString().PadLeft(2, '0') + ":" + M.E_minute.ToString().PadLeft(2, '0');
                    c.sLocation = M.Meeting_Location.Title;
                }
                #endregion

                var Hs = from q in DC.Event_Join_Header.Where(q => q.TargetType == 0 && q.TargetID > 0)
                         join p in OIs
                         on q.TargetID equals p.OIID
                         select q;
                if (c.EDate != "")
                    Hs = Hs.Where(q => q.EventDate.Date <= _edt.Date);
                if (c.SDate != "")
                    Hs = Hs.Where(q => q.EventDate.Date >= _sdt.Date);


                var TopTitles = new List<cTableCell>();
                TopTitles.Add(new cTableCell { Title = "小組名稱" });
                TopTitles.Add(new cTableCell { Title = "聚會日期" });
                TopTitles.Add(new cTableCell { Title = "出席狀況" });
                c.cTL.Rs.Add(SetTableRowTitle(TopTitles));

                c.cTL.TotalCt = Hs.Count();
                c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
                Hs = Hs.OrderByDescending(q => q.EventDate).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);
                foreach (var H in Hs)
                {
                    OI = OIs.First(q => q.OIID == H.TargetID);
                    var D = H.Event_Join_Detail.FirstOrDefault(q => q.ACID == ACID);
                    cTableRow cTR = new cTableRow();
                    cTR.Cs.Add(new cTableCell { Value = OI.OrganizeInfo.Title + OI.OrganizeInfo.Organize.Title });//小組名稱
                    cTR.Cs.Add(new cTableCell { Value = H.EventDate.ToString(DateFormat) });//聚會日期
                    cTR.Cs.Add(new cTableCell { Value = D == null ? "未出席" : "出席" });//出席狀況
                    c.cTL.Rs.Add(SetTableCellSortNo(cTR));
                }
            }
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
        #region 事工團活動報到
        [HttpGet]
        public ActionResult StaffEvent_Join(int ID)
        {
            GetViewBag();
            ACID = GetACID();
            var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID && !q.DeleteFlag && q.ActiveFlag);
            var EH = DC.Event_Join_Header.FirstOrDefault(q => q.EJHID == ID && !q.Event.DeleteFlag && q.TargetType == 1);

            if (AC == null)
                SetAlert("請先登入", 2, "/Web/Home/Login");
            else if (EH == null)
                SetAlert("缺少事工團活動資訊,無法報到", 2, "/Web/Home/Index");
            else if (EH.Event.DeleteFlag)
                SetAlert("該活動已被移除,無法報到", 2, "/Web/Home/Index");
            else if (!EH.Event.ActiveFlag)
                SetAlert("該活動未被啟用,無法報到", 2, "/Web/Home/Index");
            else
            {
                var MSA = DC.M_Staff_Account.FirstOrDefault(q => q.SID == EH.TargetID && q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag);
                if (MSA == null)
                    SetAlert("您並非該事工團成員,無法報到", 2, "/Web/Home/Index");
                else if (EH.Event_Join_Detail.Any(q => !q.DeleteFlag && q.ACID == ACID))
                    SetAlert("您已報到過了,請勿重複報到", 3, "/Web/Home/Index");
                else if (EH.EventDate.Date != DT.Date)
                    SetAlert("今天不是活動日,不能報到", 2, "/Web/Home/Index");
                else
                {
                    DateTime STime = Convert.ToDateTime(EH.EventDate.ToString(DateFormat) + " " + GetTimeSpanToString(EH.Event.STime));
                    DateTime ETime = Convert.ToDateTime(EH.EventDate.ToString(DateFormat) + " " + GetTimeSpanToString(EH.Event.ETime));
                    if (DT < STime.AddMinutes(-10) || DT > ETime)
                        SetAlert("本事工團活動可打卡時間為" + STime.AddMinutes(-10).ToString(DateTimeFormat) + " 至 " + ETime.ToString(DateTimeFormat), 2, "/Web/Home/Index");
                    else
                    {
                        Event_Join_Detail EJD = DC.Event_Join_Detail.FirstOrDefault(q => q.ACID == ACID);
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
                            EJD.SaveACID = ACID;
                            DC.Event_Join_Detail.InsertOnSubmit(EJD);
                            DC.SubmitChanges();
                        }
                        else
                        {
                            EJD.Name = AC.Name_Last + AC.Name_Last;
                            EJD.DeleteFlag = false;
                            EJD.JoinDate = DT;
                            EJD.UpdDate = DT;
                            EJD.SaveACID = ACID;
                            DC.SubmitChanges();
                        }

                        SetAlert("您已報到完成", 1, "/Web/StaffPlace/MyStaffEvnet_List");
                    }
                }
            }

            return View();
        }

        #endregion
        #region 申請換組
        [HttpGet]
        public string AddChangeGroup()
        {
            Error = "";
            ACID = GetACID();
            int OIID = GetQueryStringInInt("GID");
            var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OID == 8 && q.OIID == OIID && q.ActiveFlag && !q.DeleteFlag);
            if (OI == null)
                Error = "此小組不存在";
            else if (ACID < 0)
                Error = "請先登入會員";
            else
            {
                var COI = DC.Change_OI_Order.FirstOrDefault(q => q.ACID == ACID && q.To_OIID == OIID && !q.DeleteFlag);
                if (COI != null)
                {
                    if (COI.Order_Type == 0)
                        Error = "您已提出過申請,請靜待審核通知";
                    else if (COI.Order_Type == 1)
                        Error = "您已提出過申請已通過審核,請靜待小組長執行落戶";
                    else if (COI.Order_Type == 2)
                        Error = "您已為小組成員";
                    else
                        Error = "您的申請已被駁回";
                }
                else
                {
                    var M = DC.M_OI_Account.FirstOrDefault(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag && q.OrganizeInfo.OID == 8);
                    if (M != null)
                        if (M.OIID == OIID && M.JoinDate != M.CreDate)
                            Error = "您不能對申請加入您目前已經加入的小組";
                    if (Error == "")
                    {
                        COI = new Change_OI_Order();
                        COI.ACID = ACID;
                        COI.From_OIID = M != null ? M.OIID : 1;
                        COI.To_OIID = OI.OIID;
                        COI.Order_Type = 0;
                        COI.DeleteFlag = false;
                        COI.CreDate = DT;
                        COI.UpdDate = DT;
                        COI.SaveACID = ACID;
                        DC.Change_OI_Order.InsertOnSubmit(COI);
                        DC.SubmitChanges();
                        Error = "OK";
                    }
                }
            }
            return Error;
        }
        #endregion
    }
}

