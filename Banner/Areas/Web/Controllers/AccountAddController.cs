using Banner.Models;
using Microsoft.Ajax.Utilities;
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
            var AC = DC.Account.FirstOrDefault(q => q.ACID == Convert.ToInt32(ACID == "" ? "0" : ACID));
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
            string sPhone = CheckCellPhoneDouble(N.CellPhone);
            string sEmail = CheckEmailDouble(N.Email);
            if (FC == null)
                SetAlert("無正確傳回,請重新填寫", 2);
            else if (sPhone != "OK")
                SetAlert(sPhone, 2);
            else if (sEmail != "OK")
                SetAlert(sEmail, 2);
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
                        TeacherFlag = false,
                        ActiveFlag = false,
                        DeleteFlag = false,
                        CreDate = DT,
                        UpdDate = DT,
                        LoginDate=DT,
                        LogoutDate=DT,
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
            return output;
            //return "{\"res\":\"" + output + "\"}";
        }
        public string CheckCellPhoneDouble(string Input)
        {
            string output = "";
            var Con = DC.Contect.FirstOrDefault(q => q.ContectValue == Input && q.ContectType == 1);
            if (Con != null)
                output = "此手機號碼已被使用";
            else
                output = "OK";
            return output;
        }
        public string CheckPhoneNoDouble(string Input)
        {
            string output = "";
            var Con = DC.Contect.FirstOrDefault(q => q.ContectValue == Input && q.ContectType == 0);
            if (Con != null)
                output = "此電話號碼已被使用";
            else
                output = "OK";
            return output;
        }
        public string CheckEmailDouble(string Input)
        {
            string output = "";
            var Con = DC.Contect.FirstOrDefault(q => q.ContectValue == Input && q.ContectType == 2);
            if (Con != null)
                output = "此Email已被使用";
            else
                output = "OK";
            return output;
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
            public string HideEmail = "";
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
            {
                N.HideEmail = N.Email = Con.ContectValue;
                string[] CutEmail = N.Email.Split('@');
                if (CutEmail.Length > 1)
                    N.HideEmail = (CutEmail[0].Length > 3 ? CutEmail[0].Substring(0, 3) + new string('*', 5) : CutEmail[0].Substring(0, 1)) + "@" + CutEmail[1];
            }
            //2023/8/14 新增


            string CheckCode = GetRand(1000000).ToString().PadLeft(6, '0');
            N.CheckCode_Get = HSM.Enc_1(CheckCode);
            Error = SendMail(N.Email, N.Email, "【全球旌旗資訊網】Email認證", "親愛的旌旗家人,您的驗證碼為：" + CheckCode + "。");
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
            {
                N.HideEmail = N.Email = Con2.ContectValue;
                string[] CutEmail = N.Email.Split('@');
                if (CutEmail.Length > 1)
                    N.HideEmail = (CutEmail[0].Length > 3 ? CutEmail[0].Substring(0, 3) + new string('*', 5) : CutEmail[0].Substring(0, 1)) + "@" + CutEmail[1];
            }
            //2023/08/14 只要驗證Email就好
            /*bool PhoneFlag = Convert.ToBoolean(FC.Get("rbut_Con"));
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
            }*/


            N.CheckCode_Get = HSM.Des_1(FC.Get("txb_Code_Get"));
            string sSetCode = FC.Get("txb_CheckCode");
            if (N.CheckCode_Get != sSetCode)
                SetAlert("檢查碼輸入錯誤", 2);
            else
            {
                Con2.CheckFlag = true;
                Con2.CheckDate = DT;
                DC.SubmitChanges();
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
            public Location Loc = new Location();//聯絡地址
            //受洗
            public List<SelectListItem> BaptizedTypes = new List<SelectListItem> {
                new SelectListItem{ Text="旌旗受洗",Value="1",Selected=true},
                new SelectListItem{ Text="非旌旗受洗",Value="2"}
            };

            public List<SelectListItem> MLs = new List<SelectListItem>();//主日聚會點
        }
        public cStep3 SetStep3(string ACID, FormCollection FC)
        {
            cStep3 N = new cStep3();
            N.sACID = ACID;
            #region 初始化物件
            //社群帳號
            N.Coms = new List<ListInput>();
            for (int i = 0; i < CommunityTitle.Length; i++)
                N.Coms.Add(new ListInput { Title = CommunityTitle[i], SortNo = i, ControlName = "txb_Com_" + i, InputData = "" });
            //市話
            N.C.ContectType = 0;
            N.C.ZID = 10;
            N.C.CheckFlag = false;
            N.C.CreDate = N.C.CheckDate = DT;

            //主日聚會點初始化
            N.MLs.AddRange((from q in DC.Meeting_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag && q.SetType == 0)
                            select new SelectListItem { Text = q.Meeting_Location.Title, Value = q.MLID.ToString() }).ToList());
            N.MLs[0].Selected = true;
            #endregion

            #region 帶入使用者資料
            if (!string.IsNullOrEmpty(ACID))
            {
                N.iACID = Convert.ToInt32(HSM.Des_1(ACID));
                var AC = DC.Account.FirstOrDefault(q => q.ACID == N.iACID && !q.DeleteFlag);
                if (AC != null)
                {
                    N.AC = AC;
                    //社群帳號
                    foreach (var Com_ in AC.Community.OrderBy(q => q.CommunityType))
                    {
                        N.Coms.First(q => q.SortNo == Com_.CommunityType).InputData = Com_.CommunityValue;
                    }
                    //市話
                    N.C = DC.Contect.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == N.AC.ACID && q.ContectType == 0);
                    if (N.C == null)
                    {
                        N.C = new Contect
                        {
                            TargetType = 2,
                            TargetID = N.AC.ACID,
                            ZID = 10,
                            ContectType = 0,
                            ContectValue = "",
                            CheckFlag = false,
                            CreDate = DT,
                            CheckDate = DT
                        };
                    }
                    //居住地址
                    var Loc = DC.Location.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == AC.ACID);
                    if (Loc != null)
                        N.Loc = Loc;
                    //受洗狀態
                    N.BaptizedTypes[0].Selected = N.AC.BaptizedType == 1;
                    N.BaptizedTypes[1].Selected = N.AC.BaptizedType == 2;
                    //主日聚會點
                    var M_MLs = from q in DC.M_ML_Account.Where(q => q.ACID == N.iACID)
                                join p in DC.Meeting_Location_Set.Where(q => q.SetType == 0 && !q.DeleteFlag && q.ActiveFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag)
                                on q.MLID equals p.MLID
                                select q;
                    if (M_MLs.Count() > 0)
                    {
                        int iMLID = M_MLs.OrderByDescending(p => p.MID).First().MLID;
                        N.MLs.ForEach(q => q.Selected = false);
                        N.MLs.First(q => q.Value == iMLID.ToString()).Selected = true;
                    }
                }
                else
                    SetAlert("註冊資料已遺失,請重新註冊", 2, "/Web/AccountAdd/Step1");
            }
            #endregion

            #region 帶入回傳內容
            if (FC != null)
            {
                //社群帳號
                foreach (var CM in N.Coms.OrderByDescending(q => q.SortNo))
                    CM.InputData = FC.Get(CM.ControlName);
                //身分證/居留證
                N.AC.IDNumber = FC.Get("txb_SSN");
                N.AC.IDType = CheckSSN(N.AC.IDNumber) ? 0 : 1;

                //電話(市話)
                N.C.ContectValue = FC.Get("txb_PhoneNo");
                N.C.ZID = Convert.ToInt32(FC.Get("ddl_PhoneZip"));

                //居住地址
                if (FC.Get("ddl_Zip0") == "10")//台灣
                {
                    N.Loc.ZID = Convert.ToInt32(FC.Get("ddl_Zip2"));
                    N.Loc.Address = FC.Get("txb_Address0");
                }
                else//海外
                {
                    N.Loc.ZID = Convert.ToInt32(FC.Get("ddl_Zip3"));
                    N.Loc.Address = FC.Get("txb_Address1_1") + "\n" + FC.Get("txb_Address1_2");
                }
                //信仰資料
                if (FC.Get("rbl_Baptized2") == "1")
                    N.AC.BaptizedType = Convert.ToInt32(FC.Get("ddl_BaptizedType"));
                else
                    N.AC.BaptizedType = 0;
                //主日聚會點
                N.MLs.ForEach(q => q.Selected = false);
                N.MLs.First(q => q.Value == FC.Get("ddl_ML")).Selected = true;
            }
            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult Step3()
        {
            //https://localhost:44307/Web/AccountAdd/Step3?ACID=16DEB26362377E03
            GetViewBag();
            cStep3 N = SetStep3(GetQueryStringInString("ACID"), null);
            return View(N);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Step3(FormCollection FC)
        {
            GetViewBag();
            cStep3 N = SetStep3(GetQueryStringInString("ACID"), FC);
            var AC_ = DC.Account.FirstOrDefault(q => q.ACID != N.AC.ACID && q.IDNumber == N.AC.IDNumber && !q.DeleteFlag);
            if (AC_ != null) //檢查身分證字號是否被其他帳號使用
                SetAlert("此身份證字號已被申請帳號,無法送出存檔", 2);
            else if (FC != null)
            {
                //社群帳號
                for (int i = 0; i < CommunityTitle.Length; i++)
                {
                    var Com = DC.Community.FirstOrDefault(q => q.ACID == N.AC.ACID && q.CommunityType == i);
                    if (N.Coms.FirstOrDefault(q => q.SortNo == i) != null)
                    {
                        if (Com != null)
                            Com.CommunityValue = N.Coms.First(q => q.SortNo == i).InputData;
                        else
                        {
                            Com = new Community
                            {
                                ACID = N.AC.ACID,
                                CommunityType = i,
                                CommunityValue = N.Coms.First().InputData
                            };
                            DC.Community.InsertOnSubmit(Com);
                        }
                        DC.SubmitChanges();
                    }

                }
                //市話
                var Con = DC.Contect.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == N.AC.ACID && q.ContectType == 0);
                if (Con == null)
                {
                    DC.Contect.InsertOnSubmit(N.C);
                    DC.SubmitChanges();
                }
                else
                {
                    Con.ZID = N.C.ZID;
                    Con.ContectValue = N.C.ContectValue;
                    DC.SubmitChanges();
                }
                //地址
                var Loc = DC.Location.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == N.AC.ACID);
                if (Loc == null)
                {
                    Loc = new Location
                    {
                        TargetType = 2,
                        TargetID = N.AC.ACID,
                        ZID = N.Loc.ZID,
                        Address = N.Loc.Address
                    };
                    DC.Location.InsertOnSubmit(Loc);
                    DC.SubmitChanges();
                }
                else
                {
                    Loc.ZID = N.Loc.ZID;
                    Loc.Address = N.Loc.Address;
                    DC.SubmitChanges();
                }
                //主日聚會點
                var MLSs = from q in DC.Meeting_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.SetType == 0 && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag)
                           join p in DC.M_ML_Account.Where(q => !q.DeleteFlag && q.ACID == N.AC.ACID)
                           on q.MLID equals p.MLID
                           select p;
                if (MLSs.Count() > 0)
                {
                    var LI = N.MLs.FirstOrDefault(q => q.Selected);
                    var MML = MLSs.First();
                    if (LI != null)
                        MML.MLID = Convert.ToInt32(LI.Value);
                    else
                        MML.MLID = Convert.ToInt32(N.MLs[0].Value);
                    MML.SaveACID = N.AC.ACID;
                    MML.UpdDate = DT;
                    DC.SubmitChanges();
                }
                else
                {
                    M_ML_Account MML = new M_ML_Account
                    {
                        MLID = Convert.ToInt32(N.MLs.First(q => q.Selected).Value),
                        Account = N.AC,
                        DeleteFlag = false,
                        CreDate = DT,
                        UpdDate = DT,
                        SaveACID = N.AC.ACID
                    };
                    DC.M_ML_Account.InsertOnSubmit(MML);
                    DC.SubmitChanges();
                }
                SetAlert("", 1, "/Web/AccountAdd/Step4?ACID=" + N.sACID);
            }
            else
                SetAlert("發生錯誤,將返回第一步驟", 2, "/Web/AccountAdd/Step1?ACID=" + N.sACID);
            return View(N);
        }
        #endregion
        #region 註冊會員4
        public class cStep4
        {
            public string sACID = "";
            public int iACID = 0;
            public Account AC = null;
            public int JoinGroupType = 0;//入組意願調查選擇
            public List<cJoinGroupWish> cJGWs = new List<cJoinGroupWish>();//加入小組有意願選項
            public string GroupNo = "";//想加入小組的編號

            public bool b_4_1 = false;//是否在旌旗小組
            public bool b_4_2 = true;//是否願意加入旌旗小組
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
        public cStep4 GetStep4(string sACID, FormCollection FC)
        {
            #region 物件初始化

            cStep4 N = new cStep4();
            N.sACID = sACID;

            //加入小組有意願選項初始化
            N.cJGWs = new List<cJoinGroupWish>();
            for (int i = 0; i < 2; i++)
            {
                for (int j = 1; j < 4; j++)
                {
                    cJoinGroupWish cJ = new cJoinGroupWish();
                    cJ.JGW = new JoinGroupWish
                    {
                        ACID = 0,
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
                    cJ.ddl_Time.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
                    for (int k = 1; k < sWeeks.Length; k++)
                        cJ.ddl_Weekly.ddlList.Add(new SelectListItem { Text = sWeeks[k], Value = (k + 1).ToString() });
                    for (int k = 0; k < sTimeSpans.Length; k++)
                        cJ.ddl_Time.ddlList.Add(new SelectListItem { Text = sTimeSpans[k], Value = (k + 1).ToString() });
                    N.cJGWs.Add(cJ);
                }
            }

            #endregion
            #region 會員資料帶入

            if (!string.IsNullOrEmpty(sACID))
            {
                N.iACID = Convert.ToInt32(HSM.Des_1(sACID));
                var AC = DC.Account.FirstOrDefault(q => q.ACID == N.iACID && !q.DeleteFlag);
                if (AC != null)
                {
                    N.AC = AC;
                    var MOIACs = GetMOIAC(0, 0, AC.ACID);
                    if (MOIACs.Count() == 0)
                        N.JoinGroupType = 0;
                    else
                    {
                        var Js = DC.JoinGroupWish.Where(q => q.ACID == AC.ACID).ToList();
                        var MOIAC = MOIACs.OrderByDescending(q => q.MID).First();
                        if (MOIAC.OIID == 1)
                        {
                            N.b_4_1 = false;
                            N.b_4_2 = false;
                            N.JoinGroupType = 1;
                            foreach (var cJGW in N.cJGWs)
                            {
                                cJGW.JGW.ACID = AC.ACID;
                                var JGW = Js.FirstOrDefault(q => q.JoinType == cJGW.JoinType && q.SortNo == cJGW.SortNo);
                                cJGW.JGW = JGW;
                                if (JGW != null)
                                {
                                    if (!cJGW.SelectFalg)
                                    {
                                        cJGW.SelectFalg = cJGW.JGW.WeeklyNo > 0 && cJGW.JGW.TimeNo > 0;
                                        if (!N.b_4_2)
                                            N.b_4_2 = cJGW.SelectFalg;
                                    }
                                    cJGW.ddl_Weekly.ddlList.ForEach(q => q.Selected = false);
                                    cJGW.ddl_Weekly.ddlList.First(q => q.Value == cJGW.JGW.WeeklyNo.ToString()).Selected = true;
                                    cJGW.ddl_Time.ddlList.ForEach(q => q.Selected = false);
                                    cJGW.ddl_Time.ddlList.First(q => q.Value == cJGW.JGW.TimeNo.ToString()).Selected = true;
                                }
                            }
                            N.b_4_1 = N.b_4_2;
                        }
                        else
                        {
                            N.b_4_1 = true;
                            N.b_4_2 = false;
                            N.JoinGroupType = 2;
                            N.GroupNo = MOIAC.OrganizeInfo.OIID.ToString();
                        }
                    }
                }
                else
                    SetAlert("註冊資料已遺失,請重新註冊", 2, "/Web/AccountAdd/Step1");
            }

            #endregion
            #region 帶入回傳內容
            if (FC != null)
            {
                N.b_4_1 = Convert.ToBoolean(FC.Get("rbut_1"));
                N.b_4_2 = Convert.ToBoolean(FC.Get("rbut_2"));
                N.JoinGroupType = N.b_4_2 ? 1 : 0;
                for (int i = 0; i < 2; i++)
                {
                    foreach (var _cJGW in N.cJGWs.Where(q => q.JoinType == (i + 1)).OrderBy(q => q.SortNo))
                    {
                        _cJGW.SelectFalg = GetViewCheckBox(FC.Get("cbox_JoinGroupWish" + i));
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
                    N.AC.GroupType = N.GroupNo = FC.Get("txb_GroupNo");
                else
                    N.AC.GroupType = N.JoinGroupType == 0 ? "無意願" : "有意願";
            }
            #endregion
            return N;
        }

        public ActionResult Step4()
        {
            GetViewBag();
            cStep4 N = GetStep4(GetQueryStringInString("ACID"), null);

            return View(N);

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Step4(FormCollection FC)
        {
            GetViewBag();
            cStep4 N = GetStep4(GetQueryStringInString("ACID"), FC);
            if (FC != null)
            {
                var Js = DC.JoinGroupWish.Where(q => q.ACID == N.AC.ACID).ToList();
                foreach (var cJGW in N.cJGWs)
                {
                    var WNo = cJGW.ddl_Weekly.ddlList.FirstOrDefault(q => q.Selected);
                    var TNo = cJGW.ddl_Time.ddlList.FirstOrDefault(q => q.Selected);
                    var JGW = Js.FirstOrDefault(q => q.JoinType == cJGW.JoinType && q.SortNo == cJGW.SortNo);
                    if (JGW != null)
                    {
                        JGW.WeeklyNo = Convert.ToInt32(WNo.Value);
                        JGW.TimeNo = Convert.ToInt32(TNo.Value);
                    }
                    else
                    {
                        JGW = new JoinGroupWish
                        {
                            ACID = N.AC.ACID,
                            JoinType = cJGW.JoinType,
                            SortNo = cJGW.SortNo,
                            WeeklyNo = Convert.ToInt32(WNo.Value),
                            TimeNo = Convert.ToInt32(TNo.Value)
                        };
                        DC.JoinGroupWish.InsertOnSubmit(JGW);
                    }
                    DC.SubmitChanges();
                }
                SetAlert("", 1, "/Web/AccountAdd/Step5?ACID=" + N.sACID);
            }
            else
                SetAlert("發生錯誤,將返回第一步驟", 2, "/Web/AccountAdd/Step1?ACID=" + N.sACID);
            return View(N);

        }
        #endregion
        #region 註冊會員5
        public ActionResult Step5()
        {
            GetViewBag();
            string sACID = GetQueryStringInString("ACID");
            int iACID = 0;
            try
            {
                iACID = Convert.ToInt32(HSM.Des_1(sACID));
                var AC = DC.Account.FirstOrDefault(q => q.ACID == iACID);
                if (AC != null)
                {
                    AC.ActiveFlag = true;
                    AC.UpdDate = DT;
                    AC.SaveACID = AC.ACID;
                    DC.SubmitChanges();
                }
                else
                    SetAlert("註冊完成,但因為遺失註冊會員資料無法啟用帳戶,請通知管理員協助處理", 2);
            }
            catch
            {
                SetAlert("註冊完成,但因為遺失註冊會員資料無法啟用帳戶,請通知管理員協助處理", 2);
            }
            return View();

        }
        #endregion
    }
}