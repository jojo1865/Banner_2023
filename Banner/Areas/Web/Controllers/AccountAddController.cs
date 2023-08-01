using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using static Banner.Areas.Web.Controllers.AccountAddController;

namespace Banner.Areas.Web.Controllers
{
    public class AccountAddController : PublicClass
    {
        #region 註冊會員1
        public class cStep1
        {
            public int ACID = 0;
            public string Login = "";
            public string PW = "";
            public int ZID = 10;
            public string CellPhone = "";
            public string Email = "";
            public string Name_F = "";
            public string Name_L = "";
            public bool Sex = true;
            public string sBD = "";
            public DateTime dBD = DateTime.Now;
        }
        public cStep1 GetStep1(string ACID = "", FormCollection FC = null)
        {
            cStep1 N = new cStep1();
            var AC = DC.Account.FirstOrDefault(q => q.ACID.ToString() == ACID);
            if (AC != null)
            {
                N.ACID = AC.ACID;
                N.Login = AC.Login;
                N.Name_F = AC.Name.Split('@')[0];
                N.Name_L = AC.Name.Contains("@") ? AC.Name.Split('@')[1] : "";
                N.Sex = AC.ManFlag;
                N.sBD = AC.CreDate != AC.Birthday ? AC.Birthday.ToString("yyyy-MM-dd") : "";
                N.dBD = AC.Birthday;

                var Con = DC.Contect.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == AC.ACID && q.ContectType == 1);
                if (Con != null)
                {
                    N.ZID = Con.ZID;
                    N.CellPhone = Con.ContectValue;
                }
                else
                {
                    N.ZID = 10;
                    N.CellPhone = "";
                }
                Con = DC.Contect.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == AC.ACID && q.ContectType == 2);
                if (Con != null)
                    N.Email = Con.ContectValue;
                else
                    N.Email = "";
            }
            if (FC != null)
            {
                N.Login = FC.Get("txb_Login");
                N.PW = FC.Get("txb_PW1");

                N.ZID = Convert.ToInt32(FC.Get("ddl_PhoneZip"));
                N.CellPhone =  FC.Get("txb_PhoneNo");
                N.Email = FC.Get("txb_Email");
                N.Name_F = FC.Get("txb_Name_First") ;
                N.Name_L = FC.Get("txb_Name_Last");
                N.Sex= Convert.ToBoolean(FC.Get("cbox_Sex"));
                N.sBD = FC.Get("txb_Birthday");
                N.dBD = Convert.ToDateTime(N.sBD);
            }

            return N;
        }
        public ActionResult Step1()
        {
            GetViewBag();
            if (GetACID() > 0)
                SetAlert("您已登入帳戶", 4, "/Web/Home/Index");
            cStep1 N = new cStep1();
            if (GetQueryStringInString("ACID") != "")
                N = GetStep1(HSM.Des_1(GetQueryStringInString("ACID")), null);

            return View(N);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Step1(FormCollection FC)
        {
            GetViewBag();
            cStep1 N = new cStep1();
            if (GetQueryStringInString("ACID") != "")
                N = GetStep1(HSM.Des_1(GetQueryStringInString("ACID")), FC);
            else
                N = GetStep1("", FC);

            bool bLogin = GetViewCheckBox(FC.Get("cbox_Login_Success"));
            bool bPW = GetViewCheckBox(FC.Get("cbox_PW_Success"));
            if (FC == null)
                SetAlert("無正確傳回,請重新填寫", 2);
            else if (bLogin && bPW)
            {
                if(N.ACID == 0)
                {
                    Account AC = new Account
                    {
                        Login = N.Login,
                        Password = HSM.Enc_1(N.PW),
                        Name = N.Name_F+"@"+N.Name_L,
                        ManFlag = N.Sex,
                        IDNumber = "",
                        IDType = 0,
                        Birthday = N.dBD,
                        EducationType = 0,
                        JobType = 0,
                        MarriageType = 0,
                        MarriageNote = "",
                        BaptizedType = 0,
                        GroupType = "無意願",
                        BackUsedFlag = false,
                        ActiveFlag = false,
                        DeleteFlag = false,
                        CreDate = DT,
                        UpdDate = DT,
                        SaveACID = 0,
                        OldID = 0
                    };
                    DC.Account.InsertOnSubmit(AC);
                    DC.SubmitChanges();

                    N.ACID = AC.ACID;

                    Contect Con = new Contect
                    {
                        TargetType = 2,
                        TargetID = AC.ACID,
                        ZID = N.ZID,
                        ContectType = 1,
                        ContectValue = N.CellPhone
                    };
                    DC.Contect.InsertOnSubmit(Con);
                    DC.SubmitChanges();

                    Con = new Contect
                    {
                        TargetType = 2,
                        TargetID = AC.ACID,
                        ZID = 0,
                        ContectType = 2,
                        ContectValue = N.Email
                    };
                    DC.Contect.InsertOnSubmit(Con);
                    DC.SubmitChanges();

                }
                else
                {
                    var AC = DC.Account.FirstOrDefault(q => q.ACID == N.ACID);
                    AC.Login = N.Login;
                    AC.Password = HSM.Enc_1(N.PW);
                    AC.Name = N.Name_F + "@" + N.Name_L;
                    AC.ManFlag = N.Sex;
                    AC.Birthday = N.dBD;
                    DC.SubmitChanges();

                    Contect Con = DC.Contect.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == N.ACID && q.ContectType == 1);
                    if(Con!=null)
                    {
                        Con.ContectValue = N.CellPhone;
                        DC.SubmitChanges();
                    }
                    else
                    {
                        Con = new Contect
                        {
                            TargetType = 2,
                            TargetID = AC.ACID,
                            ZID = N.ZID,
                            ContectType = 1,
                            ContectValue = N.CellPhone
                        };
                        DC.Contect.InsertOnSubmit(Con);
                        DC.SubmitChanges();
                    }

                    Con = DC.Contect.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == N.ACID && q.ContectType == 2);
                    if (Con != null)
                    {
                        Con.ContectValue = N.Email;
                        DC.SubmitChanges();
                    }
                    else
                    {
                        Con = new Contect
                        {
                            TargetType = 2,
                            TargetID = AC.ACID,
                            ZID = N.ZID,
                            ContectType = 2,
                            ContectValue = N.Email
                        };
                        DC.Contect.InsertOnSubmit(Con);
                        DC.SubmitChanges();
                    }
                }

                Response.Redirect("/Web/AccountAdd/Step2?ACID" + HSM.Enc_1(N.ACID.ToString().PadLeft(5, '0')));
            }
            return View(N);
        }

        [HttpGet]
        public string CheckLogin(string input)
        {
            string output = "";
            if (string.IsNullOrEmpty(input))
            {
                var AC = DC.Account.FirstOrDefault(q => !q.DeleteFlag && q.Login == input);
                if (AC != null) { output = "此帳號已被使用"; }
                else { output = "OK"; }
            }
            else { output = "請輸入帳號"; }

            return output;
        }
        #endregion
        #region 註冊會員2
        
        public ActionResult Step2()
        {
            //https://localhost:44307/Web/AccountAdd/Step2?ACID16DEB26362377E03
            GetViewBag();
            string sACID = HSM.Des_1(GetQueryStringInString("ACID"));

            return View();
        }
        #endregion
    }
}