using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

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
        public class cGetOrder_List
        {
            public cTableList cTL = new cTableList();

            public ListSelect ddl_Type = new ListSelect();

            public DateTime Cre_SDate = DateTime.Now;
            public DateTime Cre_EDate = DateTime.Now;

            public DateTime Class_SDate = DateTime.Now;
            public DateTime Class_EDate = DateTime.Now;

            public string Title = "";
        }
        public cGetOrder_List GetOrder_List(FormCollection FC)
        {
            cGetOrder_List c = new cGetOrder_List();
            ACID = GetACID();
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
                cTR.Cs.Add(new cTableCell { Value = N.Title, Type = "link", URL = "/Web/MyClass/Order_Info/" + N.PID + "?OPID=" + N.OPID });//課程名稱
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
        public ActionResult Order_List()
        {
            GetViewBag();
            return View(GetOrder_List(null));
        }
        [HttpPost]
        public ActionResult Order_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetOrder_List(FC));
        }
        #endregion
        #region 課程資訊-內容
        public class cGetOrder_Info
        {
            public ClassStoreController.cProduct_Info cPI = new ClassStoreController.cProduct_Info();
        }
        [HttpGet]
        public ActionResult Order_Info(int ID)
        {
            GetViewBag();
            ACID = GetACID();
            cGetOrder_Info c = new cGetOrder_Info();

            ClassStoreController CSC = new ClassStoreController();
            c.cPI = CSC.GETProduct_Info(ID);
            var OP = DC.Order_Product.FirstOrDefault(q => q.OPID == GetQueryStringInInt("OPID"));
            if (c.cPI != null)
            {
                foreach (var R in c.cPI.cTL.Rs)
                    if (OP.PCID != R.ID)
                        c.cPI.cTL.Rs.Remove(R);
            }

            return View(c);
        }


        #endregion
    }
}