using Banner.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Drawing;
using System.Linq;
using System.Net;
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
            public int ZID_Phone = 10;
            public int ZID_Location = 10;
            public string CellPhone = "";
            public string Email = "";
            public string Name_F = "";
            public string Name_L = "";
            public bool Sex = true;
            public string sBD = "";
            public DateTime dBD = DateTime.Now;

            public List<SelectListItem> Zs = new List<SelectListItem>();
        }
        public cStep1 GetStep1(string ACID = "", FormCollection FC = null)
        {
            cStep1 N = new cStep1();
            var AC = DC.Account.FirstOrDefault(q => q.ACID == Convert.ToInt32(ACID));
            if (AC != null)
            {
                N.ACID = AC.ACID;
                N.Login = AC.Login;
                N.Name_F = AC.Name_First;
                N.Name_L = AC.Name_Last;
                N.Sex = AC.ManFlag;
                N.sBD = AC.CreDate != AC.Birthday ? AC.Birthday.ToString("yyyy-MM-dd") : "";
                N.dBD = AC.Birthday;

                var Con = DC.Contect.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == AC.ACID && q.ContectType == 1);
                if (Con != null)
                {
                    N.ZID_Phone = Con.ZID;
                    N.CellPhone = Con.ContectValue;
                }
                else
                {
                    N.ZID_Phone = 10;
                    N.CellPhone = "";
                }
                Con = DC.Contect.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == AC.ACID && q.ContectType == 2);
                if (Con != null)
                    N.Email = Con.ContectValue;
                else
                    N.Email = "";

                var Loc = DC.Location.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == AC.ACID);
                if (Loc != null)
                    N.ZID_Location = Loc.ZID;
            }
            var Zs = DC.ZipCode.Where(q => q.ActiveFlag && q.GroupName == "國").OrderBy(q => q.ZID);
            foreach (var Z in Zs)
                N.Zs.Add(new SelectListItem { Text = Z.Title, Value = Z.ZID.ToString(), Selected = N.ZID_Location == Z.ZID });

            if (FC != null)
            {
                N.Login = FC.Get("txb_Login");
                N.PW = FC.Get("txb_PW1");

                N.ZID_Phone = Convert.ToInt32(FC.Get("ddl_PhoneZip"));
                N.CellPhone = FC.Get("txb_PhoneNo");
                N.Email = FC.Get("txb_Email");
                N.Name_F = FC.Get("txb_Name_First");
                N.Name_L = FC.Get("txb_Name_Last");
                N.Sex = Convert.ToBoolean(FC.Get("cbox_Sex"));
                N.sBD = FC.Get("txb_Birthday");
                N.dBD = Convert.ToDateTime(N.sBD);
                N.ZID_Location = Convert.ToInt32(FC.Get("ddl_CountryZip"));
                N.Zs.ForEach(q => q.Selected = false);
                N.Zs.First(q => q.Value == N.ZID_Location.ToString()).Selected = true;
            }

            return N;
        }
        public ActionResult Step1()
        {
            GetViewBag();
            if (GetACID() > 0)
                SetAlert("您已登入帳戶", 4, "/Web/Home/Index");
            cStep1 N = GetStep1(HSM.Des_1(GetQueryStringInString("ACID")), null);

            return View(N);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Step1(FormCollection FC)
        {
            GetViewBag();
            cStep1 N = GetStep1(HSM.Des_1(GetQueryStringInString("ACID")), FC);

            bool bLogin = GetViewCheckBox(FC.Get("cbox_Login_Success"));
            bool bPW = GetViewCheckBox(FC.Get("cbox_PW_Success"));
            if (FC == null)
                SetAlert("無正確傳回,請重新填寫", 2);
            else if (bLogin && bPW)
            {
                if (N.ACID == 0)
                {
                    Account AC = new Account
                    {
                        Login = N.Login,
                        Password = HSM.Enc_1(N.PW),
                        Name_First = N.Name_F,
                        Name_Last = N.Name_L,
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
                        ZID = N.ZID_Phone,
                        ContectType = 1,
                        ContectValue = N.CellPhone,
                        CheckFlag = false,
                        CreDate = DT,
                        CheckDate = DT
                    };
                    DC.Contect.InsertOnSubmit(Con);
                    DC.SubmitChanges();

                    Con = new Contect
                    {
                        TargetType = 2,
                        TargetID = AC.ACID,
                        ZID = 0,
                        ContectType = 2,
                        ContectValue = N.Email,
                        CheckFlag = false,
                        CreDate = DT,
                        CheckDate = DT
                    };
                    DC.Contect.InsertOnSubmit(Con);
                    DC.SubmitChanges();

                    Location L = new Location
                    {
                        TargetType = 2,
                        TargetID = AC.ACID,
                        ZID = N.ZID_Location,
                        Address = ""
                    };
                    DC.Location.InsertOnSubmit(L);
                    DC.SubmitChanges();

                }
                else
                {
                    var AC = DC.Account.FirstOrDefault(q => q.ACID == N.ACID);
                    AC.Login = N.Login;
                    AC.Password = HSM.Enc_1(N.PW);
                    AC.Name_First = N.Name_F;
                    AC.Name_Last = N.Name_L;
                    AC.ManFlag = N.Sex;
                    AC.Birthday = N.dBD;
                    DC.SubmitChanges();

                    Contect Con = DC.Contect.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == N.ACID && q.ContectType == 1);
                    if (Con != null)
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
                            ZID = N.ZID_Phone,
                            ContectType = 1,
                            ContectValue = N.CellPhone,
                            CheckFlag = false,
                            CreDate = DT,
                            CheckDate = DT
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
                            ZID = N.ZID_Phone,
                            ContectType = 2,
                            ContectValue = N.Email,
                            CheckFlag = false,
                            CreDate = DT,
                            CheckDate = DT
                        };
                        DC.Contect.InsertOnSubmit(Con);
                        DC.SubmitChanges();
                    }


                    Location L = DC.Location.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == N.ACID);
                    if (L != null)
                    {
                        L.ZID = N.ZID_Location;
                        DC.SubmitChanges();
                    }
                    else
                    {
                        L = new Location
                        {
                            TargetType = 2,
                            TargetID = AC.ACID,
                            ZID = N.ZID_Location,
                            Address = ""
                        };
                        DC.Location.InsertOnSubmit(L);
                        DC.SubmitChanges();
                    }
                }

                Response.Redirect("/Web/AccountAdd/Step2?ACID=" + HSM.Enc_1(N.ACID.ToString().PadLeft(5, '0')));
            }
            return View(N);
        }

        [HttpGet]
        public string CheckLogin(string input)
        {
            string output = "";
            if (!string.IsNullOrEmpty(input))
            {
                var AC = DC.Account.FirstOrDefault(q => !q.DeleteFlag && q.Login == input);
                if (AC != null) { output = "此帳號已被使用"; }
                else { output = "OK"; }
            }
            else { output = "請輸入帳號"; }

            return "{\"res\":\"" + output + "\"}";
        }
        #endregion
        #region 註冊會員2
        public class cStep2
        {
            public string sACID = "";
            public int iACID = 0;
            public int PhoneZip = 10;
            public string PhoneNo = "";
            public string Email = "";
            public string CheckCode_Get = "";
            public string CheckCode_Input = "";
        }
        public ActionResult Step2()
        {
            //https://localhost:44307/Web/AccountAdd/Step2?ACID=16DEB26362377E03
            GetViewBag();
            cStep2 N = new cStep2();
            N.sACID = GetQueryStringInString("ACID");
            N.iACID = Convert.ToInt32(HSM.Des_1(N.sACID));
            var Cons = DC.Contect.Where(q => q.TargetType == 2 && q.TargetID == N.iACID).ToList();
            var Con = Cons.FirstOrDefault(q => q.ContectType == 1);
            if (Con != null)
            {
                N.PhoneZip = Con.ZID;
                N.PhoneNo = Con.ContectValue;
            }

            Con = Cons.FirstOrDefault(q => q.ContectType == 2);
            if (Con != null)
                N.Email = Con.ContectValue;

            return View(N);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Step2(FormCollection FC)
        {
            GetViewBag();
            cStep2 N = new cStep2();
            N.sACID = GetQueryStringInString("ACID");
            N.iACID = Convert.ToInt32(HSM.Des_1(N.sACID));


            var Cons = DC.Contect.Where(q => q.TargetType == 2 && q.TargetID == N.iACID).ToList();
            var Con1 = Cons.FirstOrDefault(q => q.ContectType == 1);
            if (Con1 != null)
            {
                N.PhoneZip = Con1.ZID;
                N.PhoneNo = Con1.ContectValue;

            }

            var Con2 = Cons.FirstOrDefault(q => q.ContectType == 2);
            if (Con2 != null)
                N.Email = Con2.ContectValue;

            bool PhoneFlag = Convert.ToBoolean(FC.Get("rbut_Con"));
            string sGetCode = FC.Get("txb_Code_Get");
            string sSetCode = FC.Get("txb_CheckCode");
            if (sGetCode != sSetCode)
                SetAlert("檢查碼輸入錯誤", 2);
            else
            {
                if (PhoneFlag)
                {
                    Con1.CheckFlag = true;
                    Con1.CheckDate = DT;
                    DC.SubmitChanges();
                }
                else
                {
                    Con2.CheckFlag = true;
                    Con2.CheckDate = DT;
                    DC.SubmitChanges();
                }
                SetAlert("", 1, "/Web/AccountAdd/Step3?ACID=" + N.sACID);
            }
            return View(N);
        }
        #endregion
        #region 註冊會員3
        public class cStep3
        {
            public string sACID = "";
            public int iACID = 0;
            public Account AC = new Account();
            public List<ListInput> Coms = new List<ListInput>();//社群帳號
            public Contect C = new Contect();//市話
            public int LID = 0;//聯絡地址
            //受洗
            public List<SelectListItem> BaptizedTypes = new List<SelectListItem> {
                new SelectListItem{ Text="旌旗受洗",Value="1",Selected=true},
                new SelectListItem{ Text="非旌旗受洗",Value="2"}
            };

            public List<SelectListItem> MLs = new List<SelectListItem>();//主日聚會點
        }
        public cStep3 SetStep3(string ACID,FormCollection FC)
        {
            cStep3 N = new cStep3();
            N.sACID = ACID;
            #region 初始化物件
            //社群帳號
            N.Coms = new List<ListInput>();
            for (int i = 0; i < CommunityTitle.Length; i++)
                N.Coms.Add(new ListInput { Title = CommunityTitle[i], SortNo = i, ControlName = "txb_Com_" + i, InputData = "" });
            N.C.ContectType = 0;
            N.C.ZID = 10;
            N.C.CheckFlag = false;
            N.C.CreDate = N.C.CheckDate = DT;

            //主日聚會點初始化
            N.MLs.AddRange((from q in DC.M_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag && q.SetType == 0)
                            select new SelectListItem { Text = q.Meeting_Location.Title, Value = q.MLID.ToString() }).ToList());
            N.MLs[0].Selected = true;
            #endregion

            #region 帶入使用者資料
            if (!string.IsNullOrEmpty(ACID))
            {
                N.iACID = Convert.ToInt32(HSM.Des_1(ACID));
                var AC = DC.Account.FirstOrDefault(q => q.ACID == N.iACID && !q.DeleteFlag);
                if(AC!=null)
                {
                    N.AC = AC;
                    //社群帳號
                    foreach (var Com_ in AC.Community.OrderBy(q=>q.CommunityType))
                    {
                        N.Coms.First(q => q.SortNo == Com_.CommunityType).InputData = Com_.CommunityValue;
                    }
                    //居住地址
                    var Loc = DC.Location.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == AC.ACID);
                    if (Loc != null)
                        N.LID = Loc.LID;
                    //受洗狀態
                    N.BaptizedTypes[0].Selected = N.AC.BaptizedType == 1;
                    N.BaptizedTypes[1].Selected = N.AC.BaptizedType == 2;
                    //主日聚會點
                    var M_MLs = from q in DC.M_ML_Account.Where(q => q.ACID == N.iACID)
                               join p in DC.M_Location_Set.Where(q => q.SetType == 0 && !q.DeleteFlag && q.ActiveFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag)
                               on q.MLID equals p.MLID
                               select q;
                    if(M_MLs.Count()>0)
                    {
                        int iMLID = M_MLs.OrderByDescending(p => p.MID).First().MLID;
                        N.MLs.ForEach(q => q.Selected = false);
                        N.MLs.First(q => q.Value == iMLID.ToString()).Selected = true;
                    }
                }

            }
            #endregion

            #region 帶入回傳內容
            if (FC!=null)
            {

            }
            #endregion
            



            return N;
        }
        [HttpGet]
        public ActionResult Step3()
        {
            //https://localhost:44307/Web/AccountAdd/Step3?ACID=16DEB26362377E03
            GetViewBag();
            cStep3 N = SetStep3(GetQueryStringInString("ACID"),null);
            return View(N);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Step3(FormCollection FC)
        {
            GetViewBag();
            cStep3 N = SetStep3(GetQueryStringInString("ACID"), FC);
            return View(N);
        }
        #endregion
    }
}