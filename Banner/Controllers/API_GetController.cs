using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Web;
using System.Web.Mvc;

namespace Banner.Controllers
{
    public class API_GetController : PublicClass
    {
        public string sReturn = "";
        // GET: API_Get
        public ActionResult Index()
        {
            /*string Code = "Banner_" + DT.ToString("yyyyMMddHHmmssfff") + "_" + GetRand(10000000);
            string JWT = SetJWT(1, Code);
            var T = DC.Token_Check.First(q => q.TCID == 1);
            if(T!=null)
            {
                T.CheckCode = T.TCID + "_" + Code;
                T.JWT = JWT;
                DC.SubmitChanges();
            }*/
            return View();
        }
        #region API_1 牧養組織
        public class Return1
        {
            public string Status = "Error";
            public string ErrorMsg = "";
            public List<cItem1> Items = new List<cItem1>();
        }
        public class cItem1
        {
            public string Title = "";
            public string JobTitle = "";
            public List<cItem1> Items = new List<cItem1>();
        }

        [HttpGet]
        public string API_1()//牧養組織
        {
            Return1 R = new Return1();
            if (CheckJWT(1))
            {
                R.Status = "OK";
                R.Items = new List<cItem1>();
                var Os = DC.Organize.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList();
                var OIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList();
                var O = Os.First(q => q.OID == 1);
                do
                {
                    var OI_s = OIs.Where(q => q.OID == O.OID).OrderBy(q => q.OIID).ToList();
                    foreach (var OI_ in OI_s)
                        R.Items.Add(GetItem1(ref OIs, OI_));

                    O = Os.FirstOrDefault(q => q.ParentID == O.OID);
                } while (O != null);
            }
            else
                R.ErrorMsg = "您不具備查詢此項目的許可";
            sReturn = JsonConvert.SerializeObject(R);
            return sReturn;
        }
        private cItem1 GetItem1(ref List<OrganizeInfo> OIs, OrganizeInfo OI)
        {
            cItem1 c = new cItem1();
            c.Title = OI.Title + OI.Organize.Title;
            if (OI.Organize.JobTitle == "")
                c.JobTitle = "";
            else if (OI.ACID == 1)
                c.JobTitle = "--";
            else
                c.JobTitle = OI.Account.Name_First + OI.Account.Name_Last + " " + OI.Organize.JobTitle;
            c.Items = new List<cItem1>();
            if (OI.OID != 8)
                foreach (var OI_ in OIs.Where(q => q.ParentID == OI.OIID).OrderBy(q => q.OIID).ToList())
                    c.Items.Add(GetItem1(ref OIs, OI_));
            return c;
        }

        #endregion
        #region API_2 牧養身份
        public class Return2
        {
            public string Status = "Error";
            public string ErrorMsg = "";
            public string Name = "";
            public List<cItem2> Items = new List<cItem2>();
        }
        public class cItem2
        {
            public string Title = "";
            public string JobTitle = "";
        }

        public string API_2()//牧養身份
        {
            Return2 R = new Return2();
            if (CheckJWT(2))
            {
                string sACID = HSM.Des_1(GetQueryStringInString("ID"));
                int iACID = 0;
                if (int.TryParse(sACID, out iACID))
                {
                    if (iACID > 1)
                    {
                        var AC = DC.Account.FirstOrDefault(q => q.ACID == iACID && q.ActiveFlag && !q.DeleteFlag);
                        if (AC == null)
                            R.ErrorMsg = "此會員不存在";
                        else
                        {
                            R.Status = "OK";
                            R.Name = AC.Name_First + AC.Name_Last;
                            R.Items = new List<cItem2>();
                            var OIs = DC.OrganizeInfo.Where(q => q.ACID == iACID && q.ActiveFlag && !q.DeleteFlag && q.Organize.ItemID == "Shepherding").OrderBy(q => q.OID).ThenBy(q => q.OIID);
                            foreach (var OI in OIs)
                                R.Items.Add(new cItem2 { Title = OI.Title + OI.Organize.Title, JobTitle = OI.Organize.JobTitle });

                            var MOIs = GetMOIAC(8, 0, AC.ACID);
                            foreach (var OI in MOIs)
                                R.Items.Add(new cItem2 { Title = OI.OrganizeInfo.Title + OI.OrganizeInfo.Organize.Title, JobTitle = "小組員" });
                        }
                    }
                    else
                        R.ErrorMsg = "此會員不被允許進行此查詢";
                }
                else
                    R.ErrorMsg = "參數傳遞錯誤";
            }
            else
                R.ErrorMsg = "您不具備查詢此項目的許可";
            sReturn = JsonConvert.SerializeObject(R);
            return sReturn;
        }
        #endregion
        #region API_3 聚會點組織
        public class Return3
        {
            public string Status = "Error";
            public string ErrorMsg = "";
            public List<cItem3> Items = new List<cItem3>();
        }
        public class cItem3
        {
            public string Title = "";
            public string Address = "";
            public List<cItem3> Items = new List<cItem3>();
        }
        public string API_3()//聚會點組織
        {
            Return3 R = new Return3();
            if (CheckJWT(3))
            {
                R.Status = "OK";
                R.Items = new List<cItem3>();

                var Ls = DC.Location.Where(q => q.TargetType == 3).ToList();
                var Z10s = DC.ZipCode.Where(q => q.ParentID == 10 && q.ActiveFlag).ToList();
                //從旌旗開始
                var OI_1s = DC.OrganizeInfo.Where(q => q.OID == 1 && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.OIID);
                foreach (var OI_1 in OI_1s)
                {
                    cItem3 c1 = new cItem3();
                    c1.Title = OI_1.Title + OI_1.Organize.Title;
                    c1.Address = "";
                    c1.Items = new List<cItem3>();
                    var OI_2s = DC.OrganizeInfo.Where(q => q.ParentID == OI_1.OIID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.OIID);
                    foreach (var OI_2 in OI_2s)
                    {
                        cItem3 c2 = new cItem3();
                        c2.Title = OI_2.Title + OI_2.Organize.Title;
                        c2.Address = "";
                        c2.Items = new List<cItem3>();
                        var MLSs = DC.Meeting_Location_Set.Where(q => q.OIID == OI_2.OIID && q.ActiveFlag && !q.DeleteFlag && q.SetType == 0).OrderBy(q => q.OIID);
                        foreach (var MLS in MLSs)
                        {
                            cItem3 c3 = new cItem3();
                            c3.Title = MLS.Meeting_Location.Title;
                            c3.Address = "--";
                            var L = Ls.FirstOrDefault(q => q.TargetID == MLS.MLID);
                            if (L != null)
                            {
                                var Z10 = Z10s.FirstOrDefault(q => q.ZID == L.ZipCode.ParentID);
                                c3.Address = (Z10 != null ? Z10.Title : "") + L.ZipCode.Title + L.Address;
                            }
                            c3.Items = null;
                            c2.Items.Add(c3);
                        }
                        c1.Items.Add(c2);
                    }
                    R.Items.Add(c1);
                }

            }
            else
                R.ErrorMsg = "您不具備查詢此項目的許可";
            sReturn = JsonConvert.SerializeObject(R);
            return sReturn;
        }
        #endregion
        #region API_4 事工團身份
        public string API_4()//事工團身份
        {
            return "";
        }
        #endregion
        #region API_5 小組出缺席紀錄
        public string API_5()//小組出缺席紀錄
        {
            return "";
        }
        #endregion
        #region API_6 事工團出席紀錄
        public string API_6()//事工團出席紀錄
        {
            return "";
        }
        #endregion
        #region API_7 課程歷程
        public string API_7()//課程歷程
        {
            return "";
        }
        #endregion
        #region API_8 屬靈健檢表
        public string API_8()//屬靈健檢表
        {
            return "";
        }
        #endregion
    }
}