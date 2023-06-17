using Banner.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;

namespace Banner.Areas.Admin.Controllers
{
    public class OrganizeSetController : PublicClass
    {
        // GET: Admin/OrganizeSet
        public ActionResult Index()
        {
            return View();
        }

        #region 牧養組織與職分管理-列表
        public ActionResult Organize_Map_List(string ItemID = "Shepherding")
        {
            GetViewBag();
            return View(GetMapTable(ItemID));
        }
        //取得組織與職分的表單
        private cTableList GetMapTable(string ItemID)
        {
            cTableList cTL = new cTableList();
            cTL.ItemID = ItemID;
            cTL.Rs = new List<cTableRow>();
            var Os = DC.Organize.Where(q => !q.DeleteFlag && q.ItemID == ItemID).ToList();
            var O = Os.FirstOrDefault(q => q.ParentID == 0);
            int iSortNo = 0;
            if (O != null)
            {
                do
                {
                    cTableRow R = new cTableRow();
                    R.SortNo = iSortNo++;
                    R.Cs = new List<cTableCell>();

                    cTableCell C = new cTableCell();
                    C.Value = O.OID.ToString();
                    C.Title = O.Title;
                    C.ControlName = O.JobTitle;
                    C.URL = "/Admin/OrganizeSet/Organize_Map_Edit/" + ItemID + "/" + O.OID;
                    R.Cs.Add(C);

                    cTL.Rs.Add(R);

                    O = Os.FirstOrDefault(q => q.ParentID == O.OID);
                    if (O == null)
                        break;
                } while (true);
            }
            return cTL;
        }
        #endregion
        #region 牧養組織與職分管理-新增/編輯/刪除
        public class cOrganize_Map_Edit
        {
            public Organize O = new Organize();
            public List<SelectListItem> OList = new List<SelectListItem>();
            public int NewParentID = 0;
        }
        public ActionResult Organize_Map_Edit(string ItemID, int ID)
        {
            GetViewBag();
            return View(ReSetOrganize(ItemID, ID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Organize_Map_Edit(string ItemID, int ID, FormCollection FC)
        {
            GetViewBag();

            var cOE = ReSetOrganize(ItemID, ID, FC);
            if (cOE.O.Title == "")
                Error = "請輸入組織名稱";
            else if (cOE.O.DeleteFlag)//刪除
            {
                var OI = DC.OrganizeInfo.FirstOrDefault(q => !q.DeleteFlag && q.OID == cOE.O.OID);
                if (OI != null)
                    Error = "請先移除此組織等級的組織資料後再行刪除";
            }
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (cOE.O.ParentID > 0)
                {
                    //抽離的狀況
                    if (cOE.O.ParentID != cOE.NewParentID)
                    {
                        var OC = DC.Organize.FirstOrDefault(q => q.ParentID == cOE.O.OID && q.ItemID == ItemID && !q.DeleteFlag);
                        if (OC != null)
                        {
                            OC.ParentID = cOE.O.ParentID;
                            DC.SubmitChanges();

                            cOE.O.ParentID = cOE.NewParentID;
                        }
                    }
                }
                else
                {
                    cOE.O.ParentID = cOE.NewParentID;
                }
                cOE.O.UpdDate = DT;
                if (ID == 0)
                {
                    cOE.O.DeleteFlag = false;
                    cOE.O.CreDate = cOE.O.UpdDate;
                    DC.Organize.InsertOnSubmit(cOE.O);
                }
                DC.SubmitChanges();

                //檢查層級是否會發生衝突
                //發生於插入的狀況
                var OP = DC.Organize.FirstOrDefault(q => q.ParentID == cOE.NewParentID && q.ItemID == ItemID && q.OID != cOE.O.OID && !q.DeleteFlag);
                if (OP != null)
                {
                    OP.ParentID = cOE.O.OID;
                    DC.SubmitChanges();
                }

                SetAlert((ID == 0 ? "新增" : "更新") + "完成", 1, "/Admin/OrganizeSet/Organize_Map_List/" + ItemID + "/");
            }


            return View(cOE);
        }
        //初始化或取得組織資料
        private cOrganize_Map_Edit ReSetOrganize(string ItemID, int ID, FormCollection FC)
        {
            cOrganize_Map_Edit cME = new cOrganize_Map_Edit();
            if (ID > 0)
            {
                cME.O = DC.Organize.FirstOrDefault(q => q.ItemID == ItemID && q.OID == ID && !q.DeleteFlag);
                if (cME.O == null)
                    SetAlert("查無資料,請重新操作", 2, "/Admin/OrganizeSet/Organize_Map_List/" + ItemID + "/0");
                else
                {
                    cME.OList = new List<SelectListItem>();
                    var Os = DC.Organize.Where(q => !q.DeleteFlag && q.ItemID == ItemID).ToList();
                    var O = Os.FirstOrDefault(q => q.ParentID == 0);
                    if (O != null)
                    {
                        do
                        {
                            cME.OList.Add(new SelectListItem { Text = O.Title, Value = O.OID.ToString(), Selected = (cME.O.ParentID == O.OID), Disabled = (O.OID == ID) });
                            O = Os.FirstOrDefault(q => q.ParentID == O.OID);
                            if (O == null)
                                break;
                        } while (true);
                    }
                }
            }
            else
            {
                cME.O = new Organize() { ItemID = ItemID, ActiveFlag = true, DeleteFlag = false };
                cME.OList = new List<SelectListItem>();
                var Os = DC.Organize.Where(q => !q.DeleteFlag && q.ItemID == ItemID).ToList();
                var O = Os.FirstOrDefault(q => q.ParentID == 0);
                if (O != null)
                {
                    do
                    {
                        cME.OList.Add(new SelectListItem { Text = O.Title, Value = O.OID.ToString(), Selected = O.ParentID == 0 });
                        O = Os.FirstOrDefault(q => q.ParentID == O.OID);
                        if (O == null)
                            break;
                    } while (true);
                }
            }
            cME.O.SaveACID = GetACID();
            if (FC != null)
            {
                cME.O.Title = FC.Get("txb_Title");
                cME.O.JobTitle = FC.Get("txb_JobTitle");
                cME.NewParentID = Convert.ToInt32(FC.Get("ddl_Parent"));
                cME.O.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                cME.O.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
            }
            return cME;
        }
        #endregion
        #region 牧養組織與職分管理-匯出
        public ActionResult Organize_Map_Print(string ItemID)
        {
            GetViewBag();
            ArrayList AL = new ArrayList();
            ArrayList ALS = new ArrayList();
            ALS.Add("ID");
            ALS.Add("組織架構類別");
            ALS.Add("組織名稱");
            ALS.Add("職分名稱");
            ALS.Add("啟用狀態");
            ALS.Add("建立時間");
            ALS.Add("更新時間");
            ALS.Add("最後更新者");
            AL.Add((string[])ALS.ToArray(typeof(string)));
            var Os = DC.Organize.Where(q => !q.DeleteFlag && q.ItemID == ItemID).ToList();
            var ACs = (from q in DC.Account.Where(q => !q.DeleteFlag).ToList()
                       join p in Os.GroupBy(q => q.SaveACID)
                       on q.ACID equals p.Key
                       select q).ToList();

            var O = Os.FirstOrDefault(q => q.ParentID == 0);
            if (O != null)
            {
                do
                {
                    ALS = new ArrayList();
                    ALS.Add(O.OID.ToString());
                    ALS.Add(O.ItemID);
                    ALS.Add(O.Title);
                    ALS.Add(O.JobTitle);
                    ALS.Add(O.ActiveFlag ? "V" : "");
                    ALS.Add(O.CreDate.ToString(DateTimeFormat));
                    ALS.Add(O.CreDate == O.UpdDate ? "" : O.UpdDate.ToString(DateTimeFormat));
                    var AC = ACs.FirstOrDefault(q => q.ACID == O.SaveACID);
                    if (AC != null)
                        ALS.Add(AC.Name);
                    else
                        ALS.Add("--");
                    AL.Add((string[])ALS.ToArray(typeof(string)));

                    O = Os.FirstOrDefault(q => q.ParentID == O.OID);
                    if (O == null)
                        break;
                } while (true);
            }

            WriteExcelFromString("組織與職分管理列表", AL);
            SetAlert("已完成匯出", 1, "/Admin/OrganizeSet/Organize_Map_List/" + ItemID + "/0");
            return View();
        }
        #endregion

        #region 牧養組織與職分-列表
        public class cOrganize_Info_List
        {
            public string OTitle = "";
            public cTableRow cOrganize = new cTableRow();
            public cTableList cTL = new cTableList();
            public string sAddURL = "";
        }
        public ActionResult Organize_Info_List(string ItemID, int OID, int OIID)
        {
            GetViewBag();
            string sKey = GetQueryStringInString("Key");
            ViewBag._Key = sKey;

            int iNumCut = GetQueryStringInInt("NumCut");
            int iNowPage = GetQueryStringInInt("PageNo");
            if (iNumCut <= 0)
            {
                if (GetCookie("NumCut") == "")
                {
                    iNumCut = 10;
                    SetCookie("NumCut", iNumCut.ToString());
                }
                else
                    iNumCut = Convert.ToInt32(GetCookie("NumCut"));
            }
            else
                SetCookie("NumCut", iNumCut.ToString());
            if (iNowPage <= 0) iNowPage = 1;

            cOrganize_Info_List cOL = new cOrganize_Info_List();
            cOL.sAddURL = "/Admin/OrganizeSet/Organize_Info_Edit/" + ItemID + "/" + OID + "/" + OIID + "/0";
            cOL.cOrganize = new cTableRow();
            cOL.cTL = new cTableList();
            cOL.cTL.Title = "";
            cOL.cTL.NowPage = iNowPage;
            cOL.cTL.ItemID = ItemID;
            cOL.cTL.NowURL = "/Admin/OrganizeSet/Organize_Info_List/" + ItemID + "/" + OID + "/" + OIID + (sKey == "" ? "" : "?Key=" + sKey);
            cOL.cTL.NumCut = iNumCut;
            cOL.cTL.Rs = new List<cTableRow>();

            #region 上選單
            var Os = DC.Organize.Where(q => !q.DeleteFlag && q.ItemID == ItemID).ToList();
            var O = Os.FirstOrDefault(q => q.ParentID == 0);
            int iTab = 0;
            int iNowOSort = 0;
            if (O != null)
            {
                if (OID == 0)
                    OID = O.OID;
                do
                {
                    cOL.cOrganize.Cs.Add(new cTableCell
                    {
                        Value = O.OID.ToString(),
                        Title = O.Title,
                        SortNo = iTab++,
                        Target = "_self",
                        URL = "/Admin/OrganizeSet/Organize_Info_List/" + ItemID + "/" + O.OID + "/0",
                        CSS = (OID == O.OID ? "a_Select" : ""),
                        ControlName = (OID == O.OID ? "col_Select" : ""),
                    });
                    if (OID == O.OID)
                    {
                        cOL.OTitle = O.Title;
                        iNowOSort = iTab;
                    }

                    O = Os.FirstOrDefault(q => q.ParentID == O.OID);
                    if (O == null)
                        break;
                } while (true);
            }
            #endregion
            #region 搜尋內容
            var Ns = DC.OrganizeInfo.Where(q => q.OID == OID);
            if (OIID > 0)
                Ns = Ns.Where(q => q.ParentID == OIID);
            if (sKey != "")
                Ns = Ns.Where(q => q.Title.Contains(sKey));

            string sNextTitle = "下層組織";
            var _Last = cOL.cOrganize.Cs.OrderByDescending(q => q.Value).FirstOrDefault();
            if (_Last != null)
            {
                if (_Last.Value == OID.ToString())
                    sNextTitle = "組員數量";
            }
            cOL.cTL.Rs = new List<cTableRow>();
            cOL.cTL.Rs.Add(SetTableRowTitle(
                    new List<cTableCell>
                    {
                        new cTableCell { Title = "", WidthPX = 100 },
                        new cTableCell { Title = "編號(ID)", WidthPX = 100 },
                        new cTableCell { Title = "上層名稱" },
                        new cTableCell { Title = "本層名稱" },
                        new cTableCell { Title = sNextTitle, WidthPX = 120 },
                        new cTableCell { Title = "職分主管", WidthPX = 160 },
                        new cTableCell { Title = "狀態", WidthPX = 120 }
                    }
                ));



            cOL.cTL.TotalCt = Ns.Count();
            cOL.cTL.MaxNum = GetMaxNum(cOL.cTL.TotalCt, cOL.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.CreDate).Skip((iNowPage - 1) * cOL.cTL.NumCut).Take(cOL.cTL.NumCut);

            foreach (var N in Ns)
            {
                var NP = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == N.ParentID);
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/OrganizeSet/Organize_Info_Edit/" + ItemID + "/" + N.OID +"/"+ N.ParentID + "/" + N.OIID, Target = "_self", Value = "編輯" });//
                cTR.Cs.Add(new cTableCell { Value = N.OIID.ToString() });//編號(ID)
                cTR.Cs.Add(new cTableCell { Value = NP != null ? NP.Title : "" });//上層名稱
                cTR.Cs.Add(new cTableCell { Value = N.Title });//本層名稱
                if (sNextTitle == "下層組織")
                {
                    var O_Next = cOL.cOrganize.Cs.Where(q => q.SortNo >= iNowOSort).OrderBy(q => q.SortNo).FirstOrDefault();

                    int Ct = DC.OrganizeInfo.Count(q => !q.DeleteFlag && q.ParentID == N.OIID);
                    if (Ct == 0)//若還沒有下層,可直接新增
                        cTR.Cs.Add(new cTableCell { Type = "link", Target = "_self", CSS = "btn_Basic_W", URL = "/Admin/OrganizeSet/Organize_Info_Edit/" + ItemID + "/" + N.OID + "/" + N.OIID + "/0", Value = "新增" });
                    else//引導到下層
                        cTR.Cs.Add(new cTableCell { Type = "link", Target = "_self", CSS = "", URL = "/Admin/OrganizeSet/Organize_Info_List/" + ItemID + "/" + (O_Next != null ? O_Next.Value : "0") + "/" + N.OIID, Value = "(" + Ct + ")" });//下層組織
                }
                else
                {
                    int Ct = DC.M_Group_Account.Count(q => !q.DeleteFlag && q.Groups.OIID == N.OIID && (q.JoinDate >= q.CreDate && q.JoinDate.Date <= DT.Date) && (q.LeaveDate == q.CreDate || q.LeaveDate.Date >= DT.Date));
                    cTR.Cs.Add(new cTableCell { Type = "link", Target = "_self", CSS = "", URL = "/Admin/Account/Account_List/" + N.OID + "/" + N.OIID, Value = "(" + Ct + ")" });//組員數量
                }
                cTR.Cs.Add(new cTableCell { Value = N.Account.Name });//職分主管
                if (N.ActiveFlag)
                    cTR.Cs.Add(new cTableCell { Value = "啟用", CSS = "btn btn-outline-success" });//狀態
                else
                    cTR.Cs.Add(new cTableCell { Value = "啟用", CSS = "btn btn-outline-danger" });//狀態

                cOL.cTL.Rs.Add(cTR);
            }

            #endregion


            return View(cOL);
        }
        #endregion
        #region 牧養組織與職分-新增/編輯/刪除
        public class cOrganize_Info_Edit
        {
            public OrganizeInfo OI = new OrganizeInfo();
            public List<SelectListItem> ACList = new List<SelectListItem>();

            public List<ListSelect> OIParent = new List<ListSelect>();
        }
        public class ListSelect
        {
            public string ControlName = "";
            public int SortNo = 0;
            public List<SelectListItem> OIList = new List<SelectListItem>();
        }

        public ActionResult Organize_Info_Edit(string ItemID, int OID, int PID, int OIID)
        {
            GetViewBag();
            return View(ReSetOrganizeInfo(ItemID, OID, PID, OIID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Organize_Info_Edit(string ItemID, int OID, int PID, int OIID, FormCollection FC)
        {
            GetViewBag();
            cOrganize_Info_Edit cIE = ReSetOrganizeInfo(ItemID, OID, PID, OIID, FC);
            if (cIE.OI.Title == "")
                Error = "請輸入組織名稱";
            else if (cIE.OI.DeleteFlag)//刪除
            {
                var OI = DC.OrganizeInfo.FirstOrDefault(q => !q.DeleteFlag && q.ParentID == cIE.OI.OIID);
                if (OI != null)
                    Error = "請先移除此組織下的組織資料後再行刪除";
                else
                {
                    var G = DC.Groups.FirstOrDefault(q => q.OIID == cIE.OI.OIID);
                    if (G != null)
                        Error = "請先移除此組織下的小組名單後再行刪除";
                }
            }
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                cIE.OI.UpdDate = DT;
                if (OIID == 0)
                {
                    cIE.OI.DeleteFlag = false;
                    cIE.OI.CreDate = cIE.OI.UpdDate;
                    DC.OrganizeInfo.InsertOnSubmit(cIE.OI);
                }
                DC.SubmitChanges();

                SetAlert((OIID == 0 ? "新增" : "更新") + "完成", 1, "/Admin/OrganizeSet/Organize_Info_List/" + ItemID + "/" + OID + "/" + PID);
            }
            return View(cIE);
        }

        private cOrganize_Info_Edit ReSetOrganizeInfo(string ItemID, int OID, int PID, int OIID, FormCollection FC)
        {
            cOrganize_Info_Edit cIE = new cOrganize_Info_Edit();
            if (OIID > 0)
            {
                cIE.OI = DC.OrganizeInfo.FirstOrDefault(q => q.OID == OID && q.OIID == OIID && !q.DeleteFlag);
                if (cIE.OI == null)
                    SetAlert("查無資料,請重新操作", 2, "/Admin/OrganizeSet/Organize_Info_List/" + ItemID + "/" + OID + "/0");
                else
                {
                    cIE.ACList = new List<SelectListItem>();
                    var ACs = from q in DC.Account.Where(q => !q.DeleteFlag)
                              join p in (from q in DC.M_Group_Account.Where(q => !q.Groups.DeleteFlag && q.Groups.OIID == OIID && !q.DeleteFlag && (q.JoinDate >= q.CreDate && q.JoinDate.Date <= DT.Date) && (q.LeaveDate == q.CreDate || q.LeaveDate.Date >= DT.Date))
                                         group q by new { q.ACID } into g
                                         select new { g.Key.ACID }
                               )
                              on q.ACID equals p.ACID
                              select q;
                    foreach (var A in ACs.OrderBy(q => q.Name))
                        cIE.ACList.Add(new SelectListItem { Value = A.ACID.ToString(), Text = A.Name, Selected = A.ACID == cIE.OI.ACID });
                    if (cIE.ACList.Count > 0)
                    {
                        if (cIE.ACList.FirstOrDefault(q => q.Selected) == null)
                            cIE.ACList[0].Selected = true;
                    }
                    else
                    {
                        var A = DC.Account.FirstOrDefault(q => q.ACID == 1);
                        if (A != null)
                            cIE.ACList.Add(new SelectListItem { Value = A.ACID.ToString(), Text = A.Name, Selected = true });
                        else
                            cIE.ACList.Add(new SelectListItem { Value = "", Text = "此組織內無名單可以選擇", Selected = true });
                    }

                    cIE.OIParent = new List<ListSelect>();
                    int SortNo = 10;
                    var OIP = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == cIE.OI.ParentID && !q.DeleteFlag);
                    if (OIP != null)
                    {
                        int NowPID = OIP.ParentID;
                        var OIs = DC.OrganizeInfo.Where(q => q.ParentID == NowPID && !q.DeleteFlag);
                        do
                        {
                            if (OIs.Count() == 0 || SortNo<0 || NowPID<0)
                                break;
                            else
                            {
                                ListSelect cLS = new ListSelect();
                                cLS.ControlName = "ddl_OID_" + SortNo;
                                cLS.SortNo = SortNo;
                                foreach (var OI in OIs)
                                {
                                    if (OI.ParentID == NowPID)
                                    {
                                        OIP = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == NowPID && !q.DeleteFlag);
                                        if (OIP != null)
                                            NowPID = OIP.ParentID;
                                        else
                                            NowPID = -1;
                                        cLS.OIList.Add(new SelectListItem { Text = OI.Title, Value = OI.OIID.ToString(), Selected = true });
                                    }
                                    else
                                        cLS.OIList.Add(new SelectListItem { Text = OI.Title, Value = OI.OIID.ToString() });
                                }
                                cIE.OIParent.Add(cLS);
                                SortNo--;
                                OIs = DC.OrganizeInfo.Where(q => q.ParentID == NowPID && !q.DeleteFlag);
                            }
                        } while (true);
                    }
                }
            }
            else
            {
                var OI = DC.Organize.FirstOrDefault(q => q.OID == OID && q.ItemID == ItemID && !q.DeleteFlag);
                if (OI == null)
                    SetAlert("查無資料,請重新操作", 2, "/Admin/OrganizeSet/Organize_Map_List/" + ItemID + "/0");
                else
                {
                    cIE.OI = new OrganizeInfo();
                    cIE.OI.OID = OID;
                    cIE.OI.ParentID = PID;
                    cIE.ACList = new List<SelectListItem>();
                    var ACs = DC.Account.Where(q => !q.DeleteFlag);
                    foreach (var A in ACs.OrderBy(q => q.Name))
                        cIE.ACList.Add(new SelectListItem { Value = A.ACID.ToString(), Text = A.Name });
                    cIE.ACList[0].Selected = true;
                }

            }
            cIE.OI.SaveACID = GetACID();
            if (FC != null)
            {
                cIE.OI.Title = FC.Get("txb_Title");
                cIE.OI.ACID = Convert.ToInt32(FC.Get("ddl_ACID"));
                cIE.OI.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                cIE.OI.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
            }

            return cIE;
        }
        #endregion
    }
}