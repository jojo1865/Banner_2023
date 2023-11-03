using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.IO;
using System.Security.Cryptography;
using System.Web.Helpers;
using System.Drawing;

namespace Banner.Areas.Web.Controllers
{
    public class HomeController : PublicClass
    {
        // GET: Web/Home
        public ActionResult Index()
        {
            GetViewBag();
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
            /*if (Request.Url.Host == "localhost" && Request.Url.Port == 44307)
            {
                if (GetACID() <= 0)
                {
                    LogInAC(1);
                    SetBrowserData("UserName", "系統管理者");
                }
                Response.Redirect("/Web/Home/Index");
            }*/
            TempData["login"] = "";
            TempData["pw"] = "";
            return View();
        }
        [HttpPost]
        //[ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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
                    SetAlert("您的新密碼已發送,請查看您的信箱", 1, "/Web/Home/Login");
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
                Error = "無登入會員資料";
            else if (PID <= 0)
                Error = "無課程資料";
            else
            {
                var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID);
                var P = DC.Product.FirstOrDefault(q => q.PID == PID);
                #region 基本檢查
                if (AC != null)
                {
                    if (AC.DeleteFlag)
                        Error = "無會員資料";
                    else if (!AC.ActiveFlag)
                        Error = "會員資料未啟用";
                }
                else
                    Error = "無會員資料";
                if (Error == "")
                {
                    if (P != null)
                    {
                        if (P.DeleteFlag)
                            Error = "無課程資料";
                        else if (!P.ActiveFlag)
                            Error = "課程並未允許交易";
                        else
                        {
                            var OP = DC.Order_Product.FirstOrDefault(q =>
                            q.Order_Header.ACID == ACID &&
                            !q.Order_Header.DeleteFlag &&
                            q.Product_Class.PID == P.PID
                            );
                            if(OP!=null)
                            {
                                if(OP.Order_Header.Order_Type == 0)
                                    Error = "此課程已在購物車中";
                                else if (OP.Order_Header.Order_Type == 1)
                                    Error = "此課程正在結帳中...";
                                else if (OP.Order_Header.Order_Type == 2)
                                    Error = "此課程您已經買過了";
                                else
                                {
                                    //人數限制檢查

                                    //有買這個商品且結帳完成的正常訂單,統計每個班級的人數
                                    var OP_Gs = (from q in DC.Order_Product.Where(q => !q.Order_Header.DeleteFlag && q.Order_Header.Order_Type == 2 && q.Product_Class.PID == PID)
                                                 group q by new { q.PCID } into g
                                                 select new { g.Key.PCID, Ct = g.Count() }).ToList();

                                    var PCs = DC.Product_Class.Where(q => q.PID == PID).OrderBy(q => q.PCID).ToList();
                                    if (PCs.Count() > 0)//有班級可以上
                                    {
                                        //先檢查限制名額的
                                        var PC_Ns = PCs.Where(q => q.PeopleCt > 0).OrderBy(q => q.PCID).ToList();
                                        foreach (var PC_N in PC_Ns)
                                        {
                                            var OP_G = OP_Gs.FirstOrDefault(q => q.PCID == PC_N.PCID);
                                            if (OP_G == null)
                                                PCID = OP_G.PCID;
                                            else if (PC_N.PeopleCt < OP_G.Ct)
                                                PCID = OP_G.PCID;
                                            if (PCID > 0)
                                                break;
                                        }
                                        if (PCID == 0)//沒班級再檢查不限名額的
                                        {
                                            var PC_0s = PCs.Where(q => q.PeopleCt == 0).OrderBy(q => q.PCID).ToList();
                                            if (PC_0s.Count() > 0)
                                                PCID = PC_0s.First().PCID;
                                        }
                                        if (PCID == 0)
                                            Error += "目前班級均已額滿";
                                    }
                                    else
                                        Error += "目前沒有班級可以上課";
                                }
                            }             
                        }
                    }
                    else
                        Error = "無課程資料";
                }
                #endregion
                #region 購買資格檢查

                if (Error == "")
                {
                    //有線上報名日期
                    if (P.SDate_Signup_OnLine.Date >= P.CreDate.Date && P.SDate_Signup_OnLine.Date < DT.Date)
                        Error = "本課程尚未開始線上報名";
                    else if (P.EDate_Signup_OnLine.Date >= P.CreDate.Date && P.EDate_Signup_OnLine.Date > DT.Date)
                        Error = "本課程已結束線上報名";
                    else
                    {
                        bool bCheck0 = false;//先修課程檢查過沒?
                        bool bCheck1 = false;//職分檢查過沒?
                        //有購買限制規則
                        foreach (var R in P.Product_Rool.OrderBy(q => q.TargetType))
                        {
                            switch (R.TargetType)
                            {
                                case 0://先修課程ID[Course]
                                    {
                                        if (!bCheck0)
                                        {
                                            var Os = from q in DC.Order_Product.Where(q => q.Order_Header.ACID == AC.ACID && q.Order_Header.Order_Type == 2 && !q.Order_Header.DeleteFlag)
                                                     join p in DC.Product_Rool.Where(q => q.PID == P.PID && q.TargetType == 0 && q.TargetInt1 > 0)
                                                     on q.Product_Class.PID equals p.TargetInt1
                                                     select p;
                                            if (Os.Count() == 0)
                                                Error = "本課程有限制需先參加先修課程,您不具備此資格";
                                            bCheck0 = true;
                                        }
                                    }
                                    break;

                                case 1://職分ID[OID]
                                    {
                                        if (!bCheck1)
                                        {
                                            var Os = from q in DC.M_O_Account.Where(q => q.ACID == AC.ACID && !q.DeleteFlag && q.ActiveFlag)
                                                     join p in DC.Product_Rool.Where(q => q.PID == P.PID && q.TargetType == 1 && q.TargetInt1 > 0)
                                                     on q.OID equals p.TargetInt1
                                                     select p;
                                            if (Os.Count() == 0)
                                                Error = "本課程有限制指定職分參加,您不具備職分資格";
                                            bCheck1 = true;
                                        }
                                    }
                                    break;

                                case 2://性別
                                    {
                                        if (R.TargetInt1 >= 0)//有限制
                                        {
                                            if (AC.ManFlag && R.TargetInt1 == 0)
                                                Error = "本課程限制女性參加";
                                            if (!AC.ManFlag && R.TargetInt1 == 1)
                                                Error = "本課程限制男性參加";
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
                                                    Error = "您的年紀不符最小年齡限制";
                                            }
                                            else if (R.TargetInt2 > 0)//有最大年齡限制
                                            {
                                                if ((DT.Year - AC.Birthday.Year) > R.TargetInt1)//今年-生日年>最大年齡限制
                                                    Error = "您的年紀不符最大年齡限制";
                                            }
                                        }

                                    }
                                    break;

                                case 4://事工團
                                    break;

                                case 5://指定會員ACID
                                    if (AC.ACID != R.TargetInt1)
                                        Error = "您非本課程指定會員";
                                    break;
                            }
                        }
                    }
                }
                if (Error == "")
                {
                    if (ACID != 1)//非管理者
                    {
                        var OICheck = DC.v_GetAC_O2_OI.Where(q => q.ACID == ACID && q.OIID == P.OIID);
                        if (OICheck == null)//本課程限制的旌旗與報名者不符
                            Error = "本課程只允許特定旌旗教會下的會友報名";
                    }
                }
                #endregion
                if (Error == "")
                {
                    int[] iPrice = GetPrice(ACID, P);
                    var OH = DC.Order_Header.FirstOrDefault(q => q.Order_Type == 0 && q.ACID == ACID);
                    if (OH != null)
                    {
                        if (!OH.Order_Product.Any(q => q.Product_Class.PID == PID))//這商品目前尚未加入購物車中
                        {
                            var PC = DC.Product_Class.FirstOrDefault(q => q.PID == PID && q.PCID == PCID);
                            if (PC != null)
                            {
                                Order_Product OP = new Order_Product
                                {
                                    Order_Header = OH,
                                    Product_Class = PC,
                                    CAID = iPrice[2],
                                    Price_Basic = PC.Product.Price_Basic,
                                    Price_Finally = iPrice[1],
                                    Price_Type = iPrice[0],
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

                    }
                    else
                    {
                       

                        var PC = DC.Product_Class.Where(q => q.PID == PID).OrderBy(q => q.PCID).FirstOrDefault();
                        if (PC != null)
                        {
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

                            Order_Product OP = new Order_Product
                            {
                                OHID = OH.OHID,
                                PCID = PC.PCID,
                                CAID = iPrice[2],
                                Price_Basic = PC.Product.Price_Basic,
                                Price_Finally = iPrice[1],
                                Price_Type = iPrice[0],
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = ACID
                            };
                            DC.Order_Product.InsertOnSubmit(OP);
                            DC.SubmitChanges();
                        }
                            
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
    }
}