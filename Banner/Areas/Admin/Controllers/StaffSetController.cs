using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using static Banner.Areas.Admin.Controllers.StaffSetController;

namespace Banner.Areas.Admin.Controllers
{
    public class StaffSetController : PublicClass
    {
        #region 課程-分類-列表
        public class cCategory_List
        {
            public int ActiveType = -1;
            public string sKey = "";
            public cTableList cTL = new cTableList();
        }
        public cCategory_List GetCategory_List(FormCollection FC)
        {
            cCategory_List N = new cCategory_List();
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
            var Ns = DC.OrganizeStaff_Category.Where(q => !q.DeleteFlag);
            if (N.sKey != "")
                Ns = Ns.Where(q => q.Title.Contains(N.sKey));

            if (N.ActiveType >= 0)
                Ns = Ns.Where(q => q.ActiveFlag == (N.ActiveType == 1));


            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "分類名稱" });
            TopTitles.Add(new cTableCell { Title = "事工團數量", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "狀態" });


            N.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            N.cTL.TotalCt = Ns.Count();
            N.cTL.MaxNum = GetMaxNum(N.cTL.TotalCt, N.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.OSCID).Skip((iNowPage - 1) * N.cTL.NumCut).Take(N.cTL.NumCut);

            foreach (var N_ in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/StaffSet/Category_Edit/" + N_.OSCID, Target = "_self", Value = "編輯" });//編輯
                cTR.Cs.Add(new cTableCell { Value = N_.Title });//課程分類名稱
                //課程數量
                int iCt = N_.OrganizeStaff.Count(q => !q.DeleteFlag);
                if (iCt == 0)
                    cTR.Cs.Add(new cTableCell { Type = "link", URL = "/Admin/StaffSet/Staff_Edit/0?OSCID=" + N_.OSCID, Target = "_self", Value = "新增" });//課程數量
                else
                    cTR.Cs.Add(new cTableCell { Type = "link", URL = "/Admin/StaffSet/Staff_List/" + N_.OSCID, Target = "_self", Value = iCt.ToString() });
                cTR.Cs.Add(new cTableCell { Value = N_.ActiveFlag ? "啟用" : "停用" });//狀態


                N.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion


            return N;
        }
        [HttpGet]
        public ActionResult Category_List()
        {
            GetViewBag();
            return View(GetCategory_List(null));
        }
        [HttpPost]
        public ActionResult Category_List(FormCollection FC)
        {
            GetViewBag();
            return View(GetCategory_List(FC));
        }
        #endregion
        #region 課程-分類-編輯

        public OrganizeStaff_Category GetCategory_Edit(int ID, FormCollection FC)
        {
            #region 資料庫資料帶入
            OrganizeStaff_Category N = DC.OrganizeStaff_Category.FirstOrDefault(q => q.OSCID == ID);
            if (N == null)
            {
                N = new OrganizeStaff_Category
                {
                    Title = "",
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
                N.Title = FC.Get("txb_Title");
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
        [ValidateAntiForgeryToken]
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
                if (N.OSCID == 0)
                    DC.OrganizeStaff_Category.InsertOnSubmit(N);
                DC.SubmitChanges();
                SetAlert("存檔完成", 1, "/Admin/StaffSet/Category_List");
            }

            return View(N);
        }
        #endregion
    }
}