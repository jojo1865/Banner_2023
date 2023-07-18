using Newtonsoft.Json;
using OfficeOpenXml.FormulaParsing.ExpressionGraph.FunctionCompilers;
using System;
using System.Collections.Generic;
using System.Data;
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
            if(AC!=null)
            {
                SetBrowserData("ACID", AC.ACID.ToString());
                SetBrowserData("UserName", AC.Name.ToString());
                Response.Redirect("/Admin/Home/Index");
            }
            
        }
        #endregion
        #region 登入
        public ActionResult Login()
        {
            GetViewBag();
            /*if (Request.Url.Host == "localhost" && Request.Url.Port == 44307)
            {
                if (GetACID() <= 0)
                {
                    LogInAC(1);
                    SetBrowserData("UserName", "系統管理者");
                }
                Response.Redirect("/Admin/Home/Index");
            }
            else if(Request.Url.Host == "web-banner.viuto-aiot.com")
                Response.Redirect("/Web/Home/Index");
            */
            return View();
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(FormCollection FC)
        {
            GetViewBag();

            string Login = FC.Get("txb_Login");
            string PW = FC.Get("txb_Password");
            string ValidateCode = FC.Get("txb_ValidateCode");

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
                            SetBrowserData("UserName", AC.Name.ToString());
                            SetAlert(AC.Name + " 歡迎回來", 1, "/Admin/Home/Index");
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
                    DelSession("LoginCt");
                    DelSession("LoginAccount");
                    LogInAC(AC.ACID);
                    SetBrowserData("UserName", AC.Name.ToString());
                    SetAlert(AC.Name + " 歡迎回來", 1, "/Admin/Home/Index");
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
        [ValidateAntiForgeryToken]
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
                        SendMail(Email, AC.Name, "【全球旌旗資訊網】忘記密碼通知信", string.Format(MailData, PW.ToString()));
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
        [ValidateAntiForgeryToken]
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
                        SendMail(Email, AC.Name, "【全球旌旗資訊網】忘記帳號通知信", string.Format(MailData, AC.Login));
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
        [ValidateAntiForgeryToken]
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
            var OIs = from q in DC.OrganizeInfo.Where(q => q.ParentID == OIID && q.ActiveFlag).OrderBy(q => q.Title)
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
        public string ChangeActive(string TableName,int ID)
        {
            string Msg = "NG";
            switch(TableName)
            {
                case "OrganizeInfo":
                    {
                        var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == ID);
                        if (OI != null)
                        {
                            OI.ActiveFlag = !OI.ActiveFlag;
                            OI.UpdDate = DT;
                            OI.SaveACID = GetACID();
                            DC.SubmitChanges();

                            Msg = "OK";
                        }
                        else
                            Msg = "查無此組織";
                    }
                    break;
            }
            return Msg;
        }
        #endregion
        #region 新增資料
        public void SetAC()
        {
            DateTime DT_ = Convert.ToDateTime("2023/7/1");
            /*for (int i = 1; i < 4; i++)
            {
                var O = DC.Organize.FirstOrDefault(q => q.OID == 8);
                var R = DC.Rool.FirstOrDefault(q => q.RID == 13);
                string sLogin = "Test" + O.OID.ToString().PadLeft(2, '0') + i.ToString().PadLeft(2, '0');
                string Phone = "0912345678";
                Account AC = new Account
                {
                    Login = sLogin,
                    Password = HSM.Enc_1(sLogin),
                    Name = O.JobTitle + i,
                    ManFlag = i % 2 == 0,
                    IDNumber = "",
                    IDType = 0,
                    Birthday = DT_.AddYears(-23),
                    EducationType = 0,
                    JobType = 0,
                    MarriageType = 0,
                    MarriageNote = "",
                    BaptizedType = 0,
                    GroupType = "",
                    BackUsedFlag = false,
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT_,
                    UpdDate = DT_,
                    SaveACID = 1,
                    OldID = 0
                };
                DC.Account.InsertOnSubmit(AC);
                DC.SubmitChanges();

                while (O != null)
                {
                    M_O_Account M = new M_O_Account
                    {
                        Organize = O,
                        Account = AC,
                        ActiveFlag = true,
                        DeleteFlag = false,
                        CreDate = DT_.AddDays(-1 * O.OID),
                        UpdDate = DT_.AddDays(-1 * O.OID),
                        SaveACID = 1
                    };
                    DC.M_O_Account.InsertOnSubmit(M);
                    DC.SubmitChanges();
                    O = DC.Organize.FirstOrDefault(q => q.ParentID == O.OID);
                }

                Contect Con = new Contect
                {
                    TargetType = 2,
                    TargetID = AC.ACID,
                    ZID = 10,
                    ContectType = 1,
                    ContectValue = Phone
                };
                DC.Contect.InsertOnSubmit(Con);
                DC.SubmitChanges();

                M_Rool_Account MR = new M_Rool_Account
                {
                    Account = AC,
                    Rool = R,
                    JoinDate = DT_,
                    LeaveDate = DT_,
                    Note = "",
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT_,
                    UpdDate = DT_,
                    SaveACID = 1
                };
                DC.M_Rool_Account.InsertOnSubmit(MR);
                DC.SubmitChanges();

                //小組長
                var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == 1982 + i);
                if(OI!=null)
                {
                    OI.Account = AC;
                    DC.SubmitChanges();
                }
            }*/
            for (int i = 1; i < 6; i++)
            {
                var R = DC.Rool.FirstOrDefault(q => q.RID == 1);
                string sLogin = "User00" + i.ToString().PadLeft(2, '0');
                string Phone = "0912345678";
                Account AC = new Account
                {
                    Login = sLogin,
                    Password = HSM.Enc_1(sLogin),
                    Name = "會員" + i,
                    ManFlag = i % 2 == 0,
                    IDNumber = "",
                    IDType = 0,
                    Birthday = DT_.AddYears(-23),
                    EducationType = 0,
                    JobType = 0,
                    MarriageType = 0,
                    MarriageNote = "",
                    BaptizedType = 0,
                    GroupType = "有意願",
                    BackUsedFlag = false,
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT_,
                    UpdDate = DT_,
                    SaveACID = 1,
                    OldID = 0
                };
                DC.Account.InsertOnSubmit(AC);
                DC.SubmitChanges();

                Contect Con = new Contect
                {
                    TargetType = 2,
                    TargetID = AC.ACID,
                    ZID = 10,
                    ContectType = 1,
                    ContectValue = Phone
                };
                DC.Contect.InsertOnSubmit(Con);
                DC.SubmitChanges();

                M_Rool_Account MR = new M_Rool_Account
                {
                    Account = AC,
                    Rool = R,
                    JoinDate = DT_,
                    LeaveDate = DT_,
                    Note = "",
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT_,
                    UpdDate = DT_,
                    SaveACID = 1
                };
                DC.M_Rool_Account.InsertOnSubmit(MR);
                DC.SubmitChanges();
            }
        }
        #endregion
    }
}