using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
        #region API_1 牧養組織(三層)
        public class Return1
        {
            public string Status = "Error";
            public string ErrorMsg = "";
            public List<cItem> Items = new List<cItem>();
        }
        public class cItem
        {
            public string Title = "";
            public string Value = "";
            public List<cItem> Items = new List<cItem>();
        }

        [HttpGet]
        public string API_1()//牧養組織(三層)
        {
            Return1 R = new Return1();
            if (CheckJWT(1))
            {
                R.Status = "OK";
                R.Items = new List<cItem>();
                //從旌旗開始
                var OI_1s = DC.OrganizeInfo.Where(q => q.OID == 2 && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.OIID);
                foreach (var OI_1 in OI_1s)
                {
                    cItem c1 = new cItem();
                    c1.Title = OI_1.Title + OI_1.Organize.Title;
                    c1.Value = "";
                    c1.Items = new List<cItem>();
                    var OI_2s = DC.OrganizeInfo.Where(q => q.ParentID == OI_1.OIID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.OIID);
                    foreach (var OI_2 in OI_2s)
                    {
                        cItem c2 = new cItem();
                        c2.Title = OI_2.Title + OI_2.Organize.Title;
                        c2.Value = "";
                        c2.Items = new List<cItem>();
                        var OI_3s = DC.OrganizeInfo.Where(q => q.ParentID == OI_2.OIID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.OIID);
                        foreach (var OI_3 in OI_3s)
                        {
                            cItem c3 = new cItem();
                            c3.Title = OI_3.Title + OI_3.Organize.Title;
                            if (OI_3.ACID > 1)
                                c3.Value = OI_3.Account.Name_First + OI_3.Account.Name_Last + " " + OI_3.Organize.JobTitle;
                            else
                                c3.Value = "--";
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
        #region API_2 牧養身份(三層)


        public string API_2()//牧養身份(三層)
        {
            Return1 R = new Return1();
            if (CheckJWT(2))
            {
                R.Status = "OK";
                R.Items = new List<cItem>();
                


            }
            else
                R.ErrorMsg = "您不具備查詢此項目的許可";
            sReturn = JsonConvert.SerializeObject(R);
            return sReturn;
        }
        #endregion
        #region API_3 聚會點組織(三層)
        public string API_3()//聚會點組織(三層)
        {
            Return1 R = new Return1();
            if (CheckJWT(3))
            {
                R.Status = "OK";
                R.Items = new List<cItem>();

                var Ls = DC.Location.Where(q => q.TargetType == 3).ToList();
                var Z10s = DC.ZipCode.Where(q => q.ParentID == 10 && q.ActiveFlag).ToList();
                //從旌旗開始
                var OI_1s = DC.OrganizeInfo.Where(q => q.OID == 1 && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.OIID);
                foreach (var OI_1 in OI_1s)
                {
                    cItem c1 = new cItem();
                    c1.Title = OI_1.Title + OI_1.Organize.Title;
                    c1.Value = "";
                    c1.Items = new List<cItem>();
                    var OI_2s = DC.OrganizeInfo.Where(q => q.ParentID == OI_1.OIID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.OIID);
                    foreach (var OI_2 in OI_2s)
                    {
                        cItem c2 = new cItem();
                        c2.Title = OI_2.Title + OI_2.Organize.Title;
                        c2.Value = "";
                        c2.Items = new List<cItem>();
                        var MLSs = DC.Meeting_Location_Set.Where(q => q.OIID == OI_2.OIID && q.ActiveFlag && !q.DeleteFlag && q.SetType==0).OrderBy(q => q.OIID);
                        foreach (var MLS in MLSs)
                        {
                            cItem c3 = new cItem();
                            c3.Title = MLS.Meeting_Location.Title;
                            c3.Value = "--";
                            var L = Ls.FirstOrDefault(q => q.TargetID == MLS.MLID);
                            if(L!=null)
                            {
                                var Z10 = Z10s.FirstOrDefault(q => q.ZID == L.ZipCode.ParentID);
                                c3.Value = (Z10!=null ? Z10.Title : "") + L.ZipCode.Title+L.Address;
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