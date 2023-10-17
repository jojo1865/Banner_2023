using Banner.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
            public List<SelectListItem> SL = new List<SelectListItem>();
            public cTableList cTL = new cTableList();
        }
        public cProduct_List GetProduct_List(FormCollection FC)
        {
            cProduct_List N = new cProduct_List();
            #region 物件初始化
            N.SL = new List<SelectListItem>();
            N.SL.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            var CCs = DC.Course_Category.Where(q => !q.DeleteFlag).OrderByDescending(q => q.CCID);
            foreach (var CC in CCs)
                N.SL.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString() });

            #region 前端資料帶入
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            N.ActiveType = Convert.ToInt32(FC != null ? FC.Get("rbl_ActiveType") : "-1");
            N.sKey = FC != null ? FC.Get("txb_Key") : "";
            int CCID = Convert.ToInt32(FC != null ? FC.Get("ddl_CC") : "0");
            N.SL.ForEach(q => q.Selected = false);
            N.SL.First(q => q.Value == CCID.ToString()).Selected = true;
            #endregion


            N.cTL = new cTableList();
            N.cTL.Title = "";
            N.cTL.NowPage = iNowPage;
            N.cTL.ItemID = "";
            N.cTL.NumCut = iNumCut;
            N.cTL.Rs = new List<cTableRow>();

            #endregion
            #region 資料庫資料帶入
            var Ns = DC.Product.Where(q => !q.DeleteFlag);
            if (N.sKey != "")
                Ns = Ns.Where(q => q.Title.Contains(N.sKey));
            if (CCID > 0)
                Ns = Ns.Where(q => q.Course.CCID == CCID);
            if (N.ActiveType >= 0)
                Ns = Ns.Where(q => q.ActiveFlag == (N.ActiveType == 1));

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
            TopTitles.Add(new cTableCell { Title = "臨櫃報名日期" });
            TopTitles.Add(new cTableCell { Title = "線上報名日期" });
            TopTitles.Add(new cTableCell { Title = "開課日期" });
            TopTitles.Add(new cTableCell { Title = "原價" });
            TopTitles.Add(new cTableCell { Title = "開班" });
            //TopTitles.Add(new cTableCell { Title = "會友限定" });
            TopTitles.Add(new cTableCell { Title = "顯示狀態" });
            TopTitles.Add(new cTableCell { Title = "交易狀態" });

            N.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            N.cTL.TotalCt = Ns.Count();
            N.cTL.MaxNum = GetMaxNum(N.cTL.TotalCt, N.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.PID).Skip((iNowPage - 1) * N.cTL.NumCut).Take(N.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/Product_Edit/" + N_.PID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = "【" + N_.Course.Course_Category.Code + "】" + N_.Course.Course_Category.Title });//課程分類
                cTR.Cs.Add(new cTableCell { Value = N_.Course.Title + " " + N_.SubTitle });//課程名稱
                cTR.Cs.Add(new cTableCell { Value = (N_.SDate_Signup_OnSite.ToString(DateFormat) + "<br/>↕<br/>" + N_.EDate_Signup_OnSite.ToString(DateFormat)) });//臨櫃報名日期
                cTR.Cs.Add(new cTableCell { Value = (N_.SDate_Signup_OnLine.ToString(DateFormat) + "<br/>↕<br/>" + N_.EDate_Signup_OnLine.ToString(DateFormat)) });//線上報名日期
                cTR.Cs.Add(new cTableCell { Value = (N_.SDate.ToString(DateFormat) + (N_.SDate == N_.EDate ? "" : "<br/>↕<br/>" + N_.EDate.ToString(DateFormat))) });//開課日期
                cTR.Cs.Add(new cTableCell { Value = N_.Price_Basic.ToString() });//原價
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/ProductClass_List?PID=" + N_.PID, Target = "_self", Value = "開班管理" });//班別
                //cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/ProductAllowAccount_List/" + N_.PID, Target = "_self", Value = "指定名單" });//限定會員
                cTR.Cs.Add(new cTableCell { Value = N_.ShowFlag ? "顯示" : "隱藏" });//顯示狀態
                cTR.Cs.Add(new cTableCell { Value = N_.ActiveFlag ? "可交易" : "不可交易" });//交易狀態
                N.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion


            return N;
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
            //所屬旌旗
            var OI2s = DC.M_OI2_Account.Where(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag && q.OIID == 1);
            if (OI2s.Any())
            {
                var OIs = DC.OrganizeInfo.Where(q => q.OID == 2 && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.OIID);
                foreach (var OI in OIs) N.OI2SL.Add(new SelectListItem { Text = OI.Title + OI.Organize.Title, Value = OI.OIID.ToString(), Selected = OI.OIID == OIs.Min(q => q.OIID) });
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
                }
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
            var Os = DC.Organize.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ItemID == "Shepherding").ToList();
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
                N.P = new Product
                {
                    CID = 0,
                    OIID = 0,
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
                    SDate = DT.AddDays(-1),
                    EDate = DT.AddDays(-1),
                    SDate_Signup_OnSite = DT.AddDays(-1),
                    EDate_Signup_OnSite = DT.AddDays(-1),
                    SDate_Signup_OnLine = DT.AddDays(-1),
                    EDate_Signup_OnLine = DT.AddDays(-1),
                    SDate_Early = DT.AddDays(-1),
                    EDate_Early = DT.AddDays(-1),
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
            }
            else
            {
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

            }

            #endregion
            #region 前端資料帶入
            if (FC != null)
            {
                if (!string.IsNullOrEmpty(FC.Get("rbl_OI")))
                    N.P.OIID = Convert.ToInt32(FC.Get("rbl_OI"));
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
                N.P.Price_Early = Convert.ToInt32(FC.Get("txb_Price_Early"));
                if (!string.IsNullOrEmpty(FC.Get("txb_SDate")))
                    N.P.SDate = Convert.ToDateTime(FC.Get("txb_SDate"));
                if (!string.IsNullOrEmpty(FC.Get("txb_EDate")))
                    N.P.EDate = Convert.ToDateTime(FC.Get("txb_EDate"));
                if (!string.IsNullOrEmpty(FC.Get("txb_SDate_Signup_OnSite")))
                    N.P.SDate_Signup_OnSite = Convert.ToDateTime(FC.Get("txb_SDate_Signup_OnSite"));
                if (!string.IsNullOrEmpty(FC.Get("txb_EDate_Signup_OnSite")))
                    N.P.EDate_Signup_OnSite = Convert.ToDateTime(FC.Get("txb_EDate_Signup_OnSite"));
                if (!string.IsNullOrEmpty(FC.Get("txb_SDate_Signup_OnLine")))
                    N.P.SDate_Signup_OnLine = Convert.ToDateTime(FC.Get("txb_SDate_Signup_OnLine"));
                if (!string.IsNullOrEmpty(FC.Get("txb_EDate_Signup_OnLine")))
                    N.P.EDate_Signup_OnLine = Convert.ToDateTime(FC.Get("txb_EDate_Signup_OnLine"));
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
        [ValidateAntiForgeryToken]
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

            if (N.P.EDate_Early != N.P.CreDate && N.P.EDate_Early.Date > N.P.EDate.Date)
                Error += "早鳥結束日應在開課結束日之前<br/>";
            if (N.P.EDate_Signup_OnSite != N.P.CreDate && N.P.EDate_Signup_OnSite > N.P.EDate.Date)
                Error += "臨櫃報名結束日應在開課結束日之前<br/>";
            if (N.P.EDate_Signup_OnLine != N.P.CreDate && N.P.EDate_Signup_OnLine.Date > N.P.EDate.Date)
                Error += "線上報名結束日應在開課結束日之前<br/>";


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
            public cTableList cTL = new cTableList();
        }
        public cProductClass_List GetProductClass_List(int PID, FormCollection FC)
        {
            cProductClass_List N = new cProductClass_List();
            N.PID = PID;
            N.sKey = FC != null ? FC.Get("txb_Key") : "";
            N.cTL.Rs = new List<cTableRow>();
            var Ns = DC.Product_Class.Where(q => q.PID > 0);
            if (PID > 0)
                Ns = DC.Product_Class.Where(q => q.PID == PID);

            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "課程名稱" });
            TopTitles.Add(new cTableCell { Title = "班級名稱" });
            TopTitles.Add(new cTableCell { Title = "星期" });
            TopTitles.Add(new cTableCell { Title = "時段" });
            TopTitles.Add(new cTableCell { Title = "人數" });
            TopTitles.Add(new cTableCell { Title = "地點" });
            TopTitles.Add(new cTableCell { Title = "講師" });

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
                if (N_.LoopFlag)
                    cTR.Cs.Add(new cTableCell { Value = sWeeks[N_.WeeklyNo] });//星期
                else
                    cTR.Cs.Add(new cTableCell { Value = N_.TargetDate.ToString(DateFormat) });//單日
                cTR.Cs.Add(new cTableCell { Value = N_.STime.ToString() + "~" + N_.ETime.ToString() });//時段
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
            public ListSelect LS_Week = new ListSelect();
            public int ProductType = 0;
        }
        public cProductClass_Edit GetProductClass_Edit(int PID, int ID, FormCollection FC)
        {
            cProductClass_Edit N = new cProductClass_Edit();
            #region 物件初始化
            N.LS_Week.Title = "週期選擇";
            for (int i = 0; i < sWeeks.Length; i++)
                N.LS_Week.ddlList.Add(new SelectListItem { Text = sWeeks[i], Value = i.ToString(), Selected = i == 0 });
            #endregion
            #region 載入資料
            var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == ID && q.PID == PID);
            if (PC == null)
            {
                N.PC = new Product_Class
                {
                    PID = PID,
                    Title = "",
                    LoopFlag = true,
                    TargetDate = DT,
                    WeeklyNo = 0,
                    STime = new TimeSpan(9, 0, 0),
                    ETime = new TimeSpan(12, 0, 0),
                    PeopleCt = 0,
                    PhoneNo = "",
                    LocationName = "",
                    Address = "",
                    MeetURL = "",
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = 1
                };
            }
            else
            {
                N.PC = PC;
                N.LS_Week.ddlList.ForEach(q => q.Selected = false);
                N.LS_Week.ddlList.First(q => q.Value == PC.WeeklyNo.ToString()).Selected = true;
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
                N.PC.LoopFlag = GetViewCheckBox(FC.Get("cbox_LoopFlag"));
                N.PC.TargetDate = Convert.ToDateTime(FC.Get("txb_TargetDate"));
                N.PC.WeeklyNo = Convert.ToInt32(FC.Get("ddl_Week"));
                N.PC.STime = TimeSpan.Parse(FC.Get("txb_STime"));
                N.PC.ETime = TimeSpan.Parse(FC.Get("txb_ETime"));
                N.PC.PeopleCt = Convert.ToInt32(FC.Get("txb_PeopleCt"));
                N.PC.PhoneNo = FC.Get("txb_PhoneNo");
                N.PC.LocationName = FC.Get("txb_LocationName");

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
        [ValidateAntiForgeryToken]
        public ActionResult ProductClass_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            int PID = GetQueryStringInInt("PID");
            ChangeTitle(ID == 0);
            var N = GetProductClass_Edit(PID, ID, FC);
            if (N.PC.PCID == 0)
            {
                DC.Product_Class.InsertOnSubmit(N.PC);
            }
            DC.SubmitChanges();
            SetAlert("存檔完成", 1, "/Admin/StoreSet/ProductClass_List?PID=" + PID);

            return View(N);
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
            #endregion


            N.cTL = new cTableList();
            N.cTL.Title = "";
            N.cTL.NowPage = iNowPage;
            N.cTL.ItemID = "";
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

    }
}