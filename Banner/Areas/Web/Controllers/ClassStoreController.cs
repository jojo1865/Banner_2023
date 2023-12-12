using Banner.Models;
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
            public int PayID = 0;
            public string PayTitle = "";
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
            foreach (var OI in OIs)
            {
                cOICroup cOIG = new cOICroup();
                cOIG.OIID = OI.OIID;
                cOIG.OITitle = OI.Title;
                cOIG.cPTs = new List<cPayType>();
                for (int i = 0; i < sPayType.Length; i++)
                {
                    cOIG.cPTs.Add(new cPayType { PayID = i, PayTitle = sPayType[i], cPs = new List<cProduct>() });
                }
                c.cOIs.Add(cOIG);
            }

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

                    if (PC.Product_ClassTime.Any())
                    {
                        var PCT = PC.Product_ClassTime.OrderBy(q => q.ClassDate).First();
                        SL.Text += PCT.ClassDate.ToString(DateFormat) + " " + PCT.STime.ToString("HH:mm");
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
                            var PTs = DC.PayType.Where(q => q.OIID == OI.OIID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.PayTypeID);
                            foreach (var PT in PTs)
                            {
                                if (PT.PayTypeID == c.cOIs[j].cPTs[i].PayID && c.cOIs[j].OIID == OI.OIID)
                                {
                                    c.cOIs[j].cPTs[i].cPs.Add(cP);
                                }
                            }
                        }
                    }
                }

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
        #region 信用卡付款

        public class cOrder_Paid_Credit_Card
        {
            public string NewebPagURL = "";
            public string MerchantID = "";
            public string TradeInfo = "";
            public string TradeSha = "";
        }
        //正式用
        public cOrder_Paid_Credit_Card GetOrder_Paid_Credit_Card(int OHID)
        {
            var OH = DC.Order_Header.FirstOrDefault(q => q.OHID == OHID);
            cOrder_Paid_Credit_Card c = new cOrder_Paid_Credit_Card();
            string str = "";
            str += "MerchantID=" + sMerchantID;//MerchantID 商店代號 String(15)
            str += "&RespondType=JSON";//RespondType 回傳格式 String(6)
            str += "&TimeStamp=" + DT.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString();//TimeStamp 時間戳記 String(50)
            str += "&Version=2.0";//Version 串接程式版本 String(5)
            str += "&LangType=zh-tw";//LangType 語系 String(5)
            str += "&MerchantOrderNo=" + GetRand(1000);//MerchantOrderNo 商店訂單編號 String(30)
            str += "&Amt=30";//Amt 訂單金額 int(10)
            str += "&temDesc=測試商品";//temDesc 商品資訊
            str += "&TradeLimit=600";//TradeLimit 交易有效時間  Int(3)
            str += "&ExpireDate=" + DT.AddDays(2).ToString("yyyyMMdd");//ExpireDate 繳費有效期限 String(10)
            str += "&ReturnURL=" + sDomanName + "/Web/Order_End/" + OHID;//ReturnURL 支付完成返回商店網址 String(200)
            str += "&NotifyURL=" + sDomanName + "/Web/Order_Paid/" + OHID;//NotifyURL 支付通知網址 String(200)
            str += "&CustomerURL=" + sDomanName + "/Web/Order_GetNo/" + OHID;//CustomerURL 商店取號網址 String(200)
                                                                             //4.2.3 回應參數-取號完成
                                                                             //適用交易類別：超商代碼、超商條碼、超商取貨付款、ATM
            str += "&ClientBackURL=";//ClientBackURL 返回商店網址 String(200)
            str += "&Email=minto.momoko.jojo1865@gmail.com";//Email 付款人電子信箱 String(50)
            str += "&EmailModify=1";//EmailModify 付款人電子信箱是否開放修改 Int(1) 1=可修改 0=不可修改
            str += "&LoginType=0";//LoginType 藍新金流會員 0 = 不須登入藍新金流會員
            str += "&OrderComment=";//OrderComment 商店備註 String(300)

            str += "&CREDIT=1";//CREDIT 信用卡一次付清啟用 Int(1)
            str += "&ANDROIDPAY=0";//ANDROIDPAY Google Pay 啟用 Int(1)
            str += "&SAMSUNGPAY=0";//SAMSUNGPAY Samsung Pay 啟用 Int(1)
            str += "&LINEPAY=0";//LINEPAY LINE Pay 啟用 Int(1)
            str += "&ImageUrl=";//ImageUrl 產品圖檔連結網址 String(200)
            str += "&InstFlag=0";//InstFlag 信用卡分期付款啟用 String(18)
            str += "&CreditRed=0";//CreditRed 信用卡紅利啟用 Int(1)
            str += "&UNIONPAY=1";//UNIONPAY 信用卡銀聯卡啟用 Int(1)
            str += "&CREDITAE=1";//CREDITAE 信用卡美國運通卡啟用 Int(1)
            str += "&WEBATM=0";//WEBATM WEBATM啟用 Int(1)
            str += "&VACC=0";//VACC ATM轉帳啟用 Int(1)
            str += "&BankType=";//BankType 金融機構 String(26)
            str += "&CVS=0";//CVS 超商代碼繳費啟用 Int(1)
            str += "&BARCODE=0";//BARCODE 超商條碼繳費啟用 Int(1)
            str += "&ESUNWALLET=0";//ESUNWALLET 玉山Wallet Int(1)
            str += "&TAIWANPAY=0";//TAIWANPAY 台灣Pay Int(1)
            str += "&FULA=0";//FULA Fula付啦 String(20)
            str += "&CVSCOM=0";//CVSCOM 物流啟用 Int(1)
            str += "&EZPAY=0";//EZPAY 簡單付電子錢包 Int(1)
            str += "&EZPWECHAT=0";//EZPWECHAT 簡單付微信支付 Int(1)
            str += "&EZPALIPAY=0";//EZPALIPAY 簡單付支付寶 Int(1)
            str += "&LgsType=";//LgsType 物流型態 String(3)
            return c;
        }
        //測試用
        public cOrder_Paid_Credit_Card GetOrderTest_Paid_Credit_Card(int OHID)
        {
            cOrder_Paid_Credit_Card c = new cOrder_Paid_Credit_Card();
            string str = "";
            str += "MerchantID=" + sMerchantID;//MerchantID 商店代號 String(15)
            str += "&RespondType=JSON";//RespondType 回傳格式 String(6)
            str += "&TimeStamp=" + DT.Subtract(new DateTime(1970, 1, 1)).TotalSeconds.ToString();//TimeStamp 時間戳記 String(50)
            str += "&Version=2.0";//Version 串接程式版本 String(5)
            str += "&LangType=zh-tw";//LangType 語系 String(5)
            str += "&MerchantOrderNo=" + GetRand(1000);//MerchantOrderNo 商店訂單編號 String(30)
            str += "&Amt=100";//Amt 訂單金額 int(10)
            str += "&temDesc=測試商品";//temDesc 商品資訊
            str += "&TradeLimit=600";//TradeLimit 交易有效時間  Int(3)
            str += "&ExpireDate=" + DT.AddDays(2).ToString("yyyyMMdd");//ExpireDate 繳費有效期限 String(10)
            str += "&ReturnURL=" + sDomanName + "/Web/ClassStore/Order_Back_Credit_Card/" + OHID;//ReturnURL 支付完成返回商店網址 String(200)
            str += "&NotifyURL=" + sDomanName + "/Web/ClassStore/Order_Back_Credit_Card/" + OHID;//NotifyURL 支付通知網址 String(200)
            str += "&CustomerURL=" + sDomanName + "/Web/ClassStore/Order_GetNo/" + OHID;//CustomerURL 商店取號網址 String(200)
                                                                                        //4.2.3 回應參數-取號完成
                                                                                        //適用交易類別：超商代碼、超商條碼、超商取貨付款、ATM
            str += "&ClientBackURL=" + sDomanName + "/Web/ClassStore/Order_Back_Credit_Card/" + OHID;//ClientBackURL 返回商店網址 String(200)
            str += "&Email=minto.momoko.jojo1865@gmail.com";//Email 付款人電子信箱 String(50)
            str += "&EmailModify=1";//EmailModify 付款人電子信箱是否開放修改 Int(1) 1=可修改 0=不可修改
            str += "&LoginType=0";//LoginType 藍新金流會員 0 = 不須登入藍新金流會員
            str += "&OrderComment=無";//OrderComment 商店備註 String(300)

            str += "&CREDIT=1";//CREDIT 信用卡一次付清啟用 Int(1)
            str += "&ANDROIDPAY=0";//ANDROIDPAY Google Pay 啟用 Int(1)
            str += "&SAMSUNGPAY=0";//SAMSUNGPAY Samsung Pay 啟用 Int(1)
            str += "&LINEPAY=0";//LINEPAY LINE Pay 啟用 Int(1)
            str += "&ImageUrl=" + sDomanName + "/Content/Image/CourseCategory_1.jpg";//ImageUrl 產品圖檔連結網址 String(200)
            str += "&InstFlag=0";//InstFlag 信用卡分期付款啟用 String(18)
            str += "&CreditRed=0";//CreditRed 信用卡紅利啟用 Int(1)
            str += "&UNIONPAY=1";//UNIONPAY 信用卡銀聯卡啟用 Int(1)
            str += "&CREDITAE=1";//CREDITAE 信用卡美國運通卡啟用 Int(1)
            str += "&WEBATM=0";//WEBATM WEBATM啟用 Int(1)
            str += "&VACC=0";//VACC ATM轉帳啟用 Int(1)
            str += "&BankType=BOT";//BankType 金融機構 String(26)//先選台銀
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
            TempData["TradeInfo"] = c.TradeInfo;
            string str1 = "HashKey=" + sHashKey + "&" + c.TradeInfo + "&HashIV=" + sHashIV;
            c.TradeSha = HSM.EncryptSHA256(str1).ToUpper();

            TempData["TradeSha"] = c.TradeSha;

            return c;
        }
        [HttpGet]
        public ActionResult Order_Paid_Credit_Card(int ID)
        {
            GetViewBag();
            return View(GetOrderTest_Paid_Credit_Card(ID));
        }
        #endregion
        #region 信用卡返回
        public ActionResult Order_Back_Credit_Card(int ID)
        {
            GetViewBag();
            return View();
        }
        #endregion
    }
}