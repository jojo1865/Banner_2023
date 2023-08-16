using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Admin.Controllers
{
    public class CourseController : PublicClass
    {
        public int ACID = 0;
        // GET: Admin/Course
        public ActionResult Index()
        {
            return View();
        }
        #region 課程-分類-列表
        public class cCourseCategory
        {
            public int ActiveType = -1;
            public string sKey = "";
            public string sCode = "";
            public cTableList cTL = new cTableList();
        }
        public cCourseCategory GetCourseCategory(FormCollection FC)
        {
            cCourseCategory N = new cCourseCategory();
            #region 物件初始化
            #region 前端資料帶入
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            N.ActiveType = Convert.ToInt32(FC != null ? FC.Get("rbl_ActiveType") : "-1");
            N.sKey = FC != null ? FC.Get("txb_Key") : "";
            N.sCode = FC != null ? FC.Get("txb_Code") : "";
            #endregion


            N.cTL = new cTableList();
            N.cTL.Title = "";
            N.cTL.NowPage = iNowPage;
            N.cTL.ItemID = "";
            N.cTL.NumCut = iNumCut;
            N.cTL.Rs = new List<cTableRow>();

            #endregion
            #region 資料庫資料帶入
            var Ns = DC.Course_Category.Where(q => !q.DeleteFlag);
            if (N.sKey != "")
                Ns = Ns.Where(q => q.Title.Contains(N.sKey));
            if (N.sCode != "")
                Ns = Ns.Where(q => q.Code.StartsWith(N.sCode));
            if (N.ActiveType >= 0)
                Ns = Ns.Where(q => q.ActiveFlag == (N.ActiveType == 1));


            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "課程分類代碼" });
            TopTitles.Add(new cTableCell { Title = "課程分類名稱" });
            TopTitles.Add(new cTableCell { Title = "課程數量", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "狀態" });
            TopTitles.Add(new cTableCell { Title = "備註" });

            N.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            N.cTL.TotalCt = Ns.Count();
            N.cTL.MaxNum = GetMaxNum(N.cTL.TotalCt, N.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.CCID).Skip((iNowPage - 1) * N.cTL.NumCut).Take(N.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/Course/Category_Edit/" + N_.CCID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = N_.Code });//課程分類代碼
                cTR.Cs.Add(new cTableCell { Value = N_.Title });//課程分類名稱
                //課程數量
                int iCt = N_.Course.Count(q => !q.DeleteFlag);
                if (iCt == 0)
                    cTR.Cs.Add(new cTableCell { Type = "link", URL = "/Admin/Course/Course_Edit/0", Target = "_self", Value = "新增" });//課程數量
                else
                    cTR.Cs.Add(new cTableCell { Type = "link", URL = "/Admin/Course/Course_List/" + N_.CCID, Target = "_self", Value = iCt.ToString() });
                cTR.Cs.Add(new cTableCell { Value = N_.ActiveFlag ? "啟用" : "停用" });//狀態
                cTR.Cs.Add(new cTableCell { Value = N_.Note });//備註

                N.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion


            return N;
        }
        [HttpGet]
        public ActionResult Category_List()
        {
            GetViewBag();
            return View(GetCourseCategory(null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Category_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetCourseCategory(FC));
        }
        #endregion
        #region 課程-分類-編輯

        public Course_Category GetCategory_Edit(int ID, FormCollection FC)
        {
            #region 資料庫資料帶入
            ACID = GetACID();
            Course_Category N = DC.Course_Category.FirstOrDefault(q => q.CCID == ID);
            if (N == null)
                N = new Course_Category
                {
                    CCID = 0,
                    Code = "",
                    Title = "",
                    ActiveFlag = true,
                    Note = "",
                    DeleteFlag = false,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID
                };
            #endregion
            #region 前端資料帶入
            if (FC != null)
            {
                N.Code = FC.Get("txb_Code");
                N.Title = FC.Get("txb_Title");
                N.Note = FC.Get("txb_Note");
                N.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                N.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
            }
            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult Category_Edit(int ID)
        {
            GetViewBag();
            return View(GetCategory_Edit(ID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Category_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            var N = GetCategory_Edit(ID, FC);
            if (N.Title == "")
                Error = "請輸入課程分類名稱";
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (N.CCID == 0)
                    DC.Course_Category.InsertOnSubmit(N);
                DC.SubmitChanges();
                SetAlert("存檔完成", 1, "/Admin/Course/Category_List");
            }

            return View(N);
        }
        #endregion
        #region 課程-列表
        public class cCourse_List
        {
            public int ActiveType = -1;
            public string sKey = "";
            public List<SelectListItem> SL = new List<SelectListItem>();
            public cTableList cTL = new cTableList();
        }
        public cCourse_List GetCourse_List(int ID, FormCollection FC)
        {
            cCourse_List N = new cCourse_List();
            #region 物件初始化
            N.SL = new List<SelectListItem>();
            var CCs = DC.Course_Category.Where(q => !q.DeleteFlag).OrderByDescending(q => q.CCID);
            foreach (var CC in CCs)
                N.SL.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString() });

            #region 前端資料帶入
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            N.ActiveType = Convert.ToInt32(FC != null ? FC.Get("rbl_ActiveType") : "-1");
            N.sKey = FC != null ? FC.Get("txb_Key") : "";
            ID = Convert.ToInt32(FC != null ? FC.Get("ddl_CC") : "0");
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
            #endregion


            N.cTL = new cTableList();
            N.cTL.Title = "";
            N.cTL.NowPage = iNowPage;
            N.cTL.ItemID = "";
            N.cTL.NumCut = iNumCut;
            N.cTL.Rs = new List<cTableRow>();

            #endregion
            #region 資料庫資料帶入
            var Ns = DC.Course.Where(q => !q.DeleteFlag);
            if (N.sKey != "")
                Ns = Ns.Where(q => q.Title.Contains(N.sKey));
            if (ID > 0)
                Ns = Ns.Where(q => q.CCID == ID);
            if (N.ActiveType >= 0)
                Ns = Ns.Where(q => q.ActiveFlag == (N.ActiveType == 1));


            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "課程分類代碼" });
            TopTitles.Add(new cTableCell { Title = "課程分類" });
            TopTitles.Add(new cTableCell { Title = "課程名稱" });
            TopTitles.Add(new cTableCell { Title = "類型", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "狀態" });

            N.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            N.cTL.TotalCt = Ns.Count();
            N.cTL.MaxNum = GetMaxNum(N.cTL.TotalCt, N.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.CCID).Skip((iNowPage - 1) * N.cTL.NumCut).Take(N.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/Course/Course_Edit/" + N_.CID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = N_.Course_Category.Code });//課程分類代碼
                cTR.Cs.Add(new cTableCell { Value = N_.Course_Category.Title });//課程分類名稱
                cTR.Cs.Add(new cTableCell { Value = N_.Title });//課程名稱
                cTR.Cs.Add(new cTableCell { Value = sCourseType[N_.CourseType] });//課程類型
                cTR.Cs.Add(new cTableCell { Value = N_.ActiveFlag ? "啟用" : "停用" });//狀態

                N.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion


            return N;
        }
        [HttpGet]
        public ActionResult Course_List(int ID = 0)
        {
            GetViewBag();
            return View(GetCourse_List(ID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Course_List(int ID = 0, FormCollection FC = null)
        {
            GetViewBag();
            return View(GetCourse_List(ID, FC));
        }
        #endregion
        #region 課程-編輯
        public class cCourse_Edit
        {
            public Course C = new Course();
            public List<SelectListItem> SL = new List<SelectListItem>();
            public string[] sCourseType = new string[0];
        }
        public cCourse_Edit GetCourse_Edit(int ID, FormCollection FC)
        {
            cCourse_Edit N = new cCourse_Edit();
            #region 物件初始化
            N.sCourseType = sCourseType;
            N.SL = new List<SelectListItem>();
            var CCs = DC.Course_Category.Where(q => !q.DeleteFlag).OrderByDescending(q => q.CCID).ToList();
            foreach (var CC in CCs)
                N.SL.Add(new SelectListItem { Text = "【"+ CC.Code+ "】" + CC.Title, Value = CC.CCID.ToString() });
            if (CCs.Count == 0)
                SetAlert("請先新增課程分類", 3, "/Admin/Course/Category_Edit/0");
            else
                N.SL[0].Selected = true;
            #endregion
            #region 資料庫資料帶入
            ACID = GetACID();
            N.C = DC.Course.FirstOrDefault(q => q.CID == ID);
            if (N.C == null)
            {
                N.C = new Course
                {
                    CCID = CCs.Count() > 0 ? CCs[0].CCID : 0,
                    Title = "",
                    CourseType = 0,
                    CourseInfo = "",
                    TargetInfo = "",
                    GraduationInfo = "",
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID
                };
            }
            else
            {
                N.SL.ForEach(q => q.Selected = false);
                if (N.SL.FirstOrDefault(q => q.Value == N.C.CCID.ToString()) != null)
                    N.SL.FirstOrDefault(q => q.Value == N.C.CCID.ToString()).Selected = true;
                else if (N.SL.Count > 0)
                    N.SL[0].Selected = true;

            }

            #endregion
            #region 前端資料帶入
            if (FC != null)
            {
                N.C.Title = FC.Get("txb_Title");
                N.C.CourseInfo = FC.Get("txb_CourseInfo");
                N.C.TargetInfo = FC.Get("txb_TargetInfo");
                N.C.GraduationInfo = FC.Get("txb_GraduationInfo");
                N.C.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                N.C.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                N.C.CourseType = Convert.ToInt32(FC.Get("rbl_CourseType"));
                N.SL.ForEach(q => q.Selected = false);
                N.SL.FirstOrDefault(q => q.Value == FC.Get("ddl_CC")).Selected = true;
            }
            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult Course_Edit(int ID)
        {
            GetViewBag();
            return View(GetCourse_Edit(ID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Course_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            var N = GetCourse_Edit(ID, FC);
            if (N.C.Title == "")
                Error = "請輸入課程名稱";
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (N.C.CID == 0)
                    DC.Course.InsertOnSubmit(N.C);
                DC.SubmitChanges();
                SetAlert("存檔完成", 1, "/Admin/Course/Course_List");
            }

            return View(N);
        }
        #endregion



        #region 物件模型
        public class cTemp
        {

        }
        public cCourseCategory GetTemp(FormCollection FC)
        {
            cCourseCategory N = new cCourseCategory();
            #region 物件初始化

            #endregion
            #region 資料庫資料帶入

            #endregion
            #region 前端資料帶入

            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult Temp_List()
        {
            GetViewBag();
            return View(GetTemp(null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Temp_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetTemp(FC));
        }
        #endregion
    }
}