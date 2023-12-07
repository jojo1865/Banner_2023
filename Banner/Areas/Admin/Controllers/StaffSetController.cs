using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using System.Xml.Linq;
using static Banner.Areas.Admin.Controllers.StaffSetController;

namespace Banner.Areas.Admin.Controllers
{
    public class StaffSetController : PublicClass
    {
        #region 事工團-分類-列表
        public class cCategory_List
        {
            public int ActiveType = -1;
            public string sKey = "";
            public cTableList cTL = new cTableList();
        }
        public cCategory_List GetCategory_List(FormCollection FC)
        {
            cCategory_List N = new cCategory_List();
            #region 物件初始化
            #region 前端資料帶入
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            N.ActiveType = Convert.ToInt32(FC != null ? FC.Get("rbl_ActiveType") : "-1");
            N.sKey = FC != null ? FC.Get("txb_Key") : "";

            #endregion


            N.cTL = new cTableList();
            N.cTL.Title = "";
            N.cTL.NowPage = iNowPage;
            N.cTL.NumCut = iNumCut;
            N.cTL.Rs = new List<cTableRow>();

            #endregion
            #region 資料庫資料帶入
            var Ns = DC.Staff_Category.Where(q => !q.DeleteFlag);
            if (N.sKey != "")
                Ns = Ns.Where(q => q.Title.Contains(N.sKey));

            if (N.ActiveType >= 0)
                Ns = Ns.Where(q => q.ActiveFlag == (N.ActiveType == 1));


            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "分類名稱" });
            TopTitles.Add(new cTableCell { Title = "事工團數量", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "狀態" });


            N.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            N.cTL.TotalCt = Ns.Count();
            N.cTL.MaxNum = GetMaxNum(N.cTL.TotalCt, N.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.SCID).Skip((iNowPage - 1) * N.cTL.NumCut).Take(N.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StaffSet/Category_Edit/" + N_.SCID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = N_.Title });//課程分類名稱
                //課程數量
                int iCt = N_.Staff.Count(q => !q.DeleteFlag);
                if (iCt == 0)
                    cTR.Cs.Add(new cTableCell { Type = "link", URL = "/Admin/StaffSet/Staff_Edit/0?SCID=" + N_.SCID, Target = "_self", Value = "新增" });//課程數量
                else
                    cTR.Cs.Add(new cTableCell { Type = "link", URL = "/Admin/StaffSet/Staff_List/" + N_.SCID, Target = "_self", Value = iCt.ToString() });
                cTR.Cs.Add(new cTableCell { Value = N_.ActiveFlag ? "啟用" : "停用" });//狀態


                N.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion


            return N;
        }
        [HttpGet]
        public ActionResult Category_List()
        {
            GetViewBag();
            return View(GetCategory_List(null));
        }
        [HttpPost]
        public ActionResult Category_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetCategory_List(FC));
        }
        #endregion
        #region 事工團-分類-編輯

        public Staff_Category GetCategory_Edit(int ID, FormCollection FC)
        {
            #region 資料庫資料帶入
            Staff_Category N = DC.Staff_Category.FirstOrDefault(q => q.SCID == ID);
            if (N == null)
            {
                N = new Staff_Category
                {
                    Title = "",
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID
                };
            }

            #endregion
            #region 前端資料帶入
            if (FC != null)
            {
                N.Title = FC.Get("txb_Title");
                N.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                N.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                N.UpdDate = DT;
                N.SaveACID = ACID;
            }
            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult Category_Edit(int ID)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            return View(GetCategory_Edit(ID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Category_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            var N = GetCategory_Edit(ID, FC);
            if (N.Title == "")
                Error = "請輸入課程分類名稱";
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (N.SCID == 0)
                    DC.Staff_Category.InsertOnSubmit(N);
                DC.SubmitChanges();
                SetAlert("存檔完成", 1, "/Admin/StaffSet/Category_List");
            }

            return View(N);
        }
        #endregion

        #region 事工團-列表
        public class cStaff_List
        {
            public int ActiveType = -1;
            public string sKey = "";
            public List<SelectListItem> SL = new List<SelectListItem>();
            public cTableList cTL = new cTableList();
        }
        public cStaff_List GetStaff_List(int ID, FormCollection FC)
        {
            cStaff_List N = new cStaff_List();
            #region 物件初始化
            N.SL = new List<SelectListItem>();
            var SCs = DC.Staff_Category.Where(q => !q.DeleteFlag).OrderByDescending(q => q.SCID);
            foreach (var SC in SCs)
                N.SL.Add(new SelectListItem { Text = SC.Title, Value = SC.SCID.ToString(), Selected = SC.SCID == ID });

            #region 前端資料帶入
            if (FC != null)
            {
                ID = Convert.ToInt32(FC.Get("ddl_SC"));
                if (ID == 0)
                {
                    N.SL.ForEach(q => q.Selected = false);
                    N.SL[0].Selected = true;
                }
                else
                {
                    N.SL.ForEach(q => q.Selected = false);
                    if (N.SL.FirstOrDefault(q => q.Value == ID.ToString()) != null)
                        N.SL.First(q => q.Value == ID.ToString()).Selected = true;
                    else
                        N.SL[0].Selected = true;
                }
            }
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            N.ActiveType = Convert.ToInt32(FC != null ? FC.Get("rbl_ActiveType") : "-1");
            N.sKey = FC != null ? FC.Get("txb_Key") : "";


            #endregion


            N.cTL = new cTableList();
            N.cTL.Title = "";
            N.cTL.NowPage = iNowPage;
            N.cTL.NumCut = iNumCut;
            N.cTL.Rs = new List<cTableRow>();

            #endregion
            #region 資料庫資料帶入
            var Ns = DC.Staff.Where(q => !q.DeleteFlag);
            if (N.sKey != "")
                Ns = Ns.Where(q => q.Title.Contains(N.sKey));
            if (ID > 0)
                Ns = Ns.Where(q => q.SCID == ID);
            if (N.ActiveType >= 0)
                Ns = Ns.Where(q => q.ActiveFlag == (N.ActiveType == 1));


            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "事工團分類" });
            TopTitles.Add(new cTableCell { Title = "事工團名稱" });
            TopTitles.Add(new cTableCell { Title = "多位主責", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "兒童事工團", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "狀態" });

            N.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            N.cTL.TotalCt = Ns.Count();
            N.cTL.MaxNum = GetMaxNum(N.cTL.TotalCt, N.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.SID).Skip((iNowPage - 1) * N.cTL.NumCut).Take(N.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StaffSet/Staff_Edit/" + N_.SID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = N_.Staff_Category.Title });//分類名稱
                cTR.Cs.Add(new cTableCell { Value = N_.Title });//名稱
                cTR.Cs.Add(new cTableCell { Value = N_.LeadersFlag ? "V" : "" });//多位主責
                cTR.Cs.Add(new cTableCell { Value = N_.ChildrenFlag ? "V" : "" });//兒童事工團
                cTR.Cs.Add(new cTableCell { Value = N_.ActiveFlag ? "啟用" : "停用" });//狀態

                N.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion


            return N;
        }
        [HttpGet]
        public ActionResult Staff_List(int ID = 0)
        {
            GetViewBag();
            return View(GetStaff_List(ID, null));
        }
        [HttpPost]
        public ActionResult Staff_List(int ID = 0, FormCollection FC = null)
        {
            GetViewBag();
            return View(GetStaff_List(ID, FC));
        }
        #endregion
        #region 事工團-編輯
        public class cStaff_Edit
        {
            public Staff S = new Staff();
            public List<SelectListItem> SCSL = new List<SelectListItem>();
            public List<SelectListItem> OISL = new List<SelectListItem>();
            public string[] sCourseType = new string[0];
        }

        public cStaff_Edit GetStaff_Edit(int ID, FormCollection FC)
        {
            cStaff_Edit N = new cStaff_Edit();
            ACID = GetACID();
            #region 物件初始化
            N.sCourseType = sCourseType;
            int SCID = GetQueryStringInInt("SCID");
            var SCs = DC.Staff_Category.Where(q => !q.DeleteFlag && q.ActiveFlag).OrderByDescending(q => q.SCID).ToList();
            if (SCID == 0 && SCs.Count() > 0)
                SCID = SCs.First().SCID;
            foreach (var SC in SCs)
                N.SCSL.Add(new SelectListItem { Text = SC.Title, Value = SC.SCID.ToString(), Selected = SCID == SC.SCID });
            if (SCs.Count == 0)
                SetAlert("請先新增事工團分類", 3, "/Admin/StaffSet/Category_Edit/0");

            var OIs = DC.OrganizeInfo.Where(q => q.OID == 2 && !q.DeleteFlag).OrderBy(q => q.ParentID);
            foreach (var OI in OIs)
                N.OISL.Add(new SelectListItem { Text = OI.Title + OI.Organize.Title, Value = OI.OIID.ToString() });
            #endregion
            #region 資料庫資料帶入
            N.S = DC.Staff.FirstOrDefault(q => q.SID == ID);
            if (N.S == null)
            {
                N.S = new Staff
                {
                    SCID = SCID,
                    Title = "",
                    LeadersFlag = false,
                    ChildrenFlag = false,
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID
                };
            }
            else
            {
                N.SCSL.ForEach(q => q.Selected = false);
                if (N.SCSL.FirstOrDefault(q => q.Value == N.S.SCID.ToString()) != null)
                    N.SCSL.FirstOrDefault(q => q.Value == N.S.SCID.ToString()).Selected = true;
                else if (N.SCSL.Count > 0)
                    N.SCSL[0].Selected = true;

                var OSs = DC.OrganizeStaff.Where(q => q.SID == ID).ToList();
                foreach (var OI in OIs)
                {
                    var OS = OSs.FirstOrDefault(q => q.OIID == OI.OIID);
                    if (OS != null)
                    {
                        if (!OS.DeleteFlag && N.OISL.Any(q => q.Value == OI.OIID.ToString()))
                            N.OISL.First(q => q.Value == OI.OIID.ToString()).Selected = true;
                    }
                }
            }

            #endregion
            #region 前端資料帶入
            if (FC != null)
            {
                N.S.Title = FC.Get("txb_Title");
                N.S.LeadersFlag = GetViewCheckBox(FC.Get("cbox_LeadersFlag"));
                N.S.ChildrenFlag = GetViewCheckBox(FC.Get("cbox_ChildrenFlag"));
                N.S.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                N.S.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                N.S.UpdDate = DT;
                N.S.SaveACID = ACID;
                N.SCSL.ForEach(q => q.Selected = false);
                N.SCSL.FirstOrDefault(q => q.Value == FC.Get("ddl_SC")).Selected = true;
                N.S.SCID = Convert.ToInt32(FC.Get("ddl_SC"));

                foreach (var SL in N.OISL)
                {
                    SL.Selected = GetViewCheckBox(FC.Get("cbox_OI_" + SL.Value));
                }
            }

            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult Staff_Edit(int ID)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            return View(GetStaff_Edit(ID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Staff_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            ACID = GetACID();
            var N = GetStaff_Edit(ID, FC);
            if (N.S.Title == "")
                Error = "請輸入事工團名稱";
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (N.S.SID == 0)
                    DC.Staff.InsertOnSubmit(N.S);
                DC.SubmitChanges();
                var OIs = DC.OrganizeInfo.Where(q => q.OID == 2 && !q.DeleteFlag).ToList();
                var OSs = DC.OrganizeStaff.Where(q => q.SID == N.S.SID).ToList();
                foreach (var SL in N.OISL)
                {
                    var OS = OSs.FirstOrDefault(q => q.OIID.ToString() == SL.Value);
                    if (SL.Selected)
                    {
                        if (OS == null)
                        {
                            OS = new OrganizeStaff
                            {
                                OrganizeInfo = OIs.First(q => q.OIID.ToString() == SL.Value),
                                Staff = N.S,
                                ActiveFlag = true,
                                DeleteFlag = false,
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = ACID
                            };
                            DC.OrganizeStaff.InsertOnSubmit(OS);
                            DC.SubmitChanges();
                        }
                        else if (OS.DeleteFlag)
                        {
                            OS.DeleteFlag = false;
                            OS.UpdDate = DT;
                            OS.SaveACID = ACID;
                            DC.SubmitChanges();
                        }
                    }
                    else
                    {
                        if (OS != null)
                        {
                            OS.DeleteFlag = true;
                            OS.UpdDate = DT;
                            OS.SaveACID = ACID;
                            DC.SubmitChanges();
                        }
                    }
                }

                SetAlert("存檔完成", 1, "/Admin/StaffSet/Staff_List");
            }

            return View(N);
        }

        #endregion
    }
}