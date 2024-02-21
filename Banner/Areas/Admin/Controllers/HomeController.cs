using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Bson;

//using OfficeOpenXml.FormulaParsing.ExpressionGraph.FunctionCompilers;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Admin.Controllers
{
    public class HomeController : PublicClass
    {
        // GET: Admin/Home
        public ActionResult Index()
        {
            GetViewBag();
            if (GetACID() <= 0)
                SetAlert("請先登入", 2, "/Admin/Home/Login");

            return View();
        }
        #region 作弊登入
        public void LoginACID()
        {
            int ACID = GetQueryStringInInt("ACID");
            var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID);
            if (AC != null)
            {
                SetBrowserData("ACID", AC.ACID.ToString());
                SetBrowserData("UserName", AC.Name_First + AC.Name_Last);
                Response.Redirect("/Admin/Home/Index");
            }

        }
        #endregion
        #region 登入
        public ActionResult Login()
        {
            GetViewBag();
            if (Request.Url.Host == "localhost" && Request.Url.Port == 44307)
            {
                if (GetACID() <= 0)
                {
                    //LogInAC(1);
                    //SetBrowserData("UserName", "系統管理者");
                    LogInAC(8197);
                    SetBrowserData("UserName", "JOJO");
                    SetAlert("", 1, "/Admin/Home/Index");
                }
            }
            else if (Request.Url.Host == "web-banner.viuto-aiot.com")
                SetAlert("", 1, "/Web/Home/Index");
            TempData["login"] = "";
            TempData["pw"] = "";
            return View();
        }
        [HttpPost]

        public ActionResult Login(FormCollection FC)
        {
            GetViewBag();

            string Login = FC.Get("txb_Login");
            string PW = FC.Get("txb_Password");
            string ValidateCode = FC.Get("txb_ValidateCode");
            TempData["login"] = Login;
            TempData["pw"] = PW;

            if (Login.Replace(" ", "") == string.Empty)
                Error += "請輸入帳號</br>";
            if (PW.Replace(" ", "") == string.Empty)
                Error += "請輸入密碼</br>";
            if (ValidateCode.Replace(" ", "") == string.Empty)
                Error += "請輸入驗證碼</br>";
            else if (ValidateCode != GetSession("VNum"))
                Error += "驗證碼輸入錯誤</br>";
            if (Error != "")
                TempData["msg"] = Error;
            else
            {
                string EncPW = HSM.Enc_1(PW);
                var AC = DC.Account.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.BackUsedFlag && q.Login == Login && q.Password == EncPW);
                if (AC == null)
                {
                    AC = DC.Account.Where(q => q.Login == Login && q.ActiveFlag && !q.DeleteFlag).OrderByDescending(q => q.CreDate).FirstOrDefault();
                    if (AC == null)
                        SetAlert("此帳號不存在", 2);
                    /*else if (AC.DeleteFlag)
                        SetAlert("此帳號已被移除", 2);
                    else if (!AC.ActiveFlag)
                        SetAlert("此帳號已被關閉", 2);*/
                    else if (AC.Password != EncPW)
                    {
                        if (AC.Password == "")//資料庫中無密碼,所以依據此次輸入設定新密碼
                        {
                            AC.Password = EncPW;
                            DC.SubmitChanges();

                            SetBrowserData("ACID", AC.ACID.ToString());
                            SetBrowserData("UserName", AC.Name_First + AC.Name_Last);
                            SetAlert((AC.Name_First + AC.Name_Last) + " 歡迎回來", 1, "/Admin/Home/Index");
                        }
                        else
                        {
                            string sLoginCt = GetSession("LoginCt");
                            string sLoginAccount = GetSession("LoginAccount");
                            if (sLoginAccount == Login)//登入的帳號是否為同一人
                            {
                                int iLoginCt = 0;
                                int.TryParse(sLoginCt, out iLoginCt);
                                iLoginCt++;
                                SetSession("LoginCt", iLoginCt.ToString());
                                if (iLoginCt < 5)
                                {
                                    SetAlert("您的密碼錯誤!!</br>您還有" + (5 - iLoginCt) + "次機會...</br>建議您使用忘記密碼功能", 2, "/Admin/Home/ForgetPassWord/");
                                }
                                else
                                {
                                    AC.ActiveFlag = false;
                                    DC.SubmitChanges();
                                    SetAlert("您登入錯誤太多次,帳號已被鎖定</br>請洽系統管理者協助解鎖...", 2);
                                }
                            }
                            else
                            {
                                SetSession("LoginCt", "1");
                                SetAlert("您的密碼錯誤!!</br>您還有4次機會...");
                            }
                            SetSession("LoginAccount", Login);
                        }

                    }

                }
                else
                {
                    //登入成功 順便進行每日批次
                    Batch_EveryDay();

                    DelSession("LoginCt");
                    DelSession("LoginAccount");
                    LogInAC(AC.ACID);
                    SetBrowserData("UserName", (AC.Name_First + AC.Name_Last).ToString());
                    SetAlert((AC.Name_First + AC.Name_Last) + " 歡迎回來", 1, "/Admin/Home/Index");
                }
            }
            return View();
        }
        #endregion
        #region 忘記密碼
        public ActionResult ForgetPassword()
        {
            GetViewBag();
            return View();
        }
        [HttpPost]

        public ActionResult ForgetPassword(FormCollection FC)
        {
            GetViewBag();
            string Email = FC.Get("txb_Email");
            string CellPhone = FC.Get("txb_CellPhone");
            if (FC.Get("btn_SendEmail") != null)
            {
                if (Email != "")
                {
                    var AC = (from q in DC.Account.Where(q => !q.DeleteFlag && q.ActiveFlag && q.BackUsedFlag)
                              join p in DC.Contect.Where(q => q.ContectValue == Email && q.ContectType == 2 && q.TargetType == 2)
                              on q.ACID equals p.TargetID
                              select q).FirstOrDefault();
                    if (AC != null)
                    {
                        int PW = GetRand(1000000);
                        AC.Password = HSM.Enc_1(PW.ToString());
                        AC.UpdDate = DT;
                        DC.SubmitChanges();
                        string MailData = "親愛的旌旗家人 您好：</br></br>" +
                            "這裡是【全球旌旗資訊網】自動發信系統</br></br>" +
                            "您的新密碼為：{0}</br>" +
                            "請立即登入，即可修改密碼。</br></br>" +
                            "如需有任何問題，請寫信至itsupport@wwbch.org</br></br>" +
                            "旌旗教會 敬上";
                        SendMail(Email, AC.Name_First + AC.Name_Last, "【全球旌旗資訊網】忘記密碼通知信", string.Format(MailData, PW.ToString()));
                        SetAlert("您的新密碼已發送,請查看您的信箱", 1, "/Admin/Home/Login");
                    }
                    else
                        SetAlert("查無帳號,請重新輸入", 3);
                }
                else
                    SetAlert("請輸入Email", 2);
            }
            else if (FC.Get("btn_SendPhone") != null)
            {
                if (CellPhone != "")
                {
                    var AC = (from q in DC.Account.Where(q => !q.DeleteFlag && q.ActiveFlag && q.BackUsedFlag)
                              join p in DC.Contect.Where(q => q.ContectValue == CellPhone && q.ContectType == 1 && q.TargetType == 2)
                              on q.ACID equals p.TargetID
                              select q).FirstOrDefault();

                    if (AC != null)
                    {
                        int PW = GetRand(1000000);
                        AC.Password = HSM.Enc_1(PW.ToString());
                        AC.UpdDate = DT;
                        DC.SubmitChanges();
                        SendSNS(CellPhone, "【全球旌旗資訊網】忘記密碼通知簡訊", "親愛的旌旗家人,您的新密碼為：" + PW.ToString() + ",請立即登入，即可修改密碼。");

                        SetAlert("您的新密碼已發送,請查看您的簡訊", 1, "/Admin/Home/Login");
                    }
                    else
                        SetAlert("查無帳號,請重新輸入", 3);
                }
                else
                    SetAlert("請輸入手機號碼", 2);
            }
            else
                SetAlert("操作不正確", 2);


            return View();
        }
        #endregion
        #region 忘記帳號
        public ActionResult ForgetAccount()
        {
            GetViewBag();
            return View();
        }
        [HttpPost]

        public ActionResult ForgetAccount(FormCollection FC)
        {
            GetViewBag();
            string Email = FC.Get("txb_Email");
            string CellPhone = FC.Get("txb_CellPhone");
            if (FC.Get("btn_SendEmail") != null)
            {
                if (Email != "")
                {
                    var AC = (from q in DC.Account.Where(q => !q.DeleteFlag && q.ActiveFlag && q.BackUsedFlag)
                              join p in DC.Contect.Where(q => q.ContectValue == Email && q.ContectType == 2 && q.TargetType == 2)
                              on q.ACID equals p.TargetID
                              select q).FirstOrDefault();
                    if (AC != null)
                    {
                        string MailData = "親愛的旌旗家人 您好：</br></br>" +
                           "這裡是【全球旌旗資訊網】自動發信系統</br></br>" +
                           "您的帳號為：{0}</br></br>" +
                           "如需有任何問題，請寫信至itsupport@wwbch.org</br></br>" +
                           "旌旗教會 敬上";
                        SendMail(Email, AC.Name_First + AC.Name_Last, "【全球旌旗資訊網】忘記帳號通知信", string.Format(MailData, AC.Login));
                        SetAlert("您的帳號已發送,請查看您的信箱", 1, "/Admin/Home/Login");
                    }
                    else
                        SetAlert("查無帳號,請重新輸入", 3);
                }
                else
                    SetAlert("請輸入Email", 2);
            }
            else if (FC.Get("btn_SendPhone") != null)
            {
                if (CellPhone != "")
                {

                    var AC = (from q in DC.Account.Where(q => !q.DeleteFlag && q.ActiveFlag && q.BackUsedFlag)
                              join p in DC.Contect.Where(q => q.ContectValue == CellPhone && q.ContectType == 1 && q.TargetType == 2)
                              on q.ACID equals p.TargetID
                              select q).FirstOrDefault();
                    if (AC != null)
                    {
                        SendSNS(CellPhone, "【全球旌旗資訊網】忘記帳號通知簡訊", "親愛的旌旗家人您好,您的帳號為：" + AC.Login);
                        SetAlert("您的帳號已發送,請查看您的簡訊", 1, "/Admin/Home/Login");
                    }
                    else
                        SetAlert("查無帳號,請重新輸入", 3);
                }
                else
                    SetAlert("請輸入手機號碼", 2);
            }
            else
                SetAlert("操作不正確", 2);


            return View();
        }
        #endregion
        #region 登出
        public ActionResult Logout()
        {
            GetViewBag();
            LogOutAC();
            //if (GetACID() <= 0)
            SetAlert("您已登出", 1, "/Admin/Home/Login");

            return View();
        }

        #endregion
        #region 變更密碼
        public ActionResult ChangePassword()
        {
            GetViewBag();
            if (GetACID() <= 0)
                SetAlert("請先登入", 2, "/Admin/Home/Login");
            return View();
        }
        [HttpPost]

        public ActionResult ChangePassword(FormCollection FC)
        {
            GetViewBag();
            var AC = DC.Account.FirstOrDefault(q => q.ACID == GetACID() && !q.DeleteFlag);
            string Old = FC.Get("txb_Old");
            string New1 = FC.Get("txb_New1");
            string New2 = FC.Get("txb_New2");
            if (AC == null)
                SetAlert("請先登入", 2, "/Admin/Home/Login");
            else
            {
                AC.Password = HSM.Enc_1(New1);
                AC.UpdDate = DT;
                AC.SaveACID = AC.ACID;
                DC.SubmitChanges();

                SetAlert("變更密碼完成", 1, "/Admin/Home/Index");
            }

            return View();
        }
        public string CheckPasswordInput(int ACID, string Old, string New1, string New2)
        {
            var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID && !q.DeleteFlag);
            if (AC == null)
                Error += "請先登入</br>";
            else if (Old == "")
                Error += "請輸入舊密碼</br>";
            else if (New1 == "")
                Error += "請輸入新密碼</br>";
            else if (New2 == "")
                Error += "請再次輸入新密碼</br>";
            else
            {
                if (HSM.Enc_1(Old) != AC.Password)
                    Error += "舊密碼輸入錯誤</br>";
                else if (!CheckPasswork(New1))
                    Error += "密碼必須為包含大小寫英文與數字的8碼以上字串</br>";
                else if (New1 != New2)
                    Error += "新密碼與重複輸入的不同</br>";
            }


            return Error;
        }
        #endregion
        #region 取得檢查碼
        //取得/設定檢查碼
        public void ValidateCode()
        {
            string Code = GetQueryStringInString("Code");
            if (Code == "")
            {
                Code = GenerateCheckCode();
                SetSession("VNum", Code);
            }
            CreateCheckCodeImage(Code);
        }
        #endregion
        #region 取得地址下層選單
        [HttpGet]
        public string GetZipSelect(int ZID)
        {
            var Zs = from q in DC.ZipCode.Where(q => q.ParentID == ZID && q.ActiveFlag).OrderBy(q => q.Code)
                     select new { value = q.ZID, Text = q.Code + " " + q.Title, Code = q.Code };

            return JsonConvert.SerializeObject(Zs);
        }

        #endregion
        #region 取得郵遞區號 或 電話國碼
        [HttpGet]
        public string GetZipCode(int ZID)
        {
            var Z = DC.ZipCode.FirstOrDefault(q => q.ZID == ZID && q.ActiveFlag);

            return Z == null ? "" : Z.Code;
        }

        #endregion
        #region 取得下一層組織選單
        [HttpGet]
        public string GetOISelect(int OIID)
        {
            var OIs = from q in DC.OrganizeInfo.Where(q => q.ParentID == OIID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.Title)
                      select new { value = q.OIID, Text = q.Title };

            return JsonConvert.SerializeObject(OIs);
        }
        #endregion
        #region 用關鍵字查組織
        [HttpGet]
        public string GetOIList(int ACID, string Key)
        {
            if (Key.Length > 1 && !Key.Contains(")"))
            {
                var OIs_G = from q in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 8 && (q.Title.StartsWith(Key) || q.OIID.ToString().StartsWith(Key)))
                            group q by new { q.OIID, q.Title } into g
                            select new { g.Key.OIID, g.Key.Title };
                var OIs = from q in OIs_G.OrderByDescending(q => q.OIID)
                          select new { value = "(" + q.OIID + ")" + q.Title };

                if (ACID > 0)
                {
                    var MOIAs = from q in DC.M_OI_Account.Where(q => q.ACID == ACID && !q.DeleteFlag && !q.ActiveFlag)
                                group q by new { q.OIID, q.OrganizeInfo.Title } into g
                                select new { value = "(" + g.Key.OIID + ")" + g.Key.Title };
                    if (MOIAs.Count() > 0)
                        OIs = OIs.Except(MOIAs);//排除參加過的小組
                }

                return JsonConvert.SerializeObject(OIs);
            }
            else
                return "[]";
        }

        #endregion
        #region 更新啟用狀態
        public string ChangeActive(string TableName, int ID)
        {
            string Msg = "NG";
            ACID = GetACID();
            switch (TableName)
            {
                case "OrganizeInfo":
                    {
                        var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == ID);
                        if (OI != null)
                        {
                            var OIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ParentID == OI.OIID);
                            if (OIs.Count() > 0)
                                Msg = "請先移除此組織下的組織才可停用";
                            else if (OI.OID == 8)
                            {
                                var Ms = OI.M_OI_Account.Where(q => q.ActiveFlag && !q.DeleteFlag);
                                if (Ms.Count() > 0)
                                    Msg = "請先移除此小組內的成員才可停用";
                            }
                            if (Msg == "")
                            {
                                OI.ActiveFlag = !OI.ActiveFlag;
                                OI.UpdDate = DT;
                                OI.SaveACID = ACID;
                                DC.SubmitChanges();

                                Msg = "OK";
                            }
                        }
                        else
                            Msg = "查無此組織";
                    }
                    break;

                case "Menu":
                    {
                        var M = DC.Menu.FirstOrDefault(q => q.MID == ID);
                        if (M != null)
                        {
                            M.ActiveFlag = !M.ActiveFlag;
                            M.UpdDate = DT;
                            M.SaveACID = ACID;
                            DC.SubmitChanges();

                            Msg = "OK";
                        }
                        else
                            Msg = "查無此選單";
                    }
                    break;

                case "Role":
                    {
                        var M = DC.Role.FirstOrDefault(q => q.RID == ID);
                        if (M != null)
                        {
                            M.ActiveFlag = !M.ActiveFlag;
                            M.UpdDate = DT;
                            M.SaveACID = ACID;
                            DC.SubmitChanges();

                            Msg = "OK";
                        }
                        else
                            Msg = "查無此權限";
                    }
                    break;
                case "Token_Check":
                    {
                        var M = DC.Token_Check.FirstOrDefault(q => q.TCID == ID);
                        if (M != null)
                        {
                            M.ActiveFlag = !M.ActiveFlag;
                            M.UpdDate = DT;
                            M.SaveACID = ACID;
                            DC.SubmitChanges();

                            Msg = "OK";
                        }
                        else
                            Msg = "查無此權限";
                    }
                    break;

            }
            return Msg;
        }
        #endregion
        #region 更新刪除狀態
        public string DataDelete(string TableName, int ID)
        {
            string Msg = "";
            ACID = GetACID();
            switch (TableName)
            {
                case "Product_Class":
                    {
                        var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == ID);
                        if (PC != null)
                        {
                            if (DC.Order_Product.Any(q => q.Order_Header.Order_Type > 0 && q.PCID == ID))
                                Msg = "此班級已有訂單,不能刪除";

                            if (Msg == "")
                            {
                                PC.ActiveFlag = false;
                                PC.DeleteFlag = true;
                                PC.UpdDate = DT;
                                PC.SaveACID = ACID;
                                DC.SubmitChanges();

                                var OPs = DC.Order_Product.Where(q => q.PCID == PC.PCID);
                                if (OPs.Count() > 0)
                                {
                                    DC.Order_Product.DeleteAllOnSubmit(OPs);
                                    DC.SubmitChanges();
                                }

                                Msg = "OK";
                            }
                        }
                        else
                            Msg = "查無此班級";
                    }
                    break;
            }
            return Msg;
        }
        #endregion
        #region 更新講師
        public string ChangeTeacher(int TID, int PCID)
        {
            ACID = GetACID();
            string Msg = "NG";

            //目前設定是只能有一位講師
            var MP = DC.M_Product_Teacher.FirstOrDefault(q => q.PCID == PCID);
            if (MP == null)
            {
                var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == PCID);
                if (PC != null)
                {
                    MP = new M_Product_Teacher
                    {
                        PID = PC.PID,
                        PCID = PC.PCID,
                        TID = TID,
                        Title = "",
                        Note = "",
                        CreDate = DT,
                        UpdDate = DT,
                        SaveACID = ACID
                    };
                    DC.M_Product_Teacher.InsertOnSubmit(MP);
                    DC.SubmitChanges();
                }
                else
                    Msg = "查無此班級";
            }
            else
            {
                if (MP.TID != TID)//替換講師
                {
                    MP.TID = TID;
                    MP.UpdDate = DT;
                    MP.SaveACID = ACID;
                    DC.SubmitChanges();
                }
                else//刪除講師但尚未指定新講師
                {
                    DC.M_Product_Teacher.DeleteOnSubmit(MP);
                    DC.SubmitChanges();
                }
            }

            return Msg;
        }
        #endregion
        #region 取得分類下課程選單
        [HttpGet]
        public string GetCourseCagegorySelect(int CCID, int CID = 0)
        {
            var Cs = from q in DC.Course.Where(q => q.CCID == CCID && q.CID != CID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.Title)
                     select new { value = q.CID, Text = q.Title };

            return JsonConvert.SerializeObject(Cs);
        }
        #endregion
        #region 取得課程內容
        private class cCourse
        {
            public int CID = 0;
            public string Title = "";
            public int CourseType = 0;
            public string CourseInfo = "";
            public string TargetInfo = "";
            public string GraduationInfo = "";
            public List<cCR> Roles = new List<cCR>();
        }
        public class cCR
        {
            public int CRID = 0;
            public int TargetType = 0;
            public int TargetInt1 = 0;
            public int TargetInt2 = 0;
            public string CC_Title = "";
            public string C_Title = "";
            public string Job_Title = "";
        }
        [HttpGet]
        public string GetCourse(string CID)
        {
            Error = "";
            var C = DC.Course.FirstOrDefault(q => q.CID.ToString() == CID && q.ActiveFlag && !q.DeleteFlag);
            if (C != null)
            {
                cCourse cC = new cCourse
                {
                    CID = C.CID,
                    Title = C.Title,
                    CourseType = C.CourseType,
                    CourseInfo = C.CourseInfo,
                    TargetInfo = C.TargetInfo,
                    GraduationInfo = C.GraduationInfo,
                    Roles = new List<cCR>()
                };

                var CRs = C.Course_Rule.OrderBy(q => q.TargetType);
                foreach (var CR in CRs)
                {
                    cCR c = new cCR();
                    c.CRID = CR.CRID;
                    c.TargetType = CR.TargetType;
                    c.TargetInt1 = CR.TargetInt1;
                    c.TargetInt2 = CR.TargetInt2;
                    switch (CR.TargetType)
                    {
                        case 0://先修課程ID[Course]
                            {
                                var C_ = DC.Course.FirstOrDefault(q => q.CID == CR.TargetInt1 && q.ActiveFlag && !q.DeleteFlag);
                                if (C_ != null)
                                {
                                    c.CC_Title = "【" + C_.Course_Category.Code + "】" + C_.Course_Category.Title;
                                    c.C_Title = C_.Title;
                                }
                            }
                            break;
                        case 1://職分ID[OID]
                            {
                                var O = DC.Organize.FirstOrDefault(q => q.OID == CR.TargetInt1);
                                if (O != null)
                                    c.Job_Title = O.JobTitle;
                            }
                            break;

                        case 4://事工團
                            {

                            }
                            break;
                        case 5://指定會員ACID
                            {

                            }
                            break;
                    }
                    cC.Roles.Add(c);
                }

                Error = JsonConvert.SerializeObject(cC);
            }
            else
                Error = "{\"Msg\"=\"課程不存在\"}";
            return Error;
        }
        #endregion
        #region 取得所選旌旗之上協會允許的交易方式
        public string GetPayType(int OIID)
        {
            string sPayType = "";
            var OI = (from q in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OIID == OIID)
                      join p in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 1)
                      on q.ParentID equals p.OIID
                      select p).FirstOrDefault();
            if (OI != null)
            {
                var PTs = DC.PayType.Where(q => q.OIID == OI.OIID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.PayTypeID);
                foreach (var PT in PTs)
                    sPayType += (sPayType == "" ? "" : ",") + PT.PayTypeID;
            }

            return sPayType;
        }
        #endregion
        #region 搜尋會員姓名
        public class cAC
        {
            public int ACID { get; set; }
            public string Name { get; set; }
            public string GroupName { get; set; }
        }
        /// <summary>
        /// 取得會員搜尋列表
        /// </summary>
        /// <param name="Name">姓名關鍵字</param>
        /// <param name="OIID">限制所屬旌旗</param>
        /// <param name="Type">0:成人會員/1:兒童會員/2:全職同工/3:成人會友/4:講師</param>
        /// <returns></returns>
        [HttpGet]
        public string GetAccountList(string Name, int OIID, int Type)
        {
            List<cAC> Ns = new List<cAC>();


            var ACs = DC.Account.Where(q => q.ActiveFlag && !q.DeleteFlag);
            if (Name != "")
                ACs = ACs.Where(q => (q.Name_First + q.Name_Last).Contains(Name));
            if (Type == 1)//小孩就年齡限制12歲(含)以下,不限小組
            {
                ACs = ACs.Where(q => DT.Year - q.Birthday.Year <= iChildAge && q.Birthday != q.CreDate);

                foreach (var AC in ACs.OrderBy(q => q.Name_First).ThenBy(q => q.Name_Last))
                {
                    cAC N = new cAC { ACID = AC.ACID, Name = AC.Name_First + AC.Name_Last, GroupName = "" };
                    //if (!BackendFlag)
                    //    N.Name = CutName(N.Name);
                    Ns.Add(N);
                }
            }
            else
            {
                if (OIID > 0)//隸屬哪個旌旗(兒童不查例外)
                {
                    ACs = from q in ACs
                          join p in DC.M_OI_Account.Where(q => q.ActiveFlag &&
                                                      !q.DeleteFlag &&
                                                      q.OrganizeInfo.ActiveFlag &&
                                                      !q.OrganizeInfo.DeleteFlag &&
                                                      q.OrganizeInfo.OI2_ID == OIID).GroupBy(q => q.ACID)
                          on q.ACID equals p.Key
                          select q;
                }
                if (Type == 2)//全職同工
                {
                    ACs = ACs.Where(q => q.BackUsedFlag);
                }
                else if (Type == 3)//成人會友(會友卡)
                {
                    ACs = from q in ACs
                          join p in DC.M_Role_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.RID == 2)
                          on q.ACID equals p.ACID
                          select q;
                }
                else if (Type == 4)//講師
                {
                    ACs = from q in ACs
                          join p in DC.Teacher.Where(q => q.ActiveFlag && !q.DeleteFlag)
                          on q.ACID equals p.ACID
                          select q;
                }

                //所屬小組資料
                var Ms_ = (from q in DC.M_OI_Account.Where(q => q.OrganizeInfo.OID == 8)
                           join p in ACs
                           on q.ACID equals p.ACID
                           select q).ToList();

                foreach (var AC in ACs.OrderBy(q => q.Name_First).ThenBy(q => q.Name_Last))
                {
                    cAC N = new cAC { ACID = AC.ACID, Name = AC.Name_First + AC.Name_Last, GroupName = "" };
                    var M_ACs = Ms_.Where(q => q.ACID == AC.ACID);
                    foreach (var M in M_ACs)
                        N.GroupName += (N.GroupName == "" ? "" : ",") + M.OrganizeInfo.Title + M.OrganizeInfo.Organize.Title;
                    Ns.Add(N);
                }
            }
            return JsonConvert.SerializeObject(Ns);
        }

        //取得全職同工列表
        [HttpGet]
        public string GetOI2UserList(string Name, int OIID)
        {
            List<cAC> Ns = new List<cAC>();

            var ACs = DC.Account.Where(q => q.ActiveFlag && !q.DeleteFlag);
            if (Name != "")
                ACs = ACs.Where(q => (q.Name_First + q.Name_Last).Contains(Name));
            if (OIID > 0)//隸屬哪個旌旗
            {
                ACs = (from q in DC.M_OI2_Account.Where(q => !q.DeleteFlag && q.ActiveFlag && (q.OIID == OIID || q.OIID == 1)).GroupBy(q => q.ACID).Select(q => q.Key)
                       join p in ACs
                       on q equals p.ACID
                       select p);
            }
            else
            {
                ACs = (from q in DC.M_OI2_Account.Where(q => !q.DeleteFlag && q.ActiveFlag).GroupBy(q => q.ACID).Select(q => q.Key)
                       join p in ACs
                       on q equals p.ACID
                       select p);
            }

            var Ms_ = (from q in DC.M_OI_Account.Where(q => q.OrganizeInfo.OID == 8)
                       join p in ACs
                       on q.ACID equals p.ACID
                       select q).ToList();
            var MOI2s = DC.M_OI2_Account.Where(q => !q.DeleteFlag && q.ActiveFlag && q.OIID == 1).ToList();
            foreach (var AC in ACs.OrderBy(q => q.Name_First).ThenBy(q => q.Name_Last))
            {
                cAC N = new cAC { ACID = AC.ACID, Name = AC.Name_First + AC.Name_Last, GroupName = "" };
                var M_ACs = Ms_.Where(q => q.ACID == AC.ACID);
                foreach (var M in M_ACs)
                    N.GroupName += (N.GroupName == "" ? "" : ",") + M.OrganizeInfo.Title + M.OrganizeInfo.Organize.Title;

                if (N.GroupName != "" || AC.ACID == 1 || (MOI2s.Any(q => q.ACID == AC.ACID)))//沒有小組的就不算,但Admin例外,全部旌旗的同工例外
                    Ns.Add(N);
            }

            return JsonConvert.SerializeObject(Ns);
        }
        #endregion
        #region 將多選的會員ID加入特定團體
        public class BasicResponse
        {
            public List<string> Messages = new List<string>();
        }
        [HttpGet]
        public string SaveACToTargetGroup(string IDs, string TargetTable, int ID1, int ID2)
        {
            BasicResponse R = new BasicResponse();
            ACID = GetACID();
            string[] sIDs = IDs.Split(',');
            List<int> ACIDs = new List<int>();
            for (int i = 0; i < sIDs.Length; i++)
            {
                int iACID = 0;
                if (int.TryParse(sIDs[i], out iACID))
                    ACIDs.Add(iACID);
            }

            if (ACIDs.Count == 0)
                R.Messages.Add("請選擇會員ID");
            if (string.IsNullOrEmpty(TargetTable))
                R.Messages.Add("請輸入要加入的目標");


            if (R.Messages.Count == 0)
            {

                switch (TargetTable)
                {
                    case "Staff"://加入事工團
                        {
                            //ID1=Staff->SID
                            //ID2=OrganizeInfo->OID=2
                            var S = DC.Staff.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.SID == ID1);
                            var ACs = from q in DC.Account.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList()
                                      join p in ACIDs
                                      on q.ACID equals p
                                      select q;
                            int AC_All_Ct = ACs.Count();
                            int AC_Child_Ct = ACs.Count(q => DT.Year - q.Birthday.Year <= iChildAge && q.Birthday != q.CreDate);
                            int AC_Aldult_Ct = AC_All_Ct - AC_Child_Ct;
                            if (S == null)
                                R.Messages.Add("此事工團已被關閉或被移除");
                            if (AC_All_Ct == 0)
                                R.Messages.Add("所選的會員均已關閉或刪除");
                            if (S != null && AC_All_Ct > 0)
                            {
                                //if (S.ChildrenFlag && AC_Child_Ct != AC_All_Ct)
                                //    R.Messages.Add("本事工團僅限兒童,名單中有成人,請移除後重試");
                                if (!S.ChildrenFlag && AC_Aldult_Ct != AC_All_Ct)
                                    R.Messages.Add("本事工團僅限成人,名單中有兒童,請移除後重試");
                            }

                            if (R.Messages.Count == 0)
                            {
                                foreach (int iACID in ACIDs)
                                {
                                    var M = DC.M_Staff_Account.FirstOrDefault(q => q.SID == ID1 && q.OIID == ID2 && q.ACID == iACID);
                                    if (M == null)
                                    {
                                        M = new M_Staff_Account
                                        {
                                            SID = ID1,
                                            OIID = ID2,
                                            ACID = iACID,
                                            LeaderFlag = false,
                                            JoinDate = DT,
                                            LeaveDate = DT,
                                            ActiveFlag = true,
                                            DeleteFlag = false,
                                            CreDate = DT,
                                            UpdDate = DT,
                                            SaveACID = ACID
                                        };
                                        DC.M_Staff_Account.InsertOnSubmit(M);
                                        DC.SubmitChanges();
                                    }
                                    else
                                    {
                                        if (M.DeleteFlag || !M.ActiveFlag)
                                        {
                                            M.ActiveFlag = true;
                                            M.DeleteFlag = false;
                                            M.UpdDate = DT;
                                            M.SaveACID = ACID;
                                            DC.SubmitChanges();
                                        }
                                    }
                                }
                            }

                        }
                        break;
                }
            }
            return JsonConvert.SerializeObject(R);
        }
        #endregion
        #region 事工團指定主責
        public string SetLeader(string IDs)
        {
            BasicResponse R = new BasicResponse();
            ACID = GetACID();
            string[] sIDs = IDs.Split(',');
            List<int> MIDs = new List<int>();
            for (int i = 0; i < sIDs.Length; i++)
            {
                int iMID = 0;
                if (int.TryParse(sIDs[i], out iMID))
                    MIDs.Add(iMID);
            }
            var Ms = (from q in DC.M_Staff_Account.Where(q => !q.DeleteFlag).ToList()
                      join p in MIDs
                      on q.MID equals p
                      select q).ToList();

            int SID = 0;
            if (Ms.Count > 0)
                SID = Ms[0].SID;

            var S = DC.Staff.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.SID == SID);
            if (Ms.Count() == 0)
                R.Messages.Add("請選擇會員ID");
            else if (S == null)
                R.Messages.Add("此事工團已被關閉或被移除");
            else if (!S.LeadersFlag && Ms.Count > 1)
                R.Messages.Add("此事工團只能指定一位主責");

            if (R.Messages.Count() == 0)
            {
                if (!S.LeadersFlag)//限一個主責
                {
                    //先通通移除主責權限
                    foreach (var M in S.M_Staff_Account.Where(q => q.LeaderFlag == true))
                    {
                        M.LeaderFlag = false;
                        M.UpdDate = DT;
                        M.SaveACID = ACID;
                        DC.SubmitChanges();
                    }
                }

                foreach (var M in Ms)
                {
                    if (!M.LeaderFlag)//有被選擇,但目前非主責
                    {
                        M.LeaderFlag = true;
                        M.UpdDate = DT;
                        M.SaveACID = ACID;
                        DC.SubmitChanges();
                    }
                }
            }
            return JsonConvert.SerializeObject(R);
        }
        #endregion
        #region 事工團移除主責
        public string RemoveLeader(string IDs)
        {
            BasicResponse R = new BasicResponse();
            ACID = GetACID();
            string[] sIDs = IDs.Split(',');
            List<int> MIDs = new List<int>();
            for (int i = 0; i < sIDs.Length; i++)
            {
                int iMID = 0;
                if (int.TryParse(sIDs[i], out iMID))
                    MIDs.Add(iMID);
            }
            var Ms = (from q in DC.M_Staff_Account.Where(q => !q.DeleteFlag).ToList()
                      join p in MIDs
                      on q.MID equals p
                      select q).ToList();

            int SID = 0;
            if (Ms.Count > 0)
                SID = Ms[0].SID;

            var S = DC.Staff.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.SID == SID);
            if (Ms.Count() == 0)
                R.Messages.Add("請選擇會員ID");
            else if (S == null)
                R.Messages.Add("此事工團已被關閉或被移除");
            else
            {
                var Ms_ = S.M_Staff_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.LeaderFlag).ToList();
                if (Ms_.Count() > 0)//目前已經有主責
                {
                    if (S.LeadersFlag)//允許多位主責,檢查移除後是否主責會歸0
                    {
                        var MIDs_ = Ms_.Select(q => q.MID).ToList();
                        var CutMIDs = MIDs_.Except(MIDs);//資料庫中的主責排除移除的主責後是否還有剩下的主責
                        if (CutMIDs.Count() == 0)
                            R.Messages.Add("請勿移除全部的主責");
                    }
                    else//只能一位主責,檢查移除的是不是就是這個人
                    {
                        if (Ms[0].MID == Ms_[0].MID)
                            R.Messages.Add("請直接指定接班主責,舊主責會同步移除權限");
                    }
                }
            }

            if (R.Messages.Count() == 0)
            {
                foreach (var M in Ms.Where(q => q.LeaderFlag))
                {
                    M.LeaderFlag = false;
                    M.UpdDate = DT;
                    M.SaveACID = ACID;
                    DC.SubmitChanges();
                }
            }
            return JsonConvert.SerializeObject(R);
        }
        #endregion
        #region 事工團移除團員
        public string RemoveACFromStaff(string IDs, int SID)
        {
            BasicResponse R = new BasicResponse();
            ACID = GetACID();
            string[] sIDs = IDs.Split(',');
            List<int> MIDs = new List<int>();
            for (int i = 0; i < sIDs.Length; i++)
            {
                int iMID = 0;
                if (int.TryParse(sIDs[i], out iMID))
                    MIDs.Add(iMID);
            }
            var Ms = (from q in DC.M_Staff_Account.Where(q => !q.DeleteFlag && q.SID == SID).ToList()
                      join p in MIDs
                      on q.MID equals p
                      select q).ToList();

            if (Ms.Count() == 0)
                R.Messages.Add("請選擇會員ID");
            if (Ms.Count(q => q.LeaderFlag) > 0)
                R.Messages.Add("所選名單包含主責,請先移除主責權限後再行移除");

            if (R.Messages.Count() == 0)
            {
                foreach (var MSA in Ms)
                {
                    MSA.LeaveDate = DT;
                    MSA.LeaderFlag = false;
                    MSA.ActiveFlag = false;
                    MSA.DeleteFlag = true;
                    MSA.UpdDate = DT;
                    MSA.SaveACID = ACID;
                    DC.SubmitChanges();
                }
            }

            return JsonConvert.SerializeObject(R);
        }
        #endregion
        #region 每日批次
        public string Batch_EveryDay()
        {
            Error = "OK";
            ChangeOrder();//未完成交易的商品塞回購物車
            return Error;
        }
        #endregion
        #region 測試用(檔案上傳)
        [HttpGet]
        public ActionResult Test()
        {
            GetViewBag();

            return View();
        }
        [HttpPost]
        public ActionResult Test(FormCollection FC, HttpPostedFileBase file_upload)
        {
            GetViewBag();

            bool CheckFileFlag = true;
            if (file_upload.ContentLength <= 0 || file_upload.ContentLength > 5242880)
                CheckFileFlag = false;
            else if (file_upload.ContentType != "image/png" && file_upload.ContentType != "image/jpeg")
                CheckFileFlag = false;
            if (CheckFileFlag)
            {
                string Ex = Path.GetExtension(file_upload.FileName);
                string FileName = $"{DT.ToString("yyyyMMddHHmmssfff")}{Ex}";
                string SavaPath = Path.Combine(Server.MapPath("~/Files/Product/"), FileName);
                file_upload.SaveAs(SavaPath);
            }


            return View();
        }
        #endregion
    }
}