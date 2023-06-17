using OfficeOpenXml.FormulaParsing.ExpressionGraph.FunctionCompilers;
using System;
using System.Collections.Generic;
using System.Linq;
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
        #region 登入
        public ActionResult Login()
        {
            GetViewBag();
            if (Request.Url.Host == "localhost")
                if (GetACID() > 0)
                    Response.Redirect("/Admin/Home/Index");
            
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
                Error += "請輸入檢查碼</br>";
            else if (ValidateCode != GetSession("VNum"))
                Error += "檢查碼輸入錯誤</br>";
            if (Error != "")
                TempData["msg"] = Error;
            else
            {
                string EncPW = HSM.Enc_1(PW);
                var AC = DC.Account.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.BackUsedFlag && q.Login == Login && q.Password == EncPW);
                if (AC == null)
                {
                    AC = DC.Account.Where(q => q.Login == Login).OrderByDescending(q => q.CreDate).FirstOrDefault();
                    if (AC == null)
                        SetAlert("此帳號不存在", 2);
                    else if (AC.DeleteFlag)
                        SetAlert("此帳號已被移除", 2);
                    else if (!AC.ActiveFlag)
                        SetAlert("此帳號已被關閉", 2);
                    else if (AC.Password != EncPW)
                    {
                        if (AC.Password == "")//資料庫中無密碼,所以依據此次輸入設定新密碼
                        {
                            AC.Password = EncPW;
                            DC.SubmitChanges();

                            SetBrowserData("ACID", AC.ACID.ToString());
                            SetBrowserData("UserName", AC.Name.ToString());
                            SetAlert(AC.Name + "歡迎回來", 1, "/Admin/Home/Index");
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
                                    SetAlert("您的密碼錯誤!!</br>您還有" + (5 - iLoginCt) + "次機會...");
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
                    else
                        SetAlert("此帳號發生不明原因錯誤</br>請管理員協助排除", 2, "/Admin/Home/Login");
                }
                else
                {
                    DelSession("LoginCt");
                    DelSession("LoginAccount");
                    LogInAC(AC.ACID);
                    SetBrowserData("UserName", AC.Name.ToString());
                    SetAlert(AC.Name + "歡迎回來", 1, "/Admin/Home/Index");
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
                    var AC = DC.Account.FirstOrDefault(q => !q.DeleteFlag && q.ActiveFlag && q.BackUsedFlag && q.Contect.FirstOrDefault(p => p.ContectValue == Email && p.ContectType == 2) != null);
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
                    var AC = DC.Account.FirstOrDefault(q => !q.DeleteFlag && q.ActiveFlag && q.BackUsedFlag && q.Contect.FirstOrDefault(p => p.ContectValue == CellPhone && p.ContectType == 1) != null);
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
                    var AC = DC.Account.FirstOrDefault(q => !q.DeleteFlag && q.ActiveFlag && q.BackUsedFlag && q.Contect.FirstOrDefault(p => p.ContectValue == Email && p.ContectType == 2) != null);
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
                    var AC = DC.Account.FirstOrDefault(q => !q.DeleteFlag && q.ActiveFlag && q.BackUsedFlag && q.Contect.FirstOrDefault(p => p.ContectValue == CellPhone && p.ContectType == 1) != null);
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
            if (GetACID() <= 0)
                SetAlert("您已登出", 1, "/Admin/Home/Index");

            return View();
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

        public string GetTreeToNext(string TargetTable, int ParentID, int SelectedID)
        {
            string sReturn = "";
            switch (TargetTable)
            {
                case "OrganizeInfo":
                    {
                        var OIs = DC.OrganizeInfo.Where(q => q.ParentID == ParentID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.Title).ToList();
                        var OI = OIs.FirstOrDefault(q => q.OIID == SelectedID);
                        for (int i = 0; i < OIs.Count; i++)
                        {
                            if ((OI == null && i == 0) || OI != null && OIs[i].OIID == OI.OIID)
                                sReturn += (sReturn == "" ? "" : ",") + "{'Text'='" + OIs[i].Title + "','Value'=" + OIs[i].OIID + ",'Selected'='T'}";
                            else
                                sReturn += (sReturn == "" ? "" : ",") + "{'Text'='" + OIs[i].Title + "','Value'=" + OIs[i].OIID + ",'Selected'='F'}";
                        }
                    }
                    break;
            }
            return "[" + sReturn + "]";
        }
    }
}