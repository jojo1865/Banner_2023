using Banner.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using static Banner.Areas.Web.Controllers.LeaderPlaceController;

namespace Banner.Areas.Web.Controllers
{
    public class LeaderPlaceController : PublicClass
    {

        // GET: Web/LeaderPlace
        public ActionResult Index()
        {
            GetViewBag();
            return View();
        }

        #region 會友異動
        public class cGetMove_People
        {
            public int ACID = 0;//輸入的ACID
            public int OIID = 0;//要被轉去的小組ID
            public int MyOIID = 0;//管理者的組織ID
        }
        public cGetMove_People GetMove_People(FormCollection FC)
        {
            cGetMove_People c = new cGetMove_People();
            int ACID = GetACID();
            var OIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID).ToList();
            Error = "";

            if (FC != null)
            {
                c.ACID = Convert.ToInt32(FC.Get("txb_ACID"));
                c.OIID = Convert.ToInt32(FC.Get("txb_GroupOIID"));
            }
            #region 檢查輸入
            if (ACID == 0)
                SetAlert("請先登入", 2, "/Web/Home/Login");
            else if (OIs.Count() == 0)
                SetAlert("您並非組織中的領導人員,不能使用本功能", 2, "/Web/Home/Index");
            else
            {
                var OS = GetO();
                var O = OS.FirstOrDefault(q => q.JobTitle == "區長");
                var OIs_ = (from q in OS.Where(q => q.SortNo <= O.SortNo)
                            join p in OIs
                            on q.OID equals p.OID
                            select new { OIs = p, SortNo = q.SortNo }).ToList();
                if (OIs_.Count == 0)
                    SetAlert("您並未具備區長以上職分,不能使用本功能", 2, "/Web/Home/Index");
                else
                    c.MyOIID = OIs_.OrderBy(q => q.SortNo).First().OIs.OIID;


                if (FC != null)
                {
                    //取得轄下所有組織
                    var OIs_1 = OIs_.OrderBy(q => q.SortNo).First();
                    OIs = GetThisOIsFromTree(ref OIs, OIs_1.OIs.OIID);
                    OIs = (from q in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList()
                           join p in OIs.GroupBy(q => q.OIID).ToList()
                           on q.OIID equals p.Key
                           select q).ToList();

                    int Old_OIID = 0;
                    var AC = DC.Account.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == c.ACID);
                    var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.OIID == c.OIID && q.OID == 8);
                    if (AC == null)
                        Error += "此會友不存在<br/>";
                    else
                    {
                        /*var MOIA = DC.M_Role_Account.FirstOrDefault(q => q.ACID == AC.ACID && q.RID == 2 && q.ActiveFlag && !q.DeleteFlag);//是否為會友
                        if (MOIA == null)
                            Error += "此會員尚未成為會友<br/>";
                        else*/
                        {
                            var OI8s = OIs.Where(q => q.OID == 8);
                            var OI8 = (from q in OIs.Where(q => q.OID == 8)
                                       join p in DC.M_OI_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == AC.ACID)
                                       on q.OIID equals p.OIID
                                       select p).FirstOrDefault();
                            if (OI8 == null)
                                Error += "此會友並非您底下的小組成員<br/>";
                            else
                                Old_OIID = OI8.OIID;
                        }
                    }
                    if (OI == null)
                        Error += "此小組不存在<br/>";
                    else
                    {
                        var OI8 = OIs.FirstOrDefault(q => q.OID == 8 && q.OIID == c.OIID);
                        if (OI8 == null)
                            Error += "此組織並非您底下的小組<br/>";
                    }

                    if (Error != "")
                        SetAlert(Error, 2);
                    else
                    {
                        string sLog = DT.ToString(DateTimeFormat) + " 前台" + ACID + "由";

                        //移除該小組名單
                        var MOI = DC.M_OI_Account.FirstOrDefault(q => q.OIID == Old_OIID && q.ACID == c.ACID && q.ActiveFlag && !q.DeleteFlag);
                        if (MOI != null)
                        {
                            MOI.ActiveFlag = false;
                            MOI.LeaveDate = DT;
                            MOI.LeaveUID = ACID;
                            MOI.UpdDate = DT;
                            MOI.SaveACID = ACID;
                            DC.SubmitChanges();

                            sLog += MOI.OIID + "(" + MOI.OrganizeInfo.Title + ")";
                        }
                        sLog += "轉換至";
                        MOI = new M_OI_Account
                        {
                            OrganizeInfo = OI,
                            Account = AC,
                            JoinDate = DT,
                            JoinUID = ACID,
                            LeaveDate = DT,
                            LeaveUID = 0,
                            ActiveFlag = true,
                            DeleteFlag = false,
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = ACID
                        };
                        DC.M_OI_Account.InsertOnSubmit(MOI);
                        DC.SubmitChanges();
                        sLog += MOI.OIID + "(" + MOI.OrganizeInfo.Title + ")";

                        SaveLog(sLog, "組員轉換", AC.ACID.ToString());
                        SetAlert("此會友已更換小組完成", 1);
                    }
                }
            }

            #endregion
            return c;
        }
        [HttpGet]
        public ActionResult Move_People()
        {
            GetViewBag();
            return View(GetMove_People(null));
        }
        [HttpPost]
        public ActionResult Move_People(FormCollection FC)
        {
            GetViewBag();
            return View(GetMove_People(FC));
        }
        #endregion
        #region 牧養組織異動
        public class cGetMove_OrganizeInfo
        {
            public ListSelect LS_Left_O = new ListSelect();
            public ListSelect LS_Target_Left_O = new ListSelect();
            public int Old_OID = 0;
            public int New_OID = 0;
            public int New_OIID = 0;
            public int Old_OIID = 0;
            public string Title = "";
            public int MyOIID = 0;
        }
        public cGetMove_OrganizeInfo GetMove_OrganizeInfo(FormCollection FC)
        {
            cGetMove_OrganizeInfo c = new cGetMove_OrganizeInfo();
            ACID = GetACID();
            var OIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID).ToList();
            Error = "";
            if (ACID == 0)
                SetAlert("請先登入", 2, "/Web/Home/Login");
            else if (OIs.Count() == 0)
                SetAlert("您並非組織中的領導人員,不能使用本功能", 2, "/Web/Home/Index");
            else
            {
                var OS = GetO();
                var O = OS.FirstOrDefault(q => q.OID == 5);
                var OIs_ = (from q in OS.Where(q => q.SortNo <= O.SortNo)
                            join p in OIs
                            on q.OID equals p.OID
                            select new { OIs = p, SortNo = q.SortNo }).ToList();
                if (OIs_.Count == 0)
                    SetAlert("您並未具備區督以上職分,不能使用本功能", 2, "/Web/Home/Index");
                else
                {
                    c.MyOIID = OIs_.OrderBy(q => q.SortNo).First().OIs.OIID;
                    #region 初始化
                    c.LS_Left_O.ControlName = "rbl_Left_O";
                    c.LS_Left_O.ddlList = new List<SelectListItem>();
                    c.LS_Target_Left_O.ControlName = "rbl_TL_O";
                    c.LS_Target_Left_O.ddlList = new List<SelectListItem>();


                    var OIs_1 = OIs_.OrderBy(q => q.SortNo).First();
                    var OSs_ = OS.Where(q => q.SortNo > OIs_1.SortNo).ToList();
                    foreach (var OS_ in OSs_)
                        c.LS_Left_O.ddlList.Add(new SelectListItem { Text = OS_.Title, Value = OS_.OID.ToString() });


                    for (int i = 1; i < OSs_.Count; i++)
                    {
                        c.LS_Target_Left_O.ddlList.Add(new SelectListItem { Text = OSs_[i].Title, Value = OSs_[i].OID.ToString(), Selected = (i == 1) });
                        if (i == 1)
                        {
                            c.Old_OID = OSs_[i].OID;
                            c.Title = OSs_[i - 1].Title;
                            c.New_OID = OSs_[i - 1].OID;
                        }
                    }
                    #endregion
                    #region 前端載入
                    if (FC != null)
                    {
                        c.Old_OID = Convert.ToInt32(FC.Get(c.LS_Target_Left_O.ControlName));
                        c.LS_Target_Left_O.ddlList.ForEach(q => q.Selected = false);
                        c.LS_Target_Left_O.ddlList.First(q => q.Value == c.Old_OID.ToString()).Selected = true;

                        for (int i = 0; i < OSs_.Count; i++)
                        {
                            if (OSs_[i].OID == c.Old_OID)
                            {
                                c.Title = OSs_[i - 1].Title;
                                c.New_OID = OSs_[i - 1].OID;
                            }
                        }
                        c.Old_OIID = Convert.ToInt32(FC.Get("txb_Old_OIID"));
                        c.New_OIID = Convert.ToInt32(FC.Get("txb_New_OIID"));
                        //取得轄下所有組織
                        OIs = GetThisOIsFromTree(ref OIs, OIs_1.OIs.OIID);
                        OIs = (from q in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList()
                               join p in OIs.GroupBy(q => q.OIID).ToList()
                               on q.OIID equals p.Key
                               select q).ToList();
                        //先檢查左邊
                        if (!OIs.Any(q => q.OID == c.Old_OID && q.OIID == c.Old_OIID))
                            Error += "此組織不在您的管轄範圍內,您無法更動<br/>";
                        //再檢查右邊
                        if (!OIs.Any(q => q.OID == c.New_OID && q.OIID == c.New_OIID))
                            Error += "目標" + c.Title + "不在您的管轄範圍內,您無法更動<br/>";



                        if (Error != "")
                            SetAlert(Error, 2);
                        else
                        {
                            string From = "";
                            string To = "";
                            var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == c.Old_OIID);
                            if (OI != null)
                            {
                                var OI_F = DC.OrganizeInfo.First(q => q.OIID == OI.ParentID);
                                From = OI_F.Title + OI_F.Organize.Title;

                                var OI_T = DC.OrganizeInfo.First(q => q.OIID == c.New_OIID);
                                To = OI_T.Title + OI_T.Organize.Title;

                                OI.ParentID = c.New_OIID;
                                OI.UpdDate = DT;
                                OI.SaveACID = ACID;
                                DC.SubmitChanges();
                            }
                            string sLog = DT.ToString(DateTimeFormat) + " 前台" + ACID + "將" + From + "轉移至" + To;

                            SaveLog(sLog, "組織轉換", OI.OIID.ToString());
                            SetAlert(OI.Title + OI.Organize.Title + "由" + From + "至" + To + "搬移完成", 1, "/Web/LeaderPlace/Move_OrganizeInfo");
                        }
                    }
                    #endregion
                }
            }



            return c;
        }
        [HttpGet]
        public ActionResult Move_OrganizeInfo()
        {
            GetViewBag();
            return View(GetMove_OrganizeInfo(null));
        }
        [HttpPost]
        public ActionResult Move_OrganizeInfo(FormCollection FC)
        {
            GetViewBag();
            return View(GetMove_OrganizeInfo(FC));
        }
        //找上一層組織標題
        [HttpGet]
        public string GetUPOTitle(int OID)
        {
            var OS = GetO();
            var O = OS.First(q => q.OID == OID);
            var O_ = OS.Where(q => q.SortNo < O.SortNo).OrderByDescending(q => q.SortNo).First();
            return O_.Title;
        }
        #endregion
        #region 找下下層與下一層組織
        [HttpGet]
        public string GetNextOID(int OID)
        {
            var OS = GetO();
            var O = OS.First(q => q.OID == OID);
            var O_ = OS.Where(q => q.SortNo < O.SortNo).OrderByDescending(q => q.SortNo).First();
            return O_.OID.ToString();
        }
        #endregion
        #region 會友查詢與轉領夜-列表
        public class cGetSet_LeadTheNight_List
        {
            public cTableList cTL = new cTableList();
            public string GroupTitle = "";
            public string ACName = "";
            public int MyOIID = 0;
        }
        public cGetSet_LeadTheNight_List GetSet_LeadTheNight_List(FormCollection FC)
        {
            cGetSet_LeadTheNight_List c = new cGetSet_LeadTheNight_List();
            ACID = GetACID();
            var OIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID).ToList();
            Error = "";
            #region 物件初始化
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.cTL.Title = "小組名單";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();
            if (FC != null)
            {
                c.GroupTitle = FC.Get("txb_GroupTitle");
                c.ACName = FC.Get("txb_ACName");
            }
            #endregion

            if (ACID == 0)
                SetAlert("請先登入", 2, "/Web/Home/Login");
            else if (OIs.Count() == 0)
                SetAlert("您並非組織中的領導人員,不能使用本功能", 2, "/Web/Home/Index");
            else
            {
                var OS = GetO();
                var O = OS.FirstOrDefault(q => q.JobTitle == "區長");
                var OIs_ = (from q in OS.Where(q => q.SortNo <= O.SortNo)
                            join p in OIs
                            on q.OID equals p.OID
                            select new { OIs = p, SortNo = q.SortNo }).ToList();
                if (OIs_.Count == 0)
                    SetAlert("您並未具備區長以上職分,不能使用本功能", 2, "/Web/Home/Index");
                else
                {
                    c.MyOIID = OIs_.OrderBy(q => q.SortNo).First().OIs.OIID;

                    //取得轄下所有組織
                    var OIs_1 = OIs_.OrderBy(q => q.SortNo).First();
                    OIs = GetThisOIsFromTree(ref OIs, OIs_1.OIs.OIID);
                    OIs = (from q in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList()
                           join p in OIs.GroupBy(q => q.OIID).ToList()
                           on q.OIID equals p.Key
                           select q).ToList();
                    //篩選小組
                    if (!string.IsNullOrEmpty(c.GroupTitle))
                        OIs = OIs.Where(q => q.Title == c.GroupTitle && q.OID == 8).ToList();
                    //取得轄下小組員
                    var OI_ACs = from q in DC.M_OI_Account.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList()
                                 join p in OIs.Where(q => q.OID == 8).ToList()
                                 on q.OIID equals p.OIID
                                 select q;
                    //篩選具會友資格
                    var ACs_1 = from q in DC.M_Role_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && (q.RID == 2 || q.RID == 24)).ToList()
                                join p in OI_ACs
                                on q.ACID equals p.ACID
                                select new { q.ACID };
                    //篩選被案例的人
                    var ACs_2 = from q in DC.M_O_Account.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList()
                                join p in OI_ACs
                                on q.ACID equals p.ACID
                                select new { q.ACID };
                    //會友或領夜 排除被案立(小組長椅上)的人
                    var ACs_3 = ACs_1.Except(ACs_2);

                    //篩選會友名單
                    var Ns = (from q in DC.Account.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList()
                              join p in ACs_3.GroupBy(q => q.ACID)
                              on q.ACID equals p.Key
                              select q).ToList();
                    if (!string.IsNullOrEmpty(c.ACName))
                        Ns = Ns.Where(q => (q.Name_First + q.Name_Last).Contains(c.ACName) || q.ACID.ToString() == c.ACName).ToList();

                    var TopTitles = new List<cTableCell>();

                    TopTitles.Add(new cTableCell { Title = "會員ID", WidthPX = 100 });
                    TopTitles.Add(new cTableCell { Title = "姓名", WidthPX = 160 });
                    TopTitles.Add(new cTableCell { Title = "身分", WidthPX = 120 });
                    TopTitles.Add(new cTableCell { Title = "所屬小組", WidthPX = 160 });
                    TopTitles.Add(new cTableCell { Title = "動作" });

                    c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
                    ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
                    c.cTL.TotalCt = Ns.Count();
                    c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
                    Ns = Ns.OrderByDescending(q => q.Name_First).ThenByDescending(q => q.Name_Last).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut).ToList();
                    //分辨會員(1)/會友(2)/領夜(24)用
                    var Rs = (from q in Ns.GroupBy(q => q.ACID)
                              join p in DC.M_Role_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.Role.RoleType == 0)
                              on q.Key equals p.ACID
                              select p).ToList();

                    foreach (var N in Ns)
                    {
                        cTableRow cTR = new cTableRow();
                        cTR.Cs.Add(new cTableCell { Value = N.ACID.ToString() }); //會員ID
                        cTR.Cs.Add(new cTableCell { Value = N.Name_First + N.Name_Last }); // 姓名

                        //身分
                        #region 身分判定
                        //檢查領夜
                        var R_24 = Rs.FirstOrDefault(q => q.ACID == N.ACID && q.RID == 24);
                        //檢查會友
                        var R_2 = Rs.FirstOrDefault(q => q.ACID == N.ACID && q.RID == 2);

                        if (R_24 != null)
                            cTR.Cs.Add(new cTableCell { Value = R_24.Role.Title });
                        else if (R_2 != null)
                            cTR.Cs.Add(new cTableCell { Value = R_2.Role.Title });
                        else
                            cTR.Cs.Add(new cTableCell { Value = "會員" });
                        #endregion
                        //所屬小組
                        var OI_ = OI_ACs.FirstOrDefault(q => q.ACID == N.ACID);
                        if (OI_ != null)
                            cTR.Cs.Add(new cTableCell { Value = OI_.OrganizeInfo.Title });
                        else
                            cTR.Cs.Add(new cTableCell { Value = "--" });

                        cTableCell cTC = new cTableCell();
                        cTC.cTCs.Add(new cTableCell { Type = "link", Value = "查看", Target = "_blank", CSS = "btn btn-primary", URL = "/Web/LeaderPlace/Set_LeadTheNight_Info?ID=" + N.ACID });
                        //移除?
                        if (R_24 != null)
                            cTC.cTCs.Add(new cTableCell { Type = "button", Value = "取消領夜", Target = "_blank", CSS = "btn btn-warning", URL = "RemoveLeaderNight(" + N.ACID + ");" });
                        else
                            cTC.cTCs.Add(new cTableCell { Type = "button", Value = "設為領夜", Target = "_blank", CSS = "btn btn-success", URL = "AddLeaderNight(" + N.ACID + ");" });
                        cTR.Cs.Add(cTC);
                        c.cTL.Rs.Add(SetTableCellSortNo(cTR));
                    }
                }
            }

            return c;
        }
        [HttpGet]
        public ActionResult Set_LeadTheNight_List()
        {
            GetViewBag();
            return View(GetSet_LeadTheNight_List(null));
        }
        [HttpPost]
        public ActionResult Set_LeadTheNight_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetSet_LeadTheNight_List(FC));
        }
        [HttpGet]
        public void RemoveLeaderNight(int ACID)
        {
            var MR = DC.M_Role_Account.FirstOrDefault(q => q.ACID == ACID && q.RID == 24 && q.ActiveFlag);
            if (MR != null)
            {
                MR.ActiveFlag = false;
                MR.LeaveDate = DT;
                MR.UpdDate = DT;
                MR.SaveACID = GetACID();
                DC.SubmitChanges();
            }
        }
        [HttpGet]
        public void AddLeaderNight(int ACID)
        {
            var MR = DC.M_Role_Account.FirstOrDefault(q => q.ACID == ACID && q.RID == 24);
            if (MR != null)
            {
                MR.ActiveFlag = true;
                MR.DeleteFlag = false;
                MR.JoinDate = DT;
                MR.LeaveDate = MR.CreDate;
                MR.UpdDate = DT;
                MR.SaveACID = GetACID();
                DC.SubmitChanges();
            }
            else
            {
                MR = new M_Role_Account();
                MR.ACID = ACID;
                MR.RID = 24;
                MR.JoinDate = DT;
                MR.LeaveDate = DT;
                MR.Note = "";
                MR.ActiveFlag = true;
                MR.DeleteFlag = false;
                MR.CreDate = DT;
                MR.UpdDate = DT;
                MR.SaveACID = GetACID();
                DC.M_Role_Account.InsertOnSubmit(MR);
                DC.SubmitChanges();
            }
        }
        #endregion
        #region 會友查詢與轉領夜-查看
        public class cGetSet_LeadTheNight_Info
        {
            public string Name = "";//姓名
            public string PhoneNo = "";//手機
            public string Email = "";
            public string BD = "";//生日
            public string JobTitle_LN = "";//身分
            public string GroupTitle = "";//小組

            public cTableList cTL_Class = new cTableList();//課程歷程
            public cTableList cTL_Banner = new cTableList();//牧養歷程
        }

        public cGetSet_LeadTheNight_Info GetSet_LeadTheNight_Info(int ID)
        {
            cGetSet_LeadTheNight_Info c = new cGetSet_LeadTheNight_Info();
            ACID = GetACID();
            var OIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID).ToList();

            Error = "";
            if (ACID == 0)
                SetAlert("請先登入", 2, "/Web/Home/Login");
            else if (OIs.Count() == 0)
                SetAlert("您並非組織中的領導人員,不能使用本功能", 2, "/Web/Home/Index");

            else
            {
                var AC = DC.Account.FirstOrDefault(q => q.ACID == ID && !q.DeleteFlag);

                var OS = GetO();
                var O = OS.FirstOrDefault(q => q.JobTitle == "區長");
                var OIs_ = (from q in OS.Where(q => q.SortNo <= O.SortNo)
                            join p in OIs
                            on q.OID equals p.OID
                            select new { OIs = p, SortNo = q.SortNo }).ToList();
                if (OIs_.Count == 0)
                    SetAlert("您並未具備區長以上職分,不能使用本功能", 2, "/Web/Home/Index");
                else if (AC == null)
                    SetAlert("此會友資料不存在", 2, "/Web/LeaderPlace/Set_LeadTheNight_List");
                else
                {
                    //取得旗下組織
                    var OIs_1 = OIs_.OrderBy(q => q.SortNo).First();
                    OIs = GetThisOIsFromTree(ref OIs, OIs_1.OIs.OIID);
                    OIs = (from q in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList()
                           join p in OIs.GroupBy(q => q.OIID).ToList()
                           on q.OIID equals p.Key
                           select q).ToList();
                    //取得旗下小組員
                    var OI_ACs = from q in DC.M_OI_Account.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList()
                                 join p in OIs.Where(q => q.OID == 8).ToList()
                                 on q.OIID equals p.OIID
                                 select q;
                    if (!OI_ACs.Any(q => q.ACID == AC.ACID))
                        SetAlert("此會友非您旗下會友,您不能檢視資料", 2, "/Web/LeaderPlace/Set_LeadTheNight_List");
                    else
                    {
                        c.Name = AC.Name_First + AC.Name_Last;
                        c.BD = AC.Birthday != AC.CreDate ? AC.Birthday.ToString(DateFormat) : "";
                        var OI = OI_ACs.First(q => q.ACID == AC.ACID);
                        c.GroupTitle = OI.OrganizeInfo.Title + OI.OrganizeInfo.Organize.Title;
                        #region 聯絡資訊

                        var Cons = DC.Contect.Where(q => q.TargetID == AC.ACID && q.TargetType == 2);
                        var Con = Cons.FirstOrDefault(q => q.ContectType == 2);
                        if (Con != null)
                            c.PhoneNo = Con.ContectValue;
                        Con = Cons.FirstOrDefault(q => q.ContectType == 3);
                        if (Con != null)
                            c.Email = Con.ContectValue;
                        #endregion
                        #region 身分
                        var Rs = DC.M_Role_Account.Where(q => q.ACID == AC.ACID);
                        if (Rs.Any(q => q.RID == 24))
                            c.JobTitle_LN = "領夜同工";
                        else if (Rs.Any(q => q.RID == 2))
                            c.JobTitle_LN = "會友";
                        #endregion
                        #region 課程歷程
                        c.cTL_Class.Rs = new List<cTableRow>();
                        c.cTL_Class.NumCut = 0;
                        var TopTitles = new List<cTableCell>();

                        TopTitles.Add(new cTableCell { Title = "結業日期", WidthPX = 120 });
                        TopTitles.Add(new cTableCell { Title = "結業課程" });

                        c.cTL_Class.Rs.Add(SetTableRowTitle(TopTitles));
                        ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";

                        List<cPhistry> Ls_P = GetPHistory(AC);
                        foreach (var L in Ls_P.OrderByDescending(q => q.dDate))
                        {
                            cTableRow cTR = new cTableRow();
                            cTR.Cs.Add(new cTableCell { Value = L.dDate.ToString(DateFormat) }); //結業日期
                            cTR.Cs.Add(new cTableCell { Value = L.sTitle_C + " " + L.sTitle_P }); //結業課程

                            c.cTL_Class.Rs.Add(SetTableCellSortNo(cTR));
                        }
                        #endregion
                        #region 牧養歷程
                        List<cAChistry> Ls_AC = GetACHistory(AC);
                        c.cTL_Banner.Rs = new List<cTableRow>();
                        c.cTL_Banner.NumCut = 0;
                        TopTitles = new List<cTableCell>();
                        TopTitles.Add(new cTableCell { Title = "日期", WidthPX = 120 });
                        TopTitles.Add(new cTableCell { Title = "類型", WidthPX = 160 });
                        TopTitles.Add(new cTableCell { Title = "歷程" });
                        
                        c.cTL_Banner.Rs.Add(SetTableRowTitle(TopTitles));
                        foreach (var L in Ls_AC.OrderByDescending(q=>q.dDate))
                        {
                            cTableRow cTR = new cTableRow();
                            cTR.Cs.Add(new cTableCell { Value = L.dDate.ToString(DateFormat) }); //日期
                            cTR.Cs.Add(new cTableCell { Value = L.sType }); //類型
                            cTR.Cs.Add(new cTableCell { Value = L.sTitle }); //歷程
                            c.cTL_Banner.Rs.Add(SetTableCellSortNo(cTR));
                        }

                        #endregion
                    }
                }
            }

            return c;
        }
        [HttpGet]
        public ActionResult Set_LeadTheNight_Info(int ID)
        {
            GetViewBag();
            return View(GetSet_LeadTheNight_Info(ID));
        }
        #endregion


    }
}