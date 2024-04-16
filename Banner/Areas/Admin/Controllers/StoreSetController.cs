using Banner.Models;
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
using System.Web.UI.WebControls;
using ZXing.QrCode.Internal;

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
            public List<SelectListItem> OISL = new List<SelectListItem>();
            public cTableList cTL = new cTableList();
        }
        public cProduct_List GetProduct_List(FormCollection FC)
        {
            cProduct_List c = new cProduct_List();
            ACID = GetACID();
            #region 物件初始化
            c.SL = new List<SelectListItem>();
            c.SL.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            var CCs = DC.Course_Category.Where(q => !q.DeleteFlag).OrderBy(q => q.Code);
            foreach (var CC in CCs)
                c.SL.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString() });

            c.OISL = new List<SelectListItem>();
            c.OISL.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            if (ACID == 1)
            {
                c.OISL.AddRange(from q in DC.OrganizeInfo.Where(q => q.OID == 2 && q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID).OrderBy(q => q.OID).ThenBy(q => q.OIID)
                                select new SelectListItem { Text = q.Title + q.Organize.Title, Value = q.OIID.ToString() });
            }
            else
            {
                c.OISL.AddRange(from q in DC.M_OI2_Account.Where(q => q.OrganizeInfo.OID == 2 && q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID).OrderBy(q => q.OrganizeInfo.OID).ThenBy(q => q.OIID)
                                select new SelectListItem { Text = q.OrganizeInfo.Title + q.OrganizeInfo.Organize.Title, Value = q.OIID.ToString() });
            }

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

            int OIID = Convert.ToInt32(FC != null ? FC.Get("ddl_OI2") : "0");
            c.OISL.ForEach(q => q.Selected = false);
            c.OISL.First(q => q.Value == OIID.ToString()).Selected = true;
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
            if (OIID > 0)
                Ns = Ns.Where(q => q.OIID == OIID);

            if (!string.IsNullOrEmpty(c.SDate))
            {
                DateTime SDT_ = DateTime.Now;
                if (DateTime.TryParse(c.SDate, out SDT_))
                {
                    Ns = from q in Ns
                         join p in DC.Product_ClassTime.Where(q => q.ClassDate.Date >= SDT_.Date && !q.Product_Class.DeleteFlag).GroupBy(q => q.Product_Class.PID).Select(q => q.Key)
                         on q.PID equals p
                         select q;
                }
            }

            if (!string.IsNullOrEmpty(c.EDate))
            {
                DateTime EDT_ = DateTime.Now;
                if (DateTime.TryParse(c.EDate, out EDT_))
                {
                    Ns = from q in Ns
                         join p in DC.Product_ClassTime.Where(q => q.ClassDate.Date <= EDT_.Date && !q.Product_Class.DeleteFlag).GroupBy(q => q.Product_Class.PID).Select(q => q.Key)
                         on q.PID equals p
                         select q;
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
            //TopTitles.Add(new cTableCell { Title = "課程開班" });
            TopTitles.Add(new cTableCell { Title = "班級管理" });
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
                cTR.Cs.Add(new cTableCell { Value = (N_.SDate_Signup.ToString(DateFormat) + "<br/>" + N_.EDate_Signup.ToString(DateFormat)) });//線上報名日期
                var PCTs = from q in DC.Product_Class.Where(q => q.PID == N_.PID && !q.DeleteFlag)
                           join p in DC.Product_ClassTime
                           on q.PCID equals p.PCID
                           select p;
                if (PCTs.Count() > 0)//開課日期
                {
                    DateTime SDate = PCTs.Min(q => q.ClassDate);
                    DateTime EDate = PCTs.Max(q => q.ClassDate);
                    if (SDate.Date != EDate.Date)
                        cTR.Cs.Add(new cTableCell { Value = SDate.ToString(DateFormat) + "<br/>" + EDate.ToString(DateFormat) });
                    else
                    {
                        TimeSpan STime = PCTs.Where(q => q.ClassDate.Date == SDate).Min(q => q.STime);
                        TimeSpan ETime = PCTs.Where(q => q.ClassDate.Date == SDate).Max(q => q.ETime);
                        //cTR.Cs.Add(new cTableCell { Value = SDate.ToString(DateFormat) + "<br/>" + STime.ToString(@"hh\:mm") + "~" + ETime.ToString(@"hh\:mm") });
                        cTR.Cs.Add(new cTableCell { Value = SDate.ToString(DateFormat) });
                    }
                }
                else
                    cTR.Cs.Add(new cTableCell { Value = "尚未設定" });

                cTR.Cs.Add(new cTableCell { Value = N_.Price_Basic.ToString() });//原價
                                                                                 //cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/ProductClass_List?PID=" + N_.PID, Target = "_self", Value = "班級管理" });//班別
                                                                                 //cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/ProductClass_BatchAdd/" + N_.PID, Target = "_self", Value = "課程開班" });//班別

                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/ProductClass_BatchEdit/" + N_.PID, Target = "_self", Value = (PCTs.Count() > 0 ? "班級管理" : "開班") });//班別
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
            public List<Product_Rule> PRs = new List<Product_Rule>();
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
            var CCs = DC.Course_Category.Where(q => !q.DeleteFlag && q.ActiveFlag && q.Course.Count(p => p.ActiveFlag && !p.DeleteFlag) > 0).OrderBy(q => q.Code).ToList();
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
                    var CRs = DC.Course_Rule.Where(q => q.CID == CID).OrderBy(q => q.CID);
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
                            foreach (var C in Cs) PB.CSL_Before.Add(new SelectListItem { Text = C.Title, Value = C.CID.ToString(), Selected = CR.CID == CID });

                            N.PBs.Add(PB);
                        }
                        else if (CR.TargetType == 1)//職分
                        {
                            N.OSL.SortNo = CR.CRID;
                            if (N.OSL.ddlList.Any(q => q.Value == CR.TargetInt1.ToString()))
                                N.OSL.ddlList.First(q => q.Value == CR.TargetInt1.ToString()).Selected = true;
                        }

                        N.PRs.Add(new Product_Rule
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

                N.PRs = N.P.Product_Rule.ToList();
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
                        foreach (var C in Cs) PB.CSL_Before.Add(new SelectListItem { Text = C.Title, Value = C.CID.ToString(), Selected = C.CID == C_.CID });

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
                //N.P.ShowFlag = GetViewCheckBox(FC.Get("cbox_ShowFlag"));
                //N.P.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                if (FC.Get("cbox_ShowFlag") == "3")
                {
                    N.P.ShowFlag = false;
                    N.P.ActiveFlag = false;
                }
                else
                {
                    N.P.ActiveFlag = true;
                    N.P.ShowFlag = FC.Get("cbox_ShowFlag") == "1";
                }
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
                //string zzz = FC.Get("ddl_CBasic");
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
                            N.PRs.Add(new Product_Rule
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
                        N.PRs.Add(new Product_Rule
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
                    N.PRs.Add(new Product_Rule
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
                            N.PRs.Add(new Product_Rule
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
                #region 永久課程的擋修覆蓋
                var CRs = DC.Course_Rule.Where(q => q.CID == N.P.CID).OrderBy(q => q.TargetType);
                foreach (var CR in CRs)
                {
                    var PR = N.PRs.FirstOrDefault(q => q.TargetType == CR.TargetType);
                    if (PR != null)
                    {
                        PR.TargetInt1 = CR.TargetInt1;
                        PR.TargetInt2 = CR.TargetInt2;
                    }
                    else
                    {
                        N.PRs.Add(new Product_Rule
                        {
                            Product = N.P,
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
            if (N.PTSL.ddlList.Count(q => q.Selected) == 0)
                Error += "請至少選擇一種交易方式<br/>";
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
                    DC.Product_Rule.InsertOnSubmit(PR);
                    DC.SubmitChanges();
                }

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

                if (ID == 0)
                    SetAlert("新增完成,請進行開班", 1, "/Admin/StoreSet/ProductClass_BatchAdd/" + N.P.PID);
                else
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
                Ns = from q in Ns
                     join p in DC.Product.Where(q => q.Title.Contains(c.sKey))
                     on q.PID equals p.PID
                     select q;
            if (CCID > 0)
                Ns = from q in Ns
                     join p in DC.Course.Where(q => q.CCID == CCID)
                     on q.CID equals p.CID
                     select q;
            //Ns = Ns.Where(q => q.Product.Course.CCID == CCID);
            if (c.ActiveType >= 0)
                Ns = Ns.Where(q => q.ActiveFlag == (c.ActiveType == 1));

            //旌旗權限檢視門檻設置
            var OI2s = DC.M_OI2_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID);
            if (OI2s.Any())
            {
                var OI2_1 = OI2s.FirstOrDefault(q => q.OIID == 1);//是否有檢視全部旌旗的權限?
                if (OI2_1 == null)
                {
                    var Ps_ = from q in OI2s.Where(q => q.OIID > 2)
                              join p in DC.Product.Where(q => !q.DeleteFlag)
                              on q.OIID equals p.OIID
                              select p;
                    Ns = from q in Ns.Where(q => q.PID > 0)
                         join p in Ps_.GroupBy(q => q.PID)
                         on q.PID equals p.Key
                         select q;
                    /*Ns = from q in Ns
                         join p in OI2s.Where(q => q.OIID > 2)
                         on q.Product.OIID equals p.OIID
                         select q;*/
                }
            }
            else if (ACID == 1) { }
            else//沒有旌旗權限~暫時就乾脆都先不給看好了
            {
                Ns = Ns.Where(q => q.CHID == 0);
            }

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "課程分類" });
            TopTitles.Add(new cTableCell { Title = "課程名稱" });
            TopTitles.Add(new cTableCell { Title = "可用期間" });
            TopTitles.Add(new cTableCell { Title = "分配名單" });
            TopTitles.Add(new cTableCell { Title = "啟用狀態" });

            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.PID).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                var Cou = DC.Course.FirstOrDefault(q => q.CID == N_.CID);
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/Coupon_Edit/" + N_.CHID, Target = "_self", Value = "編輯" });//編輯

                if (Cou != null)
                    cTR.Cs.Add(new cTableCell { Value = "【" + Cou.Course_Category.Code + "】" + Cou.Course_Category.Title });//課程分類
                else
                    cTR.Cs.Add(new cTableCell { Value = "" });//課程分類
                                                              //cTR.Cs.Add(new cTableCell { Value = "【" + N_.Product.Course.Course_Category.Code + "】" + N_.Product.Course.Course_Category.Title });//課程分類

                var P = DC.Product.FirstOrDefault(q => q.PID == N_.PID);
                if (P != null)
                    cTR.Cs.Add(new cTableCell { Value = P.Course.Title + " " + P.SubTitle });//課程名稱
                else
                    cTR.Cs.Add(new cTableCell { Value = "" });
                cTR.Cs.Add(new cTableCell { Value = (N_.SDateTime.ToString(DateTimeFormat) + "<br/>↕<br/>" + N_.EDateTime.ToString(DateTimeFormat)) });//可用期間
                var CR5 = N_.Coupon_Rule.FirstOrDefault(q => q.Target_Type == 5);
                if(CR5!=null)
                    cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/Coupon_Account_List/" + CR5.CRID, Target = "_self", Value = "檢視名單(" + CR5.Coupon_Account.Count(q => !q.DeleteFlag) + ")" });//分配名單
                else
                    cTR.Cs.Add(new cTableCell { Value ="--" });//指定名單
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
            public List<SelectListItem> CC_SL = new List<SelectListItem>();
            public List<SelectListItem> C_SL = new List<SelectListItem>();
            public List<SelectListItem> P_SL = new List<SelectListItem>();
            public List<SelectListItem> O_SL = new List<SelectListItem>();

            public List<cCouponRoolCell> cCRCs = new List<cCouponRoolCell>();//折價劵規則

            public int CCID = 0;
            public int CID = 0;
            public int PID = 0;
        }
        public cCoupon_Edit GetCoupon_Edit(int ID, FormCollection FC)
        {
            cCoupon_Edit c = new cCoupon_Edit();
            Error = "";
            #region 提前取前端資料
            c.CID = GetQueryStringInInt("CID");
            c.PID = GetQueryStringInInt("PID");
            if (FC != null)
            {
                int i = 0;
                if (int.TryParse(FC.Get("ddl_CC"), out i))
                    c.CCID = i;
                else
                    c.CCID = 0;

                if (int.TryParse(FC.Get("ddl_C"), out i))
                    c.CID = i;
                else
                    c.CID = 0;

                if (int.TryParse(FC.Get("ddl_P"), out i))
                    c.PID = i;
                else
                    c.PID = 0;

                if (c.CID == 0)
                    Error += "請選擇永久課程<br/>";
            }
            #endregion
            #region 物件初始化
            ACID = GetACID();
            var MOI2 = DC.M_OI2_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID && (q.OIID == 1 || q.OIID > 2));

            c.CH = DC.Coupon_Header.FirstOrDefault(q => q.CHID == ID && !q.DeleteFlag);
            if (c.CH == null)
            {
                c.CH = new Coupon_Header
                {
                    CID = 0,
                    PID = 0,
                    Title = "",
                    Code = "",
                    SDateTime = DT,
                    EDateTime = DT,
                    Life_Cut = 0,
                    Note = "",
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID
                };
            }
            else
            {
                c.CH.UpdDate = DT;
                c.CH.SaveACID = ACID;
            }


            #endregion
            #region 課程與商品
            var CCs = DC.Course_Category.Where(q => !q.DeleteFlag && q.ActiveFlag).OrderBy(q => q.Code);
            foreach (var CC in CCs)
                c.CC_SL.Add(new SelectListItem { Text = CC.Code + " " + CC.Title, Value = CC.CCID.ToString(), Selected = CC.CCID == c.CCID });
            if (!c.CC_SL.Any(q => q.Selected) && CCs.Count() > 0)
            {
                c.CCID = CCs.First().CCID;
                c.CC_SL[0].Selected = true;
            }

            var Cs = DC.Course.Where(q => !q.DeleteFlag && q.ActiveFlag && q.CCID == c.CCID).OrderBy(q => q.Title);
            foreach (var C in Cs)
                c.C_SL.Add(new SelectListItem { Text = C.Title, Value = C.CID.ToString(), Selected = C.CID == c.CID });
            if (!c.C_SL.Any(q => q.Selected) && Cs.Count() > 0)
            {
                c.CID = Cs.First().CID;
                c.C_SL[0].Selected = true;
            }

            c.P_SL.Add(new SelectListItem { Text = "不選擇商品", Value = "0", Selected = c.PID == 0 });
            var Ps = DC.Product.Where(q => !q.DeleteFlag && q.ActiveFlag && q.CID == c.CID);
            if (MOI2.Any(q => q.OIID == 1)) { }//全部旌旗都可以
            else
            {
                Ps = from q in Ps
                     join p in MOI2
                     on q.OIID equals p.OIID
                     select q;
            }
            foreach (var P in Ps.OrderBy(q => q.SubTitle))
                c.P_SL.Add(new SelectListItem { Text = P.SubTitle, Value = P.PID.ToString(), Selected = P.PID == c.PID });
            if (!c.P_SL.Any(q => q.Selected))
            {
                c.C_SL[0].Selected = true;
            }

            if (FC != null)
            {
                c.CH.CID = Convert.ToInt32(FC.Get("ddl_C"));
                c.CH.PID = Convert.ToInt32(FC.Get("ddl_P"));

                c.CH.Title = FC.Get("txb_Title");
                if (string.IsNullOrEmpty(c.CH.Title))
                    Error += "請輸入優惠劵名稱<br/>";
                c.CH.Note = FC.Get("txb_Note");
                c.CH.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                c.CH.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));

                if (FC.Get("rbut_TimeType") == "0")
                {
                    DateTime DT_S = DT, DT_E = DT;
                    if (DateTime.TryParse(FC.Get("txb_SDate"), out DT_S))
                        c.CH.SDateTime = DT_S;
                    else
                        Error += "請輸入優惠劵有效起始日<br/>";

                    if (DateTime.TryParse(FC.Get("txb_EDate"), out DT_E))
                        c.CH.EDateTime = DT_E;
                    else
                        Error += "請輸入優惠劵有效結束日<br/>";

                    if (c.CH.SDateTime.Date > c.CH.EDateTime.Date)
                    {
                        DateTime DT__ = c.CH.SDateTime;
                        c.CH.SDateTime = c.CH.EDateTime;
                        c.CH.EDateTime = DT__;
                    }
                    c.CH.Life_Cut = 0;
                }
                else
                {
                    c.CH.SDateTime = c.CH.EDateTime = c.CH.CreDate;
                    int Cut = 0;
                    if (int.TryParse(FC.Get("txb_Life_Cut"), out Cut))
                    {
                        if (Cut <= 0)
                            Error += "請輸入適用天數<br/>";
                        else
                            c.CH.Life_Cut = Cut;
                    }
                    else
                        Error += "請輸入適用天數<br/>";
                }
            }
            #endregion
            #region 限制旌旗
            var OIs = DC.OrganizeInfo.Where(q => q.OID == 2);
            if (ACID == 1) { }
            else
            {
                var MOI2s = DC.M_OI2_Account.Where(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag);
                if (!MOI2s.Any(q => q.OIID == 1))
                    OIs = from q in OIs
                          join p in MOI2s
                          on q.OIID equals p.OIID
                          select q;
            }
            c.O_SL = new List<SelectListItem>();
            foreach (var OI in OIs.OrderBy(q => q.OID))
                c.O_SL.Add(new SelectListItem { Text = OI.Title + OI.Organize.Title, Value = OI.OIID.ToString(), Selected = true });

            if (FC != null)
            {
                foreach (SelectListItem SL in c.O_SL)
                {
                    SL.Selected = GetViewCheckBox(FC.Get("cbox_OI_" + SL.Value));
                }
            }

            #endregion
            #region 限制對象

            var CRs = DC.Coupon_Rule.Where(q => q.CHID == ID).ToList();
            c.cCRCs = new List<cCouponRoolCell>();
            int iPrice_Type = 0;
            int iPrice_Cut = 0;
            #region 牧養職分
            var Os = GetO();
            int iSort = 0;
            foreach (var O in Os.Where(q => q.JobTitle != "").OrderBy(q => q.SortNo))
            {
                cCouponRoolCell CRC = new cCouponRoolCell();
                CRC.Category = O.Title;
                CRC.Title = O.JobTitle;
                CRC.Target_ID = O.OID;
                CRC.SortNo = iSort;
                CRC.Target_Type = 1;
                CRC.Category_ID = 0;
                var CR = CRs.FirstOrDefault(q => q.Target_Type == 1 && q.Target_ID == O.OID);
                if (CR != null)
                {
                    CRC.CRID = CR.CRID;
                    CRC.Price_Type = CR.Price_Type;
                    CRC.Price_Cut = CR.Price_Cut;
                }
                else
                {
                    CRC.CRID = 0;
                    CRC.Price_Type = 0;
                    CRC.Price_Cut = 0;
                }

                iPrice_Type = 0;
                iPrice_Cut = 0;
                if (FC != null)
                {
                    string sPrice_Cut = FC.Get("txb_Price_Cut_" + CRC.CRID + "_" + CRC.Target_Type + "_" + CRC.Target_ID);
                    if (!string.IsNullOrEmpty(sPrice_Cut))
                    {
                        if (int.TryParse(sPrice_Cut, out iPrice_Cut))
                            CRC.Price_Cut = iPrice_Cut;
                    }

                    string sPrice_Type = FC.Get("rbl_Price_Type_" + CRC.CRID + "_" + CRC.Target_Type + "_" + CRC.Target_ID);
                    if (!string.IsNullOrEmpty(sPrice_Type))
                    {
                        if (int.TryParse(sPrice_Type, out iPrice_Type))
                            CRC.Price_Type = iPrice_Type;
                    }
                }
                c.cCRCs.Add(CRC);
            }
            #endregion

            #region 事工團
            var Ss = DC.Staff.Where(q => q.ActiveFlag && !q.DeleteFlag);
            iSort = 0;
            foreach (var S in Ss.OrderBy(q => q.SCID).ThenBy(q => q.Title))
            {
                cCouponRoolCell CRC = new cCouponRoolCell();
                CRC.Category = S.Staff_Category.Title;
                CRC.Title = S.Title;
                CRC.Target_ID = S.SID;
                CRC.SortNo = iSort;
                CRC.Target_Type = 2;
                CRC.Category_ID = S.SCID;
                var CR = CRs.FirstOrDefault(q => q.Target_Type == 2 && q.Target_ID == S.SID);
                if (CR != null)
                {
                    CRC.CRID = CR.CRID;
                    CRC.Price_Type = CR.Price_Type;
                    CRC.Price_Cut = CR.Price_Cut;
                }
                else
                {
                    CRC.CRID = 0;
                    CRC.Price_Type = 0;
                    CRC.Price_Cut = 0;
                }

                iPrice_Type = 0;
                iPrice_Cut = 0;
                if (FC != null)
                {
                    string sPrice_Cut = FC.Get("txb_Price_Cut_" + CRC.CRID + "_" + CRC.Target_Type + "_" + CRC.Target_ID);
                    if (!string.IsNullOrEmpty(sPrice_Cut))
                    {
                        if (int.TryParse(sPrice_Cut, out iPrice_Cut))
                            CRC.Price_Cut = iPrice_Cut;
                    }

                    string sPrice_Type = FC.Get("rbl_Price_Type_" + CRC.CRID + "_" + CRC.Target_Type + "_" + CRC.Target_ID);
                    if (!string.IsNullOrEmpty(sPrice_Type))
                    {
                        if (int.TryParse(sPrice_Type, out iPrice_Type))
                            CRC.Price_Type = iPrice_Type;
                    }
                }
                c.cCRCs.Add(CRC);
            }
            #endregion

            #region 新生
            var CR_ = CRs.FirstOrDefault(q => q.Target_Type == 3);
            cCouponRoolCell CRC_ = new cCouponRoolCell();
            CRC_.Category = "";
            CRC_.Title = "新生";
            CRC_.Target_ID = 0;
            CRC_.SortNo = 3;
            CRC_.Target_Type = 3;
            CRC_.Category_ID = 0;
            if (CR_ != null)
            {
                CRC_.CRID = CR_.CRID;
                CRC_.Price_Type = CR_.Price_Type;
                CRC_.Price_Cut = CR_.Price_Cut;
            }
            else
            {
                CRC_.CRID = 0;
                CRC_.Price_Type = 0;
                CRC_.Price_Cut = 0;
            }
            iPrice_Type = 0;
            iPrice_Cut = 0;
            if (FC != null)
            {
                string sPrice_Cut = FC.Get("txb_Price_Cut_" + CRC_.CRID + "_" + CRC_.Target_Type + "_" + CRC_.Target_ID);
                if (!string.IsNullOrEmpty(sPrice_Cut))
                {
                    if (int.TryParse(sPrice_Cut, out iPrice_Cut))
                        CRC_.Price_Cut = iPrice_Cut;
                }

                string sPrice_Type = FC.Get("rbl_Price_Type_" + CRC_.CRID + "_" + CRC_.Target_Type + "_" + CRC_.Target_ID);
                if (!string.IsNullOrEmpty(sPrice_Type))
                {
                    if (int.TryParse(sPrice_Type, out iPrice_Type))
                        CRC_.Price_Type = iPrice_Type;
                }
            }
            c.cCRCs.Add(CRC_);
            #endregion

            #region 領夜同工
            CR_ = CRs.FirstOrDefault(q => q.Target_Type == 4);
            CRC_ = new cCouponRoolCell();
            CRC_.Category = "";
            CRC_.Title = "領夜同工";
            CRC_.Target_ID = 0;
            CRC_.SortNo = 4;
            CRC_.Target_Type = 4;
            CRC_.Category_ID = 0;
            if (CR_ != null)
            {
                CRC_.CRID = CR_.CRID;
                CRC_.Price_Type = CR_.Price_Type;
                CRC_.Price_Cut = CR_.Price_Cut;
            }
            else
            {
                CRC_.CRID = 0;
                CRC_.Price_Type = 0;
                CRC_.Price_Cut = 0;
            }
            iPrice_Type = 0;
            iPrice_Cut = 0;
            if (FC != null)
            {
                string sPrice_Cut = FC.Get("txb_Price_Cut_" + CRC_.CRID + "_" + CRC_.Target_Type + "_" + CRC_.Target_ID);
                if (!string.IsNullOrEmpty(sPrice_Cut))
                {
                    if (int.TryParse(sPrice_Cut, out iPrice_Cut))
                        CRC_.Price_Cut = iPrice_Cut;
                }

                string sPrice_Type = FC.Get("rbl_Price_Type_" + CRC_.CRID + "_" + CRC_.Target_Type + "_" + CRC_.Target_ID);
                if (!string.IsNullOrEmpty(sPrice_Type))
                {
                    if (int.TryParse(sPrice_Type, out iPrice_Type))
                        CRC_.Price_Type = iPrice_Type;
                }
            }
            c.cCRCs.Add(CRC_);
            #endregion

            #region 匯入
            CR_ = CRs.FirstOrDefault(q => q.Target_Type == 5);
            CRC_ = new cCouponRoolCell();
            CRC_.Category = "";
            CRC_.Title = "匯入名單";
            CRC_.Target_ID = 0;
            CRC_.SortNo = 5;
            CRC_.Target_Type = 5;
            CRC_.Category_ID = 0;
            if (CR_ != null)
            {
                CRC_.CRID = CR_.CRID;
                CRC_.Price_Type = CR_.Price_Type;
                CRC_.Price_Cut = CR_.Price_Cut;
            }
            else
            {
                CRC_.CRID = 0;
                CRC_.Price_Type = 0;
                CRC_.Price_Cut = 0;
            }
            iPrice_Type = 0;
            iPrice_Cut = 0;
            if (FC != null)
            {
                string sPrice_Cut = FC.Get("txb_Price_Cut_" + CRC_.CRID + "_" + CRC_.Target_Type + "_" + CRC_.Target_ID);
                if (!string.IsNullOrEmpty(sPrice_Cut))
                {
                    if (int.TryParse(sPrice_Cut, out iPrice_Cut))
                        CRC_.Price_Cut = iPrice_Cut;
                }

                string sPrice_Type = FC.Get("rbl_Price_Type_" + CRC_.CRID + "_" + CRC_.Target_Type + "_" + CRC_.Target_ID);
                if (!string.IsNullOrEmpty(sPrice_Type))
                {
                    if (int.TryParse(sPrice_Type, out iPrice_Type))
                        CRC_.Price_Type = iPrice_Type;
                }
            }
            c.cCRCs.Add(CRC_);
            #endregion

            #region 檢查
            if (!c.O_SL.Any(q => q.Selected))
                Error += "請選擇限制旌旗<br/>";
            if (c.cCRCs.Any(q => (q.Price_Type == 0 || q.Price_Type == 1) && q.Price_Cut < 0))
                Error += "折價或指定金額請輸入正整數或0<br/>";
            
            var OPs = from q in DC.Order_Product.Where(q=>!q.Order_Header.DeleteFlag && q.Order_Header.Order_Type==2 && q.CRID>0)
                      join p in DC.Coupon_Rule.Where(q=>q.CHID==c.CH.CHID)
                      on q.CRID equals p.CRID
                      select q;
            if(OPs.Count()>0)
                Error += "此折價卷已被使用,不能變更<br/>";
            #endregion

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
        public ActionResult Coupon_Edit(int ID, FormCollection FC, HttpPostedFileBase file_upload)
        {
            GetViewBag();
            var c = GetCoupon_Edit(ID, FC);


            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                #region 優惠劵頭
                if (c.CH.CHID == 0)
                {
                    c.CH.CreDate = c.CH.UpdDate;
                    DC.Coupon_Header.InsertOnSubmit(c.CH);
                }
                DC.SubmitChanges();
                #endregion
                #region 限定旌旗
                var Ms = DC.M_OI_Coupon.Where(q => q.CHID == c.CH.CHID);
                if (Ms.Count() > 0)
                {
                    DC.M_OI_Coupon.DeleteAllOnSubmit(Ms);
                    DC.SubmitChanges();
                }
                foreach (var SL in c.O_SL.Where(q => q.Selected))
                {
                    M_OI_Coupon M = new M_OI_Coupon
                    {
                        Coupon_Header = c.CH,
                        OIID = Convert.ToInt32(SL.Value)
                    };
                    DC.M_OI_Coupon.InsertOnSubmit(M);
                    DC.SubmitChanges();
                }
                #endregion
                #region 職分等其他限制
                /*var CRs = DC.Coupon_Rule.Where(q => q.CHID == c.CH.CHID);
                if (CRs.Count() > 0)
                {
                    var CAs = from q in CRs
                              join p in DC.Coupon_Account
                              on q.CRID equals p.CRID
                              select p;
                    if(CAs.Count()>0)
                    {
                        DC.Coupon_Account.DeleteAllOnSubmit(CAs);
                        DC.SubmitChanges();
                    }

                    DC.Coupon_Rule.DeleteAllOnSubmit(CRs);
                    DC.SubmitChanges();
                }*/
                foreach (var CRC in c.cCRCs.OrderBy(q => q.Price_Type).ThenBy(q => q.SortNo))
                {
                    if(CRC.CRID==0)
                    {
                        Coupon_Rule CR = new Coupon_Rule();
                        CR.Coupon_Header = c.CH;
                        CR.SortNo = CRC.SortNo;
                        CR.Code = "";
                        CR.Price_Type = CRC.Price_Type;
                        CR.Price_Cut = CRC.Price_Cut;
                        CR.Target_Type = CRC.Target_Type;
                        CR.Target_ID = CRC.Target_ID;
                        DC.Coupon_Rule.InsertOnSubmit(CR);
                        DC.SubmitChanges();
                    }
                    else
                    {
                        var CR = DC.Coupon_Rule.FirstOrDefault(q => q.CRID == CRC.CRID && q.CHID == c.CH.CHID);
                        if(CR!=null)
                        {
                            CR.Price_Type = CRC.Price_Type;
                            CR.Price_Cut = CRC.Price_Cut;
                            CR.Target_Type = CRC.Target_Type;
                            CR.Target_ID = CRC.Target_ID;
                            DC.SubmitChanges();
                        }
                        else
                        {
                            CR = new Coupon_Rule();
                            CR.Coupon_Header = c.CH;
                            CR.SortNo = CRC.SortNo;
                            CR.Code = "";
                            CR.Price_Type = CRC.Price_Type;
                            CR.Price_Cut = CRC.Price_Cut;
                            CR.Target_Type = CRC.Target_Type;
                            CR.Target_ID = CRC.Target_ID;
                            DC.Coupon_Rule.InsertOnSubmit(CR);
                            DC.SubmitChanges();
                        }
                    }
                }

                #endregion

                #region 匯入名單
                bool CheckFileFlag = true;
                if (file_upload == null)
                    CheckFileFlag = false;
                else if (file_upload.ContentLength <= 0 || file_upload.ContentLength > 5242880)
                    CheckFileFlag = false;
                else if (file_upload.ContentType != "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
                    CheckFileFlag = false;

                if (CheckFileFlag)
                {
                    string Ex = Path.GetExtension(file_upload.FileName);
                    string FileName = $"{DT.ToString("yyyyMMddHHmmssfff")}{Ex}";
                    string SavaPath = Path.Combine(Server.MapPath("~/Files/Coupon/"), FileName);
                    file_upload.SaveAs(SavaPath);


                    List<cID> cIDs = new List<cID>();
                    ArrayList AL = ReadExcel("~/Files/Coupon/" + FileName);
                    foreach (string[] S in AL)
                    {
                        int iID = 0;
                        if (int.TryParse(S[0], out iID))
                        {
                            if (DC.Account.Any(q => q.ACID == iID && q.ActiveFlag && !q.DeleteFlag))
                                cIDs.Add(new cID { ID = iID });
                        }
                    }

                    if(cIDs.Count>0)
                    {
                        var CR5 = DC.Coupon_Rule.FirstOrDefault(q => q.CHID == c.CH.CHID && q.Target_Type == 5);
                        if(CR5!=null)
                        {
                            var CAs = (from q in DC.Coupon_Account.Where(q => q.CRID == CR5.CRID && q.ActiveFlag && !q.DeleteFlag)
                                       select new cID { ID = q.ACID }).ToList();

                            var OtherIDs = cIDs.Except(CAs);
                            foreach (var O in OtherIDs)
                            {
                                Coupon_Account CA = new Coupon_Account
                                {
                                    Coupon_Rule = CR5,
                                    ACID = O.ID,
                                    OHID = 0,
                                    OPID = 0,
                                    UsedDate = DT,
                                    Note = "",
                                    ActiveFlag = true,
                                    DeleteFlag = false,
                                    CreDate = DT,
                                    UpdDate = DT,
                                    SaveACID = ACID

                                };
                                DC.Coupon_Account.InsertOnSubmit(CA);
                                DC.SubmitChanges();
                            }
                        }
                    }
                    
                }
                #endregion

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
            var Ns = DC.Coupon_Account.Where(q => !q.DeleteFlag && q.CRID == ID);
            if (c.sKey != "")
                Ns = Ns.Where(q => (q.Account.Name_First + q.Account.Name_Last) == c.sKey);

            var CR = DC.Coupon_Rule.FirstOrDefault(q => q.CRID == ID);
            if (CR == null)
                Error += "參數錯誤...無法顯示";
            else
            {
                if (CR.Coupon_Header.EDateTime <= DT)
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
            #endregion

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
                    var CR5 = DC.Coupon_Rule.FirstOrDefault(q => q.CRID == ID);
                    if (CR5 != null)
                    {
                        foreach (string[] str in IDList)
                        {
                            int iACID = 0;
                            if (int.TryParse(str[0], out iACID))
                            {
                                var AC = DC.Account.FirstOrDefault(q => q.ACID == iACID && !q.DeleteFlag);
                                var CA = DC.Coupon_Account.FirstOrDefault(q => q.ACID == iACID && q.CRID == CR5.CRID);
                                if (AC != null && CA == null)
                                {
                                    CAs_New.Add(new Coupon_Account
                                    {
                                        Coupon_Rule = CR5,
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
                        }
                        SetAlert("上傳名單完成", 1);
                    }
                    else
                        SetAlert("此折價劵尚無指定名單的折價規則", 2);
                }
            }
            return View(GetCoupon_Account_List(ID, FC));
        }

        #endregion

        #region 批次新增班級
        public class cProductClass_BatchAdd
        {
            public int RowCt = 0;
            public string sDate = DateTime.Now.ToString("yyyy-MM-dd");
            public cClassCell cCBasic = new cClassCell();
            public List<cClassCell> cCs = new List<cClassCell>();
        }

        public cProductClass_BatchAdd GetProductClass_BatchAdd(int ID, FormCollection FC)
        {
            cProductClass_BatchAdd c = new cProductClass_BatchAdd();

            #region 物件初始化
            List<SelectListItem> SLIs = new List<SelectListItem>();
            SLIs = (from q in DC.Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.TeacherFlag).OrderBy(q => q.Name_First).ThenBy(q => q.Name_Last)
                    select new SelectListItem { Text = q.Name_First + q.Name_Last, Value = q.ACID.ToString() }).ToList();
            if (SLIs.Count > 0)
                SLIs[0].Selected = true;

            var P = DC.Product.FirstOrDefault(q => q.PID == ID);
            if (P != null)
            {
                ViewBag._Title += " - " + P.Title + P.SubTitle;
                c.sDate = P.EDate_Signup.ToString(DateFormat);
            }
            #endregion
            #region 前端資料匯入
            if (FC != null)
            {
                int RowCt = 0;
                int j = 0;
                if (int.TryParse(FC.Get("txb_AddClassCt"), out RowCt))
                {
                    c.RowCt = RowCt;
                    for (int i = 0; i <= RowCt; i++)
                    {
                        string sTitle = FC.Get("txb_ClassTitle_" + i + "_");
                        if (!string.IsNullOrEmpty(sTitle))
                        {
                            cClassCell CC = new cClassCell();
                            CC.PCID = 0;
                            CC.OIID = P.OIID;
                            CC.SortNo = i.ToString();
                            CC.ClassTitle = sTitle;//課堂名稱
                            CC.ClassSDate = FC.Get("txb_ClassSDate_" + i + "_");//開課日期
                            if (int.TryParse(FC.Get("txb_ClassCutDay_" + i + "_"), out j))
                                CC.ClassCutDay = j;//相隔週期
                            if (int.TryParse(FC.Get("txb_ClassCt_" + i + "_"), out j))
                                CC.ClassCt = j;//上課堂數
                            CC.ProductType = P.ProductType;//上課屬性:實體/線上
                            CC.ClassSTime = FC.Get("txb_ClassSTime_" + i + "_");//上課時間-始
                            CC.ClassETime = FC.Get("txb_ClassETime_" + i + "_");//上課時間-末
                            CC.ClassPhoneNo = FC.Get("txb_ClassPhoneNo_" + i + "_");//連絡電話
                            CC.ClassLocationName = FC.Get("txb_ClassLocationName_" + i + "_");//地標名稱
                            CC.ClassAddress = FC.Get("txb_ClassAddress_" + i + "_");//上課地點
                            CC.ClassMeetURL = FC.Get("txb_ClassMeetURL_" + i + "_");//上課網址
                            if (int.TryParse(FC.Get("txb_ClassPeopleCt_" + i + "_"), out j))
                                CC.ClassPeopleCt = j;//人數限制
                            if (int.TryParse(FC.Get("txb_ClassGraduateAddDay_" + i + "_"), out j))
                                CC.ClassGraduateAddDay = j;//結業準備天數
                            if (int.TryParse(FC.Get("txb_TeacherID_" + i + "_"), out j))
                                CC.ClassTeacher_ACID = j;//講師的ACID
                            var AC = DC.Account.FirstOrDefault(q => q.ACID == CC.ClassTeacher_ACID);
                            if (AC != null)
                                CC.ClassTeacher_Name = AC.Name_First + AC.Name_Last;
                            CC.cCCTs = null;
                            c.cCs.Add(CC);
                        }
                    }
                }
            }
            else
            {
                c.RowCt = 1;
                c.cCs.Add(new cClassCell { SortNo = "1", ProductType = P.ProductType, ClassSDate = P.EDate_Signup.ToString(DateFormat) });
                c.cCBasic.ProductType = P.ProductType;
            }
            #endregion
            #region 檢查輸入資料
            Error = "";

            foreach (var CC in c.cCs)
            {
                TimeSpan STime = new TimeSpan();
                TimeSpan ETime = new TimeSpan();
                TimeSpan.TryParse(CC.ClassSTime, out STime);
                TimeSpan.TryParse(CC.ClassETime, out ETime);
                if (ETime < STime)
                    Error += CC.ClassTitle + "的上課時間輸入錯誤<br/>";
            }
            #endregion
            return c;
        }
        [HttpGet]
        public ActionResult ProductClass_BatchAdd(int ID)
        {
            GetViewBag();
            if (ID == 0)
                SetAlert("請先選擇課程後再建置班級", 2, "/Admin/StoreSet/Product_List");
            else
            {
                var P = DC.Product.FirstOrDefault(q => !q.DeleteFlag && q.PID == ID);
                if (P == null)
                    SetAlert("請先選擇課程後再建置班級", 2, "/Admin/StoreSet/Product_List");
            }
            return View(GetProductClass_BatchAdd(ID, null));
        }
        [HttpPost]
        public ActionResult ProductClass_BatchAdd(int ID, FormCollection FC)
        {
            GetViewBag();
            ACID = GetACID();
            var c = GetProductClass_BatchAdd(ID, FC);
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                var P = DC.Product.FirstOrDefault(q => !q.DeleteFlag && q.PID == ID);
                foreach (var CC in c.cCs.OrderBy(q => q.SortNo))
                {
                    Product_Class PC_ = new Product_Class
                    {
                        Product = P,
                        Title = CC.ClassTitle,
                        PeopleCt = CC.ClassPeopleCt,
                        PhoneNo = CC.ClassPhoneNo,
                        LocationName = CC.ClassLocationName,
                        Address = CC.ClassAddress,
                        MeetURL = "",
                        GraduateDate = DT,
                        ActiveFlag = true,
                        DeleteFlag = false,
                        CreDate = DT,
                        UpdDate = DT,
                        SaveACID = ACID
                    };
                    DateTime SDate = DateTime.Now;
                    DateTime.TryParse(CC.ClassSDate, out SDate);
                    TimeSpan STime = new TimeSpan();
                    TimeSpan ETime = new TimeSpan();
                    TimeSpan.TryParse(CC.ClassSTime, out STime);
                    TimeSpan.TryParse(CC.ClassSTime, out STime);

                    for (int i = 0; i < CC.ClassCt; i++)
                    {
                        Product_ClassTime PCT = new Product_ClassTime
                        {
                            Product_Class = PC_,
                            ClassDate = SDate,
                            STime = STime,
                            ETime = ETime
                        };
                        PC_.Product_ClassTime.Add(PCT);

                        SDate = SDate.AddDays(CC.ClassCutDay);
                    }
                    PC_.GraduateDate = SDate;
                    DC.Product_Class.InsertOnSubmit(PC_);
                    DC.SubmitChanges();

                    var AC = DC.Account.FirstOrDefault(q => q.ACID == CC.ClassTeacher_ACID);
                    if (AC != null)
                    {
                        var T = DC.Teacher.FirstOrDefault(q => q.ACID == AC.ACID);
                        if (T == null)
                        {
                            T = new Teacher
                            {
                                ACID = AC.ACID,
                                Title = AC.Name_First + AC.Name_Last,
                                Note = "",
                                ActiveFlag = true,
                                DeleteFlag = false,
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = ACID
                            };
                            DC.Teacher.InsertOnSubmit(T);
                            DC.SubmitChanges();
                        }
                        M_Product_Teacher M = new M_Product_Teacher
                        {
                            Product = P,
                            Product_Class = PC_,
                            TID = T.TID,
                            Title = T.Title,
                            Note = "",
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = ACID
                        };
                        DC.M_Product_Teacher.InsertOnSubmit(M);
                        DC.SubmitChanges();
                    }

                }

                SetAlert("新增完成", 1, "/Admin/StoreSet/ProductClass_BatchEdit/" + P.PID);
            }
            return View(c);
        }
        #endregion
        #region 批次班級一覽表
        public class cGetProductClass_BatchEdit
        {
            public Product P = new Product();
            public List<cClassCell> CCs = new List<cClassCell>();
        }
        public cGetProductClass_BatchEdit GetProductClass_BatchEdit(int ID, FormCollection FC)
        {
            cGetProductClass_BatchEdit c = new cGetProductClass_BatchEdit();
            #region 資料庫匯入
            var P = DC.Product.FirstOrDefault(q => !q.DeleteFlag && q.PID == ID);
            if (P == null)
                SetAlert("查無此課程,請重新操作", 2, "/Admin/StoreSet/Product_List");
            else
            {
                c.P = P;
                ViewBag._Title += " - " + P.Title + P.SubTitle;
                var Cs = DC.Product_Class.Where(q => !q.DeleteFlag && q.PID == P.PID);
                foreach (var C in Cs.OrderBy(q => q.PCID))
                {
                    cClassCell CC = new cClassCell();
                    CC.PCID = C.PCID;
                    CC.OIID = P.OIID;
                    CC.SortNo = C.PCID.ToString();
                    CC.ClassTitle = C.Title;//課堂名稱

                    CC.ProductType = P.ProductType;//上課屬性:實體/線上
                    CC.ClassPeopleCt = C.PeopleCt;//人數限制
                    CC.ClassPhoneNo = C.PhoneNo;//連絡電話
                    CC.ClassLocationName = C.LocationName;//地標名稱
                    CC.ClassAddress = C.Address;//上課地點
                    CC.ClassMeetURL = C.MeetURL;//上課網址
                    CC.ClassGraduateDate = C.GraduateDate.ToString(DateFormat);//預計結業日期
                                                                               //講師部分
                    var MPT = DC.M_Product_Teacher.FirstOrDefault(q => q.PID == P.PID && q.PCID == C.PCID);
                    if (MPT != null)
                    {
                        CC.TID = MPT.MID;
                        CC.ClassTeacher_Name = MPT.Title;
                        CC.ClassTeacher_ACID = MPT.SaveACID;
                    }

                    CC.cCCTs = new List<cClassCellTime>();
                    CC.OrderCt = DC.Order_Product.Count(q => q.Order_Header.Order_Type == 2 && !q.Order_Header.DeleteFlag && q.PCID == C.PCID);
                    var CTs = DC.Product_ClassTime.Where(q => q.PCID == CC.PCID).OrderBy(q => q.ClassDate).ThenBy(q => q.STime);
                    foreach (var CT in CTs)
                    {
                        cClassCellTime CCT = new cClassCellTime();
                        CCT.PCTID = CT.PCTID;
                        CCT.ClassDate = CT.ClassDate.ToString(DateFormat);
                        CCT.STime = CT.STime.ToString(@"hh\:mm");
                        CCT.ETime = CT.ETime.ToString(@"hh\:mm");
                        if (DT.Date > CT.ClassDate)
                            CCT.JoinCt = DC.Order_Join.Count(q => q.PCTID == CT.PCTID && !q.DeleteFlag && q.Order_Product.Order_Header.Order_Type == 2 && !q.Order_Product.Order_Header.DeleteFlag);
                        else
                            CCT.JoinCt = -1;
                        CC.cCCTs.Add(CCT);
                    }


                    c.CCs.Add(CC);
                }
            }
            #endregion






            return c;
        }
        [HttpGet]
        public ActionResult ProductClass_BatchEdit(int ID)
        {
            GetViewBag();
            cGetProductClass_BatchEdit c = new cGetProductClass_BatchEdit();
            if (ID == 0)
                SetAlert("請先選擇課程後再建置班級", 2, "/Admin/StoreSet/Product_List");
            else
            {
                var P = DC.Product.FirstOrDefault(q => !q.DeleteFlag && q.PID == ID);
                if (P == null)
                    SetAlert("請先選擇課程後再建置班級", 2, "/Admin/StoreSet/Product_List");
                else
                    c = GetProductClass_BatchEdit(ID, null);
            }
            return View(c);
        }

        #endregion
        #region 批次班級 更新
        public class cProductClass_Update
        {
            public int PCID { get; set; } = 0;
            public string ClassTitle { get; set; } = "";
            public string LocationName { get; set; } = "";
            public string ClassAddress { get; set; } = "";
            public string ClassMeetURL { get; set; } = "";
            public string ClassPhoneNo { get; set; } = "";
            public string GraduateDate { get; set; } = "";
            public int TeacherID { get; set; } = 0;
            public string TimeStr { get; set; } = "";
            public int ClassPeopleCt { get; set; } = 0;
        }
        public class cTime
        {
            public int PCTID = 0;
            public DateTime ClassDate = DateTime.Now;
            public TimeSpan STime = new TimeSpan();
            public TimeSpan ETime = new TimeSpan();
        }
        [HttpPost]
        public string ProductClass_UpdateClass(cProductClass_Update c)
        {
            Error = "";
            ACID = GetACID();
            string[] Times = c.TimeStr.Split(';');
            List<cTime> Ts = new List<cTime>();
            for (int i = 0; i < Times.Length; i++)
            {
                string[] sT = Times[i].Split(',');//PCTID,Date,STime,ETime;

                int _PCTID = 0;
                DateTime _ClassDate = DateTime.Now;
                TimeSpan _STime = new TimeSpan();
                TimeSpan _ETime = new TimeSpan();
                int.TryParse(sT[0], out _PCTID);
                DateTime.TryParse(sT[1], out _ClassDate);
                TimeSpan.TryParse(sT[2], out _STime);
                TimeSpan.TryParse(sT[3], out _ETime);
                if (_ETime < _STime)
                {
                    TimeSpan _Time = _ETime;
                    _ETime = _STime;
                    _STime = _Time;
                }
                Ts.Add(new cTime
                {
                    PCTID = _PCTID,
                    ClassDate = _ClassDate,
                    STime = _STime,
                    ETime = _ETime
                });

            }

            var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == c.PCID && !q.DeleteFlag);
            if (c.PCID == 0)
                Error += "班級ID遺失...無法後續處理<br/>";
            else if (PC == null)
                Error += "查無此班<br/>";
            else if (string.IsNullOrEmpty(c.ClassAddress) && string.IsNullOrEmpty(c.ClassMeetURL))
                Error += "上課地點或網址至少要填入一個<br/>";
            else if (string.IsNullOrEmpty(c.ClassPhoneNo))
                Error += "請輸入連絡電話<br/>";
            else if (Ts.Count == 0)
                Error += "本班級沒有上課時間,請建立後再行調整<br/>";
            else
            {
                foreach (var T in Ts)
                    if (T.ETime <= T.STime)
                        Error += T.ClassDate + "的啟始或結束時間錯誤<br/>";

                var T_Max = Ts.OrderByDescending(q => q.ClassDate).First();
                DateTime _GraduateDate = PC.GraduateDate;
                if (DateTime.TryParse(c.GraduateDate, out _GraduateDate))
                {
                    if (_GraduateDate.Date < T_Max.ClassDate)
                        Error += "結業日期應該在課程最後一天之後<br/>";
                }
                else
                    Error += "結業日期格式錯誤<br/>";
                if (Error == "")
                {
                    PC.Title = c.ClassTitle;
                    PC.LocationName = c.LocationName;
                    PC.Address = c.ClassAddress;
                    PC.MeetURL = c.ClassMeetURL;
                    PC.PhoneNo = c.ClassPhoneNo;
                    PC.GraduateDate = _GraduateDate;
                    PC.PeopleCt = c.ClassPeopleCt;

                    PC.UpdDate = DT;
                    PC.SaveACID = ACID;
                    DC.SubmitChanges();
                    var PCTs = PC.Product_ClassTime.Where(q => q.PCID == PC.PCID);
                    foreach (var PCT in PCTs)
                    {
                        var T = Ts.FirstOrDefault(q => q.PCTID == PCT.PCTID);
                        if (T != null)
                        {
                            PCT.ClassDate = T.ClassDate;
                            PCT.STime = T.STime;
                            PCT.ETime = T.ETime;
                            DC.SubmitChanges();
                        }
                    }

                    var Tea = DC.Teacher.FirstOrDefault(q => q.ACID == c.TeacherID);
                    if (Tea != null)
                    {
                        var M = DC.M_Product_Teacher.FirstOrDefault(q => q.PCID == PC.PCID);
                        if (M == null)
                        {
                            M = new M_Product_Teacher
                            {
                                Product = PC.Product,
                                Product_Class = PC,
                                TID = Tea.TID,
                                Title = Tea.Title,
                                Note = "",
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = ACID
                            };
                            DC.M_Product_Teacher.InsertOnSubmit(M);
                            DC.SubmitChanges();
                        }
                        else
                        {
                            M.TID = Tea.TID;
                            M.Title = Tea.Title;
                            M.UpdDate = DT;
                            M.SaveACID = ACID;
                            DC.SubmitChanges();
                        }
                    }
                }
            }
            return Error;
        }
        #endregion
        #region 批次班級 複製
        [HttpGet]
        public string ProductClass_CopyClass(int PCID)
        {
            Error = "";
            ACID = GetACID();
            var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == PCID);
            if (PC != null)
            {
                Product_Class PC_ = new Product_Class
                {
                    Product = PC.Product,
                    Title = PC.Title,
                    PeopleCt = PC.PeopleCt,
                    PhoneNo = PC.PhoneNo,
                    LocationName = PC.LocationName,
                    Address = PC.Address,
                    MeetURL = PC.MeetURL,
                    GraduateDate = PC.GraduateDate,
                    ActiveFlag = PC.ActiveFlag,
                    DeleteFlag = PC.DeleteFlag,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID
                };
                DC.Product_Class.InsertOnSubmit(PC_);
                DC.SubmitChanges();

                var PCTs = DC.Product_ClassTime.Where(q => q.PCID == PCID).OrderBy(q => q.ClassDate);
                foreach (var PCT in PCTs)
                {
                    Product_ClassTime PCT_ = new Product_ClassTime
                    {
                        Product_Class = PC_,
                        ClassDate = PCT.ClassDate,
                        STime = PCT.STime,
                        ETime = PCT.ETime
                    };
                    DC.Product_ClassTime.InsertOnSubmit(PCT_);
                    DC.SubmitChanges();
                }

                var T = DC.M_Product_Teacher.FirstOrDefault(q => q.PCID == PCID);
                if (T != null)
                {
                    M_Product_Teacher T_ = new M_Product_Teacher
                    {
                        Product = PC.Product,
                        Product_Class = PC_,
                        TID = T.TID,
                        Title = T.Title,
                        Note = T.Note,
                        CreDate = DT,
                        UpdDate = DT,
                        SaveACID = ACID
                    };
                    DC.M_Product_Teacher.InsertOnSubmit(T_);
                    DC.SubmitChanges();

                }
            }
            return Error;
        }
        #endregion
        #region 課程報名名單/打卡一覽
        public cTableList GetProductClass_JoinList(int PCID, int PCTID, FormCollection FC)
        {
            #region 物件初始化
            var PCT = DC.Product_ClassTime.FirstOrDefault(q => q.PCTID == PCTID);
            if (PCT != null)
                ViewBag._Title += " - " + PCT.Product_Class.Title + "：" + PCT.ClassDate + " " + PCT.STime.ToString(@"hh\:mm") + "～" + PCT.ETime.ToString(@"hh\:mm");
            else
            {
                var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == PCID);
                if (PC != null)
                    ViewBag._Title += " - " + PC.Title;
            }
            #region 前端資料帶入
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            #endregion
            cTableList c = new cTableList();
            c.Title = "";
            c.NowPage = iNowPage;
            c.NumCut = iNumCut;
            c.Rs = new List<cTableRow>();

            #endregion
            #region 資料庫資料帶入

            var Ns = DC.Order_Product.Where(q => q.Order_Header.Order_Type == 2 && !q.Order_Header.DeleteFlag && q.PCID == PCID);
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
                Ns = Ns.Where(q => q.Product.OIID == 1);

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "會員ID" });
            TopTitles.Add(new cTableCell { Title = "會員名稱" });
            TopTitles.Add(new cTableCell { Title = "所屬小組" });
            TopTitles.Add(new cTableCell { Title = "連絡電話" });
            if (PCTID == 0)//只顯示報名的人
            {
                TopTitles.Add(new cTableCell { Title = "訂單ID" });
                TopTitles.Add(new cTableCell { Title = "訂購日期" });
                TopTitles.Add(new cTableCell { Title = "訂購班別" });
                TopTitles.Add(new cTableCell { Title = "課程狀態" });
            }
            else//顯示這個班打卡的人
            {
                TopTitles.Add(new cTableCell { Title = "打卡狀態" });
            }
            c.Rs.Add(SetTableRowTitle(TopTitles));
            c.TotalCt = Ns.Count();
            c.MaxNum = GetMaxNum(c.TotalCt, c.NumCut);
            Ns = Ns.OrderBy(q => q.Order_Header.CreDate).Skip((iNowPage - 1) * c.NumCut).Take(c.NumCut);

            var Cons = (from q in DC.Contect.Where(q => q.TargetType == 2 && q.ContectType == 1)
                        join p in Ns
                        on q.TargetID equals p.Order_Header.ACID
                        select q).ToList();
            var OJs = (from q in DC.Order_Join.Where(q => !q.DeleteFlag && q.PCTID == PCTID)
                       join p in Ns
                       on q.OPID equals p.OPID
                       select q).ToList();
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Value = N.Order_Header.ACID.ToString() });//會員ID
                cTR.Cs.Add(new cTableCell { Value = N.Order_Header.Account.Name_First + N.Order_Header.Account.Name_Last });//會員名稱
                cTR.Cs.Add(new cTableCell { Value = string.Join(",", GetMOIAC(8, 0, N.Order_Header.ACID).Select(q => q.OrganizeInfo.Title + q.OrganizeInfo.Organize.Title)) });//所屬小組
                var Con = Cons.FirstOrDefault(q => q.TargetID == N.Order_Header.ACID);
                cTR.Cs.Add(new cTableCell { Value = Con != null ? Con.ContectValue : "" });//連絡電話
                if (PCTID == 0)//只顯示報名的人
                {
                    cTR.Cs.Add(new cTableCell { Value = N.Order_Header.OHID.ToString() });//訂單ID
                    cTR.Cs.Add(new cTableCell { Value = N.Order_Header.UpdDate.ToString(DateFormat) });//訂購日期
                    cTR.Cs.Add(new cTableCell { Value = N.Product_Class.Title });//訂購班別
                    string sGraduation = "尚未結業";
                    if (N.Graduation_Flag && N.Graduation_Date != N.CreDate)
                        sGraduation = "已於" + N.Graduation_Date.ToString(DateFormat) + "結業";
                    cTR.Cs.Add(new cTableCell { Value = sGraduation });//課程狀態
                }
                else//顯示這個班打卡的人
                {
                    var OJ = OJs.FirstOrDefault(q => q.ACID == N.Order_Header.ACID);
                    cTR.Cs.Add(new cTableCell { Value = OJ == null ? "尚未打卡" : OJ.CreDate.ToString(DateTimeFormat) });//打卡狀態
                }
                c.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion


            return c;
        }
        [HttpGet]
        public ActionResult ProductClass_JoinList(int ID, int PCTID = 0)
        {
            GetViewBag();
            return View(GetProductClass_JoinList(ID, PCTID, null));
        }
        [HttpPost]
        public ActionResult ProductClass_JoinList(int ID, int PCTID = 0, FormCollection FC = null)
        {
            GetViewBag();
            return View(GetProductClass_JoinList(ID, PCTID, FC));
        }

        #endregion

        #region 訂單-列表
        public class cOrder_List
        {
            public string ProductTitle = "";//課程標題關鍵字
            public string AccountTitle = "";//會員姓名關鍵字
            public string SDate = "";//訂單送出起始日
            public string EDate = "";//訂單送出結束日
            public List<SelectListItem> OTSL = new List<SelectListItem>();//訂單狀況
            public List<SelectListItem> OPSL = new List<SelectListItem>();//交易方式
            public List<SelectListItem> OISL = new List<SelectListItem>();//所屬旌旗
            public cTableList cTL = new cTableList();
        }
        public cOrder_List GetOrder_List(FormCollection FC)
        {
            cOrder_List c = new cOrder_List();
            ACID = GetACID();
            #region 物件初始化
            int iNumCut = 10;
            int iNowPage = 1;
            int OIID = -1;
            int OTID = -1;
            int OPID = -1;
            //訂單狀況(包含購物車未結帳的人)
            c.OTSL = new List<SelectListItem>();
            c.OTSL.Add(new SelectListItem { Text = "請選擇", Value = "-1", Selected = true });
            for (int i = 0; i < sOrderType.Length; i++)
                c.OTSL.Add(new SelectListItem { Text = sOrderType[i], Value = i.ToString() });

            c.OPSL = new List<SelectListItem>();
            c.OPSL.Add(new SelectListItem { Text = "請選擇", Value = "-1", Selected = true });
            for (int i = 0; i < sPayType.Length; i++)
                c.OPSL.Add(new SelectListItem { Text = sPayType[i], Value = i.ToString() });

            //所屬協會
            c.OISL = new List<SelectListItem>();
            c.OISL.Add(new SelectListItem { Text = "請選擇", Value = "-1", Selected = true });
            var OI2s = DC.M_OI2_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID);
            var OI1s = DC.OrganizeInfo.Where(q => q.OID == 1 && !q.DeleteFlag).ToList();
            var OI2_1 = OI2s.FirstOrDefault(q => q.OIID == 1);
            if (ACID == 1 || OI2_1 != null)
            {
                c.OISL.AddRange(from q in DC.OrganizeInfo.Where(q => q.OID == 1 && q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID).OrderBy(q => q.OID).ThenBy(q => q.OIID)
                                select new SelectListItem { Text = q.Title + q.Organize.Title, Value = q.OIID.ToString() });
            }
            else
            {
                OI1s = (from q in OI2s.GroupBy(q => q.OrganizeInfo.ParentID).Select(q => q.Key).ToList()
                        join p in OI1s.ToList()
                        on q equals p.OIID
                        select p).ToList();
                c.OISL.AddRange(from q in OI1s.OrderBy(q => q.OIID)
                                select new SelectListItem { Text = q.Title + q.Organize.Title, Value = q.OIID.ToString() });
            }

            #region 前端資料帶入

            if (FC != null)
            {
                iNumCut = Convert.ToInt32(FC.Get("ddl_ChangePageCut"));
                iNowPage = Convert.ToInt32(FC.Get("hid_NextPage"));

                c.SDate = FC.Get("txb_SDate");
                c.EDate = FC.Get("txb_EDate");

                c.ProductTitle = FC.Get("txb_ProductTitle");
                c.AccountTitle = FC.Get("txb_AccountTitle");

                OIID = Convert.ToInt32(FC.Get("ddl_OI2"));
                c.OISL.ForEach(q => q.Selected = false);
                c.OISL.First(q => q.Value == OIID.ToString()).Selected = true;

                OTID = Convert.ToInt32(FC.Get("ddl_OT"));
                c.OTSL.ForEach(q => q.Selected = false);
                c.OTSL.First(q => q.Value == OTID.ToString()).Selected = true;

                OPID = Convert.ToInt32(FC.Get("ddl_OP"));
                c.OPSL.ForEach(q => q.Selected = false);
                c.OPSL.First(q => q.Value == OPID.ToString()).Selected = true;
            }

            #endregion


            c.cTL = new cTableList();
            c.cTL.Title = "";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            #endregion
            #region 資料庫資料帶入
            var Ns = DC.Order_Header.Where(q => !q.DeleteFlag);
            if (!string.IsNullOrEmpty(c.ProductTitle))
                Ns = Ns.Where(q => q.Order_Product.Any(p => p.Product.Title.Contains(c.ProductTitle) || p.Product.SubTitle.Contains(c.ProductTitle)));
            if (!string.IsNullOrEmpty(c.AccountTitle))
                Ns = Ns.Where(q => (q.Account.Name_First + q.Account.Name_Last).Contains(c.AccountTitle));

            //旌旗權限檢視門檻設置
            if (OIID > 0)
                Ns = Ns.Where(q => q.OIID == OIID);
            else if (ACID == 1 || OI2_1 != null)//Admin 或可以看全部旌旗
            {
            }
            else
            {
                Ns = from q in Ns
                     join p in OI1s.GroupBy(q => q.OIID).Select(q => q.Key)
                     on q.OIID equals p
                     select q;
            }
            if (OPID >= 0)
                Ns = Ns.Where(q => q.Order_Paid.Any(p => p.PayType.PayTypeID == OPID));
            if (OTID >= 0)
                Ns = Ns.Where(q => q.Order_Type == OTID);

            if (!string.IsNullOrEmpty(c.SDate))
            {
                DateTime SDT_ = DateTime.Now;
                if (DateTime.TryParse(c.SDate, out SDT_))
                    Ns = Ns.Where(q => q.CreDate.Date >= SDT_.Date);

            }

            if (!string.IsNullOrEmpty(c.EDate))
            {
                DateTime EDT_ = DateTime.Now;
                if (DateTime.TryParse(c.EDate, out EDT_))
                    Ns = Ns.Where(q => q.CreDate.Date <= EDT_.Date);
            }

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "訂單編號" });
            TopTitles.Add(new cTableCell { Title = "訂單日期" });
            TopTitles.Add(new cTableCell { Title = "所屬協會" });
            TopTitles.Add(new cTableCell { Title = "訂購人" });
            TopTitles.Add(new cTableCell { Title = "總金額" });
            TopTitles.Add(new cTableCell { Title = "交易方式" });
            TopTitles.Add(new cTableCell { Title = "訂單狀態" });

            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.CreDate).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);

            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/Order_Info/" + N.OHID, Target = "_self", Value = "檢視" });//檢視
                cTR.Cs.Add(new cTableCell { Value = N.OHID.ToString() });//訂單編號
                cTR.Cs.Add(new cTableCell { Value = N.CreDate.ToString(DateTimeFormat) });//訂單日期
                var OI = OI1s.FirstOrDefault(q => q.OIID == N.OIID);
                if (OI != null)
                    cTR.Cs.Add(new cTableCell { Value = OI.Title + OI.Organize.Title });//所屬協會
                else
                    cTR.Cs.Add(new cTableCell { Value = "--" });//無所屬協會
                cTR.Cs.Add(new cTableCell { Value = N.Account.Name_First + N.Account.Name_Last });//訂購人
                cTR.Cs.Add(new cTableCell { Value = N.TotalPrice.ToString() }); //總金額

                if (N.Order_Paid != null)
                {
                    var OP = N.Order_Paid.FirstOrDefault();
                    if (OP != null)
                        cTR.Cs.Add(new cTableCell { Value = sPayType[OP.PayType.PayTypeID] });//交易方式
                    else
                        cTR.Cs.Add(new cTableCell { Value = "--" });
                }
                else
                    cTR.Cs.Add(new cTableCell { Value = "--" });

                if (N.Order_Type != 1)//訂單狀態 非交易中
                    cTR.Cs.Add(new cTableCell { Value = sOrderType[N.Order_Type] });
                else if (N.Order_Paid != null)//交易中
                {
                    var OP = N.Order_Paid.FirstOrDefault();
                    if (OP != null)
                    {
                        if (OP.PayType.PayTypeID == 0)//現金
                        {
                            cTableCell cTC = new cTableCell();
                            cTC.cTCs.Add(new cTableCell { Value = sOrderType[N.Order_Type] });
                            cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "javascript:ChengeOrderType(" + N.OHID + ",2);", Target = "_self", Value = "改為已繳費" });//訂單狀態

                            cTR.Cs.Add(cTC);
                        }
                        else
                            cTR.Cs.Add(new cTableCell { Value = sOrderType[N.Order_Type] });
                    }
                    else
                        cTR.Cs.Add(new cTableCell { Value = sOrderType[N.Order_Type] });
                }
                else
                    cTR.Cs.Add(new cTableCell { Value = sOrderType[N.Order_Type] });


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
        public ActionResult Order_List(FormCollection FC = null)
        {
            GetViewBag();
            return View(GetOrder_List(FC));
        }
        #endregion
        #region 訂單-內容
        public class cGetOrder_Info
        {
            public int OHID = 0;
            public string CreDate = "";
            public string PayType = "";
            public string OrderType = "";
            public List<cGetOrder_Info_Product> OPs = new List<cGetOrder_Info_Product>();

        }
        public class cGetOrder_Info_Product
        {
            public int OPID = 0;
            public int PID = 0;
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
        public cGetOrder_Info GetOrder_Info(int ID)
        {
            cGetOrder_Info c = new cGetOrder_Info();

            var OH = DC.Order_Header.FirstOrDefault(q => q.OHID == ID && !q.DeleteFlag);
            if (OH == null)
                SetAlert("此訂單不存在", 2, "/Admin/StoreSet/Order_List");
            else
            {
                c.OHID = OH.OHID;
                c.CreDate = OH.CreDate.ToString(DateTimeFormat);

                var OPaid = DC.Order_Paid.FirstOrDefault(q => q.OHID == c.OHID);
                if (OPaid != null)
                    c.PayType = OPaid.PayType.Title;
                c.OrderType = sOrderType[OH.Order_Type];

                c.OPs = new List<cGetOrder_Info_Product>();
                foreach (var OP in OH.Order_Product)
                {
                    cGetOrder_Info_Product cOP = new cGetOrder_Info_Product();
                    cOP.OPID = OP.OPID;
                    cOP.PID = OP.PID;

                    cOP.Peice = OP.Price_Finally;

                    cOP.ProductTitle = OP.Product.Title + OP.Product.SubTitle;
                    cOP.ProductType = OP.Product.ProductType == 0 ? "實體與線上" : (OP.Product.ProductType == 1 ? "實體" : "線上");
                    cOP.ProductInfo = OP.Product.ProductInfo;
                    cOP.TargetInfo = OP.Product.TargetInfo;
                    cOP.GraduationInfo = OP.Product.GraduationInfo;

                    var Class = DC.Product_Class.FirstOrDefault(q => q.PCID == OP.PCID);
                    if (Class != null)
                    {
                        cOP.ClassTitle = Class.Title;

                        var MT = DC.M_Product_Teacher.FirstOrDefault(q => q.PCID == Class.PCID);
                        if (MT != null)
                            cOP.TeacherTitle = MT.Title;

                        if (Class.GraduateDate.Date > DT.Date)
                            cOP.Graduation = "預計於" + Class.GraduateDate.ToString(DateFormat) + "結業計算";
                        else if (!OP.Graduation_Flag)
                            cOP.Graduation = "尚未未結業...";
                        else
                            cOP.Graduation = "已於" + OP.Graduation_Date.ToString(DateFormat) + "結業";

                        cOP.cTL = new cTableList();
                        cOP.cTL.NumCut = 0;//分頁數字一次顯示幾個
                        cOP.cTL.MaxNum = 0;//分頁數量最多多少
                                           //c.cTL.TotalCt = 0;//全部共多少資料
                        cOP.cTL.NowPage = 1;//目前所在頁數
                        cOP.cTL.NowURL = "";
                        cOP.cTL.CID = 0;
                        cOP.cTL.ATID = 0;
                        cOP.cTL.Title = "";
                        cOP.cTL.Rs = new List<cTableRow>();
                        cOP.cTL.ShowFloor = false;

                        var TopTitles = new List<cTableCell>();
                        TopTitles.Add(new cTableCell { Title = "日期", WidthPX = 160 });
                        TopTitles.Add(new cTableCell { Title = "起始時間", WidthPX = 100 });
                        TopTitles.Add(new cTableCell { Title = "結束時間", WidthPX = 100 });
                        TopTitles.Add(new cTableCell { Title = "上課簽到" });
                        cOP.cTL.Rs.Add(SetTableRowTitle(TopTitles));

                        var OJs = DC.Order_Join.Where(q => q.OPID == OP.OPID && !q.DeleteFlag).ToList();

                        var CTs = DC.Product_ClassTime.Where(q => q.PCID == Class.PCID).OrderBy(q => q.ClassDate).ThenBy(q => q.STime);
                        cOP.cTL.TotalCt = CTs.Count();//全部共多少資料
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
                            cOP.cTL.Rs.Add(SetTableCellSortNo(cTR));
                        }

                    }
                    c.OPs.Add(cOP);
                }

            }


            return c;
        }
        [HttpGet]
        public ActionResult Order_Info(int ID)
        {
            GetViewBag();
            return View(GetOrder_Info(ID));
        }
        #endregion

        #region 修改訂單狀態
        public void ChangeOrderType(int OHID, int OrderType)
        {
            var OH = DC.Order_Header.FirstOrDefault(q => q.OHID == OHID && !q.DeleteFlag);
            if (OH != null)
            {
                OH.Order_Type = OrderType;
                OH.UpdDate = DT;
                OH.SaveACID = GetACID();
                DC.SubmitChanges();

                if (OrderType == 2)
                {
                    var OP = DC.Order_Paid.FirstOrDefault(q => q.OHID == OH.OHID);
                    if (OP != null)
                    {
                        if (!OP.PaidFlag)
                        {
                            OP.PaidFlag = true;
                            OP.PaidDateTime = DT;
                            OP.PayAmt = OH.TotalPrice;
                            OP.UpdDate = DT;
                            DC.SubmitChanges();
                        }
                    }
                    else
                    {
                        Error = "";
                        var PT = DC.PayType.FirstOrDefault(q => q.OIID == OH.OIID && q.PayTypeID == 0);
                        if (PT == null)
                        {
                            Error = "no PT";
                            PT = DC.PayType.FirstOrDefault(q => q.OIID == 1 && q.PayTypeID == 0);
                        }

                        OP = new Order_Paid();
                        OP.Order_Header = OH;
                        OP.PayType = PT;
                        OP.PaidFlag = true;
                        OP.PaidDateTime = DT;
                        OP.TradeNo = Error;
                        OP.TradeAmt = OH.TotalPrice;
                        OP.PayAmt = OH.TotalPrice;
                        OP.CreDate = DT;
                        OP.UpdDate = DT;
                        DC.Order_Paid.InsertOnSubmit(OP);
                        DC.SubmitChanges();
                    }
                }

            }
        }
        #endregion
    }
}