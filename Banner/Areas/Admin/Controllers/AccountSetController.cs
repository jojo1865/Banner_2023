using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Admin.Controllers
{
    public class AccountSetController : PublicClass
    {
        // GET: Admin/AccountSet
        public ActionResult Index()
        {
            return View();
        }

        #region 牧養名單成人列表
        public class cAccount_List
        {
            public cTableList cTL = new cTableList();
            public List<ListSelect> LS_O = new List<ListSelect>();
            public string sAddURL = "/Admin/AccountSet/Account_Aldult_Edit/0";
            public string Name = "";
            public string CellPhone = "";
            public ListSelect LS_Sex = new ListSelect();
            public ListSelect LS_Baptized = new ListSelect();

            public cAccount_List()
            {
                if (LS_Sex == null)
                    LS_Sex = new ListSelect();
                LS_Sex.ControlName = "ddl_Sex";
                LS_Sex.Title = "性別";
                LS_Sex.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
                LS_Sex.ddlList.Add(new SelectListItem { Text = "男", Value = "1" });
                LS_Sex.ddlList.Add(new SelectListItem { Text = "女", Value = "2" });

                if (LS_Baptized == null)
                    LS_Baptized = new ListSelect();
                LS_Baptized.ControlName = "ddl_Baptized";
                LS_Baptized.Title = "受洗狀態";
                LS_Baptized.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
                LS_Baptized.ddlList.Add(new SelectListItem { Text = "已受洗", Value = "1" });
                LS_Baptized.ddlList.Add(new SelectListItem { Text = "未受洗", Value = "2" });
            }

        }
        public cAccount_List sAccount_Aldult_List(FormCollection FC)
        {
            cAccount_List cAL = new cAccount_List();
            cAL.cTL = new cTableList();
            if(FC!=null)
            {
                cAL.Name = FC.Get("txb_Name");
                cAL.CellPhone = FC.Get("txb_CellPhone");

                cAL.LS_Sex.ddlList.ForEach(q => q.Selected = false);
                cAL.LS_Sex.ddlList.First(q => q.Value == FC.Get("ddl_Sex")).Selected = true;

                cAL.LS_Baptized.ddlList.ForEach(q => q.Selected = false);
                cAL.LS_Baptized.ddlList.First(q => q.Value == FC.Get("ddl_Baptized")).Selected = true;
            }
            



            return cAL;
        }
        public cTableList GetAccountTable(int iType, FormCollection FC)
        {
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
            cTL.ItemID = "";

            cTL.NumCut = iNumCut;
            cTL.Rs = new List<cTableRow>();

            var Ns_ = DC.Account.Where(q => !q.DeleteFlag);
            switch (iType)
            {
                case 1://成人
                    cTL.NowURL = "/Admin/AccountSet/Account_Aldult_List";
                    Ns_ = Ns_.Where(q => DT.Year - q.Birthday.Year > 13);
                    break;

                case 2://兒童
                    cTL.NowURL = "/Admin/AccountSet/Account_Childen_List";
                    Ns_ = Ns_.Where(q => DT.Year - q.Birthday.Year <= 13);
                    break;

                case 3://新人
                    cTL.NowURL = "/Admin/AccountSet/Account_New_List";
                    Ns_ = from q in DC.M_OI_Account.Where(q => q.OIID == 1 && !q.DeleteFlag && q.ActiveFlag).GroupBy(q => q.ACID).Select(q => q.Key)
                         join p in Ns_
                         on q equals p.ACID
                         select p;

                    break;

                case 4://受洗
                    cTL.NowURL = "/Admin/AccountSet/Account_Baptized_List";
                    Ns_ = from q in DC.Baptized.Where(q => !q.ImplementFlag && !q.DeleteFlag).GroupBy(q => q.ACID).Select(q => q.Key)
                         join p in Ns_
                         on q equals p.ACID
                         select p;

                    break;
            }
            List<Account> Ns = null;
            if (FC != null)
            {
                if (!string.IsNullOrEmpty(FC.Get("txb_Name")))
                    Ns_ = Ns_.Where(q => q.Name.Contains(FC.Get("txb_Name")));

                if (!string.IsNullOrEmpty(FC.Get("txb_CellPhone")))
                    Ns_ = from q in Ns_
                          join p in DC.Contect.Where(q => q.TargetType == 2 && q.ContectType == 1 && q.ContectValue.Contains(FC.Get("txb_CellPhone"))).GroupBy(q => q.TargetID)
                          on q.ACID equals p.Key
                          select q;

                if (FC.Get("ddl_Sex") != "0")
                    Ns_ = Ns_.Where(q => q.ManFlag == (FC.Get("ddl_Sex") == "1"));
                if (FC.Get("ddl_Baptized") != "0")
                    Ns_ = from q in Ns_
                          join p in DC.Baptized.Where(q => q.ImplementFlag == (FC.Get("ddl_Baptized") == "1") && !q.DeleteFlag).GroupBy(q => q.ACID).Select(q => q.Key)
                          on q.ACID equals p
                          select q;
                Ns = Ns_.ToList();
                int iOIID = 0, j = 0;
                for (int i = 0; i < 10; i++)
                {
                    string str = FC.Get("ddl_" + i);
                    if (!string.IsNullOrEmpty(str))
                    {
                        try
                        {
                            j = Convert.ToInt32(str);
                            if (j > 0)
                                iOIID = j;
                        }
                        catch { }
                    }
                }
                if (iOIID > 0)
                {
                    var OIs = new List<OrganizeInfo>();
                    GetThisOIsFromTree(ref OIs, iOIID);
                    var IDs = OIs.GroupBy(q => q.OIID).Select(q => q.Key).ToList();
                    var Ms = (from q in DC.M_OI_Account.Where(q => !q.DeleteFlag && (q.JoinDate >= q.CreDate && q.JoinDate.Date <= DT.Date) && (q.LeaveDate == q.CreDate || q.LeaveDate.Date >= DT.Date)).ToList()
                              join p in IDs
                              on q.OIID equals p
                              select q).ToList();
                    Ns = (from q in Ns.ToList()
                          join p in Ms.GroupBy(q => q.ACID).ToList()
                          on q.ACID equals p.Key
                          select q).ToList();
                }
            }
            else
                Ns = Ns_.ToList();

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "選擇", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "編輯", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "牧區" });
            TopTitles.Add(new cTableCell { Title = "督區" });
            TopTitles.Add(new cTableCell { Title = "區" });
            TopTitles.Add(new cTableCell { Title = "小組" });
            TopTitles.Add(new cTableCell { Title = "姓名" });
            TopTitles.Add(new cTableCell { Title = "性別" });
            TopTitles.Add(new cTableCell { Title = "受洗狀態" });
            TopTitles.Add(new cTableCell { Title = "行動電話" });

            cTL.Rs.Add(SetTableRowTitle(TopTitles));

            cTL.TotalCt = Ns.Count();
            cTL.MaxNum = GetMaxNum(cTL.TotalCt, cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.ACID).Skip((iNowPage - 1) * cTL.NumCut).Take(cTL.NumCut).ToList();
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "checkbox", Value = "false", ControlName = "cbox_S" + N.ACID, CSS = "form-check-input cbox_S" });//選擇
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_Aldult_Edit/" + N.ACID, Target = "_black", Value = "編輯" });//
                var OI_8 = DC.M_OI_Account.Where(q => q.ACID == N.ACID && q.OrganizeInfo.OID == 8 && q.ActiveFlag && !q.DeleteFlag && (q.JoinDate == q.CreDate || q.JoinDate.Date <= DT.Date) && (q.LeaveDate == q.CreDate || q.LeaveDate.Date >= DT.Date)).FirstOrDefault();
                if (OI_8 != null)
                {
                    var OI_7 = DC.OrganizeInfo.FirstOrDefault(q => !q.DeleteFlag && q.OID == 7 && q.OIID == OI_8.OrganizeInfo.ParentID);
                    if (OI_7 != null)
                    {
                        var OI_6 = DC.OrganizeInfo.FirstOrDefault(q => !q.DeleteFlag && q.OID == 6 && q.OIID == OI_7.ParentID);
                        if (OI_6 != null)
                        {
                            var OI_5 = DC.OrganizeInfo.FirstOrDefault(q => !q.DeleteFlag && q.OID == 5 && q.OIID == OI_6.ParentID);
                            if (OI_5 != null)
                            {
                                cTR.Cs.Add(new cTableCell { Value = OI_5.Title });//牧區
                            }
                            else
                                cTR.Cs.Add(new cTableCell { Value = "--" });//牧區
                            cTR.Cs.Add(new cTableCell { Value = OI_6.Title });//督區
                        }
                        else
                        {
                            cTR.Cs.Add(new cTableCell { Value = "--" });//牧區
                            cTR.Cs.Add(new cTableCell { Value = "--" });//督區
                        }
                        cTR.Cs.Add(new cTableCell { Value = OI_7.Title });//區
                    }
                    else
                    {
                        cTR.Cs.Add(new cTableCell { Value = "--" });//牧區
                        cTR.Cs.Add(new cTableCell { Value = "--" });//督區
                        cTR.Cs.Add(new cTableCell { Value = "--" });//區
                    }
                    cTR.Cs.Add(new cTableCell { Value = OI_8.OrganizeInfo.Title });//小組
                }
                else
                {
                    cTR.Cs.Add(new cTableCell { Value = "" });//牧區
                    cTR.Cs.Add(new cTableCell { Value = "" });//督區
                    cTR.Cs.Add(new cTableCell { Value = "" });//區
                    cTR.Cs.Add(new cTableCell { Value = "" });//小組
                }

                cTR.Cs.Add(new cTableCell { Value = N.Name.ToString() });//姓名
                cTR.Cs.Add(new cTableCell { Value = N.ManFlag ? "男" : "女" });//性別

                var B = DC.Baptized.Where(q => q.ACID == N.ACID && !q.DeleteFlag).OrderByDescending(q => q.BID).FirstOrDefault();
                if (B == null)
                    cTR.Cs.Add(new cTableCell { Value = "--" });//受洗狀態
                else if (!B.ImplementFlag)
                    cTR.Cs.Add(new cTableCell { Value = "預計於" + B.BaptismDate.ToShortDateString() + "受洗" });//受洗狀態
                else
                    cTR.Cs.Add(new cTableCell { Value = "已於" + B.BaptismDate.ToShortDateString() + "受洗" });//受洗狀態

                var C = DC.Contect.Where(q => q.TargetType == 2 && q.ContectType == 1 && q.TargetID == N.ACID).OrderByDescending(q => q.CID).FirstOrDefault();
                cTR.Cs.Add(new cTableCell { Value = C != null ? C.ContectValue : "" });//行動電話
                cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            return cTL;
        }
        public ActionResult Account_Aldult_List()
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(null);
            cAL.LS_O = GetO(0);
            cAL.cTL = GetAccountTable(1, null);

            return View(cAL);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Account_Aldult_List(FormCollection FC)
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(FC);
            int iOIID = 0, j = 0;
            for (int i = 0; i < 10; i++)
            {
                string str = FC.Get("ddl_" + i);
                if (!string.IsNullOrEmpty(str))
                {
                    try
                    {
                        j = Convert.ToInt32(str);
                        if (j > 0)
                            iOIID = j;
                    }
                    catch { }
                }
            }

            cAL.LS_O = GetO(iOIID);
            cAL.cTL = GetAccountTable(1, FC);

            return View(cAL);
        }
        #endregion
    }
}