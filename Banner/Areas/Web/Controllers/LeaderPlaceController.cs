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
            public int ACID = 0;
            public int OIID = 0;
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
                c.OIID = Convert.ToInt32(FC.Get("txb_OIID"));
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
                else if (FC != null)
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
                            LeaveDate = DT,
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
                            SetAlert(OI.Title + OI.Organize.Title + "由" + From + "至" + To + "搬移完成", 1);
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
        #region 新增牧養組織與按立
        //New_OrganizeInfo
        #endregion
    }
}