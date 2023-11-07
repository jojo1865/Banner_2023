using Banner.Models;
using OfficeOpenXml.ConditionalFormatting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace Banner.Areas.Web.Controllers
{
    public class ClassStoreController : PublicClass
    {
        // GET: Web/ClassStore
        #region 首頁
        public class cIndex
        {
            public List<Product> Ps_Classical = new List<Product>();//經典課程
            public List<Product> Ps_News = new List<Product>();//最新課程
            public List<Product> Ps_Close = new List<Product>();//即將結束報名課程
        }
        public class c_TempProduct
        {
            public Product P = null;
            public int iDay = 0;
        }
        public cIndex GetIndex()
        {
            var N = new cIndex();
            var Ps = DC.Product.Where(q => q.ActiveFlag &&
            !q.DeleteFlag &&
            q.Course.ActiveFlag &&
            !q.Course.DeleteFlag &&
            q.ShowFlag
            );
            //過濾使用者所屬旌旗
            /*ACID = GetACID();
            if (ACID != 1)//非管理者
            {
                Ps = from q in DC.v_GetAC_O2_OI.Where(q => q.ACID == ACID)
                     join p in Ps
                     on q.OIID equals p.OIID
                     select p;
            }*/

            //經典課程
            var Ps_ = Ps.Where(q => q.Course.ClassicalFlag).OrderByDescending(q => q.UpdDate).Take(12);
            foreach (var P_ in Ps_)
                N.Ps_Classical.Add(P_);

            //最新課程
            Ps_ = Ps.OrderByDescending(q => q.UpdDate).Take(12);
            foreach (var P_ in Ps_)
                N.Ps_News.Add(P_);

            //即將結束報名課程
            List<c_TempProduct> c_TP = new List<c_TempProduct>();
            //找臨櫃
            Ps_ = Ps.Where(q => (q.EDate_Signup_OnSite > q.CreDate && q.EDate_Signup_OnSite > DT.Date)
            ).OrderBy(q => (DT.Date - q.EDate_Signup_OnSite)).Take(12);
            foreach (var P_ in Ps_)
            {
                c_TP.Add(new c_TempProduct
                {
                    P = P_,
                    iDay = (DT.Date - P_.EDate_Signup_OnSite).Days
                });
            }
            //找線上
            Ps_ = Ps.Where(q => (q.EDate_Signup_OnLine > q.CreDate && q.EDate_Signup_OnLine > DT.Date)
            ).OrderBy(q => (DT.Date - q.EDate_Signup_OnLine)).Take(12);
            foreach (var P_ in Ps_)
            {
                if (!c_TP.Any(q => q.P.PID == P_.PID))//排除重複
                {
                    c_TP.Add(new c_TempProduct
                    {
                        P = P_,
                        iDay = (DT.Date - P_.EDate_Signup_OnLine).Days
                    });
                }
            }
            //排序剩餘天數後塞入物件
            foreach (var P_ in c_TP.OrderBy(q => q.iDay).Take(12))
                N.Ps_Close.Add(P_.P);
            return N;
        }
        [HttpGet]
        public ActionResult Index()
        {
            GetViewBag();
            return View(GetIndex());
        }
        #endregion
        #region 搜尋

        public class cProduct_Search
        {
            public List<Product> Ps = new List<Product>();
        }
        public cProduct_Search GetProduct_Search(FormCollection FC)
        {
            cProduct_Search c = new cProduct_Search();
            string sKey = GetQueryStringInString("Key");
            int iType = GetQueryStringInInt("Type");
            var Ns = DC.Product.Where(q => !q.DeleteFlag);
            if (sKey != "")
                Ns = Ns.Where(q => q.Course.Title.Contains(sKey) || q.SubTitle.Contains(sKey) || q.Title.Contains(sKey));
            switch (iType)
            {
                case 1://更多經典課程
                    {
                        c.Ps = Ns.Where(q => q.Course.ClassicalFlag).OrderByDescending(q => q.UpdDate).ToList();
                    }
                    break;
                case 2://更多最新課程
                    {
                        c.Ps = Ns.OrderByDescending(q => q.UpdDate).ToList();
                    }
                    break;
                case 3://更多即將結束報名課程
                    {
                        List<c_TempProduct> c_TP = new List<c_TempProduct>();
                        //找臨櫃
                        var Ps_ = Ns.Where(q => (q.EDate_Signup_OnSite > q.CreDate && q.EDate_Signup_OnSite > DT.Date)
                        ).OrderBy(q => (DT.Date - q.EDate_Signup_OnSite)).Take(25);
                        foreach (var P_ in Ps_)
                        {
                            c_TP.Add(new c_TempProduct
                            {
                                P = P_,
                                iDay = (DT.Date - P_.EDate_Signup_OnSite).Days
                            });
                        }
                        //找線上
                        Ps_ = Ns.Where(q => (q.EDate_Signup_OnLine > q.CreDate && q.EDate_Signup_OnLine > DT.Date)
                        ).OrderBy(q => (DT.Date - q.EDate_Signup_OnLine)).Take(25);
                        foreach (var P_ in Ps_)
                        {
                            if (!c_TP.Any(q => q.P.PID == P_.PID))//排除重複
                            {
                                c_TP.Add(new c_TempProduct
                                {
                                    P = P_,
                                    iDay = (DT.Date - P_.EDate_Signup_OnLine).Days
                                });
                            }
                        }
                        //排序剩餘天數後塞入物件
                        foreach (var P_ in c_TP.OrderBy(q => q.iDay))
                            c.Ps.Add(P_.P);
                    }
                    break;

                default:
                    {
                        c.Ps = Ns.OrderByDescending(q => q.CreDate).ToList();
                    }
                    break;
            }


            return c;
        }
        [HttpGet]
        public ActionResult Product_Search()
        {
            GetViewBag();
            return View(GetProduct_Search(null));
        }
        [HttpPost]
        public ActionResult Product_Search(FormCollection FC)
        {
            GetViewBag();
            var c = GetProduct_Search(FC);
            return View(c);
        }
        #endregion
        #region 課程內頁
        public class cProduct_Info
        {
            public Product P = null;
            public string TeacherName = "";
            public cTableList cTL = new cTableList();
            public string[] sRool = new string[] { "", "", "", "", "", "" };
        }
        public cProduct_Info GETProduct_Info(int ID)
        {
            cProduct_Info c = new cProduct_Info();
            c.P = DC.Product.FirstOrDefault(q => q.PID == ID && !q.DeleteFlag);
            #region 取得師資
            var Ts = DC.M_Product_Teacher.Where(q => q.PID == ID).OrderBy(q => q.CreDate);
            foreach (var T in Ts)
            {
                string cTitle = T.Title;
                if (string.IsNullOrEmpty(cTitle))
                {
                    if (T.TID > 0)
                    {
                        var T_ = DC.Teacher.FirstOrDefault(q => q.TID == T.TID && q.ActiveFlag && !q.DeleteFlag);
                        if (T_ != null)
                            if (!string.IsNullOrEmpty(T_.Title))
                                cTitle = T_.Title;
                    }
                }
                c.TeacherName += (c.TeacherName == "" ? "" : "、") + cTitle;
            }
            #endregion
            #region 取得班級
            c.cTL.Rs = new List<cTableRow>();
            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "班級名稱" });
            TopTitles.Add(new cTableCell { Title = "上課日期" });
            TopTitles.Add(new cTableCell { Title = "上課時間" });
            TopTitles.Add(new cTableCell { Title = "上課地點" });
            TopTitles.Add(new cTableCell { Title = "人數限制" });
            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));

            var PCs = DC.Product_Class.Where(q => q.PID == ID).OrderBy(q => q.LoopFlag).ThenBy(q => q.WeeklyNo);
            foreach (var PC in PCs)
            {
                cTableRow cTR = new cTableRow();

                cTR.Cs.Add(new cTableCell { Value = PC.Title });//班級名稱
                string ClassDate = "";
                if (PC.LoopFlag)
                {
                    if (PC.WeeklyNo > 0)
                        ClassDate = "每周" + sWeeks[PC.WeeklyNo].Replace("星期", "");
                    else
                        ClassDate = "每周(未確定日期)";
                }
                else
                    ClassDate = PC.TargetDate.ToString(DateFormat);
                cTR.Cs.Add(new cTableCell { Value = ClassDate });//上課日期
                cTR.Cs.Add(new cTableCell { Value = PC.STime.Hours + ":" + PC.STime.Minutes + "~" + PC.ETime.Hours + ":" + PC.ETime.Minutes });//上課時間
                cTR.Cs.Add(new cTableCell { Value = PC.LocationName });//上課地點
                cTR.Cs.Add(new cTableCell { Value = (PC.PeopleCt == 0 ? "不限制" : "限" + PC.PeopleCt + "人") });//人數限制
                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion
            #region 取得規則
            foreach (var PR in DC.Product_Rool.Where(q => q.PID == ID).OrderBy(q => q.TargetType))
            {
                switch (PR.TargetType)
                {
                    case 0://0:先修課程ID[Course]/
                        {
                            var C = DC.Course.FirstOrDefault(q => q.CID == PR.TargetInt1);
                            if (C != null)
                                c.sRool[PR.TargetType] += (c.sRool[PR.TargetType] == "" ? "先修課程：" : "、") + C.Title;
                        }
                        break;

                    case 1://1:職分ID[OID]/
                        {
                            var O = DC.Organize.FirstOrDefault(q => q.OID == PR.TargetInt1);
                            if (O != null)
                                c.sRool[PR.TargetType] += (c.sRool[PR.TargetType] == "" ? "限定職分：" : "、") + O.JobTitle;
                        }
                        break;

                    case 2://2:性別/
                        {
                            if (PR.TargetType >= 0)
                                c.sRool[PR.TargetType] = "限定性別：" + (PR.TargetInt1 == 1 ? "男性" : "女性");
                        }
                        break;

                    case 3://3:年齡/
                        {
                            if (PR.TargetInt1 > 0 && PR.TargetInt2 > 0)
                                c.sRool[PR.TargetType] = "限定年齡：" + PR.TargetInt1 + "歲 ~ " + PR.TargetInt2 + "歲";
                        }
                        break;

                    case 4://4:事工團/
                        break;

                    case 5://5:指定會員ACID
                        {
                            if (c.sRool[PR.TargetType] == "")
                                c.sRool[PR.TargetType] = "限定特定會友";
                        }
                        break;
                }

            }
            #endregion
            return c;
        }
        [HttpGet]
        public ActionResult Product_Info(int ID)
        {
            GetViewBag();

            return View(GETProduct_Info(ID));
        }

        #endregion

        #region 購物車1
        public class cOrder_Step1
        {
            public int OHID = 0;
            public List<cProduct> cPs = new List<cProduct>();
        }
        public class cProduct
        {
            public int OPID;
            public int PID = 0;
            public string ImgURL = "";
            public string ClassType = "";//實體/虛擬課程
            public string Title = "";//商品名稱
            public int Price_Type = 0;
            public int Price_Basic = 0;
            public int Price_New = 0;
            public int CAID = 0;
            public ListSelect LS = new ListSelect();
        }

        public cOrder_Step1 GetOrder_Step1(FormCollection FC)
        {
            cOrder_Step1 c = new cOrder_Step1();
            ACID = GetACID();
            #region 資料庫導入

            var OPs = DC.Order_Product.Where(q => q.Order_Header.ACID == ACID && q.Order_Header.Order_Type == 0 && !q.Order_Header.DeleteFlag).OrderBy(q => q.OPID);
            foreach (var OP in OPs)
            {
                int[] iPrice = GetPrice(ACID, OP.Product);
                c.OHID = OP.OHID;
                cProduct cP = new cProduct();
                cP.OPID = OP.OPID;
                cP.PID = OP.Product.PID;
                cP.ImgURL = string.IsNullOrEmpty(OP.Product.ImgURL) ? "/Content/Image/CourseCategory_" + OP.Product.Course.CCID + ".jpg" : OP.Product.ImgURL;
                cP.ClassType = OP.Product.ProductType == 0 ? "實體+線上" : (OP.Product.ProductType == 1 ? "實體課程" : "線上課程");
                cP.Title = "【" + OP.Product.Course.Title + "】" + OP.Product.SubTitle;
                cP.Price_Basic = OP.Product.Price_Basic;
                cP.Price_Type = iPrice[0];
                cP.Price_New = iPrice[1];
                cP.CAID = iPrice[2];
                cP.LS = new ListSelect();
                cP.LS.Title = "開課班別";
                cP.LS.ControlName = "rbl_Class_" + OP.Product.PID;
                cP.LS.SortNo = 0;
                cP.LS.ddlList = new List<SelectListItem>();

                //有買這個商品且結帳完成的正常訂單,統計每個班級的人數
                var OP_Gs = (from q in DC.Order_Product.Where(q => !q.Order_Header.DeleteFlag && q.Order_Header.Order_Type == 2 && q.PID == OP.PID)
                             group q by new { q.PCID } into g
                             select new { g.Key.PCID, Ct = g.Count() }).ToList();

                var PCs = DC.Product_Class.Where(q => q.PID == OP.PID).OrderBy(q => q.PCID);
                foreach (var PC in PCs)
                {
                    SelectListItem SL = new SelectListItem();
                    SL.Value = PC.PCID.ToString();
                    
                    #region 課程文字
                    //A班：2023/9/14 09:00-12:00 | 台北台中高雄宜蘭
                    SL.Text = PC.Title + "：";
                    if (PC.LoopFlag)
                        SL.Text += "每周" + sWeeks[PC.WeeklyNo];
                    else
                        SL.Text = PC.TargetDate.ToString(DateFormat);
                    if (string.IsNullOrEmpty(PC.LocationName) && string.IsNullOrEmpty(PC.Address)) { }
                    else if (string.IsNullOrEmpty(PC.LocationName) && !string.IsNullOrEmpty(PC.Address))
                        SL.Text += " | " + PC.Address;
                    else if (!string.IsNullOrEmpty(PC.LocationName) && string.IsNullOrEmpty(PC.Address))
                        SL.Text += " | " + PC.LocationName;
                    else if (!string.IsNullOrEmpty(PC.LocationName) && !string.IsNullOrEmpty(PC.Address))
                        SL.Text += " | " + PC.LocationName + "(" + PC.Address + ")";
                    #endregion
                    #region 是否可以選課
                    var OP_G = OP_Gs.FirstOrDefault(q => q.PCID == PC.PCID);
                    if (OP_G != null)//這堂課有人買
                    {
                        if (PC.PeopleCt > 0 && OP_G.Ct >= PC.PeopleCt)//有限制人數 + 名額已滿
                        {
                            SL.Disabled = true;
                            if (PC.PCID == OP.PCID)//已額滿,所以把之前選擇的拿掉
                                OP.PCID = 0;
                        }
                    }

                    #endregion
                    SL.Selected = PC.PCID == OP.PCID;
                    cP.LS.ddlList.Add(SL);
                }

                if(OP.PCID == 0 || !cP.LS.ddlList.Any(q=>q.Selected))
                {
                    if (cP.LS.ddlList.Count(q => !q.Disabled) > 0)
                    {
                        cP.LS.ddlList.Where(q => !q.Disabled).OrderBy(q => q.Value).First().Selected = true;
                        string sPCID = cP.LS.ddlList.First(q => q.Selected).Value; 
                        OP.PCID = Convert.ToInt32(sPCID);
                        DC.SubmitChanges();
                    }
                        
                }
                c.cPs.Add(cP);
            }
            #endregion

            #region 前端載入




            #endregion



            return c;
        }
        [HttpGet]
        public ActionResult Order_Step1()
        {
            GetViewBag();
            return View(GetOrder_Step1(null));
        }
        [HttpPost]
        public ActionResult Order_Step1(FormCollection FC)
        {
            GetViewBag();
            var c = GetOrder_Step1(FC);
            return View(c);
        }

        #endregion
        #region 購物車2

        public class cOrder_Step2
        {

        }
        public cOrder_Step2 GetOrder_Step2(FormCollection FC)
        {
            cOrder_Step2 c = new cOrder_Step2();




            return c;
        }
        [HttpGet]
        public ActionResult Order_Step2()
        {
            GetViewBag();
            return View(GetOrder_Step2(null));
        }
        [HttpPost]
        public ActionResult Order_Step2(FormCollection FC)
        {
            GetViewBag();
            var c = GetOrder_Step2(FC);
            return View(c);
        }
        #endregion
    }
}