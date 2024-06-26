﻿using Banner.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Admin.Controllers
{
    public class CourseController : PublicClass
    {
        // GET: Admin/Course
        public ActionResult Index()
        {
            GetViewBag();
            GetACID();
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
                    cTR.Cs.Add(new cTableCell { Type = "link", URL = "/Admin/Course/Course_Edit/0?CCID=" + N_.CCID, Target = "_self", Value = "新增" });//課程數量
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
            Course_Category N = DC.Course_Category.FirstOrDefault(q => q.CCID == ID);
            if (N == null)
            {
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
            }

            #endregion
            #region 前端資料帶入
            if (FC != null)
            {
                N.Code = FC.Get("txb_Code");
                N.Title = FC.Get("txb_Title");
                N.Note = FC.Get("txb_Note");
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
            var CCs = DC.Course_Category.Where(q => !q.DeleteFlag).OrderBy(q => q.Code);
            foreach (var CC in CCs)
                N.SL.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString(), Selected = CC.CCID == ID });

            #region 前端資料帶入
            if (FC != null)
            {
                ID = Convert.ToInt32(FC.Get("ddl_CC"));
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
            var Ns = DC.Course.Where(q => !q.DeleteFlag);
            if (N.sKey != "")
                Ns = Ns.Where(q => q.Title.Contains(N.sKey));
            if (ID > 0)
                Ns = Ns.Where(q => q.CCID == ID);
            if (N.ActiveType >= 0)
                Ns = Ns.Where(q => q.ActiveFlag == (N.ActiveType == 1));


            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "課程分類" });
            TopTitles.Add(new cTableCell { Title = "課程代碼" });
            TopTitles.Add(new cTableCell { Title = "課程名稱" });
            TopTitles.Add(new cTableCell { Title = "類型", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "狀態" });

            N.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            N.cTL.TotalCt = Ns.Count();
            N.cTL.MaxNum = GetMaxNum(N.cTL.TotalCt, N.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.CID).Skip((iNowPage - 1) * N.cTL.NumCut).Take(N.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/Course/Course_Edit/" + N_.CID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = N_.Course_Category.Title });//課程分類名稱
                cTR.Cs.Add(new cTableCell { Value = N_.Code });//課程代碼
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
            public List<SelectListItem> CCSL = new List<SelectListItem>();
            public List<cCourse_Before> CBs = new List<cCourse_Before>();
            public List<Course_Rule> CRs = new List<Course_Rule>();
            public List<SelectListItem> OSL = new List<SelectListItem>();
            public string[] sCourseType = new string[0];
        }
        public class cCourse_Before
        {
            public int CRID = 0;
            public List<SelectListItem> CCSL_Before = new List<SelectListItem>();
            public List<SelectListItem> CSL_Before = new List<SelectListItem>();
        }
        public cCourse_Edit GetCourse_Edit(int ID, FormCollection FC)
        {
            cCourse_Edit N = new cCourse_Edit();
            ACID = GetACID();
            #region 物件初始化
            N.sCourseType = sCourseType;
            int CCID = GetQueryStringInInt("CCID");
            var CCs = DC.Course_Category.Where(q => !q.DeleteFlag && q.ActiveFlag).OrderBy(q => q.Code).ToList();
            foreach (var CC in CCs)
            {
                if (CCID > 0)
                    N.CCSL.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString(), Selected = CCID == CC.CCID });
                else
                    N.CCSL.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString() });
            }
            if (CCs.Count == 0)
                SetAlert("請先新增課程分類", 3, "/Admin/Course/Category_Edit/0");
            else if (CCID == 0)
                N.CCSL[0].Selected = true;

            //擋修用選單
            N.CBs = new List<cCourse_Before>();
            cCourse_Before CB = new cCourse_Before();
            CB.CRID = 0;
            CCs = DC.Course_Category.Where(q => !q.DeleteFlag && q.Course.Count(p => p.ActiveFlag && !p.DeleteFlag && p.CID != ID) > 0).OrderByDescending(q => q.CCID).ToList();
            CB.CCSL_Before.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            foreach (var CC in CCs)
                CB.CCSL_Before.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString() });
            CB.CSL_Before = new List<SelectListItem>();
            N.CBs.Add(CB);
            //組織
            var Os = DC.Organize.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList();
            var O = Os.FirstOrDefault(q => q.ParentID == 0);
            while (O != null)
            {
                if (O.JobTitle != "")
                    N.OSL.Add(new SelectListItem { Text = O.JobTitle, Value = O.OID.ToString() });
                O = Os.FirstOrDefault(q => q.ParentID == O.OID);
            };
            #endregion
            #region 資料庫資料帶入
            N.C = DC.Course.FirstOrDefault(q => q.CID == ID);
            if (N.C == null)
            {
                int Sort = 1;
                string Code = "E";
                int iCCID = 5;
                if (CCs.Count() > 0)
                {
                    iCCID = CCs[0].CCID;
                    Code = CCs[0].Code;
                }

                var Cs = DC.Course.Where(q => q.CCID == iCCID);
                if (Cs.Count() > 0)
                    Sort = Cs.Count() + 1;
                N.C = new Course
                {
                    CCID = iCCID,
                    Code = Code + Sort,
                    Title = "",
                    CourseType = 0,
                    CourseInfo = "",
                    TargetInfo = "",
                    GraduationInfo = "",
                    ClassicalFlag = false,
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID
                };
            }
            else
            {
                N.CCSL.ForEach(q => q.Selected = false);
                if (N.CCSL.FirstOrDefault(q => q.Value == N.C.CCID.ToString()) != null)
                    N.CCSL.FirstOrDefault(q => q.Value == N.C.CCID.ToString()).Selected = true;
                else if (N.CCSL.Count > 0)
                    N.CCSL[0].Selected = true;

                N.CRs = N.C.Course_Rule.ToList();
                //擋修
                foreach (var CR in N.CRs.Where(q => q.TargetType == 0).OrderBy(q => q.CRID))
                {
                    var C_ = DC.Course.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.CID == CR.TargetInt1);
                    if (C_ != null)
                    {
                        CB = new cCourse_Before();
                        CB.CRID = CR.CRID;
                        CCs = DC.Course_Category.Where(q => !q.DeleteFlag && q.Course.Count(p => p.ActiveFlag && !p.DeleteFlag && p.CID != ID) > 0).OrderBy(q => q.Code).ToList();
                        foreach (var CC in CCs) CB.CCSL_Before.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString(), Selected = CC.CCID == C_.CCID });
                        var Cs = DC.Course.Where(q => q.CCID == C_.CCID && q.CID != N.C.CID).OrderByDescending(q => q.CID);
                        foreach (var C in Cs) CB.CSL_Before.Add(new SelectListItem { Text = C.Title, Value = C.CID.ToString(), Selected = C.CID == C_.CID });

                        N.CBs.Add(CB);
                    }
                    else//這堂課已經不見了~就直接移除
                    {
                        CR.TargetInt1 = 0;
                    }
                }

                //職分
                foreach (var CR1 in N.CRs.Where(q => q.TargetType == 1))
                {
                    if (N.OSL.Any(q => q.Value == CR1.TargetInt1.ToString()))
                        N.OSL.Find(q => q.Value == CR1.TargetInt1.ToString()).Selected = true;
                }
            }

            #endregion
            #region 前端資料帶入
            if (FC != null)
            {
                N.C.Title = FC.Get("txb_Title");
                N.C.Code = FC.Get("txb_Code");
                N.C.CourseInfo = FC.Get("txb_CourseInfo");
                N.C.TargetInfo = FC.Get("txb_TargetInfo");
                N.C.GraduationInfo = FC.Get("txb_GraduationInfo");
                N.C.ClassicalFlag = GetViewCheckBox(FC.Get("cbox_ClassicalFlag"));
                N.C.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                N.C.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                N.C.CourseType = Convert.ToInt32(FC.Get("rbl_CourseType"));
                N.C.UpdDate = DT;
                N.C.SaveACID = ACID;
                N.CCSL.ForEach(q => q.Selected = false);
                N.CCSL.FirstOrDefault(q => q.Value == FC.Get("ddl_CC")).Selected = true;
                N.C.CCID = Convert.ToInt32(FC.Get("ddl_CC"));
                //擋修與對象
                //先把原本有的更新
                foreach (var CR in N.CRs)
                {
                    switch (CR.TargetType)
                    {
                        case 0://0:先修課程ID[Course]
                            {
                                if (!string.IsNullOrEmpty(FC.Get("ddl_CDB_" + CR.CRID)))//項目還在
                                    CR.TargetInt1 = Convert.ToInt32(FC.Get("ddl_CDB_" + CR.CRID));
                                else
                                    CR.TargetInt1 = 0;
                            }
                            break;
                        case 1://1:職分ID[OID]
                            {
                                //後面處理
                            }
                            break;
                        case 2://2:性別
                            {
                                CR.TargetInt1 = Convert.ToInt32(FC.Get("rbl_Sex"));
                            }
                            break;
                        case 3://3:年齡
                            {
                                try
                                {
                                    CR.TargetInt1 = Convert.ToInt32(FC.Get("txb_Age_Min"));
                                }
                                catch
                                {
                                    CR.TargetInt1 = 0;
                                }
                                try
                                {
                                    CR.TargetInt2 = Convert.ToInt32(FC.Get("txb_Age_Max"));
                                }
                                catch
                                {
                                    CR.TargetInt2 = 0;
                                }
                            }
                            break;
                        case 4://4:事工團
                            {

                            }
                            break;
                    }
                }

                //再新增UI新增的部分
                #region 先修課程
                int iCt = Convert.ToInt32(FC.Get("txb_AddCourseCt"));
                for (int i = 0; i <= iCt; i++)
                {
                    if (!string.IsNullOrEmpty(FC.Get("ddl_C_" + i)))
                    {
                        N.CRs.Add(new Course_Rule
                        {
                            CID = N.C.CID,
                            TargetType = 0,
                            TargetInt1 = Convert.ToInt32(FC.Get("ddl_CC_" + i)),
                            TargetInt2 = 0,
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = ACID
                        });
                    }
                }
                #endregion
                #region 性別
                if (N.CRs.FirstOrDefault(q => q.TargetType == 2) == null && FC.Get("rbl_Sex") != null)
                {
                    if (FC.Get("rbl_Sex") != "-1")
                    {
                        N.CRs.Add(new Course_Rule
                        {
                            CID = N.C.CID,
                            TargetType = 2,
                            TargetInt1 = Convert.ToInt32(FC.Get("rbl_Sex")),
                            TargetInt2 = 0,
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = ACID
                        });
                    }
                }
                #endregion
                #region 年齡限制
                int iMin = 0, iMax = 0;
                try { iMin = int.Parse(FC.Get("txb_Age_Min")); }
                catch { iMin = 0; }
                try { iMax = int.Parse(FC.Get("txb_Age_Max")); }
                catch { iMax = 0; }
                if (N.CRs.FirstOrDefault(q => q.TargetType == 3) == null && (iMin > 0 || iMax > 0))
                {
                    N.CRs.Add(new Course_Rule
                    {
                        CID = N.C.CID,
                        TargetType = 3,
                        TargetInt1 = iMin,
                        TargetInt2 = iMax,
                        CreDate = DT,
                        UpdDate = DT,
                        SaveACID = ACID
                    });
                }
                #endregion
                #region 職分
                foreach (var _O in N.OSL)
                {
                    _O.Selected = GetViewCheckBox(FC.Get("cbox_O" + _O.Value));
                    var CR = N.CRs.FirstOrDefault(q => q.TargetInt1.ToString() == _O.Value && q.TargetType == 1);
                    if (_O.Selected && CR == null)
                    {
                        N.CRs.Add(new Course_Rule
                        {
                            CID = N.C.CID,
                            TargetType = 1,
                            TargetInt1 = Convert.ToInt32(_O.Value),
                            TargetInt2 = 0,
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = ACID
                        });
                    }
                    else if (!_O.Selected && CR != null)
                        CR.TargetInt1 = 0;
                }
                #endregion
            }

            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult Course_Edit(int ID)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            return View(GetCourse_Edit(ID, null));
        }
        [HttpPost]
        
        public ActionResult Course_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            var N = GetCourse_Edit(ID, FC);
            if (N.C.Title == "")
                Error = "請輸入課程名稱";
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (N.C.CID == 0)
                {
                    foreach (var CR in N.CRs)
                    {
                        CR.Course = N.C;
                    }
                    DC.Course.InsertOnSubmit(N.C);
                }
                else
                {
                    foreach (var CR in N.CRs.Where(q => q.CID == 0 || q.CRID == 0))
                    {
                        CR.Course = N.C;
                        DC.Course_Rule.InsertOnSubmit(CR);
                    }
                    foreach (var CR in N.CRs.Where(q => q.TargetInt1 == 0 && (q.TargetType == 0 || q.TargetType == 1 || q.TargetType == 4)))
                    {
                        DC.Course_Rule.DeleteOnSubmit(CR);
                    }
                }
                DC.SubmitChanges();
                SetAlert("存檔完成", 1, "/Admin/Course/Course_List");
            }

            return View(N);
        }

        #endregion

        #region 講師-列表

        public cTeacher_List GetTeacher_List(FormCollection FC)
        {
            cTeacher_List N = new cTeacher_List();
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
            var Ns = DC.Teacher.Where(q => !q.DeleteFlag);
            if (N.sKey != "")
                Ns = Ns.Where(q => q.Title.Contains(N.sKey));
            if (N.ActiveType >= 0)
                Ns = Ns.Where(q => q.ActiveFlag == (N.ActiveType == 1));


            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "講師姓名" });
            TopTitles.Add(new cTableCell { Title = "所屬旌旗" });
            TopTitles.Add(new cTableCell { Title = "所屬小組" });
            TopTitles.Add(new cTableCell { Title = "狀態" });
            TopTitles.Add(new cTableCell { Title = "備註" });

            N.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            N.cTL.TotalCt = Ns.Count();
            N.cTL.MaxNum = GetMaxNum(N.cTL.TotalCt, N.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.TID).Skip((iNowPage - 1) * N.cTL.NumCut).Take(N.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/Course/Teacher_Edit/" + N_.TID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = N_.Title });//講師名稱
                if (N_.ACID > 0)
                {
                    var OIs = GetMOIAC(8, 0, N_.ACID);
                    var OI2s = from q in (from q in OIs
                              join p in DC.OrganizeInfo.Where(q=>q.OID==2)
                              on q.OrganizeInfo.OI2_ID equals p.OIID
                              select p)
                              group q by new {q.OIID,q.Title} into g
                              select new {g.Key.OIID,g.Key.Title};
                    cTR.Cs.Add(new cTableCell { Value = string.Join("<br/>", OI2s.Select(q => q.Title + "旌旗")) });//所屬旌旗
                    cTR.Cs.Add(new cTableCell { Value = string.Join("<br/>", OIs.Select(q => q.OrganizeInfo.Title + "小組")) });//所屬小組
                }
                else
                {
                    cTR.Cs.Add(new cTableCell { Value = "--" });//所屬旌旗
                    cTR.Cs.Add(new cTableCell { Value = "--" });//所屬小組
                }
                   

                cTR.Cs.Add(new cTableCell { Value = N_.ActiveFlag ? "啟用" : "停用" });//狀態
                cTR.Cs.Add(new cTableCell { Value = N_.Note });//備註

                N.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult Teacher_List()
        {
            GetViewBag();
            return View(GetTeacher_List(null));
        }
        [HttpPost]
        public ActionResult Teacher_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetTeacher_List(FC));
        }
        #endregion
        #region 講師-編輯

        public Teacher GetTeacher_Edit(int ID, FormCollection FC)
        {
            #region 資料庫資料帶入
            Teacher N = DC.Teacher.FirstOrDefault(q => q.TID == ID);
            if (N == null)
            {
                N = new Teacher
                {
                    TID = 0,
                    Title = "",
                    ActiveFlag = true,
                    Note = "",
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
                N.Note = FC.Get("txb_Note");
                N.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                N.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                N.UpdDate = DT;
                N.SaveACID = ACID;
            }
            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult Teacher_Edit(int ID)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            return View(GetTeacher_Edit(ID, null));
        }
        [HttpPost]
        
        public ActionResult Teacher_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            var N = GetTeacher_Edit(ID, FC);
            if (N.Title == "")
                Error = "請輸入講師名稱";
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (N.TID == 0)
                    DC.Teacher.InsertOnSubmit(N);
                DC.SubmitChanges();
                SetAlert("存檔完成", 1, "/Admin/Course/Teacher_List");
            }

            return View(N);
        }
        #endregion

    }
}