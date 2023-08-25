using Banner.Models;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace Banner.Areas.Web.Controllers
{
    public class ChildrenAddController : PublicClass
    {
        // GET: Web/ChildrenAdd
        public ActionResult Index()
        {
            return View();
        }
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
            public bool ParentPhone = true;
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
                N.ParentPhone = Convert.ToBoolean(FC.Get("cbox_PhoneOwner"));
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
            if (GetACID() <= 0)
                SetAlert("請先登入", 4, "/Web/Home/Index");
            cStep1 N = GetStep1(HSM.Des_1(GetQueryStringInString("ACID")), null);

            return View(N);
        }
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Step1(FormCollection FC)
        {
            GetViewBag();
            cStep1 N = GetStep1(HSM.Des_1(GetQueryStringInString("ACID")), FC);

            bool bLogin = GetViewCheckBox(FC.Get("cbox_Login_Success"));
            bool bPW = GetViewCheckBox(FC.Get("cbox_PW_Success"));
            string sPhone = N.ParentPhone ? "OK" : CheckCellPhoneDouble(N.CellPhone);
            //string sEmail = CheckEmailDouble(N.Email);
            if (FC == null)
                SetAlert("無正確傳回,請重新填寫", 2);
            else if (sPhone != "OK")
                SetAlert(sPhone, 2);
            //else if (sEmail != "OK")
            //    SetAlert(sEmail, 2);
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
                        SaveACID = ACID,
                        OldID = 0
                    };
                    DC.Account.InsertOnSubmit(AC);
                    DC.SubmitChanges();

                    N.ACID = AC.ACID;

                    var Fs = DC.Family.Where(q => q.ACID == GetACID() && q.FamilyType == 3);
                    int iMax = (Fs.Count() > 0 ? Fs.Max(q => q.SortNo) : 0) + 1;

                    Family F = new Family
                    {
                        ACID = ACID,
                        Name = N.Name_F + N.Name_L,
                        IDNumber = "",
                        Login = N.Login,
                        FamilyType = 3,
                        FamilyTitle = "",
                        TargetACID = N.ACID,
                        SortNo = iMax,
                        DeleteFlag = false
                    };
                    DC.Family.InsertOnSubmit(F);
                    DC.SubmitChanges();

                    Contect Con = new Contect();
                    if (!N.ParentPhone)//兒童手機
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

                    //Email
                    /*Con = new Contect
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
                    */

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
                    AC.SaveACID = ACID;
                    AC.UpdDate = DT;
                    DC.SubmitChanges();

                    var F = DC.Family.FirstOrDefault(q => q.ACID == GetACID() && q.FamilyType == 3 && q.TargetACID == AC.ACID);
                    if (F == null)
                    {
                        var Fs = DC.Family.Where(q => q.ACID == GetACID() && q.FamilyType == 3);
                        int iMax = (Fs.Count() > 0 ? Fs.Max(q => q.SortNo) : 0) + 1;
                        F = new Family
                        {
                            ACID = ACID,
                            Name = N.Name_F + N.Name_L,
                            IDNumber = "",
                            Login = N.Login,
                            FamilyType = 3,
                            FamilyTitle = "",
                            TargetACID = N.ACID,
                            SortNo = iMax,
                            DeleteFlag = false
                        };
                        DC.Family.InsertOnSubmit(F);
                        DC.SubmitChanges();
                    }
                    else
                    {
                        F.Name = N.Name_F + N.Name_L;
                        F.Login = N.Login;
                        DC.SubmitChanges();
                    }

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

                    //Email
                    /*Con = DC.Contect.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == N.ACID && q.ContectType == 2);
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
                    }*/

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

                Response.Redirect("/Web/ChildrenAdd/Step2?ACID=" + HSM.Enc_1(N.ACID.ToString().PadLeft(5, '0')));
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
        public cStep2 SetStep2(string ACID, FormCollection FC)
        {
            cStep2 N = new cStep2();
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
            N.MLs.AddRange((from q in DC.M_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag && q.SetType == 0)
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
                                join p in DC.M_Location_Set.Where(q => q.SetType == 0 && !q.DeleteFlag && q.ActiveFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag)
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
                    SetAlert("註冊資料已遺失,請重新註冊", 2, "/Web/ChildrenAdd/Step1");
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
        public ActionResult Step2()
        {
            GetViewBag();
            cStep2 N = SetStep2(GetQueryStringInString("ACID"), null);
            return View(N);
        }
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Step2(FormCollection FC)
        {
            GetViewBag();
            cStep2 N = SetStep2(GetQueryStringInString("ACID"), FC);
            if (FC != null)
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
                var MLSs = from q in DC.M_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.SetType == 0 && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag)
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
                SetAlert("", 1, "/Web/ChildrenAdd/Step3?ACID=" + N.sACID);
            }
            else
                SetAlert("發生錯誤,將返回第一步驟", 2, "/Web/ChildrenAdd/Step1?ACID=" + N.sACID);
            return View(N);
        }
        #endregion
        #region 註冊會員3
        public ActionResult Step3()
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
                    AC.SaveACID = ACID;
                    DC.SubmitChanges();
                }
                else
                    SetAlert("註冊完成,但因為遺失兒童資料無法啟用兒童帳戶,請通知管理員協助處理", 2);
            }
            catch
            {
                SetAlert("註冊完成,但因為遺失兒童資料無法啟用兒童帳戶,請通知管理員協助處理", 2);
            }
            return View();

        }
        #endregion
    }
}