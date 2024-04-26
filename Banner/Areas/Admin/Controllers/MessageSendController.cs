using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Admin.Controllers
{
    public class MessageSendController : PublicClass
    {
        public string[] sMHType = new string[] { "個人", "廣告" };
        #region 訊息管理-列表
        public class cGetMessage_List
        {
            public cTableList cTL = new cTableList();
            public string sKey = "";
            public string sSDate = "";
            public string sEDate = "";
            public int iSendType = -1;
        }
        public cGetMessage_List GetMessage_List(FormCollection FC)
        {
            cGetMessage_List c =new cGetMessage_List();
            #region 初始化
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            ACID = GetACID();
            c.cTL.Title = "";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            if (FC != null)
            {
                c.sKey = FC.Get("txb_Key");
                c.sSDate = FC.Get("txb_SDate");
                c.sEDate = FC.Get("txb_EDate");
                c.iSendType = Convert.ToInt32(FC.Get("rbl_SendType"));
            }

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 200 });
            TopTitles.Add(new cTableCell { Title = "訊息主題" });
            TopTitles.Add(new cTableCell { Title = "發送狀態", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "類型", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "建立日期", WidthPX = 120 });
            TopTitles.Add(new cTableCell { Title = "發送日期", WidthPX = 120 });
            TopTitles.Add(new cTableCell { Title = "建立人", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "對象名單", WidthPX = 100 });
            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            #endregion
            #region 資料過濾
            var Ns = DC.Message_Header.Where(q => !q.DeleteFlag);
            if (!string.IsNullOrEmpty(c.sKey))
                Ns = Ns.Where(q => q.Title.Contains(c.sKey));
            if(!string.IsNullOrEmpty(c.sSDate))
            {
                DateTime DT_ = DateTime.Now;
                if (DateTime.TryParse(c.sSDate, out DT_))
                    Ns = Ns.Where(q => q.PlanSendDateTime.Date>=DT_.Date);
            }
            if (!string.IsNullOrEmpty(c.sEDate))
            {
                DateTime DT_ = DateTime.Now;
                if (DateTime.TryParse(c.sEDate, out DT_))
                    Ns = Ns.Where(q => q.PlanSendDateTime.Date <= DT_.Date);
            }
            if (c.iSendType >= 0)
                Ns = Ns.Where(q => q.SendFlag == (c.iSendType == 1));
            #endregion


            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.PlanSendDateTime).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);
            foreach(var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.SortNo = N.MHID;
                cTR.Cs.Add(new cTableCell { Value = "編輯",Type="linkbutton",URL = "/Admin/MessageSend/Message_Edit/"+N.MHID });//操作
                cTR.Cs.Add(new cTableCell { Value = N.Title });//訊息主題
                cTR.Cs.Add(new cTableCell { Value = N.SendFlag ? "已發送" : "等待發送" });//發送狀態
                cTR.Cs.Add(new cTableCell { Value = sMHType[N.MHType] });//類型
                cTR.Cs.Add(new cTableCell { Value = N.CreDate.ToString(DateTimeFormat) });//建立日期
                cTR.Cs.Add(new cTableCell { Value = N.PlanSendDateTime.ToString(DateTimeFormat) });//發送日期
                var U = DC.Account.FirstOrDefault(q => q.ACID == N.CreUID);
                cTR.Cs.Add(new cTableCell { Value = U!=null ? U.Name_First+U.Name_Last : "--" });//建立人
                cTR.Cs.Add(new cTableCell { Value = "檢視", Type = "linkbutton", URL = "/Admin/MessageSend/Message_Target_List/" + N.MHID });//對象名單
                //cTR.Cs.Add(new cTableCell { Value = "" });
            }
            return c;
        }
        [HttpGet]
        public ActionResult Message_List()
        {
            GetViewBag();
            return View(GetMessage_List(null));
        }
        [HttpPost]
        public ActionResult Message_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetMessage_List(FC));
        }
        #endregion
        #region 訊息管理-編輯
        [HttpGet]
        public ActionResult Message_Edit(int ID)
        {
            GetViewBag();
            return View();
        }
        #endregion
        #region 訊息管理-名單
        [HttpGet]
        public ActionResult Message_Target_List(int ID)
        {
            GetViewBag();
            return View();
        }
        #endregion
        public ActionResult Index()
        {
            GetViewBag();
            return View();
        }
    }
}