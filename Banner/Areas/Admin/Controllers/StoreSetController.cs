﻿using Banner.Models;
using Microsoft.Ajax.Utilities;
using OfficeOpenXml.ConditionalFormatting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace Banner.Areas.Admin.Controllers
{
    public class StoreSetController : PublicClass
    {
        // GET: Admin/StoreSet

        #region 上架課程-列表
        public class cProduct_List
        {
            public int ActiveType = -1;
            public string sKey = "";
            public string SDate = "";
            public string EDate = "";
            public List<SelectListItem> SL = new List<SelectListItem>();
            public cTableList cTL = new cTableList();
        }
        public cProduct_List GetProduct_List(FormCollection FC)
        {
            cProduct_List c = new cProduct_List();
            #region 物件初始化
            c.SL = new List<SelectListItem>();
            c.SL.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            var CCs = DC.Course_Category.Where(q => !q.DeleteFlag).OrderByDescending(q => q.CCID);
            foreach (var CC in CCs)
                c.SL.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString() });

            #region 前端資料帶入
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.ActiveType = Convert.ToInt32(FC != null ? FC.Get("rbl_ActiveType") : "-1");
            c.sKey = FC != null ? FC.Get("txb_Key") : "";
            c.SDate = FC != null ? FC.Get("txb_SDate") : "";
            c.EDate = FC != null ? FC.Get("txb_EDate") : "";
            int CCID = Convert.ToInt32(FC != null ? FC.Get("ddl_CC") : "0");
            c.SL.ForEach(q => q.Selected = false);
            c.SL.First(q => q.Value == CCID.ToString()).Selected = true;
            #endregion


            c.cTL = new cTableList();
            c.cTL.Title = "";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            #endregion
            #region 資料庫資料帶入
            var Ns = DC.Product.Where(q => !q.DeleteFlag);
            if (c.sKey != "")
                Ns = Ns.Where(q => q.Title.Contains(c.sKey));
            if (CCID > 0)
                Ns = Ns.Where(q => q.Course.CCID == CCID);
            if (c.ActiveType >= 0)
                Ns = Ns.Where(q => q.ActiveFlag == (c.ActiveType == 1));
            DateTime DT_ = DateTime.Now;
            if (DateTime.TryParse(c.SDate, out DT_))
            {
                var PC_Gs = from q in
                            (from q in DC.Product_ClassTime.Where(q => q.ClassDate.Date >= DT_.Date)
                             join p in DC.Product_Class.Where(q => !q.DeleteFlag)
                             on q.PCID equals p.PCID
                             select p)
                            group q by new { q.PID } into g
                            select new { g.Key.PID };
                Ns = from q in Ns
                     join p in PC_Gs
                     on q.PID equals p.PID
                     select q;
            }
            if (DateTime.TryParse(c.EDate, out DT_))
            {
                var PC_Gs = from q in
                            (from q in DC.Product_ClassTime.Where(q => q.ClassDate.Date <= DT_.Date)
                             join p in DC.Product_Class.Where(q => !q.DeleteFlag)
                             on q.PCID equals p.PCID
                             select p)
                            group q by new { q.PID } into g
                            select new { g.Key.PID };
                Ns = from q in Ns
                     join p in PC_Gs
                     on q.PID equals p.PID
                     select q;
            }

            //旌旗權限檢視門檻設置
            var OI2s = DC.M_OI2_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID);
            if (OI2s.Any())
            {
                var OI2_1 = OI2s.FirstOrDefault(q => q.OIID == 1);
                if (OI2_1 == null)
                {
                    Ns = from q in Ns
                         join p in OI2s.Where(q => q.OIID > 2)
                         on q.OIID equals p.OIID
                         select q;
                }
            }
            else if (ACID == 1) { }
            else//沒有旌旗權限~暫時就乾脆都先不給看好了
            {
                Ns = Ns.Where(q => q.OIID == 1);
            }

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "課程分類" });
            TopTitles.Add(new cTableCell { Title = "課程名稱" });
            TopTitles.Add(new cTableCell { Title = "線上報名日期" });
            TopTitles.Add(new cTableCell { Title = "開課日期" });
            TopTitles.Add(new cTableCell { Title = "原價" });
            TopTitles.Add(new cTableCell { Title = "開班" });
            //TopTitles.Add(new cTableCell { Title = "會友限定" });
            TopTitles.Add(new cTableCell { Title = "顯示狀態" });
            TopTitles.Add(new cTableCell { Title = "交易狀態" });

            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.PID).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/Product_Edit/" + N_.PID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = "【" + N_.Course.Course_Category.Code + "】" + N_.Course.Course_Category.Title });//課程分類
                cTR.Cs.Add(new cTableCell { Value = N_.Course.Title + " " + N_.SubTitle });//課程名稱
                cTR.Cs.Add(new cTableCell { Value = (N_.SDate_Signup.ToString(DateFormat) + "<br/>↕<br/>" + N_.EDate_Signup.ToString(DateFormat)) });//線上報名日期
                var PCTs = from q in DC.Product_Class.Where(q => q.PID == N_.PID && !q.DeleteFlag)
                           join p in DC.Product_ClassTime
                           on q.PCID equals p.PCID
                           select p;
                if (PCTs.Count() > 0)//開課日期
                {
                    DateTime SDate = PCTs.Min(q => q.ClassDate);
                    DateTime EDate = PCTs.Max(q => q.ClassDate);
                    if (SDate.Date != EDate.Date)
                        cTR.Cs.Add(new cTableCell { Value = SDate.ToString(DateFormat) + "<br/>↕<br/>" + EDate.ToString(DateFormat) });
                    else
                    {
                        TimeSpan STime = PCTs.Where(q => q.ClassDate.Date == SDate).Min(q => q.STime);
                        TimeSpan ETime = PCTs.Where(q => q.ClassDate.Date == SDate).Max(q => q.ETime);
                        cTR.Cs.Add(new cTableCell { Value = SDate.ToString(DateFormat) + "<br/>" + STime.ToString(@"hh\:mm") + "~" + ETime.ToString(@"hh\:mm") });
                    }
                }
                else
                    cTR.Cs.Add(new cTableCell { Value = "尚未設定" });

                cTR.Cs.Add(new cTableCell { Value = N_.Price_Basic.ToString() });//原價
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/ProductClass_List?PID=" + N_.PID, Target = "_self", Value = "班級管理" });//班別
                //cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/ProductAllowAccount_List/" + N_.PID, Target = "_self", Value = "指定名單" });//限定會員
                cTR.Cs.Add(new cTableCell { Value = N_.ShowFlag ? "顯示" : "隱藏" });//顯示狀態
                cTR.Cs.Add(new cTableCell { Value = N_.ActiveFlag ? "可交易" : "不可交易" });//交易狀態
                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion


            return c;
        }
        [HttpGet]
        public ActionResult Product_List()
        {
            GetViewBag();
            return View(GetProduct_List(null));
        }
        [HttpPost]
        public ActionResult Product_List(FormCollection FC = null)
        {
            GetViewBag();
            return View(GetProduct_List(FC));
        }
        #endregion
        #region 上架課程-編輯
        public class cProduct_Edit
        {
            public Product P = new Product();
            public List<SelectListItem> CCSL = new List<SelectListItem>();
            public List<SelectListItem> CSL = new List<SelectListItem>();
            public List<SelectListItem> OI2SL = new List<SelectListItem>();
            //擋修限制
            public List<cProduct_Before> PBs = new List<cProduct_Before>();
            public List<Product_Rool> PRs = new List<Product_Rool>();
            public ListSelect OSL = new ListSelect();
            public string[] sCourseType = new string[0];
            //細節設定
            public ListSelect Years = new ListSelect();
            public ListSelect Echelons = new ListSelect();
            //付款設定
            public ListSelect PTSL = new ListSelect();
        }
        public class cProduct_Before
        {
            public int PRID = 0;
            public int CRID = 0;
            public List<SelectListItem> CCSL_Before = new List<SelectListItem>();
            public List<SelectListItem> CSL_Before = new List<SelectListItem>();
        }

        public cProduct_Edit GetProduct_Edit(int ID, FormCollection FC, HttpPostedFileBase file_upload)
        {
            cProduct_Edit N = new cProduct_Edit();
            ACID = GetACID();
            int CID = 0;
            #region 物件初始化
            N.sCourseType = sCourseType;
            var CCs = DC.Course_Category.Where(q => !q.DeleteFlag && q.ActiveFlag && q.Course.Count(p => p.ActiveFlag && !p.DeleteFlag) > 0).OrderByDescending(q => q.CCID).ToList();
            foreach (var CC in CCs) N.CCSL.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString() });
            if (CCs.Count == 0)
                SetAlert("請先新增課程分類", 3, "/Admin/StoreSet/Product_List");
            else
            {
                N.CCSL[0].Selected = true;
                var Cs = DC.Course.Where(q => q.ActiveFlag && !q.DeleteFlag && q.CCID.ToString() == N.CCSL[0].Value).OrderBy(q => q.CCID);
                foreach (var C in Cs)
                {
                    if (CID == 0)
                    {
                        CID = C.CID;
                        N.CSL.Add(new SelectListItem { Text = C.Title, Value = C.CID.ToString(), Selected = true });
                    }
                    else
                        N.CSL.Add(new SelectListItem { Text = C.Title, Value = C.CID.ToString() });
                }
            }
            //付款方式
            N.PTSL = new ListSelect();
            N.PTSL.ControlName = "cbox_PayType";
            N.PTSL.ddlList = new List<SelectListItem>();
            for (int i = 0; i < sPayType.Length; i++)
                N.PTSL.ddlList.Add(new SelectListItem { Text = sPayType[i], Value = i.ToString() });
            //所屬旌旗

            //後臺使用者限制使用那些旌旗資訊
            int Min_OIID = 0;
            var OI2s = DC.M_OI2_Account.Where(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag && q.OIID == 1);
            if (OI2s.Any())
            {
                var OIs = DC.OrganizeInfo.Where(q => q.OID == 2 && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.OIID);
                foreach (var OI in OIs) N.OI2SL.Add(new SelectListItem { Text = OI.Title + OI.Organize.Title, Value = OI.OIID.ToString(), Selected = OI.OIID == OIs.Min(q => q.OIID) });

                Min_OIID = OIs.Min(q => q.OIID);
            }
            else
            {
                OI2s = DC.M_OI2_Account.Where(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag && q.OIID > 2);
                if (OI2s.Any())
                {
                    var OIs = (from q in DC.OrganizeInfo.Where(q => q.OID == 2 && q.ActiveFlag && !q.DeleteFlag)
                               join p in OI2s
                               on q.OIID equals p.OIID
                               select q)
                              .OrderBy(q => q.OIID);
                    foreach (var OI in OIs) N.OI2SL.Add(new SelectListItem { Text = OI.Title + OI.Organize.Title, Value = OI.OIID.ToString(), Selected = OI.OIID == OIs.Min(q => q.OIID) });

                    Min_OIID = OIs.Min(q => q.OIID);
                }
                else
                    SetAlert("此使用者未設定所屬旌旗,請設定完成後再新增/編輯上架課程", 2, "/Admin/StoreSet/Product_List");
            }

            //擋修用選單
            N.PBs = new List<cProduct_Before>();
            cProduct_Before PB = new cProduct_Before();
            PB.PRID = 0;
            PB.CRID = 0;
            CCs = DC.Course_Category.Where(q => !q.DeleteFlag && q.Course.Count(p => p.ActiveFlag && !p.DeleteFlag && p.CID != CID) > 0).OrderByDescending(q => q.CCID).ToList();
            PB.CCSL_Before.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            foreach (var CC in CCs)
                PB.CCSL_Before.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString() });
            PB.CSL_Before = new List<SelectListItem>();
            N.PBs.Add(PB);

            //組織
            N.OSL.Title = "";
            N.OSL.SortNo = 0;
            var Os = DC.Organize.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList();
            var O = Os.FirstOrDefault(q => q.ParentID == 0);
            while (O != null)
            {
                if (O.JobTitle != "")
                    N.OSL.ddlList.Add(new SelectListItem { Text = O.JobTitle, Value = O.OID.ToString() });
                O = Os.FirstOrDefault(q => q.ParentID == O.OID);
            };

            //年度
            for (int i = 10; i > 0; i--)
            {
                int Y = DT.Year - 8 + i;
                N.Years.ddlList.Add(new SelectListItem { Text = Y.ToString(), Value = Y.ToString(), Selected = (Y == DT.Year) });
            }
            //梯次
            for (int i = 1; i <= 10; i++)
                N.Echelons.ddlList.Add(new SelectListItem { Text = "第" + i + "梯次", Value = i.ToString(), Selected = i == 1 });
            #endregion


            #region 資料庫資料帶入
            N.P = DC.Product.FirstOrDefault(q => q.PID == ID);
            if (N.P == null)
            {
                var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 2 && q.OIID == Min_OIID);
                N.P = new Product
                {
                    CID = 0,
                    OrganizeInfo = OI,
                    Title = "",
                    SubTitle = "",
                    ProductType = 0,
                    ProductInfo = "",
                    TargetInfo = "",
                    GraduationInfo = "",
                    YearNo = DT.Year,
                    EchelonNo = 1,
                    ImgURL = "",
                    Price_Basic = 0,
                    Price_Early = 0,

                    SDate_Signup = DT,
                    EDate_Signup = DT,
                    SDate_Early = DT,
                    EDate_Early = DT,
                    ShowFlag = false,
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID
                };
                if (CID > 0)
                {
                    var CRs = DC.Course_Rool.Where(q => q.CID == CID).OrderBy(q => q.CID);
                    foreach (var CR in CRs)
                    {
                        if (CR.TargetType == 0)//擋修
                        {
                            PB = new cProduct_Before();
                            PB.PRID = 0;
                            PB.CRID = CR.CRID;
                            CCs = DC.Course_Category.Where(q => !q.DeleteFlag && q.Course.Count(p => p.ActiveFlag && !p.DeleteFlag && p.CID != ID) > 0).OrderByDescending(q => q.CCID).ToList();
                            foreach (var CC in CCs) PB.CCSL_Before.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString(), Selected = CC.CCID == CR.Course.CCID });
                            var Cs = DC.Course.Where(q => q.CCID == CR.Course.CCID).OrderByDescending(q => q.CID);
                            foreach (var C in Cs) PB.CSL_Before.Add(new SelectListItem { Text = C.Title, Value = C.CCID.ToString(), Selected = CR.CID == CID });

                            N.PBs.Add(PB);
                        }
                        else if (CR.TargetType == 1)//職分
                        {
                            N.OSL.SortNo = CR.CRID;
                            if (N.OSL.ddlList.Any(q => q.Value == CR.TargetInt1.ToString()))
                                N.OSL.ddlList.First(q => q.Value == CR.TargetInt1.ToString()).Selected = true;
                        }

                        N.PRs.Add(new Product_Rool
                        {
                            PID = 0,
                            CRID = CR.CRID,
                            TargetType = CR.TargetType,
                            TargetInt1 = CR.TargetInt1,
                            TargetInt2 = CR.TargetInt2,
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = ACID
                        });


                    }
                }


                //依據所屬旌旗的協會初始化付款方式可選擇的部分
                var OI1 = DC.OrganizeInfo.FirstOrDefault(q => q.OID == 1 && q.OIID == N.P.OrganizeInfo.ParentID);
                if (OI1 != null)
                {
                    var PTs = DC.PayType.Where(q => q.OIID == OI1.OIID);
                    foreach (var PT in PTs)
                        N.PTSL.ddlList.First(q => q.Value == PT.PayTypeID.ToString()).Disabled = !PT.ActiveFlag;
                }
            }
            else
            {
                //所屬旌旗
                N.OI2SL.ForEach(q => q.Selected = false);
                N.OI2SL.Find(q => q.Value == N.P.OIID.ToString()).Selected = true;

                if (N.CCSL.FirstOrDefault(q => q.Value == N.P.Course.CCID.ToString()) != null)
                {
                    N.CCSL.ForEach(q => q.Selected = false);
                    N.CCSL.FirstOrDefault(q => q.Value == N.P.Course.CCID.ToString()).Selected = true;
                    N.CSL = new List<SelectListItem>();
                    var Cs = DC.Course.Where(q => q.ActiveFlag && !q.DeleteFlag && q.CCID == N.P.Course.CCID).OrderBy(q => q.CCID);
                    foreach (var C in Cs)
                    {
                        if (C.CID == N.P.CID)
                            N.CSL.Add(new SelectListItem { Text = C.Title, Value = C.CID.ToString(), Selected = true });
                        else
                            N.CSL.Add(new SelectListItem { Text = C.Title, Value = C.CID.ToString() });
                    }
                }

                N.PRs = N.P.Product_Rool.ToList();
                N.PBs.Clear();
                //擋修
                foreach (var PR in N.PRs.Where(q => q.TargetType == 0).OrderBy(q => q.PRID))
                {
                    var C_ = DC.Course.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.CID == PR.TargetInt1);
                    if (C_ != null)
                    {
                        PB = new cProduct_Before();
                        PB.PRID = PR.PRID;
                        PB.CRID = PR.CRID;
                        CCs = DC.Course_Category.Where(q => !q.DeleteFlag && q.Course.Count(p => p.ActiveFlag && !p.DeleteFlag && p.CID != ID) > 0).OrderByDescending(q => q.CCID).ToList();
                        foreach (var CC in CCs) PB.CCSL_Before.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString(), Selected = CC.CCID == C_.CCID });
                        var Cs = DC.Course.Where(q => q.CCID == C_.CCID).OrderByDescending(q => q.CID);
                        foreach (var C in Cs) PB.CSL_Before.Add(new SelectListItem { Text = C.Title, Value = C.CCID.ToString() });

                        N.PBs.Add(PB);
                    }
                    else//這堂課已經不見了~就直接移除
                    {
                        PR.TargetInt2 = -1;
                    }
                }

                //職分
                foreach (var PR1 in N.PRs.Where(q => q.TargetType == 1))
                {
                    if (N.OSL.ddlList.Any(q => q.Value == PR1.TargetInt1.ToString()))
                        N.OSL.ddlList.Find(q => q.Value == PR1.TargetInt1.ToString()).Selected = true;
                }
                //年分
                N.Years.ddlList.ForEach(q => q.Selected = false);
                if (N.Years.ddlList.FirstOrDefault(q => q.Value == N.P.YearNo.ToString()) == null)
                    N.Years.ddlList.Add(new SelectListItem { Text = N.P.YearNo.ToString(), Value = N.P.YearNo.ToString(), Selected = true });
                else
                    N.Years.ddlList.First(q => q.Value == N.P.YearNo.ToString()).Selected = true;
                //梯次
                N.Echelons.ddlList.ForEach(q => q.Selected = false);
                N.Echelons.ddlList.First(q => q.Value == N.P.EchelonNo.ToString()).Selected = true;


                //依據所屬旌旗的協會初始化付款方式可選擇的部分
                var OI1 = DC.OrganizeInfo.FirstOrDefault(q => q.OID == 1 && q.OIID == N.P.OrganizeInfo.ParentID);
                if (OI1 != null)
                {
                    var PTs = DC.PayType.Where(q => q.OIID == OI1.OIID);
                    if (PTs.Count() > 0)
                        foreach (var PT in PTs)
                            N.PTSL.ddlList.First(q => q.Value == PT.PayTypeID.ToString()).Disabled = !PT.ActiveFlag;
                    else
                        N.PTSL.ddlList.ForEach(q => q.Disabled = true);
                }

                //付款方式
                var PPTs = DC.M_Product_PayType.Where(q => q.PID == N.P.PID);
                foreach (var PPT in PPTs)
                    N.PTSL.ddlList.Find(q => q.Value == PPT.PayType.PayTypeID.ToString()).Selected = PPT.ActiveFlag;
            }

            #endregion
            #region 前端資料帶入
            if (FC != null)
            {

                if (!string.IsNullOrEmpty(FC.Get("rbl_OI")))
                {
                    var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID.ToString() == FC.Get("rbl_OI"));
                    if (OI != null)
                        N.P.OrganizeInfo = OI;
                }
                //N.P.Title = FC.Get("txb_Title");

                N.P.SubTitle = FC.Get("txb_SubTitle");
                N.P.ProductInfo = FC.Get("txb_ProductInfo");
                N.P.TargetInfo = FC.Get("txb_TargetInfo");
                N.P.GraduationInfo = FC.Get("txb_GraduationInfo");
                N.P.ShowFlag = GetViewCheckBox(FC.Get("cbox_ShowFlag"));
                N.P.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                N.P.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                N.P.ProductType = Convert.ToInt32(FC.Get("rbl_ProductType"));
                N.P.YearNo = Convert.ToInt32(FC.Get("ddl_Year"));

                N.P.EchelonNo = Convert.ToInt32(FC.Get("ddl_EchelonNo"));
                N.P.Price_Basic = Convert.ToInt32(FC.Get("txb_Price_Basic"));
                int iPrice = 0;
                if (int.TryParse(FC.Get("txb_Price_Early"), out iPrice))
                    N.P.Price_Early = iPrice;
                else
                    N.P.Price_Early = 0;


                if (!string.IsNullOrEmpty(FC.Get("txb_SDate_Signup")))
                    N.P.SDate_Signup = Convert.ToDateTime(FC.Get("txb_SDate_Signup"));
                if (!string.IsNullOrEmpty(FC.Get("txb_EDate_Signup")))
                    N.P.EDate_Signup = Convert.ToDateTime(FC.Get("txb_EDate_Signup"));
                if (!string.IsNullOrEmpty(FC.Get("txb_SDate_Early")))
                    N.P.SDate_Early = Convert.ToDateTime(FC.Get("txb_SDate_Early"));
                if (!string.IsNullOrEmpty(FC.Get("txb_EDate_Early")))
                    N.P.EDate_Early = Convert.ToDateTime(FC.Get("txb_EDate_Early"));

                N.P.UpdDate = DT;
                N.P.SaveACID = ACID;
                N.CCSL.ForEach(q => q.Selected = false);
                N.CCSL.FirstOrDefault(q => q.Value == FC.Get("ddl_CCBasic")).Selected = true;
                string zzz = FC.Get("ddl_CBasic");
                var Cou = DC.Course.FirstOrDefault(q => q.CID == Convert.ToInt32(FC.Get("ddl_CBasic")));
                if (Cou != null)
                    N.P.Course = Cou;
                N.CSL = new List<SelectListItem>();
                var Cs = DC.Course.Where(q => q.ActiveFlag && !q.DeleteFlag && q.CCID.ToString() == FC.Get("ddl_CCBasic")).OrderBy(q => q.CCID);
                foreach (var C in Cs)
                {
                    if (C.CID == N.P.CID)
                    {
                        N.CSL.Add(new SelectListItem { Text = C.Title, Value = C.CID.ToString(), Selected = true });
                        N.P.Title = C.Title;
                    }
                    else
                        N.CSL.Add(new SelectListItem { Text = C.Title, Value = C.CID.ToString() });
                }
                //圖片上傳
                bool CheckFileFlag = true;
                if (file_upload == null)
                    CheckFileFlag = false;
                else if (file_upload.ContentLength <= 0 || file_upload.ContentLength > 5242880)
                    CheckFileFlag = false;
                else if (file_upload.ContentType != "image/png" && file_upload.ContentType != "image/jpeg")
                    CheckFileFlag = false;
                if (CheckFileFlag)
                {
                    string Ex = Path.GetExtension(file_upload.FileName);
                    string FileName = $"{DT.ToString("yyyyMMddHHmmssfff")}{Ex}";
                    string SavaPath = Path.Combine(Server.MapPath("~/Photo/Product/"), FileName);
                    file_upload.SaveAs(SavaPath);
                    N.P.ImgURL = "/Photo/Product/" + FileName;
                }
                //擋修與對象
                //新增UI新增的部分
                #region 先修課程
                if (N.PRs.Count(q => q.TargetType == 0) == 0)
                {
                    int iCt = Convert.ToInt32(FC.Get("txb_AddCourseCt"));
                    for (int i = 0; i <= iCt; i++)
                    {
                        if (!string.IsNullOrEmpty(FC.Get("ddl_C_" + i)))
                        {
                            N.PRs.Add(new Product_Rool
                            {
                                Product = N.P,
                                CRID = 0,
                                TargetType = 0,
                                TargetInt1 = Convert.ToInt32(FC.Get("ddl_C_" + i)),
                                TargetInt2 = 0,
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = ACID
                            });
                        }
                    }
                }

                #endregion
                #region 性別
                if (N.PRs.FirstOrDefault(q => q.TargetType == 2) == null && FC.Get("rbl_Sex") != null)
                {
                    if (FC.Get("rbl_Sex") != "-1")
                    {
                        N.PRs.Add(new Product_Rool
                        {
                            Product = N.P,
                            CRID = 0,
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
                if (N.PRs.FirstOrDefault(q => q.TargetType == 3) == null && (iMin > 0 || iMax > 0))
                {
                    N.PRs.Add(new Product_Rool
                    {
                        Product = N.P,
                        CRID = 0,
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
                if (N.OSL.SortNo <= 0)
                {
                    foreach (var _O in N.OSL.ddlList)
                    {
                        _O.Selected = GetViewCheckBox(FC.Get("cbox_O" + _O.Value));
                        var CR = N.PRs.FirstOrDefault(q => q.TargetInt1.ToString() == _O.Value && q.TargetType == 1);
                        if (_O.Selected && CR == null)
                        {
                            N.PRs.Add(new Product_Rool
                            {
                                Product = N.P,
                                CRID = 0,
                                TargetType = 1,
                                TargetInt1 = Convert.ToInt32(_O.Value),
                                TargetInt2 = 0,
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = ACID
                            });
                        }
                        else if (!_O.Selected && CR != null)
                            CR.TargetInt2 = -1;
                    }
                }

                #endregion
                #region 付款方式
                foreach (var ddl in N.PTSL.ddlList)
                    ddl.Selected = GetViewCheckBox(FC.Get(N.PTSL.ControlName + ddl.Value));
                #endregion
            }

            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult Product_Edit(int ID)
        {
            GetViewBag();

            ChangeTitle(ID == 0);
            return View(GetProduct_Edit(ID, null, null));
        }


        [HttpPost]
        //
        public ActionResult Product_Edit(int ID, FormCollection FC, HttpPostedFileBase file_upload)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            var N = GetProduct_Edit(ID, FC, file_upload);
            #region 檢查輸入

            if (N.P.SubTitle == "")
                Error += "請輸入課程副標題<br/>";
            if (N.P.Price_Basic <= 0)
                Error += "請輸入課程原價<br/>";
            if (N.P.Price_Basic < N.P.Price_Early)
                Error += "課程原價應低於早鳥價<br/>";
            if (N.P.SDate_Early != N.P.CreDate && N.P.EDate_Early != N.P.CreDate)
            {
                if (N.P.SDate_Early.Date > N.P.EDate_Early.Date)
                    Error += "早鳥報名起始日應在結束日之前<br/>";
            }
            if (N.P.SDate_Signup != N.P.CreDate && N.P.EDate_Signup != N.P.CreDate)
            {
                if (N.P.SDate_Signup.Date > N.P.EDate_Signup.Date)
                    Error += "線上報名起始日應在結束日之前<br/>";
                else if (N.P.EDate_Early != N.P.CreDate && N.P.EDate_Early.Date > N.P.EDate_Signup.Date)
                    Error += "早鳥結束日應在線上報名結束日之前<br/>";
            }
            else
                Error += "線上報名起始日 與 結束日 為必填<br/>";

            if (N.P.SDate_Signup != N.P.CreDate && N.P.EDate_Signup.Date < N.P.SDate_Early.Date)
                Error += "線上報名起始日應在線上報名結束日之前<br/>";
            #endregion


            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (N.P.PID == 0)
                {
                    N.P.CreDate = N.P.UpdDate;
                    DC.Product.InsertOnSubmit(N.P);
                }
                DC.SubmitChanges();
                foreach (var PR in N.PRs.Where(q => q.PRID == 0))
                {
                    PR.PID = N.P.PID;
                    DC.Product_Rool.InsertOnSubmit(PR);
                    DC.SubmitChanges();
                }

                //DC.Product_Rool.DeleteAllOnSubmit(N.PRs.Where(q => q.TargetInt2 < 0));
                //DC.SubmitChanges();
                var Ms = DC.M_Product_PayType.Where(q => q.PID == N.P.PID).ToList();
                foreach (var ddl in N.PTSL.ddlList)
                {
                    if (Ms.Any(q => q.PayType.PayTypeID.ToString() == ddl.Value))
                    {
                        var M = Ms.Find(q => q.PayType.PayTypeID.ToString() == ddl.Value);
                        M.ActiveFlag = ddl.Selected;
                        M.UpdDate = DT;
                        M.SaveACID = ACID;
                        DC.SubmitChanges();
                    }
                    else if (ddl.Selected)
                    {
                        var OI1 = (from q in DC.OrganizeInfo.Where(q => q.OID == 2 && q.OIID == N.P.OIID)
                                   join p in DC.OrganizeInfo.Where(q => q.OID == 1)
                                   on q.ParentID equals p.OIID
                                   select p).FirstOrDefault();
                        if (OI1 != null)
                        {
                            var PT = DC.PayType.FirstOrDefault(q => q.OIID == OI1.OIID && q.PayTypeID.ToString() == ddl.Value);
                            if (PT != null)
                            {
                                M_Product_PayType M = new M_Product_PayType
                                {
                                    Product = N.P,
                                    PayType = PT,
                                    ActiveFlag = ddl.Selected,
                                    CreDate = DT,
                                    UpdDate = DT,
                                    SaveACID = ACID
                                };
                                DC.M_Product_PayType.InsertOnSubmit(M);
                                DC.SubmitChanges();
                            }
                        }
                    }
                }


                SetAlert("存檔完成", 1, "/Admin/StoreSet/Product_List");
            }

            return View(N);
        }
        #endregion

        #region 上架課程-班別列表
        public class cProductClass_List
        {
            public int PID = 0;
            public string sKey = "";
            public string sP_Title = "";
            public cTableList cTL = new cTableList();
        }
        public cProductClass_List GetProductClass_List(int PID, FormCollection FC)
        {
            cProductClass_List N = new cProductClass_List();
            N.PID = PID;
            var P = DC.Product.FirstOrDefault(q => q.PID == PID);
            if (P != null)
                N.sP_Title = P.Title + P.SubTitle;
            N.sKey = FC != null ? FC.Get("txb_Key") : "";
            N.cTL.Rs = new List<cTableRow>();
            var Ns = DC.Product_Class.Where(q => q.PID == PID && !q.DeleteFlag);

            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "課程名稱" });
            TopTitles.Add(new cTableCell { Title = "班級名稱" });
            TopTitles.Add(new cTableCell { Title = "開班日設定" });
            TopTitles.Add(new cTableCell { Title = "人數" });
            TopTitles.Add(new cTableCell { Title = "地點" });
            TopTitles.Add(new cTableCell { Title = "講師" });
            TopTitles.Add(new cTableCell { Title = "刪除" });

            N.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            N.cTL.TotalCt = Ns.Count();
            N.cTL.MaxNum = GetMaxNum(N.cTL.TotalCt, N.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.PID).Skip((iNowPage - 1) * N.cTL.NumCut).Take(N.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/ProductClass_Edit/" + N_.PCID + "?PID=" + N_.PID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = N_.Product.Title });//課程名稱
                cTR.Cs.Add(new cTableCell { Value = N_.Title });//班級名稱
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/ProductClassDate_List/0?PID=" + N_.PID + "&PCID=" + N_.PCID, Value = "開班日管理" });//開班日設定"

                cTR.Cs.Add(new cTableCell { Value = N_.PeopleCt.ToString() });//人數

                string sLocation = string.IsNullOrEmpty(N_.LocationName) ? "" : N_.LocationName;
                if (N_.Product.ProductType == 0)
                    sLocation += (sLocation == "" ? "" : "<br/>") + N_.Address + "<br/>" + N_.MeetURL;
                else if (N_.Product.ProductType == 1)
                    sLocation += (sLocation == "" ? "" : "<br/>") + N_.Address;
                else if (N_.Product.ProductType == 2)
                    sLocation += (sLocation == "" ? "" : "<br/>") + N_.MeetURL;

                cTR.Cs.Add(new cTableCell { Value = sLocation });//地點
                var MPT = (from q in DC.M_Product_Teacher.Where(q => q.PID == N_.PID && q.PCID == N_.PCID)
                           join p in DC.Teacher.Where(q => !q.DeleteFlag)
                           on q.TID equals p.TID
                           select p).FirstOrDefault();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/ProductClassTeacher_List/" + N_.PCID + "?PID=" + N_.PID, Target = "_self", Value = (MPT != null ? MPT.Title : "設定講師") });//編輯
                cTR.Cs.Add(new cTableCell { Type = "deletebutton", URL = "DataDelete('Product_Class', " + N_.PCID + ")", CSS = "btn btn-outline-danger", Value = "刪除" });//刪除
                N.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }

            return N;
        }
        [HttpGet]
        public ActionResult ProductClass_List()
        {
            GetViewBag();
            int PID = GetQueryStringInInt("PID");
            return View(GetProductClass_List(PID, null));
        }
        [HttpPost]
        public ActionResult ProductClass_List(FormCollection FC)
        {
            GetViewBag();
            int PID = GetQueryStringInInt("PID");
            return View(GetProductClass_List(PID, null));
        }
        #endregion
        #region 上架課程-班別新增/修改
        public class cProductClass_Edit
        {
            public Product_Class PC = new Product_Class();
            public int ProductType = 0;
        }
        public cProductClass_Edit GetProductClass_Edit(int PID, int ID, FormCollection FC)
        {
            cProductClass_Edit N = new cProductClass_Edit();
            #region 物件初始化

            #endregion
            #region 載入資料
            var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == ID && q.PID == PID && !q.DeleteFlag);
            if (PC == null)
            {
                N.PC = new Product_Class
                {
                    PID = PID,
                    Title = "",

                    PeopleCt = 0,
                    PhoneNo = "",
                    LocationName = "",
                    Address = "",
                    MeetURL = "",
                    GraduateDate = DT,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = 1
                };
            }
            else
            {
                N.PC = PC;
            }
            var P = DC.Product.FirstOrDefault(q => q.PID == PID);
            if (P == null)
                SetAlert("參數遺失,請重試一次", 2, "/Admin/StoreSet/Product_List");
            else
                N.ProductType = P.ProductType;

            #endregion
            #region 前端載入
            if (FC != null)
                if (FC.Keys.Count == 0)
                    FC = null;
            if (FC != null)
            {
                N.PC.Title = FC.Get("txb_Title");
                N.PC.PeopleCt = Convert.ToInt32(FC.Get("txb_PeopleCt"));
                N.PC.PhoneNo = FC.Get("txb_PhoneNo");
                N.PC.LocationName = FC.Get("txb_LocationName");
                DateTime DT_ = DateTime.Now;
                DateTime.TryParse(FC.Get("txb_GraduateDate"), out DT_);
                N.PC.GraduateDate = DT_;
                if (N.ProductType == 0 || N.ProductType == 1)
                    N.PC.Address = FC.Get("txb_Address");
                else
                    N.PC.Address = "";

                if (N.ProductType == 0 || N.ProductType == 2)
                    N.PC.MeetURL = FC.Get("txb_MeetURL");
                else
                    N.PC.MeetURL = "";
            }
            #endregion
            #region 檢查
            if (N.PC.PCID > 0)
            {
                if (N.PC.Product_ClassTime.Count() > 0)
                {
                    DateTime MaxClassDate = N.PC.Product_ClassTime.Max(q => q.ClassDate);
                    if (MaxClassDate.Date > N.PC.GraduateDate.Date)
                        Error += "您設定的結業日應大於目前課程的最後上課日:" + MaxClassDate.ToString(DateFormat);
                }
            }

            #endregion
            return N;
        }
        [HttpGet]
        public ActionResult ProductClass_Edit(int ID)
        {
            GetViewBag();
            CheckVCookie();
            int PID = GetQueryStringInInt("PID");
            ChangeTitle(ID == 0);
            return View(GetProductClass_Edit(PID, ID, null));
        }
        [HttpPost]
        
        public ActionResult ProductClass_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            int PID = GetQueryStringInInt("PID");
            ChangeTitle(ID == 0);
            var N = GetProductClass_Edit(PID, ID, FC);
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (N.PC.PCID == 0)
                {
                    DC.Product_Class.InsertOnSubmit(N.PC);
                }
                DC.SubmitChanges();
                SetAlert("存檔完成", 1, "/Admin/StoreSet/ProductClass_List?PID=" + PID);

            }

            return View(N);
        }
        #endregion

        #region 上架課程-班別-開課日期列表
        public class cProductClassDate_List
        {
            public int PID = 0;
            public int PCID = 0;
            public cTableList cTL = new cTableList();
        }
        public cProductClassDate_List GetProductClassDate_List(int PID, int PCID, FormCollection FC)
        {
            cProductClassDate_List N = new cProductClassDate_List();
            N.PID = PID;
            N.PCID = PCID;
            N.cTL.Rs = new List<cTableRow>();
            var Ns = DC.Product_ClassTime.Where(q => q.PCID == PCID);
            var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == PCID);
            if (PC != null)
                ViewBag._Title += " - " + PC.Product.Title + PC.Product.SubTitle + " " + PC.Title;

            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "日期" });
            TopTitles.Add(new cTableCell { Title = "起始時間" });
            TopTitles.Add(new cTableCell { Title = "結束時間" });

            N.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            N.cTL.TotalCt = Ns.Count();
            N.cTL.MaxNum = GetMaxNum(N.cTL.TotalCt, N.cTL.NumCut);
            Ns = Ns.OrderBy(q => q.ClassDate).Skip((iNowPage - 1) * N.cTL.NumCut).Take(N.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/ProductClassDate_Edit/" + N_.PCTID + "?PID=" + PID + "&PCID=" + N_.PCID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = N_.ClassDate.ToString(DateFormat) });//日期
                cTR.Cs.Add(new cTableCell { Value = N_.STime.ToString(@"hh\:mm") });//起始時間
                cTR.Cs.Add(new cTableCell { Value = N_.ETime.ToString(@"hh\:mm") });//結束時間
                N.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }

            return N;
        }
        [HttpGet]
        public ActionResult ProductClassDate_List()
        {
            GetViewBag();
            return View(GetProductClassDate_List(GetQueryStringInInt("PID"), GetQueryStringInInt("PCID"), null));
        }
        [HttpPost]
        public ActionResult ProductClassDate_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetProductClassDate_List(GetQueryStringInInt("PID"), GetQueryStringInInt("PCID"), null));
        }
        #endregion
        #region 上架課程-班別-開課日期新增/修改

        public class cProductClassDate_Edit
        {
            public int PID = 0;
            public int PCID = 0;
            public Product_Class PC = new Product_Class();
            public Product_ClassTime PCT = new Product_ClassTime();
        }
        public cProductClassDate_Edit GetProductClassDate_Edit(int PID, int PCID, int ID, FormCollection FC)
        {
            cProductClassDate_Edit N = new cProductClassDate_Edit();
            #region 物件初始化
            N.PID = PID;
            N.PCID = PCID;
            #endregion
            #region 載入資料
            var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == PCID && !q.DeleteFlag);
            if (PC == null)
                SetAlert("此班級不存在...", 2, "/Admin/StoreSet/Product_List");
            else
            {
                N.PC = PC;
                var PCT = DC.Product_ClassTime.FirstOrDefault(q => q.PCTID == ID && q.PCID == PCID);
                if (PCT == null)
                {
                    N.PCT = new Product_ClassTime
                    {
                        PCID = PCID,
                        ClassDate = DT,
                        STime = new TimeSpan(9, 0, 0),
                        ETime = new TimeSpan(12, 0, 0)

                    };
                }
                else
                {
                    N.PCT = PCT;
                }
            }

            #endregion
            #region 前端載入
            if (FC != null)
                if (FC.Keys.Count == 0)
                    FC = null;
            if (FC != null)
            {
                N.PCT.ClassDate = Convert.ToDateTime(FC.Get("txb_ClassDate"));
                N.PCT.STime = TimeSpan.Parse(FC.Get("txb_STime"));
                N.PCT.ETime = TimeSpan.Parse(FC.Get("txb_ETime"));
            }
            #endregion
            #region 檢查

            if (N.PCT.ClassDate.Date > N.PC.GraduateDate.Date)
                Error += "您設定的結業日" + N.PC.GraduateDate.ToString(DateFormat) + "應大於上課日期<br/>";
            if (N.PCT.STime >= N.PCT.ETime)
                Error += "課程結束時間應晚與開始時間<br/>";
            #endregion
            return N;
        }
        [HttpGet]
        public ActionResult ProductClassDate_Edit(int ID)
        {
            GetViewBag();
            CheckVCookie();
            ChangeTitle(ID == 0);
            return View(GetProductClassDate_Edit(GetQueryStringInInt("PID"), GetQueryStringInInt("PCID"), ID, null));
        }
        [HttpPost]
        
        public ActionResult ProductClassDate_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            var N = GetProductClassDate_Edit(GetQueryStringInInt("PID"), GetQueryStringInInt("PCID"), ID, FC);
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (N.PCT.PCTID == 0)
                {
                    DC.Product_ClassTime.InsertOnSubmit(N.PCT);
                }
                DC.SubmitChanges();
                SetAlert("存檔完成", 1, "/Admin/StoreSet/ProductClassDate_List?PID=" + N.PID + "&PCID=" + N.PCID);
            }

            return View(N);
        }
        #endregion
        #region 上課課程-班別-批次新增日期
        [HttpGet]
        public string CreateClassDate()
        {
            string sReturn = "";
            string sSDate = GetQueryStringInString("SDate");
            string sSTime = GetQueryStringInString("STime");
            string sETime = GetQueryStringInString("ETime");

            DateTime SDate = DateTime.Now;
            TimeSpan STime = new TimeSpan();
            TimeSpan ETime = new TimeSpan();
            int PCID = GetQueryStringInInt("PCID");
            int DaySpan = GetQueryStringInInt("DaySpan");
            int No = GetQueryStringInInt("No");
            if (!DateTime.TryParse(sSDate, out SDate))
                sReturn = "起始日期格式錯誤,請輸入日期";
            if (!TimeSpan.TryParse(sSTime, out STime))
                sReturn = "起始時間格式錯誤,請輸入時與分";
            if (!TimeSpan.TryParse(sETime, out ETime))
                sReturn = "結束時間格式錯誤,請輸入時與分";

            if (DaySpan <= 0)
                sReturn = "相隔週期請重新輸入";
            if (No <= 0)
                sReturn = "上課堂數請重新輸入";
            if (sReturn == "")
            {
                var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == PCID && !q.DeleteFlag);
                if (PC != null)
                {
                    DateTime Max_Date = SDate.AddDays(DaySpan * No);
                    if (PC.GraduateDate.Date < Max_Date.Date)
                        sReturn = "您設定的結業日" + PC.GraduateDate.ToString(DateFormat) + "預期將小於最後上課日:" + Max_Date.ToString(DateFormat) + ",請修改本班級結業日或批次數值以符合規則";
                    else
                    {
                        List<Product_ClassTime> PCTs = new List<Product_ClassTime>();
                        for (int i = 0; i < No; i++)
                        {
                            PCTs.Add(new Product_ClassTime
                            {
                                Product_Class = PC,
                                ClassDate = SDate.AddDays(i * DaySpan),
                                STime = STime,
                                ETime = ETime
                            });
                        }
                        DC.Product_ClassTime.InsertAllOnSubmit(PCTs);
                        DC.SubmitChanges();
                    }

                }
                else
                    sReturn = "缺少上課資料...無法新增";
            }
            return sReturn == "" ? "OK" : sReturn;
        }
        #endregion
        #region 上課課程-班別講師設定
        public cTeacher_List ProductClassTeacher_List(int ID, int PID, FormCollection FC)
        {
            cTeacher_List N = new cTeacher_List();
            #region 物件初始化
            #region 前端資料帶入
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            //N.ActiveType = Convert.ToInt32(FC != null ? FC.Get("rbl_ActiveType") : "-1");
            N.sKey = FC != null ? FC.Get("txb_Key") : "";
            N.sGroupKey = FC != null ? FC.Get("txb_GroupKey") : "";
            N.PID = PID;
            var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == ID);
            if (PC != null)
                ViewBag._Title += " - " + PC.Product.Title + PC.Product.SubTitle + " " + PC.Title;
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
            //if (N.ActiveType >= 0)
            //    Ns = Ns.Where(q => q.ActiveFlag == (N.ActiveType == 1));
            if (N.sGroupKey != "")
            {
                Ns = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag &&
                     q.ActiveFlag &&
                     q.OrganizeInfo.OID == 8 &&
                     q.OrganizeInfo.Title.Contains(N.sGroupKey))
                     join p in Ns.Where(q => q.ACID > 0)
                     on q.ACID equals p.ACID
                     select p;
            }
            int TargetID = 0;
            if (ID != 0)
            {
                var PCT = DC.M_Product_Teacher.FirstOrDefault(q => q.PID == PID && q.PCID == ID);
                if (PCT != null)
                    TargetID = PCT.TID;
            }
            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "指定狀態", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "講師名稱" });
            TopTitles.Add(new cTableCell { Title = "小組" });
            TopTitles.Add(new cTableCell { Title = "狀態" });
            TopTitles.Add(new cTableCell { Title = "備註" });

            N.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            N.cTL.TotalCt = Ns.Count();
            N.cTL.MaxNum = GetMaxNum(N.cTL.TotalCt, N.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.TID == TargetID).ThenByDescending(q => q.TID).Skip((iNowPage - 1) * N.cTL.NumCut).Take(N.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                if (N_.TID == TargetID)
                    cTR.Cs.Add(new cTableCell { Value = "已指定", CSS = "btn btn-outline-success", Type = "activebutton", URL = "ChangeTeacher(this," + N_.TID + "," + ID + ")" });//狀態
                else
                    cTR.Cs.Add(new cTableCell { Value = "未指定", CSS = "btn btn-outline-danger", Type = "activebutton", URL = "ChangeTeacher(this," + N_.TID + "," + ID + ")" });//狀態
                cTR.Cs.Add(new cTableCell { Value = N_.Title });//講師名稱
                //小組
                var M = DC.M_OI_Account.FirstOrDefault(q => q.ACID == N_.ACID && q.ActiveFlag && !q.DeleteFlag);
                cTR.Cs.Add(new cTableCell { Value = M != null ? M.OrganizeInfo.Title + M.OrganizeInfo.Organize.Title : "停用" });//小組
                cTR.Cs.Add(new cTableCell { Value = N_.ActiveFlag ? "啟用" : "停用" });//狀態
                cTR.Cs.Add(new cTableCell { Value = N_.Note });//備註

                N.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult ProductClassTeacher_List(int ID)
        {
            GetViewBag();
            int PID = GetQueryStringInInt("PID");
            return View(ProductClassTeacher_List(ID, PID, null));
        }
        [HttpPost]
        public ActionResult ProductClassTeacher_List(int ID, FormCollection FC)
        {
            GetViewBag();
            int PID = GetQueryStringInInt("PID");
            return View(ProductClassTeacher_List(ID, PID, FC));
        }
        #endregion

        #region 折價劵-列表
        public class cCoupon_List
        {
            public cTableList cTL = new cTableList();
            public List<SelectListItem> SL = new List<SelectListItem>();
            public string sKey = "";
            public int ActiveType = -1;
        }
        public cCoupon_List GetCoupon_List(FormCollection FC)
        {
            cCoupon_List c = new cCoupon_List();

            #region 物件初始化
            c.SL = new List<SelectListItem>();
            c.SL.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            var CCs = DC.Course_Category.Where(q => !q.DeleteFlag).OrderByDescending(q => q.CCID);
            foreach (var CC in CCs)
                c.SL.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString() });
            #region 前端資料帶入
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.ActiveType = Convert.ToInt32(FC != null ? FC.Get("rbl_ActiveType") : "-1");
            c.sKey = FC != null ? FC.Get("txb_Key") : "";
            int CCID = Convert.ToInt32(FC != null ? FC.Get("ddl_CC") : "0");
            c.SL.ForEach(q => q.Selected = false);
            c.SL.First(q => q.Value == CCID.ToString()).Selected = true;
            #endregion


            c.cTL = new cTableList();
            c.cTL.Title = "";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            #endregion
            #region 資料庫資料帶入
            var Ns = DC.Coupon_Header.Where(q => !q.DeleteFlag);
            if (c.sKey != "")
                Ns = Ns.Where(q => q.Product.Title.Contains(c.sKey));
            if (CCID > 0)
                Ns = Ns.Where(q => q.Product.Course.CCID == CCID);
            if (c.ActiveType >= 0)
                Ns = Ns.Where(q => q.ActiveFlag == (c.ActiveType == 1));

            //旌旗權限檢視門檻設置
            var OI2s = DC.M_OI2_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID);
            if (OI2s.Any())
            {
                var OI2_1 = OI2s.FirstOrDefault(q => q.OIID == 1);
                if (OI2_1 == null)
                {
                    Ns = from q in Ns
                         join p in OI2s.Where(q => q.OIID > 2)
                         on q.Product.OIID equals p.OIID
                         select q;
                }
            }
            else if (ACID == 1) { }
            else//沒有旌旗權限~暫時就乾脆都先不給看好了
            {
                Ns = Ns.Where(q => q.Product.OIID == 1);
            }

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "課程分類" });
            TopTitles.Add(new cTableCell { Title = "課程名稱" });
            TopTitles.Add(new cTableCell { Title = "可用期間" });
            TopTitles.Add(new cTableCell { Title = "折價金額" });
            TopTitles.Add(new cTableCell { Title = "適用職分" });
            TopTitles.Add(new cTableCell { Title = "分配名單" });
            TopTitles.Add(new cTableCell { Title = "啟用狀態" });

            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.PID).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/Coupon_Edit/" + N_.CHID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = "【" + N_.Product.Course.Course_Category.Code + "】" + N_.Product.Course.Course_Category.Title });//課程分類
                cTR.Cs.Add(new cTableCell { Value = N_.Product.Course.Title + " " + N_.Product.SubTitle });//課程名稱
                cTR.Cs.Add(new cTableCell { Value = (N_.SDateTime.ToString(DateTimeFormat) + "<br/>↕<br/>" + N_.EDateTime.ToString(DateTimeFormat)) });//可用期間
                cTR.Cs.Add(new cTableCell { Value = (N_.Price_Cut.ToString()) });//折價金額
                if (N_.OID == -1)
                    cTR.Cs.Add(new cTableCell { Value = "自行匯入" });//適用職分
                else if (N_.OID == 0)
                    cTR.Cs.Add(new cTableCell { Value = "全體會友" });//適用職分
                else
                {
                    var O = DC.Organize.FirstOrDefault(q => q.OID == N_.OID);
                    cTR.Cs.Add(new cTableCell { Value = (O != null ? O.JobTitle : "") });//適用職分
                }

                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/Coupon_Account_List/" + N_.CHID, Target = "_self", Value = "檢視名單(" + N_.Coupon_Account.Count(q => !q.DeleteFlag) + ")" });//分配名單
                cTR.Cs.Add(new cTableCell { Value = N_.ActiveFlag ? "已啟用" : "已關閉" });//啟用狀態
                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion
            return c;
        }
        [HttpGet]
        public ActionResult Coupon_List()
        {
            GetViewBag();
            return View(GetCoupon_List(null));
        }
        [HttpPost]
        public ActionResult Coupon_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetCoupon_List(FC));
        }

        #endregion
        #region 折價劵-編輯
        public class cCoupon_Edit
        {
            public Coupon_Header CH = new Coupon_Header();
            public List<SelectListItem> P_SL = new List<SelectListItem>();
            public List<SelectListItem> O_SL = new List<SelectListItem>();
        }
        public cCoupon_Edit GetCoupon_Edit(int ID, FormCollection FC)
        {
            cCoupon_Edit c = new cCoupon_Edit();
            #region 物件初始化
            ACID = GetACID();
            var Ps = DC.Product.Where(q => !q.DeleteFlag);
            //旌旗權限檢視門檻設置
            var OI2s = DC.M_OI2_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID);
            if (OI2s.Any())
            {
                var OI2_1 = OI2s.FirstOrDefault(q => q.OIID == 1);
                if (OI2_1 == null)
                {
                    Ps = from q in Ps
                         join p in OI2s.Where(q => q.OIID > 2)
                         on q.OIID equals p.OIID
                         select q;
                }
            }
            else if (ACID == 1) { }
            else//沒有旌旗權限~暫時就乾脆都先不給看好了
            {
                Ps = Ps.Where(q => q.OIID == 1);
            }
            foreach (var P in Ps.OrderByDescending(q => q.CreDate))
                c.P_SL.Add(new SelectListItem { Text = "【" + P.Course.Title + "】" + P.SubTitle, Value = P.PID.ToString() });
            if (Ps.Count() > 0)
                c.P_SL[0].Selected = true;
            else
                Error += "請先建立商品";

            var Os = DC.Organize.Where(q => q.JobTitle != "" && q.ActiveFlag && !q.DeleteFlag);
            c.O_SL.Add(new SelectListItem { Text = "會員", Value = "-1" });
            c.O_SL.Add(new SelectListItem { Text = "會員-兒童", Value = "-2" });
            c.O_SL.Add(new SelectListItem { Text = "自行匯入", Value = "0", Selected = true });
            foreach (var O in Os)
                c.O_SL.Add(new SelectListItem { Text = "會友-" + O.JobTitle, Value = O.OID.ToString() });//不統計人數的版本
            c.O_SL.Add(new SelectListItem { Text = "會友-小組員", Value = "-3" });
            c.O_SL.Add(new SelectListItem { Text = "會友-講師", Value = "-4" });
            //c.O_SL.Add(new SelectListItem { Text = O.JobTitle + "(共" + O.OrganizeInfo.Count(q => q.ActiveFlag && !q.DeleteFlag && q.ACID>1 && q.Account.ActiveFlag && !q.Account.DeleteFlag) + "人)", Value = O.OID.ToString() });
            #endregion
            #region 資料庫匯入
            var CH = DC.Coupon_Header.FirstOrDefault(q => q.CHID == ID && !q.DeleteFlag);
            if (CH != null)
            {
                c.CH = CH;

                c.P_SL.ForEach(q => q.Selected = false);
                c.P_SL.First(q => q.Value == c.CH.PID.ToString()).Selected = true;

                c.O_SL.ForEach(q => q.Selected = false);
                c.O_SL.First(q => q.Value == c.CH.OID.ToString()).Selected = true;
            }

            else if (Ps.Count() > 0)
            {
                c.CH = new Coupon_Header
                {
                    Product = Ps.OrderByDescending(q => q.CreDate).First(),
                    Price_Cut = 0,
                    SDateTime = DT,
                    EDateTime = DT,
                    OID = 0,
                    Note = "",
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID

                };

            }

            #endregion
            #region 前端輸入
            if (FC != null)
            {
                var P = DC.Product.FirstOrDefault(q => q.PID.ToString() == FC.Get("ddl_Product"));
                if (P != null)
                    c.CH.Product = P;
                int Price_Cut = 0;
                if (int.TryParse(FC.Get("txb_Price_Cut"), out Price_Cut))
                    c.CH.Price_Cut = Price_Cut < 0 ? Price_Cut * -1 : Price_Cut;
                DateTime DT_ = DateTime.Now;
                if (DateTime.TryParse(FC.Get("txb_SDate"), out DT_))
                    c.CH.SDateTime = DT_;
                DT_ = DateTime.Now;
                if (DateTime.TryParse(FC.Get("txb_EDate"), out DT_))
                    c.CH.EDateTime = DT_;
                //if (c.CH.CHID == 0 || c.CH.OID<0)//只有新增可以設定
                c.CH.OID = Convert.ToInt32(FC.Get("ddl_Organize"));
                c.CH.Note = FC.Get("txb_Note");
                c.CH.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                c.CH.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                c.CH.UpdDate = DT;
                c.CH.SaveACID = ACID;
            }
            #endregion
            return c;
        }
        [HttpGet]
        public ActionResult Coupon_Edit(int ID)
        {
            GetViewBag();
            return View(GetCoupon_Edit(ID, null));
        }
        [HttpPost]
        public ActionResult Coupon_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            var c = GetCoupon_Edit(ID, FC);
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (c.CH.CHID == 0)
                {
                    c.CH.CreDate = c.CH.UpdDate;
                    DC.Coupon_Header.InsertOnSubmit(c.CH);
                }
                DC.SubmitChanges();

                List<Coupon_Account> CAs_New = new List<Coupon_Account>();
                var CAs = DC.Coupon_Account.Where(q => q.CHID == c.CH.CHID).ToList();


                if (c.CH.OID > 0)//會友-職分/事工團
                {

                    var OIs = DC.OrganizeInfo.Where(q => q.OID == c.CH.OID && q.ACID > 1 && q.ActiveFlag && !q.DeleteFlag).ToList();
                    foreach (var OI in OIs)
                    {
                        if (CAs.Count() == 0)//新建資料=一開始就沒有配發
                        {
                            CAs_New.Add(new Coupon_Account
                            {
                                Coupon_Header = c.CH,
                                Account = OI.Account,
                                OHID = 0,
                                OPID = 0,
                                UsedDate = DT,
                                Note = "",
                                ActiveFlag = true,
                                DeleteFlag = false,
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = ACID
                            });
                        }
                        else if (!CAs.Any(q => q.ACID == OI.ACID))//非新增資料,但是資料庫沒有=補配發
                        {
                            CAs_New.Add(new Coupon_Account
                            {
                                Coupon_Header = c.CH,
                                Account = OI.Account,
                                OHID = 0,
                                OPID = 0,
                                UsedDate = DT,
                                Note = "",
                                ActiveFlag = true,
                                DeleteFlag = false,
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = ACID
                            });
                        }
                    }

                    var ACIDs = CAs.Select(q => q.ACID).Except(OIs.Select(q => q.ACID));
                    if (ACIDs.Count() > 0)
                    {
                        var CAs_ = from q in CAs
                                   join p in ACIDs
                                   on q.ACID equals p
                                   select q;
                        foreach (var CA_ in CAs_)
                        {
                            CA_.DeleteFlag = true;
                            CA_.ActiveFlag = false;
                            CA_.Note = "已無職分而移除";
                            CA_.UpdDate = DT;
                            CA_.SaveACID = ACID;
                        }
                    }
                }
                else if (c.CH.OID < 0)
                {
                    var As = DC.Account.Where(q => !q.DeleteFlag && q.ActiveFlag);
                    switch (c.CH.OID)
                    {

                        case -1://全部會員
                            {

                            }
                            break;

                        case -2://會員-兒童
                            {
                                As = As.Where(q => DT.Year - q.Birthday.Year <= iChildAge && q.Birthday != q.CreDate);
                            }
                            break;
                        case -3://會友-小組員
                            {
                                As = from q in As
                                     join p in GetMOIAC(8, 0, 0)
                                     on q.ACID equals p.ACID
                                     select q;
                            }
                            break;
                        case -4://會友-講師
                            {
                                As = from q in As
                                     join p in DC.Teacher.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID > 1)
                                     on q.ACID equals p.ACID
                                     select q;
                            }
                            break;

                    }
                    foreach (var A in As)
                    {
                        if (CAs.Count() == 0)//新建資料=一開始就沒有配發
                        {
                            CAs_New.Add(new Coupon_Account
                            {
                                Coupon_Header = c.CH,
                                Account = A,
                                OHID = 0,
                                OPID = 0,
                                UsedDate = DT,
                                Note = "",
                                ActiveFlag = true,
                                DeleteFlag = false,
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = ACID
                            });
                        }
                        else if (!CAs.Any(q => q.ACID == A.ACID))//非新增資料,但是資料庫沒有=補配發
                        {
                            CAs_New.Add(new Coupon_Account
                            {
                                Coupon_Header = c.CH,
                                Account = A,
                                OHID = 0,
                                OPID = 0,
                                UsedDate = DT,
                                Note = "",
                                ActiveFlag = true,
                                DeleteFlag = false,
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = ACID
                            });
                        }
                    }
                }


                if (CAs_New.Count() > 0)
                    DC.Coupon_Account.InsertAllOnSubmit(CAs_New);
                DC.SubmitChanges();

                SetAlert("存檔完成", 1, "/Admin/StoreSet/Coupon_List");
            }
            return View(c);
        }
        #endregion
        #region 折價劵-使用者-列表
        public class cCoupon_Account_List
        {
            public cTableList cTL = new cTableList();
            public string sKey = "";
            public int OID = 0;
        }
        public cCoupon_Account_List GetCoupon_Account_List(int ID, FormCollection FC)
        {
            cCoupon_Account_List c = new cCoupon_Account_List();

            #region 物件初始化

            #region 前端資料帶入
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.sKey = FC != null ? FC.Get("txb_Key") : "";

            #endregion


            c.cTL = new cTableList();
            c.cTL.Title = "";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            #endregion
            #region 資料庫資料帶入
            var Ns = DC.Coupon_Account.Where(q => !q.DeleteFlag && q.CHID == ID);
            if (c.sKey != "")
                Ns = Ns.Where(q => (q.Account.Name_First + q.Account.Name_Last) == c.sKey);

            var CH = DC.Coupon_Header.FirstOrDefault(q => q.CHID == ID);
            if (CH == null)
                Error += "參數錯誤...無法顯示";
            else
            {
                c.OID = CH.OID;
                if (CH.EDateTime <= DT)
                {
                    foreach (var N in Ns.Where(q => q.ActiveFlag && q.OHID == 0))
                    {
                        N.Note = "到期未使用,自動取消";
                        N.ActiveFlag = false;
                        N.UpdDate = DT;
                        N.SaveACID = 1;
                        DC.SubmitChanges();
                    }
                }
            }



            //旌旗權限檢視門檻設置
            var OI2s = DC.M_OI2_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID);
            if (OI2s.Any())
            {
                var OI2_1 = OI2s.FirstOrDefault(q => q.OIID == 1);
                if (OI2_1 == null)
                {
                    Ns = from q in Ns
                         join p in OI2s.Where(q => q.OIID > 2)
                         on q.Coupon_Header.Product.OIID equals p.OIID
                         select q;
                }
            }
            else if (ACID == 1) { }
            else//沒有旌旗權限~暫時就乾脆都先不給看好了
            {
                Ns = Ns.Where(q => q.Coupon_Header.Product.OIID == 1);
            }

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "ID", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "姓名" });
            TopTitles.Add(new cTableCell { Title = "使用狀態" });


            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.CAID).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Value = N_.CAID.ToString() });//ID
                cTR.Cs.Add(new cTableCell { Value = N_.Account.Name_First + N_.Account.Name_Last });//姓名

                if (N_.OHID > 0)
                    cTR.Cs.Add(new cTableCell { Value = "已用於訂單" + N_.OHID + "-" + N_.OPID });//使用狀態
                else if (!N_.ActiveFlag)
                    cTR.Cs.Add(new cTableCell { Value = N_.Note });
                else
                    cTR.Cs.Add(new cTableCell { Value = "可正常使用" });
                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            if (Error != "")
                SetAlert(Error, 2, "/Admin/StoreSet/Coupon_List");
            #endregion
            return c;
        }
        [HttpGet]
        public ActionResult Coupon_Account_List(int ID)
        {
            GetViewBag();
            return View(GetCoupon_Account_List(ID, null));
        }
        [HttpPost]
        public ActionResult Coupon_Account_List(int ID, FormCollection FC, HttpPostedFileBase fu)
        {
            GetViewBag();
            ACID = GetACID();
            if (fu != null)
            {
                var fileValid = true;
                // Limit File Szie Below : 5MB
                if (fu.ContentLength <= 0 || fu.ContentLength > 5242880)
                    fileValid = false;
                else if (fu.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                    fileValid = false;

                if (fileValid == true)
                {
                    string extension = Path.GetExtension(fu.FileName);
                    string fileName = $"{Guid.NewGuid()}{extension}";
                    string savePath = Path.Combine(Server.MapPath("~/Files/Coupon/"), fileName);
                    fu.SaveAs(savePath);
                    //ViewBag.Message = "檔案上傳成功。";

                    ArrayList IDList = ReadExcel("~/Files/Coupon/" + fileName);
                    List<Coupon_Account> CAs_New = new List<Coupon_Account>();
                    var CH = DC.Coupon_Header.FirstOrDefault(q => q.CHID == ID);
                    foreach (string[] str in IDList)
                    {
                        int iACID = 0;
                        if (int.TryParse(str[0], out iACID))
                        {
                            var AC = DC.Account.FirstOrDefault(q => q.ACID == iACID && !q.DeleteFlag);
                            var CA = DC.Coupon_Account.FirstOrDefault(q => q.ACID == iACID && q.CHID == CH.CHID);
                            if (CH != null && AC != null && CA == null)
                            {
                                CAs_New.Add(new Coupon_Account
                                {
                                    Coupon_Header = CH,
                                    Account = AC,
                                    OHID = 0,
                                    OPID = 0,
                                    UsedDate = DT,
                                    Note = "",
                                    ActiveFlag = true,
                                    DeleteFlag = false,
                                    CreDate = DT,
                                    UpdDate = DT,
                                    SaveACID = ACID
                                });
                            }
                        }
                    }
                    if (CAs_New.Count > 0)
                    {
                        DC.Coupon_Account.InsertAllOnSubmit(CAs_New);
                        DC.SubmitChanges();

                        SetAlert("上傳名單完成", 1);
                    }
                }

            }

            return View(GetCoupon_Account_List(ID, FC));
        }

        #endregion

        #region 
        [HttpGet]
        public ActionResult ProductClass_BatchAdd(int ID)
        {
            GetViewBag();
            return View();
        }
        #endregion
    }
}