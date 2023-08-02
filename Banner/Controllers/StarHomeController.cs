using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;

namespace Banner.Controllers
{
    public class StarHomeController : PublicClass
    {
        // GET: Home
        public ActionResult Index()
        {
            return View();
        }

        #region 新增資料
        public void SetAC()
        {
            DateTime DT_ = Convert.ToDateTime("2023/7/1");
            int OID = GetQueryStringInInt("OID");
            int RID = GetQueryStringInInt("RID");
            if (OID > 0 && RID > 0)
            {
                //管理者類
                for (int i = 1; i < 4; i++)
                {
                    var O = DC.Organize.FirstOrDefault(q => q.OID == OID);
                    var R = DC.Rool.FirstOrDefault(q => q.RID == RID);
                    string sLogin = "Test" + O.OID.ToString().PadLeft(2, '0') + i.ToString().PadLeft(2, '0');
                    string Phone = "0912345678";
                    Account AC = new Account
                    {
                        Login = sLogin,
                        Password = HSM.Enc_1(sLogin),
                        Name_First = "-",
                        Name_Last = O.JobTitle + i,
                        ManFlag = i % 2 == 0,
                        IDNumber = "",
                        IDType = 0,
                        Birthday = DT_.AddYears(-23),
                        EducationType = 0,
                        JobType = 0,
                        MarriageType = 0,
                        MarriageNote = "",
                        BaptizedType = 1,
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
                        //案例
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

                        //主管
                        var OI_ = DC.OrganizeInfo.Where(q => !q.DeleteFlag && q.OID == O.OID && q.ACID == 2).OrderBy(q => q.OIID).FirstOrDefault();
                        if (OI_ != null)
                        {
                            OI_.Account = AC;
                            AC.GroupType = OI_.OIID.ToString().PadLeft(5, '0');

                            M_OI_Account MOI = new M_OI_Account
                            {
                                OrganizeInfo = OI_,
                                Account = AC,
                                JoinDate = DT_.AddDays(1),
                                LeaveDate = DT_,
                                ActiveFlag = true,
                                DeleteFlag = false,
                                CreDate = DT_,
                                UpdDate = DT_,
                                SaveACID = 1

                            };
                            DC.M_OI_Account.InsertOnSubmit(MOI);
                            DC.SubmitChanges();
                        }

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
                    /*var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == 1982 + i);
                    if(OI!=null)
                    {
                        OI.Account = AC;
                        DC.SubmitChanges();
                    }*/

                }
            }
            else
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int i = 1; i < 7; i++)
                    {
                        var R = DC.Rool.FirstOrDefault(q => q.RID == 1);
                        string sLogin = "User00" + i.ToString().PadLeft(2, '0');
                        string Phone = "0912345678";
                        Account AC = new Account
                        {
                            Login = sLogin,
                            Password = HSM.Enc_1(sLogin),
                            Name_First = "-",
                            Name_Last = "會員" + (i + (j*6)),
                            ManFlag = i % 2 == 0,
                            IDNumber = "",
                            IDType = 0,
                            Birthday = DT_.AddYears(-23),
                            EducationType = 0,
                            JobType = 0,
                            MarriageType = j,
                            MarriageNote = "",
                            BaptizedType = 0,
                            GroupType = j == 0 ? "無意願" : (i < 4 ? "有意願" : "00534"),
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

                        if (j > 0)
                        {
                            M_OI_Account MOI = new M_OI_Account
                            {
                                OIID = i < 4 ? 1 : 534,
                                Account = AC,
                                JoinDate = DT_,
                                LeaveDate = DT_,
                                ActiveFlag = true,
                                DeleteFlag = false,
                                CreDate = DT_,
                                UpdDate = DT_,
                                SaveACID = 1

                            };
                            DC.M_OI_Account.InsertOnSubmit(MOI);
                            DC.SubmitChanges();
                        }

                    }
                }
            }

        }
        #endregion
    }
}