using Banner.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using OfficeOpenXml.ConditionalFormatting;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using WebGrease.Css.Ast.Selectors;

namespace Banner.Areas.Web.Controllers
{
    public class ClassStoreController : PublicClass
    {
        //測試預定值
        public string sStoreTitle = "JOJO測試店家";
        public string sNewebPagURL = "https://ccore.newebpay.com/MPG/mpg_gateway";//藍星金流網址
        public string sMerchantID = "MS151311400";//商店代號
        public string sHashKey = "xiI3upb6WXwkvJS7PrYPfGrb6gX2aAAh";
        public string sHashIV = "CHioAFLrrYhkcpYP";
        public string Email = "minto.momoko.jojo1865@gmail.com";
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

            //找線上
            Ps_ = Ps.Where(q => (q.EDate_Signup > q.CreDate && q.EDate_Signup > DT.Date)).OrderBy(q => (DT.Date - q.EDate_Signup)).Take(12);
            foreach (var P_ in Ps_)
            {
                if (!c_TP.Any(q => q.P.PID == P_.PID))//排除重複
                {
                    c_TP.Add(new c_TempProduct
                    {
                        P = P_,
                        iDay = (DT.Date - P_.EDate_Signup).Days
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

                        //找線上
                        var Ps_ = Ns.Where(q => (q.EDate_Signup > q.CreDate && q.EDate_Signup > DT.Date)
                        ).OrderBy(q => (DT.Date - q.EDate_Signup)).Take(25);
                        foreach (var P_ in Ps_)
                        {
                            if (!c_TP.Any(q => q.P.PID == P_.PID))//排除重複
                            {
                                c_TP.Add(new c_TempProduct
                                {
                                    P = P_,
                                    iDay = (DT.Date - P_.EDate_Signup).Days
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
            TopTitles.Add(new cTableCell { Title = "上課地點" });
            TopTitles.Add(new cTableCell { Title = "人數限制" });
            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));


            var PCs = DC.Product_Class.Where(q => q.PID == ID).OrderBy(q => q.Product_ClassTime.Min(p => p.ClassDate));
            foreach (var PC in PCs)
            {
                cTableRow cTR = new cTableRow();

                cTR.Cs.Add(new cTableCell { Value = PC.Title });//班級名稱
                string ClassDate = "";
                if (PC.Product_ClassTime.Any())
                {
                    var PCT_Min = PC.Product_ClassTime.Min(q => q.ClassDate);
                    var PCT_Max = PC.Product_ClassTime.Max(q => q.ClassDate);
                    if (PCT_Min.Date != PCT_Max)
                        ClassDate = PCT_Min.ToString(DateFormat) + "~" + PCT_Max.ToString(DateFormat);
                    else
                    {
                        var PCT = PC.Product_ClassTime.First();
                        ClassDate = PCT.ClassDate.ToString(DateFormat) + " " + PCT.STime.ToString("HH:mm") + "~" + PCT.ETime.ToString("HH:mm");
                    }
                }
                else
                    ClassDate = "尚無開課日期";
                cTR.Cs.Add(new cTableCell { Value = ClassDate });//上課日期
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
            public bool PayFlag = false;
            public List<cOICroup> cOIs = new List<cOICroup>();
        }
        public class cOICroup
        {
            public int OIID = 0;
            public string OITitle = "";
            public List<cPayType> cPTs = new List<cPayType>();
        }
        public class cPayType
        {
            public int PTID = 0;
            public string PayTitle = "";
            public int TotalPrice = 0;
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
            c.cOIs = new List<cOICroup>();
            var OIs = DC.OrganizeInfo.Where(q => q.OID == 1 && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.OIID);
            var PTs = DC.PayType.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList();
            foreach (var OI in OIs)
            {
                cOICroup cOIG = new cOICroup();
                cOIG.OIID = OI.OIID;
                cOIG.OITitle = OI.Title;
                cOIG.cPTs = new List<cPayType>();
                for (int i = 0; i < sPayType.Length; i++)
                {
                    var PT = PTs.FirstOrDefault(q => q.OIID == OI.OIID && q.PayTypeID == i);
                    if (PT != null)
                        cOIG.cPTs.Add(new cPayType { PTID = PT.PTID, PayTitle = sPayType[i], cPs = new List<cProduct>() });
                }
                c.cOIs.Add(cOIG);
            }
            //購物車中的商品
            var OPs = DC.Order_Product.Where(q => q.Order_Header.ACID == ACID && q.Order_Header.Order_Type == 0 && !q.Order_Header.DeleteFlag).OrderBy(q => q.OPID);

            //購物車中課程的付款方式
            var MPTs = (from q in OPs.GroupBy(q => q.PID)
                        join p in DC.M_Product_PayType.Where(q => q.ActiveFlag)
                        on q.Key equals p.PID
                        select p);
            int k = 0;
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
                cP.LS.SortNo = k++;
                cP.LS.ddlList = new List<SelectListItem>();

                //有買這個商品且結帳完成的正常訂單,統計每個班級的人數
                var OP_Gs = (from q in DC.Order_Product.Where(q => !q.Order_Header.DeleteFlag && q.Order_Header.Order_Type == 2 && q.PID == OP.PID)
                             group q by new { q.PCID } into g
                             select new { g.Key.PCID, Ct = g.Count() }).ToList();

                var PCs = DC.Product_Class.Where(q => q.PID == OP.PID && q.Product_ClassTime.Count() > 0).OrderBy(q => q.Product_ClassTime.Min(p => p.ClassDate));
                foreach (var PC in PCs)
                {
                    SelectListItem SL = new SelectListItem();
                    SL.Value = PC.PCID.ToString();

                    #region 課程文字
                    //A班：2023/9/14 09:00-12:00 | 台北台中高雄宜蘭
                    SL.Text = PC.Title + "：";

                    if (PC.Product_ClassTime.Any())
                    {
                        var PCT = PC.Product_ClassTime.OrderBy(q => q.ClassDate).First();
                        SL.Text += PCT.ClassDate.ToString(DateFormat) + " " + GetTimeSpanToString(PCT.STime);
                    }
                    else
                        SL.Text += "班級時間未定";
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

                if (OP.PCID == 0 || !cP.LS.ddlList.Any(q => q.Selected))
                {
                    if (cP.LS.ddlList.Count(q => !q.Disabled) > 0)
                    {
                        cP.LS.ddlList.Where(q => !q.Disabled).OrderBy(q => q.Value).First().Selected = true;
                        string sPCID = cP.LS.ddlList.First(q => q.Selected).Value;
                        OP.PCID = Convert.ToInt32(sPCID);
                        DC.SubmitChanges();
                    }

                }
                for (int j = 0; j < c.cOIs.Count; j++)
                {
                    for (int i = 0; i < c.cOIs[j].cPTs.Count; i++)
                    {
                        var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OID == 1 && q.OIID == OP.Product.OrganizeInfo.ParentID);
                        if (OI != null)
                        {
                            var PTs_OI = PTs.Where(q => q.OIID == OI.OIID).OrderBy(q => q.PayTypeID);
                            foreach (var PT in PTs_OI)
                            {
                                if (PT.PTID == c.cOIs[j].cPTs[i].PTID && c.cOIs[j].OIID == OI.OIID)
                                {
                                    if (MPTs.Any(q => q.PTID == PT.PTID && q.PID == cP.PID))//現金
                                        c.cOIs[j].cPTs[i].cPs.Add(cP);
                                }
                            }
                        }
                        if (c.cOIs[j].cPTs[i].cPs.Count > 0)
                            c.cOIs[j].cPTs[i].TotalPrice = c.cOIs[j].cPTs[i].cPs.Sum(q => q.Price_New);
                    }
                }
            }
            #endregion
            #region 前端載入
            if (FC != null)
            {
                c.PayFlag = true;
                var but = FC.AllKeys.FirstOrDefault(q => q.StartsWith("but_Next_"));
                if (but != null)
                {
                    int OIID = 0;
                    int PTID = 0;
                    string[] sID = but.Replace("but_Next_", "").Split('_');
                    int.TryParse(sID[0], out OIID);
                    int.TryParse(sID[1], out PTID);
                    var PT = DC.PayType.FirstOrDefault(q => q.PTID == PTID);
                    if (PT != null)
                    {
                        var OH_ = DC.Order_Header.FirstOrDefault(q => q.ACID == ACID && q.Order_Type == 0 && !q.DeleteFlag);
                        if (OH_ != null)
                        {
                            //建立訂單
                            /*var OH = DC.Order_Header.FirstOrDefault(q => q.OIID == OIID && q.ACID == OH_.ACID && q.Order_Type == 1 && !q.DeleteFlag && q.Order_Paid.Any(p => p.PTID == PT.PTID));
                            if (OH == null)
                            {
                                OH = new Order_Header
                                {
                                    OIID = OIID,
                                    ACID = OH_.ACID,
                                    Order_Type = 1,
                                    TotalPrice = 0,
                                    DeleteFlag = false,
                                    CreDate = DT,
                                    UpdDate = DT,
                                    SaveACID = ACID
                                };
                                DC.Order_Header.InsertOnSubmit(OH);
                                DC.SubmitChanges();
                            }*/
                            Order_Header OH = new Order_Header
                            {
                                OIID = OIID,
                                ACID = OH_.ACID,
                                Order_Type = 1,
                                TotalPrice = 0,
                                DeleteFlag = false,
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = ACID
                            };
                            DC.Order_Header.InsertOnSubmit(OH);
                            DC.SubmitChanges();

                            var OPs_ = from q in OPs.Where(q => q.OHID == OH_.OHID)
                                       join p in MPTs
                                       on q.PID equals p.PID
                                       select q;

                            foreach (var OP_ in OPs_)
                            {
                                string sPCID = FC.Get("rbl_Class_" + OP_.PID);
                                int iPCID = 0;
                                if (int.TryParse(sPCID, out iPCID))
                                {
                                    var cP_ = c.cOIs.First(q => q.OIID == OIID).cPTs.First(q => q.PTID == PTID).cPs.First(q => q.OPID == OP_.OPID);
                                    OP_.Order_Header = OH;
                                    OP_.PCID = iPCID;
                                    OP_.CAID = cP_.CAID;
                                    OP_.Price_Basic = cP_.Price_Basic;
                                    OP_.Price_Finally = cP_.Price_New;
                                    OP_.Price_Type = cP_.CAID > 0 ? 2 : (cP_.Price_Basic == cP_.Price_New ? 0 : 1);
                                    DC.SubmitChanges();
                                }
                            }
                            if (OH.Order_Product.Count() > 0)
                                OH.TotalPrice = OH.Order_Product.Sum(q => q.Price_Finally);

                            /*if (OH.Order_Paid.Count() == 0)
                            {
                                var OPA = new Order_Paid
                                {
                                    Order_Header = OH,
                                    PayType = PT,
                                    PaidFlag = false,
                                    PaidDateTime = DT,
                                    TradeNo = "",
                                    TradeAmt = OH.TotalPrice,
                                    PayAmt = 0,
                                    CreDate = DT,
                                    UpdDate = DT
                                };
                                DC.Order_Paid.InsertOnSubmit(OPA);
                            }
                            else if (OH.Order_Paid.First().PTID != PT.PTID)
                                OH.Order_Paid.First().PayType = PT;*/

                            var OPA = new Order_Paid
                            {
                                Order_Header = OH,
                                PayType = PT,
                                PaidFlag = false,
                                PaidDateTime = DT,
                                TradeNo = "",
                                TradeAmt = OH.TotalPrice,
                                PayAmt = 0,
                                CreDate = DT,
                                UpdDate = DT
                            };
                            DC.Order_Paid.InsertOnSubmit(OPA);
                            DC.SubmitChanges();
                            switch (PT.PayTypeID)
                            {
                                case 0://現金
                                    SetAlert("", 1, "/Web/ClassStore/Order_Step2/" + OH.OHID);
                                    break;

                                case 1://藍新-信用卡
                                    SetAlert("", 1, "/Web/ClassStore/Order_Paid_Credit_Card/" + OH.OHID);
                                    break;

                                case 2://藍新-ATM
                                    SetAlert("", 1, "/Web/ClassStore/Order_Paid_ATM/" + OH.OHID);
                                    break;

                                case 3://PayPel
                                    break;

                                case 4://支付寶
                                    break;
                            }
                        }
                    }
                }
            }
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
            return View(GetOrder_Step1(FC));
        }

        #endregion
        #region 購物車2-結束
        public class cOrder_Step2
        {
            public int OHID = 0;
            public int TotalPrice = 0;
            public List<cProduct> cPs = new List<cProduct>();
            public string PayType = "";//付款方式
            public string Status = "";//付款狀況
            public string Title1 = "";//相關資訊標題1
            public string Data1 = "";//相關資訊1
            public string Title2 = "";//相關資訊標題2
            public string Data2 = "";//相關資訊2
            public string Title3 = "";//相關資訊標題3
            public string Data3 = "";//相關資訊3
            public string Title4 = "";//相關資訊標題4
            public string Data4 = "";//相關資訊4
            public string Title5 = "";//相關資訊標題5
            public string Data5 = "";//相關資訊5
        }
        public cOrder_Step2 GetOrder_Step2(int ID, FormCollection FC)
        {
            cOrder_Step2 c = new cOrder_Step2();
            ACID = GetACID();
            var OH = DC.Order_Header.FirstOrDefault(q => q.OHID == ID && q.ACID == ACID && !q.DeleteFlag);
            if (OH == null)
                SetAlert("此訂單已刪除", 2, "/Web/ClassStore/Index");
            else
            {
                #region 組合前台確認項目
                c.TotalPrice = OH.TotalPrice;

                var P = DC.Order_Paid.FirstOrDefault(q => q.OHID == OH.OHID);
                if (P != null)
                {
                    if (P.PaidFlag)
                        c.Status = P.PaidDateTime.ToString(DateTimeFormat) + "付款成功";
                    else
                    {
                        #region 不同付款顯示
                        switch (P.PayType.PayTypeID)
                        {
                            case 0://現金
                                c.Status = "等待現金付款中";
                                break;

                            case 1://藍新-信用卡
                                c.Status = "信用卡交易中";
                                break;

                            case 2://藍新-ATM
                                {
                                    c.Status = "等待ATM付款中";
                                    var PD = P.Order_PaidDetail.FirstOrDefault(q => q.Title == "BankCode");
                                    if (PD != null)
                                    {
                                        c.Title1 = "匯款銀行代碼";
                                        c.Data1 = PD.Description;
                                    }
                                    PD = P.Order_PaidDetail.FirstOrDefault(q => q.Title == "CodeNo");
                                    if (PD != null)
                                    {
                                        c.Title2 = "匯款帳號";
                                        c.Data2 = PD.Description;
                                    }
                                    PD = P.Order_PaidDetail.FirstOrDefault(q => q.Title == "ExpireDate");
                                    if (PD != null)
                                    {
                                        c.Title3 = "匯款截止日";
                                        c.Data3 = PD.Description;
                                    }
                                }
                                break;

                            case 3://PayPel
                                break;

                            case 4://支付寶
                                break;
                        }
                        #endregion
                    }


                    if (sDomanName.Contains("viuto-aiot.com"))//測試環境
                    {
                        var PD = P.Order_PaidDetail.FirstOrDefault(q => q.Title == "PaymentType");
                        if (PD != null)
                        {
                            if (PD.Description == "VACC" && (DT - P.CreDate).Minutes < 10)//故意顯示ATM匯款資訊
                            {
                                c.Status = "等待ATM付款中";
                                PD = P.Order_PaidDetail.FirstOrDefault(q => q.Title == "BankCode");
                                if (PD != null)
                                {
                                    c.Title1 = "匯款銀行代碼";
                                    c.Data1 = PD.Description;
                                }
                                PD = P.Order_PaidDetail.FirstOrDefault(q => q.Title == "CodeNo");
                                if (PD != null)
                                {
                                    c.Title2 = "匯款帳號";
                                    c.Data2 = PD.Description;
                                }
                                PD = P.Order_PaidDetail.FirstOrDefault(q => q.Title == "ExpireDate");
                                if (PD != null)
                                {
                                    c.Title3 = "匯款截止日";
                                    c.Data3 = PD.Description;
                                }
                            }
                        }
                    }
                }
                else
                {
                    c.Status = "無交易類別,請通知旌旗處理";
                }
                #region 訂單內容顯示

                var OPs_ = OH.Order_Product.OrderBy(q => q.OPID);
                int k = 0;
                foreach (var OP_ in OPs_)
                {
                    cProduct cP = new cProduct();
                    cP.OPID = 0;
                    cP.PID = OP_.Product.PID;
                    cP.ImgURL = string.IsNullOrEmpty(OP_.Product.ImgURL) ? "/Content/Image/CourseCategory_" + OP_.Product.Course.CCID + ".jpg" : OP_.Product.ImgURL;
                    cP.ClassType = OP_.Product.ProductType == 0 ? "實體+線上" : (OP_.Product.ProductType == 1 ? "實體課程" : "線上課程");
                    cP.Title = "【" + OP_.Product.Course.Title + "】" + OP_.Product.SubTitle;
                    cP.Price_Basic = OP_.Product.Price_Basic;
                    cP.Price_Type = OP_.Price_Type;
                    cP.Price_New = OP_.Price_Finally;
                    cP.CAID = OP_.CAID;
                    cP.LS = new ListSelect();
                    cP.LS.Title = "開課班別";
                    cP.LS.ControlName = "rbl_Class_" + OP_.Product.PID;
                    cP.LS.SortNo = k++;
                    cP.LS.ddlList = new List<SelectListItem>();
                    var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == OP_.PCID);
                    if (PC != null)
                    {
                        SelectListItem SL = new SelectListItem();
                        SL.Value = PC.PCID.ToString();

                        #region 課程文字
                        //A班：2023/9/14 09:00-12:00 | 台北台中高雄宜蘭
                        SL.Text = PC.Title + "：";

                        if (PC.Product_ClassTime.Any())
                        {
                            var PCT = PC.Product_ClassTime.OrderBy(q => q.ClassDate).First();
                            SL.Text += PCT.ClassDate.ToString(DateFormat) + " " + GetTimeSpanToString(PCT.STime);
                        }
                        else
                            SL.Text += "班級時間未定";
                        if (string.IsNullOrEmpty(PC.LocationName) && string.IsNullOrEmpty(PC.Address)) { }
                        else if (string.IsNullOrEmpty(PC.LocationName) && !string.IsNullOrEmpty(PC.Address))
                            SL.Text += " | " + PC.Address;
                        else if (!string.IsNullOrEmpty(PC.LocationName) && string.IsNullOrEmpty(PC.Address))
                            SL.Text += " | " + PC.LocationName;
                        else if (!string.IsNullOrEmpty(PC.LocationName) && !string.IsNullOrEmpty(PC.Address))
                            SL.Text += " | " + PC.LocationName + "(" + PC.Address + ")";
                        #endregion
                        SL.Selected = true;
                        cP.LS.ddlList.Add(SL);
                    }
                    c.cPs.Add(cP);
                }
                #endregion
                #endregion
            }

            return c;
        }
        [HttpGet]
        public ActionResult Order_Step2(int ID)
        {
            GetViewBag();
            return View(GetOrder_Step2(ID, null));
        }
        [HttpPost]
        public ActionResult Order_Step2(int ID, FormCollection FC)
        {
            GetViewBag();
            var c = GetOrder_Step2(ID, FC);
            return View(c);
        }
        #endregion
        #region 將要付費的商品返還購物車(API)
        public void Order_RetrunCart()
        {
            int OHID = GetQueryStringInInt("OHID");
            var OH = DC.Order_Header.FirstOrDefault(q => q.OHID == OHID);
            if (OH != null)
            {
                var OH_0 = DC.Order_Header.FirstOrDefault(q => q.ACID == OH.ACID && q.Order_Type == 0);
                if (OH_0 != null)
                {
                    foreach (var OP in OH.Order_Product)
                    {
                        OP.OHID = OH_0.OHID;
                    }
                    OH_0.TotalPrice = 0;
                    OH.TotalPrice = 0;
                    DC.SubmitChanges();
                }
            }
        }
        #endregion
        #region 信用卡付款

        public class cOrder_Paid_Credit_Card
        {
            public string NewebPagURL = "";
            public string MerchantID = "";
            public string TradeInfo = "";
            public string TradeSha = "";
            public int TotalPrice = 0;
            public int OHID = 0;
            public List<cProduct> cPs = new List<cProduct>();
        }

        public cOrder_Paid_Credit_Card GetOrder_Paid_Credit_Card(int OHID)
        {
            string OrderInfo = "";

            ACID = GetACID();
            Error = "";
            cOrder_Paid_Credit_Card c = new cOrder_Paid_Credit_Card();
            var OH = DC.Order_Header.FirstOrDefault(q => q.OHID == OHID);
            if (OH == null)
                Error = "訂單不存在,無法付款";
            else if (OH.DeleteFlag)
                Error = "訂單已刪除";
            else if (OH.ACID != ACID)
                Error = "您並非此訂單擁有者,不能代為交易";
            else if (OH.Order_Type != 1)
                Error = "此訂單並非交易中訂單,請勿重複付款";

            if (Error != "")
                SetAlert(Error, 2, "/Web/ClassStore/Index");
            else
            {
                c.OHID = OH.OHID;
                var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == OH.OIID);
                if (OI != null)
                    OrderInfo = OI.Title + OI.Organize.Title;
                var OP = DC.Order_Paid.FirstOrDefault(q => q.OHID == OHID);
                if (OP == null)
                    Error = "此訂單缺少交易資訊,請回購物車重新選擇付款方式";
                else
                {
                    OP = DC.Order_Paid.FirstOrDefault(q => q.OHID == OHID && q.PayType.PayTypeID == 1);
                    if (OP == null)
                        Error = "此訂單並非選擇以信用卡付款";
                    else if (OP.PaidFlag)
                        Error = "此訂單已完成付款,請至您的訂單檢視";
                }

                if (Error != "")
                    SetAlert(Error, 2, "/Web/ClassStore/Order_Step1");
                else
                {
                    #region 組合前台確認項目
                    c.TotalPrice = OH.TotalPrice;
                    var OPs_ = OH.Order_Product.OrderBy(q => q.OPID);
                    int k = 0;
                    foreach (var OP_ in OPs_)
                    {
                        cProduct cP = new cProduct();
                        cP.OPID = OP.OPID;
                        cP.PID = OP_.Product.PID;
                        cP.ImgURL = string.IsNullOrEmpty(OP_.Product.ImgURL) ? "/Content/Image/CourseCategory_" + OP_.Product.Course.CCID + ".jpg" : OP_.Product.ImgURL;
                        cP.ClassType = OP_.Product.ProductType == 0 ? "實體+線上" : (OP_.Product.ProductType == 1 ? "實體課程" : "線上課程");
                        cP.Title = "【" + OP_.Product.Course.Title + "】" + OP_.Product.SubTitle;
                        cP.Price_Basic = OP_.Product.Price_Basic;
                        cP.Price_Type = OP_.Price_Type;
                        cP.Price_New = OP_.Price_Finally;
                        cP.CAID = OP_.CAID;
                        cP.LS = new ListSelect();
                        cP.LS.Title = "開課班別";
                        cP.LS.ControlName = "rbl_Class_" + OP_.Product.PID;
                        cP.LS.SortNo = k++;
                        cP.LS.ddlList = new List<SelectListItem>();
                        var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == OP_.PCID);
                        if (PC != null)
                        {
                            SelectListItem SL = new SelectListItem();
                            SL.Value = PC.PCID.ToString();

                            #region 課程文字
                            //A班：2023/9/14 09:00-12:00 | 台北台中高雄宜蘭
                            SL.Text = PC.Title + "：";

                            if (PC.Product_ClassTime.Any())
                            {
                                var PCT = PC.Product_ClassTime.OrderBy(q => q.ClassDate).First();
                                SL.Text += PCT.ClassDate.ToString(DateFormat) + " " + GetTimeSpanToString(PCT.STime);
                            }
                            else
                                SL.Text += "班級時間未定";
                            if (string.IsNullOrEmpty(PC.LocationName) && string.IsNullOrEmpty(PC.Address)) { }
                            else if (string.IsNullOrEmpty(PC.LocationName) && !string.IsNullOrEmpty(PC.Address))
                                SL.Text += " | " + PC.Address;
                            else if (!string.IsNullOrEmpty(PC.LocationName) && string.IsNullOrEmpty(PC.Address))
                                SL.Text += " | " + PC.LocationName;
                            else if (!string.IsNullOrEmpty(PC.LocationName) && !string.IsNullOrEmpty(PC.Address))
                                SL.Text += " | " + PC.LocationName + "(" + PC.Address + ")";
                            #endregion
                            SL.Selected = true;
                            cP.LS.ddlList.Add(SL);
                        }
                        c.cPs.Add(cP);
                    }
                    #endregion

                    //正式
                    if (sDomanName != "https://web-banner.viuto-aiot.com" && OP != null)
                    {
                        sMerchantID = OP.PayType.MerchantID;
                        sHashKey = OP.PayType.HashKey;
                        sHashIV = OP.PayType.HashIV;
                        sNewebPagURL = OP.PayType.TargetURL;
                        sStoreTitle = OP.PayType.Title;
                    }
                    var Con = DC.Contect.FirstOrDefault(q => q.TargetID == OH.ACID && q.TargetType == 2 && q.ContectType == 2);
                    if (Con != null)
                        Email = Con.ContectValue;
                    string str = "";
                    str += "MerchantID=" + sMerchantID;//MerchantID 商店代號 String(15)
                    str += "&RespondType=JSON";//RespondType 回傳格式 String(6)
                    str += "&TimeStamp=" + DT.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString().Split('.')[0];//TimeStamp 時間戳記 String(50)
                    str += "&Version=2.0";//Version 串接程式版本 String(5)
                    str += "&LangType=zh-tw";//LangType 語系 String(5)
                    str += "&MerchantOrderNo=" + "banner_" + OHID.ToString().PadLeft(10, '0') + "_" + DT.ToString("mmssfff");//MerchantOrderNo 商店訂單編號 String(30)
                    str += "&Amt=" + OH.TotalPrice;//Amt 訂單金額 int(10)
                    str += "&ItemDesc=" + OrderInfo + "課程共" + OH.Order_Product.Count() + "堂";//temDesc 商品資訊
                    str += "&TradeLimit=600";//TradeLimit 交易有效時間  Int(3)
                    str += "&ExpireDate=" + DT.AddDays(2).ToString("yyyyMMdd");//ExpireDate 繳費有效期限 String(10)
                    str += "&ReturnURL=" + sDomanName + "/Web/ClassStore/Order_Back_Credit_Card?OHID=" + OHID;//ReturnURL 支付完成返回商店網址 String(200)
                    //str += "&NotifyURL=" + sDomanName + "/Web/ClassStore/Order_Back_Paid?OHID=" + OHID;//NotifyURL 支付通知網址 String(200)
                    str += "&NotifyURL=";//信用卡就不用回傳了
                    //str += "&CustomerURL=" + sDomanName + "/Web/ClassStore/Order_GetNo/" + OHID;//CustomerURL 商店取號網址 String(200)
                    //4.2.3 回應參數-取號完成
                    //適用交易類別：超商代碼、超商條碼、超商取貨付款、ATM
                    str += "&CustomerURL=";

                    str += "&ClientBackURL=" + sDomanName + "/Web/ClassStore/Order_Back_Credit_Card?OHID=" + OHID;//ClientBackURL 返回商店網址 String(200)
                    str += "&Email=" + Email;//Email 付款人電子信箱 String(50)
                    str += "&EmailModify=1";//EmailModify 付款人電子信箱是否開放修改 Int(1) 1=可修改 0=不可修改
                    str += "&LoginType=0";//LoginType 藍新金流會員 0 = 不須登入藍新金流會員
                    str += "&OrderComment=";//OrderComment 商店備註 String(300)
                    str += "&CREDIT=1";//CREDIT 信用卡一次付清啟用 Int(1)
                    str += "&ANDROIDPAY=0";//ANDROIDPAY Google Pay 啟用 Int(1)
                    str += "&SAMSUNGPAY=0";//SAMSUNGPAY Samsung Pay 啟用 Int(1)
                    str += "&LINEPAY=0";//LINEPAY LINE Pay 啟用 Int(1)
                    str += "&ImageUrl=" + sDomanName + "/Content/Image/CourseCategory_1.jpg";//ImageUrl 產品圖檔連結網址 String(200)
                    str += "&InstFlag=0";//InstFlag 信用卡分期付款啟用 String(18)
                    str += "&CreditRed=0";//CreditRed 信用卡紅利啟用 Int(1)
                    str += "&UNIONPAY=0";//UNIONPAY 信用卡銀聯卡啟用 Int(1)
                    str += "&CREDITAE=0";//CREDITAE 信用卡美國運通卡啟用 Int(1)
                    str += "&WEBATM=0";//WEBATM WEBATM啟用 Int(1)
                    str += "&VACC=0";//VACC ATM轉帳啟用 Int(1)
                    str += "&BankType=";//BankType 金融機構 String(26)//先選台銀
                    str += "&CVS=0";//CVS 超商代碼繳費啟用 Int(1)
                    str += "&BARCODE=0";//BARCODE 超商條碼繳費啟用 Int(1)
                    str += "&ESUNWALLET=0";//ESUNWALLET 玉山Wallet Int(1)
                    str += "&TAIWANPAY=0";//TAIWANPAY 台灣Pay Int(1)
                    str += "&FULA=0";//FULA Fula付啦 String(20)
                    str += "&CVSCOM=0";//CVSCOM 物流啟用 Int(1)
                    str += "&EZPAY=0";//EZPAY 簡單付電子錢包 Int(1)
                    str += "&EZPWECHAT=0";//EZPWECHAT 簡單付微信支付 Int(1)
                    str += "&EZPALIPAY=0";//EZPALIPAY 簡單付支付寶 Int(1)
                    str += "&LgsType=B2C";//LgsType 物流型態 String(3)
                    c.NewebPagURL = sNewebPagURL;
                    c.MerchantID = sMerchantID;
                    c.TradeInfo = HSM.EncryptAESHex(str, sHashKey, sHashIV);
                    string str1 = "HashKey=" + sHashKey + "&" + c.TradeInfo + "&HashIV=" + sHashIV;
                    c.TradeSha = HSM.EncryptSHA256(str1).ToUpper();


                    var OPD = new Order_PaidDetail();
                    OPD.Order_Paid = OP;
                    OPD.OPDType = 0;
                    OPD.Title = "Data";
                    OPD.Description = str;
                    DC.Order_PaidDetail.InsertOnSubmit(OPD);
                    DC.SubmitChanges();

                    OPD = new Order_PaidDetail();
                    OPD.Order_Paid = OP;
                    OPD.OPDType = 0;
                    OPD.Title = "TradeInfo";
                    OPD.Description = c.TradeInfo;
                    DC.Order_PaidDetail.InsertOnSubmit(OPD);
                    DC.SubmitChanges();

                    OPD = new Order_PaidDetail();
                    OPD.Order_Paid = OP;
                    OPD.OPDType = 0;
                    OPD.Title = "TradeSha";
                    OPD.Description = c.TradeSha;
                    DC.Order_PaidDetail.InsertOnSubmit(OPD);
                    DC.SubmitChanges();
                }
            }
            return c;
        }
        [HttpGet]
        public ActionResult Order_Paid_Credit_Card(int ID)
        {
            GetViewBag();
            return View(GetOrder_Paid_Credit_Card(ID));
        }
        #endregion
        #region 信用卡返回
        [HttpPost]
        public ActionResult Order_Back_Credit_Card(FormCollection FC)
        {
            GetViewBag();
            Order_Header OH = null;
            int OHID = GetQueryStringInInt("OHID");
            int OPID = 0;
            ACID = GetACID();
            OH = DC.Order_Header.FirstOrDefault(q => q.OHID == OHID);
            if (OH != null)
            {
                if (FC != null)
                {
                    var OP = DC.Order_Paid.FirstOrDefault(q => q.OHID == OHID && q.PayType.PayTypeID == 1 && !q.PaidFlag);
                    if (OP != null)
                    {
                        OPID = OP.OPID;
                        var OPD = new Order_PaidDetail();
                        OPD.Order_Paid = OP;
                        OPD.OPDType = 1;
                        OPD.Title = "Status";
                        OPD.Description = FC.Get("Status");
                        DC.Order_PaidDetail.InsertOnSubmit(OPD);
                        DC.SubmitChanges();

                        if (OPD.Description == "SUCCESS")//付款完成
                        {
                            OH.Order_Type = 2;
                            OH.UpdDate = DT;

                            OP.PaidFlag = true;
                            OP.PaidDateTime = DT;
                            OP.PayAmt = OP.TradeAmt;
                            OP.UpdDate = DT;
                            DC.SubmitChanges();
                        }

                        OPD = new Order_PaidDetail();
                        OPD.Order_Paid = OP;
                        OPD.OPDType = 1;
                        OPD.Title = "MerchantID";
                        OPD.Description = FC.Get("MerchantID");
                        DC.Order_PaidDetail.InsertOnSubmit(OPD);
                        DC.SubmitChanges();

                        OPD = new Order_PaidDetail();
                        OPD.Order_Paid = OP;
                        OPD.OPDType = 1;
                        OPD.Title = "TradeInfo";
                        OPD.Description = FC.Get("TradeInfo");
                        DC.Order_PaidDetail.InsertOnSubmit(OPD);
                        DC.SubmitChanges();

                        OPD = new Order_PaidDetail();
                        OPD.Order_Paid = OP;
                        OPD.OPDType = 1;
                        OPD.Title = "TradeSha";
                        OPD.Description = FC.Get("TradeSha");
                        DC.Order_PaidDetail.InsertOnSubmit(OPD);
                        DC.SubmitChanges();

                        /*OPD = new Order_PaidDetail();
                        OPD.Order_Paid = OP;
                        OPD.OPDType = 1;
                        OPD.Title = "EncryptType";
                        OPD.Description = FC.Get("EncryptType");
                        DC.Order_PaidDetail.InsertOnSubmit(OPD);
                        DC.SubmitChanges();*/
                    }

                }
                if (ACID <= 0)
                {
                    LogInAC(OH.ACID);
                    SetBrowserData("UserName", OH.Account.Name_First + OH.Account.Name_Last);
                }

                SetAlert("", 2, "/Web/ClassStore/Order_Step2/" + OHID + "?OPID=" + OPID);
            }
            return View();
        }
        #endregion
        #region ATM付款

        public cOrder_Paid_Credit_Card GetOrder_Paid_ATM(int OHID)
        {
            string OrderInfo = "";

            ACID = GetACID();
            Error = "";
            cOrder_Paid_Credit_Card c = new cOrder_Paid_Credit_Card();
            var OH = DC.Order_Header.FirstOrDefault(q => q.OHID == OHID);
            if (OH == null)
                Error = "訂單不存在,無法付款";
            else if (OH.DeleteFlag)
                Error = "訂單已刪除";
            else if (OH.ACID != ACID)
                Error = "您並非此訂單擁有者,不能代為交易";
            else if (OH.Order_Type != 1)
                Error = "此訂單並非交易中訂單,請勿重複付款";

            if (Error != "")
                SetAlert(Error, 2, "/Web/ClassStore/Index");
            else
            {
                c.OHID = OH.OHID;
                var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == OH.OIID);
                if (OI != null)
                    OrderInfo = OI.Title + OI.Organize.Title;
                var OP = DC.Order_Paid.FirstOrDefault(q => q.OHID == OHID);
                if (OP == null)
                    Error = "此訂單缺少交易資訊,請回購物車重新選擇付款方式";
                else
                {
                    OP = DC.Order_Paid.FirstOrDefault(q => q.OHID == OHID && q.PayType.PayTypeID == 2);
                    if (OP == null)
                        Error = "此訂單並非選擇以ATM付款";
                    else if (OP.PaidFlag)
                        Error = "此訂單已完成付款,請至您的訂單檢視";
                }

                if (Error != "")
                    SetAlert(Error, 2, "/Web/ClassStore/Order_Step1");
                else
                {
                    #region 組合前台確認項目
                    c.TotalPrice = OH.TotalPrice;
                    var OPs_ = OH.Order_Product.OrderBy(q => q.OPID);
                    int k = 0;
                    foreach (var OP_ in OPs_)
                    {
                        cProduct cP = new cProduct();
                        cP.OPID = OP.OPID;
                        cP.PID = OP_.Product.PID;
                        cP.ImgURL = string.IsNullOrEmpty(OP_.Product.ImgURL) ? "/Content/Image/CourseCategory_" + OP_.Product.Course.CCID + ".jpg" : OP_.Product.ImgURL;
                        cP.ClassType = OP_.Product.ProductType == 0 ? "實體+線上" : (OP_.Product.ProductType == 1 ? "實體課程" : "線上課程");
                        cP.Title = "【" + OP_.Product.Course.Title + "】" + OP_.Product.SubTitle;
                        cP.Price_Basic = OP_.Product.Price_Basic;
                        cP.Price_Type = OP_.Price_Type;
                        cP.Price_New = OP_.Price_Finally;
                        cP.CAID = OP_.CAID;
                        cP.LS = new ListSelect();
                        cP.LS.Title = "開課班別";
                        cP.LS.ControlName = "rbl_Class_" + OP_.Product.PID;
                        cP.LS.SortNo = k++;
                        cP.LS.ddlList = new List<SelectListItem>();
                        var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == OP_.PCID);
                        if (PC != null)
                        {
                            SelectListItem SL = new SelectListItem();
                            SL.Value = PC.PCID.ToString();

                            #region 課程文字
                            //A班：2023/9/14 09:00-12:00 | 台北台中高雄宜蘭
                            SL.Text = PC.Title + "：";

                            if (PC.Product_ClassTime.Any())
                            {
                                var PCT = PC.Product_ClassTime.OrderBy(q => q.ClassDate).First();
                                SL.Text += PCT.ClassDate.ToString(DateFormat) + " " + GetTimeSpanToString(PCT.STime);
                            }
                            else
                                SL.Text += "班級時間未定";
                            if (string.IsNullOrEmpty(PC.LocationName) && string.IsNullOrEmpty(PC.Address)) { }
                            else if (string.IsNullOrEmpty(PC.LocationName) && !string.IsNullOrEmpty(PC.Address))
                                SL.Text += " | " + PC.Address;
                            else if (!string.IsNullOrEmpty(PC.LocationName) && string.IsNullOrEmpty(PC.Address))
                                SL.Text += " | " + PC.LocationName;
                            else if (!string.IsNullOrEmpty(PC.LocationName) && !string.IsNullOrEmpty(PC.Address))
                                SL.Text += " | " + PC.LocationName + "(" + PC.Address + ")";
                            #endregion
                            SL.Selected = true;
                            cP.LS.ddlList.Add(SL);
                        }
                        c.cPs.Add(cP);
                    }
                    #endregion

                    //正式
                    if (sDomanName != "https://web-banner.viuto-aiot.com" && OP != null)
                    {
                        sMerchantID = OP.PayType.MerchantID;
                        sHashKey = OP.PayType.HashKey;
                        sHashIV = OP.PayType.HashIV;
                        sNewebPagURL = OP.PayType.TargetURL;
                        sStoreTitle = OP.PayType.Title;
                    }
                    var Con = DC.Contect.FirstOrDefault(q => q.TargetID == OH.ACID && q.TargetType == 2 && q.ContectType == 2);
                    if (Con != null)
                        Email = Con.ContectValue;
                    string str = "";
                    str += "MerchantID=" + sMerchantID;//MerchantID 商店代號 String(15)
                    str += "&RespondType=JSON";//RespondType 回傳格式 String(6)
                    str += "&TimeStamp=" + DT.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString().Split('.')[0];//TimeStamp 時間戳記 String(50)
                    str += "&Version=2.0";//Version 串接程式版本 String(5)
                    str += "&LangType=zh-tw";//LangType 語系 String(5)
                    str += "&MerchantOrderNo=" + "banner_" + OHID.ToString().PadLeft(10, '0') + "_" + DT.ToString("mmssfff");//MerchantOrderNo 商店訂單編號 String(30)
                    str += "&Amt=" + OH.TotalPrice;//Amt 訂單金額 int(10)
                    str += "&ItemDesc=" + OrderInfo + "課程共" + OH.Order_Product.Count() + "堂";//temDesc 商品資訊
                    str += "&TradeLimit=600";//TradeLimit 交易有效時間  Int(3)
                    str += "&ExpireDate=" + DT.AddDays(2).ToString("yyyyMMdd");//ExpireDate 繳費有效期限 String(10)

                    str += "&ReturnURL=" + sDomanName + "/Web/ClassStore/Order_Back_ATM?OHID=" + OHID;//ReturnURL 支付完成返回商店網址 String(200)
                                                                                                      //str += "&ReturnURL=";

                    str += "&NotifyURL=" + sDomanName + "/Web/ClassStore/Order_Back_Paid?OHID=" + OHID;//NotifyURL 支付通知網址 String(200)
                                                                                                       //str += "&NotifyURL=";//信用卡就不用回傳了

                    str += "&CustomerURL=" + sDomanName + "/Web/ClassStore/Order_Back_ATM?OHID=" + OHID; ;//CustomerURL 商店取號網址 String(200)
                    //4.2.3 回應參數-取號完成
                    //適用交易類別：超商代碼、超商條碼、超商取貨付款、ATM
                    //str += "&CustomerURL=";

                    str += "&ClientBackURL=" + sDomanName + "/Web/ClassStore/Order_Back_ATM?OHID=" + OHID;//ClientBackURL 返回商店網址 String(200)
                    str += "&Email=" + Email;//Email 付款人電子信箱 String(50)
                    str += "&EmailModify=1";//EmailModify 付款人電子信箱是否開放修改 Int(1) 1=可修改 0=不可修改
                    str += "&LoginType=0";//LoginType 藍新金流會員 0 = 不須登入藍新金流會員
                    str += "&OrderComment=";//OrderComment 商店備註 String(300)
                    str += "&CREDIT=0";//CREDIT 信用卡一次付清啟用 Int(1)
                    str += "&ANDROIDPAY=0";//ANDROIDPAY Google Pay 啟用 Int(1)
                    str += "&SAMSUNGPAY=0";//SAMSUNGPAY Samsung Pay 啟用 Int(1)
                    str += "&LINEPAY=0";//LINEPAY LINE Pay 啟用 Int(1)
                    str += "&ImageUrl=" + sDomanName + "/Content/Image/CourseCategory_1.jpg";//ImageUrl 產品圖檔連結網址 String(200)
                    str += "&InstFlag=0";//InstFlag 信用卡分期付款啟用 String(18)
                    str += "&CreditRed=0";//CreditRed 信用卡紅利啟用 Int(1)
                    str += "&UNIONPAY=0";//UNIONPAY 信用卡銀聯卡啟用 Int(1)
                    str += "&CREDITAE=0";//CREDITAE 信用卡美國運通卡啟用 Int(1)
                    str += "&WEBATM=1";//WEBATM WEBATM啟用 Int(1)
                    str += "&VACC=1";//VACC ATM轉帳啟用 Int(1)
                    str += "&BankType=";//BankType 金融機構 String(26)//先選台銀
                    str += "&CVS=0";//CVS 超商代碼繳費啟用 Int(1)
                    str += "&BARCODE=0";//BARCODE 超商條碼繳費啟用 Int(1)
                    str += "&ESUNWALLET=0";//ESUNWALLET 玉山Wallet Int(1)
                    str += "&TAIWANPAY=0";//TAIWANPAY 台灣Pay Int(1)
                    str += "&FULA=0";//FULA Fula付啦 String(20)
                    str += "&CVSCOM=0";//CVSCOM 物流啟用 Int(1)
                    str += "&EZPAY=0";//EZPAY 簡單付電子錢包 Int(1)
                    str += "&EZPWECHAT=0";//EZPWECHAT 簡單付微信支付 Int(1)
                    str += "&EZPALIPAY=0";//EZPALIPAY 簡單付支付寶 Int(1)
                    str += "&LgsType=B2C";//LgsType 物流型態 String(3)
                    c.NewebPagURL = sNewebPagURL;
                    c.MerchantID = sMerchantID;
                    c.TradeInfo = HSM.EncryptAESHex(str, sHashKey, sHashIV);
                    string str1 = "HashKey=" + sHashKey + "&" + c.TradeInfo + "&HashIV=" + sHashIV;
                    c.TradeSha = HSM.EncryptSHA256(str1).ToUpper();


                    var OPD = new Order_PaidDetail();
                    OPD.Order_Paid = OP;
                    OPD.OPDType = 0;
                    OPD.Title = "Data";
                    OPD.Description = str;
                    DC.Order_PaidDetail.InsertOnSubmit(OPD);
                    DC.SubmitChanges();

                    OPD = new Order_PaidDetail();
                    OPD.Order_Paid = OP;
                    OPD.OPDType = 0;
                    OPD.Title = "TradeInfo";
                    OPD.Description = c.TradeInfo;
                    DC.Order_PaidDetail.InsertOnSubmit(OPD);
                    DC.SubmitChanges();

                    OPD = new Order_PaidDetail();
                    OPD.Order_Paid = OP;
                    OPD.OPDType = 0;
                    OPD.Title = "TradeSha";
                    OPD.Description = c.TradeSha;
                    DC.Order_PaidDetail.InsertOnSubmit(OPD);
                    DC.SubmitChanges();
                }
            }
            return c;
        }
        [HttpGet]
        public ActionResult Order_Paid_ATM(int ID)
        {
            GetViewBag();
            return View(GetOrder_Paid_ATM(ID));
        }
        #endregion
        #region ATM返回
        [HttpPost]
        public ActionResult Order_Back_ATM(FormCollection FC)
        {
            GetViewBag();
            Order_Header OH = null;
            int OHID = GetQueryStringInInt("OHID");
            int OPID = 0;
            ACID = GetACID();
            OH = DC.Order_Header.FirstOrDefault(q => q.OHID == OHID);
            if (OH != null)
            {
                if (FC != null)
                {
                    var OP = DC.Order_Paid.FirstOrDefault(q => q.OHID == OHID && q.PayType.PayTypeID == 2 && !q.PaidFlag);
                    Account_Note AN = new Account_Note
                    {
                        Account = OH.Account,
                        OIID = 0,
                        NoteType = -1,
                        Note = string.Join(", ", FC.AllKeys),
                        DeleteFlag = OP == null,
                        CreDate = DT,
                        UpdDate = DT,
                        SaveACID = 1
                    };
                    DC.Account_Note.InsertOnSubmit(AN);
                    DC.SubmitChanges();

                    if (OP != null)
                    {
                        OPID = OP.OPID;
                        var OPD = new Order_PaidDetail();
                        OPD.Order_Paid = OP;
                        OPD.OPDType = 1;
                        OPD.Title = "Status";
                        OPD.Description = FC.Get("Status");
                        DC.Order_PaidDetail.InsertOnSubmit(OPD);
                        DC.SubmitChanges();

                        if (OPD.Description == "SUCCESS")//付款完成
                        {
                            OH.Order_Type = 2;
                            OH.UpdDate = DT;

                            OP.PaidFlag = true;
                            OP.PaidDateTime = DT;
                            OP.PayAmt = OP.TradeAmt;
                            OP.UpdDate = DT;
                            DC.SubmitChanges();
                        }

                        OPD = new Order_PaidDetail();
                        OPD.Order_Paid = OP;
                        OPD.OPDType = 1;
                        OPD.Title = "MerchantID";
                        OPD.Description = FC.Get("MerchantID");
                        DC.Order_PaidDetail.InsertOnSubmit(OPD);
                        DC.SubmitChanges();

                        OPD = new Order_PaidDetail();
                        OPD.Order_Paid = OP;
                        OPD.OPDType = 1;
                        OPD.Title = "TradeInfo";
                        OPD.Description = FC.Get("TradeInfo");
                        DC.Order_PaidDetail.InsertOnSubmit(OPD);
                        DC.SubmitChanges();

                        string TradeInfo = OPD.Description;

                        /*OPD = new Order_PaidDetail();
                        OPD.Order_Paid = OP;
                        OPD.OPDType = 1;
                        OPD.Title = "TradeSha";
                        OPD.Description = FC.Get("TradeSha");
                        DC.Order_PaidDetail.InsertOnSubmit(OPD);
                        DC.SubmitChanges();*/

                        /*OPD = new Order_PaidDetail();
                        OPD.Order_Paid = OP;
                        OPD.OPDType = 1;
                        OPD.Title = "EncryptType";
                        OPD.Description = FC.Get("EncryptType");
                        DC.Order_PaidDetail.InsertOnSubmit(OPD);
                        DC.SubmitChanges();*/

                        string sInfo = HSM.DecryptAESHex(TradeInfo, sHashKey, sHashIV);
                        var J1 = ChangeJsonStringToClass(sInfo);
                        var J2 = ChangeJsonStringToClass(J1["Result"].ToString());

                        OP.TradeNo = J2["TradeNo"].ToString();//藍星交易編號
                        DC.SubmitChanges();

                        string PaymentType = J2["PaymentType"].ToString();//WEBATM / 其他
                        OPD = new Order_PaidDetail();
                        OPD.Order_Paid = OP;
                        OPD.OPDType = 1;
                        OPD.Title = "PaymentType";
                        OPD.Description = PaymentType;
                        DC.Order_PaidDetail.InsertOnSubmit(OPD);
                        DC.SubmitChanges();

                        if (PaymentType == "WEBATM")//線上ATM
                        {
                            OPD = new Order_PaidDetail();
                            OPD.Order_Paid = OP;
                            OPD.OPDType = 1;
                            OPD.Title = "PayerAccount5Code";
                            OPD.Description = J2["PayerAccount5Code"].ToString();//後五碼
                            DC.Order_PaidDetail.InsertOnSubmit(OPD);
                            DC.SubmitChanges();

                            OPD = new Order_PaidDetail();
                            OPD.Order_Paid = OP;
                            OPD.OPDType = 1;
                            OPD.Title = "PayBankCode";
                            OPD.Description = J2["PayBankCode"].ToString(); //付款銀行代碼809
                            DC.Order_PaidDetail.InsertOnSubmit(OPD);
                            DC.SubmitChanges();
                        }
                        else if (PaymentType == "VACC")//ATM轉帳
                        {
                            OPD = new Order_PaidDetail();
                            OPD.Order_Paid = OP;
                            OPD.OPDType = 1;
                            OPD.Title = "BankCode";
                            OPD.Description = J2["BankCode"].ToString();//付款銀行代碼
                            DC.Order_PaidDetail.InsertOnSubmit(OPD);
                            DC.SubmitChanges();

                            OPD = new Order_PaidDetail();
                            OPD.Order_Paid = OP;
                            OPD.OPDType = 1;
                            OPD.Title = "CodeNo";
                            OPD.Description = J2["CodeNo"].ToString();//付款銀行帳號
                            DC.Order_PaidDetail.InsertOnSubmit(OPD);
                            DC.SubmitChanges();

                            OPD = new Order_PaidDetail();
                            OPD.Order_Paid = OP;
                            OPD.OPDType = 1;
                            OPD.Title = "ExpireDate";
                            OPD.Description = J2["ExpireDate"].ToString() + " " + J2["ExpireTime"].ToString();//匯款截止日
                            DC.Order_PaidDetail.InsertOnSubmit(OPD);
                            DC.SubmitChanges();
                        }
                    }
                }
                if (ACID <= 0)
                {
                    LogInAC(OH.ACID);
                    SetBrowserData("UserName", OH.Account.Name_First + OH.Account.Name_Last);
                }

                SetAlert("", 2, "/Web/ClassStore/Order_Step2/" + OHID + "?OPID=" + OPID);
            }
            return View();
        }
        #endregion
        #region 付款資料回傳
        [HttpPost]
        public void Order_Back_Paid(FormCollection FC)
        {
            /*int OHID = GetQueryStringInInt("OHID");
            var OH = DC.Order_Header.FirstOrDefault(q => q.OHID == OHID);
            if (OH != null)
            {
                //若訂單存在且尚未有繳費成功紀錄
                if (!OH.Order_Paid.Any(q => q.PaidFlag))
                {
                    var OP = DC.Order_Paid.FirstOrDefault(q => q.OHID == OHID);
                    if (OP == null)
                    {
                        OP = new Order_Paid
                        {
                            Order_Header = OH,
                            PTID = 1,
                            PaidFlag = false,
                            PaidDateTime = DT,
                            TradeNo = "",
                            TradeAmt = OH.TotalPrice,
                            PayAmt = 0,
                            CreDate = DT,
                            UpdDate = DT
                        };
                        DC.Order_Paid.InsertOnSubmit(OP);
                        DC.SubmitChanges();
                    }
                }
            }*/

        }

        #endregion
        #region 測試
        [HttpGet]
        public void Test()
        {
            //string TradeInfo = "336518f8623afe2947e078ec89fa446900766cc94b564a3614ba38bdf207b07cab5fdb416a880bce7104aba68cd0cb22b9e87032b7f4b6dabb066dfa5af654674399207cd8a70785596bc76ffbae46021e3841bf44ce28b3b336db9b8a61467f3c138cb45aab4ca8e59ca8d132381a03dd06d6d0d0ebb93ccea8330b9008d2bb89878aae58106dced8f67a389ea61ca5484427a4a037fee97dfc2bad311ebcc8144bb1863dcc2d37f55bc69d62167aad874d14a57e42c203e2231d5cab644eab281b2215ec28c26f9ef372d82bee6701d87468af5be491cdbe16515370f068ea53e2e4ff07090428d1bd80c42082475c343191420ce87f734b127b7ef0b6be9f4be73e8c1040ef0a18525383c16c57bf238c4f2eba9d6e063fa540acb498b4696ec25e7b1b515e4ace896f3d87d747ec14b5ca88cb56b422578e6e329113d5719ecb554916773da26079b0716c028edf3f48332727c52deb9898c934a9a9f078c00a40f644168d7df0af1bbde4b2a3b7350aa0491dbbc5d4c025e6582fae5bdc049df745ca00d1240c5d9265264142ebbafe50600c98219e65231db6b9f3adf047e54820ce26b6addcd7f08a484ac0d00a45fe9315463bed4b52acea70f0b5c3bad01051ea55cc5b8d17394f31d4f966ae66d08058ff5361dfa4cad32d92ed17";
            string TradeInfo = "336518f8623afe2947e078ec89fa446900766cc94b564a3614ba38bdf207b07c3f005f94a09ba3842ae762c914f16b8dea152c00825475ad7722980a219509b938da02e6bb97547f75eedd461de31a79a6e5610d02cc041c3ffb7d59efa9c0ee1f6d360e456373d9e2ea30591666b01a65923db3e6c010c6c143ba9d945e23ac5dc741f6f42bab8e7c0c3a1bd2b5e271a59900ad74e4b926a31e3dcb1a45e599cf8edf6b78101cc21c8156e6718bb6cb9ec18dd924682511004b2419d8a180ced2ca96523bca5d102891940ee8fb823e31d9e839e1752b8adefd2c55adb14d309685b3ccbbf6f6b6bb7c2e739ec527a657f59c62778211bbbda9ff32c2fdb77124ce7514e36a059d92dc4d099fb20fc9a84347b7deec02773e90119d5f745a3b642714d4827f01da5fa3bc721c5405cbc5389298a970728c22756b60e9448132";

            /*string sInfo = HSM.DecryptAESHex(TradeInfo, sHashKey, sHashIV);
            var J1 = ChangeJsonStringToClass(sInfo);
            var J2 = ChangeJsonStringToClass(J1["Result"].ToString());
            string TradeNo = J2["TradeNo"].ToString();
            string EscrowBank = J2["EscrowBank"].ToString();
            string PaymentType = J2["PaymentType"].ToString();//WEBATM
            string PayerAccount5Code = J2["PayerAccount5Code"].ToString();//後五碼
            string PayBankCode = J2["PayBankCode"].ToString(); //付款銀行代碼809*/
        }


        #endregion
    }
}