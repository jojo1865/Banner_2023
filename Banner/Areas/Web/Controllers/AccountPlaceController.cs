using Antlr.Runtime;
using Banner.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using static Banner.Areas.Admin.Controllers.AccountSetController;
using static Banner.Areas.Admin.Controllers.OrganizeSetController;

namespace Banner.Areas.Web.Controllers
{
    public class AccountPlaceController : PublicClass
    {
        int ACID = 0;
        #region 會員中心-首頁
        public class cAccountPlace_Index
        {
            public List<cMeetingMsg> cMLs = new List<cMeetingMsg>();
            //public cTree Tree = new cTree();
        }
        public class cMeetingMsg
        {
            public string JobTitle = "";
            public string OIList = "";
            public string GroupTitle = "";
            public string Week = "";
            public string Time = "";
            public string Location = "";
        }
        public ActionResult Index()
        {
            GetViewBag();
            ACID = GetACID();
            cAccountPlace_Index N = new cAccountPlace_Index();
            N.cMLs = new List<cMeetingMsg>();
            var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag);
            if (AC != null)
            {

                var MOIs = GetMOIAC(0, 0, ACID);
                foreach (var MOI in MOIs)
                {
                    cMeetingMsg cM = new cMeetingMsg
                    {
                        JobTitle = MOI.OrganizeInfo.ACID == ACID ? "小組長" : "小組員",
                        OIList = "",
                        GroupTitle = MOI.OrganizeInfo.Title + MOI.OrganizeInfo.Organize.Title,
                        Week = "--",
                        Time = "--",
                        Location = "--",
                    };
                    #region 小組層級

                    string sOITitle = MOI.OrganizeInfo.Title + MOI.OrganizeInfo.Organize.Title + "(" + MOI.OrganizeInfo.Organize.Title + "編號:" + MOI.OrganizeInfo.OIID.ToString().PadLeft(5, '0') + ")";
                    var OI_ = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == MOI.OrganizeInfo.ParentID && !q.DeleteFlag && q.ActiveFlag && q.OID > 4);
                    while (OI_ != null)
                    {
                        sOITitle = OI_.Title + OI_.Organize.Title + "/" + sOITitle;
                        OI_ = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == OI_.ParentID && !q.DeleteFlag && q.ActiveFlag && q.OID > 4);
                    }
                    cM.OIList = sOITitle;
                    #endregion
                    #region 小組聚會點

                    var MLS = DC.M_Location_Set.FirstOrDefault(q => q.SetType == 1 && q.TargetID == MOI.OIID && q.ActiveFlag && !q.DeleteFlag);
                    if (MLS != null)
                    {
                        cM.Week = sWeeks[MLS.WeeklyNo];
                        cM.Time = MLS.S_hour.ToString().PadLeft(2, '0') + ":" +
                            MLS.S_minute.ToString().PadLeft(2, '0') + "~" +
                            MLS.E_hour.ToString().PadLeft(2, '0') + ":" +
                            MLS.E_minute.ToString().PadLeft(2, '0');

                        var L = DC.Location.FirstOrDefault(q => q.TargetType == 3 && q.TargetID == MLS.MID);
                        cM.Location = GetLocationString(L);
                    }

                    #endregion
                    N.cMLs.Add(cM);
                }
            }

            return View(N);
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
                            if (Con.ZID != 10 && Con.ZID != 1)
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
                #region 介面資料填入
                if (FC != null)
                {
                    N.AC.Name = FC.Get("txb_Name");
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
                        N.AC.GroupType = N.GroupNo = FC.Get("txb_GroupNo");
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
            ACID = GetACID();
            return View(GerAccountData(ACID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BasicData(FormCollection FC)
        {
            GetViewBag();
            ACID = GetACID();
            var N = GerAccountData(ACID, FC);


            //會員資料更新
            N.AC.UpdDate = DT;
            N.AC.SaveACID = GetACID();
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
                          join p in DC.M_Location_Set.Where(q => q.SetType == 0)
                          on q.MLID equals p.MLID
                          select q;
                foreach (var ML in MLs)
                {
                    ML.DeleteFlag = true;
                    ML.UpdDate = DT;
                    ML.SaveACID = GetACID();
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


    }
}

