using Banner.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
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
            N.SL.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
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

                SetAlert("存檔完成", 1, "/Admin/StaffSet/Staff_List");
            }

            return View(N);
        }

        #endregion
        #region 全職同工用的事工團管理
        public class cOI_Staff_List
        {
            public List<SelectListItem> ddl_OI = new List<SelectListItem>();
            public List<SelectListItem> ddl_Category = new List<SelectListItem>();

        }
        public cOI_Staff_List GetOI_Staff_List()
        {
            cOI_Staff_List c = new cOI_Staff_List();
            ACID = GetACID();
            c.ddl_OI.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            if (ACID == 1)
                c.ddl_OI.AddRange(from q in DC.OrganizeInfo.Where(q => q.OID == 2 && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.Title)
                                  select new SelectListItem { Text = q.Title + q.Organize.Title, Value = q.OIID.ToString() });
            else
                c.ddl_OI.AddRange(from q in DC.M_OI2_Account.Where(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.OrganizeInfo.Title)
                                  select new SelectListItem { Text = q.OrganizeInfo.Title + q.OrganizeInfo.Organize.Title, Value = q.OIID.ToString() });

            c.ddl_Category.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            c.ddl_Category.AddRange(from q in DC.Staff_Category.Where(q => q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.Title)
                                    select new SelectListItem { Text = q.Title, Value = q.SCID.ToString() });

            return c;
        }
        [HttpGet]
        public ActionResult OI_Staff_List()
        {
            GetViewBag();
            return View(GetOI_Staff_List());
        }
        //下拉類別篩選事工團
        [HttpGet]
        public string GetStaffList(int CID)
        {
            var Ns = from q in DC.Staff.Where(q => q.ActiveFlag && !q.DeleteFlag && q.SCID == CID).OrderBy(q => q.ChildrenFlag).ThenBy(q => q.Title)
                     select new { Title = (q.ChildrenFlag ? "[兒童]" : "") + q.Title, q.SID };

            return JsonConvert.SerializeObject(Ns);
        }
        public class cSAL
        {
            public int OIID { get; set; }
            public string OI2Title { get; set; }
            public int SCID { get; set; }
            public string SCTitle { get; set; }
            public int SID { get; set; }
            public string STitle { get; set; }

            public int MID { get; set; }
            public int ACID { get; set; }
            public string Name { get; set; }
            public string GroupName { get; set; }
            public string Contect { get; set; }

            public string JoinDate { get; set; }
            public string LeaderFlag { get; set; }
        }
        [HttpGet]
        public string GetStaffAccountList(int OIID, int SID, string Name = "", bool BUFlag = false)//取得 旌旗+事工團+同工 名單
        {
            return JsonConvert.SerializeObject(GetStaffACList(OIID, SID, Name, BUFlag));
        }
        #endregion
        #region 各事工團主責名單管理
        public class cStaffAccount_List
        {
            public cTableList cTL = new cTableList();
        }

        [HttpGet]
        public ActionResult StaffAccount_List(int ID)
        {
            GetViewBag();
            ACID = GetACID();
            ViewBag.SID = ID;
            ViewBag.Child = "false";
            var SA = DC.M_Staff_Account.FirstOrDefault(q => q.SID == ID && q.ACID == ACID && !q.DeleteFlag);
            if(SA!=null)
            {
                ViewBag.OIID = SA.OIID;
                if (SA.Staff.ChildrenFlag)
                    ViewBag.Child = "true";
            }
            else
            ViewBag.OIID = 0;
            cStaffAccount_List c = new cStaffAccount_List();
            c.cTL = new cTableList();
            c.cTL.Rs = new List<cTableRow>();
            var Ss = GetStaffACList(0, ID, "", false);
            int i = 1;
            foreach (var S in Ss)
            {
                cTableRow R = new cTableRow();
                R.SortNo = i++;
                if (S.LeaderFlag == "V")
                    R.CSS = "tr_Leader";
                R.Cs.Add(new cTableCell { Value = "cbox_Staff_" + S.MID });
                R.Cs.Add(new cTableCell { Value = S.ACID.ToString() });
                R.Cs.Add(new cTableCell { Value = S.Name });
                R.Cs.Add(new cTableCell { Value = S.GroupName });
                R.Cs.Add(new cTableCell { Value = S.Contect });
                R.Cs.Add(new cTableCell { Value = S.JoinDate });
                R.Cs.Add(new cTableCell { Value = S.LeaderFlag });
                c.cTL.Rs.Add(R);
            }

            return View(c);
        }

        public List<cSAL> GetStaffACList(int OIID, int SID, string Name, bool BUFlag)
        {

            var Ns = DC.M_Staff_Account.Where(q => !q.DeleteFlag && q.ActiveFlag);
            if (BUFlag)//只搜尋同工
            {
                Ns = from q in Ns
                     join p in DC.M_OI2_Account.Where(q => q.ActiveFlag && !q.DeleteFlag).GroupBy(q => q.ACID).Select(q => q.Key)
                     on q.ACID equals p
                     select q;
            }
            if (OIID > 0)//限定旌旗
                Ns = Ns.Where(q => q.OIID == OIID);
            if (SID > 0)//限定事工團
                Ns = Ns.Where(q => q.SID == SID);
            if (!string.IsNullOrEmpty(Name))//姓名
                Ns = Ns.Where(q => (q.Account.Name_First + q.Account.Name_Last).Contains(Name));

            List<cSAL> Ns_ = new List<cSAL>();
            foreach (var N in Ns)
            {
                cSAL c = new cSAL();
                c.OIID = N.OIID;
                c.OI2Title = N.OrganizeInfo.Title + N.OrganizeInfo.Organize.Title;
                c.SCID = N.Staff.SCID;
                c.SCTitle = N.Staff.Staff_Category.Title;
                c.SID = N.SID;
                c.STitle = (N.Staff.ChildrenFlag ? "[兒童]" : "") + N.Staff.Title;

                c.MID = N.MID;
                c.ACID = N.ACID;
                c.Name = N.Account.Name_First + N.Account.Name_Last;
                var Ms = GetMOIAC(8, 0, N.ACID);

                c.GroupName = Ms.Count() > 0 ? string.Join(",", Ms.Select(q => q.OrganizeInfo.Title + q.OrganizeInfo.Organize.Title).ToArray()) : "";
                var C = DC.Contect.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == N.ACID && q.ContectType == 1);
                c.Contect = C != null ? C.ContectValue : "--";

                c.JoinDate = N.JoinDate.ToString(DateFormat);
                c.LeaderFlag = N.LeaderFlag ? "V" : "";

                Ns_.Add(c);
            }

            Ns_ = Ns_.OrderBy(q => q.OIID).ThenBy(q => q.SCID).ThenBy(q => q.SID).ThenByDescending(q => q.LeaderFlag).ToList();
            return Ns_;
        }
        #endregion
    }
}