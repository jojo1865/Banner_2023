using Banner.Models;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Drawing;
using System.Linq;
using System.Net.Mime;
using System.Runtime.ConstrainedExecution;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Web.UI.WebControls;
using static Banner.Areas.Admin.Controllers.OrganizeSetController;

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
        public ActionResult Organize_Map_List()
        {
            GetViewBag();
            return View(GetMapTable());
        }
        //取得組織與職分的表單
        private cTableList GetMapTable()
        {
            cTableList cTL = new cTableList();
            cTL.Rs = new List<cTableRow>();
            var Os = DC.Organize.Where(q => !q.DeleteFlag).ToList();
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
                    C.Title = O.Title + "(" + O.OrganizeInfo.Count() + ")";
                    C.ControlName = O.JobTitle;
                    C.URL = "/Admin/OrganizeSet/Organize_Map_Edit/" + O.OID;
                    C.CSS = O.ActiveFlag ? "td_O_Basic3_Active" : "td_O_Basic3_UnActive";
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
        //初始化或取得組織資料
        private cOrganize_Map_Edit ReSetOrganize(int ID, FormCollection FC)
        {
            cOrganize_Map_Edit cME = new cOrganize_Map_Edit();
            if (ID > 0)
            {
                cME.O = DC.Organize.FirstOrDefault(q => q.OID == ID && !q.DeleteFlag);
                if (cME.O == null)
                    SetAlert("查無資料,請重新操作", 2, "/Admin/OrganizeSet/Organize_Map_List/0");
                else
                {
                    cME.OList = new List<SelectListItem>();
                    var Os = DC.Organize.Where(q => !q.DeleteFlag).ToList();
                    var O = Os.FirstOrDefault(q => q.ParentID == 0);
                    if (O != null)
                    {
                        do
                        {
                            cME.OList.Add(new SelectListItem { Text = O.Title, Value = O.OID.ToString(), Selected = (cME.O.ParentID == O.OID) });
                            O = Os.FirstOrDefault(q => q.ParentID == O.OID);
                            if (O == null || O.OID == 8)//排除小組~讓小組一定在最下面
                                break;
                        } while (true);
                    }
                }
            }
            else
            {
                cME.O = new Organize() { ActiveFlag = true, DeleteFlag = false };
                cME.OList = new List<SelectListItem>();
                var Os = DC.Organize.Where(q => !q.DeleteFlag).ToList();
                var O = Os.FirstOrDefault(q => q.ParentID == 0);
                if (O != null)
                {
                    do
                    {
                        cME.OList.Add(new SelectListItem { Text = O.Title, Value = O.OID.ToString(), Selected = O.ParentID == 0 });
                        O = Os.FirstOrDefault(q => q.ParentID == O.OID);
                        if (O == null || O.OID == 8)//排除小組~讓小組一定在最下面
                            break;
                    } while (true);
                }
            }
            cME.O.SaveACID = ACID;
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
        public ActionResult Organize_Map_Edit(int ID)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            return View(ReSetOrganize(ID, null));
        }
        [HttpPost]

        public ActionResult Organize_Map_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            var cOE = ReSetOrganize(ID, FC);
            if (cOE.O.Title == "")
                Error += "請輸入組織名稱</br>";
            else if (!cOE.O.ActiveFlag || cOE.O.DeleteFlag)//刪除
            {
                var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.OID == cOE.O.OID);
                if (OI != null)
                    Error += "請先移除此組織等級的組織資料後再行刪除</br>";
                else if (cOE.O.OID == 8)
                {
                    var Ms = DC.M_OI_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OrganizeInfo.OID == cOE.O.OID);
                    if (Ms.Count() > 0)
                        Error += "請先移除所有小組層級的會友後再行刪除</br>";
                }
            }
            else if (cOE.O.OID == cOE.NewParentID)
                Error += "您不能指定自己為自己的上層組織</br>";

            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                if (cOE.O.ParentID > 0)
                {
                    //抽離的狀況
                    if (cOE.O.ParentID != cOE.NewParentID)
                    {
                        var OC = DC.Organize.FirstOrDefault(q => q.ParentID == cOE.O.OID && !q.DeleteFlag);
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
                var OP = DC.Organize.FirstOrDefault(q => q.ParentID == cOE.NewParentID && q.OID != cOE.O.OID && !q.DeleteFlag);
                if (OP != null)
                {
                    OP.ParentID = cOE.O.OID;
                    DC.SubmitChanges();
                }
                #region 選單角色設定
                //選單角色設定
                var R = DC.Role.FirstOrDefault(q => q.OID == cOE.O.OID);
                if (R == null)//新增
                {
                    R = new Role
                    {
                        ParentID = 2,//限會友
                        OID = cOE.O.OID,
                        Title = cOE.O.JobTitle,
                        RoleType = 2,//前台牧養職分功能
                        ActiveFlag = cOE.O.ActiveFlag,
                        DeleteFlag = false,
                        CreDate = DT,
                        UpdDate = DT,
                        SaveACID = ACID
                    };
                    DC.Role.InsertOnSubmit(R);
                    DC.SubmitChanges();
                }
                else
                {
                    R.Title = cOE.O.JobTitle;
                    R.ActiveFlag = cOE.O.ActiveFlag;
                    R.DeleteFlag = cOE.O.DeleteFlag;
                    R.SaveACID = ACID;
                    R.UpdDate = DT;
                    DC.SubmitChanges();
                }
                #endregion
                #region 2024/1/9 新增一鍵調整新組織插入後的組織樹調整
                if (ID == 0)
                {
                    //牧區->督區  ==> 牧區->大督區->督區
                    var OI_Ps = DC.OrganizeInfo.Where(q => q.OID == cOE.O.ParentID);//先找到屬於上一層的冠名組織(OO牧區)
                    foreach (var OI_P in OI_Ps)
                    {
                        //先建立本層的冠名組織,但是使用上一層的資料來用
                        //(OO大督區)
                        OrganizeInfo OI = new OrganizeInfo
                        {
                            OID = cOE.O.OID,
                            ParentID = OI_P.OIID,
                            OI2_ID = OI_P.OI2_ID,
                            Title = OI_P.Title,
                            ACID = OI_P.ACID,
                            Note = OI_P.Note,
                            BusinessType = OI_P.BusinessType,
                            ActiveFlag = OI_P.ActiveFlag,
                            DeleteFlag = OI_P.DeleteFlag,
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = ACID,
                            OldID = 0
                        };
                        DC.OrganizeInfo.InsertOnSubmit(OI);
                        DC.SubmitChanges();

                        //再來找XX督區
                        var OI_Cs = DC.OrganizeInfo.Where(q => q.ParentID == OI_P.OIID && q.OID != OI.OID);
                        OI_Cs.ForEach(q => q.ParentID = OI.OIID);
                        DC.SubmitChanges();
                    }
                }
                #endregion
                SetAlert((ID == 0 ? "新增" : "更新") + "完成", 1, "/Admin/OrganizeSet/Organize_Map_List/");
            }


            return View(cOE);
        }

        #endregion
        #region 牧養組織與職分管理-匯出
        public ActionResult Organize_Map_Print()
        {
            GetViewBag();
            ArrayList AL = new ArrayList();
            ArrayList ALS = new ArrayList();
            ALS.Add("ID");
            ALS.Add("組織名稱");
            ALS.Add("職分名稱");
            ALS.Add("啟用狀態");
            ALS.Add("建立時間");
            ALS.Add("更新時間");
            ALS.Add("最後更新者");
            AL.Add((string[])ALS.ToArray(typeof(string)));
            var Os = DC.Organize.Where(q => !q.DeleteFlag).ToList();
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
                    ALS.Add(O.Title);
                    ALS.Add(O.JobTitle);
                    ALS.Add(O.ActiveFlag ? "啟用中" : "");
                    ALS.Add(O.CreDate.ToString(DateTimeFormat));
                    ALS.Add(O.CreDate == O.UpdDate ? "" : O.UpdDate.ToString(DateTimeFormat));
                    var AC = ACs.FirstOrDefault(q => q.ACID == O.SaveACID);
                    if (AC != null)
                        ALS.Add((AC.Name_First + AC.Name_Last));
                    else
                        ALS.Add("--");
                    AL.Add((string[])ALS.ToArray(typeof(string)));

                    O = Os.FirstOrDefault(q => q.ParentID == O.OID);
                    if (O == null)
                        break;
                } while (true);
            }

            WriteExcelFromString("組織與職分管理列表", AL);
            SetAlert("已完成匯出", 1, "/Admin/OrganizeSet/Organize_Map_List/0");
            return View();
        }
        #endregion

        #region 牧養組織與職分-列表
        public class cOrganize_Info_List
        {
            public int OID = 0;
            public string OTitle = "";
            //public cTableRow cOrganize = new cTableRow();
            public cTableList cTL = new cTableList();
            public string sAddURL = "";
            
        }
        private cOrganize_Info_List GetOrganize_Info_List(int OID, int OIID, FormCollection FC)
        {
            ACID = GetACID();
            string sKey = "";
            if (FC != null)
            {
                sKey = FC.Get("txb_OTitle");
                OID = Convert.ToInt32(FC.Get("ddl_O"));
            }
            else if (ACID != 1 && OID < 2)//非Admin,故須限制使用範圍,初始組織為"旌旗"
            {
                OID = 2;
            }
            //檢查目前組織OID跟OIID是否對的上,對不上表示使用的搜尋,把OIID歸零處理
            if (OIID > 0)
            {
                var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ParentID == OIID && q.OID == OID && !q.DeleteFlag);
                if (OI == null)
                    OIID = 0;
            }

            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");

            cOrganize_Info_List cOL = new cOrganize_Info_List();
            cOL.sAddURL = "/Admin/OrganizeSet/Organize_Info_Edit/" + OID + "/" + OIID + "/0";
            cOL.OID = OID;
            cOL.OTitle = sKey;

            cOL.cTL = new cTableList();
            cOL.cTL.Title = "";
            cOL.cTL.NowPage = iNowPage;
            cOL.cTL.NowURL = "/Admin/OrganizeSet/Organize_Info_List/" + OID + "/" + OIID;
            cOL.cTL.NumCut = iNumCut;
            cOL.cTL.Rs = new List<cTableRow>();

            string ParentTitle = "上層名稱";
            string NowTitle = "本層名稱";
            string NextTitle = "下層名稱";
            int POID = 0, NOID = 0;

            var O_ = DC.Organize.FirstOrDefault(q => q.OID == OID);
            if (O_ != null)
            {
                NowTitle = O_.Title;
                var O_Up = DC.Organize.FirstOrDefault(q => q.OID == O_.ParentID && !q.DeleteFlag);
                if (O_Up != null)
                {
                    POID = O_Up.OID;
                    ParentTitle = O_Up.Title;
                }
                else
                    ParentTitle = "";

                var O_Next = DC.Organize.FirstOrDefault(q => q.ParentID == OID && !q.DeleteFlag);
                if (O_Next != null)
                {
                    NOID = O_Next.OID;
                    NextTitle = O_Next.Title;
                }
                else
                    NextTitle = "";
            }

            #region 搜尋內容
            var Ns_ = DC.OrganizeInfo.Where(q => !q.DeleteFlag);
            if (OID > 0)
                Ns_ = Ns_.Where(q => q.OID == OID);
            if (OIID > 0)
                Ns_ = Ns_.Where(q => q.ParentID == OIID);
            if (sKey != "")
                Ns_ = Ns_.Where(q => q.Title.Contains(sKey));
            var Ns = Ns_.ToList();
            if (ACID != 1)//全職同工權限鎖定可以給16個旌旗,指定之後看不到其他不同的資料
            {
                var MOI2 = DC.M_OI2_Account.FirstOrDefault(q => q.OIID == 1 && q.ACID == ACID && !q.DeleteFlag && q.ActiveFlag);
                if (MOI2 == null)//他沒有全部的權限
                {
                    Ns = (from q in Ns
                          join p in DC.M_OI2_Account.Where(q => q.ACID == ACID && !q.DeleteFlag && q.ActiveFlag).ToList()
                          on q.OI2_ID equals p.OIID
                          select q).ToList();

                    if (OID <= 2)
                    {
                        if (bGroup[1])//不能新增
                            bGroup[1] = false;
                        if (bGroup[2])//不能編輯
                            bGroup[2] = false;
                        if (bGroup[3])//不能刪除
                            bGroup[3] = false;
                        ViewBag._Power = bGroup;
                    }
                }
            }
            bool HaveOldNext = false;
            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "控制", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "編號(ID)", WidthPX = 100 });
            if (ParentTitle != "")
                TopTitles.Add(new cTableCell { Title = ParentTitle });

            TopTitles.Add(new cTableCell { Title = NowTitle });
            if (OID == 8)
                TopTitles.Add(new cTableCell { Title = "組員數量", WidthPX = 80 });
            else
            {
                if (NextTitle != "下層名稱")
                    TopTitles.Add(new cTableCell { Title = NextTitle + "數量", WidthPX = 100 });
                else if (sKey == "")
                {
                    var COIs = from q in Ns
                               join p in DC.OrganizeInfo.Where(q => q.OID != NOID && !q.DeleteFlag)
                               on q.OIID equals p.ParentID
                               select p;
                    if (COIs.Count() > 0)
                    {
                        HaveOldNext = true;
                        TopTitles.Add(new cTableCell { Title = "舊下層", WidthPX = 100 });
                    }
                    else
                        TopTitles.Add(new cTableCell { Title = "--", WidthPX = 120 });
                }
                TopTitles.Add(new cTableCell { Title = "新增下層", WidthPX = 120 });
            }
            TopTitles.Add(new cTableCell { Title = "職分主管", WidthPX = 160 });
            TopTitles.Add(new cTableCell { Title = "狀態", WidthPX = 120 });

            cOL.cTL.Rs.Add(SetTableRowTitle(TopTitles));

            cOL.cTL.TotalCt = Ns.Count();
            cOL.cTL.MaxNum = GetMaxNum(cOL.cTL.TotalCt, cOL.cTL.NumCut);
            Ns = Ns.OrderBy(q => q.OID).ThenByDescending(q => q.CreDate).Skip((iNowPage - 1) * cOL.cTL.NumCut).Take(cOL.cTL.NumCut).ToList();

            foreach (var N in Ns)
            {

                var NP = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == N.ParentID);
                cTableRow cTR = new cTableRow();
                if (bGroup[0])
                {
                    if (bGroup[2])
                        cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/OrganizeSet/Organize_Info_Edit/" + N.OID + "/" + N.ParentID + "/" + N.OIID, Target = "_self", Value = "編輯" });//
                    else
                        cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/OrganizeSet/Organize_Info_Edit/" + N.OID + "/" + N.ParentID + "/" + N.OIID, Target = "_self", Value = "檢視" });//
                }
                else
                    cTR.Cs.Add(new cTableCell { Value = "" });//控制

                cTR.Cs.Add(new cTableCell { Value = N.OIID.ToString() });//編號(ID)
                if (ParentTitle != "")
                {
                    if (NP != null)
                    {
                        string sTitle = (NP.OID == POID ? NP.Title + NP.Organize.Title : "[" + NP.Title + NP.Organize.Title + "]");
                        cTR.Cs.Add(new cTableCell { Type = "link", URL = "/Admin/OrganizeSet/Organize_Info_List/" + NP.OID + "/" + NP.ParentID, Target = "_self", Value = sTitle });//
                    }
                    else
                        cTR.Cs.Add(new cTableCell { Value = "" });//上層名稱
                }
                cTR.Cs.Add(new cTableCell { Value = N.Title + N.Organize.Title + (N.BusinessType == 1 ? "(外展)" : "") });//本層名稱
                if (OID != 8)
                {
                    var RightOIs = DC.OrganizeInfo.Where(q => !q.DeleteFlag && q.ParentID == N.OIID && q.OID == NOID).ToList();
                    var NextOIs = DC.OrganizeInfo.Where(q => !q.DeleteFlag && q.ParentID == N.OIID && q.OID != NOID).ToList();

                    if (RightOIs.Count > 0)//新下層已有資料
                        cTR.Cs.Add(new cTableCell { Type = "link", Target = "_self", CSS = "", URL = "/Admin/OrganizeSet/Organize_Info_List/" + NOID + "/" + N.OIID, Value = RightOIs.Count.ToString() });//下層組織
                    else if (sKey == "")
                        cTR.Cs.Add(new cTableCell { Value = "0" });
                    if (HaveOldNext)
                    {
                        if (NextOIs.Count > 0)//舊下層仍有資料
                            cTR.Cs.Add(new cTableCell { Type = "link", Target = "_self", CSS = "", URL = "/Admin/OrganizeSet/Organize_Info_List/" + NextOIs[0].OID + "/" + N.OIID, Value = NextOIs.Count.ToString() });//下層組織
                        else
                            cTR.Cs.Add(new cTableCell { Value = "0" });
                    }

                    //if (bGroup[1])
                        cTR.Cs.Add(new cTableCell { Type = "link", Target = "_self", CSS = "btn_Basic_W", URL = "/Admin/OrganizeSet/Organize_Info_Edit/" + NOID + "/" + N.OIID + "/0", Value = "新增" + NextTitle });
                    //else
                    //    cTR.Cs.Add(new cTableCell { Value = "" });
                }
                else
                {
                    int Ct = GetMOIAC(0, N.OIID, 0).Count();
                    if (Ct == 0)//沒有新成員
                        cTR.Cs.Add(new cTableCell { Value = "0" });//等待分發名單中
                    else
                        cTR.Cs.Add(new cTableCell { Type = "link", Target = "_black", CSS = "", URL = "/Admin/OrganizeSet/Organize_Info_Account_List?OID=" + N.OID + "&OIID=" + N.OIID, Value = "(" + Ct + ")" });//組員數量
                }
                if (OID < 3)
                    cTR.Cs.Add(new cTableCell { Value = "" });//職分主管
                else
                    cTR.Cs.Add(new cTableCell { Value = (N.Account.Name_First + N.Account.Name_Last) });//職分主管

                if (N.ActiveFlag)
                    cTR.Cs.Add(new cTableCell { Value = "啟用", CSS = "btn btn-outline-success", Type = "activebutton", URL = (bGroup[2] ? "ChangeActive(this,'OrganizeInfo'," + N.OIID + ")" : "javascript:alert('無修改權限')") });//狀態
                else
                    cTR.Cs.Add(new cTableCell { Value = "停用", CSS = "btn btn-outline-danger", Type = "activebutton", URL = (bGroup[2] ? "ChangeActive(this,'OrganizeInfo'," + N.OIID + ")" : "javascript:alert('無修改權限')") });//狀態

                cOL.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }

            #endregion
            return cOL;
        }
        [HttpGet]
        public ActionResult Organize_Info_List(int OID, int ID)
        {
            GetViewBag();
            return View(GetOrganize_Info_List(OID, ID, null));
        }
        [HttpPost]
        public ActionResult Organize_Info_List(int OID, int ID, FormCollection FC)
        {
            GetViewBag();
            return View(GetOrganize_Info_List(OID, ID, FC));
        }
        #endregion
        #region 牧養組織與職分-新增/編輯/刪除
        public class cOrganize_Info_Edit
        {
            public OrganizeInfo OI = new OrganizeInfo();
            public List<SelectListItem> ACList = new List<SelectListItem>();
            public List<SelectListItem> dUPOIList = new List<SelectListItem>();
            public Contect C = new Contect();
            public Location L = new Location();
            public string sUPOIList = "";
            public string C_str = "";
            public string L_str = "";
            public List<SelectListItem> PList = new List<SelectListItem>();
            public List<PayType> PTList = new List<PayType>();
        }
        public class OIListSelect
        {
            public string ControlName = "";
            public int SortNo = 0;
            public List<SelectListItem> OIList = new List<SelectListItem>();
        }

        public ActionResult Organize_Info_Edit(int OID, int PID, int OIID)
        {
            GetViewBag();
            ChangeTitle(OIID == 0);
            return View(ReSetOrganizeInfo(OID, PID, OIID, null));
        }
        [HttpPost]

        public ActionResult Organize_Info_Edit(int OID, int PID, int OIID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(OIID == 0);
            cOrganize_Info_Edit cIE = ReSetOrganizeInfo(OID, PID, OIID, FC);

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
                if (OID <= 2)
                {
                    cIE.C.TargetType = 1;
                    cIE.C.TargetID = cIE.OI.OIID;
                    cIE.C.ContectType = 0;
                    cIE.C.CheckFlag = false;
                    cIE.C.CreDate = DT;
                    cIE.C.CheckDate = DT;
                    if (cIE.C.CID == 0)
                        DC.Contect.InsertOnSubmit(cIE.C);
                    DC.SubmitChanges();

                    cIE.L.TargetType = 1;
                    cIE.L.TargetID = cIE.OI.OIID;
                    if (cIE.L.LID == 0)
                        DC.Location.InsertOnSubmit(cIE.L);
                    DC.SubmitChanges();
                }

                if (cIE.OI.DeleteFlag || !cIE.OI.ActiveFlag)//若組織被關閉則移除相關權限
                {
                    var MOI2s = GetMOI2AC(cIE.OI.OIID);
                    foreach (var MOI2 in MOI2s)
                    {
                        MOI2.ActiveFlag = cIE.OI.ActiveFlag;
                        MOI2.DeleteFlag = cIE.OI.DeleteFlag;
                        MOI2.UpdDate = DT;
                        MOI2.SaveACID = ACID;
                        DC.SubmitChanges();
                    }
                }

                if (OID == 1)
                {
                    foreach (var PT in cIE.PTList)
                    {
                        if (PT.PTID == 0)
                        {
                            PT.OIID = cIE.OI.OIID;
                            DC.PayType.InsertOnSubmit(PT);
                            DC.SubmitChanges();
                        }
                        else
                        {
                            PT.UpdDate = DT;
                            PT.SaveACID = ACID;
                            DC.SubmitChanges();
                        }
                    }
                }


                PID = cIE.OI.ParentID;
                SetAlert((OIID == 0 ? "新增" : "更新") + "完成", 1, "/Admin/OrganizeSet/Organize_Info_List/" + OID + "/" + PID);
            }
            return View(cIE);
        }

        private cOrganize_Info_Edit ReSetOrganizeInfo(int OID, int PID, int OIID, FormCollection FC)
        {

            cOrganize_Info_Edit cIE = new cOrganize_Info_Edit();

            #region 物件初始化

            for (int i = 0; i < sPayType.Length; i++)
                cIE.PList.Add(new SelectListItem { Text = sPayType[i], Value = i.ToString() });

            #endregion
            #region 權限調整-牧養部的人來改組織須限制不能改協會與旌旗
            if (ACID != 1)//全職同工權限鎖定可以給16個旌旗,指定之後看不到其他不同的資料
            {
                var MOI2 = DC.M_OI2_Account.FirstOrDefault(q => q.OIID == 1 && q.ACID == ACID && !q.DeleteFlag && q.ActiveFlag);
                if (MOI2 == null)//他沒有全部的權限
                {
                    if (OID <= 2)
                    {
                        if (bGroup[1])//不能新增
                            bGroup[1] = false;
                        if (bGroup[2])//不能編輯
                            bGroup[2] = false;
                        if (bGroup[3])//不能刪除
                            bGroup[3] = false;
                        ViewBag._Power = bGroup;
                    }
                }
            }
            #endregion
            var ACs = from q in DC.M_O_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OID == OID && !q.Account.DeleteFlag)
                      group q by new { q.ACID, q.Account.Name_First, q.Account.Name_Last } into g
                      select new { g.Key.ACID, Name = g.Key.Name_First + g.Key.Name_Last };
            var AC0 = DC.Account.First(q => q.ACID == 1);
            if (OIID > 0)//更新
            {
                cIE.OI = DC.OrganizeInfo.FirstOrDefault(q => q.OID == OID && q.OIID == OIID && !q.DeleteFlag);
                if (cIE.OI == null)
                    SetAlert("查無資料,請重新操作", 2, "/Admin/OrganizeSet/Organize_Info_List/" + OID + "/0");
                else
                {
                    #region 主管列表
                    cIE.ACList = new List<SelectListItem>();
                    cIE.ACList.Add(new SelectListItem { Text = AC0.Name_First + AC0.Name_Last, Value = AC0.ACID.ToString(), Selected = AC0.ACID == cIE.OI.ACID });
                    cIE.ACList.AddRange(from q in ACs.OrderBy(q => q.Name)
                                        select new SelectListItem { Text = "(" + q.ACID + ")" + q.Name, Value = q.ACID.ToString(), Selected = q.ACID == cIE.OI.ACID });

                    if (cIE.ACList.Count > 0)
                    {
                        if (cIE.ACList.FirstOrDefault(q => q.Selected) == null)
                            cIE.ACList[0].Selected = true;
                    }

                    #endregion
                    #region 上層--文字版
                    var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == cIE.OI.ParentID);
                    while (OI != null)
                    {
                        cIE.sUPOIList = OI.Title + OI.Organize.Title + (cIE.sUPOIList == "" ? "" : "/" + cIE.sUPOIList);
                        OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == OI.ParentID);
                    }
                    #endregion
                    #region 上層--下拉選單

                    var OP_ = DC.Organize.FirstOrDefault(q => q.OID == cIE.OI.Organize.ParentID);
                    var OIP_ = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == cIE.OI.ParentID);
                    if (OP_ != null && OIP_ != null)
                    {
                        if (OP_.OID != OIP_.OID)//層級不對,要選擇新層級
                        {
                            var OICs = DC.OrganizeInfo.Where(q => q.OID == OP_.OID && !q.DeleteFlag);
                            if (OICs.Count() > 0)
                            {
                                foreach (var OIC in OICs)
                                    cIE.dUPOIList.Add(new SelectListItem { Text = OIC.Title, Value = OIC.OIID.ToString() });
                                cIE.dUPOIList[0].Selected = true;
                            }
                        }
                    }

                    #endregion
                    #region 查聚會點
                    var MLS = DC.Meeting_Location_Set.FirstOrDefault(q => q.SetType == 1 && q.OIID == cIE.OI.OIID && q.ActiveFlag && !q.DeleteFlag);
                    if (MLS != null)
                    {
                        #region 地址
                        var L = DC.Location.FirstOrDefault(q => q.TargetType == 3 && q.TargetID == MLS.MLSID);
                        if (L != null)
                        {
                            cIE.L = L;
                            cIE.L_str = GetLocationString(L);
                        }
                        else
                            cIE.L = new Location { ZID = 10 };

                        #endregion
                        #region 電話
                        var C = DC.Contect.FirstOrDefault(q => q.TargetType == 3 && q.TargetID == MLS.MLSID);
                        if (C != null)
                        {
                            cIE.C = C;

                            if (C.ZID > 0)
                            {
                                var Z = DC.ZipCode.FirstOrDefault(q => q.ZID == C.ZID);
                                cIE.C_str = (Z != null ? (Z.Title + "(" + Z.Code + ")") : "") + C.ContectValue;
                            }
                        }
                        else
                            cIE.C = new Contect { ZID = 10, ContectType = 0, ContectValue = "" };
                        #endregion
                    }
                    else
                    {
                        if (DC.Contect.Count(q => q.TargetID == cIE.OI.OIID && q.ContectType == 0 && q.TargetType == 1) > 1)
                        {
                            int MaxID = DC.Contect.Where(q => q.TargetID == cIE.OI.OIID && q.ContectType == 0 && q.TargetType == 1).Max(q => q.CID);
                            DC.Contect.DeleteAllOnSubmit(DC.Contect.Where(q => q.TargetID == cIE.OI.OIID && q.ContectType == 0 && q.TargetType == 1 && q.CID != MaxID));
                            DC.SubmitChanges();
                        }
                        if (DC.Location.Count(q => q.TargetID == cIE.OI.OIID && q.TargetType == 1) > 1)
                        {
                            int MaxID = DC.Location.Where(q => q.TargetID == cIE.OI.OIID && q.TargetType == 1).Max(q => q.LID);
                            DC.Location.DeleteAllOnSubmit(DC.Location.Where(q => q.TargetID == cIE.OI.OIID && q.TargetType == 1 && q.LID != MaxID));
                            DC.SubmitChanges();
                        }

                        cIE.C = DC.Contect.FirstOrDefault(q => q.TargetID == cIE.OI.OIID && q.ContectType == 0 && q.TargetType == 1);
                        cIE.L = DC.Location.FirstOrDefault(q => q.TargetID == cIE.OI.OIID && q.TargetType == 1);
                        if (cIE.C == null)
                            cIE.C = new Contect { ZID = 10, ContectType = 0, ContectValue = "" };
                        if (cIE.L == null)
                            cIE.L = new Location { ZID = 10, Address = "" };
                    }
                    #endregion
                    #region 付款方式
                    var PLs = DC.PayType.Where(q => q.OIID == OIID && !q.DeleteFlag).ToList();
                    if (PLs.Count() == 0)
                    {
                        for (int i = 0; i < sPayType.Length; i++)
                        {
                            PayType PT = new PayType
                            {
                                OrganizeInfo = cIE.OI,
                                PayTypeID = i,
                                Title = "",
                                Note = "",
                                TargetURL = "",
                                BackURL = "",
                                MerchantID = "",
                                HashKey = "",
                                HashIV = "",
                                PayKey1 = "",
                                PayKey2 = "",
                                PayKey3 = "",
                                PayKey4 = "",
                                PayKey5 = "",
                                ActiveFlag = false,
                                DeleteFlag = false,
                                CreDate = DT,
                                UpdDate = DT,
                            };
                            cIE.PTList.Add(PT);
                        }
                    }
                    else
                        cIE.PTList = PLs;
                    for (int i = 0; i < sPayType.Length; i++)
                    {
                        var PL = PLs.FirstOrDefault(q => q.PayTypeID == i);
                        if (PL != null)
                            cIE.PList.First(q => q.Value == i.ToString()).Selected = PL.ActiveFlag;
                    }
                    #endregion
                }
            }
            else//新增
            {
                var O = DC.Organize.FirstOrDefault(q => q.OID == OID && !q.DeleteFlag);
                if (O == null)
                    SetAlert("查無資料,請重新操作", 2, "/Admin/OrganizeSet/Organize_Map_List/0");
                else
                {
                    cIE.OI = new OrganizeInfo();
                    cIE.OI.Organize = O;
                    cIE.OI.ParentID = PID;
                    cIE.OI.ActiveFlag = true;
                    cIE.OI.BusinessType = 0;
                    cIE.OI.OI2_ID = 0;
                    #region 上層--文字版
                    var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == cIE.OI.ParentID);
                    while (OI != null)
                    {
                        cIE.sUPOIList = OI.Title + OI.Organize.Title + (cIE.sUPOIList == "" ? "" : "/" + cIE.sUPOIList);
                        if (OI.OID == 2)
                            cIE.OI.OI2_ID = OI.OIID;
                        OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == OI.ParentID);
                    }
                    #endregion
                    #region 主管
                   
                    cIE.ACList = new List<SelectListItem>();
                    cIE.ACList.Add(new SelectListItem { Text = AC0.Name_First + AC0.Name_Last, Value = AC0.ACID.ToString() });
                    foreach (var A in ACs.OrderBy(q => q.Name))
                        cIE.ACList.Add(new SelectListItem { Value = A.ACID.ToString(), Text = A.Name });
                    cIE.ACList[0].Selected = true;

                    #endregion
                    #region 地址/電話
                    cIE.C = new Contect { ZID = 10, ContectType = 0, ContectValue = "" };
                    cIE.L = new Location { ZID = 10, Address = "" };
                    #endregion
                    #region 付款方式
                    for (int i = 0; i < sPayType.Length; i++)
                    {
                        PayType PT = new PayType
                        {
                            OrganizeInfo = cIE.OI,
                            PayTypeID = i,
                            Title = "",
                            Note = "",
                            TargetURL = "",
                            BackURL = "",
                            MerchantID = "",
                            HashKey = "",
                            HashIV = "",
                            PayKey1 = "",
                            PayKey2 = "",
                            PayKey3 = "",
                            PayKey4 = "",
                            PayKey5 = "",
                            ActiveFlag = false,
                            DeleteFlag = false,
                            CreDate = DT,
                            UpdDate = DT,
                        };
                        cIE.PTList.Add(PT);
                    }
                    #endregion
                }
            }
            cIE.OI.SaveACID = ACID;
            if (FC != null)
            {
                cIE.OI.Title = FC.Get("txb_Title");
                cIE.OI.ACID = Convert.ToInt32(FC.Get("ddl_ACID"));
                cIE.OI.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                cIE.OI.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                cIE.OI.BusinessType = GetViewCheckBox(FC.Get("cbox_Business")) ? 1 : 0;
                if (!string.IsNullOrEmpty(FC.Get("ddl_UPOIList")))
                {
                    cIE.dUPOIList.ForEach(q => q.Selected = false);
                    cIE.dUPOIList.First(q => q.Value == FC.Get("ddl_UPOIList")).Selected = true;
                    cIE.OI.ParentID = Convert.ToInt32(FC.Get("ddl_UPOIList"));
                }
                if (cIE.OI.OID <= 2)
                {
                    cIE.C.ContectValue = FC.Get("txb_PhoneNo");
                    cIE.C.ZID = Convert.ToInt32(FC.Get("ddl_PhoneZip"));

                    if (FC.Get("ddl_Zip0") == "10")
                    {
                        cIE.L.ZID = Convert.ToInt32(FC.Get("ddl_Zip2"));
                        cIE.L.Address = FC.Get("txb_Address0");
                    }
                    else if (FC.Get("ddl_Zip0") == "2")
                    {
                        cIE.L.ZID = Convert.ToInt32(FC.Get("ddl_Zip3"));
                        cIE.L.Address = FC.Get("txb_Address1_1") + "\n" + FC.Get("txb_Address1_2");
                    }
                    else
                    {
                        cIE.L.ZID = Convert.ToInt32(FC.Get("ddl_Zip0"));
                        cIE.L.Address = FC.Get("txb_Address2");
                    }

                }
                for (int i = 0; i < sPayType.Length; i++)
                {
                    var PL = cIE.PTList.FirstOrDefault(q => q.PayTypeID == i);
                    if (PL != null)
                    {
                        PL.ActiveFlag = GetViewCheckBox(FC.Get("cbox_PayType_" + i));
                        switch (i)
                        {
                            case 1://信用卡
                            case 2://ATM
                                {
                                    PL.Title = FC.Get("txb_PayType_Title");
                                    PL.MerchantID = FC.Get("txb_PayType_MerchantID");
                                    PL.HashKey = FC.Get("txb_PayType_HashKey");
                                    PL.HashIV = FC.Get("txb_PayType_HashIV");
                                    PL.TargetURL = FC.Get("txb_PayType_TargetURL");
                                }
                                break;
                        }
                    }
                }
            }

            return cIE;
        }


        #endregion
        #region 牧養組織與職分-成員列表

        private cTableList GetOrganize_Info_Account_List(int OID, int OIID, FormCollection FC)
        {
            cTableList cTL = new cTableList();
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            string sKey = FC != null ? FC.Get("txb_Key") : "";
            ViewBag._Key = sKey;
            cTL = new cTableList();
            cTL.Title = "";
            cTL.NowPage = iNowPage;
            cTL.NowURL = "/Admin/OrganizeSet/Organize_Info_Account_List/" + OID + "/" + OIID;
            cTL.NumCut = iNumCut;
            cTL.Rs = new List<cTableRow>();

            int LeaderACID = 0;
            var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == OIID && q.OID == OID && !q.DeleteFlag);
            if (OI != null)
            {
                ViewBag._Title = OI.Title + OI.Organize.Title + (OI.BusinessType == 1 ? "(外展)" : "") + " 成員列表";
                LeaderACID = OI.ACID;
            }
            var Ns = DC.M_OI_Account.Where(q => !q.DeleteFlag && q.OIID == OIID && !q.Account.DeleteFlag);
            if (!string.IsNullOrEmpty(sKey))
                Ns = Ns.Where(q => (q.Account.Name_First + q.Account.Name_Last).Contains(sKey));

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "會員資料", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "成員ID", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "會員ID", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "姓名" });
            TopTitles.Add(new cTableCell { Title = "加入日期" });
            TopTitles.Add(new cTableCell { Title = "離開日期" });
            TopTitles.Add(new cTableCell { Title = "職務" });
            TopTitles.Add(new cTableCell { Title = "受洗狀態" });

            cTL.Rs.Add(SetTableRowTitle(TopTitles));

            cTL.TotalCt = Ns.Count();
            cTL.MaxNum = GetMaxNum(cTL.TotalCt, cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.OrganizeInfo.ACID == q.ACID).ThenByDescending(q => q.MID).Skip((iNowPage - 1) * cTL.NumCut).Take(cTL.NumCut);
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_Aldult_Info/" + N.ACID, Target = "_black", Value = "編輯" });//
                cTR.Cs.Add(new cTableCell { Value = N.MID.ToString() });//成員ID
                cTR.Cs.Add(new cTableCell { Value = N.ACID.ToString() });//會員ID
                cTR.Cs.Add(new cTableCell { Value = N.Account.Name_First + N.Account.Name_Last });//姓名
                if (N.JoinDate == N.CreDate)
                {
                    cTR.Cs.Add(new cTableCell { Value = "未落戶" });//加入日期
                    cTR.Cs.Add(new cTableCell { Value = "" });//離開日期
                }
                else
                {
                    cTR.Cs.Add(new cTableCell { Value = N.JoinDate.ToString(DateFormat) });//加入日期
                    cTR.Cs.Add(new cTableCell { Value = N.LeaveDate == N.CreDate ? "" : N.LeaveDate.ToString(DateFormat) });//離開日期
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
            return cTL;
        }

        public ActionResult Organize_Info_Account_List(int OID, int OIID)
        {
            GetViewBag();
            return View(GetOrganize_Info_Account_List(OID, OIID, null));
        }
        [HttpPost]
        public ActionResult Organize_Info_Account_List(int OID, int OIID, FormCollection FC)
        {
            GetViewBag();
            return View(GetOrganize_Info_Account_List(OID, OIID, FC));
        }
        #endregion
        #region 牧養組織與職分-匯出全部
        public ActionResult Organize_Info_Print()
        {
            GetViewBag();
            var Os = DC.Organize.Where(q => !q.DeleteFlag);
            var Os_All = Os.ToList();
            var OIs_All = (from q in DC.OrganizeInfo.Where(q => !q.DeleteFlag)
                           join p in Os
                           on q.OID equals p.OID
                           select q).ToList();

            int XCt = 0;
            ArrayList AL = new ArrayList();
            ArrayList ALS = new ArrayList();
            var O = Os_All.FirstOrDefault(q => q.ParentID == 0);
            while (O != null)
            {
                XCt++;
                ALS.Add(O.Title);
                if (!string.IsNullOrEmpty(O.JobTitle))
                    ALS.Add(O.JobTitle);
                O = Os_All.FirstOrDefault(q => q.ParentID == O.OID);
            };
            AL.Add((string[])ALS.ToArray(typeof(string)));

            var OIs = OIs_All.Where(q => q.OID == 8);
            int YCt = OIs.Count();
            OITable[,] T = new OITable[XCt, YCt];
            int Y = 0, X = XCt - 1;




            foreach (var OI in OIs.OrderBy(q => q.ParentID))
            {
                T[X, Y] = new OITable
                {
                    OID = OI.OID,
                    OIID = OI.OIID,
                    POID = OI.Organize.ParentID,
                    POIID = OI.ParentID,
                    Title = OI.Title + OI.Organize.Title,
                    ACName = string.IsNullOrEmpty(OI.Organize.JobTitle) ? "" : OI.Account.Name_First + OI.Account.Name_Last,
                    ActiceFlag = OI.ActiveFlag
                };
                Y++;
            }
            var O8 = Os_All.First(q => q.OID == 8);

            Y = 0;
            X--;

            for (int j = X; j >= 0; j--)
            {
                for (int i = 0; i < YCt; i++)
                {
                    if (T[j + 1, i] != null)
                    {
                        var OI = OIs_All.FirstOrDefault(q => q.OIID == T[j + 1, i].POIID && q.OID == T[j + 1, i].POID);
                        if (OI != null)
                        {
                            T[j, i] = new OITable
                            {
                                OID = OI.OID,
                                OIID = OI.OIID,
                                POID = OI.Organize.ParentID,
                                POIID = OI.ParentID,
                                Title = OI.Title + OI.Organize.Title,
                                ACName = string.IsNullOrEmpty(OI.Organize.JobTitle) ? "" : OI.Account.Name_First + OI.Account.Name_Last,
                                ActiceFlag = OI.ActiveFlag
                            };
                        }
                    }
                }

            }


            for (int j = 0; j < YCt; j++)
            {
                ALS = new ArrayList();
                for (int i = 0; i < XCt; i++)
                {
                    if (T[i, j] != null)
                    {
                        ALS.Add(T[i, j].Title);
                        if (!string.IsNullOrEmpty(T[i, j].ACName))
                            ALS.Add(T[i, j].ACName);
                    }
                    else
                    {
                        ALS.Add("--");
                        ALS.Add("--");
                    }
                }
                AL.Add((string[])ALS.ToArray(typeof(string)));
            }

            WriteExcelFromString("組織與職分管理列表", AL);
            SetAlert("已完成匯出", 1);
            return View();
        }
        private class OITable
        {
            public int OID = 0;
            public int OIID = 0;
            public int POID = 0;
            public int POIID = 0;
            public string Title = "";
            public string ACName = "";
            public bool ActiceFlag = false;
        }


        #endregion

        #region 主日聚會點-列表
        public class cMLL
        {
            public string ThisTitle = "";
            public string ControlName = "";
            public int OIID = 0;
            public List<SelectListItem> OIList = new List<SelectListItem>();
            public string TreeJson = "";
            public cTree Tree = new cTree();
            public int TreeLv = 0;
        }

        /// <summary>
        /// 聚會點
        /// </summary>
        /// <param name="OID">目前所在跟目錄的OIID</param>
        /// <param name="ID">無用</param>
        /// <returns></returns>
        public ActionResult Meeting_Location_List(int OID, int ID)
        {
            GetViewBag();
            cMLL MLL = new cMLL();
            MLL.ThisTitle = "";//隸屬組織
            MLL.ControlName = "ddl_OI2";
            MLL.OIList = new List<SelectListItem>();
            MLL.OIList.Add(new SelectListItem { Text = "全部選擇", Value = "0", Selected = OID == 0 });
            var POIs = DC.OrganizeInfo.Where(q => q.OID == 1 && !q.DeleteFlag).OrderBy(q => q.OIID);
            foreach (var OI in POIs)
                MLL.OIList.Add(new SelectListItem { Text = OI.Title, Value = OI.OIID.ToString(), Selected = OI.OIID == OID });

            cTree T0 = new cTree();
            T0.name = "主日聚會點";
            T0.title = "";
            T0.children = new List<cTree>();

            if (OID > 0)
                POIs = POIs.Where(q => q.OIID == OID).OrderBy(q => q.OIID);

            foreach (var POI in POIs)
            {
                MLL.ThisTitle = POI.Title;
                cTree T1 = new cTree();
                T1.name = POI.Title.Replace("基督教", "</br>基督教") + POI.Organize.Title;
                T1.title = "";
                T1.children = new List<cTree>();
                var OIs = DC.OrganizeInfo.Where(q => q.OID == 2 && !q.DeleteFlag && q.ParentID == POI.OIID).OrderBy(q => q.OIID);
                foreach (var OI in OIs)
                {
                    var MLSs = DC.Meeting_Location_Set.Where(q => q.SetType == 0 && q.OIID == OI.OIID && !q.DeleteFlag && !q.Meeting_Location.DeleteFlag).Select(q => q.Meeting_Location).OrderBy(q => q.MLID);
                    cTree T2 = new cTree();
                    T2.name = OI.Title + OI.Organize.Title + " (" + MLSs.Count() + ")";
                    T2.title = "新增";
                    T2.url = "/Admin/OrganizeSet/Meeting_Location_Edit/" + OI.OIID + "/0";
                    T2.children = new List<cTree>();
                    foreach (var MLS in MLSs)
                    {
                        T2.children.Add(new cTree
                        {
                            name = MLS.Title,
                            title = "編輯",
                            url = "/Admin/OrganizeSet/Meeting_Location_Edit/" + OI.OIID + "/" + MLS.MLID,
                            css = MLS.ActiveFlag ? "td_O_Basic3_Active" : "td_O_Basic3_UnActive"
                        });
                    }
                    T1.children.Add(T2);
                }

                T0.children.Add(T1);
            }

            if (POIs.Count() == 1)
            {
                MLL.Tree = T0.children[0];
                MLL.TreeLv = 3;
                MLL.TreeJson = JsonConvert.SerializeObject(T0.children[0]);
            }
            else
            {
                MLL.ThisTitle = "全部主日聚會點";
                MLL.Tree = T0;
                MLL.TreeLv = 4;
                MLL.TreeJson = JsonConvert.SerializeObject(T0);
            }
            return View(MLL);
        }

        #endregion
        #region 主日聚會點-新增/編輯/刪除
        public class cMeeting_Location_Edit
        {
            public Meeting_Location cML = new Meeting_Location();
            public Meeting_Location_Set cMLS = new Meeting_Location_Set();
            public List<OIListSelect> OIParent = new List<OIListSelect>();
            public Contect C = new Contect();
            public Location L = new Location();
            public int OIID = 0;

            public List<SelectListItem> SH_List = new List<SelectListItem>();
            public List<SelectListItem> SM_List = new List<SelectListItem>();
            public List<SelectListItem> EH_List = new List<SelectListItem>();
            public List<SelectListItem> EM_List = new List<SelectListItem>();
            public string SH_ControlName = "ddl_SH", SM_ControlName = "ddl_SM", EH_ControlName = "ddl_EH", EM_ControlName = "ddl_EM";
        }

        public ActionResult Meeting_Location_Edit(int OID, int ID)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            return View(ReSetMeetingLocationInfo(OID, ID, null));
        }

        [HttpPost]

        public ActionResult Meeting_Location_Edit(int OID, int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            cMeeting_Location_Edit cMLE = ReSetMeetingLocationInfo(OID, ID, FC);
            if (cMLE.cML.Title == "")
                Error = "請輸入聚會點名稱</br>";
            else if (cMLE.cML.Title.Length > 18)
                Error = "聚會點名稱請輸入18字內</br>";
            else if (cMLE.cML.DeleteFlag)//刪除
            {
                var MLS = DC.Meeting_Location_Set.FirstOrDefault(q => !q.DeleteFlag && q.SetType == 0 && q.OIID == cMLE.OIID);
                if (MLS != null)
                    Error = "請先移除使用此聚會點的組織資料後再行刪除</br>";
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
                    cMLE.cMLS.OIID = OID;
                    cMLE.cMLS.MLID = cMLE.cML.MLID;
                    cMLE.cMLS.DeleteFlag = false;
                    cMLE.cMLS.CreDate = cMLE.cML.UpdDate;
                    DC.Meeting_Location_Set.InsertOnSubmit(cMLE.cMLS);
                }
                DC.SubmitChanges();


                cMLE.C.TargetType = 3;
                cMLE.C.TargetID = cMLE.cML.MLID;
                cMLE.C.ContectType = 0;
                cMLE.C.CheckFlag = false;
                cMLE.C.CreDate = DT;
                cMLE.C.CheckDate = DT;
                if (cMLE.C.CID == 0)
                    DC.Contect.InsertOnSubmit(cMLE.C);
                DC.SubmitChanges();

                cMLE.L.TargetType = 3;
                cMLE.L.TargetID = cMLE.cML.MLID;
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
                    cMLE.cMLS = DC.Meeting_Location_Set.FirstOrDefault(q => q.SetType == 0 && q.OIID == cMLE.cML.MLID);
                    if (cMLE.cMLS == null)
                    {
                        cMLE.cMLS = new Meeting_Location_Set
                        {
                            MLID = cMLE.cML.MLID,
                            SetType = 0,
                            OIID = OIID,
                            S_hour = 9,
                            S_minute = 0,
                            E_hour = 12,
                            E_minute = 0,
                            ActiveFlag = true,
                            DeleteFlag = false,
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = ACID
                        };
                    }
                    #endregion
                    #region 上層
                    cMLE.OIParent = new List<OIListSelect>();
                    int SortNo = 10;
                    var OIP = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == OIID && !q.DeleteFlag);
                    if (OIP != null)
                    {
                        int NowPID = OIP.ParentID;
                        var OIs = DC.OrganizeInfo.Where(q => q.ParentID == NowPID && !q.DeleteFlag);
                        do
                        {
                            if (OIs.Count() == 0 || SortNo < 8 || NowPID < 0)
                                break;
                            else
                            {
                                OIListSelect cLS = new OIListSelect();
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

                                        cLS.OIList.Add(new SelectListItem { Text = OI.Title + OI.Organize.Title, Value = OI.OIID.ToString(), Selected = true });
                                    }
                                    else
                                        cLS.OIList.Add(new SelectListItem { Text = OI.Title + OI.Organize.Title, Value = OI.OIID.ToString() });
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
                        cMLE.L = L;
                    else
                        cMLE.L = new Location();
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
                    cMLE.cML.ActiveFlag = true;
                    #region 上層
                    cMLE.OIParent = new List<OIListSelect>();
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
                                OIListSelect cLS = new OIListSelect();
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
                    cMLE.L = new Location();
                    cMLE.L.Address = "";
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
            cMLE.cML.SaveACID = ACID;
            if (FC != null)
            {
                cMLE.cML.Title = FC.Get("txb_Title");
                cMLE.cML.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                cMLE.cML.DeleteFlag = GetViewCheckBox(FC.Get("cbox_DeleteFlag"));
                cMLE.cMLS.OIID = cMLE.cML.MLID;//Convert.ToInt32(FC.Get("ddl_OIID_10"));
                cMLE.cMLS.S_hour = Convert.ToInt32(FC.Get(cMLE.SH_ControlName));
                cMLE.cMLS.S_minute = Convert.ToInt32(FC.Get(cMLE.SM_ControlName));
                cMLE.cMLS.E_hour = Convert.ToInt32(FC.Get(cMLE.EH_ControlName));
                cMLE.cMLS.E_minute = Convert.ToInt32(FC.Get(cMLE.EM_ControlName));
                cMLE.C.ContectValue = FC.Get("txb_PhoneNo");
                cMLE.C.ZID = Convert.ToInt32(FC.Get("ddl_PhoneZip"));
                if (FC.Get("ddl_Zip0") == "10")
                {
                    cMLE.L.ZID = Convert.ToInt32(FC.Get("ddl_Zip2"));
                    cMLE.L.Address = FC.Get("txb_Address0");
                }
                else if (FC.Get("ddl_Zip0") == "2")
                {
                    cMLE.L.ZID = Convert.ToInt32(FC.Get("ddl_Zip3"));
                    cMLE.L.Address = FC.Get("txb_Address1_1") + "\n" + FC.Get("txb_Address1_2");
                }
                else
                {
                    cMLE.L.ZID = Convert.ToInt32(FC.Get("ddl_Zip0"));
                    cMLE.L.Address = FC.Get("txb_Address2");
                }

            }
            return cMLE;
        }
        #endregion
        #region 主日聚會點-匯出
        public ActionResult Meeting_Location_Print(int OID, int ID)
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
            if (OID > 0)
                POIs = POIs.Where(q => q.OIID == OID);
            POIs = POIs.OrderBy(q => q.OIID);

            foreach (var POI in POIs)
            {
                var cMLs = from q in DC.OrganizeInfo.Where(q => q.ParentID == POI.OIID && q.OID == 2 && !q.DeleteFlag)
                           join p in DC.Meeting_Location_Set.Where(q => !q.DeleteFlag && q.SetType == 0)
                           on q.OIID equals p.OIID
                           select new { q.OIID, OITitle = q.Title, p.MLID, MLTitle = p.Meeting_Location.Title, p.ActiveFlag, p.CreDate, p.UpdDate, p.SaveACID, OTitle = q.Organize.Title };
                var ACs = (from q in DC.Account
                           join p in cMLs
                           on q.ACID equals p.SaveACID
                           select new { q.ACID, q.Name_First, q.Name_Last }).ToList();
                var Cs = (from q in DC.Contect.Where(q => q.TargetType == 3 && q.ContectType == 0)
                          join p in cMLs
                          on q.TargetID equals p.MLID
                          join r in DC.ZipCode
                          on q.ZID equals r.ZID
                          select new { p.MLID, q.ContectValue, Code = r.Code }).ToList();

                var Ls = (from q in DC.Location.Where(q => q.TargetType == 3)
                          join p in cMLs
                          on q.TargetID equals p.MLID
                          join r in DC.ZipCode
                          on q.ZipCode.ParentID equals r.ZID
                          select new { p.MLID, sZipCode = q.ZipCode.Code, County = r.Title, Area = q.ZipCode.Title, q.Address, ZipType = q.ZipCode.GroupName }).ToList();
                foreach (var cML in cMLs.OrderBy(q => q.OIID).ThenBy(q => q.MLID))
                {
                    ALS = new ArrayList();
                    ALS.Add(POI.Title + POI.Organize.Title);
                    ALS.Add(cML.OITitle + cML.OTitle);
                    ALS.Add(cML.MLTitle);
                    var C = Cs.FirstOrDefault(q => q.MLID == cML.MLID);
                    ALS.Add(C != null ? C.Code + " " + C.ContectValue : "");
                    var L = Ls.FirstOrDefault(q => q.MLID == cML.MLID);
                    if (L != null)
                    {
                        switch (L.ZipType)
                        {
                            case "縣市":
                                ALS.Add(L.Area + L.Address);
                                break;

                            case "鄉鎮市區"://台灣地址
                                ALS.Add(L.sZipCode + " " + L.County + L.Area + L.Address);
                                break;

                            case "洲"://國外
                                ALS.Add(L.Area + L.Address.Replace("%", ""));
                                break;

                            case "網路":
                                ALS.Add(L.Area + L.Address);
                                break;

                            default:
                                ALS.Add(L.Address);
                                break;
                        }
                    }
                    //ALS.Add(L != null ? (L.sZipCode + " " + L.County + L.Area + L.Address) : "");
                    ALS.Add(cML.ActiveFlag ? "啟用中" : "");
                    ALS.Add(cML.CreDate.ToString(DateTimeFormat));
                    ALS.Add(cML.CreDate == cML.UpdDate ? "" : cML.UpdDate.ToString(DateTimeFormat));
                    var AC = ACs.FirstOrDefault(q => q.ACID == cML.SaveACID);
                    if (AC != null)
                        ALS.Add(AC.Name_First + AC.Name_Last);
                    else
                        ALS.Add("--");
                    AL.Add((string[])ALS.ToArray(typeof(string)));
                }
            }


            WriteExcelFromString("主日聚會點列表", AL);
            SetAlert("已完成匯出", 1, "/Admin/OrganizeSet/Meeting_Location_List/" + OID + "/0");
            return View();
        }
        #endregion
    }
}