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
                            var OIs = DC.OrganizeInfo.Where(q => q.ACID == iACID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.OID).ThenBy(q => q.OIID);
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
        public class Return4
        {
            public string Status = "Error";
            public string ErrorMsg = "";
            public List<cItem4> Items = new List<cItem4>();
        }
        public class cItem4
        {
            public string OrganizeTitle = "";
            public string CategoryTitle = "";
            public string Title = "";
            public string JobTitle = "";
        }
        public string API_4(int ACID)//事工團身份
        {
            Return4 R = new Return4();
            if (CheckJWT(4))
            {
                string sACID = HSM.Des_1(GetQueryStringInString("ID"));
                int iACID = 0;
                if (int.TryParse(sACID, out iACID))
                {
                    var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID && !q.DeleteFlag);
                    if (AC != null)
                    {
                        R.Items = new List<cItem4>();

                        var Ms = DC.M_Staff_Account.Where(q => q.ACID == AC.ACID && q.Staff.ActiveFlag && !q.DeleteFlag && q.ActiveFlag && q.LeaveDate == q.CreDate);
                        if (Ms.Count() > 0)
                        {
                            R.Status = "OK";
                            foreach (var M in Ms.OrderBy(q => q.JoinDate))
                            {
                                R.Items.Add(new cItem4
                                {
                                    OrganizeTitle = M.OrganizeInfo.Title + M.OrganizeInfo.Organize.Title,
                                    CategoryTitle = M.Staff.Staff_Category.Title,
                                    Title = M.Staff.Title,
                                    JobTitle = M.LeaderFlag ? "主責" : (M.SubLeaderFlag ? "帶職主責" : "團員")
                                });
                            }
                        }
                        else
                            R.ErrorMsg = "此會員並未加入事工團員";
                    }
                    else
                        R.ErrorMsg = "此團員不存在";
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
        #region API_5 小組出缺席紀錄
        public class Return5
        {
            public string Status = "Error";
            public string ErrorMsg = "";
            public List<cItem5> Items = new List<cItem5>();
        }
        public class cItem5
        {
            public string OrganizeTitle = "";
            public string Date = "";
            public string JoinType = "";
        }
        public string API_5()//小組出缺席紀錄
        {
            Return5 R = new Return5();
            if (CheckJWT(5))
            {
                string sACID = HSM.Des_1(GetQueryStringInString("ID"));
                int iACID = 0;
                if (int.TryParse(sACID, out iACID))
                {
                    var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID && !q.DeleteFlag);
                    if (AC != null)
                    {
                        R.Items = new List<cItem5>();

                        var MOIs = DC.M_OI_Account.Where(q => q.ACID == AC.ACID).OrderBy(q => q.JoinDate);
                        foreach (var MOI in MOIs)
                        {
                            var EJHs = DC.Event_Join_Header.Where(q => q.TargetType == 2 && q.OIID == MOI.OIID).OrderBy(q => q.EventDate);
                            foreach (var EJH in EJHs)
                            {
                                R.Items.Add(new cItem5
                                {
                                    OrganizeTitle = MOI.OrganizeInfo.Title + MOI.OrganizeInfo.Organize.Title,
                                    Date = EJH.EventDate.ToString(DateFormat),
                                    JoinType = EJH.Event_Join_Detail.Any(q => q.ACID == AC.ACID && !q.DeleteFlag) ? "有參加" : "缺席"
                                });
                            }
                        }
                        if (MOIs.Count() == 0)
                            R.ErrorMsg = "此會員並未加入過任何小組";
                        else if (R.Items.Count == 0)
                            R.ErrorMsg = "此小組組員所加入的小組沒有任何小組聚會紀錄";
                        else
                            R.Items = R.Items.OrderByDescending(q => q.Date).ToList();
                    }
                    else
                        R.ErrorMsg = "此小組組員不存在";
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
        #region API_6 事工團出席紀錄
        public class Return6
        {
            public string Status = "Error";
            public string ErrorMsg = "";
            public List<cItem6> Items = new List<cItem6>();
        }
        public class cItem6
        {
            public string OrganizeTitle = "";
            public string Title = "";
            public string Date = "";
            public string JoinType = "";
        }
        public string API_6()//事工團出席紀錄
        {
            Return6 R = new Return6();
            if (CheckJWT(6))
            {
                string sACID = HSM.Des_1(GetQueryStringInString("ID"));
                int iACID = 0;
                if (int.TryParse(sACID, out iACID))
                {
                    var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID && !q.DeleteFlag);
                    if (AC != null)
                    {
                        R.Items = new List<cItem6>();

                        var Ms = DC.M_Staff_Account.Where(q => q.ACID == AC.ACID);

                        var EJHs = from q in Ms.GroupBy(q => q.SID)
                                   join p in DC.Event_Join_Header.Where(q => q.TargetType == 1)
                                   on q.Key equals p.TargetID
                                   select p;

                        foreach (var EJH in EJHs.OrderBy(q => q.EventDate))
                        {
                            var M = Ms.FirstOrDefault(q => q.JoinDate < EJH.EventDate && (q.LeaveDate.Date >= EJH.EventDate.Date || q.LeaveDate == q.CreDate) && !q.DeleteFlag);
                            if (M != null)//該員於該聚會日時正擔任團員
                            {
                                var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == EJH.OIID);
                                R.Items.Add(new cItem6
                                {
                                    OrganizeTitle = OI != null ? (OI.Title + OI.Organize.Title) : "",
                                    Title = M.Staff.Staff_Category.Title + " " + M.Staff.Title,
                                    Date = EJH.EventDate.ToString(DateFormat),
                                    JoinType = EJH.Event_Join_Detail.Any(q => q.ACID == AC.ACID && !q.DeleteFlag) ? "有參加" : "缺席"
                                });

                            }
                        }

                        R.Items = R.Items.OrderByDescending(q => q.Date).ToList();
                    }
                    else
                        R.ErrorMsg = "此團員不存在";
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
        #region API_7 課程歷程
        public class Return7
        {
            public string Status = "Error";
            public string ErrorMsg = "";
            public List<cItem7> Items = new List<cItem7>();
        }
        public class cItem7
        {
            public int OrderNo = 0;//訂單ID
            public string CourseTitle = "";//永久課程標題
            public string ProductTitle = "";//上架課程副標題
            public string Date = "";//第一次上課日期
            public string Graduation = "";//結業狀態
        }
        public string API_7()//課程歷程
        {
            Return7 R = new Return7();
            if (CheckJWT(7))
            {
                string sACID = HSM.Des_1(GetQueryStringInString("ID"));
                int iACID = 0;
                if (int.TryParse(sACID, out iACID))
                {
                    var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID && !q.DeleteFlag);
                    if (AC != null)
                    {
                        R.Items = new List<cItem7>();
                        var OPs = DC.Order_Product.Where(q =>
                                    q.Order_Header.ACID == AC.ACID &&
                                    !q.Order_Header.DeleteFlag &&
                                    q.Order_Header.Order_Type == 2
                            );
                      foreach(var OP in OPs)
                        {
                            cItem7 I7 = new cItem7();
                            I7.OrderNo = OP.OHID;
                            I7.CourseTitle = OP.Product.Course.Course_Category.Title + " " + OP.Product.Course.Title;
                            I7.ProductTitle = OP.Product.SubTitle;
                            I7.Graduation = OP.Graduation_Flag ? "已於" + OP.Graduation_Date.ToString(DateFormat) + "結業" : "尚未結業";
                            if (OP.Product_Class.Product_ClassTime.Count > 0)
                                I7.Date = OP.Product_Class.Product_ClassTime.Min(q => q.ClassDate).ToString(DateFormat);
                            else
                                I7.Date = "遺失上課日期";
                        }

                        R.Items = R.Items.OrderByDescending(q => q.OrderNo).ToList();
                    }
                    else
                        R.ErrorMsg = "此團員不存在";
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
        #region API_8 屬靈健檢表
        public class Return8
        {
            public string Status = "Error";
            public string ErrorMsg = "";
            public List<cItem8> Items = new List<cItem8>();
        }
        public class cItem8
        {
            public string Date = "";//第一次上課日期
            public string Type = "";//是否主日
            public int QTCt = 0;//當周靈修(QT)次數
        }
        public string API_8()//屬靈健檢表
        {
            Return8 R = new Return8();
            if (CheckJWT(8))
            {
                string sACID = HSM.Des_1(GetQueryStringInString("ID"));
                int iACID = 0;
                if (int.TryParse(sACID, out iACID))
                {
                    var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID && !q.DeleteFlag);
                    if (AC != null)
                    {
                        R.Items = new List<cItem8>();

                        DateTime MaxDate = DT.AddDays(-1 * ((int)DT.DayOfWeek));//最近一個星期天
                        var ASs = DC.Account_Spiritual.Where(q => q.ACID == AC.ACID).ToList();
                        for (int i = 0; i < 30; i++)
                        {
                            DateTime DT_ = MaxDate.AddDays(i * -7).Date;
                            var AS = ASs.FirstOrDefault(q => q.QTDate == DT_);

                            cItem8 I8 = new cItem8();
                            I8.Date = DT_.ToString(DateFormat);
                            if (AS != null)
                            {
                                I8.QTCt = AS.QTCt;
                                I8.Type = AS.QTFlag ? "true" : "false";
                            }
                            else
                            {
                                I8.QTCt = 0;
                                I8.Type = "null";
                            }

                            R.Items.Add(I8);
                        }

                        R.Items = R.Items.OrderByDescending(q => q.Date).ToList();
                    }
                    else
                        R.ErrorMsg = "此團員不存在";
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
    }
}