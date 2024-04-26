using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

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
        public class cGetMessage_Edit
        {
            public int MHID = 0;
            public string sSendDateTime = DT.ToString(DateTimeFormat);
            public string Title = "";
            public string Description = "";
            public int MHType = 0;
            public bool ActiveFlag = true;
            public bool DeleteFlag = false;
            public int TargetType = 0;
            public bool bSendFlag = false;

            //組織與職分
            public string sOITitle = "";//組織名稱
            public ListSelect SL_O = new ListSelect();//組織下拉選單
            public ListSelect SL_O_Target = new ListSelect();//目標組織下對象
        }
        public cGetMessage_Edit GetMessage_Edit(int ID,FormCollection FC)
        {
            cGetMessage_Edit c = new cGetMessage_Edit();
            #region 物件初始化
            c.SL_O.ControlName = "ddl_O";
            c.SL_O.ddlList = new List<SelectListItem>();
            c.SL_O_Target.ControlName = "ddl_O_Target";
            c.SL_O_Target.ddlList = new List<SelectListItem>();
            var Os = GetO();
            foreach(var O in Os.OrderBy(q=>q.SortNo))
            {
                c.SL_O.ddlList.Add(new SelectListItem { Text = O.Title, Value = O.OID.ToString() });

                if (!string.IsNullOrEmpty(O.JobTitle))
                    c.SL_O_Target.ddlList.Add(new SelectListItem { Text = O.JobTitle, Value = "O_" + O.OID });
            }
            c.SL_O_Target.ddlList.Add(new SelectListItem { Text = "領夜同工", Value = "R_24" });
            c.SL_O_Target.ddlList.Add(new SelectListItem { Text = "會友", Value = "R_2" });
            c.SL_O_Target.ddlList.Add(new SelectListItem { Text = "會員", Value = "R_1" });

            if(c.SL_O.ddlList.Count>0)
                c.SL_O.ddlList[0].Selected = true;
            if (c.SL_O_Target.ddlList.Count > 0)
                c.SL_O_Target.ddlList[0].Selected = true;
            #endregion

            if (FC!=null)
            {
                c.sSendDateTime = FC.Get("txb_PlanSendDateTime");
                c.MHType = Convert.ToInt32(FC.Get("rbl_Type"));
                c.Title = FC.Get("txb_Title");
                c.Description= FC.Get("txb_Description");
                c.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                c.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                //發送對象選擇
                c.TargetType = Convert.ToInt32(FC.Get("rbl_TargetType"));
                //牧養組織與職分
                c.sOITitle = FC.Get("txb_OI_Title");
                if (c.SL_O.ddlList.Count > 0)
                {
                    c.SL_O.ddlList.ForEach(q => q.Selected = false);
                    c.SL_O.ddlList.First(q=>q.Value==FC.Get(c.SL_O.ControlName)).Selected = true;
                }
                    
                if (c.SL_O_Target.ddlList.Count > 0)
                {
                    c.SL_O_Target.ddlList.ForEach(q => q.Selected = false);
                    c.SL_O_Target.ddlList.First(q => q.Value == FC.Get(c.SL_O.ControlName)).Selected = true;
                }
            }

            return c;
        }
        [HttpGet]
        public ActionResult Message_Edit(int ID)
        {
            GetViewBag();
            return View(GetMessage_Edit(ID, null));
        }
        [HttpPost]
        public ActionResult Message_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            return View(GetMessage_Edit(ID, FC));
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