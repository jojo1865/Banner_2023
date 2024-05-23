﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Security.Cryptography;
using System.Web.Helpers;
using System.Drawing;
using static Banner.Areas.Admin.Controllers.HomeController;
using Banner.Models;

namespace Banner.Areas.Web.Controllers
{
    public class HomeController : PublicClass
    {
        // GET: Web/Home
        public ActionResult Index()
        {
            GetViewBag();
            if (GetACID() <= 0)
                Response.Redirect("/Web/Home/Login");
            else
            Response.Redirect("/Web/AccountPlace/Index");

            //Response.Write("2023/10/22" + " = "+ (int)((Convert.ToDateTime("2023/10/22")).DayOfWeek));
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
                Response.Redirect("/Web/Home/Index");
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
                    //SetBrowserData("UserName", "系統管理員");

                    //LogInAC(443);
                    //SetBrowserData("UserName", "江晨旭");

                    //測推播
                    LogInAC(2213);
                    SetBrowserData("UserName", "劉冠廷");

                    //測試事工團主責
                    //LogInAC(8197);
                    //SetBrowserData("UserName", "JOJO");

                    //測試帶職主責
                    //LogInAC(6741);
                    //SetBrowserData("UserName", "何張森妹");

                    //測試區長功能
                    //LogInAC(746);
                    //SetBrowserData("UserName", "柯佳慧");

                    //LogInAC(1511);
                    //SetBrowserData("UserName", "莊懷德");

                    Response.Redirect("/Web/Home/Index");
                }
            }
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
            //string ss = GetSession("VNum");
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
                var AC = DC.Account.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && (q.Login == Login || q.IDNumber == Login) && (q.Password == EncPW || q.Password == PW));
                if (AC == null)
                {
                    AC = DC.Account.Where(q => (q.Login == Login || q.IDNumber == Login) && q.ActiveFlag && !q.DeleteFlag).OrderByDescending(q => q.CreDate).FirstOrDefault();
                    if (AC == null)
                        SetAlert("此帳號/身分證字號不存在", 2);
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
                            SetAlert(AC.Name_First + AC.Name_Last + " 歡迎回來", 1, "/Web/Home/Index");
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
                                    SetAlert("您的密碼錯誤!!</br>您還有" + (5 - iLoginCt) + "次機會...</br>建議您使用忘記密碼功能", 2, "/Web/Home/ForgetPassWord/");
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
                        SetAlert("此帳號不存在", 2);
                }
                else
                {
                    if (AC.Password == PW)
                    {
                        AC.Password = EncPW;
                        DC.SubmitChanges();
                    }

                    DelSession("LoginCt");
                    DelSession("LoginAccount");
                    LogInAC(AC.ACID);
                    SetBrowserData("UserName", AC.Name_First + AC.Name_Last);
                    SetAlert(AC.Name_First + AC.Name_Last + " 歡迎回來", 1, "/Web/Home/Index");
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
            string Login = FC.Get("txb_Login");
            string IDNumber = FC.Get("txb_IDNumber");

            var AC = DC.Account.FirstOrDefault(q => !q.DeleteFlag && q.ActiveFlag && (q.Login == Login || q.IDNumber == IDNumber));
            if (AC != null)
            {
                int PW = GetRand(1000000);


                //先發Email
                var Con_Mail = DC.Contect.FirstOrDefault(q => q.ContectType == 2 && q.TargetType == 2 && q.TargetID == AC.ACID);
                var Con_Phone = DC.Contect.FirstOrDefault(q => q.ContectType == 1 && q.TargetType == 2 && q.TargetID == AC.ACID);
                int ConSndType = 0;
                if (Con_Mail != null)
                    if (CheckEmail(Con_Mail.ContectValue))
                        ConSndType = 2;

                if (Con_Phone != null && ConSndType == 0)
                    if (CheckCellPhone(Con_Phone.ContectValue))
                        ConSndType = 1;

                if (ConSndType == 1)//可發簡訊
                {
                    AC.Password = HSM.Enc_1(PW.ToString());
                    AC.UpdDate = DT;
                    DC.SubmitChanges();

                    SendSNS(Con_Phone.ContectValue, "【全球旌旗資訊網】忘記密碼通知簡訊", "親愛的旌旗家人,您的新密碼為：" + PW.ToString() + ",請立即登入，即可修改密碼。");
                }
                else if (ConSndType == 2)//可發Mail
                {
                    AC.Password = HSM.Enc_1(PW.ToString());
                    AC.UpdDate = DT;
                    DC.SubmitChanges();

                    string MailData = "親愛的旌旗家人 您好：</br></br>" +
                            "這裡是【全球旌旗資訊網】自動發信系統</br></br>" +
                            "您的新密碼為：{0}</br>" +
                            "請立即登入，即可修改密碼。</br></br>" +
                            "如需有任何問題，請寫信至itsupport@wwbch.org</br></br>" +
                            "旌旗教會 敬上";
                    SendMail(Con_Mail.ContectValue, AC.Name_First + AC.Name_Last, "【全球旌旗資訊網】忘記密碼通知信", string.Format(MailData, PW.ToString()));
                    SetAlert("您的新密碼已發送,請查看您的信箱:" + CutMail(Con_Mail.ContectValue), 1, "/Web/Home/Login");
                }
                else
                    SetAlert("此帳號的Email或手機資料有問題,無法查詢密碼", 3);
            }
            else
                SetAlert("查無帳號,請重新輸入", 3);
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
                        SetAlert("您的帳號已發送,請查看您的信箱", 1, "/Web/Home/Login");
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
                        SetAlert("您的帳號已發送,請查看您的簡訊", 1, "/Web/Home/Login");
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
            SetAlert("您已登出", 1, "/Web/Home/Login");

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
        #region 取得下一層組織選單
        [HttpGet]
        public string GetOISelect(int OIID)
        {
            var OIs = from q in DC.OrganizeInfo.Where(q => q.ParentID == OIID && q.ActiveFlag).OrderBy(q => q.Title)
                      select new { value = q.OIID, Text = q.Title };

            return JsonConvert.SerializeObject(OIs);
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
        #region 發認證簡訊
        public string SendSSN()
        {
            string ZID = GetQueryStringInString("ZID");
            string PhoneNo = GetQueryStringInString("PhoneNo");
            var Z = DC.ZipCode.FirstOrDefault(q => q.ZID.ToString() == ZID);
            string CellPhone = (ZID == "10" ? "" : (Z != null ? Z.Code.Replace(" ", "") : "") + " ") + PhoneNo.Replace(" ", "");

            string CheckCode = GetRand(1000000).ToString().PadLeft(6, '0');
            Error = SendSNS(CellPhone, "【全球旌旗資訊網】手機驗證簡訊", "親愛的旌旗家人,您的驗證碼為：" + CheckCode + "。");

            return (Z != null ? Z.Code : "") + " " + PhoneNo.Substring(0, 4) + new string('*', PhoneNo.Length - 4) + ";" + CheckCode;
        }
        #endregion
        #region 發認證信
        public string SendMailCode()
        {
            string Email = GetQueryStringInString("email");
            string CheckCode = GetRand(1000000).ToString().PadLeft(6, '0');
            Error = SendMail(Email, Email, "【全球旌旗資訊網】Email認證", "親愛的旌旗家人,您的驗證碼為：" + CheckCode + "。");

            return Email.Split('@')[0].Substring(0, 4) + new string('*', 5) + Email.Split('@')[1] + ";" + CheckCode;
        }
        #endregion
        #region 確認婚姻關係
        [HttpGet]
        public ActionResult CheckWedding()
        {
            GetViewBag();
            string ID1 = HSM.Des_1(GetQueryStringInString("ID1"));
            string ID2 = HSM.Des_1(GetQueryStringInString("ID2"));
            var AC1 = DC.Account.FirstOrDefault(q => q.ACID.ToString() == ID1 && !q.DeleteFlag);//送出配對要求者
            var AC2 = DC.Account.FirstOrDefault(q => q.ACID.ToString() == ID2 && !q.DeleteFlag);//被配對者
            if (AC1 != null && AC2 != null)
            {
                //先把送出者跟被配對者組合
                var F = DC.Family.FirstOrDefault(q => q.ACID == AC1.ACID && q.FamilyType == 2 && q.SortNo == -1 && !q.DeleteFlag && q.IDNumber == AC2.IDNumber);
                if (F == null)
                {
                    F = new Family
                    {
                        Account = AC1,
                        Name = AC2.Name_First + AC2.Name_Last,
                        IDNumber = AC2.IDNumber,
                        Login = AC2.Login,
                        FamilyType = 2,
                        FamilyTitle = "配偶",
                        TargetACID = AC2.ACID,
                        SortNo = 0,
                        DeleteFlag = false
                    };
                    DC.Family.InsertOnSubmit(F);
                    DC.SubmitChanges();
                }
                else
                {
                    F.TargetACID = AC2.ACID;
                    F.SortNo = 0;
                    F.DeleteFlag = false;
                    DC.SubmitChanges();
                }
                //再把被配對者與送出者組合
                F = DC.Family.FirstOrDefault(q => q.ACID == AC2.ACID && q.FamilyType == 2 && q.SortNo == -1 && !q.DeleteFlag && q.IDNumber == AC1.IDNumber);
                if (F == null)
                {
                    F = new Family
                    {
                        Account = AC2,
                        Name = AC1.Name_First + AC1.Name_Last,
                        IDNumber = AC1.IDNumber,
                        Login = AC1.Login,
                        FamilyType = 2,
                        FamilyTitle = "配偶",
                        TargetACID = AC1.ACID,
                        SortNo = 0,
                        DeleteFlag = false
                    };
                    DC.Family.InsertOnSubmit(F);
                    DC.SubmitChanges();
                }
                else
                {
                    F.TargetACID = AC1.ACID;
                    F.SortNo = 0;
                    F.DeleteFlag = false;
                    DC.SubmitChanges();
                }

                SetAlert("確認完成", 1, "/Web/Home/Login");
            }
            else
                SetAlert("確認失敗:其中一方帳戶不存在", 2, "/Web/Home/Login");
            return View();
        }
        #endregion
        #region 加入購物車
        private class cPID
        {
            public int PID = 0;
        }
        public string SendCart()
        {
            int ACID = GetQueryStringInInt("ACID");
            int PID = GetQueryStringInInt("PID");
            int PCID = 0;
            Error = "";
            if (ACID <= 0)
                Error += "無登入會員資料<br/>";
            else if (PID <= 0)
                Error += "無課程資料<br/>";
            else
            {
                var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID);
                var P = DC.Product.FirstOrDefault(q => q.PID == PID);
                #region 基本檢查
                if (AC != null)
                {
                    if (AC.DeleteFlag)
                        Error += "無會員資料<br/>";
                    else if (!AC.ActiveFlag)
                        Error += "會員資料未啟用<br/>";
                }
                else
                    Error += "無會員資料<br/>";
                if (Error == "")
                {
                    if (P != null)
                    {
                        if (P.DeleteFlag)
                            Error += "無課程資料<br/>";
                        else if (!P.ActiveFlag)
                            Error += "課程並未允許交易<br/>";
                        else
                        {
                            var OP = DC.Order_Product.FirstOrDefault(q =>
                            q.Order_Header.ACID == ACID &&
                            !q.Order_Header.DeleteFlag &&
                            q.PID == P.PID
                            );
                            if (OP != null)
                            {
                                if (OP.Order_Header.Order_Type == 0)
                                    Error += "此課程已在購物車中<br/>";
                                else if (OP.Order_Header.Order_Type == 1)
                                    Error += "此課程正在結帳中...<br/>";
                                else if (OP.Order_Header.Order_Type == 2)
                                    Error += "此課程您已經買過了<br/>";
                            }
                            if (Error == "")
                            {
                                //人數限制檢查
                                //有買這個商品且結帳完成的正常訂單,統計每個班級的人數
                                var OP_Gs = (from q in DC.Order_Product.Where(q => !q.Order_Header.DeleteFlag && q.Order_Header.Order_Type == 2 && q.PID == PID)
                                             group q by new { q.PCID } into g
                                             select new { g.Key.PCID, Ct = g.Count() }).ToList();

                                var PCs = DC.Product_Class.Where(q => q.PID == PID && !q.DeleteFlag && q.Product_ClassTime.Count() > 0).OrderBy(q => q.PCID).ToList();
                                if (PCs.Count() > 0)//有班級可以上
                                {
                                    //先檢查限制名額的
                                    var PC_Ns = PCs.Where(q => q.PeopleCt > 0 && q.Product_ClassTime.Count > 0).OrderBy(q => q.Product_ClassTime.Min(p => p.ClassDate)).ToList();
                                    if (OP_Gs.Count > 0)//已有相關訂單
                                    {
                                        foreach (var PC_N in PC_Ns)
                                        {
                                            var OP_G = OP_Gs.FirstOrDefault(q => q.PCID == PC_N.PCID);
                                            if (OP_G != null)//有限制
                                            {
                                                if (PC_N.PeopleCt < OP_G.Ct)
                                                    PCID = OP_G.PCID;
                                            }
                                            else
                                                PCID = PC_N.PCID;

                                            if (PCID > 0)
                                                break;
                                        }
                                    }
                                    else//目前沒人訂課
                                    {
                                        if (PC_Ns.Count > 0)
                                        {
                                            PCID = PC_Ns.First().PCID;
                                        }
                                    }

                                    if (PCID == 0)//沒班級再檢查不限名額的
                                    {
                                        var PCT_0s = from q in PCs.Where(q => q.PeopleCt == 0).ToList()
                                                     join p in DC.Product_ClassTime.Where(q => q.Product_Class.PID == PID).ToList()
                                                     on q.PCID equals p.PCID
                                                     select p;
                                        if (PCT_0s.Count() > 0)
                                            PCID = PCT_0s.OrderBy(q => q.ClassDate).First().PCID;

                                    }

                                    if (PCID == 0)
                                        Error += "目前班級均已額滿<br/>";
                                }
                                else
                                    Error += "目前沒有班級可以上課<br/>";
                            }


                        }
                    }
                    else
                        Error += "無課程資料<br/>";
                }
                #endregion
                #region 購買資格檢查

                if (Error == "")
                {
                    //有線上報名日期
                    if (P.SDate_Signup.Date >= P.CreDate.Date && P.SDate_Signup.Date > DT.Date)
                        Error += "本課程尚未開始線上報名<br/>";
                    else if (P.EDate_Signup.Date >= P.CreDate.Date && P.EDate_Signup.Date < DT.Date)
                        Error += "本課程已結束線上報名<br/>";
                    else
                    {
                        bool bCheck0 = false;//先修課程檢查過沒?
                        bool bCheck1 = false;//職分檢查過沒?
                        //有購買限制規則
                        foreach (var R in P.Product_Rule.OrderBy(q => q.TargetType))
                        {
                            switch (R.TargetType)
                            {
                                case 0://先修課程ID[Course]
                                    {
                                        if (!bCheck0)
                                        {
                                            var Os = from q in DC.Order_Product.Where(q => q.Order_Header.ACID == AC.ACID && q.Order_Header.Order_Type == 2 && !q.Order_Header.DeleteFlag)
                                                     join p in DC.Product_Rule.Where(q => q.PID == P.PID && q.TargetType == 0 && q.TargetInt1 > 0)
                                                     on q.PID equals p.TargetInt1
                                                     select p;
                                            if (Os.Count() == 0)
                                                Error += "本課程有限制需先參加先修課程,您不具備此資格<br/>";
                                            bCheck0 = true;
                                        }
                                    }
                                    break;

                                case 1://職分ID[OID]
                                    {
                                        if (!bCheck1)
                                        {
                                            var Os = from q in DC.M_O_Account.Where(q => q.ACID == AC.ACID && !q.DeleteFlag && q.ActiveFlag)
                                                     join p in DC.Product_Rule.Where(q => q.PID == P.PID && q.TargetType == 1 && q.TargetInt1 > 0)
                                                     on q.OID equals p.TargetInt1
                                                     select p;
                                            if (Os.Count() == 0)
                                                Error += "本課程有限制指定職分參加,您不具備職分資格<br/>";
                                            bCheck1 = true;
                                        }
                                    }
                                    break;

                                case 2://性別
                                    {
                                        if (R.TargetInt1 >= 0)//有限制
                                        {
                                            if (AC.ManFlag && R.TargetInt1 == 0)
                                                Error += "本課程限制女性參加<br/>";
                                            if (!AC.ManFlag && R.TargetInt1 == 1)
                                                Error += "本課程限制男性參加<br/>";
                                        }
                                    }
                                    break;

                                case 3://年齡
                                    {
                                        if (AC.Birthday.Date != AC.CreDate.Date)//會友有生日資料
                                        {
                                            if (R.TargetInt1 > 0)//有最小年齡限制
                                            {
                                                if ((DT.Year - AC.Birthday.Year) < R.TargetInt1)//今年-生日年<最小年齡限制
                                                    Error += "您的年紀不符最小年齡限制<br/>";
                                            }
                                            else if (R.TargetInt2 > 0)//有最大年齡限制
                                            {
                                                if ((DT.Year - AC.Birthday.Year) > R.TargetInt1)//今年-生日年>最大年齡限制
                                                    Error += "您的年紀不符最大年齡限制<br/>";
                                            }
                                        }

                                    }
                                    break;

                                case 4://事工團
                                    break;

                                case 5://指定會員ACID
                                    if (AC.ACID != R.TargetInt1)
                                        Error += "您非本課程指定會員<br/>";
                                    break;
                            }
                        }
                    }
                }
                if (Error == "")
                {
                    if (ACID != 1)//非管理者
                    {
                        var OICheck = DC.M_OI2_Account.Where(q => q.ACID == ACID && q.OIID == P.OIID);
                        if (OICheck == null)//本課程限制的旌旗與報名者不符
                            Error += "本課程只允許特定旌旗教會下的會友報名<br/>";
                        else
                        {
                            /*var PTs = from q in DC.PayType.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OIID == P.OrganizeInfo.ParentID)
                                      join p in DC.M_Product_PayType.Where(q => q.ActiveFlag && q.PID == P.PID)
                                      on q.PTID equals p.PTID
                                      select p;
                            if (PTs.Count() == 0)
                                Error += "本課程所屬協會並未設定付款方式,因此不能加入購物車<br/>";*/
                        }
                    }
                }
                #endregion
                if (Error == "")
                {
                    int[] iPrice = GetPrice(ACID, P);
                    var OH = DC.Order_Header.FirstOrDefault(q => q.Order_Type == 0 && q.ACID == ACID);
                    if (OH != null)
                    {
                        if (!OH.Order_Product.Any(q => q.PID == PID))//這商品目前尚未加入購物車中
                        {
                            var PC = DC.Product_Class.FirstOrDefault(q => q.PID == PID && q.PCID == PCID);
                            Order_Product OP = new Order_Product
                            {
                                Order_Header = OH,
                                Product = P,
                                PCID = PC != null ? PC.PCID : 0,
                                CRID = iPrice[3],
                                Price_Basic = P.Price_Basic,
                                Price_Finally = iPrice[1],
                                Price_Type = iPrice[0],
                                Graduation_Flag = false,
                                Graduation_ACID = 0,
                                Graduation_Date = DT,
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = ACID
                            };
                            DC.Order_Product.InsertOnSubmit(OP);
                            DC.SubmitChanges();

                            OH.TotalPrice = OH.Order_Product.Sum(q => q.Price_Finally);
                            OH.DeleteFlag = false;
                            DC.SubmitChanges();
                        }
                    }
                    else
                    {
                        var PC = DC.Product_Class.Where(q => q.PID == PID).OrderBy(q => q.PCID).FirstOrDefault();
                        OH = new Order_Header
                        {
                            OIID = 0,
                            ACID = ACID,
                            Order_Type = 0,
                            TotalPrice = iPrice[1],
                            DeleteFlag = false,
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = ACID
                        };
                        DC.Order_Header.InsertOnSubmit(OH);
                        DC.SubmitChanges();

                        string sNote = "";
                        if (iPrice[2] > 0)
                        {
                            var CR = DC.Coupon_Rule.FirstOrDefault(q => q.CHID == iPrice[2]);
                            sNote = GetCouponNote(CR);
                        }
                        Order_Product OP = new Order_Product
                        {
                            OHID = OH.OHID,
                            PID = P.PID,
                            PCID = PC != null ? PC.PCID : 0,
                            CRID = iPrice[3],
                            Price_Basic = P.Price_Basic,
                            Price_Finally = iPrice[1],
                            Price_Type = iPrice[0],
                            Price_Note = sNote,
                            Graduation_Flag = false,
                            Graduation_ACID = 0,
                            Graduation_Date = DT,
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = ACID
                        };
                        DC.Order_Product.InsertOnSubmit(OP);
                        DC.SubmitChanges();
                    }

                }
            }

            if (Error != "")
                Error = "Error;" + Error;
            else
                Error = "OK;";
            return Error;
        }
        #endregion
        #region 自購物車移除
        public string RemoveCart(int OPID)
        {
            var OP = DC.Order_Product.FirstOrDefault(q => q.OPID == OPID && q.Order_Header.Order_Type == 0);
            if (OP != null)
            {
                var OHID = OP.OHID;
                DC.Order_Product.DeleteOnSubmit(OP);
                DC.SubmitChanges();

                var OH = DC.Order_Header.FirstOrDefault(q => q.OHID == OHID && q.Order_Type == 0);
                if (OH != null)
                {
                    var OPs = DC.Order_Product.Where(q => q.OHID == OHID);
                    OH.TotalPrice = OPs.Count() > 0 ? OPs.Sum(q => q.Price_Finally) : 0;
                    OH.UpdDate = DT;
                    OH.DeleteFlag = OPs.Count() == 0;
                    DC.SubmitChanges();
                }
            }
            else
                Error = "此課程不存在";

            if (Error != "")
                Error = "Error;" + Error;
            else
                Error = "OK;";
            return Error;
        }

        #endregion
        #region 每日批次
        public string Batch_EveryDay()
        {
            
            
            Error = "OK";
            //7天未落戶改回新人(有意願),並可重新申請加入小組
            ChangeOIAccount();//退回未落戶者

            //信用卡3天未付款移除訂單
            //ATM7天未付款移除訂單
            ChangeOrder();//未完成交易的商品塞回購物車

            //建立當日活動
            CreageEvent();

            return Error;
        }
        #endregion
        #region 每分鐘批次
        public string Batch_EveryMin()
        {
            
            Error = "OK";
            //一般推撥
            SendMessage();

            //加入小組後推撥
            //直接推
            return Error;
        }
        #endregion
        #region 前台搜尋會員列表

        /// <summary>
        /// 取得會員搜尋列表
        /// </summary>
        /// <param name="Name">姓名關鍵字</param>
        /// <param name="OIID">限制所屬旌旗</param>
        /// <param name="Type">-1:不限制/0:成人會員/1:兒童會員/2:全職同工/3:成人會友/4:講師</param>
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
                    var OI = DC.M_OI_Account.FirstOrDefault(q => q.ACID == AC.ACID && q.ActiveFlag && !q.DeleteFlag && q.OrganizeInfo.OID == 8);
                    cAC N = new cAC { ACID = AC.ACID, Name = AC.Name_First + AC.Name_Last, GroupName = (OI != null ? OI.OrganizeInfo.Title + OI.OrganizeInfo.Organize.Title : ""), Child = (CheckChild(AC.Birthday) ? "兒童" : "成人") };
                    //if (!BackendFlag)
                    //    N.Name = CutName(N.Name);
                    Ns.Add(N);
                }
            }
            else
            {
                if (Type == -1)
                {
                    var Ls = new List<OrganizeInfo>();
                    var MOIs = from q in GetThisOIsFromTree(ref Ls, OIID).Where(q => q.OID == 8).ToList()
                               join p in DC.M_OI_Account.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList()
                               on q.OIID equals p.OIID
                               select p;
                    ACs = from q in MOIs.GroupBy(q => q.ACID).ToList().AsQueryable()
                          join p in ACs
                          on q.Key equals p.ACID
                          select p;
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
                var Ms_ = (from q in DC.M_OI_Account.Where(q => q.OrganizeInfo.OID == 8).ToList()
                           join p in ACs.ToList()
                           on q.ACID equals p.ACID
                           select q).ToList();

                foreach (var AC in ACs.OrderBy(q => q.Name_First).ThenBy(q => q.Name_Last))
                {
                    cAC N = new cAC { ACID = AC.ACID, Name = AC.Name_First + AC.Name_Last, GroupName = "", Child = "" };
                    var M_ACs = Ms_.Where(q => q.ACID == AC.ACID);
                    foreach (var M in M_ACs)
                        N.GroupName += (N.GroupName == "" ? "" : ",") + M.OrganizeInfo.Title + M.OrganizeInfo.Organize.Title;
                    N.Child = CheckChild(AC.Birthday) ? "兒童" : "成人";
                    Ns.Add(N);
                }
            }
            return JsonConvert.SerializeObject(Ns);
        }
        #endregion
        #region 前臺主管管轄的小組清單(人換組)
        [HttpGet]
        public string GetACChangeOIList(string KeyTitle, int OID)
        {
            ACID = GetACID();
            string sReturn = "";
            if (ACID == 1)
            {
                var OIs = (from q in DC.OrganizeInfo.Where(q => q.OID == OID && q.ActiveFlag && !q.DeleteFlag && (string.IsNullOrEmpty(KeyTitle) ? true : q.Title.Contains(KeyTitle)))
                           select new { value = q.OIID, Text = q.Title + q.Organize.Title + (q.BusinessType == 1 ? "(外展)" : "") }).OrderBy(q => q.Text);

                sReturn = JsonConvert.SerializeObject(OIs);
            }
            else
            {
                var OS = GetO();
                var MyOIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID).ToList();
                var Gs = from q in OS
                         join p in MyOIs
                         on q.OID equals p.OID
                         select new { q.SortNo, OI = p };
                OrganizeInfo MyOI = new OrganizeInfo();
                if (Gs.Count() > 0)
                    MyOI = Gs.OrderBy(q => q.SortNo).First().OI;

                if (MyOI.OIID > 0)
                {
                    List<OrganizeInfo> OIs = new List<OrganizeInfo>();
                    OIs = GetThisOIsFromTree(ref OIs, MyOI.OIID);

                    OIs = OIs.Where(q => q.OID == 8 && q.Title.Contains(KeyTitle)).ToList();
                    var OIs_ = from q in OIs
                               select new { value = q.OIID, Text = q.Title + q.Organize.Title + (q.BusinessType == 1 ? "(外展)" : "") };
                    sReturn = JsonConvert.SerializeObject(OIs_.OrderBy(q => q.Text));
                }

            }

            return sReturn;
        }
        #endregion
        #region 前台主者管轄的小組清單(組換組)
        [HttpGet]
        public string GetOIChangeOIList(string KeyTitle, int OID)
        {
            ACID = GetACID();
            string sReturn = "";
            var O = DC.Organize.FirstOrDefault(q => q.OID == OID && !q.DeleteFlag);
            if (O != null)
            {
                if (ACID == 1)
                {
                    var OIs = (from q in DC.OrganizeInfo.Where(q => q.OID == OID && q.ActiveFlag && !q.DeleteFlag && (string.IsNullOrEmpty(KeyTitle) ? true : q.Title.Contains(KeyTitle)))
                               select new { value = q.OIID, Text = q.Title + q.Organize.Title + (q.BusinessType == 1 ? "(外展)" : "") }).OrderBy(q => q.Text);

                    sReturn = JsonConvert.SerializeObject(OIs);
                }
                else
                {
                    var OS = GetO();
                    var MyOIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID).ToList();
                    var Gs = from q in OS
                             join p in MyOIs
                             on q.OID equals p.OID
                             select new { q.SortNo, OI = p };
                    OrganizeInfo MyOI = new OrganizeInfo();
                    if (Gs.Count() > 0)
                        MyOI = Gs.OrderBy(q => q.SortNo).First().OI;

                    List<OrganizeInfo> OIs = new List<OrganizeInfo>();
                    OIs = GetThisOIsFromTree(ref OIs, MyOI.OIID);


                    var OIs_ = (from q in OIs.Where(q => q.OID == O.OID && q.ActiveFlag && !q.DeleteFlag && (string.IsNullOrEmpty(KeyTitle) ? true : q.Title.Contains(KeyTitle)))
                                select new { value = q.OIID, Text = q.Title + q.Organize.Title + (q.BusinessType == 1 ? "(外展)" : "") }).OrderBy(q => q.Text);

                    sReturn = JsonConvert.SerializeObject(OIs_);
                }
            }

            return sReturn;
        }
        #endregion
        #region 組織換老爸
        [HttpGet]
        public string OIChangeParent(int ThisOIID, int OIID, int OID)
        {
            Error = "";
            int UID = GetACID();
            var O = DC.Organize.FirstOrDefault(q => q.OID == OID && !q.DeleteFlag);
            var OI_Basic = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == ThisOIID && !q.DeleteFlag);
            var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.OIID == OIID && q.OID == O.ParentID);
            if (OI_Basic == null)
                Error += "此組織不存在<br/>";
            if (OI == null)
                Error += "目標組織不存在<br/>";
            if (Error == "")
            {
                OI_Basic.ParentID = OI.OIID;
                OI_Basic.UpdDate = DT;
                OI_Basic.SaveACID = UID;
                DC.SubmitChanges();
                Error = "OK";
            }
            return Error;
        }
        #endregion
    }
}