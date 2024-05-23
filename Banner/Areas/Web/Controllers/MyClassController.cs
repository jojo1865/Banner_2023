using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using ZXing.OneD;
using Banner.Areas.Admin.Controllers;
using Antlr.Runtime.Tree;

namespace Banner.Areas.Web.Controllers
{
    public class MyClassController : PublicClass
    {
        // GET: Web/MyClass
        public ActionResult Index()
        {
            return View();
        }
        #region 課程資訊-列表
        public class cGetMyClass_List
        {
            public cTableList cTL = new cTableList();

            public ListSelect ddl_Type = new ListSelect();

            public DateTime Cre_SDate = DateTime.Now;
            public DateTime Cre_EDate = DateTime.Now;

            public DateTime Class_SDate = DateTime.Now;
            public DateTime Class_EDate = DateTime.Now;

            public string Title = "";
        }
        public cGetMyClass_List GetMyClass_List(FormCollection FC)
        {
            cGetMyClass_List c = new cGetMyClass_List();
            ACID = GetACID();
            ChangeOrder(ACID);
            int OHID = GetQueryStringInInt("OHID");
            #region 物件初始化
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.cTL.Title = "課程清單";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            c.Cre_SDate = DT.AddMonths(-3);
            c.Cre_EDate = DT.AddMonths(3);
            c.Class_SDate = DT;
            c.Class_EDate = DT.AddMonths(3);
            #endregion
            #region 前端物件帶入
            string sOT = "0";
            c.ddl_Type = new ListSelect();
            c.ddl_Type.ControlName = "ddl_OrderType";
            if (FC != null)
            {
                sOT = FC.Get(c.ddl_Type.ControlName);
                DateTime dCre_SDate = DT, dCre_EDate = DT, dClass_SDate = DT, dClass_EDate = DT;
                if (DateTime.TryParse(FC.Get("Cre_SDate"), out dCre_SDate))
                    c.Cre_SDate = dCre_SDate;
                if (DateTime.TryParse(FC.Get("Cre_EDate"), out dCre_EDate))
                    c.Cre_EDate = dCre_EDate;
                if (DateTime.TryParse(FC.Get("Class_SDate"), out dClass_SDate))
                    c.Class_SDate = dClass_SDate;
                if (DateTime.TryParse(FC.Get("Class_EDate"), out dClass_EDate))
                    c.Class_EDate = dClass_EDate;
                c.Title = FC.Get("txb_Title");

            }
            c.ddl_Type.ddlList = new List<SelectListItem>();
            c.ddl_Type.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = sOT == "0" });
            for (int i = 1; i < sOrderType.Length; i++)
                c.ddl_Type.ddlList.Add(new SelectListItem { Text = sOrderType[i], Value = i.ToString(), Selected = i.ToString() == sOT });

            #endregion
            #region 表單帶入
            var ClassTimeGroup = (from q in DC.Product_ClassTime.Where(q => !q.Product_Class.DeleteFlag && q.Product_Class.ActiveFlag)
                                  group q by new { q.Product_Class.PID, q.PCID, q.Product_Class } into g
                                  select new
                                  {
                                      g.Key.PID,
                                      g.Key.PCID,
                                      minTime = g.Min(q => ((DateTime)q.ClassDate).Add(q.STime)),
                                      Class = g.Key.Product_Class
                                  }).Where(q => q.minTime >= c.Class_SDate && q.minTime <= c.Class_EDate);

            var Ns = from q in DC.Order_Product.Where(q => !q.Order_Header.DeleteFlag &&
                                q.Order_Header.ACID == ACID &&
                                q.Order_Header.Order_Type == 2 &&
                                q.CreDate.Date >= c.Cre_SDate &&
                                q.CreDate <= c.Cre_EDate)
                     join p in ClassTimeGroup
                     on q.PCID equals p.PCID
                     select new
                     {
                         OPID = q.OPID,
                         Title = q.Product.Title,
                         SubTitle = q.Product.SubTitle,
                         OrderType = q.Order_Header.Order_Type,
                         OHID = q.OHID,
                         CreDate = q.CreDate,
                         ClassTitle = p.Class.Title,
                         LocationName = p.Class.LocationName,
                         Address = p.Class.Address,
                         MeetURL = p.Class.MeetURL,
                         MinTime = p.minTime,
                         PCID = p.PCID,
                         PID = q.PID,
                         ProductType = q.Product.ProductType,
                     };
            if (sOT != "0")
                Ns = Ns.Where(q => q.OrderType.ToString() == sOT);
            if (!string.IsNullOrEmpty(c.Title))
                Ns = Ns.Where(q => (q.Title + q.SubTitle).Contains(c.Title));
            if (OHID > 0)
                Ns = Ns.Where(q => q.OHID == OHID);

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "訂單編號", WidthPX = 80 });
            TopTitles.Add(new cTableCell { Title = "訂單狀態", WidthPX = 80 });
            TopTitles.Add(new cTableCell { Title = "報名日期", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "課程名稱" });
            TopTitles.Add(new cTableCell { Title = "報名班級" });
            TopTitles.Add(new cTableCell { Title = "講師姓名", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "上課方式", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "上課地點" });

            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.MinTime).ThenByDescending(q => q.CreDate).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Value = N.OHID.ToString().PadLeft(5, '0') });//訂單編號
                cTR.Cs.Add(new cTableCell { Value = sOrderType[N.OrderType] });//訂單狀態
                cTR.Cs.Add(new cTableCell { Value = N.CreDate.ToString(DateTimeFormat) });//報名日期
                cTR.Cs.Add(new cTableCell { Value = N.Title, Type = "link", URL = "/Web/MyClass/MyClass_Info/" + N.PID + "?OPID=" + N.OPID });//課程名稱
                cTR.Cs.Add(new cTableCell { Value = N.ClassTitle });//報名班級
                var MT = DC.M_Product_Teacher.FirstOrDefault(q => q.PCID == N.PCID);
                if (MT != null)
                {
                    var T = DC.Teacher.FirstOrDefault(q => q.TID == MT.TID && !q.DeleteFlag);
                    if (T != null)
                        cTR.Cs.Add(new cTableCell { Value = T.Title });//講師姓名
                    else
                        cTR.Cs.Add(new cTableCell { Value = MT.Title });//講師姓名
                }
                else
                    cTR.Cs.Add(new cTableCell { Value = "特約講師" });//講師姓名
                if (N.ProductType == 0)
                    cTR.Cs.Add(new cTableCell { Value = "線上/實體" });//上課方式
                else
                    cTR.Cs.Add(new cTableCell { Value = sCourseType[N.ProductType] });//上課方式

                if (N.ProductType == 0)
                {
                    string sAddress = "";
                    if (N.Address != "")
                        sAddress += "實體地址：" + (string.IsNullOrEmpty(N.LocationName) ? "" : "(" + N.LocationName + ")") + N.Address;
                    if (N.MeetURL != "")
                        sAddress += (sAddress == "" ? "" : "<br/>") + "線上：<a href='" + N.MeetURL + "' target='_blank' class='btn btn-primary btn-round btn_Basic' >點我上課</a>";
                    cTR.Cs.Add(new cTableCell { Value = sAddress });//上課地點
                }
                else if (N.ProductType == 1)
                    cTR.Cs.Add(new cTableCell { Value = N.Address });//上課地點
                else if (N.ProductType == 2)
                    cTR.Cs.Add(new cTableCell { Value = "點我上課", URL = N.MeetURL, Target = "_blank" });//上課地點

                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion
            return c;
        }

        [HttpGet]
        public ActionResult MyClass_List()
        {
            GetViewBag();
            return View(GetMyClass_List(null));
        }
        [HttpPost]
        public ActionResult MyClass_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetMyClass_List(FC));
        }
        #endregion
        #region 課程資訊-內容
        public class cGetMyClass_Info
        {
            public int OHID = 0;
            public int OPID = 0;
            public int PID = 0;

            public string CreDate = "";
            public string PayType = "";
            public string OrderType = "";

            public string ProductTitle = "";
            public string ProductType = "";
            public string ProductInfo = "";
            public string TargetInfo = "";
            public string GraduationInfo = "";
            public int Peice = 0;

            public string ClassTitle = "";
            public string TeacherTitle = "";
            public string Graduation = "";//結業時間
            public cTableList cTL = new cTableList();
        }
        [HttpGet]
        public ActionResult MyClass_Info(int ID)
        {
            GetViewBag();
            ACID = GetACID();
            cGetMyClass_Info c = new cGetMyClass_Info();

            var OP = DC.Order_Product.FirstOrDefault(q => !q.Order_Header.DeleteFlag && q.Order_Header.ACID == ACID && q.PID == ID);
            if (OP == null)
                SetAlert("此課程資料不存在", 2, "/Web/MyClass/MyClass_List");
            else
            {
                c.OHID = OP.OHID;
                c.OPID = OP.OPID;
                c.PID = OP.PID;
                c.CreDate = OP.Order_Header.CreDate.ToString(DateTimeFormat);

                var OPaid = DC.Order_Paid.FirstOrDefault(q => q.OHID == c.OHID);
                if (OPaid != null)
                    c.PayType = OPaid.PayType.Title;
                c.OrderType = sOrderType[OP.Order_Header.Order_Type];
                c.Peice = OP.Price_Finally;

                c.ProductTitle = OP.Product.Title + OP.Product.SubTitle;
                c.ProductType = OP.Product.ProductType == 0 ? "實體與線上" : (OP.Product.ProductType == 1 ? "實體" : "線上");
                c.ProductInfo = OP.Product.ProductInfo;
                c.TargetInfo = OP.Product.TargetInfo;
                c.GraduationInfo = OP.Product.GraduationInfo;

                var Class = DC.Product_Class.FirstOrDefault(q => q.PCID == OP.PCID);
                if (Class != null)
                {
                    c.ClassTitle = Class.Title;

                    var MT = DC.M_Product_Teacher.FirstOrDefault(q => q.PCID == Class.PCID);
                    if (MT != null)
                        c.TeacherTitle = MT.Title;

                    if (Class.GraduateDate.Date > DT.Date)
                        c.Graduation = "預計於" + Class.GraduateDate.ToString(DateFormat) + "結業計算";
                    else if (!OP.Graduation_Flag)
                        c.Graduation = "您未結業...";
                    else
                    {
                        /*var AC = DC.Account.FirstOrDefault(q => q.ACID == OP.Graduation_ACID);
                        if (AC != null)
                            c.Graduation = "您已於" + OP.Graduation_Date.ToString(DateFormat) + "由" + AC.Name_First + AC.Name_Last + "判定可以結業";
                        else
                            c.Graduation = "您已於" + OP.Graduation_Date.ToString(DateFormat) + "由--判定可以結業";*/
                        c.Graduation = "您已於" + OP.Graduation_Date.ToString(DateFormat) + "結業";
                    }


                    c.cTL = new cTableList();
                    c.cTL.NumCut = 0;//分頁數字一次顯示幾個
                    c.cTL.MaxNum = 0;//分頁數量最多多少
                    //c.cTL.TotalCt = 0;//全部共多少資料
                    c.cTL.NowPage = 1;//目前所在頁數
                    c.cTL.NowURL = "";
                    c.cTL.CID = 0;
                    c.cTL.ATID = 0;
                    c.cTL.Title = "";
                    c.cTL.Rs = new List<cTableRow>();
                    c.cTL.ShowFloor = false;

                    var TopTitles = new List<cTableCell>();
                    TopTitles.Add(new cTableCell { Title = "日期", WidthPX = 160 });
                    TopTitles.Add(new cTableCell { Title = "起始時間", WidthPX = 100 });
                    TopTitles.Add(new cTableCell { Title = "結束時間", WidthPX = 100 });
                    TopTitles.Add(new cTableCell { Title = "上課簽到" });
                    c.cTL.Rs.Add(SetTableRowTitle(TopTitles));

                    var OJs = DC.Order_Join.Where(q => q.OPID == OP.OPID && !q.DeleteFlag).ToList();

                    var CTs = DC.Product_ClassTime.Where(q => q.PCID == Class.PCID).OrderBy(q => q.ClassDate).ThenBy(q => q.STime);
                    c.cTL.TotalCt = CTs.Count();//全部共多少資料
                    foreach (var CT in CTs)
                    {
                        cTableRow cTR = new cTableRow();
                        cTR.Cs.Add(new cTableCell { Value = CT.ClassDate.ToString(DateFormat) });//日期
                        cTR.Cs.Add(new cTableCell { Value = GetTimeSpanToString(CT.STime) });//起始時間
                        cTR.Cs.Add(new cTableCell { Value = GetTimeSpanToString(CT.ETime) });//結束時間
                        string sJoinNote = "";
                        if (DT.Date > CT.ClassDate.Date)
                            sJoinNote = "尚未開始上課";
                        else
                        {
                            var OJ = OJs.FirstOrDefault(q => q.PCTID == CT.PCTID);
                            if (OJ != null)
                            {
                                if (OJ.SaveACID != ACID)
                                {
                                    var AC = DC.Account.FirstOrDefault(q => q.ACID == OJ.SaveACID);
                                    if (AC != null)
                                        sJoinNote = "已於" + OJ.CreDate.ToString(DateTimeFormat) + "由" + AC.Name_First + AC.Name_Last + "代為簽到";
                                    else
                                        sJoinNote = "已於" + OJ.CreDate.ToString(DateTimeFormat) + "由--代為簽到";
                                }
                                else
                                    sJoinNote = "已於" + OJ.CreDate.ToString(DateTimeFormat) + "簽到";
                            }
                            else
                                sJoinNote = "尚未簽到";
                        }
                        cTR.Cs.Add(new cTableCell { Value = sJoinNote });//上課簽到
                        c.cTL.Rs.Add(SetTableCellSortNo(cTR));
                    }

                }
            }

            return View(c);
        }


        #endregion

        #region 我的訂單-列表
        public class cGetMyOrder_List
        {
            public cTableList cTL = new cTableList();

            public ListSelect ddl_Type = new ListSelect();

            public DateTime Cre_SDate = DateTime.Now;
            public DateTime Cre_EDate = DateTime.Now;
        }
        public cGetMyOrder_List GetMyOrder_List(FormCollection FC)
        {
            cGetMyOrder_List c = new cGetMyOrder_List();
            ACID = GetACID();
            ChangeOrder(ACID);
            #region 物件初始化
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.cTL.Title = "訂單清單";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            c.Cre_SDate = DT.AddMonths(-3);
            c.Cre_EDate = DT.AddMonths(3);

            #endregion
            #region 前端物件帶入
            string sOT = "0";
            c.ddl_Type = new ListSelect();
            c.ddl_Type.ControlName = "ddl_OrderType";
            if (FC != null)
            {
                sOT = FC.Get(c.ddl_Type.ControlName);
                DateTime dCre_SDate = DT, dCre_EDate = DT, dClass_SDate = DT, dClass_EDate = DT;
                if (DateTime.TryParse(FC.Get("Cre_SDate"), out dCre_SDate))
                    c.Cre_SDate = dCre_SDate;
                if (DateTime.TryParse(FC.Get("Cre_EDate"), out dCre_EDate))
                    c.Cre_EDate = dCre_EDate;
            }
            c.ddl_Type.ddlList = new List<SelectListItem>();
            c.ddl_Type.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = sOT == "0" });
            for (int i = 1; i < sOrderType.Length; i++)
                c.ddl_Type.ddlList.Add(new SelectListItem { Text = sOrderType[i], Value = i.ToString(), Selected = i.ToString() == sOT });

            #endregion
            #region 表單帶入


            var Ns = DC.Order_Header.Where(q => !q.DeleteFlag &&
                                q.ACID == ACID &&
                                q.Order_Type > 0 &&
                                q.CreDate.Date >= c.Cre_SDate &&
                                q.CreDate <= c.Cre_EDate);
            if (sOT != "0")
                Ns = Ns.Where(q => q.Order_Type.ToString() == sOT);


            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "刪除或補繳", WidthPX = 130 });
            TopTitles.Add(new cTableCell { Title = "訂單編號", WidthPX = 80 });
            TopTitles.Add(new cTableCell { Title = "訂單狀態", WidthPX = 80 });
            TopTitles.Add(new cTableCell { Title = "訂單日期", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "金額", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "付款方式" });
            TopTitles.Add(new cTableCell { Title = "課程名稱" });


            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.OHID).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);
            foreach (var N in Ns)
            {
                var Paid = N.Order_Paid.OrderByDescending(q => q.OPID).FirstOrDefault();
                var MinClass = (from q in DC.Order_Product.Where(q => q.OHID == N.OHID)
                                join p in DC.Product_ClassTime.Where(q => !q.Product_Class.DeleteFlag)
                                on q.PCID equals p.PCID
                                select p).OrderBy(q => q.ClassDate).FirstOrDefault();
                DateTime DT_MinClass = MinClass != null ? MinClass.ClassDate : N.CreDate.AddDays(3);
                cTableRow cTR = new cTableRow();
                //控制
                if (N.Order_Type > 2)//被取消就可以刪除了
                    cTR.Cs.Add(new cTableCell { Value = "刪除", Type = "delete", URL = "/Web/MyClass/MyOrder_Delete/" + N.OHID });
                else if (Paid != null)
                {
                    switch (Paid.PayType.PayTypeID)
                    {
                        case 0://現金
                            {
                                if (DT >= DT_MinClass && ACID != 1)//已經超過課程第一天,不能刪除
                                    cTR.Cs.Add(new cTableCell { Value = "" });
                                else
                                    cTR.Cs.Add(new cTableCell { Value = "刪除", Type = "delete", URL = "/Web/MyClass/MyOrder_Delete/" + N.OHID });
                            }
                            break;

                        case 1://信用卡
                            {
                                if (DT > N.CreDate.AddDays(CreditCardAddDays))//已過期,可以刪除
                                    cTR.Cs.Add(new cTableCell { Value = "刪除", Type = "delete", URL = "/Web/MyClass/MyOrder_Delete/" + N.OHID });
                                else if (DT.Date < DT_MinClass.Date && N.Order_Type == 2)//尚未超過課程第一天且已完成付款,可以退刷
                                    cTR.Cs.Add(new cTableCell { Value = "取消訂單", Type = "cancel", URL = "/Web/MyClass/MyOrder_Cancel/" + N.OHID });
                                else //可重新付款
                                    cTR.Cs.Add(new cTableCell { Value = "補刷卡", Type = "paid", URL = "/Web/ClassStore/Order_Paid_Credit_Card/" + N.OHID });
                            }
                            break;

                        case 2://ATM
                            {
                                if (DT > N.CreDate.AddDays(ATMAddDays))//已過期,可以刪除
                                    cTR.Cs.Add(new cTableCell { Value = "刪除", Type = "delete", URL = "/Web/MyClass/MyOrder_Delete/" + N.OHID });
                                else //可重新付款
                                    cTR.Cs.Add(new cTableCell { Value = "重新取號", Type = "paid", URL = "/Web/ClassStore/Order_Paid_ATM?OHID=" + N.OHID });
                            }
                            break;

                        case 3://Paypal
                            {
                                if (Paid.PaidFlag)
                                    cTR.Cs.Add(new cTableCell { Value = "刪除", Type = "delete", URL = "/Web/MyClass/MyOrder_Delete/" + N.OHID });
                                else
                                    cTR.Cs.Add(new cTableCell { Value = "重新付款", Type = "paid", URL = "/Web/ClassStore/Order_Paid_PayPal/" + N.OHID });
                                //cTR.Cs.Add(new cTableCell { Value = "" });
                            }
                            break;

                        case 4://支付寶
                            {
                                cTR.Cs.Add(new cTableCell { Value = "" });
                            }
                            break;
                    }
                }
                else
                    cTR.Cs.Add(new cTableCell { Value = "" });
                cTR.Cs.Add(new cTableCell { Value = N.OHID.ToString().PadLeft(5, '0') });//訂單編號
                cTR.Cs.Add(new cTableCell { Value = sOrderType[N.Order_Type] });//訂單狀態
                cTR.Cs.Add(new cTableCell { Value = N.CreDate.ToString(DateTimeFormat) });//訂單日期
                cTR.Cs.Add(new cTableCell { Value = N.TotalPrice.ToString() });//金額
                if (Paid != null)
                {
                    if (Paid.PaidFlag)
                        cTR.Cs.Add(new cTableCell { Value = "已於" + Paid.PaidDateTime.ToString(DateTimeFormat) + "使用" + sPayType[Paid.PayType.PayTypeID] + "付款完成" });
                    else
                        cTR.Cs.Add(new cTableCell { Value = "預計使用" + sPayType[Paid.PayType.PayTypeID] + "付款" });
                }
                else
                    cTR.Cs.Add(new cTableCell { Value = "--" });//付款方式



                if (N.Order_Type == 2)
                    cTR.Cs.Add(new cTableCell { Value = string.Join("<br/>", N.Order_Product.Select(q => q.Product.Title + " " + q.Product.SubTitle)), Type = "link", URL = "/Web/MyClass/MyClass_List?OHID=" + N.OHID });//課程名稱
                else
                    cTR.Cs.Add(new cTableCell { Value = string.Join("<br/>", N.Order_Product.Select(q => q.Product.Title + " " + q.Product.SubTitle)) });//課程名稱

                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion
            return c;
        }

        [HttpGet]
        public ActionResult MyOrder_List()
        {
            GetViewBag();
            return View(GetMyOrder_List(null));
        }
        [HttpPost]
        public ActionResult MyOrder_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetMyOrder_List(FC));
        }
        #endregion
        #region 移除訂單
        [HttpGet]
        public ActionResult MyOrder_Delete(int ID)
        {
            GetViewBag();
            ACID = GetACID();
            Error = "";
            var OH = DC.Order_Header.FirstOrDefault(q => q.OHID == ID && q.ACID == ACID);
            if (OH == null)
                Error = "此訂單不存在或非您的訂單,不能刪除";
            else if (OH.Order_Type == 2 && ACID != 1)
                Error = "此訂單已完成付款,不能刪除";
            else if (OH.Order_Type == 1)//交易中
            {
                var OP = DC.Order_Paid.OrderByDescending(q => q.OHID == OH.OHID).FirstOrDefault();
                if (OP != null)
                {
                    switch (OP.PayType.PayTypeID)
                    {
                        case 0://現金
                            break;

                        case 1://信用卡
                            if (DT < OH.CreDate.AddDays(CreditCardAddDays))//尚未過期
                                Error = "此訂單正在等待第三方回應,不能自行刪除";
                            break;

                        case 2://ATM
                            if (DT < OH.CreDate.AddDays(ATMAddDays))//尚未過期
                                Error = "此訂單正在等待第三方回應,不能自行刪除";
                            break;

                        case 3://Paypal
                            //Error = "此訂單正在等待第三方回應,不能自行刪除";
                            break;

                        case 4://支付寶
                            //Error = "此訂單正在等待第三方回應,不能自行刪除";
                            break;
                    }
                }
            }

            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                OH.DeleteFlag = true;
                OH.UpdDate = DT;
                OH.SaveACID = ACID;
                DC.SubmitChanges();

                SetAlert("訂單已移除", 1, "/Web/MyClass/MyOrder_List");
            }
            return View();
        }
        #endregion
        #region 取消訂單
        [HttpGet]
        public ActionResult MyOrder_Cancel(int ID)
        {
            GetViewBag();
            ACID = GetACID();
            Error = "";
            var OH = DC.Order_Header.FirstOrDefault(q => q.OHID == ID && q.ACID == ACID);
            if (OH == null)
                Error = "此訂單不存在或非您的訂單,不能取消訂單";
            else if (OH.Order_Type != 2 && ACID != 1)
                Error = "此訂單非付款完成訂單,不能取消訂單";
            else if (OH.Order_Type == 2)//交易完成
            {
                var OP = DC.Order_Paid.OrderByDescending(q => q.OHID == OH.OHID).FirstOrDefault();
                if (OP != null)
                {
                    switch (OP.PayType.PayTypeID)
                    {
                        case 0://現金
                            break;
                        case 2://ATM
                        case 3://Paypal
                        case 4://支付寶

                            break;

                        case 1://信用卡
                            {
                                Error = CancelOrder_CradCard(ID);
                            }
                            break;
                    }
                }
            }

            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (OH.Order_Type == 2)//已付款
                    OH.Order_Type = 5;
                else if (OH.Order_Type == 1)
                    OH.Order_Type = 3;
                OH.UpdDate = DT;
                OH.SaveACID = ACID;
                DC.SubmitChanges();

                SetAlert("訂單已取消", 1, "/Web/MyClass/MyOrder_List");
            }
            return View();
        }
        #endregion
        #region 我的打卡紀錄
        public class cGetMyJoinClassHistory_List
        {
            public cTableList cTL = new cTableList();
            public string Title = "";
            public DateTime SDate = DateTime.Now;
            public DateTime EDate = DateTime.Now;
        }
        public cGetMyJoinClassHistory_List GetMyJoinClassHistory_List(FormCollection FC)
        {
            cGetMyJoinClassHistory_List c = new cGetMyJoinClassHistory_List();
            ACID = GetACID();
            var Ns = DC.Order_Join.Where(q => !q.DeleteFlag && !q.Order_Product.Order_Header.DeleteFlag && q.Order_Product.Order_Header.Order_Type == 2);
            #region 物件初始化
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.cTL.Title = "出勤紀錄";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            c.SDate = DT.AddMonths(-3);
            c.EDate = DT;

            #endregion
            #region 前端物件帶入
            if (FC != null)
            {
                DateTime dSDate = DT, dEDate = DT;
                if (DateTime.TryParse(FC.Get("txb_SDate"), out dSDate))
                    c.SDate = dSDate;
                if (DateTime.TryParse(FC.Get("txb_EDate"), out dEDate))
                    c.EDate = dEDate;

                c.Title = FC.Get("txb_Title");

            }
            #endregion
            #region 表單帶入
            Ns = Ns.Where(q => q.CreDate.Date >= c.SDate.Date && q.CreDate.Date <= c.EDate.Date);
            if (!string.IsNullOrEmpty(c.Title))
                Ns = Ns.Where(q => (q.Order_Product.Product.Title + " " + q.Order_Product.Product.SubTitle).Contains(c.Title));

            var TopTitles = new List<cTableCell>();

            TopTitles.Add(new cTableCell { Title = "課程名稱" });
            TopTitles.Add(new cTableCell { Title = "報名班級" });
            TopTitles.Add(new cTableCell { Title = "上課時間" });
            TopTitles.Add(new cTableCell { Title = "打卡時間" });

            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.CreDate).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Value = N.Order_Product.Product.Title + " " + N.Order_Product.Product.SubTitle });//課程名稱
                cTR.Cs.Add(new cTableCell { Value = N.Product_ClassTime.Product_Class.Title });//報名班級
                cTR.Cs.Add(new cTableCell { Value = ChangeTimeSpanToDateTime(N.Product_ClassTime.ClassDate, N.Product_ClassTime.STime).ToString(DateTimeFormat) + " ~ " + ChangeTimeSpanToDateTime(N.Product_ClassTime.ClassDate, N.Product_ClassTime.ETime).ToString(DateTimeFormat) });//上課時間
                cTR.Cs.Add(new cTableCell { Value = N.CreDate.ToString(DateTimeFormat) });//打卡時間

                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion
            return c;
        }
        [HttpGet]
        public ActionResult MyJoinClassHistory_List()
        {
            GetViewBag();
            return View(GetMyJoinClassHistory_List(null));
        }
        [HttpPost]
        public ActionResult MyJoinClassHistory_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetMyJoinClassHistory_List(FC));
        }

        #endregion
        #region 我的結業紀錄
        public class cGetMyGraduationHistory_List
        {
            public cTableList cTL = new cTableList();
            public ListSelect LS_Graduation = new ListSelect();
            public string Title = "";
        }
        public cGetMyGraduationHistory_List GetMyGraduationHistory_List(FormCollection FC)
        {
            cGetMyGraduationHistory_List c = new cGetMyGraduationHistory_List();
            ACID = GetACID();
            var Ns = DC.Order_Product.Where(q => !q.Order_Header.DeleteFlag && q.Order_Header.Order_Type == 2);
            #region 物件初始化
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.cTL.Title = "結業狀態";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            c.LS_Graduation = new ListSelect();
            c.LS_Graduation.ControlName = "ddl_Type";
            c.LS_Graduation.ddlList.Add(new SelectListItem { Text = "全部", Value = "-1", Selected = true });
            c.LS_Graduation.ddlList.Add(new SelectListItem { Text = "已結業", Value = "1" });
            c.LS_Graduation.ddlList.Add(new SelectListItem { Text = "上課中", Value = "0" });
            #endregion
            #region 前端物件帶入
            string GType = "-1";
            if (FC != null)
            {
                c.Title = FC.Get("txb_Title");
                GType = FC.Get(c.LS_Graduation.ControlName);
                c.LS_Graduation.ddlList.ForEach(q => q.Selected = false);
                c.LS_Graduation.ddlList.First(q => q.Value == GType).Selected = true;
            }
            #endregion
            #region 表單帶入

            if (!string.IsNullOrEmpty(c.Title))
                Ns = Ns.Where(q => (q.Product.Title + " " + q.Product.SubTitle).Contains(c.Title));
            if (GType != "-1")
                Ns = Ns.Where(q => q.Graduation_Flag == (GType == "1"));

            var TopTitles = new List<cTableCell>();

            TopTitles.Add(new cTableCell { Title = "課程名稱" });
            TopTitles.Add(new cTableCell { Title = "報名班級" });
            TopTitles.Add(new cTableCell { Title = "結業狀態" });
            TopTitles.Add(new cTableCell { Title = "結業時間" });

            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            ViewBag._CSS1 = "/Areas/Web/Content/css/list.css";
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.CreDate).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Value = N.Product.Title + " " + N.Product.SubTitle });//課程名稱
                cTR.Cs.Add(new cTableCell { Value = N.Product_Class.Title });//報名班級
                if (N.Graduation_Flag)
                {
                    cTR.Cs.Add(new cTableCell { Value = "已結業" });//結業狀態
                    cTR.Cs.Add(new cTableCell { Value = N.Graduation_Date.ToString(DateFormat) });//結業時間
                }
                else
                {
                    cTR.Cs.Add(new cTableCell { Value = "上課中" });//結業狀態
                    cTR.Cs.Add(new cTableCell { Value = "--" });//結業時間
                }
                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion
            return c;
        }
        [HttpGet]
        public ActionResult MyGraduationHistory_List()
        {
            GetViewBag();
            return View(GetMyGraduationHistory_List(null));
        }
        [HttpPost]
        public ActionResult MyGraduationHistory_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetMyGraduationHistory_List(FC));
        }

        #endregion
    }
}