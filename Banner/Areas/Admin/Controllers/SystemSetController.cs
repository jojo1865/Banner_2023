using Banner.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Web;
using System.Web.Http.Results;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using ZXing.OneD;

namespace Banner.Areas.Admin.Controllers
{
    public class SystemSetController : PublicClass
    {
        // GET: Admin/SystemSet
        public ActionResult Index()
        {
            return View();
        }
        #region 帳號管理-列表
        public class cBackUser_List
        {
            public cTableList cTL = new cTableList();
            public string sAddURL = "/Admin/SystemSet/BackUser_Edit";
            public string Name = "";
            public string Account = "";
            public string CellPhone = "";
            public int CellPhoneZipID = 0;
            public int Sex = 0;
            public int OID = 0;
            public string OTitle = "";
        }

        public ActionResult BackUser_List()
        {
            GetViewBag();
            cBackUser_List cBUL = new cBackUser_List();
            cBUL.cTL = GetBackUser(null);

            return View(cBUL);
        }
        [HttpPost]
        public ActionResult BackUser_List(FormCollection FC)
        {
            GetViewBag();
            cBackUser_List cBUL = new cBackUser_List();
            cBUL.cTL = GetBackUser(FC);
            cBUL.Name = FC.Get("txb_Name");
            cBUL.Account = FC.Get("txb_Account");
            cBUL.CellPhone = FC.Get("txb_PhoneNo");
            cBUL.CellPhoneZipID = Convert.ToInt32(FC.Get("ddl_Zip"));
            cBUL.Sex = Convert.ToInt32(FC.Get("rbl_Sex"));
            cBUL.OID = Convert.ToInt32(FC.Get("ddl_O"));
            cBUL.OTitle = FC.Get("txb_OTitle");
            TempData["OID"] = cBUL.OID;
            TempData["OITitle"] = cBUL.OTitle;

            return View(cBUL);
        }

        private cTableList GetBackUser(FormCollection FC)
        {
            cTableList cTL = new cTableList();
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            cTL = new cTableList();
            cTL.Title = "";
            cTL.NowPage = iNowPage;
            cTL.ItemID = "";
            cTL.NowURL = "/Admin/SystemSet/BackUser_List";
            cTL.NumCut = iNumCut;
            cTL.Rs = new List<cTableRow>();

            var Ns = DC.Account.Where(q => !q.DeleteFlag && q.BackUsedFlag);
            if (ACID <= 0)
                Ns = Ns.Where(q => q.ACID == 0);
            else
            {

            }
            if (FC != null)
            {
                if (!string.IsNullOrEmpty(FC.Get("txb_Name")))
                    Ns = Ns.Where(q => (q.Name_First + q.Name_Last).Contains(FC.Get("txb_Name")));
                if (!string.IsNullOrEmpty(FC.Get("txb_Account")))
                    Ns = Ns.Where(q => q.Login.Contains(FC.Get("txb_Account")));
                if (!string.IsNullOrEmpty(FC.Get("txb_PhoneNo")))
                {
                    string PhoneNo = FC.Get("txb_PhoneNo");
                    int ZipID = Convert.ToInt32(FC.Get("ddl_PhoneZip"));
                    if (ZipID > 0)
                    {
                        Ns = from q in Ns
                             join p in DC.Contect.Where(q => q.TargetType == 2 && q.ContectType == 1 && q.ContectValue.Contains(PhoneNo) && q.ZID == ZipID).GroupBy(q => q.TargetID)
                             on q.ACID equals p.Key
                             select q;
                    }
                    else
                    {
                        Ns = from q in Ns
                             join p in DC.Contect.Where(q => q.TargetType == 2 && q.ContectType == 1 && q.ContectValue.Contains(PhoneNo)).GroupBy(q => q.TargetID)
                             on q.ACID equals p.Key
                             select q;
                    }
                }


                if (!string.IsNullOrEmpty(FC.Get("rbl_Sex")))
                    if (FC.Get("rbl_Sex") != "0")
                        Ns = Ns.Where(q => q.ManFlag == (FC.Get("rbl_Sex") == "1"));


                int iOID = Convert.ToInt32(FC.Get("ddl_O"));
                string OITitle = FC.Get("txb_OTitle");
                if (iOID > 0 || OITitle != "")
                {
                    var OIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag);
                    if (OITitle != "")
                        OIs = OIs.Where(q => q.Title.Contains(OITitle));
                    if (iOID > 0)
                        OIs = OIs.Where(q => q.OID == iOID);
                    var IDs = OIs.GroupBy(q => q.OIID).Select(q => q.Key);
                    /*var Ms = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag && (q.JoinDate == q.CreDate || q.JoinDate.Date <= DT.Date) && (q.LeaveDate == q.CreDate || q.LeaveDate.Date >= DT.Date))
                             join p in IDs
                             on q.OIID equals p
                             select q;*/
                    var Ms = from q in GetMOIAC()
                             join p in IDs
                             on q.OIID equals p
                             select q;
                    Ns = from q in Ns
                         join p in Ms.GroupBy(q => q.ACID)
                         on q.ACID equals p.Key
                         select q;
                }
            }

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "控制", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "姓名" });
            TopTitles.Add(new cTableCell { Title = "性別" });
            TopTitles.Add(new cTableCell { Title = "帳號" });
            TopTitles.Add(new cTableCell { Title = "行動電話" });

            cTL.Rs.Add(SetTableRowTitle(TopTitles));

            cTL.TotalCt = Ns.Count();
            cTL.MaxNum = GetMaxNum(cTL.TotalCt, cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.ACID).Skip((iNowPage - 1) * cTL.NumCut).Take(cTL.NumCut);
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/SystemSet/BackUser_Edit/" + N.ACID, Target = "_black", Value = "編輯" });//
                cTR.Cs.Add(new cTableCell { Value = N.Name_First + N.Name_Last });//姓名
                cTR.Cs.Add(new cTableCell { Value = N.ManFlag ? "男" : "女" });//性別
                cTR.Cs.Add(new cTableCell { Value = N.Login });//帳號
                var C = DC.Contect.Where(q => q.TargetType == 2 && q.ContectType == 1 && q.TargetID == N.ACID).OrderByDescending(q => q.CID).FirstOrDefault();
                cTR.Cs.Add(new cTableCell { Value = C != null ? C.ContectValue : "" });//行動電話
                cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            return cTL;
        }


        #endregion
        #region 帳號管理-新增/修改/刪除
        public class cBackUser_Edit
        {
            public int ACID = 0;
            public string Login = "";
            public string PW = "";
            public bool ActiveFlag = true;
            public bool DeleteFlag = false;
            public bool R4Flag = false;
            public string BackURL = "/Admin/SystemSet/BackUser_List";
            public List<SelectListItem> RList = new List<SelectListItem>();
            public List<SelectListItem> OIList = new List<SelectListItem>();
        }

        private cBackUser_Edit GetBackUser_Edit(int ID, FormCollection FC)
        {
            cBackUser_Edit cBUE = new cBackUser_Edit();
            cBUE.ACID = ID;
            var N = DC.Account.FirstOrDefault(q => q.ACID == ID && !q.DeleteFlag);
            if (N != null)
            {
                if (FC != null)
                {
                    cBUE.Login = FC.Get("txb_Login");
                    if (FC.Get("txb_PW") != "")
                        cBUE.PW = FC.Get("txb_PW");
                    cBUE.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                    cBUE.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));


                }
                else
                {
                    cBUE.Login = N.Login;
                    cBUE.PW = "";
                    cBUE.ActiveFlag = N.ActiveFlag;
                    cBUE.DeleteFlag = N.DeleteFlag;
                }
            }
            else
                SetAlert("查無資料,請重新操作", 2, "/Admin/SystemSet/BackUser_List");
            cBUE.RList = new List<SelectListItem>();
            var Rs = DC.Rool.Where(q => (q.RoolType == 3 || q.RoolType == 4) && !q.DeleteFlag && q.ActiveFlag).ToList();
            var MAs = GetMRAC(0, ID).ToList();
            foreach (var R in Rs.Where(q => q.RoolType != 5).OrderBy(q => q.RID))
            {
                var MA = MAs.FirstOrDefault(q => q.RID == R.RID);
                if (MA != null)
                {
                    if ((!cBUE.R4Flag && MA.Rool.RoolType == 4) || ID == 0)
                        cBUE.R4Flag = true;
                }
                if (FC != null)
                    cBUE.RList.Add(new SelectListItem { Text = R.Title, Value = R.RID.ToString(), Selected = GetViewCheckBox(FC.Get("cbox_rool_" + R.RID)) });
                else
                    cBUE.RList.Add(new SelectListItem { Text = R.Title, Value = R.RID.ToString(), Selected = MA != null });
            }

            //旌旗權限項目
            var MA2s = GetMOI2AC(0, ID);
            if (FC != null)
                cBUE.OIList.Add(new SelectListItem { Text = "全部旌旗", Value = "1", Selected = GetViewCheckBox(FC.Get("cbox_OI_1")) });
            else
                cBUE.OIList.Add(new SelectListItem { Text = "全部旌旗", Value = "1", Selected = MA2s.Any(q => q.OIID == 1) });
            foreach (var OI in DC.OrganizeInfo.Where(q => !q.DeleteFlag && q.OID == 2).OrderBy(q => q.OIID))
            {
                var MA = MA2s.FirstOrDefault(q => q.OIID == OI.OIID);
                if (FC != null)
                    cBUE.OIList.Add(new SelectListItem { Text = OI.Title + OI.Organize.Title, Value = OI.OIID.ToString(), Selected = GetViewCheckBox(FC.Get("cbox_OI_" + OI.OIID)) });
                else
                    cBUE.OIList.Add(new SelectListItem { Text = OI.Title + OI.Organize.Title, Value = OI.OIID.ToString(), Selected = MA != null });
            }

            return cBUE;
        }
        public ActionResult BackUser_Edit(int ID)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            cBackUser_Edit cBUE = GetBackUser_Edit(ID, null);
            return View(cBUE);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BackUser_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            cBackUser_Edit cBUE = GetBackUser_Edit(ID, FC);
            Error = "";
            var N_ = DC.Account.FirstOrDefault(q => q.ACID != ID && !q.DeleteFlag && q.Login == cBUE.Login);
            if (N_ != null)
                Error += "您輸入的帳號與他人重複,請重新輸入</br>";
            else if (cBUE.PW != "")
            {
                if (!CheckPasswork(cBUE.PW))
                    Error += "密碼必須為包含大小寫英文與數字的8碼以上字串</br>";
            }
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                var N = DC.Account.FirstOrDefault(q => q.ACID == ID);
                if (N != null)
                {
                    N.Login = cBUE.Login;
                    if (cBUE.PW != "")
                        N.Password = HSM.Enc_1(cBUE.PW);
                    N.ActiveFlag = cBUE.ActiveFlag;
                    N.DeleteFlag = cBUE.DeleteFlag;
                    N.UpdDate = DT;
                    N.SaveACID = ACID;
                    DC.SubmitChanges();

                    var MAs = GetMRAC(0, ID).ToList();
                    foreach (var B in cBUE.RList)
                    {
                        var MA = MAs.FirstOrDefault(q => q.RID.ToString() == B.Value);
                        if (MA == null)
                        {
                            if (B.Selected)
                            {
                                MA = new M_Rool_Account
                                {
                                    ACID = ID,
                                    RID = Convert.ToInt32(B.Value),
                                    JoinDate = DT,
                                    LeaveDate = DT,
                                    Note = "",
                                    ActiveFlag = true,
                                    DeleteFlag = false,
                                    CreDate = DT,
                                    UpdDate = DT,
                                    SaveACID = ACID
                                };
                                DC.M_Rool_Account.InsertOnSubmit(MA);
                                DC.SubmitChanges();
                            }
                        }
                        else
                        {
                            if (!B.Selected)
                            {
                                MA.ActiveFlag = false;
                                MA.DeleteFlag = true;
                                MA.LeaveDate = MA.UpdDate = DT;

                            }
                            else if (!MA.ActiveFlag)
                            {
                                MA.ActiveFlag = true;
                                MA.JoinDate = DT;
                                MA.LeaveDate = MA.CreDate;
                                MA.UpdDate = DT;
                                MA.SaveACID = ACID;
                            }
                            DC.SubmitChanges();
                        }
                    }
                    #region 旌旗角色更新
                    var MA2s = GetMOI2AC(0, ID);
                    foreach (var OI in cBUE.OIList)
                    {
                        if (OI.Selected)
                        {
                            if (!MA2s.Any(q => q.OIID.ToString() == OI.Value))
                            {
                                M_OI2_Account MA2 = new M_OI2_Account
                                {
                                    OIID = Convert.ToInt32(OI.Value),
                                    ACID = N.ACID,
                                    ActiveFlag = true,
                                    DeleteFlag = false,
                                    CreDate = DT,
                                    UpdDate = DT,
                                    SaveACID = ACID
                                };
                                DC.M_OI2_Account.InsertOnSubmit(MA2);
                            }
                        }
                        else if (!OI.Selected)
                        {
                            if (MA2s.Any(q => q.OIID.ToString() == OI.Value))
                            {
                                var MA2 = MA2s.FirstOrDefault(q => q.OIID.ToString() == OI.Value);
                                if (MA2 != null)
                                {
                                    MA2.DeleteFlag = true;
                                    MA2.UpdDate = DT;
                                    MA2.SaveACID = ACID;
                                }
                            }
                        }
                    }
                    DC.SubmitChanges();
                    #endregion
                }
                SetAlert((ID == 0 ? "新增" : "更新") + "完成", 1, "/Admin/SystemSet/BackUser_List");
            }

            return View(cBUE);
        }

        #endregion

        #region 選單管理-列表
        public string[] sMenuType = new string[] { "後台選單", "前台會員選單", "前台小組長選單", "前台義工團選單" };
        public class cMenu_List
        {
            public cTableList cTL = new cTableList();

            public int MenuType = 0;
            public int ActiveFlag = -1;

        }
        private cMenu_List GetMenu_List(FormCollection FC)
        {
            cMenu_List ML = new cMenu_List();
            ML.cTL = new cTableList();
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            ML.cTL.Title = "";
            ML.cTL.NowPage = iNowPage;
            ML.cTL.ItemID = "";
            ML.cTL.NowURL = "/Admin/SystemSet/Menu_List";
            ML.cTL.NumCut = iNumCut;
            ML.cTL.ShowFloor = false;
            ML.cTL.Rs = new List<cTableRow>();

            var Ns = DC.Menu.Where(q => !q.DeleteFlag);
            if (FC != null)
            {
                if (FC.Get("rbl_MenuType") == "0")
                {
                    ML.MenuType = 0;
                    Ns = Ns.Where(q => q.MenuType == 0);
                }
                else
                {
                    ML.MenuType = 1;
                    Ns = Ns.Where(q => q.MenuType != 0);
                }

                if (FC.Get("rbl_ActiveFlag") != "-1")
                {
                    ML.ActiveFlag = Convert.ToInt32(FC.Get("rbl_ActiveFlag"));
                    Ns = Ns.Where(q => q.ActiveFlag == (ML.ActiveFlag == 1));
                }

                foreach (var N in Ns)
                {
                    string s = FC.Get("txb_Sort_" + N.MID);
                    if (!string.IsNullOrEmpty(FC.Get("txb_Sort_" + N.MID)))
                    {
                        if (N.SortNo.ToString() != s)
                        {
                            try
                            {
                                N.SortNo = Convert.ToInt32(FC.Get("txb_Sort_" + N.MID));
                                N.UpdDate = DT;
                                N.SaveACID = ACID;
                                DC.SubmitChanges();
                            }
                            catch { }
                        }
                    }
                }
            }
            else
                Ns = Ns.Where(q => q.MenuType == ML.MenuType);
            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "控制", WidthPX = 250 });
            TopTitles.Add(new cTableCell { Title = "類別" });
            TopTitles.Add(new cTableCell { Title = "選單名稱" });
            TopTitles.Add(new cTableCell { Title = "對應網址" });
            TopTitles.Add(new cTableCell { Title = "排序", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "狀態" });

            ML.cTL.Rs.Add(SetTableRowTitle(TopTitles));

            ML.cTL.TotalCt = Ns.Count();
            ML.cTL.MaxNum = GetMaxNum(ML.cTL.TotalCt, ML.cTL.NumCut);

            foreach (var N1 in Ns.Where(q => q.ParentID == 0).OrderBy(q => q.SortNo))
            {
                cTableRow cTR = new cTableRow();
                cTR.ID = N1.MID;

                cTableCell TC = new cTableCell();
                TC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/SystemSet/Menu_Edit/" + N1.ParentID + "/" + N1.MID, Target = "_self", Value = "編輯" });
                TC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/SystemSet/Menu_Edit/" + N1.MID + "/0", Target = "_self", Value = "新增下層" });
                cTR.Cs.Add(TC);//

                cTR.Cs.Add(new cTableCell { Value = sMenuType[N1.MenuType] });//類別
                cTR.Cs.Add(new cTableCell { Value = N1.Title, CSS = "ms-3 float-start" });//選單名稱
                cTR.Cs.Add(new cTableCell { Value = N1.URL });//對應網址
                cTR.Cs.Add(new cTableCell { Type = "input-number", Value = N1.SortNo.ToString(), CSS = "bg_lemo", ControlName = "txb_Sort_" });//排序
                if (N1.ActiveFlag)
                    cTR.Cs.Add(new cTableCell { Value = "啟用", CSS = "btn btn-outline-success", Type = "activebutton", URL = "ChangeActive(this,'Menu'," + N1.MID + ")" });//狀態
                else
                    cTR.Cs.Add(new cTableCell { Value = "停用", CSS = "btn btn-outline-danger", Type = "activebutton", URL = "ChangeActive(this,'Menu'," + N1.MID + ")" });//狀態
                ML.cTL.Rs.Add(SetTableCellSortNo(cTR));
                foreach (var N2 in Ns.Where(q => q.ParentID == N1.MID).OrderBy(q => q.SortNo))
                {
                    cTR = new cTableRow();
                    cTR.ID = N2.MID;
                    cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/SystemSet/Menu_Edit/" + N2.ParentID + "/" + N2.MID, Target = "_self", Value = "編輯" });//
                    cTR.Cs.Add(new cTableCell { Value = sMenuType[N2.MenuType] });//類別
                    cTR.Cs.Add(new cTableCell { Value = "|--" + N2.Title, CSS = "ms-3 float-start" });//選單名稱
                    cTR.Cs.Add(new cTableCell { Value = N2.URL });//對應網址
                    cTR.Cs.Add(new cTableCell { Type = "input-number", Value = N2.SortNo.ToString(), ControlName = "txb_Sort_" });//排序
                    if (N2.ActiveFlag)
                        cTR.Cs.Add(new cTableCell { Value = "啟用", CSS = "btn btn-outline-success", Type = "activebutton", URL = "ChangeActive(this,'Menu'," + N2.MID + ")" });//狀態
                    else
                        cTR.Cs.Add(new cTableCell { Value = "停用", CSS = "btn btn-outline-danger", Type = "activebutton", URL = "ChangeActive(this,'Menu'," + N2.MID + ")" });//狀態
                    ML.cTL.Rs.Add(SetTableCellSortNo(cTR));
                }
            }
            return ML;
        }
        [HttpGet]
        public ActionResult Menu_List()
        {
            GetViewBag();
            return View(GetMenu_List(null));
        }
        [HttpPost]
        public ActionResult Menu_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetMenu_List(FC));
        }

        #endregion
        #region 選單管理-新增/修改/刪除
        public class cMenu_Edit
        {
            public Menu M = new Menu();
            public string PName = "";
            public string PMenuType = "";
            public List<SelectListItem> TypeList = new List<SelectListItem>();
        }
        private cMenu_Edit GetMenu_Edit(string ItemID, int ID, FormCollection FC)
        {
            cMenu_Edit N = new cMenu_Edit();
            #region 物件初始化
            int PID = 0;
            int.TryParse(ItemID, out PID);
            int iMenuType = 0;
            var PM = DC.Menu.FirstOrDefault(q => q.MID == PID);
            if (PM != null)
            {
                N.PName = PM.Title;
                N.PMenuType = sMenuType[PM.MenuType];
                iMenuType = PM.MenuType;
            }

            #endregion
            #region 資料帶入
            N.M = DC.Menu.FirstOrDefault(q => q.MID == ID && !q.DeleteFlag);
            if (N.M == null)
            {
                N.M = new Menu
                {
                    ParentID = PID,
                    MenuType = iMenuType,
                    Title = "",
                    URL = "",
                    ImgURL = "",
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
                N.M.Title = FC.Get("txb_Title");
                N.M.URL = FC.Get("txb_URL");
                N.M.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                N.M.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));

            }
            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult Menu_Edit(string ItemID, int ID)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            return View(GetMenu_Edit(ItemID, ID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Menu_Edit(string ItemID, int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            var N = GetMenu_Edit(ItemID, ID, FC);
            Error = "";
            if (N.M.Title == "")
                Error += "請輸入選單標題";
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (N.M.MID == 0)
                {
                    N.M.CreDate = N.M.UpdDate = DT;
                    N.M.SaveACID = ACID;
                    DC.Menu.InsertOnSubmit(N.M);
                }
                else
                {
                    N.M.UpdDate = DT;
                    N.M.SaveACID = ACID;
                }
                DC.SubmitChanges();

                SetAlert((ID == 0 ? "新增" : "更新") + "完成", 1, "/Admin/SystemSet/Menu_List");
            }

            return View(N);
        }


        #endregion

        #region 權限管理-列表
        public string[] sRoolType = new string[] { "前台基本功能", "前台會友功能", "前台牧養職分功能", "後台一般功能", "後台系統管理功能" };

        public class cRool_List
        {
            public cTableList cTL = new cTableList();

            public int RoolType = 0;
            public int ActiveFlag = -1;
            public List<SelectListItem> TypeList = new List<SelectListItem>();
        }
        private cRool_List GetRool_List(FormCollection FC)
        {
            cRool_List RL = new cRool_List();
            RL.cTL = new cTableList();
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            RL.cTL.Title = "";
            RL.cTL.NowPage = iNowPage;
            RL.cTL.ItemID = "";
            RL.cTL.NowURL = "/Admin/SystemSet/Rool_List";
            RL.cTL.NumCut = iNumCut;
            RL.cTL.ShowFloor = false;
            RL.cTL.Rs = new List<cTableRow>();

            RL.TypeList = new List<SelectListItem>();
            RL.TypeList.Add(new SelectListItem { Text = "請選擇", Value = "-1", Selected = true });
            for (int i = 3; i <= 4; i++)
                RL.TypeList.Add(new SelectListItem { Text = sRoolType[i], Value = i.ToString() });

            var Ns = DC.Rool.Where(q => !q.DeleteFlag);
            if (FC != null)
            {
                if (FC.Get("rbl_RoolType") == "-1")
                    Ns = Ns.Where(q => q.RoolType == 3 || q.RoolType == 4);
                else
                    Ns = Ns.Where(q => q.RoolType.ToString() == FC.Get("rbl_RoolType"));
                RL.TypeList.ForEach(q => q.Selected = false);
                RL.TypeList.First(q => q.Value == FC.Get("rbl_RoolType")).Selected = true;

                if (FC.Get("rbl_ActiveFlag") != "-1")
                {
                    RL.ActiveFlag = Convert.ToInt32(FC.Get("rbl_ActiveFlag"));
                    Ns = Ns.Where(q => q.ActiveFlag == (RL.ActiveFlag == 1));
                }
            }
            else
                Ns = Ns.Where(q => q.RoolType == 3 || q.RoolType == 4);
            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "控制", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "類別" });
            TopTitles.Add(new cTableCell { Title = "權限名稱" });
            TopTitles.Add(new cTableCell { Title = "狀態" });

            RL.cTL.Rs.Add(SetTableRowTitle(TopTitles));

            RL.cTL.TotalCt = Ns.Count();
            RL.cTL.MaxNum = GetMaxNum(RL.cTL.TotalCt, RL.cTL.NumCut);

            foreach (var N1 in Ns.OrderBy(q => q.RoolType).ThenByDescending(q => q.RID))
            {
                cTableRow cTR = new cTableRow();
                cTR.ID = N1.RID;
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/SystemSet/Rool_Edit/" + N1.RID, Target = "_self", Value = "編輯" });
                cTR.Cs.Add(new cTableCell { Value = sRoolType[N1.RoolType] });//類別
                cTR.Cs.Add(new cTableCell { Value = N1.Title });//權限名稱
                if (N1.ActiveFlag)
                    cTR.Cs.Add(new cTableCell { Value = "啟用", CSS = "btn btn-outline-success", Type = "activebutton", URL = "ChangeActive(this,'Rool'," + N1.RID + ")" });//狀態
                else
                    cTR.Cs.Add(new cTableCell { Value = "停用", CSS = "btn btn-outline-danger", Type = "activebutton", URL = "ChangeActive(this,'Rool'," + N1.RID + ")" });//狀態
                RL.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            return RL;
        }
        [HttpGet]
        public ActionResult Rool_List()
        {
            GetViewBag();
            return View(GetRool_List(null));
        }
        [HttpPost]
        public ActionResult Rool_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetRool_List(FC));
        }

        #endregion
        #region 權限管理-新增/修改/刪除
        public class cRool_Edit
        {
            public Rool R = new Rool();
            public List<SelectListItem> TypeList = new List<SelectListItem>();
            public List<cRoolMenu> MsnuList = new List<cRoolMenu>();
        }
        public class cRoolMenu
        {
            public string Title = "";
            public int MID = 0;
            public int PMID = 0;
            public int SortNo = 0;
            public bool AllFlag = false;
            public bool ShowFlag = false;
            public bool AddFlag = false;
            public bool EditFlag = false;
            public bool DeleteFlag = false;
            public bool PrintFlag = false;
            public bool UploadFlag = false;
        }
        private cRool_Edit GetRool_Edit(int ID, FormCollection FC)
        {
            cRool_Edit N = new cRool_Edit();
            #region 物件初始化
            for (int i = 3; i <= 4; i++)
                N.TypeList.Add(new SelectListItem { Text = sRoolType[i], Value = i.ToString() });
            N.TypeList[0].Selected = true;

            var Ms = DC.Menu.Where(q => !q.DeleteFlag && q.MenuType == 0).ToList();
            foreach (var M1 in Ms.Where(q => q.ParentID == 0).OrderBy(q => q.SortNo))
            {
                N.MsnuList.Add(new cRoolMenu { MID = M1.MID, PMID = M1.ParentID, SortNo = M1.SortNo, Title = M1.Title });
                foreach (var M2 in Ms.Where(q => q.ParentID == M1.MID).OrderBy(q => q.SortNo))
                {
                    N.MsnuList.Add(new cRoolMenu { MID = M2.MID, PMID = M2.ParentID, SortNo = M2.SortNo, Title = M2.Title });
                }
            }

            #endregion
            #region 資料帶入
            N.R = DC.Rool.FirstOrDefault(q => q.RID == ID && !q.DeleteFlag);
            if (N.R == null)
            {
                N.R = new Rool
                {
                    ParentID = 0,
                    OID = 0,
                    Title = "",
                    RoolType = 3,
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID
                };
            }
            else
            {
                var MRMs = DC.M_Rool_Menu.Where(q => q.RID == N.R.RID);
                foreach (var MRM in MRMs)
                {
                    var ML = N.MsnuList.FirstOrDefault(q => q.MID == MRM.MID);
                    if (ML != null)
                    {
                        ML.ShowFlag = MRM.ShowFlag;
                        ML.AddFlag = MRM.AddFlag;
                        ML.EditFlag = MRM.EditFlag;
                        ML.DeleteFlag = MRM.DeleteFlag;//刪除現在不用
                        ML.PrintFlag = MRM.PrintFlag;
                        ML.UploadFlag = MRM.UploadFlag;//上傳現在不用
                        ML.AllFlag = (MRM.ShowFlag && MRM.AddFlag && MRM.EditFlag && MRM.PrintFlag);
                    }
                }
            }
            if (N.R.RoolType == 3 || N.R.RoolType == 4)
            {
                N.TypeList.ForEach(q => q.Selected = false);
                N.TypeList.First(q => q.Value == N.R.RoolType.ToString()).Selected = true;
            }

            #endregion
            #region 前端資料帶入
            if (FC != null)
            {
                N.R.Title = FC.Get("txb_Title");
                N.R.RoolType = Convert.ToInt32(FC.Get("rbl_RoolType"));
                N.TypeList.ForEach(q => q.Selected = false);
                N.TypeList.First(q => q.Value == N.R.RoolType.ToString()).Selected = true;
                N.R.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                N.R.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));

                foreach (var ML in N.MsnuList)
                {
                    ML.ShowFlag = GetViewCheckBox(FC.Get("cbox_ShowFlag" + ML.MID));
                    ML.AddFlag = GetViewCheckBox(FC.Get("cbox_AddFlag" + ML.MID));
                    ML.EditFlag = GetViewCheckBox(FC.Get("cbox_EditFlag" + ML.MID));
                    ML.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag" + ML.MID));
                    ML.PrintFlag = GetViewCheckBox(FC.Get("cbox_PrintFlag" + ML.MID));
                    ML.UploadFlag = GetViewCheckBox(FC.Get("cbox_UploadFlag" + ML.MID));
                    ML.AllFlag = GetViewCheckBox(FC.Get("cbox_AllFlag" + ML.MID));
                }
            }
            #endregion

            return N;
        }
        [HttpGet]
        public ActionResult Rool_Edit(int ID)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            return View(GetRool_Edit(ID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Rool_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            var N = GetRool_Edit(ID, FC);
            Error = "";
            if (N.R.Title == "")
                Error += "請輸入權限標題";
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (N.R.RID == 0)
                {
                    N.R.CreDate = N.R.UpdDate = DT;
                    N.R.SaveACID = ACID;
                    DC.Rool.InsertOnSubmit(N.R);
                }
                else
                {
                    N.R.UpdDate = DT;
                    N.R.SaveACID = ACID;
                }
                DC.SubmitChanges();

                foreach (var ML in N.MsnuList)
                {
                    var MRM = DC.M_Rool_Menu.FirstOrDefault(q => q.RID == N.R.RID && q.MID == ML.MID);
                    if (MRM != null)
                    {
                        if (ML.AllFlag)
                        {
                            MRM.ShowFlag = true;
                            MRM.AddFlag = true;
                            MRM.EditFlag = true;
                            MRM.DeleteFlag = true;
                            MRM.PrintFlag = true;
                            MRM.UploadFlag = true;
                        }
                        else
                        {
                            MRM.ShowFlag = ML.ShowFlag;
                            MRM.AddFlag = ML.AddFlag;
                            MRM.EditFlag = ML.EditFlag;
                            MRM.DeleteFlag = ML.DeleteFlag;
                            MRM.PrintFlag = ML.PrintFlag;
                            MRM.UploadFlag = ML.UploadFlag;
                        }
                        DC.SubmitChanges();
                    }
                    else
                    {
                        MRM = new M_Rool_Menu
                        {
                            RID = N.R.RID,
                            MID = ML.MID,
                            ShowFlag = ML.ShowFlag,
                            AddFlag = ML.AddFlag,
                            EditFlag = ML.EditFlag,
                            DeleteFlag = ML.DeleteFlag,
                            PrintFlag = ML.PrintFlag,
                            UploadFlag = ML.UploadFlag,
                        };
                        DC.M_Rool_Menu.InsertOnSubmit(MRM);
                        DC.SubmitChanges();
                    }
                }

                SetAlert((ID == 0 ? "新增" : "更新") + "完成", 1, "/Admin/SystemSet/Rool_List");
            }

            return View(N);
        }


        #endregion

        #region API管理-列表

        public class cToken_List
        {
            public cTableList cTL = new cTableList();
            public ListSelect LS = new ListSelect();
            public int ActiveFlag = -1;
            public string sKey = "";
        }
        private cToken_List GetToken_List(FormCollection FC)
        {
            cToken_List c = new cToken_List();
            c.cTL = new cTableList();
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.cTL.Title = "";
            c.cTL.NowPage = iNowPage;
            c.cTL.ItemID = "";
            c.cTL.NowURL = "/Admin/SystemSet/Token_List";
            c.cTL.NumCut = iNumCut;
            c.cTL.ShowFloor = false;
            c.cTL.Rs = new List<cTableRow>();

            c.LS = new ListSelect();
            c.LS.ControlName = "ddl_CheckType";
            c.LS.ddlList = new List<SelectListItem>();
            c.LS.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            foreach (var cCT in cCheckType_List.OrderBy(q => q.SortNo))
                c.LS.ddlList.Add(new SelectListItem { Text = cCT.Title, Value = cCT.SortNo.ToString() });

            var Ns = DC.Token_Check.Where(q => !q.DeleteFlag);
            if (FC != null)
            {
                if (FC.Get("ddl_CheckType") != "0")
                {
                    Ns = Ns.Where(q => q.CheckType.Contains(FC.Get("ddl_CheckType") + ","));
                }

                if (FC.Get("rbl_ActiveFlag") != "-1")
                {
                    c.ActiveFlag = Convert.ToInt32(FC.Get("rbl_ActiveFlag"));
                    Ns = Ns.Where(q => q.ActiveFlag == (c.ActiveFlag == 1));
                }
            }

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "控制", WidthPX = 250 });
            TopTitles.Add(new cTableCell { Title = "標題" });
            TopTitles.Add(new cTableCell { Title = "使用期限" });
            TopTitles.Add(new cTableCell { Title = "可使用內容" });
            TopTitles.Add(new cTableCell { Title = "狀態" });

            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));

            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);

            foreach (var N in Ns.OrderBy(q => q.TCID))
            {
                cTableRow cTR = new cTableRow();
                cTR.ID = N.TCID;
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/SystemSet/Token_Edit/" + N.TCID, Target = "_self", Value = "編輯" });

                cTR.Cs.Add(new cTableCell { Value = N.Title });//標題
                cTR.Cs.Add(new cTableCell { Value = N.NoEndFlag ? "無限期" : N.S_DateTime.ToString(DateFormat) + "~" + N.E_DateTime.ToString(DateFormat) });//使用期限
                string[] sTypes = N.CheckType.Split(',');
                string sType = "";
                for (int i = 0; i < sTypes.Length; i++)
                {
                    int y = 0;
                    if (int.TryParse(sTypes[i], out y))
                    {
                        var cCT = cCheckType_List.FirstOrDefault(q => q.SortNo == y);
                        if (cCT != null)
                            sType += (sType == "" ? "" : "、") + cCT.Title;
                    }

                }
                cTR.Cs.Add(new cTableCell { Value = sType });//可使用內容

                if (N.ActiveFlag)
                    cTR.Cs.Add(new cTableCell { Value = "啟用", CSS = "btn btn-outline-success", Type = "activebutton", URL = "ChangeActive(this,'Token_Check'," + N.TCID + ")" });//狀態
                else
                    cTR.Cs.Add(new cTableCell { Value = "停用", CSS = "btn btn-outline-danger", Type = "activebutton", URL = "ChangeActive(this,'Token_Check'," + N.TCID + ")" });//狀態
                c.cTL.Rs.Add(SetTableCellSortNo(cTR));

            }
            return c;
        }
        [HttpGet]
        public ActionResult Token_List()
        {
            GetViewBag();
            return View(GetToken_List(null));
        }
        [HttpPost]
        public ActionResult Token_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetToken_List(FC));
        }

        #endregion
        #region API管理-新增/修改/刪除

        public class cToken_Edit
        {
            public Token_Check N = new Token_Check();

            public List<SelectListItem> TypeList = new List<SelectListItem>();
        }
        private cToken_Edit GetToken_Edit(int ID, FormCollection FC)
        {
            cToken_Edit c = new cToken_Edit();
            ACID = GetACID();
            #region 物件初始化
            foreach (cCheckType cT in cCheckType_List.OrderBy(q => q.SortNo))
                c.TypeList.Add(new SelectListItem { Text = cT.Title, Value = cT.SortNo.ToString() });
            #endregion
            #region 資料庫載入
            c.N = DC.Token_Check.FirstOrDefault(q => !q.DeleteFlag && q.TCID == ID);
            if (c.N == null)
            {
                c.N = new Token_Check
                {
                    TCID = 0,
                    Title = "",
                    Note = "",
                    S_DateTime = DT,
                    E_DateTime = DT.AddYears(1),
                    NoEndFlag = true,
                    CheckCode = "",
                    JWT = "",
                    CheckType = "",
                    Doman = "",
                    LoginBack = "",
                    ActiveFlag = true,
                    DeleteFlag = false,
                    CreDate = DT,
                    UpdDate = DT,
                    SaveACID = ACID
                };
            }
            else
            {
                string[] s = c.N.CheckType.Split(',');
                for (int i = 0; i < s.Length; i++)
                {
                    for (int j = 0; j < c.TypeList.Count; j++)
                    {
                        if (c.TypeList[j].Value == s[i])
                        {
                            c.TypeList[j].Selected = true;
                            break;
                        }
                    }
                }
            }
            #endregion
            #region 前端載入
            if (FC != null)
            {
                c.N.Title = FC.Get("txb_Title");
                c.N.NoEndFlag = GetViewCheckBox(FC.Get("cbox_NoEndFlag"));
                c.N.Doman = FC.Get("txb_Doman");
                c.N.LoginBack = FC.Get("txb_LoginBack");
                c.N.Note = FC.Get("txb_Note");

                DateTime DT_ = c.N.CreDate;
                if (DateTime.TryParse(FC.Get("txb_S_DateTime"), out DT_))
                    c.N.S_DateTime = DT_;

                if (c.N.NoEndFlag)
                    c.N.E_DateTime = c.N.CreDate;
                else
                {
                    DT_ = c.N.CreDate;
                    if (DateTime.TryParse(FC.Get("txb_E_DateTime"), out DT_))
                        c.N.E_DateTime = DT_;
                    else
                        c.N.E_DateTime = c.N.CreDate;
                }
                c.N.CheckType = "";
                for (int i = 0; i < c.TypeList.Count; i++)
                {
                    if (GetViewCheckBox(FC.Get("cbox_CheckType_" + c.TypeList[i].Value)))
                    {
                        c.TypeList[i].Selected = true;
                        c.N.CheckType += c.TypeList[i].Value + ",";
                    }
                    else
                        c.TypeList[i].Selected = false;
                }
            }
            #endregion
            #region 檢查
            Error = "";
            if (string.IsNullOrEmpty(c.N.Title))
                Error += "請填寫對象名稱<br/>";
            if (!c.N.NoEndFlag)
            {
                if (c.N.E_DateTime == c.N.CreDate)
                    Error += "請填期限結束日<br/>";
                if (c.N.S_DateTime > c.N.E_DateTime)
                    Error += "期限起始與結束日填寫錯誤<br/>";
            }
            #endregion
            return c;
        }
        [HttpGet]
        public ActionResult Token_Edit(int ID)
        {
            GetViewBag();
            return View(GetToken_Edit(ID, null));
        }
        [HttpPost]
        public ActionResult Token_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            var c = GetToken_Edit(ID, FC);
            if (Error != "")
                SetAlert(Error);
            else
            {

                if (c.N.TCID == 0)
                    DC.Token_Check.InsertOnSubmit(c.N);
                DC.SubmitChanges();
                if (ID == 0 || c.N.CheckCode == "")
                {
                    if (c.N.NoEndFlag)
                        c.N.CheckCode = "Banner_0_" + c.N.CreDate.AddYears(1).ToString("yyyyMMdd_hhmmssfff") + "_" + GetRand(1000000);
                    else
                        c.N.CheckCode = "Banner_1_" + c.N.E_DateTime.ToString("yyyyMMdd_hhmmssfff") + "_" + GetRand(1000000);
                    c.N.JWT = SetJWT(c.N.TCID, c.N.CheckCode);
                    DC.SubmitChanges();
                }
                SetAlert((ID == 0 ? "新增" : "更新") + "完成", 1, "/Admin/SystemSet/Token_List");
            }

            return View(c);
        }

        #endregion
    }
}