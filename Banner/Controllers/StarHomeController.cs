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

        public ActionResult Test()
        {
            GetViewBag();
            //加密測試
            string str = "MerchantID=MS127874575&RespondType=String&TimeStamp=1695795410&Version=2.0&MerchantOrderNo=Vanespl_ec_1695795410&Amt=30&ItemDesc=test&NotifyURL=https%3A%2F%2Fwebhook.site%2Fd4db5ad1-2278-466a-9d66-78585c0dbadb";
            sMerchantID = "MS127874575";//商店代號
            sHashKey = "Fs5cX1TGqYM2PpdbE14a9H83YQSQF5jn";
            sHashIV = "C6AcmfqJILwgnhIP";
            string Data1 = HSM.EncryptAESHex(str, sHashKey, sHashIV);

            ViewBag._Return1 = Data1;
            string Data2 = "HashKey=" + sHashKey + "&" + Data1 + "&HashIV=" + sHashIV;
            ViewBag._Return2 = Data2;
            ViewBag._Return3 = HSM.EncryptSHA256(Data2).ToUpper();



            //解密測試
            str = "cc65583f1cfef54e661efa4264cafff0c673276e4868b0ea033d2e5f1074cb4990c4628005cf542419b0ba41e5b7ce583c3a643f59d83f14afd52f5e566d6b24cbc49eb8fc33f811bbde0f11f1ec97e2e97295af49aa4721f06d7412db6165a1283d83bfe032af8e3cb97ef85335040d3e7046a25d4e225894884fe702377e490eac11cf738618206602d77f64fe48f70bb61edf5af00f609726416e82e31d956a48ec06f013bf88c51d761f399910b486235af6bb2638ee3f094b00b4489d81d7d2f275ee696df207cccd5afd1ab386a079ca64c4455b3b8e1bf1ca6d2427d05cb80413f37edbd3d8d7671c46de4a93e1d947e605479f2eadb8d1fed532e26dc40f27669f6fce2510d706adff56e8a9ba078ef81e90710ae3ede94c70fe5d9bfa271864058c61d754772336ddfb429205bc7fd87fddf16c8220615bcdfd058ae67590a256f28766e936e1a48c50b6aa10158829402a8f40f7516238bdee3a81fcb37d37f6e397743336abb33a632aa53272ccf9b216216480443fd3e5d3632bd7e170fec1a1522c506e805f7abb671e2974675120a91d24fb8556aafd03667a63c9ee39072c264d2cccbaa2014f55f09050687a0fb47faef94a523eeba7a7fa42e38efdc41c0d80c6cc6a734041d94c072afed6b569bf18c200adce53a433aafd8e7100f2eafd52d2abb18d2e7d6377ce0f8ed520f0840cbb0e3b8eaf2d4989f76ffe97973f0af0c636c20a17e3ec484042f883f0e7b618a1d24ce87f3738a122f33a05fea44b264c81d3a0acc92908334f380f6184e6c3a72300e9f59e76cd";
            //sHashKey = "AAAvw3YlqoEk6G4HqRKDAYpHKZWxBBB";
            //sHashIV = "AAAC1FplieBBBP";
            //ViewBag._Return2 = HSM.DecryptAESHex(str, sHashKey, sHashIV);
            //str = "f79eac33c4f3245d58f17b544c5d38b09457a6d77e77bae6f10fcc7236fe153ccef1a80001c0746afc063a7570f80ad970d8a32c72332c9ec5547410188007876bdca2bafa52d07d31b6b183f2204d6e4feee6d245e286ab198cf95422ad5843c7696fc943cbb65979ad207607d4b5d97dac4a90ccd5e7a37adb7d7062e838be09d94e8c5dfa145c048e17feabe58c2e310792f0f50f5af32961ffb07ff6649ae1021ad558242551de5f09316e3182e198775e5d1ad5b66a70be290004de750fa85d86b0c2f087b40005d89e048be2ab6fd83f1c522494c093426a10a1f73fe4";
            //ViewBag._Return2 = HSM.Des_AES256(HSM.HexStr2ASCII(str.ToUpper()), sHashKey, sHashIV);

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
                        LoginDate = DT_,
                        LogoutDate = DT_,
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
                            CreUID = 1,
                            UpdDate = DT_.AddDays(-1 * O.OID),
                            UpdUID = 1
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
                        ContectValue = Phone,
                        CheckFlag = false,
                        CreDate = DT,
                        CheckDate = DT
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
                            Name_Last = "會員" + (i + (j * 6)),
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
                            LoginDate = DT_,
                            LogoutDate = DT_,
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
                            ContectValue = Phone,
                            CheckFlag = false,
                            CreDate = DT,
                            CheckDate = DT
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