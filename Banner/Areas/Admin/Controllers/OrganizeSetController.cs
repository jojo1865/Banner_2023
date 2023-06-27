using Banner.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Net.Mime;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
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
            ChangeTitle(ID == 0);
            return View(ReSetOrganize(ItemID, ID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Organize_Map_Edit(string ItemID, int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
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
                    ALS.Add(O.ActiveFlag ? "啟用中" : "");
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
            string ParentTitle = "上層名稱";
            string NowTitle = "本層名稱";
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
                        NowTitle = O.Title;
                        var PO = DC.Organize.FirstOrDefault(q => q.OID == O.ParentID);
                        if (PO != null)
                            ParentTitle = PO.Title;
                        else
                            ParentTitle = "";
                        var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == OIID);
                        if (OI != null)
                            cOL.OTitle = OI.Title;
                        else
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

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "編號(ID)", WidthPX = 100 });
            if (ParentTitle != "")
                TopTitles.Add(new cTableCell { Title = ParentTitle });
            TopTitles.Add(new cTableCell { Title = NowTitle });
            TopTitles.Add(new cTableCell { Title = sNextTitle, WidthPX = 120 });
            TopTitles.Add(new cTableCell { Title = "職分主管", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "狀態", WidthPX = 120 });

            cOL.cTL.Rs.Add(SetTableRowTitle(TopTitles));

            cOL.cTL.TotalCt = Ns.Count();
            cOL.cTL.MaxNum = GetMaxNum(cOL.cTL.TotalCt, cOL.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.CreDate).Skip((iNowPage - 1) * cOL.cTL.NumCut).Take(cOL.cTL.NumCut);

            foreach (var N in Ns)
            {
                var NP = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == N.ParentID);
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/OrganizeSet/Organize_Info_Edit/" + ItemID + "/" + N.OID + "/" + N.ParentID + "/" + N.OIID, Target = "_self", Value = "編輯" });//
                cTR.Cs.Add(new cTableCell { Value = N.OIID.ToString() });//編號(ID)
                if (ParentTitle != "")
                    cTR.Cs.Add(new cTableCell { Value = NP != null ? NP.Title : "" });//上層名稱
                cTR.Cs.Add(new cTableCell { Value = N.Title + (N.BusinessType == 1 ? "(外展)" : "") });//本層名稱
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
                    int Ct = DC.M_OI_Account.Count(q => !q.DeleteFlag && q.OIID == N.OIID && (q.JoinDate >= q.CreDate && q.JoinDate.Date <= DT.Date) && (q.LeaveDate == q.CreDate || q.LeaveDate.Date >= DT.Date));
                    if (Ct == 0)//沒有新成員
                        cTR.Cs.Add(new cTableCell { Value = "" });//等待分發名單中
                    else
                        cTR.Cs.Add(new cTableCell { Type = "link", Target = "_black", CSS = "", URL = "/Admin/OrganizeSet/Organize_Info_Account_List/" + ItemID + "/" + N.OID + "/" + N.OIID, Value = "(" + Ct + ")" });//組員數量
                }
                if (OID < 3)
                    cTR.Cs.Add(new cTableCell { Value = "" });//職分主管
                else
                    cTR.Cs.Add(new cTableCell { Value = N.Account.Name });//職分主管
                if (N.ActiveFlag)
                    cTR.Cs.Add(new cTableCell { Value = "啟用", CSS = "btn btn-outline-success" });//狀態
                else
                    cTR.Cs.Add(new cTableCell { Value = "啟用", CSS = "btn btn-outline-danger" });//狀態

                cOL.cTL.Rs.Add(SetTableCellSortNo(cTR));
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
            public Contect C = new Contect();
            public Location L = new Location();

            public List<SelectListItem> Z1List = new List<SelectListItem>();
            public List<SelectListItem> Z2List = new List<SelectListItem>();
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
            ChangeTitle(OIID == 0);
            return View(ReSetOrganizeInfo(ItemID, OID, PID, OIID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Organize_Info_Edit(string ItemID, int OID, int PID, int OIID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(OIID == 0);
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
                    var G = DC.M_OI_Account.FirstOrDefault(q => q.OIID == cIE.OI.OIID);
                    if (G != null)
                        Error = "請先移除此組織下的名單後再行刪除";
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

                cIE.C.TargetType = 1;
                cIE.C.TargetID = cIE.OI.OIID;
                cIE.C.ContectType = 0;
                if (cIE.C.CID == 0)
                    DC.Contect.InsertOnSubmit(cIE.C);
                DC.SubmitChanges();

                cIE.L.TargetType = 1;
                cIE.L.TargetID = cIE.OI.OIID;
                cIE.L.ZID = Convert.ToInt32(cIE.Z2List.First(q => q.Selected).Value);
                if (cIE.L.LID == 0)
                    DC.Location.InsertOnSubmit(cIE.L);
                DC.SubmitChanges();

                PID = cIE.OI.ParentID;
                SetAlert((OIID == 0 ? "新增" : "更新") + "完成", 1, "/Admin/OrganizeSet/Organize_Info_List/" + ItemID + "/" + OID + "/" + PID);
            }
            return View(cIE);
        }

        private cOrganize_Info_Edit ReSetOrganizeInfo(string ItemID, int OID, int PID, int OIID, FormCollection FC)
        {
            var OIs__ = new List<OrganizeInfo>();
            GetThisOIsFromTree(ref OIs__, OIID);

            var ACs = from q in DC.Account.Where(q => !q.DeleteFlag).ToList()
                      join p in OIs__.GroupBy(q=>q.ACID)
                      on q.ACID equals p.Key
                      select q;
              cOrganize_Info_Edit cIE = new cOrganize_Info_Edit();
            if (OIID > 0)//更新
            {
                cIE.OI = DC.OrganizeInfo.FirstOrDefault(q => q.OID == OID && q.OIID == OIID && !q.DeleteFlag);
                if (cIE.OI == null)
                    SetAlert("查無資料,請重新操作", 2, "/Admin/OrganizeSet/Organize_Info_List/" + ItemID + "/" + OID + "/0");
                else
                {
                    #region 主管列表
                    cIE.ACList = new List<SelectListItem>();
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
                    #endregion
                    #region 上層
                    cIE.OIParent = new List<ListSelect>();
                    int SortNo = 10;
                    var OIP = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == cIE.OI.ParentID && !q.DeleteFlag);
                    if (OIP != null)
                    {
                        int NowPID = OIP.ParentID;
                        var OIs = DC.OrganizeInfo.Where(q => q.ParentID == NowPID && !q.DeleteFlag);
                        do
                        {
                            if (OIs.Count() == 0 || SortNo < 0 || NowPID < 0)
                                break;
                            else
                            {
                                ListSelect cLS = new ListSelect();
                                cLS.ControlName = "ddl_OIID_" + SortNo;
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


                                if (SortNo == 10)
                                {
                                    cLS.OIList.ForEach(q => q.Selected = false);
                                    cLS.OIList.Find(q => q.Value == cIE.OI.ParentID.ToString()).Selected = true;
                                }

                                cIE.OIParent.Add(cLS);
                                SortNo--;
                                OIs = DC.OrganizeInfo.Where(q => q.ParentID == NowPID && !q.DeleteFlag);
                            }
                        } while (true);
                    }
                    #endregion
                    #region 地址
                    var L = DC.Location.FirstOrDefault(q => q.TargetType == 1 && q.TargetID == OIID);
                    if (L != null)
                    {
                        cIE.L = L;
                        var Z1s = DC.ZipCode.Where(q => q.ParentID == 10 && q.ActiveFlag).OrderBy(q => q.Title).ToList();
                        for (int i = 0; i < Z1s.Count; i++)
                        {
                            cIE.Z1List.Add(new SelectListItem { Text = Z1s[i].Title, Value = Z1s[i].ZID.ToString(), Selected = Z1s[i].ZID == L.ZipCode.ParentID });

                            var Z2s = DC.ZipCode.Where(q => q.ParentID == L.ZipCode.ParentID && q.ActiveFlag).OrderBy(q => q.Code).ToList();
                            for (int j = 0; j < Z2s.Count; j++)
                                cIE.Z2List.Add(new SelectListItem { Text = Z2s[j].Code + " " + Z2s[j].Title, Value = Z2s[j].ZID.ToString(), Selected = Z2s[j].ZID == L.ZID });
                        }
                    }
                    else
                    {
                        var Z1s = DC.ZipCode.Where(q => q.ParentID == 10 && q.ActiveFlag).OrderBy(q => q.Title).ToList();
                        for (int i = 0; i < Z1s.Count; i++)
                        {
                            cIE.Z1List.Add(new SelectListItem { Text = Z1s[i].Title, Value = Z1s[i].ZID.ToString(), Selected = i == 0 });
                            if (i == 0)
                            {
                                var Z2s = DC.ZipCode.Where(q => q.ParentID == Z1s[i].ZID && q.ActiveFlag).OrderBy(q => q.Code).ToList();
                                for (int j = 0; j < Z2s.Count; j++)
                                    cIE.Z2List.Add(new SelectListItem { Text = Z2s[j].Code + " " + Z2s[j].Title, Value = Z2s[j].ZID.ToString(), Selected = j == 0 });
                            }
                        }
                    }
                    #endregion
                    #region 電話
                    var C = DC.Contect.FirstOrDefault(q => q.TargetType == 1 && q.TargetID == OIID);
                    if (C != null)
                        cIE.C = C;
                    #endregion
                }
            }
            else//新增
            {
                var O = DC.Organize.FirstOrDefault(q => q.OID == OID && q.ItemID == ItemID && !q.DeleteFlag);
                if (O == null)
                    SetAlert("查無資料,請重新操作", 2, "/Admin/OrganizeSet/Organize_Map_List/" + ItemID + "/0");
                else
                {
                    cIE.OI = new OrganizeInfo();
                    cIE.OI.Organize = DC.Organize.FirstOrDefault(q => q.OID == OID);
                    cIE.OI.ParentID = PID;
                    #region 主管
                    cIE.ACList = new List<SelectListItem>();
                    foreach (var A in ACs.OrderBy(q => q.Name))
                        cIE.ACList.Add(new SelectListItem { Value = A.ACID.ToString(), Text = A.Name });
                    cIE.ACList[0].Selected = true;
                    #endregion
                    #region 地址
                    var Z1s = DC.ZipCode.Where(q => q.ParentID == 10 && q.ActiveFlag).OrderBy(q => q.Title).ToList();
                    for (int i = 0; i < Z1s.Count; i++)
                    {
                        cIE.Z1List.Add(new SelectListItem { Text = Z1s[i].Title, Value = Z1s[i].ZID.ToString(), Selected = (i == 0) });
                        if (i == 0)
                        {
                            var Z2s = DC.ZipCode.Where(q => q.ParentID == Z1s[i].ZID && q.ActiveFlag).OrderBy(q => q.Code).ToList();
                            for (int j = 0; j < Z2s.Count; j++)
                                cIE.Z2List.Add(new SelectListItem { Text = Z2s[j].Code + " " + Z2s[j].Title, Value = Z2s[j].ZID.ToString(), Selected = (j == 0) });
                            cIE.L = new Location { ZID = Z2s[0].ZID };
                        }
                    }
                    #endregion
                }
            }
            cIE.OI.SaveACID = GetACID();
            if (FC != null)
            {
                cIE.OI.Title = FC.Get("txb_Title");
                cIE.OI.ACID = Convert.ToInt32(FC.Get("ddl_ACID"));
                cIE.OI.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                cIE.OI.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                cIE.OI.ParentID = Convert.ToInt32(FC.Get("ddl_OIID_10"));
                cIE.C.ContectValue = FC.Get("txb_Phone");
                cIE.L.Address = FC.Get("txb_Address");
                if (FC.Get("ddl_Zip1") != null)
                {
                    cIE.Z2List = new List<SelectListItem>();
                    int Z1ID = Convert.ToInt32(FC.Get("ddl_Zip1"));
                    cIE.Z1List.ForEach(q => q.Selected = false);
                    cIE.Z1List.First(q => q.Value == Z1ID.ToString()).Selected = true;
                    var Z2s = DC.ZipCode.Where(q => q.ParentID == Z1ID && q.ActiveFlag).OrderBy(q => q.Code).ToList();
                    for (int j = 0; j < Z2s.Count; j++)
                    {
                        if (FC.Get("ddl_Zip2") == Z2s[j].ZID.ToString())
                        {
                            cIE.Z2List.Add(new SelectListItem { Text = Z2s[j].Code + " " + Z2s[j].Title, Value = Z2s[j].ZID.ToString(), Selected = true });
                            cIE.L.ZipCode = Z2s[j];
                        }
                        else
                            cIE.Z2List.Add(new SelectListItem { Text = Z2s[j].Code + " " + Z2s[j].Title, Value = Z2s[j].ZID.ToString() });
                    }
                }
            }

            return cIE;
        }

        
        #endregion
        #region 牧養組織與職分-成員列表
        public ActionResult Organize_Info_Account_List(string ItemID, int OID, int OIID)
        {
            GetViewBag();
            cTableList cTL = new cTableList();
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
            cTL = new cTableList();
            cTL.Title = "";
            cTL.NowPage = iNowPage;
            cTL.ItemID = ItemID;
            cTL.NowURL = "/Admin/OrganizeSet/Organize_Info_Account_List/" + ItemID + "/" + OID + "/" + OIID;
            cTL.NumCut = iNumCut;
            cTL.Rs = new List<cTableRow>();

            int LeaderACID = 0;
            var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == OIID && q.OID == OID && !q.DeleteFlag);
            if (OI != null)
            {
                ViewBag._Title = OI.Title + (OI.BusinessType == 1 ? "(外展)" : "") + " 成員列表";
                LeaderACID = OI.ACID;
            }
            var Ns = DC.M_OI_Account.Where(q => !q.DeleteFlag && q.OIID == OIID && !q.Account.DeleteFlag);

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "會員資料", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "編號(ID)", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "會員ID", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "姓名" });
            TopTitles.Add(new cTableCell { Title = "加入日期" });
            TopTitles.Add(new cTableCell { Title = "離開日期" });
            TopTitles.Add(new cTableCell { Title = "職務" });
            TopTitles.Add(new cTableCell { Title = "受洗狀態" });

            cTL.Rs.Add(SetTableRowTitle(TopTitles));

            cTL.TotalCt = Ns.Count();
            cTL.MaxNum = GetMaxNum(cTL.TotalCt, cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.OrganizeInfo.ACID == q.ACID).ThenByDescending(q => q.MOIAID).Skip((iNowPage - 1) * cTL.NumCut).Take(cTL.NumCut);
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/Account/Account_Edit/" + N.ACID, Target = "_black", Value = "編輯" });//
                cTR.Cs.Add(new cTableCell { Value = N.MOIAID.ToString() });//編號(ID)
                cTR.Cs.Add(new cTableCell { Value = N.ACID.ToString() });//會員ID
                cTR.Cs.Add(new cTableCell { Value = N.Account.Name });//姓名
                if (N.JoinDate == N.CreDate)
                {
                    cTR.Cs.Add(new cTableCell { Value = "未落戶" });//加入日期
                    cTR.Cs.Add(new cTableCell { Value = "" });//離開日期
                }
                else
                {
                    cTR.Cs.Add(new cTableCell { Value = N.JoinDate.ToShortDateString() });//加入日期
                    cTR.Cs.Add(new cTableCell { Value = N.LeaveDate == N.CreDate ? "" : N.LeaveDate.ToShortDateString() });//離開日期
                }
                if (N.OrganizeInfo.ACID == N.ACID)
                    cTR.Cs.Add(new cTableCell { Value = "小組長", CSS = "btn btn-outline-success" });//職務
                else
                    cTR.Cs.Add(new cTableCell { Value = "" });//職務

                var B = DC.Baptized.Where(q => q.ACID == N.ACID && !q.DeleteFlag).OrderByDescending(q => q.BaptismDate).FirstOrDefault();
                if (B == null)
                    cTR.Cs.Add(new cTableCell { Value = "未受洗" });//受洗狀態
                else if (B.ImplementFlag)
                    cTR.Cs.Add(new cTableCell { Value = "已於" + B.BaptismDate.ToLongDateString() + "受洗" });//受洗狀態
                else
                    cTR.Cs.Add(new cTableCell { Value = "預計於" + B.BaptismDate.ToLongDateString() + "受洗" });//受洗狀態
                cTL.Rs.Add(SetTableCellSortNo(cTR));
            }

            return View(cTL);
        }

        #endregion

        #region 聚會地點-列表
        public class cMLL
        {
            public string ThisTitle = "";
            public string ControlName = "";
            public int OIID = 0;
            public List<SelectListItem> OIList = new List<SelectListItem>();
            public string TreeJson = "";
        }
        public class cTree
        {
            public string name = "";
            public string title = "";
            public List<cTree> children = new List<cTree>();
        }
        /// <summary>
        /// 聚會點編輯/新增
        /// </summary>
        /// <param name="OIID">目前所在跟目錄的OIID</param>
        /// <param name="ID">無用</param>
        /// <returns></returns>
        public ActionResult Meeting_Location_List(int ItemID, int ID)
        {
            GetViewBag();
            cMLL MLL = new cMLL();
            MLL.ThisTitle = "";//隸屬組織
            MLL.ControlName = "ddl_OI2";
            MLL.OIList = new List<SelectListItem>();
            MLL.OIList.Add(new SelectListItem { Text = "全部選擇", Value = "0", Selected = ItemID == 0 });
            var POIs = DC.OrganizeInfo.Where(q => q.OID == 1 && !q.DeleteFlag).OrderBy(q => q.OIID);
            foreach (var OI in POIs)
                MLL.OIList.Add(new SelectListItem { Text = OI.Title, Value = OI.OIID.ToString(), Selected = OI.OIID == ItemID });

            cTree T0 = new cTree();
            T0.name = "主日聚會點";
            T0.title = "";
            T0.children = new List<cTree>();

            if (ItemID > 0)
                POIs = POIs.Where(q => q.OIID == ItemID).OrderBy(q => q.OIID);

            foreach (var POI in POIs)
            {
                MLL.ThisTitle = POI.Title;
                cTree T1 = new cTree();
                T1.name = POI.Organize.Title;
                T1.title = POI.Title.Replace("基督教", "</br>基督教");
                T1.children = new List<cTree>();
                var OIs = DC.OrganizeInfo.Where(q => q.OID == 2 && !q.DeleteFlag && q.ParentID == POI.OIID).OrderBy(q => q.OIID);
                foreach (var OI in OIs)
                {
                    var MLSs = DC.M_Location_Set.Where(q => q.SetType == 0 && q.TargetID == OI.OIID && !q.DeleteFlag && !q.Meeting_Location.DeleteFlag).Select(q => q.Meeting_Location).OrderBy(q => q.MLID);
                    cTree T2 = new cTree();
                    T2.name = OI.Organize.Title;
                    T2.title = OI.Title + " (" + MLSs.Count() + ")</br><a href='/Admin/OrganizeSet/Meeting_Location_Edit/" + OI.OIID + "/0' class='btn_Basic btn btn-primary btn-round'>新增</a>";
                    T2.children = new List<cTree>();
                    foreach (var MLS in MLSs)
                    {
                        T2.children.Add(new cTree
                        {
                            name = "主日聚會點",
                            title = MLS.Title + "</br><a href='/Admin/OrganizeSet/Meeting_Location_Edit/" + OI.OIID + "/" + MLS.MLID + "'  class='btn_Basic btn btn-primary btn-round'>編輯</a>"
                        });
                    }

                    T1.children.Add(T2);
                }

                T0.children.Add(T1);
            }
            if (POIs.Count() == 1)
            {
                MLL.TreeJson = JsonConvert.SerializeObject(T0.children[0]);
            }
            else
            {
                MLL.ThisTitle = "全部主日聚會點";
                MLL.TreeJson = JsonConvert.SerializeObject(T0);
            }
            return View(MLL);
        }

        #endregion
        #region 聚會地點-新增/編輯/刪除
        public class cMeeting_Location_Edit
        {
            public Meeting_Location cML = new Meeting_Location();
            public M_Location_Set cMLS = new M_Location_Set();
            public List<ListSelect> OIParent = new List<ListSelect>();
            public Contect C = new Contect();
            public Location L = new Location();
            public int OIID = 0;
            public List<SelectListItem> Z1List = new List<SelectListItem>();
            public List<SelectListItem> Z2List = new List<SelectListItem>();

            public List<SelectListItem> SH_List = new List<SelectListItem>();
            public List<SelectListItem> SM_List = new List<SelectListItem>();
            public List<SelectListItem> EH_List = new List<SelectListItem>();
            public List<SelectListItem> EM_List = new List<SelectListItem>();
            public string SH_ControlName = "ddl_SH", SM_ControlName = "ddl_SM", EH_ControlName = "ddl_EH", EM_ControlName = "ddl_EM";
        }

        public ActionResult Meeting_Location_Edit(int ItemID, int ID)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            return View(ReSetMeetingLocationInfo(ItemID, ID, null));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Meeting_Location_Edit(int ItemID, int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            cMeeting_Location_Edit cMLE = ReSetMeetingLocationInfo(ItemID, ID, FC);
            if (cMLE.cML.Title == "")
                Error = "請輸入聚會點名稱";
            else if (cMLE.cML.DeleteFlag)//刪除
            {
                var MLS = DC.M_Location_Set.FirstOrDefault(q => !q.DeleteFlag && q.SetType == 0 && q.TargetID == cMLE.OIID);
                if (MLS != null)
                    Error = "請先移除使用此聚會點的組織資料後再行刪除";
            }
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                cMLE.cML.UpdDate = DT;
                if (ID == 0)
                {
                    cMLE.cML.Code = "";
                    cMLE.cML.DeleteFlag = false;
                    cMLE.cML.CreDate = cMLE.cML.UpdDate;
                    DC.Meeting_Location.InsertOnSubmit(cMLE.cML);
                }
                DC.SubmitChanges();

                cMLE.cMLS.UpdDate = DT;
                if (ID == 0)
                {
                    cMLE.cMLS.MLID = cMLE.cML.MLID;
                    cMLE.cMLS.DeleteFlag = false;
                    cMLE.cMLS.CreDate = cMLE.cML.UpdDate;
                    DC.M_Location_Set.InsertOnSubmit(cMLE.cMLS);
                }
                DC.SubmitChanges();


                cMLE.C.TargetType = 3;
                cMLE.C.TargetID = cMLE.cML.MLID;
                cMLE.C.ContectType = 0;
                if (cMLE.C.CID == 0)
                    DC.Contect.InsertOnSubmit(cMLE.C);
                DC.SubmitChanges();

                cMLE.L.TargetType = 3;
                cMLE.L.TargetID = cMLE.cML.MLID;
                cMLE.L.ZID = Convert.ToInt32(cMLE.Z2List.First(q => q.Selected).Value);
                if (cMLE.L.LID == 0)
                    DC.Location.InsertOnSubmit(cMLE.L);
                DC.SubmitChanges();


                SetAlert((ID == 0 ? "新增" : "更新") + "完成", 1, "/Admin/OrganizeSet/Meeting_Location_List/" + cMLE.OIID + "/0");
            }

            return View(cMLE);
        }
        private cMeeting_Location_Edit ReSetMeetingLocationInfo(int OIID, int ID, FormCollection FC)
        {
            cMeeting_Location_Edit cMLE = new cMeeting_Location_Edit();
            var POI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == OIID);
            if (POI != null)
                cMLE.OIID = POI.ParentID;
            else
                cMLE.OIID = 0;
            if (ID > 0)//更新
            {
                cMLE.cML = DC.Meeting_Location.FirstOrDefault(q => q.MLID == ID && !q.DeleteFlag);
                if (cMLE.cML == null)
                    SetAlert("查無資料,請重新操作", 2, "/Admin/OrganizeSet/Meeting_Location_List/" + OIID + "/" + ID);
                else
                {
                    #region 聚會點使用對象
                    cMLE.cMLS = DC.M_Location_Set.FirstOrDefault(q => q.SetType == 0 && q.TargetID == cMLE.cML.MLID);
                    if (cMLE.cMLS == null)
                    {
                        cMLE.cMLS = new M_Location_Set
                        {
                            MLID = cMLE.cML.MLID,
                            SetType = 0,
                            TargetID = OIID,
                            S_hour = 9,
                            S_minute = 0,
                            E_hour = 12,
                            E_minute = 0,
                            ActiveFlag = true,
                            DeleteFlag = false,
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = GetACID()
                        };
                    }
                    #endregion
                    #region 上層
                    cMLE.OIParent = new List<ListSelect>();
                    int SortNo = 10;
                    var OIP = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == OIID && !q.DeleteFlag);
                    if (OIP != null)
                    {
                        int NowPID = OIP.ParentID;
                        var OIs = DC.OrganizeInfo.Where(q => q.ParentID == NowPID && !q.DeleteFlag);
                        do
                        {
                            if (OIs.Count() == 0 || SortNo < 0 || NowPID < 0)
                                break;
                            else
                            {
                                ListSelect cLS = new ListSelect();
                                cLS.ControlName = "ddl_OIID_" + SortNo;
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


                                if (SortNo == 10)
                                {
                                    cLS.OIList.ForEach(q => q.Selected = false);
                                    cLS.OIList.Find(q => q.Value == OIID.ToString()).Selected = true;
                                }

                                cMLE.OIParent.Add(cLS);
                                SortNo--;
                                OIs = DC.OrganizeInfo.Where(q => q.ParentID == NowPID && !q.DeleteFlag);
                            }
                        } while (true);
                    }
                    #endregion
                    #region 地址
                    var L = DC.Location.FirstOrDefault(q => q.TargetType == 3 && q.TargetID == ID);
                    if (L != null)
                    {
                        cMLE.L = L;
                        var Z1s = DC.ZipCode.Where(q => q.ParentID == 10 && q.ActiveFlag).OrderBy(q => q.Title).ToList();
                        for (int i = 0; i < Z1s.Count; i++)
                        {
                            cMLE.Z1List.Add(new SelectListItem { Text = Z1s[i].Title, Value = Z1s[i].ZID.ToString(), Selected = Z1s[i].ZID == L.ZipCode.ParentID });

                            var Z2s = DC.ZipCode.Where(q => q.ParentID == L.ZipCode.ParentID && q.ActiveFlag).OrderBy(q => q.Code).ToList();
                            for (int j = 0; j < Z2s.Count; j++)
                                cMLE.Z2List.Add(new SelectListItem { Text = Z2s[j].Code + " " + Z2s[j].Title, Value = Z2s[j].ZID.ToString(), Selected = Z2s[j].ZID == L.ZID });
                        }
                    }
                    else
                    {
                        var Z1s = DC.ZipCode.Where(q => q.ParentID == 10 && q.ActiveFlag).OrderBy(q => q.Title).ToList();
                        for (int i = 0; i < Z1s.Count; i++)
                        {
                            cMLE.Z1List.Add(new SelectListItem { Text = Z1s[i].Title, Value = Z1s[i].ZID.ToString(), Selected = i == 0 });
                            if (i == 0)
                            {
                                var Z2s = DC.ZipCode.Where(q => q.ParentID == Z1s[i].ZID && q.ActiveFlag).OrderBy(q => q.Code).ToList();
                                for (int j = 0; j < Z2s.Count; j++)
                                    cMLE.Z2List.Add(new SelectListItem { Text = Z2s[j].Code + " " + Z2s[j].Title, Value = Z2s[j].ZID.ToString(), Selected = j == 0 });
                            }
                        }
                    }
                    #endregion
                    #region 電話
                    var C = DC.Contect.FirstOrDefault(q => q.TargetType == 3 && q.TargetID == ID);
                    if (C != null)
                        cMLE.C = C;
                    #endregion
                    #region 時間
                    for (int i = 0; i < 24; i++)
                    {
                        cMLE.SH_List.Add(new SelectListItem { Text = i.ToString().PadLeft(2, '0'), Value = i.ToString(), Selected = i == cMLE.cMLS.S_hour });
                        cMLE.EH_List.Add(new SelectListItem { Text = i.ToString().PadLeft(2, '0'), Value = i.ToString(), Selected = i == cMLE.cMLS.E_hour });
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        cMLE.SM_List.Add(new SelectListItem { Text = (i * 15).ToString().PadLeft(2, '0'), Value = (i * 15).ToString(), Selected = (i * 15) == cMLE.cMLS.S_minute });
                        cMLE.EM_List.Add(new SelectListItem { Text = (i * 15).ToString().PadLeft(2, '0'), Value = (i * 15).ToString(), Selected = (i * 15) == cMLE.cMLS.E_minute });
                    }
                    #endregion
                }
            }
            else//新增
            {
                if (POI == null)
                    SetAlert("查無資料,請重新操作", 2, "/Admin/OrganizeSet/Meeting_Location_List/0/0");
                else
                {
                    cMLE.cML = new Meeting_Location();
                    cMLE.cML.Code = "";
                    #region 上層
                    cMLE.OIParent = new List<ListSelect>();
                    int SortNo = 10;
                    var OIP = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == OIID && !q.DeleteFlag);
                    if (OIP != null)
                    {
                        int NowPID = OIP.ParentID;
                        var OIs = DC.OrganizeInfo.Where(q => q.ParentID == NowPID && !q.DeleteFlag);
                        do
                        {
                            if (OIs.Count() == 0 || SortNo < 0 || NowPID < 0)
                                break;
                            else
                            {
                                ListSelect cLS = new ListSelect();
                                cLS.ControlName = "ddl_OIID_" + SortNo;
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


                                if (SortNo == 10)
                                {
                                    cLS.OIList.ForEach(q => q.Selected = false);
                                    cLS.OIList.Find(q => q.Value == OIID.ToString()).Selected = true;
                                }

                                cMLE.OIParent.Add(cLS);
                                SortNo--;
                                OIs = DC.OrganizeInfo.Where(q => q.ParentID == NowPID && !q.DeleteFlag);
                            }
                        } while (true);
                    }
                    #endregion
                    #region 地址
                    var Z1s = DC.ZipCode.Where(q => q.ParentID == 10 && q.ActiveFlag).OrderBy(q => q.Title).ToList();
                    for (int i = 0; i < Z1s.Count; i++)
                    {
                        cMLE.Z1List.Add(new SelectListItem { Text = Z1s[i].Title, Value = Z1s[i].ZID.ToString(), Selected = (i == 0) });
                        if (i == 0)
                        {
                            var Z2s = DC.ZipCode.Where(q => q.ParentID == Z1s[i].ZID && q.ActiveFlag).OrderBy(q => q.Code).ToList();
                            for (int j = 0; j < Z2s.Count; j++)
                                cMLE.Z2List.Add(new SelectListItem { Text = Z2s[j].Code + " " + Z2s[j].Title, Value = Z2s[j].ZID.ToString(), Selected = (j == 0) });
                            cMLE.L = new Location { ZID = Z2s[0].ZID };
                        }
                    }
                    #endregion
                    #region 時間
                    for (int i = 0; i < 24; i++)
                    {
                        cMLE.SH_List.Add(new SelectListItem { Text = i.ToString().PadLeft(2, '0'), Value = i.ToString(), Selected = i == 0 });
                        cMLE.EH_List.Add(new SelectListItem { Text = i.ToString().PadLeft(2, '0'), Value = i.ToString(), Selected = i == 0 });
                    }
                    for (int i = 0; i < 4; i++)
                    {
                        cMLE.SM_List.Add(new SelectListItem { Text = (i * 15).ToString().PadLeft(2, '0'), Value = (i * 15).ToString(), Selected = i == 0 });
                        cMLE.EM_List.Add(new SelectListItem { Text = (i * 15).ToString().PadLeft(2, '0'), Value = (i * 15).ToString(), Selected = i == 0 });
                    }
                    #endregion
                }
            }
            cMLE.cML.SaveACID = GetACID();
            if (FC != null)
            {
                cMLE.cML.Title = FC.Get("txb_Title");
                cMLE.cML.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                cMLE.cML.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                cMLE.cMLS.TargetID = Convert.ToInt32(FC.Get("ddl_OIID_10"));
                cMLE.cMLS.S_hour = Convert.ToInt32(FC.Get(cMLE.SH_ControlName));
                cMLE.cMLS.S_minute = Convert.ToInt32(FC.Get(cMLE.SM_ControlName));
                cMLE.cMLS.E_hour = Convert.ToInt32(FC.Get(cMLE.EH_ControlName));
                cMLE.cMLS.E_minute = Convert.ToInt32(FC.Get(cMLE.EM_ControlName));
                cMLE.C.ContectValue = FC.Get("txb_Phone");
                cMLE.L.Address = FC.Get("txb_Address");
                if (FC.Get("ddl_Zip1") != null)
                {
                    cMLE.Z2List = new List<SelectListItem>();
                    int Z1ID = Convert.ToInt32(FC.Get("ddl_Zip1"));
                    cMLE.Z1List.ForEach(q => q.Selected = false);
                    cMLE.Z1List.First(q => q.Value == Z1ID.ToString()).Selected = true;
                    var Z2s = DC.ZipCode.Where(q => q.ParentID == Z1ID && q.ActiveFlag).OrderBy(q => q.Code).ToList();
                    for (int j = 0; j < Z2s.Count; j++)
                    {
                        if (FC.Get("ddl_Zip2") == Z2s[j].ZID.ToString())
                        {
                            cMLE.Z2List.Add(new SelectListItem { Text = Z2s[j].Code + " " + Z2s[j].Title, Value = Z2s[j].ZID.ToString(), Selected = true });
                            cMLE.L.ZipCode = Z2s[j];
                        }
                        else
                            cMLE.Z2List.Add(new SelectListItem { Text = Z2s[j].Code + " " + Z2s[j].Title, Value = Z2s[j].ZID.ToString() });
                    }
                }
            }
            return cMLE;
        }
        #endregion
        #region 牧養組織與職分管理-匯出
        public ActionResult Meeting_Location_Print(int ItemID, int ID)
        {
            GetViewBag();
            ArrayList AL = new ArrayList();
            ArrayList ALS = new ArrayList();
            ALS.Add("協會");
            ALS.Add("旌旗");
            ALS.Add("聚會點名稱");
            ALS.Add("電話");
            ALS.Add("地址");
            ALS.Add("啟用狀態");
            ALS.Add("建立時間");
            ALS.Add("更新時間");
            ALS.Add("最後更新者");
            AL.Add((string[])ALS.ToArray(typeof(string)));
            var POIs = DC.OrganizeInfo.Where(q => !q.DeleteFlag && q.OID == 1);
            if (ItemID > 0)
                POIs = POIs.Where(q => q.OIID == ItemID);
            POIs = POIs.OrderBy(q => q.OIID);

            foreach (var POI in POIs)
            {
                var cMLs = from q in DC.OrganizeInfo.Where(q => q.ParentID == POI.OIID && q.OID == 2 && !q.DeleteFlag)
                           join p in DC.M_Location_Set.Where(q => !q.DeleteFlag && q.SetType == 0)
                           on q.OIID equals p.TargetID
                           select new { q.OIID, OTitle = q.Title, p.MLID, MLTitle = p.Meeting_Location.Title, p.ActiveFlag, p.CreDate, p.UpdDate, p.SaveACID };
                var ACs = (from q in DC.Account
                           join p in cMLs
                           on q.ACID equals p.SaveACID
                           select new { q.ACID, q.Name }).ToList();
                var Cs = (from q in DC.Contect.Where(q => q.TargetType == 3 && q.ContectType == 0)
                          join p in cMLs
                          on q.TargetID equals p.MLID
                          select new { p.MLID, q.ContectValue }).ToList();

                var Ls = (from q in DC.Location.Where(q => q.TargetType == 3)
                          join p in cMLs
                          on q.TargetID equals p.MLID
                          join r in DC.ZipCode
                          on q.ZipCode.ParentID equals r.ZID
                          select new { p.MLID, sZipCode = q.ZipCode.Code, County = r.Title, Area = q.ZipCode.Title, q.Address }).ToList();
                foreach (var cML in cMLs.OrderBy(q => q.OIID).ThenBy(q => q.MLID))
                {
                    ALS = new ArrayList();
                    ALS.Add(POI.Title);
                    ALS.Add(cML.OTitle);
                    ALS.Add(cML.MLTitle);
                    var C = Cs.FirstOrDefault(q => q.MLID == cML.MLID);
                    ALS.Add(C != null ? C.ContectValue : "");
                    var L = Ls.FirstOrDefault(q => q.MLID == cML.MLID);
                    ALS.Add(L != null ? (L.sZipCode + " " + L.County + L.Area + L.Address) : "");
                    ALS.Add(cML.ActiveFlag ? "啟用中" : "");
                    ALS.Add(cML.CreDate.ToString(DateTimeFormat));
                    ALS.Add(cML.CreDate == cML.UpdDate ? "" : cML.UpdDate.ToString(DateTimeFormat));
                    var AC = ACs.FirstOrDefault(q => q.ACID == cML.SaveACID);
                    if (AC != null)
                        ALS.Add(AC.Name);
                    else
                        ALS.Add("--");
                    AL.Add((string[])ALS.ToArray(typeof(string)));
                }
            }


            WriteExcelFromString("主日聚會點列表", AL);
            SetAlert("已完成匯出", 1, "/Admin/OrganizeSet/Meeting_Location_List/" + ItemID + "/0");
            return View();
        }
        #endregion
    }
}