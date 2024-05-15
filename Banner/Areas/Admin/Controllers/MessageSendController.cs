using Banner.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Metadata.W3cXsd2001;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using ZXing.QrCode.Internal;

namespace Banner.Areas.Admin.Controllers
{
    public class MessageSendController : PublicClass
    {
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
            cGetMessage_List c = new cGetMessage_List();
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
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 80 });
            TopTitles.Add(new cTableCell { Title = "訊息主題" });
            TopTitles.Add(new cTableCell { Title = "發送狀態", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "類型", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "建立日期", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "發送日期", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "啟用狀態", WidthPX = 80 });
            TopTitles.Add(new cTableCell { Title = "發送狀態", WidthPX = 80 });
            TopTitles.Add(new cTableCell { Title = "建立人", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "對象名單", WidthPX = 100 });

            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            #endregion
            #region 資料過濾
            var Ns = DC.Message_Header.Where(q => !q.DeleteFlag);
            if (!string.IsNullOrEmpty(c.sKey))
                Ns = Ns.Where(q => q.Title.Contains(c.sKey));
            if (!string.IsNullOrEmpty(c.sSDate))
            {
                DateTime DT_ = DateTime.Now;
                if (DateTime.TryParse(c.sSDate, out DT_))
                    Ns = Ns.Where(q => q.PlanSendDateTime.Date >= DT_.Date);
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
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.SortNo = N.MHID;
                cTR.Cs.Add(new cTableCell { Value = "編輯", Type = "linkbutton", URL = "/Admin/MessageSend/Message_Edit/" + N.MHID });//操作
                cTR.Cs.Add(new cTableCell { Value = N.Title });//訊息主題
                cTR.Cs.Add(new cTableCell { Value = N.SendFlag ? "已發送" : "等待發送" });//發送狀態
                cTR.Cs.Add(new cTableCell { Value = sMHType[N.MHType] });//類型
                cTR.Cs.Add(new cTableCell { Value = N.CreDate.ToString(DateTimeFormat) });//建立日期
                cTR.Cs.Add(new cTableCell { Value = N.PlanSendDateTime.ToString(DateTimeFormat) });//發送日期
                cTR.Cs.Add(new cTableCell { Value = N.ActiveFlag ? "已啟用": "停用" });//啟用狀態
                cTR.Cs.Add(new cTableCell { Value = N.SendFlag ? "已發送" : "" });//發送狀態
                var U = DC.Account.FirstOrDefault(q => q.ACID == N.CreUID);
                cTR.Cs.Add(new cTableCell { Value = U != null ? U.Name_First + U.Name_Last : "--" });//建立人
                if (N.M_MH_Account.Count(q => !q.DeleteFlag) > 0)
                    cTR.Cs.Add(new cTableCell { Value = "檢視(" + N.M_MH_Account.Count(q => !q.DeleteFlag) + ")", Type = "link", URL = "/Admin/MessageSend/Message_Target_List/" + N.MHID, Target = "_black" });//對象名單
                else
                    cTR.Cs.Add(new cTableCell { Value = "" });

                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
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
            public string sSendDateTime = DT.AddMinutes(10).ToString(DateTimeFormat);
            public string Title = "";
            public string Description = "";
            public string URL = "";
            public int MHType = 0;
            public bool ActiveFlag = true;
            public bool DeleteFlag = false;
            public int TargetType = 0;
            public int TID1 = 0;
            public int TID2 = 0;
            public int TID3 = 0;
            public bool bSendFlag = false;
            //public bool bSendFlag = false;


            //組織與職分
            public string sOITitle = "";//組織名稱
            public ListSelect SL_O = new ListSelect();//組織下拉選單
            public ListSelect SL_O_Target = new ListSelect();//目標組織下對象
            public string sOIID_1 = "";//指定ID

            //事工團
            public List<SelectListItem> ddl_OI_Staff = new List<SelectListItem>();//所屬旌旗
            public List<SelectListItem> ddl_Category_Staff = new List<SelectListItem>();//事工團類別
            public List<SelectListItem> ddl_Staff = new List<SelectListItem>();//事工團
            public List<SelectListItem> ddl_Staff_Target = new List<SelectListItem>();//事工團

            //主日聚會點
            public ListSelect SL_Meet_OI = new ListSelect();//主日聚會點-旌旗
            public ListSelect SL_Meet_0 = new ListSelect();//主日聚會點

            //課程
            public ListSelect SL_Class_OI = new ListSelect();//課程所屬旌旗
            public ListSelect SL_Class_P = new ListSelect();//上架課程副標題
            public ListSelect SL_Class_Class = new ListSelect();//班級
            public ListSelect SL_Class_Graduatio_Type = new ListSelect();//是否已結業
            public string sClassID_1 = "";//指定班級ID

            //活動
            public ListSelect SL_Event_Type = new ListSelect();//活動類別
            public string EventTitle = "";//活動標題
            public string EventDate = "";//活動日期
            public string sEventID_1 = "";//指定活動ID

            public bool AllowEddit = true;
        }
        public cGetMessage_Edit GetMessage_Edit(int ID, FormCollection FC, HttpPostedFileBase fu)
        {
            cGetMessage_Edit c = new cGetMessage_Edit();
            #region 鎖旌旗
            ACID = GetACID();
            var OI2s = DC.OrganizeInfo.Where(q => q.OID == 2 && q.ActiveFlag && !q.DeleteFlag).ToList();
            if (ACID != 1)
            {
                OI2s = (from q in OI2s
                        join p in DC.M_OI2_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID).ToList()
                        on q.OIID equals p.OIID
                        select q).ToList();
            }
            OI2s = OI2s.OrderBy(q => q.OIID).ThenBy(q => q.Title).ToList();
            #endregion
            #region 物件初始化
            
            //組織與職分
            #region 組織與職分

            c.SL_O.ControlName = "ddl_O";
            c.SL_O.ddlList = new List<SelectListItem>();
            c.SL_O_Target.ControlName = "ddl_O_Target";
            c.SL_O_Target.ddlList = new List<SelectListItem>();
            var Os = GetO();
            foreach (var O in Os.OrderBy(q => q.SortNo))
            {
                c.SL_O.ddlList.Add(new SelectListItem { Text = O.Title, Value = O.OID.ToString() });

                if (!string.IsNullOrEmpty(O.JobTitle))
                    c.SL_O_Target.ddlList.Add(new SelectListItem { Text = O.JobTitle, Value = "O_" + O.OID });
            }
            c.SL_O_Target.ddlList.Add(new SelectListItem { Text = "領夜同工", Value = "R_24" });
            c.SL_O_Target.ddlList.Add(new SelectListItem { Text = "會友", Value = "R_2" });
            c.SL_O_Target.ddlList.Add(new SelectListItem { Text = "小組員", Value = "R_1" });

            if (c.SL_O.ddlList.Count > 0)
                c.SL_O.ddlList[0].Selected = true;
            if (c.SL_O_Target.ddlList.Count > 0)
                c.SL_O_Target.ddlList[0].Selected = true;
            #endregion
            //事工團
            #region 事工團

            c.ddl_OI_Staff.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            c.ddl_OI_Staff.AddRange(from q in OI2s
                                    select new SelectListItem { Text = q.Title + q.Organize.Title, Value = q.OIID.ToString() });

            c.ddl_Category_Staff.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            c.ddl_Category_Staff.AddRange(from q in DC.Staff_Category.Where(q => q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.Title)
                                          select new SelectListItem { Text = q.Title, Value = q.SCID.ToString() });

            c.ddl_Staff.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });

            c.ddl_Staff_Target.Add(new SelectListItem { Text = "全部團員", Value = "0", Selected = true });
            c.ddl_Staff_Target.Add(new SelectListItem { Text = "主責", Value = "1" });
            c.ddl_Staff_Target.Add(new SelectListItem { Text = "帶職主責", Value = "2" });
            c.ddl_Staff_Target.Add(new SelectListItem { Text = "主責與帶職主責", Value = "3" });
            #endregion
            //主日聚會點
            #region 主日聚會點

            c.SL_Meet_OI.ControlName = "ddl_Meet_OI";
            c.SL_Meet_OI.ddlList = new List<SelectListItem>();
            c.SL_Meet_OI.ddlList.AddRange(from q in OI2s
                                          select new SelectListItem { Text = q.Title + q.Organize.Title, Value = q.OIID.ToString() });
            c.SL_Meet_OI.ddlList[0].Selected = true;

            c.SL_Meet_0.ControlName = "ddl_Meet0";
            c.SL_Meet_0.ddlList = new List<SelectListItem>();
            c.SL_Meet_0.ddlList.AddRange(from q in DC.Meeting_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OIID.ToString() == c.SL_Meet_OI.ddlList[0].Value && q.SetType == 0)
                                         select new SelectListItem { Text = q.Meeting_Location.Title, Value = q.MLID.ToString() });
            if (c.SL_Meet_0.ddlList.Count > 0)
                c.SL_Meet_0.ddlList[0].Selected = true;
            #endregion
            //上架課程
            #region 上架課程
            c.SL_Class_OI.ControlName = "ddl_Class_OI";//課程所屬旌旗
            c.SL_Class_OI.ddlList = new List<SelectListItem>();
            c.SL_Class_OI.ddlList.AddRange(from q in OI2s
                                           select new SelectListItem { Text = q.Title + q.Organize.Title, Value = q.OIID.ToString() });
            c.SL_Class_OI.ddlList[0].Selected = true;
            string sP_OIID = c.SL_Class_OI.ddlList[0].Value;

            c.SL_Class_P.ControlName = "ddl_Class_P";//上架課程副標題
            c.SL_Class_P.ddlList = new List<SelectListItem>();
            var Ps = DC.Product.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OIID.ToString() == sP_OIID).OrderBy(q => q.Course.CCID).ThenBy(q => q.CID).ThenBy(q => q.SubTitle).ToList();
            c.SL_Class_P.ddlList.AddRange(from q in Ps
                                          select new SelectListItem { Text = q.Course.Course_Category.Title + " " + q.Course.Title + " " + q.SubTitle, Value = q.PID.ToString() });
            if (c.SL_Class_P.ddlList.Count > 0)
                c.SL_Class_P.ddlList[0].Selected = true;
            else
                c.SL_Class_P.ddlList.Add(new SelectListItem { Text = "查無上架課程", Value = "-1", Selected = true });

            c.SL_Class_Class.ControlName = "ddl_Class_Class";//班級
            c.SL_Class_Class.ddlList = new List<SelectListItem>();
            if (c.SL_Class_P.ddlList.Count > 0)
            {
                string sP_ID = c.SL_Class_P.ddlList[0].Value;
                c.SL_Class_Class.ddlList.AddRange(from q in DC.Product_Class.Where(q => q.ActiveFlag && !q.DeleteFlag && q.PID.ToString() == sP_ID).OrderBy(q => q.Title)
                                                  select new SelectListItem { Text = q.Title, Value = q.PCID.ToString() });
                if (c.SL_Class_Class.ddlList.Count > 0)
                    c.SL_Class_Class.ddlList[0].Selected = true;
                else
                    c.SL_Class_Class.ddlList.Add(new SelectListItem { Text = "查無班級", Value = "-1", Selected = true });
            }

            c.SL_Class_Graduatio_Type = new ListSelect();//是否已結業

            c.SL_Class_Graduatio_Type.ControlName = "ddl_Class_Graduatio_Type";//是否已結業
            c.SL_Class_Graduatio_Type.ddlList = new List<SelectListItem>();
            c.SL_Class_Graduatio_Type.ddlList.Add(new SelectListItem { Text = "全部學員", Value = "-1", Selected = true });
            c.SL_Class_Graduatio_Type.ddlList.Add(new SelectListItem { Text = "未結業", Value = "0" });
            c.SL_Class_Graduatio_Type.ddlList.Add(new SelectListItem { Text = "已結業", Value = "1" });

            #endregion
            //活動
            #region 活動
            c.SL_Event_Type = new ListSelect();//活動類型

            c.SL_Event_Type.ControlName = "ddl_Event_Type";//是否已結業
            c.SL_Event_Type.ddlList = new List<SelectListItem>();

            c.SL_Event_Type.ddlList.Add(new SelectListItem { Text = "活動", Value = "5", Selected = true });
            //c.SL_Event_Type.ddlList.Add(new SelectListItem { Text = "主日聚會", Value = "1" });
            //c.SL_Event_Type.ddlList.Add(new SelectListItem { Text = "小組聚會", Value = "2" });
            //c.SL_Event_Type.ddlList.Add(new SelectListItem { Text = "事工團聚會", Value = "3" });
            c.SL_Event_Type.ddlList.Add(new SelectListItem { Text = "領袖之夜", Value = "4" });

            #endregion
            #endregion
            #region 前端載入
            var MH = DC.Message_Header.FirstOrDefault(q => q.MHID == ID && !q.DeleteFlag);
            var MH_T = DC.Message_Target.FirstOrDefault(q => q.MHID == ID);
            if (MH != null)
            {
                c.MHID = MH.MHID;
                c.bSendFlag = MH.SendFlag;
                if(c.bSendFlag)
                {
                    ((bool[])ViewBag._Power)[2] = false;
                }

                c.sSendDateTime = MH.PlanSendDateTime.ToString(DateTimeFormat);
                c.MHType = MH.MHType;
                c.Title = MH.Title;
                c.URL = MH.URL;
                c.Description = MH.Description;
                c.ActiveFlag = MH.ActiveFlag;
                c.DeleteFlag = MH.DeleteFlag;

                //若已送出就不能修改
                if (MH.SendFlag)
                    c.AllowEddit = false;

                if (MH_T != null)
                {
                    c.TargetType = MH_T.TargetType;
                    switch (MH_T.TargetType)
                    {
                        default:
                        case 0://全體會員
                            {

                            }
                            break;

                        case 1://牧養組織與職分
                            {
                                var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == MH_T.TargetID1);
                                if (OI != null)
                                {
                                    c.sOITitle = OI.Title;
                                    c.SL_O.ddlList.ForEach(q => q.Selected = false);
                                    c.SL_O.ddlList.First(q => q.Value == OI.OID.ToString()).Selected = true;

                                    if (MH_T.TargetID2 < 8)
                                    {
                                        c.SL_O_Target.ddlList.ForEach(q => q.Selected = false);
                                        c.SL_O_Target.ddlList.First(q => q.Value == "O_" + MH_T.TargetID2.ToString()).Selected = true;
                                    }
                                    else if (MH_T.TargetID2 == 8)
                                    {
                                        if (MH_T.TargetID3 > 0)
                                        {
                                            c.SL_O_Target.ddlList.ForEach(q => q.Selected = false);
                                            c.SL_O_Target.ddlList.First(q => q.Value == "R_" + MH_T.TargetID3.ToString()).Selected = true;
                                        }
                                        else
                                        {
                                            c.SL_O_Target.ddlList.ForEach(q => q.Selected = false);
                                            c.SL_O_Target.ddlList.First(q => q.Value == "O_" + MH_T.TargetID2.ToString()).Selected = true;
                                        }
                                    }
                                }

                            }
                            break;

                        case 2://事工團
                            {
                                c.ddl_OI_Staff.ForEach(q => q.Selected = false);
                                c.ddl_OI_Staff.First(q => q.Value == MH_T.TargetID1.ToString()).Selected = true;

                                var Staff = DC.Staff.FirstOrDefault(q => q.SID == MH_T.TargetID2);
                                if (Staff != null)
                                {
                                    c.ddl_Category_Staff.ForEach(q => q.Selected = false);
                                    c.ddl_Category_Staff.First(q => q.Value == Staff.SCID.ToString()).Selected = true;

                                    c.ddl_Staff.Clear();
                                    c.ddl_Staff.AddRange(from q in DC.Staff.Where(q => q.ActiveFlag && !q.DeleteFlag && q.SCID == Staff.SCID).OrderBy(q => q.ChildrenFlag).ThenBy(q => q.Title)
                                                         select new SelectListItem { Text = (q.ChildrenFlag ? "[兒童]" : "") + q.Title, Value = q.SID.ToString(), Selected = q.SID == MH_T.TargetID2 });

                                    c.ddl_Staff_Target.ForEach(q => q.Selected = false);
                                    c.ddl_Staff_Target.First(q => q.Value == MH_T.TargetID3.ToString()).Selected = true;
                                }
                            }
                            break;

                        case 3://活動
                            {
                                c.SL_Event_Type.ddlList.ForEach(q => q.Selected = false);
                                c.SL_Event_Type.ddlList.First(q => q.Value == MH_T.TargetID1.ToString()).Selected = true;
                                var E = DC.Event.FirstOrDefault(q => q.EID == MH_T.TargetID2);
                                if (E != null)
                                {
                                    c.sEventID_1 = MH_T.TargetID2.ToString();
                                    c.EventTitle = E.Title;
                                    c.EventDate = E.EventDate.ToString(DateFormat);
                                }
                            }
                            break;

                        case 4://聚會點
                            {
                                c.SL_Meet_OI.ddlList.ForEach(q => q.Selected = false);
                                c.SL_Meet_OI.ddlList.First(q => q.Value == MH_T.TargetID1.ToString()).Selected = true;

                                c.SL_Meet_0.ddlList.ForEach(q => q.Selected = false);
                                c.SL_Meet_0.ddlList.AddRange(from q in DC.Meeting_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OIID == MH_T.TargetID1 && q.SetType == 0)
                                                             select new SelectListItem { Text = q.Meeting_Location.Title, Value = q.MLID.ToString(), Selected = MH_T.TargetID2 == q.MLID });
                            }
                            break;

                        case 5://課程
                            {
                                var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == MH_T.TargetID2);
                                if (PC != null)
                                {
                                    c.SL_Class_OI.ddlList.ForEach(q => q.Selected = false);
                                    c.SL_Class_OI.ddlList.First(q => q.Value == PC.Product.OIID.ToString()).Selected = true;

                                    c.SL_Class_P.ddlList.Clear();
                                    c.SL_Class_P.ddlList.AddRange(from q in DC.Product.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OIID == PC.Product.OIID).OrderBy(q => q.Course.CCID).ThenBy(q => q.CID).ThenBy(q => q.SubTitle)
                                                                  select new SelectListItem { Text = q.Course.Course_Category + " " + q.Course.Title + " " + q.SubTitle, Value = q.PID.ToString(), Selected = PC.PID == q.PID });

                                    c.SL_Class_Class.ddlList.Clear();
                                    c.SL_Class_Class.ddlList.AddRange(from q in DC.Product_Class.Where(q => q.ActiveFlag && !q.DeleteFlag && q.PID == PC.PID).OrderBy(q => q.Title)
                                                                      select new SelectListItem { Text = q.Title, Value = q.PCID.ToString(), Selected = PC.PCID == q.PCID });
                                }

                                c.SL_Class_Graduatio_Type.ddlList.ForEach(q => q.Selected = false);
                                c.SL_Class_Graduatio_Type.ddlList.First(q => q.Value == MH_T.TargetID3.ToString()).Selected = true;
                            }
                            break;

                        case 6://指定名單
                            {

                            }
                            break;
                    }
                }
            }

            
            if (FC != null)
            {
                c.sSendDateTime = FC.Get("txb_PlanSendDateTime");
                c.MHType = Convert.ToInt32(FC.Get("rbl_Type"));
                c.Title = FC.Get("txb_Title");
                c.Description = FC.Get("txb_Description");
                c.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                c.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                c.URL = FC.Get("txb_URL");
                //發送對象選擇
                c.TargetType = Convert.ToInt32(FC.Get("rbl_TargetType"));
                //牧養組織與職分
                c.sOITitle = FC.Get("txb_OI_Title");
                if (c.SL_O.ddlList.Count > 0)
                {
                    c.SL_O.ddlList.ForEach(q => q.Selected = false);
                    c.SL_O.ddlList.First(q => q.Value == FC.Get(c.SL_O.ControlName)).Selected = true;
                }

                if (c.SL_O_Target.ddlList.Count > 0)
                {
                    c.SL_O_Target.ddlList.ForEach(q => q.Selected = false);
                    c.SL_O_Target.ddlList.First(q => q.Value == FC.Get(c.SL_O_Target.ControlName)).Selected = true;
                }
                c.sOIID_1 = FC.Get("txb_OIID_1");
                //事工團
                c.ddl_OI_Staff.ForEach(q => q.Selected = false);
                c.ddl_OI_Staff.First(q => q.Value == FC.Get("ddl_OI_Staff")).Selected = true;

                string sCSID = FC.Get("ddl_Category_Staff");
                c.ddl_Category_Staff.ForEach(q => q.Selected = false);
                c.ddl_Category_Staff.First(q => q.Value == sCSID).Selected = true;

                string SID = FC.Get("ddl_Staff");
                c.ddl_Staff.Clear();
                c.ddl_Staff.AddRange(from q in DC.Staff.Where(q => q.ActiveFlag && !q.DeleteFlag && q.SCID.ToString() == sCSID).OrderBy(q => q.ChildrenFlag).ThenBy(q => q.Title)
                                     select new SelectListItem { Text = (q.ChildrenFlag ? "[兒童]" : "") + q.Title, Value = q.SID.ToString(), Selected = q.SID.ToString() == SID });

                c.ddl_Staff_Target.ForEach(q => q.Selected = false);
                c.ddl_Staff_Target.First(q => q.Value == FC.Get("ddl_Staff_Target")).Selected = true;

                //聚會點
                string sOIID = FC.Get(c.SL_Meet_OI.ControlName);
                c.SL_Meet_OI.ddlList.ForEach(q => q.Selected = false);
                c.SL_Meet_OI.ddlList.First(q => q.Value == sOIID).Selected = true;

                c.SL_Meet_0.ddlList.ForEach(q => q.Selected = false);
                c.SL_Meet_0.ddlList.AddRange(from q in DC.Meeting_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OIID.ToString() == sOIID && q.SetType == 0)
                                             select new SelectListItem { Text = q.Meeting_Location.Title, Value = q.MLID.ToString() });
                c.SL_Meet_0.ddlList.First(q => q.Value == FC.Get(c.SL_Meet_0.ControlName)).Selected = true;
                //課程
                string sClass_OIID = FC.Get(c.SL_Class_OI.ControlName);
                c.SL_Class_OI.ddlList.ForEach(q => q.Selected = false);
                c.SL_Class_OI.ddlList.First(q => q.Value == sClass_OIID).Selected = true;

                string sClass_PID = FC.Get(c.SL_Class_P.ControlName);
                c.SL_Class_P.ddlList.Clear();
                var Ps__ = DC.Product.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OIID.ToString() == c.SL_Class_OI.ddlList[0].Value).OrderBy(q => q.Course.CCID).ThenBy(q => q.CID).ThenBy(q => q.SubTitle).ToList();
                c.SL_Class_P.ddlList.AddRange(from q in Ps__
                                              select new SelectListItem { Text = q.Course.Course_Category + " " + q.Course.Title + " " + q.SubTitle, Value = q.PID.ToString() }); ;
                if (c.SL_Class_P.ddlList.Count > 0)
                    c.SL_Class_P.ddlList.First(q => q.Value == sClass_PID).Selected = true;
                else
                    c.SL_Class_P.ddlList.Add(new SelectListItem { Text = "查無上架課程", Value = "-1", Selected = true });

                string sClass_ClassD = FC.Get(c.SL_Class_Class.ControlName);
                c.SL_Class_Class.ddlList.Clear();
                var PCs__ = DC.Product_Class.Where(q => q.ActiveFlag && !q.DeleteFlag && q.PID.ToString() == sClass_PID).OrderBy(q => q.Title).ToList();
                c.SL_Class_Class.ddlList.AddRange(from q in PCs__
                                                  select new SelectListItem { Text = q.Title, Value = q.PCID.ToString() });
                if (c.SL_Class_Class.ddlList.Count > 0)
                {
                    if (c.SL_Class_Class.ddlList.Any(q => q.Value == sClass_ClassD))
                        c.SL_Class_Class.ddlList.First(q => q.Value == sClass_ClassD).Selected = true;
                }
                else
                    c.SL_Class_Class.ddlList.Add(new SelectListItem { Text = "查無班級", Value = "-1", Selected = true });
                c.sClassID_1 = FC.Get("txb_Class_1");

                c.SL_Class_Graduatio_Type.ddlList.ForEach(q => q.Selected = false);
                c.SL_Class_Graduatio_Type.ddlList.First(q => q.Value == FC.Get(c.SL_Class_Graduatio_Type.ControlName)).Selected = true;
                //活動
                c.SL_Event_Type.ddlList.ForEach(q => q.Selected = false);
                c.SL_Event_Type.ddlList.First(q => q.Value == FC.Get(c.SL_Event_Type.ControlName)).Selected = true;
                c.EventTitle = FC.Get("txb_EventTitle");
                c.EventDate = FC.Get("txb_EventDate");
                c.sEventID_1 = FC.Get("txb_EventID_1");
            }

            #endregion

            #region 檢查輸入
            Error = "";
            if (FC != null)
            {
                if (string.IsNullOrEmpty(c.Title))
                    Error += "請輸入推播主題<br/>";
                if (string.IsNullOrEmpty(c.sSendDateTime))
                    Error += "請輸入預計發送時間<br/>";
                else
                {
                    DateTime DT_ = DateTime.Now;
                    if (!DateTime.TryParse(c.sSendDateTime, out DT_))
                        Error += "預計發送時間輸入錯誤<br/>";
                    else if (DT_ < DT)
                        Error += "預計發送時間請勿輸入過去時間<br/>";
                }

                if (!string.IsNullOrEmpty(c.URL))
                {
                    if (!c.URL.ToLower().StartsWith("https:"))
                        Error += "網址輸入錯誤<br/>";
                }

                switch (c.TargetType)
                {
                    default:
                    case 0://全部會員
                        break;

                    case 1://牧養組織與職分
                        {
                            if (!string.IsNullOrEmpty(c.sOIID_1))//檢查指定組織ID
                            {
                                var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID.ToString() == c.sOIID_1 && !q.DeleteFlag);
                                if (OI == null)
                                    Error += "直接指定的組織(ID=" + c.sOIID_1 + ")找不到<br/>";
                                else
                                    c.TID1 = OI.OIID;
                            }
                            else//沒輸入
                            {
                                string sOID = "0";
                                if (c.SL_O.ddlList.Any(q => q.Selected))
                                    sOID = c.SL_O.ddlList.Find(q => q.Selected).Value;
                                var OIs = DC.OrganizeInfo.Where(q => q.OID.ToString() == sOID && q.Title == c.sOITitle && !q.DeleteFlag);
                                if (OIs.Count() == 0)
                                    Error += "搜尋組織(名稱=" + c.sOITitle + ")找不到<br/>";
                                else if (OIs.Count() > 1)
                                    Error += "搜尋組織(名稱=" + c.sOITitle + ")不只一個,請直接指定ID<br/>";
                                else
                                    c.TID1 = OIs.First().OIID;
                            }

                            if (c.SL_O_Target.ddlList.Any(q => q.Selected))
                            {
                                string Target = c.SL_O_Target.ddlList.Find(q => q.Selected).Value;
                                if(Target.StartsWith("O_"))
                                {
                                    c.TID2 = Convert.ToInt32(Target.Replace("O_",""));
                                }
                                else if(Target.StartsWith("R_"))
                                {
                                    c.TID2 = 8;
                                    // c.SL_O_Target.ddlList.Add(new SelectListItem { Text = "領夜同工", Value = "R_24" });
                                    //c.SL_O_Target.ddlList.Add(new SelectListItem { Text = "會友", Value = "R_2" });
                                    //c.SL_O_Target.ddlList.Add(new SelectListItem { Text = "會員", Value = "R_1" });
                                    c.TID3 = Convert.ToInt32(Target.Replace("R_", ""));
                                }
                            }
                                
                            else
                                Error += "請選擇組織職分<br/>";
                        }
                        break;

                    case 2://事工團
                        {
                            if (c.ddl_OI_Staff.Any(q => q.Selected))
                            {
                                c.TID1 = Convert.ToInt32(c.ddl_OI_Staff.First(q => q.Selected).Value);
                                var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == c.TID1 && !q.DeleteFlag && q.OID == 2);
                                if (OI == null)
                                    Error += "事工團所屬旌旗找不到<br/>";
                            }
                            else
                                Error += "請選擇事工團所屬旌旗<br/>";

                            if (c.ddl_Staff.Any(q => q.Selected))
                            {
                                c.TID2 = Convert.ToInt32(c.ddl_Staff.First(q => q.Selected).Value);
                                var S = DC.Staff.FirstOrDefault(q => q.SID == c.TID2 && !q.DeleteFlag);
                                if (S == null)
                                    Error += "找不到事工團<br/>";
                            }
                            else
                                Error += "請選擇事工團<br/>";

                            if (c.ddl_Staff_Target.Any(q => q.Selected))
                                c.TID3 = Convert.ToInt32(c.ddl_Staff_Target.First(q => q.Selected).Value);
                            else
                                Error += "請選擇事工團指定對象<br/>";
                        }
                        break;

                    case 3://活動
                        {
                            if (string.IsNullOrEmpty(c.sEventID_1))//檢查指定活動ID
                            {
                                if (c.SL_Event_Type.ddlList.Any(q => q.Selected))
                                    c.TID1 = Convert.ToInt32(c.SL_Event_Type.ddlList.First(q => q.Selected).Value);
                                else
                                    Error += "請選擇活動類型<br/>";

                                DateTime DT_ = DT;
                                if (!DateTime.TryParse(c.EventDate, out DT_))
                                    Error += "活動日期輸入錯誤<br/>";
                                var Es = DC.Event.Where(q => q.EventDate.Date == DT_.Date && q.Title == c.EventTitle && !q.DeleteFlag && q.ECID == c.TID1);
                                if (Es.Count() == 0)
                                    Error += "查無符合的活動<br/>";
                                else if (Es.Count() > 1)
                                    Error += "搜尋活動(名稱=" + c.sOITitle + ")不只一個,請直接指定ID<br/>";
                                else
                                    c.TID2 = Es.First().EID;
                            }
                            else//有輸入
                            {
                                var E = DC.Event.FirstOrDefault(q => q.EID.ToString() == c.sEventID_1 && !q.DeleteFlag);
                                if (E == null)
                                    Error += "直接指定的活動(ID=" + c.sOIID_1 + ")找不到<br/>";
                                else
                                {
                                    c.TID1 = E.ECID;
                                    c.TID2 = E.EID;
                                }
                            }

                        }
                        break;

                    case 4://聚會點
                        {
                            if (c.SL_Meet_0.ddlList.Any(q => q.Selected))
                            {
                                string sMLID = c.SL_Meet_0.ddlList.First(q => q.Selected).Value;
                                string sOID = c.SL_Meet_OI.ddlList.First(q => q.Selected).Value;
                                var MSet = DC.Meeting_Location_Set.FirstOrDefault(q => q.MLID.ToString() == sMLID && q.SetType == 0 && q.OIID.ToString() == sOID && !q.DeleteFlag);
                                if (MSet == null)
                                    Error += "查無此聚會點,可能未啟用或已移除<br/>";
                                else
                                {
                                    c.TID1 = MSet.OIID;
                                    c.TID2 = MSet.MLID;
                                }
                            }
                        }
                        break;

                    case 5://課程
                        {
                            if (string.IsNullOrEmpty(c.sClassID_1))
                            {
                                var PC = DC.Product_Class.FirstOrDefault(q => q.PCID.ToString() == c.sClassID_1 && !q.DeleteFlag);
                                if (PC == null)
                                    Error += "查無此班級<br/>";
                                else
                                {
                                    c.TID1 = PC.PID;
                                    c.TID2 = PC.PCID;
                                }
                            }
                            else
                            {
                                if (c.SL_Class_Class.ddlList.Any(q => q.Selected))
                                {
                                    string PCID = c.SL_Class_Class.ddlList.First(q => q.Selected).Value;
                                    var PC = DC.Product_Class.FirstOrDefault(q => q.PCID.ToString() == PCID && !q.DeleteFlag);
                                    if (PC == null)
                                        Error += "查無此班級<br/>";
                                    else
                                    {
                                        c.TID1 = PC.PID;
                                        c.TID2 = PC.PCID;
                                    }
                                }
                                else
                                    Error += "請選擇開課班級<br/>";
                            }

                            if (c.SL_Class_Graduatio_Type.ddlList.Any(q => q.Selected))
                                c.TID3 = Convert.ToInt32(c.SL_Class_Graduatio_Type.ddlList.First(q => q.Selected).Value);
                            else
                                Error += "請選擇課程發送對象<br/>";
                        }
                        break;

                    case 6://指定名單
                        {
                            if (fu.ContentLength <= 0 || fu.ContentLength > 5242880)
                                Error += "請上傳檔案<br/>";
                            else if (fu.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                                Error += "請上傳檔案,且附檔名應為xlsx<br/>";
                        }
                        break;
                }
            }


            #endregion

            return c;
        }
        [HttpGet]
        public ActionResult Message_Edit(int ID)
        {
            GetViewBag();
            return View(GetMessage_Edit(ID, null, null));
        }
        [HttpPost]
        public ActionResult Message_Edit(int ID, FormCollection FC, HttpPostedFileBase fu)
        {
            GetViewBag();
            ACID = GetACID();
            var c = GetMessage_Edit(ID, FC, fu);
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                Message_Header MH = DC.Message_Header.FirstOrDefault(q => q.MHID == ID);
                if (MH != null)
                {
                    MH.PlanSendDateTime = Convert.ToDateTime(c.sSendDateTime);
                    MH.MHType = c.MHType;
                    MH.Title = c.Title;
                    MH.Description = c.Description;
                    MH.ActiveFlag = c.ActiveFlag;
                    MH.DeleteFlag = c.DeleteFlag;
                    MH.URL = c.URL;
                    MH.UpdDate = DT;
                    MH.UpdUID = ACID;
                }
                else
                {
                    MH = new Message_Header
                    {
                        MHType = c.MHType,
                        Title = c.Title,
                        Description = c.Description,
                        URL = c.URL,
                        PlanSendDateTime = Convert.ToDateTime(c.sSendDateTime),
                        SendFlag = false,
                        ActiveFlag = c.ActiveFlag,
                        DeleteFlag = c.DeleteFlag,
                        CreDate = DT,
                        CreUID = ACID,
                        UpdDate = DT,
                        UpdUID = ACID,
                    };
                    DC.Message_Header.InsertOnSubmit(MH);
                }
                DC.SubmitChanges();

                var MT = DC.Message_Target.FirstOrDefault(q => q.MHID == ID);
                if (MT != null)
                {
                    MT.TargetType = c.TargetType;
                    MT.TargetID1 = c.TID1;
                    MT.TargetID2 = c.TID2;
                    MT.TargetID3 = c.TID3;
                }
                else
                {
                    MT = new Message_Target
                    {
                        Message_Header = MH,
                        TargetType = c.TargetType,
                        TargetID1 = c.TID1,
                        TargetID2 = c.TID2,
                        TargetID3 = c.TID3
                    };
                    DC.Message_Target.InsertOnSubmit(MT);
                }
                DC.SubmitChanges();
                //匯入指定名單
                if (MT.TargetType == 6 && fu!=null)
                {
                    string extension = Path.GetExtension(fu.FileName);
                    string fileName = $"{DT.ToString("yyyyMMddHHmm") + "_" + Guid.NewGuid()}{extension}";
                    string savePath = Path.Combine(Server.MapPath("~/Files/Message/"), fileName);
                    fu.SaveAs(savePath);

                    ArrayList AL = ReadExcel("~/Files/Message/" + fileName);
                    foreach (string[] s in AL)
                    {
                        var AC = DC.Account.FirstOrDefault(q => q.ACID.ToString() == s[0] && !q.DeleteFlag);
                        if (AC != null)
                        {
                            M_MH_Account MAC = DC.M_MH_Account.FirstOrDefault(q => q.MHID == MH.MHID && q.ACID == AC.ACID);
                            if (MAC == null)
                            {
                                MAC = new M_MH_Account
                                {
                                    Message_Header = MH,
                                    MTID = MT.MTID,
                                    Account = AC,
                                    SendDateTime = MH.PlanSendDateTime,
                                    ReadDateTime = DT,
                                    ReadFlag = false,
                                    DeleteFlag = false
                                };
                                DC.M_MH_Account.InsertOnSubmit(MAC);
                                DC.SubmitChanges();
                            }
                        }
                    }
                }

                SetAlert((ID == 0 ? "新增" : "更新") + "完成", 1, "/Admin/MessageSend/Message_List");
            }
            return View(c);
        }
        #endregion
        #region 訊息管理-名單
        public class cGetMessage_Target_List
        {
            public string sKey = "";
            public int ReadType = -1;
            public cTableList cTL = new cTableList();
        }
        public cGetMessage_Target_List GetMessage_Target_List(int ID, FormCollection FC)
        {
            cGetMessage_Target_List c = new cGetMessage_Target_List();

            #region 前端資料帶入
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.ReadType = Convert.ToInt32(FC != null ? FC.Get("rbl_ReadType") : "-1");
            c.sKey = FC != null ? FC.Get("txb_Key") : "";

            #endregion
            c.cTL = new cTableList();
            c.cTL.Title = "";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            #region 篩選
            var Ns = DC.M_MH_Account.Where(q => q.MHID == ID && !q.DeleteFlag);
            if (!string.IsNullOrEmpty(c.sKey))
                Ns = Ns.Where(q => (q.Account.Name_First + q.Account.Name_Last).Contains(c.sKey));
            if (c.ReadType >= 0)
                Ns = Ns.Where(q => q.ReadFlag == (c.ReadType == 1));
            #endregion

            #region 標題
            var TopTitles = new List<cTableCell>();
            //TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "ID" });
            TopTitles.Add(new cTableCell { Title = "姓名" });
            TopTitles.Add(new cTableCell { Title = "讀取狀態" });
            TopTitles.Add(new cTableCell { Title = "讀取時間" });
            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            #endregion

            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.ACID).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Value = N.ACID.ToString() });//ID
                cTR.Cs.Add(new cTableCell { Value = N.Account.Name_First + N.Account.Name_Last });//狀態
                if (N.ReadFlag)
                {
                    cTR.Cs.Add(new cTableCell { Value = "已讀", CSS = "text-success" });//讀取狀態
                    cTR.Cs.Add(new cTableCell { Value = N.ReadDateTime.ToString(DateTimeFormat) });//讀取時間
                }

                else
                {
                    cTR.Cs.Add(new cTableCell { Value = "未讀", CSS = "text-danger" });//讀取狀態
                    cTR.Cs.Add(new cTableCell { Value = "--" });//讀取時間
                }
                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            return c;
        }
        [HttpGet]
        public ActionResult Message_Target_List(int ID)
        {
            GetViewBag();
            return View(GetMessage_Target_List(ID, null));
        }
        [HttpPost]
        public ActionResult Message_Target_List(int ID, FormCollection FC)
        {
            GetViewBag();
            return View(GetMessage_Target_List(ID, FC));
        }
        #endregion
        public ActionResult Index()
        {
            GetViewBag();
            return View();
        }
    }
}