using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Web;
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
            var CCs = DC.Course_Category.Where(q => !q.DeleteFlag).OrderByDescending(q => q.CCID);
            foreach (var CC in CCs)
                N.SL.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString() });

            #region 前端資料帶入
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            N.ActiveType = Convert.ToInt32(FC != null ? FC.Get("rbl_ActiveType") : "-1");
            N.sKey = FC != null ? FC.Get("txb_Key") : "";
            int CID = Convert.ToInt32(FC != null ? FC.Get("ddl_C") : "0");
            if (CID == 0)
            {
                N.SL.ForEach(q => q.Selected = false);
                N.SL[0].Selected = true;
            }
            else
            {
                N.SL.ForEach(q => q.Selected = false);
                if (N.SL.FirstOrDefault(q => q.Value == CID.ToString()) != null)
                    N.SL.First(q => q.Value == CID.ToString()).Selected = true;
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
            var Ns = DC.Product.Where(q => !q.DeleteFlag);
            if (N.sKey != "")
                Ns = Ns.Where(q => q.Title.Contains(N.sKey));
            if (CID > 0)
                Ns = Ns.Where(q => q.CID == CID);
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
            TopTitles.Add(new cTableCell { Title = "早鳥價" });
            TopTitles.Add(new cTableCell { Title = "狀態" });

            N.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            N.cTL.TotalCt = Ns.Count();
            N.cTL.MaxNum = GetMaxNum(N.cTL.TotalCt, N.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.PID).Skip((iNowPage - 1) * N.cTL.NumCut).Take(N.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StoreSet/Product_Edit/" + N_.CID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = "【" + N_.Course.Course_Category.Code + "】" + N_.Course.Course_Category.Title });//課程分類
                cTR.Cs.Add(new cTableCell { Value = N_.Course.Title + " " + N_.SubTitle });//課程名稱
                cTR.Cs.Add(new cTableCell { Value = (N_.SDate_Signup_OnSite.ToString(DateFormat) + "~" + N_.EDate_Signup_OnSite.ToString(DateFormat)) });//臨櫃報名日期
                cTR.Cs.Add(new cTableCell { Value = (N_.SDate_Signup_OnLine.ToString(DateFormat) + "~" + N_.EDate_Signup_OnLine.ToString(DateFormat)) });//線上報名日期
                cTR.Cs.Add(new cTableCell { Value = (N_.SDate.ToString(DateFormat) + "~" + N_.EDate.ToString(DateFormat)) });//開課日期
                cTR.Cs.Add(new cTableCell { Value = N_.Price_Basic.ToString() });//原價
                cTR.Cs.Add(new cTableCell { Value = N_.Price_Early.ToString() });//早鳥價
                cTR.Cs.Add(new cTableCell { Value = N_.ActiveFlag ? "啟用" : "停用" });//狀態
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
        //[ValidateAntiForgeryToken]
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
            public List<SelectListItem> OSL = new List<SelectListItem>();
            public string[] sCourseType = new string[0];
        }
        public class cProduct_Before
        {
            public int PRID = 0;
            public int CRID = 0;
            public List<SelectListItem> CCSL_Before = new List<SelectListItem>();
            public List<SelectListItem> CSL_Before = new List<SelectListItem>();
        }
        public cProduct_Edit GetProduct_Edit(int ID, FormCollection FC)
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
            CCs = DC.Course_Category.Where(q => !q.DeleteFlag && q.Course.Count(p => p.ActiveFlag && !p.DeleteFlag && p.CID != ID) > 0).OrderByDescending(q => q.CCID).ToList();
            PB.CCSL_Before.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            foreach (var CC in CCs)
                PB.CCSL_Before.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString() });
            PB.CSL_Before = new List<SelectListItem>();
            N.PBs.Add(PB);

            //組織
            var Os = DC.Organize.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ItemID == "Shepherding").ToList();
            var O = Os.FirstOrDefault(q => q.ParentID == 0);
            while (O != null)
            {
                if (O.JobTitle != "")
                    N.OSL.Add(new SelectListItem { Text = O.JobTitle, Value = O.OID.ToString() });
                O = Os.FirstOrDefault(q => q.ParentID == O.OID);
            };
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
                    SeasonNo = (DT.Month / 4) + 1,
                    EchelonNo = 1,
                    ImgURL = "",
                    Price_Basic = 0,
                    Price_Early = 0,
                    SDate = DT,
                    EDate = DT,
                    SDate_Signup_OnSite = DT,
                    EDate_Signup_OnSite = DT,
                    SDate_Signup_OnLine = DT,
                    EDate_Signup_OnLine = DT,
                    SDate_Early = DT,
                    EDate_Early = DT,
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID
                };

                if (CID > 0)
                {
                    var CRs = DC.Course_Rool.Where(q => q.TargetType == 0 && q.CID == CID).OrderBy(q => q.CID);
                    
                    foreach (var CR in CRs)
                    {
                        if(CR.TargetType == 0)
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
                        else
                        {
                            N.PRs.Add(new Product_Rool
                            {
                                PID = 0,
                                TargetType = CR.TargetType,
                                TargetInt1 = CR.TargetInt1,
                                TargetInt2 = CR.TargetInt2,
                                CreDate = DT,
                                SaveACID = ACID
                            });
                        }
                    }
                }
            }
            else
            {
                if (N.CCSL.FirstOrDefault(q => q.Value == N.P.Course.CCID.ToString()) != null)
                {
                    N.CCSL.ForEach(q => q.Selected = false);
                    N.CCSL.FirstOrDefault(q => q.Value == N.P.Course.CCID.ToString()).Selected = true;

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

                //擋修
                    foreach (var PR in N.PRs.Where(q => q.TargetType == 0).OrderBy(q => q.PRID))
                    {
                        var C_ = DC.Course.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.CID == PR.TargetInt1);
                        if (C_ != null)
                        {
                            PB = new cProduct_Before();
                            PB.PRID = PR.PRID;
                            PB.CRID = 0;
                            CCs = DC.Course_Category.Where(q => !q.DeleteFlag && q.Course.Count(p => p.ActiveFlag && !p.DeleteFlag && p.CID != ID) > 0).OrderByDescending(q => q.CCID).ToList();
                            foreach (var CC in CCs) PB.CCSL_Before.Add(new SelectListItem { Text = "【" + CC.Code + "】" + CC.Title, Value = CC.CCID.ToString(), Selected = CC.CCID == C_.CCID });
                            var Cs = DC.Course.Where(q => q.CCID == C_.CCID).OrderByDescending(q => q.CID);
                            foreach (var C in Cs) PB.CSL_Before.Add(new SelectListItem { Text = C.Title, Value = C.CCID.ToString() });

                            N.PBs.Add(PB);
                        }
                        else//這堂課已經不見了~就直接移除
                        {
                            PR.TargetInt1 = 0;
                        }
                    }

                //職分
                foreach (var PR1 in N.PRs.Where(q => q.TargetType == 1))
                {
                    if (N.OSL.Any(q => q.Value == PR1.TargetInt1.ToString()))
                        N.OSL.Find(q => q.Value == PR1.TargetInt1.ToString()).Selected = true;
                }
            }

            #endregion
            #region 前端資料帶入
            if (FC != null)
            {
                N.P.Title = FC.Get("txb_Title");
                N.P.ProductInfo = FC.Get("txb_ProductIInfo");
                N.P.TargetInfo = FC.Get("txb_TargetInfo");
                N.P.GraduationInfo = FC.Get("txb_GraduationInfo");
                N.P.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                N.P.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                N.P.ProductType = Convert.ToInt32(FC.Get("rbl_ProductType"));
                N.P.UpdDate = DT;
                N.P.SaveACID = ACID;
                N.CCSL.ForEach(q => q.Selected = false);
                N.CCSL.FirstOrDefault(q => q.Value == FC.Get("ddl_CC")).Selected = true;
                N.P.CID = Convert.ToInt32(FC.Get("ddl_C"));
                N.CSL = new List<SelectListItem>();
                var Cs = DC.Course.Where(q => q.ActiveFlag && !q.DeleteFlag && q.CCID.ToString() == FC.Get("ddl_CC")).OrderBy(q => q.CCID);
                foreach (var C in Cs)
                {
                    if (C.CID == N.P.CID)
                        N.CSL.Add(new SelectListItem { Text = C.Title, Value = C.CID.ToString(), Selected = true });
                    else
                        N.CSL.Add(new SelectListItem { Text = C.Title, Value = C.CID.ToString() });
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
                                PID = N.P.PID,
                                TargetType = 0,
                                TargetInt1 = Convert.ToInt32(FC.Get("ddl_C_" + i)),
                                TargetInt2 = 0,
                                CreDate = DT,
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
                            PID = N.P.PID,
                            TargetType = 2,
                            TargetInt1 = Convert.ToInt32(FC.Get("rbl_Sex")),
                            TargetInt2 = 0,
                            CreDate = DT,
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
                        PID = N.P.PID,
                        TargetType = 3,
                        TargetInt1 = iMin,
                        TargetInt2 = iMax,
                        CreDate = DT,
                        SaveACID = ACID
                    });
                }
                #endregion
                #region 職分
                foreach (var _O in N.OSL)
                {
                    _O.Selected = GetViewCheckBox(FC.Get("cbox_O" + _O.Value));
                    var CR = N.PRs.FirstOrDefault(q => q.TargetInt1.ToString() == _O.Value && q.TargetType == 1);
                    if (_O.Selected && CR == null)
                    {
                        N.PRs.Add(new Product_Rool
                        {
                            PID = N.P.PID,
                            TargetType = 1,
                            TargetInt1 = Convert.ToInt32(_O.Value),
                            TargetInt2 = 0,
                            CreDate = DT,
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
        public ActionResult Product_Edit(int ID)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            return View(GetProduct_Edit(ID, null));
        }
        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Product_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            var N = GetProduct_Edit(ID, FC);
            if (N.P.Title == "")
                Error = "請輸入課程名稱";
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                /*if (N.C.CID == 0)
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
                        DC.Course_Rool.InsertOnSubmit(CR);
                    }
                    foreach (var CR in N.CRs.Where(q => q.TargetInt1 == 0 && (q.TargetType == 0 || q.TargetType == 1 || q.TargetType == 4)))
                    {
                        DC.Course_Rool.DeleteOnSubmit(CR);
                    }
                }
                DC.SubmitChanges();
                SetAlert("存檔完成", 1, "/Admin/Course/Course_List");*/
            }

            return View(N);
        }
        #endregion
    }
}