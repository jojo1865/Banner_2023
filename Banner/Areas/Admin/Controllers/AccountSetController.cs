using Banner.Models;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlTypes;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Web;
using System.Web.Mvc;
using System.Web.Security.AntiXss;
using System.Web.UI.WebControls;
using static Banner.Areas.Admin.Controllers.AccountSetController;
using static Banner.Areas.Admin.Controllers.AccountSetController;

namespace Banner.Areas.Admin.Controllers
{
    public class AccountSetController : PublicClass
    {
        // GET: Admin/AccountSet
        public ActionResult Index()
        {
            return View();
        }

        #region 牧養名單-列表
        public class cAccount_List : PublicClass
        {
            public cTableList cTL = new cTableList();
            public string sAddURL = "/Admin/AccountSet/Account_Aldult_Edit/0";
            public string Name = "";
            public string CellPhone = "";
            public int CellPhoneZipID = 0;
            public int Sex = 0;
            public int OID = 8;
            public string OTitle = "";
            public string TargetDate = "";
            public bool bPoorFlag = false;
            public ListSelect LS_Baptized = new ListSelect();
            public ListSelect LS_Group = new ListSelect();

            public cAccount_List()
            {
                if (LS_Baptized == null)
                    LS_Baptized = new ListSelect();
                LS_Baptized.ControlName = "ddl_Baptized";
                LS_Baptized.Title = "受洗狀態";
                LS_Baptized.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
                LS_Baptized.ddlList.Add(new SelectListItem { Text = "已受洗", Value = "1" });
                LS_Baptized.ddlList.Add(new SelectListItem { Text = "未受洗", Value = "2" });

                if (LS_Group == null)
                    LS_Group = new ListSelect();
                LS_Group.ControlName = "ddl_Group";
                LS_Group.Title = "入組狀態";
                LS_Group.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
                for (int i = 0; i < JoinTitle.Length; i++)
                    LS_Group.ddlList.Add(new SelectListItem { Text = JoinTitle[i], Value = (i + 1).ToString(), Disabled = i == 2 });
            }

        }

        public cAccount_List sAccount_Aldult_List(FormCollection FC)
        {
            cAccount_List cAL = new cAccount_List();
            cAL.cTL = new cTableList();
            if (FC != null)
            {
                cAL.Name = FC.Get("txb_Name");
                cAL.CellPhone = FC.Get("txb_CellPhone");
                cAL.Sex = Convert.ToInt32(FC.Get("rbl_Sex"));
                cAL.CellPhone = FC.Get("txb_PhoneNo");
                cAL.CellPhoneZipID = Convert.ToInt32(FC.Get("ddl_PhoneZip"));
                cAL.TargetDate = FC.Get("txb_TargetDate");
                cAL.bPoorFlag = GetViewCheckBox(FC.Get("cbox_PoorFlag"));
                if (FC.Get("ddl_Baptized") != null)
                {
                    cAL.LS_Baptized.ddlList.ForEach(q => q.Selected = false);
                    cAL.LS_Baptized.ddlList.First(q => q.Value == FC.Get("ddl_Baptized")).Selected = true;
                }

                if (FC.Get("ddl_Group") != null)
                {
                    cAL.LS_Group.ddlList.ForEach(q => q.Selected = false);
                    cAL.LS_Group.ddlList.First(q => q.Value == FC.Get("ddl_Group")).Selected = true;
                }

                if (FC.Get("txb_OTitle") != null)
                {
                    cAL.OTitle = FC.Get("txb_OTitle");
                }

            }

            return cAL;
        }
        public cTableList GetAccountTable(int iType, FormCollection FC)
        {
            cTableList cTL = new cTableList();
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            ACID = GetACID();
            cTL.Title = "";
            cTL.NowPage = iNowPage;
            cTL.NumCut = iNumCut;
            cTL.Rs = new List<cTableRow>();

            var Ns = DC.Account.Where(q => !q.DeleteFlag);
            #region 後台全職同工可檢視旌旗資料判斷
            {
                if (ACID != 1)
                {
                    var MOI2 = DC.M_OI2_Account.FirstOrDefault(q => q.OIID == 1 && q.ACID == ACID && !q.DeleteFlag && q.ActiveFlag);
                    if (MOI2 == null)//他沒有全部的權限
                    {
                        MOI2 = DC.M_OI2_Account.FirstOrDefault(q => q.OIID == 2 && q.ACID == ACID && !q.DeleteFlag && q.ActiveFlag);
                        if (MOI2 == null)//他沒有看未入組的會員權限=需要依據他的旌旗角色篩選
                        {
                            MOI2 = DC.M_OI2_Account.FirstOrDefault(q => q.ACID == ACID && !q.DeleteFlag && q.ActiveFlag);
                            if (MOI2 == null)//不具備旌旗的權限
                            {
                                Ns = Ns.Where(q => q.ACID == 0);
                            }
                            else
                            {
                                var OI8_ACIDs = from q in (from q in DC.M_OI2_Account.Where(q => q.ACID == ACID && !q.DeleteFlag && q.ActiveFlag)
                                                           join p in GetMOIAC(8, 0, 0)
                                                           on q.OIID equals p.OrganizeInfo.OI2_ID
                                                           select p)
                                                group q by new { q.ACID } into g
                                                select new { g.Key.ACID };

                                Ns = from q in OI8_ACIDs
                                     join p in Ns
                                     on q.ACID equals p.ACID
                                     select p;
                            }
                        }
                        else
                        {
                            var M_ACIDs = from q in DC.M_OI_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.CreDate != q.JoinDate && q.OIID > 0)
                                          group q by new { q.ACID } into g
                                          select new { g.Key.ACID };
                            var ACIDs = from q in Ns
                                        group q by new { q.ACID } into g
                                        select new { g.Key.ACID };
                            Ns = from q in ACIDs.Except(M_ACIDs)
                                 join p in Ns
                                 on q.ACID equals p.ACID
                                 select p;
                        }
                    }
                }

            }
            #endregion

            switch (iType)
            {
                case 1://成人
                    {
                        cTL.NowURL = "/Admin/AccountSet/Account_Aldult_List";
                        Ns = Ns.Where(q => DT.Year - q.Birthday.Year > iChildAge && q.Birthday != q.CreDate);
                    }
                    break;

                case 2://兒童
                    {
                        cTL.NowURL = "/Admin/AccountSet/Account_Childen_List";
                        Ns = Ns.Where(q => DT.Year - q.Birthday.Year <= iChildAge && q.Birthday != q.CreDate);
                    }
                    break;

                case 3://新人
                    {
                        cTL.NowURL = "/Admin/AccountSet/Account_New_List";
                        //先過濾仍在小組內的人
                        var M_OI_AGs = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag)
                                       group q by new { q.ACID } into g
                                       select new { g.Key.ACID, MaxID = g.Max(q => q.MID), Ct = g.Count() };
                        var M_OI_As = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag)
                                      join p in M_OI_AGs
                                      on q.MID equals p.MaxID
                                      select q;
                        var AIDs = (Ns.Select(q => q.ACID)).Except(M_OI_As.Where(q => q.OIID > 1 && q.JoinDate != q.CreDate).Select(q => q.ACID));

                    }
                    break;

                case 4://受洗
                    {
                        cTL.NowURL = "/Admin/AccountSet/Account_Baptized_List";
                        //Ns = Ns.Where(q => DT.Year - q.Birthday.Year > iChildAge && q.BaptizedType == 0);//小朋友不能受洗
                        //Ns = Ns.Where(q => q.BaptizedType == 0);//小朋友可以受洗

                        //小朋友不在受洗名單範圍內,且不考慮未入組名單
                        /*Ns = from q in Ns.Where(q => DT.Year - q.Birthday.Year > iChildAge && q.BaptizedType == 0)
                             join p in GetMOIAC().Where(q => q.OIID > 1).GroupBy(q => q.ACID).Select(q => q.Key)
                             on q.ACID equals p
                             select q;*/
                        //排除未被指定受洗日期的會員
                        Ns = from q in DC.Baptized.Where(q => !q.ImplementFlag && !q.DeleteFlag && q.BaptismDate != q.CreDate).GroupBy(q => q.ACID).Select(q => q.Key)
                             join p in Ns
                             on q equals p.ACID
                             select p;
                    }
                    break;

                case 5://會友
                    {
                        cTL.NowURL = "/Admin/AccountSet/Account_Aldult_List";
                        Ns = from q in Ns.Where(q => DT.Year - q.Birthday.Year > iChildAge && q.Birthday != q.CreDate)
                             join p in DC.M_Role_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.RID == 2).GroupBy(q => q.ACID).Select(q => q.Key)
                             on q.ACID equals p
                             select q;
                    }
                    break;

                case 6://申請轉換小組
                    {
                        cTL.NowURL = "/Admin/AccountSet/Account_GroupOrder_List";
                        var COIGs = from q in DC.Change_OI_Order.Where(q => !q.DeleteFlag)
                                    group q by new { q.ACID } into g
                                    select new { g.Key.ACID, MaxID = g.Max(q => q.COIOID) };
                        var GOIs = from q in COIGs
                                   join p in DC.Change_OI_Order.Where(q => !q.DeleteFlag)
                                   on q.MaxID equals p.COIOID
                                   select p;

                        Ns = from q in Ns
                             join p in GOIs
                             on q.ACID equals p.ACID
                             select q;

                    }
                    break;
            }

            if (FC != null)
            {
                if (!string.IsNullOrEmpty(FC.Get("txb_Name")))
                    Ns = Ns.Where(q => (q.Name_First + q.Name_Last).Contains(FC.Get("txb_Name")));

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

                string sSix = FC.Get("rbl_Sex");
                if (FC.Get("rbl_Sex") != "0")
                    Ns = Ns.Where(q => q.ManFlag == (FC.Get("rbl_Sex") == "1"));
                if (!string.IsNullOrEmpty(FC.Get("txb_OTitle")))
                {
                    string OTitle = FC.Get("txb_OTitle");
                    int OID = 8;
                    //int OID = Convert.ToInt32(FC.Get("ddl_O"));
                    var MOIAGs = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag && q.OrganizeInfo.Title.Contains(OTitle) && q.OrganizeInfo.OID == OID)
                                 group q by new { q.ACID } into g
                                 select new { g.Key.ACID };
                    Ns = from q in Ns
                         join p in MOIAGs
                         on q.ACID equals p.ACID
                         select q;
                }

                //中低收入戶篩選
                if (GetViewCheckBox(FC.Get("cbox_PoorFlag")))
                {
                    var Poors = from q in DC.Account_Note.Where(q => q.NoteType == 4 && !q.DeleteFlag)
                                group q by new { q.ACID } into g
                                select new { g.Key.ACID };

                    Ns = from q in Ns
                         join p in Poors
                         on q.ACID equals p.ACID
                         select q;
                }
                switch (iType)
                {
                    case 1://成人
                    case 5://按立會友
                    case 2://兒童
                    case 6://申請轉換小組
                        {
                            if (FC.Get("ddl_Baptized") != "0")
                                Ns = from q in Ns
                                     join p in DC.Baptized.Where(q => q.ImplementFlag == (FC.Get("ddl_Baptized") == "1") && !q.DeleteFlag).GroupBy(q => q.ACID).Select(q => q.Key)
                                     on q.ACID equals p
                                     select q;
                        }
                        break;

                    case 3://新人
                        if (FC.Get("ddl_Group") != "0")
                        {
                            var M_OI_AGs = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag)
                                           group q by new { q.ACID } into g
                                           select new { g.Key.ACID, MaxID = g.Max(q => q.MID), Ct = g.Count() };
                            var M_OI_As = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag)
                                          join p in M_OI_AGs
                                          on q.MID equals p.MaxID
                                          select new { Ms = q, p.Ct };

                            switch (FC.Get("ddl_Group"))
                            {
                                case "1"://無意願
                                    {
                                        //不存在M_OI_As的名單中
                                        var AIDs = (Ns.Select(q => q.ACID)).Except(M_OI_As.Select(q => q.Ms.ACID));
                                        Ns = from q in Ns
                                             join p in AIDs
                                             on q.ACID equals p
                                             select q;
                                    }
                                    break;

                                case "2"://已入組未落戶
                                    {
                                        //存在M_OI_As的名單中
                                        Ns = from q in Ns
                                             join p in M_OI_As.Where(q => q.Ms.OIID > 1 && q.Ms.JoinDate == q.Ms.CreDate)
                                             on q.ACID equals p.Ms.ACID
                                             select q;
                                    }
                                    break;

                                case "4"://被退回(未分發)
                                    {
                                        Ns = from q in Ns
                                             join p in M_OI_As.Where(q => q.Ms.OIID == 1 && q.Ms.JoinDate == q.Ms.CreDate && q.Ct > 1)
                                             on q.ACID equals p.Ms.ACID
                                             select q;
                                    }
                                    break;

                                case "5"://跟進中(未分發)
                                    {
                                        Ns = from q in Ns
                                             join p in M_OI_As.Where(q => q.Ms.OIID == 1 && q.Ms.JoinDate == q.Ms.CreDate && q.Ct == 1)
                                             on q.ACID equals p.Ms.ACID
                                             select q;
                                    }
                                    break;
                            }
                        }
                        break;

                    case 4://受洗
                        {

                            if (FC.Get("ddl_Baptized") == "1")//有日期
                            {
                                if (FC.Get("txb_TargetDate") != "")
                                {
                                    DateTime DT_ = DT.Date;
                                    try
                                    {
                                        DT_ = Convert.ToDateTime(FC.Get("txb_TargetDate"));
                                        Ns = from q in DC.Baptized.Where(q => !q.ImplementFlag && !q.DeleteFlag && q.BaptismDate.Date == DT_.Date).GroupBy(q => q.ACID).Select(q => q.Key)
                                             join p in Ns.Where(q => DT.Year - q.Birthday.Year > iChildAge)
                                             on q equals p.ACID
                                             select p;
                                    }
                                    catch
                                    {

                                    }
                                }


                            }
                            /*else if (FC.Get("ddl_Baptized") == "2")//沒日期
                            {
                                Ns = from q in DC.Baptized.Where(q => !q.ImplementFlag && !q.DeleteFlag && q.BaptismDate == q.CreDate).GroupBy(q => q.ACID).Select(q => q.Key)
                                     join p in Ns.Where(q => DT.Year - q.Birthday.Year > iChildAge)
                                     on q equals p.ACID
                                     select p;

                            }*/

                        }
                        break;
                }

                int iOID = Convert.ToInt32(FC.Get("ddl_O"));
                string OITitle = FC.Get("txb_OITitle");
                if (iOID > 0 || !string.IsNullOrEmpty(OITitle))
                {
                    var OIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag);
                    if (!string.IsNullOrEmpty(OITitle))
                        OIs = OIs.Where(q => q.Title.Contains(OITitle));
                    if (iOID > 0)
                        OIs = OIs.Where(q => q.OID == iOID);
                    var IDs = OIs.GroupBy(q => q.OIID).Select(q => q.Key);
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
            switch (iType)
            {
                case 1://成人
                case 5://按立會友
                case 2://兒童
                    TopTitles.Add(new cTableCell { Title = "選擇", WidthPX = 50 });
                    TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 200 });
                    TopTitles.Add(new cTableCell { Title = "牧區" });
                    TopTitles.Add(new cTableCell { Title = "督區" });
                    TopTitles.Add(new cTableCell { Title = "區" });
                    TopTitles.Add(new cTableCell { Title = "小組" });
                    TopTitles.Add(new cTableCell { Title = "姓名" });
                    TopTitles.Add(new cTableCell { Title = "性別", WidthPX = 50 });
                    TopTitles.Add(new cTableCell { Title = "受洗狀態" });
                    TopTitles.Add(new cTableCell { Title = "行動電話" });
                    break;

                /*
                    TopTitles.Add(new cTableCell { Title = "選擇", WidthPX = 50 });
                    TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
                    TopTitles.Add(new cTableCell { Title = "姓名" });
                    TopTitles.Add(new cTableCell { Title = "性別", WidthPX = 50 });
                    TopTitles.Add(new cTableCell { Title = "受洗狀態" });
                    TopTitles.Add(new cTableCell { Title = "主日聚會點" });
                    break;*/

                case 3://新人
                    TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
                    TopTitles.Add(new cTableCell { Title = "姓名" });
                    TopTitles.Add(new cTableCell { Title = "性別", WidthPX = 50 });
                    TopTitles.Add(new cTableCell { Title = "生日" });
                    TopTitles.Add(new cTableCell { Title = "手機" });
                    TopTitles.Add(new cTableCell { Title = "入組狀態" });
                    TopTitles.Add(new cTableCell { Title = "備註" });
                    break;

                case 4://受洗
                    TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
                    TopTitles.Add(new cTableCell { Title = "姓名" });
                    TopTitles.Add(new cTableCell { Title = "性別", WidthPX = 50 });
                    TopTitles.Add(new cTableCell { Title = "生日" });
                    TopTitles.Add(new cTableCell { Title = "手機" });
                    TopTitles.Add(new cTableCell { Title = "小組名稱" });
                    TopTitles.Add(new cTableCell { Title = "預定受洗日期" });
                    break;

                case 6://申請轉換小組
                    TopTitles.Add(new cTableCell { Title = "選擇", WidthPX = 100 });
                    TopTitles.Add(new cTableCell { Title = "姓名" });
                    TopTitles.Add(new cTableCell { Title = "原始小組" });
                    TopTitles.Add(new cTableCell { Title = "新申請小組" });
                    TopTitles.Add(new cTableCell { Title = "申請日期" });
                    TopTitles.Add(new cTableCell { Title = "目前狀態" });
                    break;
            }


            cTL.Rs.Add(SetTableRowTitle(TopTitles));

            cTL.TotalCt = Ns.Count();
            cTL.MaxNum = GetMaxNum(cTL.TotalCt, cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.ACID).Skip((iNowPage - 1) * cTL.NumCut).Take(cTL.NumCut);
            foreach (var N in Ns)
            {
                //手機
                var Cons = DC.Contect.Where(q => q.TargetType == 2 && q.ContectType == 1 && q.TargetID == N.ACID).OrderByDescending(q => q.CID);
                string CellPhone = Cons.Count() > 0 ? string.Join(",", Cons.Select(q => q.ContectValue)) : "--";
                cTableRow cTR = new cTableRow();
                cTR.SortNo = N.ACID;
                switch (iType)
                {
                    case 1://成人
                    case 5://按立會友
                    case 2://兒童
                        {
                            cTR.Cs.Add(new cTableCell { Type = "checkbox", Value = "false", ControlName = "cbox_S" + N.ACID, CSS = "form-check-input cbox_S" });//選擇
                            cTableCell cTC = new cTableCell();
                            cTC.cTCs = new List<cTableCell>();
                            if (iType == 1 || iType == 2)
                            {
                                if (ACID == 1)
                                    cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_Aldult_Edit/" + N.ACID, Target = "_self", Value = "編輯" });//編輯
                                else
                                    cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_Aldult_Info/" + N.ACID, Target = "_self", Value = "檢視" });//檢視
                            }
                            if (iType == 1 || iType == 5)
                                cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_Emtitle_Edit/" + N.ACID, Target = "_self", Value = "按立" });//檢視
                            cTR.Cs.Add(cTC);

                            var OI_8 = GetMOIAC(8, 0, N.ACID).FirstOrDefault(q => q.JoinDate != q.CreDate);//確定有入組再列
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

                            if (ACID == 1) //免去識別
                                cTR.Cs.Add(new cTableCell { Value = (N.Name_First + N.Name_Last) });
                            else if (!string.IsNullOrEmpty(N.Name_Last)) //去識別
                                cTR.Cs.Add(new cTableCell { Value = (N.Name_First + new string('*', N.Name_Last.Length)) });
                            else
                                cTR.Cs.Add(new cTableCell { Value = (N.Name_First + (N.ManFlag ? "先生" : "小姐")) });//姓名

                            cTR.Cs.Add(new cTableCell { Value = N.ManFlag ? "男" : "女" });//性別

                            var B = DC.Baptized.Where(q => q.ACID == N.ACID && !q.DeleteFlag).OrderByDescending(q => q.BID).FirstOrDefault();
                            if (B == null)
                                cTR.Cs.Add(new cTableCell { Value = "--" });//受洗狀態
                            else if (!B.ImplementFlag)
                                cTR.Cs.Add(new cTableCell { Value = "預計於" + B.BaptismDate.ToString(DateFormat) + "受洗" });//受洗狀態
                            else
                                cTR.Cs.Add(new cTableCell { Value = "已於" + B.BaptismDate.ToString(DateFormat) + "受洗" });//受洗狀態


                            cTR.Cs.Add(new cTableCell { Value = CellPhone });//行動電話
                        }
                        break;

                    /*case 2://兒童
                        {
                            cTR.Cs.Add(new cTableCell { Type = "checkbox", Value = "false", ControlName = "cbox_S" + N.ACID, CSS = "form-check-input cbox_S" });//選擇
                            cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_Childen_Info/" + N.ACID, Target = "_self", Value = "檢視" });//檢視
                            //去識別
                            //cTR.Cs.Add(new cTableCell { Value = (N.Name_First + new string('*', N.Name_Last.Length)) });//姓名
                            cTR.Cs.Add(new cTableCell { Value = (N.Name_First + N.Name_Last) });//姓名

                            cTR.Cs.Add(new cTableCell { Value = N.ManFlag ? "男" : "女" });//性別
                            var B = DC.Baptized.Where(q => q.ACID == N.ACID && !q.DeleteFlag).OrderByDescending(q => q.BID).FirstOrDefault();
                            if (B == null)
                                cTR.Cs.Add(new cTableCell { Value = "--" });//受洗狀態
                            else if (!B.ImplementFlag)
                                cTR.Cs.Add(new cTableCell { Value = "預計於" + B.BaptismDate.ToString(DateFormat) + "受洗" });//受洗狀態
                            else
                                cTR.Cs.Add(new cTableCell { Value = "已於" + B.BaptismDate.ToString(DateFormat) + "受洗" });//受洗狀態

                            var M = DC.M_ML_Account.FirstOrDefault(q => q.ACID == N.ACID && !q.DeleteFlag);
                            if (M == null)
                                cTR.Cs.Add(new cTableCell { Value = "" });//主日聚會點
                            else
                            {
                                var OI = (from q in DC.Meeting_Location_Set.Where(q => q.MLID == M.MLID && q.SetType == 0)
                                          join p in DC.OrganizeInfo.Where(q => !q.DeleteFlag)
                                          on q.OIID equals p.OIID
                                          select p).FirstOrDefault();
                                cTR.Cs.Add(new cTableCell { Value = (OI != null ? "(" + OI.Title + OI.Organize.Title + ")" : "") + M.Meeting_Location.Title });//主日聚會點
                            }

                        }
                        break;
                        */

                    case 3://新人
                        {
                            var Ms = DC.M_OI_Account.Where(q => q.ACID == N.ACID);
                            int iJoinType = 0;//"無意願", "已入組未落戶", "跟進中(已分發)", "被退回(未分發)","跟進中(未分發)"
                            string Note = "";
                            if (Ms.Any())
                            {
                                var M = Ms.OrderByDescending(q => q.MID).First();
                                if (M.OIID > 1)
                                {
                                    if (M.JoinDate == M.CreDate)
                                    {
                                        iJoinType = 1;//已入組未落戶
                                        Note = "已於" + M.CreDate.ToString(DateFormat) + "分派至 (" + M.OIID + ")" + M.OrganizeInfo.Title;
                                    }
                                }
                                else
                                {
                                    if (Ms.Count() > 1)
                                    {
                                        iJoinType = 3;//被退回

                                        var A_Notes = DC.Account_Note.Where(q => q.ACID == N.ACID && !q.DeleteFlag && q.NoteType == 1 && q.OIID == M.OIID).OrderByDescending(q => q.CreDate);
                                        foreach (var A_N in A_Notes)
                                            Note += (Note != "" ? "</br>" : "") + A_N.CreDate.ToString(DateTimeFormat) + ":" + A_N.Note;
                                    }
                                    else
                                        iJoinType = 4;//跟進中(未分發)
                                }
                            }
                            else if (N.GroupType == "有意願-願分發")
                                iJoinType = 4;

                            if (iJoinType == 3 || iJoinType == 4)
                                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_New_Edit/" + N.ACID, Target = "_black", Value = "分發" });//操作
                            else
                                cTR.Cs.Add(new cTableCell { Value = JoinTitle[iJoinType] });//操作
                            //去識別
                            //cTR.Cs.Add(new cTableCell { Value = (N.Name_First + new string('*',N.Name_Last.Length)) });//姓名
                            cTR.Cs.Add(new cTableCell { Value = (N.Name_First + N.Name_Last) });//姓名

                            cTR.Cs.Add(new cTableCell { Value = N.ManFlag ? "男" : "女" });//性別
                            cTR.Cs.Add(new cTableCell { Value = N.Birthday != N.CreDate ? N.Birthday.ToString(DateFormat) : "" });//生日
                            cTR.Cs.Add(new cTableCell { Value = CellPhone });//手機
                            cTR.Cs.Add(new cTableCell { Value = JoinTitle[iJoinType] });//入組狀態
                            if (Note != "")
                                cTR.Cs.Add(new cTableCell { Value = "檢視", Type = "linkbutton", URL = "javascript:SetAlertNote('" + Note + "');", Target = "_self" });//備註
                            else
                                cTR.Cs.Add(new cTableCell { Value = "" });//備註
                        }
                        break;

                    case 4://受洗
                        {
                            string sBaptismDate = "";
                            var Bs = N.Baptized.Where(q => !q.DeleteFlag);
                            if (Bs.Count() == 0)
                            {
                                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_Baptized_Edit/" + N.ACID, Target = "_black", Value = "新增日期" });//操作
                            }
                            else
                            {
                                var B = Bs.OrderByDescending(q => q.BID).FirstOrDefault();
                                if (!B.ImplementFlag)
                                {
                                    sBaptismDate = B.BaptismDate.ToString(DateFormat);
                                    cTableCell cTC = new cTableCell();
                                    cTC.cTCs = new List<cTableCell>();
                                    cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "javascript:Baptized(" + N.ACID + ");", Value = "如期受洗" });//操作
                                    cTC.cTCs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_Baptized_Edit/" + N.ACID, Target = "_black", Value = "改期" });//操作
                                    cTR.Cs.Add(cTC);
                                }
                                else
                                    cTR.Cs.Add(new cTableCell { Value = "資料未同步" });
                            }
                            //操作
                            //去識別
                            //cTR.Cs.Add(new cTableCell { Value = (N.Name_First + new string('*', N.Name_Last.Length)) });//姓名
                            cTR.Cs.Add(new cTableCell { Value = (N.Name_First + N.Name_Last) });//姓名

                            cTR.Cs.Add(new cTableCell { Value = N.ManFlag ? "男" : "女" });//性別
                            cTR.Cs.Add(new cTableCell { Value = N.Birthday != N.CreDate ? N.Birthday.ToString(DateFormat) : "" });//生日
                            cTR.Cs.Add(new cTableCell { Value = CellPhone });//手機
                            //小組名稱
                            var M = N.M_OI_Account.Where(q => !q.DeleteFlag && q.ActiveFlag && q.OrganizeInfo.OID == 8).OrderByDescending(q => q.MID).FirstOrDefault();
                            cTR.Cs.Add(new cTableCell { Value = M != null ? M.OrganizeInfo.Title : "" });
                            //預定受洗日期
                            cTR.Cs.Add(new cTableCell { Value = sBaptismDate });
                        }
                        break;

                    case 6://申請轉換小組
                        {

                            var COI = DC.Change_OI_Order.Where(q => q.ACID == N.ACID && !q.DeleteFlag).OrderByDescending(q => q.COIOID).FirstOrDefault();
                            if (COI.Order_Type == 0)
                                TopTitles.Add(new cTableCell { Type = "checkbox", ControlName = "cbox_" });//選擇
                            else
                                TopTitles.Add(new cTableCell { Value = "" });//選擇
                            TopTitles.Add(new cTableCell { Value = N.Name_First + N.Name_Last });//姓名
                            TopTitles.Add(new cTableCell { Value = string.Join("/", GetOITitles(COI.From_OIID, 3)) });//原始小組
                            TopTitles.Add(new cTableCell { Value = string.Join("/", GetOITitles(COI.To_OIID, 3)) });//新申請小組
                            TopTitles.Add(new cTableCell { Value = COI.CreDate.ToString(DateTimeFormat) });//申請日期
                            string sType = "";
                            switch (COI.Order_Type)
                            {
                                case 0: { sType = "等待審核中"; } break;
                                case 1: { sType = "已直接入組"; } break;
                                case 2:
                                    {
                                        var MOI = DC.M_OI_Account.FirstOrDefault(q => q.ACID == N.ACID && q.OIID == COI.To_OIID);
                                        if (MOI != null)
                                        {
                                            if (MOI.ActiveFlag)
                                                sType = "已許可並已落戶";
                                            else
                                                sType = "已許可,等待落戶";
                                        }
                                    }
                                    break;
                                case 3: { sType = "已駁回"; } break;
                            }
                            TopTitles.Add(new cTableCell { Value = sType });
                        }
                        break;
                }
                cTL.Rs.Add(SetTableCellSortNo(cTR));

            }

            return cTL;
        }

        #endregion
        #region 牧養名單成人-列表
        [HttpGet]
        public ActionResult Account_Aldult_List()
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(null);
            cAL.cTL = GetAccountTable(1, null);

            return View(cAL);
        }
        [HttpPost]
        public ActionResult Account_Aldult_List(FormCollection FC)
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(FC);
            cAL.cTL = GetAccountTable(1, FC);

            return View(cAL);
        }
        #endregion
        #region 牧養名單成人-新增/修改/刪除
        public class cAccount_Aldult_Edit
        {
            public int UID = 0;
            public Account AC = new Account();
            public List<SelectListItem> EducationTypes = ddl_EducationTypes;//教育程度
            public List<SelectListItem> JobTypes = ddl_JobTypes;//職業
            //受洗狀態
            public List<SelectListItem> BaptizedTypes = new List<SelectListItem> {
                new SelectListItem{ Text="旌旗受洗",Value="1",Selected=true},
                new SelectListItem{ Text="非旌旗受洗",Value="2"}
            };
            public List<SelectListItem> MLs = new List<SelectListItem>();//主日聚會點
            public List<ListInput> Coms = new List<ListInput>();//社群帳號
            public List<cContect> Cons = new List<cContect>();//通訊資料
            public Location L = new Location(); //通信地址

            public int JoinGroupType = 0;//入組意願調查選擇
            public List<cJoinGroupWish> cJGWs = new List<cJoinGroupWish>();//加入小組有意願選項
            public string GroupNo = "";//想加入小組的編號

            public List<cFamily> cFs;//家庭聯繫方式

            public Account_Bank AB = new Account_Bank();//退款費用專用帳戶
            public List<SelectListItem> Banks = new List<SelectListItem>();//銀行代號下拉選單

            public bool bPoorFlag = false;//是否為中低收戶入
            public string PoorNote = "";//中低收入戶備註

            public bool bBackUsedFlag = false;//是否為後台管理者
            public bool bShowBackUsedAreaFlag = false;//是否顯示後台管理者勾選介面

            //public bool bJob24Flag = false;//是否為領夜同工
            public List<cOAccount> OAs = new List<cOAccount>();//目前按立狀況
            public List<cOAH> OAHs = new List<cOAH>();//按立歷史

            public bool bFriendFlag = false;//是否為會友

            public bool bNightLeaderFlag = false;//是否為領夜同工
        }
        //家庭樹
        public class cFamily
        {
            public int ID = 0;
            public string TopTitle = "";
            public string ControlName = "";//姓名控制項名稱
            public string Name = "";//姓名控制項名稱
            public string ControlTitle = "";//姓名控制項名稱
            public string Title = "";//姓名控制項名稱
            public int SortNo1 = 0;
            public int SortNo2 = 0;
            public cContect Con = new cContect();//家庭聯繫方式
        }
        //聯絡方式
        public class cContect
        {
            public string Title = "";
            public string ControlName1 = "";
            public string ControlName2 = "";
            public bool RequiredFlag = false;
            public int SortNo = 0;
            public Contect C = new Contect();
            public List<SelectListItem> Zips = new List<SelectListItem>();
        }
        //期望加入組織選單
        public class cJoinGroupWish
        {
            public bool SelectFalg = false;
            public int JoinType = 0;
            public int SortNo = 0;
            public string GroupTitle = "";
            public JoinGroupWish JGW = new JoinGroupWish();
            public ListSelect ddl_Weekly = new ListSelect();
            public ListSelect ddl_Time = new ListSelect();
        }
        //按立
        public class cOAccount
        {
            public int OID = 0;
            public int SortNo = 0;
            public string JobTitle = "";
            public string Note = "";
            public int iType = 0;//0:未被按立,不能案立/1:未被按立,可以按立/2:已被按立,不能卸任/3:已被按立,可以卸任
        }
        //按立
        public class cOAH
        {
            public string Title = "";
            public DateTime TargetDate = DateTime.Now;
            public string Note = "";
        }
        public cAccount_Aldult_Edit GetAccountData(int ID, FormCollection FC)
        {
            var N = new cAccount_Aldult_Edit();
            N.UID = GetACID();
            #region 物件初始化

            //主日聚會點初始化
            N.MLs.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            N.MLs.AddRange((from q in DC.Meeting_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag && q.SetType == 0)
                            select new SelectListItem { Text = q.Meeting_Location.Title, Value = q.MLID.ToString() }).ToList());
            //社群帳號初始化
            N.Coms = new List<ListInput>();
            for (int i = 0; i < CommunityTitle.Length; i++)
                N.Coms.Add(new ListInput { Title = CommunityTitle[i], SortNo = i, ControlName = "txb_Com_" + i, InputData = "" });
            //聯絡方式初始化
            var SLI = (from q in DC.ZipCode.Where(q => q.GroupName == "國" && q.ActiveFlag && q.Title != "線上").OrderBy(q => q.ParentID).ThenBy(q => q.Code)
                       select new SelectListItem { Text = q.Title + q.Code, Value = q.ZID.ToString(), Selected = q.ZID == 10 }).ToList();
            string sSLI = JsonConvert.SerializeObject(SLI);

            //銀行代號
            N.Banks = new List<SelectListItem>();
            foreach (var B in DC.Bank.Where(q => q.ActiveFlag).OrderBy(q => q.BankNo))
                N.Banks.Add(new SelectListItem { Text = B.BankNo.ToString().PadLeft(3, '0') + " " + B.Title, Value = B.BID.ToString() });
            N.Banks[0].Selected = true;

            //List<SelectListItem>_SLI = JsonConvert.DeserializeObject<List<SelectListItem>>(sSLI);
            N.Cons = new List<cContect>
                {
                    new cContect{Title="手機號碼",ControlName1="ddl_Zip_Con0",ControlName2 = "txb_Value_Con0",RequiredFlag=true,SortNo=0,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 1, CID = 0, ContectValue = "",ZID=10 },Zips = (JsonConvert.DeserializeObject<List<SelectListItem>>(sSLI))},//手機1
                    new cContect{Title="手機號碼2",ControlName1="ddl_Zip_Con1",ControlName2 = "txb_Value_Con1",SortNo=1,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 1, CID = 0, ContectValue = "",ZID=10 },Zips = (JsonConvert.DeserializeObject<List<SelectListItem>>(sSLI))},//手機2
                    new cContect{Title="電話(市話)",ControlName1="ddl_Zip_Con2",ControlName2 = "txb_Value_Con2",SortNo=2,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 0, CID = 0, ContectValue = "",ZID=10 },Zips =(JsonConvert.DeserializeObject<List<SelectListItem>>(sSLI))},//市話
                    new cContect{Title="Email",ControlName2 = "txb_Value_Con3",RequiredFlag=true,SortNo=3,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 2, CID = 0, ContectValue = "",ZID=1 },Zips = null},//Email1
                    new cContect{Title="Email2",ControlName2 = "txb_Value_Con4",SortNo=4,C=new Contect { TargetID = ID, TargetType = 2, ContectType = 2, CID = 0, ContectValue = "",ZID=1 },Zips = null}//Email2
                };
            //加入小組有意願選項初始化
            N.cJGWs = new List<cJoinGroupWish>();
            for (int i = 0; i < 2; i++)
            {
                for (int j = 1; j < 4; j++)
                {
                    cJoinGroupWish cJ = new cJoinGroupWish();
                    cJ.JGW = new JoinGroupWish
                    {
                        ACID = ID,
                        JoinType = (i + 1),
                        SortNo = j,
                        WeeklyNo = 0,
                        TimeNo = 0
                    };
                    cJ.GroupTitle = i == 0 ? "實體" : "線上";
                    cJ.JoinType = (i + 1);
                    cJ.SortNo = j;
                    cJ.ddl_Weekly = new ListSelect
                    {
                        Title = "星期",
                        SortNo = j,
                        ControlName = "ddl_Join" + i + "_WeeklyNo_" + j,
                        ddlList = new List<SelectListItem>()
                    };
                    cJ.ddl_Time = new ListSelect
                    {
                        Title = "時段",
                        SortNo = j,
                        ControlName = "ddl_Join" + i + "_TimeNo_" + j,
                        ddlList = new List<SelectListItem>()
                    };
                    cJ.ddl_Time.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
                    for (int k = 1; k < sWeeks.Length; k++)
                        cJ.ddl_Weekly.ddlList.Add(new SelectListItem { Text = sWeeks[k], Value = (k + 1).ToString() });
                    for (int k = 0; k < sTimeSpans.Length; k++)
                        cJ.ddl_Time.ddlList.Add(new SelectListItem { Text = sTimeSpans[k], Value = (k + 1).ToString() });
                    N.cJGWs.Add(cJ);
                }
            }
            //家庭聯絡方式
            #region 家庭與聯絡方式
            N.cFs = new List<cFamily>();
            for (int i = 0; i < FamilyTitle.Length; i++)
            {
                if (i == 3 || i == 4)//緊急聯絡人/子女
                {
                    int iMax = i == 3 ? 2 : 7;
                    for (int j = 0; j < iMax; j++)
                    {
                        cFamily cF = new cFamily();
                        cF.ID = 0;
                        cF.TopTitle = FamilyTitle[i] + (j + 1);
                        cF.ControlName = "txb_Family_Name" + i + "_" + j;//姓名控制項名稱
                        cF.Name = "";//姓名
                        cF.ControlTitle = "txb_Family_Title" + i + "_" + j;//關係控制項名稱
                        cF.Title = "";//關係
                        cF.SortNo1 = i;
                        cF.SortNo2 = j;
                        cF.Con = new cContect
                        {
                            Title = "手機號碼",
                            ControlName1 = "ddl_Zip_F" + i + "_" + j,
                            ControlName2 = "txb_Value_F" + i + "_" + j,
                            RequiredFlag = true,
                            SortNo = 0,
                            C = new Contect
                            {
                                TargetID = ID,
                                TargetType = 2,
                                ContectType = 1,
                                CID = 0,
                                ContectValue = "",
                                ZID = 10
                            },
                            Zips = (JsonConvert.DeserializeObject<List<SelectListItem>>(sSLI))
                        };//家庭聯繫方式

                        N.cFs.Add(cF);
                    }
                }
                else
                {
                    cFamily cF = new cFamily();
                    cF.ID = 0;
                    cF.TopTitle = FamilyTitle[i];
                    cF.ControlName = "txb_Family_Name" + i;//姓名控制項名稱
                    cF.Name = "";//姓名
                    cF.ControlTitle = "txb_Family_Title" + i;//關係控制項名稱
                    cF.Title = "";//關係
                    cF.SortNo1 = i;
                    cF.SortNo2 = 0;
                    cF.Con = new cContect
                    {
                        Title = "手機號碼",
                        ControlName1 = "ddl_Zip_F" + i,
                        ControlName2 = "txb_Value_F" + i,
                        RequiredFlag = i == 2,
                        SortNo = 0,
                        C = new Contect
                        {
                            TargetID = ID,
                            TargetType = 2,
                            ContectType = 1,
                            CID = 0,
                            ContectValue = "",
                            ZID = 10
                        },
                        Zips = (JsonConvert.DeserializeObject<List<SelectListItem>>(sSLI))
                    };//家庭聯繫方式

                    N.cFs.Add(cF);
                }

            }

            #endregion
            //中低收入戶
            N.bShowBackUsedAreaFlag = CheckAdmin(GetACID());
            //領夜同工
            //N.bJob24Flag = DC.M_Role_Account.Any(q => q.ActiveFlag && !q.DeleteFlag && q.RID == 24 && q.ACID == ID);
            N.bNightLeaderFlag = GetMOIAC(8, 0, ID).Count() > 0;
            //按立歷史
            var OAHs = DC.M_O_Account.Where(q => q.ACID == ID);
            #region 建立按立資料

            N.OAs = new List<cOAccount>();
            int iSortNo = 0;
            var O = DC.Organize.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 2);
            do
            {
                if (!string.IsNullOrEmpty(O.JobTitle))
                {
                    cOAccount c = new cOAccount
                    {
                        OID = O.OID,
                        SortNo = iSortNo++,
                        JobTitle = O.JobTitle,
                        Note = "",
                        iType = 0//0:未被按立,不能案立/1:未被按立,可以按立/2:已被按立,不能卸任/3:已被按立,可以卸任
                    };
                    //判斷按立狀態

                    //先確認對方是否已有職位,有的話就補充按立資料
                    var OI = DC.OrganizeInfo.FirstOrDefault(q => q.OID == O.OID && q.ACID == ID && q.ActiveFlag && !q.DeleteFlag);
                    if (OI != null)
                    {
                        M_O_Account OAH = OAHs.FirstOrDefault(q => q.OID == O.OID && !q.DeleteFlag && q.ActiveFlag);
                        if (OAH == null)
                        {
                            OAH = new M_O_Account
                            {
                                OID = O.OID,
                                ACID = ID,
                                ActiveFlag = true,
                                DeleteFlag = false,
                                CreDate = DT,
                                CreUID = N.UID,
                                UpdDate = DT,
                                UpdUID = N.UID
                            };
                            DC.M_O_Account.InsertOnSubmit(OAH);
                            DC.SubmitChanges();

                            c.Note = "已按立 " + DT.ToString(DateFormat) + "(系統自動按立)";
                            c.iType = 2;
                        }
                        else
                        {
                            var AC = DC.Account.FirstOrDefault(q => q.ACID == OAH.ACID);
                            c.Note = "已按立 " + OAH.CreDate.ToString(DateFormat) + "(" + (AC != null ? AC.Name_First + AC.Name_Last : "ID:" + OAH.ACID) + "按立)";
                            c.iType = 2;
                        }
                    }
                    else
                    {
                        M_O_Account OAH = OAHs.FirstOrDefault(q => q.OID == O.OID && !q.DeleteFlag && q.ActiveFlag);
                        if (OAH != null)
                        {
                            var AC_ = DC.Account.FirstOrDefault(q => q.ACID == OAH.CreUID);
                            c.Note = "已按立 " + OAH.CreDate.ToString(DateFormat) + "(" + (AC_ != null ? AC_.Name_First + AC_.Name_Last : "ID:" + OAH.ACID) + "按立)";
                            c.iType = 2;
                        }
                    }

                    N.OAs.Add(c);
                }
                O = DC.Organize.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.ParentID == O.OID);
            } while (O != null);

            #endregion
            #region 檢查能不能按立
            //0:未被按立,不能案立/1:未被按立,可以按立/2:已被按立,不能卸任/3:已被按立,可以卸任
            N.OAs = N.OAs.OrderByDescending(q => q.SortNo).ToList();
            if (N.OAs.Count > 0)
            {
                if (N.OAs[0].iType == 0)
                {
                    N.OAs[0].iType = 1;
                }
                else
                {
                    for (int j = 0; j < N.OAs.Count; j++)
                    {
                        if (j < N.OAs.Count - 1)
                        {
                            //2<->0
                            if (N.OAs[j].iType == 2 && N.OAs[j + 1].iType == 0)
                            {
                                N.OAs[j].iType = 3;
                                N.OAs[j + 1].iType = 1;
                            }
                        }
                    }
                }
            }
            #endregion
            foreach (var M in DC.M_O_Account.Where(q => q.ACID == ID))
            {
                //按立
                cOAH OA = new cOAH();
                OA.Title = M.Organize.JobTitle;
                OA.TargetDate = M.CreDate;
                var AC_ = DC.Account.FirstOrDefault(q => q.ACID == M.CreUID);
                OA.Note = AC_ == null ? "(系統自動按立)" : "(" + AC_.Name_First + AC_.Name_Last + "按立)";
                N.OAHs.Add(OA);
                //卸任
                if (!M.ActiveFlag)
                {
                    OA = new cOAH();
                    OA.Title = M.Organize.JobTitle;
                    OA.TargetDate = M.UpdDate;
                    AC_ = DC.Account.FirstOrDefault(q => q.ACID == M.UpdUID);
                    OA.Note = AC_ == null ? "(系統自動按立)" : "(" + AC_.Name_First + AC_.Name_Last + "卸任)";

                    N.OAHs.Add(OA);
                }
            }
            //是否為會友
            N.bFriendFlag = DC.M_Role_Account.Any(q => q.ACID == ID && q.ActiveFlag && !q.DeleteFlag && q.RID == 2);

            #endregion
            #region 會員資料填入

            N.AC = DC.Account.FirstOrDefault(q => q.ACID == ID && !q.DeleteFlag);
            if (N.AC == null && ID > 0)
                Error += "此帳號已被移除";
            else if (ID == 0)
            {
                N.AC = new Account
                {
                    Birthday = DT,
                    ManFlag = true,
                    MarriageType = 0,
                };
                N.L = new Location();
                N.GroupNo = "";

            }
            else
            {
                #region 基本資料
                //N.AC.Name_Last = new string('*', N.AC.Name_Last.Length);//去識別
                //N.AC.IDNumber = N.AC.IDNumber.Length > 5 ? N.AC.IDNumber.Substring(0, 5) + new string('*', N.AC.IDNumber.Length-5) : new string('*',10);//去識別
                //教育程度
                N.EducationTypes.ForEach(q => q.Selected = false);
                N.EducationTypes.First(q => q.Value == N.AC.EducationType.ToString()).Selected = true;
                //職務
                N.JobTypes.ForEach(q => q.Selected = false);
                N.JobTypes.First(q => q.Value == N.AC.JobType.ToString()).Selected = true;
                //受洗狀況
                if (N.AC.BaptizedType > 0)
                {
                    N.BaptizedTypes[0].Selected = N.AC.BaptizedType == 1;
                    N.BaptizedTypes[1].Selected = N.AC.BaptizedType == 2;
                }
                //主日聚會點
                if (FC == null)
                {
                    var ML = (from q in DC.M_ML_Account.Where(q => !q.DeleteFlag && q.ACID == N.AC.ACID)
                              join p in DC.Meeting_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.SetType == 0)
                              on q.MLID equals p.MLID
                              select q).FirstOrDefault();
                    if (ML != null)
                    {
                        if (N.MLs.FirstOrDefault(q => q.Value == ML.MLID.ToString()) != null)
                        {
                            N.MLs.ForEach(q => q.Selected = false);
                            N.MLs.First(q => q.Value == ML.MLID.ToString()).Selected = true;
                        }
                    }
                }
                //社群狀況
                if (FC == null)
                {
                    var CMs = N.AC.Community.ToList();
                    for (int i = 0; i < CommunityTitle.Length; i++)
                    {
                        var CM = CMs.FirstOrDefault(q => q.CommunityType == i);
                        if (CM != null)
                            N.Coms[i].InputData = CM.CommunityValue;
                    }
                }
                //地址
                var L = DC.Location.FirstOrDefault(q => q.TargetType == 2 && q.TargetID == ID);
                if (L != null)
                    N.L = L;
                else
                    N.L = new Location();
                #endregion
                #region 聯絡方式
                var Cons = DC.Contect.Where(q => q.TargetType == 2 && q.TargetID == ID).OrderBy(q => q.ContectType).ToList();

                if (Cons.Count() > 0)
                {
                    int iSort = 0;
                    foreach (var Con in Cons.Where(q => q.ContectType == 1))
                    {
                        var Con_ = N.Cons.FirstOrDefault(q => q.SortNo == iSort);
                        if (Con_ != null)
                        {
                            Con_.C = Con;
                            if (Con.ZID != 10 && Con.ZID > 1)
                            {
                                Con_.Zips.ForEach(q => q.Selected = false);
                                Con_.Zips.First(q => q.Value == Con.ZID.ToString()).Selected = true;
                            }
                        }

                        iSort++;
                        if (iSort >= 2) break;
                    }
                    iSort = 2;
                    foreach (var Con in Cons.Where(q => q.ContectType == 0))
                    {
                        var Con_ = N.Cons.FirstOrDefault(q => q.SortNo == iSort);
                        if (Con_ != null)
                        {
                            Con_.C = Con;
                            if (Con.ZID != 10 && Con.ZID > 1)
                            {
                                Con_.Zips.ForEach(q => q.Selected = false);
                                Con_.Zips.First(q => q.Value == Con.ZID.ToString()).Selected = true;
                            }
                        }

                        iSort++;
                        if (iSort >= 3) break;
                    }
                    iSort = 3;
                    foreach (var Con in Cons.Where(q => q.ContectType == 2))
                    {
                        var Con_ = N.Cons.FirstOrDefault(q => q.SortNo == iSort);
                        if (Con_ != null)
                        {
                            Con_.C = Con;
                            if (Con.ZID != 10 && Con.ZID > 1)
                            {
                                Con_.Zips.ForEach(q => q.Selected = false);
                                Con_.Zips.First(q => q.Value == Con.ZID.ToString()).Selected = true;
                            }
                        }
                        iSort++;
                        if (iSort >= 5) break;
                    }
                }
                #endregion
                #region 入組意願調查
                N.JoinGroupType = N.AC.GroupType == "有意願-願分發" ? 1 : (N.AC.GroupType == "無意願" ? 0 : 2);
                switch (N.AC.GroupType)
                {
                    case "有意願-願分發":
                        {
                            N.JoinGroupType = 1;
                            var Js = DC.JoinGroupWish.Where(q => q.ACID == ID).ToList();
                            foreach (var cJGW in N.cJGWs)
                            {
                                var JGW = Js.FirstOrDefault(q => q.JoinType == cJGW.JoinType && q.SortNo == cJGW.SortNo);
                                cJGW.JGW = JGW;
                                if (JGW != null)
                                {
                                    if (!cJGW.SelectFalg)
                                        cJGW.SelectFalg = cJGW.JGW.WeeklyNo > 0 && cJGW.JGW.TimeNo > 0;
                                    cJGW.ddl_Weekly.ddlList.ForEach(q => q.Selected = false);
                                    cJGW.ddl_Weekly.ddlList.First(q => q.Value == cJGW.JGW.WeeklyNo.ToString()).Selected = true;
                                    cJGW.ddl_Time.ddlList.ForEach(q => q.Selected = false);
                                    cJGW.ddl_Time.ddlList.First(q => q.Value == cJGW.JGW.TimeNo.ToString()).Selected = true;
                                }
                            }
                        }
                        break;

                    case "無意願":
                        {
                            N.JoinGroupType = 0;
                        }
                        break;

                    default:
                        {
                            N.GroupNo = N.AC.GroupType;
                            N.JoinGroupType = 2;
                        }
                        break;
                }
                #endregion
                #region 家庭狀況
                var Fs = N.AC.Family.Where(q => !q.DeleteFlag).ToList();
                for (int i = 0; i < FamilyTitle.Length; i++)
                {
                    var F_ = Fs.FirstOrDefault(q => q.FamilyType == i);
                    if (F_ != null)//資料庫有資料
                    {
                        if (i == 3)//緊急聯絡人
                        {
                            var Fs_ = N.AC.Family.Where(q => q.FamilyType == i).OrderBy(q => q.SortNo);
                            foreach (var F in Fs_)
                            {
                                var cF_ = N.cFs.FirstOrDefault(q => q.SortNo1 == i && q.SortNo2 == F.SortNo);
                                cF_.ID = F_.FID;
                                cF_.Name = F_.Name;
                                cF_.Title = F_.FamilyTitle;
                                var Con = DC.Contect.Where(q => q.TargetType == 4 && q.TargetID == F.FID && q.ContectType == 1).OrderBy(q => q.CID).FirstOrDefault();
                                if (Con != null)
                                {
                                    cF_.Con.C = Con;
                                    if (Con.ZID != 10 && Con.ZID != 1)
                                    {
                                        cF_.Con.Zips.ForEach(q => q.Selected = false);
                                        cF_.Con.Zips.First(q => q.Value == Con.ZID.ToString()).Selected = true;
                                    }
                                }
                            }
                        }
                        else if (i == 4)//子女
                        {
                            var Fs_ = N.AC.Family.Where(q => q.FamilyType == i).OrderBy(q => q.SortNo);
                            foreach (var F in Fs_)
                            {
                                var cF_ = N.cFs.FirstOrDefault(q => q.SortNo1 == i && q.SortNo2 == F.SortNo);
                                cF_.ID = F_.FID;
                                cF_.Name = F_.Name;
                                cF_.Title = F_.FamilyTitle;
                            }
                        }
                        else if (i == 2)//配偶
                        {
                            var cF_ = N.cFs.FirstOrDefault(q => q.SortNo1 == i);
                            cF_.ID = F_.FID;
                            cF_.Name = F_.Name;
                            var AC_ = DC.Account.FirstOrDefault(q => q.ACID == F_.TargetACID && !q.DeleteFlag);
                            if (AC_ != null)
                            {
                                var Con = DC.Contect.Where(q => q.TargetType == 2 && q.TargetID == AC_.ACID && q.ContectType == 1).OrderBy(q => q.CID).FirstOrDefault();
                                if (Con != null)
                                {
                                    cF_.Con.C = Con;
                                    if (Con.ZID != 10 && Con.ZID != 1)
                                    {
                                        cF_.Con.Zips.ForEach(q => q.Selected = false);
                                        cF_.Con.Zips.First(q => q.Value == Con.ZID.ToString()).Selected = true;
                                    }
                                }
                            }

                        }
                        else
                        {
                            N.cFs.FirstOrDefault(q => q.SortNo1 == i).Name = F_.Name;
                        }

                    }

                }





                #endregion
                #region 退款帳戶
                var AB = N.AC.Account_Bank.Where(q => !q.DeleteFlag).FirstOrDefault();
                if (AB != null)
                {
                    N.AB = AB;
                    N.Banks.ForEach(q => q.Selected = false);
                    N.Banks.First(q => q.Value == AB.BID.ToString()).Selected = true;
                }
                else
                {
                    N.AB = new Account_Bank
                    {
                        Account = N.AC,
                        BID = 2,
                        Title = "",
                        BankNo = "",
                        AccountNo = "",
                        DeleteFlag = false,
                        CreDate = DT,
                        UpdDate = DT,
                        SaveACID = N.AC.ACID
                    };
                }
                #endregion
                #region 中低收入戶
                var AC_Note = DC.Account_Note.FirstOrDefault(q => q.ACID == ID && q.NoteType == 4);
                if (AC_Note != null)
                {
                    N.bPoorFlag = !AC_Note.DeleteFlag;
                    N.PoorNote = AC_Note.Note;
                }
                #endregion
                #region 介面資料填入
                if (FC != null)
                {
                    N.AC.Name_First = FC.Get("txb_Name_First");
                    N.AC.Name_Last = FC.Get("txb_Name_Last");
                    if (ID == 0 || N.UID == 1)
                    {
                        if (!string.IsNullOrEmpty(FC.Get("txb_Login")))
                            N.AC.Login = FC.Get("txb_Login");
                        else
                            Error += "請輸入帳號</br>";

                        if (!string.IsNullOrEmpty(FC.Get("txb_New1")))
                        {
                            if (!CheckPasswork(FC.Get("txb_New1")))
                                Error += "密碼必須為包含大小寫英文與數字的8碼以上字串</br>";
                            else if (FC.Get("txb_New2") != FC.Get("txb_New1"))
                                Error += "新密碼與重複輸入的不同</br>";
                            else
                                N.AC.Password = HSM.Enc_1(FC.Get("txb_New1"));
                        }
                    }

                    N.AC.ManFlag = GetViewCheckBox(FC.Get("cbox_Sex"));
                    N.AC.IDNumber = FC.Get("txb_IDNumber");
                    N.AC.IDType = CheckSSN(N.AC.IDNumber) ? 0 : 1;
                    DateTime BD_ = DT;
                    try
                    {
                        BD_ = Convert.ToDateTime(FC.Get("txb_Birthday"));
                        N.AC.Birthday = BD_;
                        if (BD_.Date > DT.Date)
                            Error += "生日填寫錯誤</br>";
                    }
                    catch { BD_ = DT; }
                    N.AC.EducationType = Convert.ToInt32(FC.Get("ddl_EducationTypes"));
                    N.AC.JobType = Convert.ToInt32(FC.Get("ddl_JobTypes"));
                    N.AC.MarriageType = Convert.ToInt32(FC.Get("rbl_MarriageType"));
                    if (N.AC.MarriageType == 2)
                        N.AC.MarriageNote = FC.Get("txb_MarriageNote");
                    else
                        N.AC.MarriageNote = "";
                    //受洗狀態
                    if (FC.Get("rbl_BaptizedType") == "1")
                        N.AC.BaptizedType = Convert.ToInt32(FC.Get("ddl_BaptizedType"));
                    else
                        N.AC.BaptizedType = 0;
                    //主日聚會點
                    N.MLs.ForEach(q => q.Selected = false);
                    N.MLs.First(q => q.Value == FC.Get("ddl_ML")).Selected = true;
                    //社群資料
                    foreach (var CM in N.Coms)
                    {
                        CM.InputData = FC.Get(CM.ControlName);
                    }
                    //通訊地址
                    N.L.LID = Convert.ToInt32(FC.Get("hid_LID"));
                    //手機電話
                    foreach (var Con in N.Cons)
                    {
                        Con.C.ContectValue = FC.Get(Con.ControlName2);
                        if (Con.ControlName1 != "")
                        {
                            Con.C.ZID = Convert.ToInt32(FC.Get(Con.ControlName1));
                            Con.Zips.ForEach(q => q.Selected = false);
                            Con.Zips.First(q => q.Value == Con.C.ZID.ToString()).Selected = true;
                        }
                    }

                    //退款帳戶
                    N.AB.Title = FC.Get("txb_Bank_Title");
                    N.AB.BankNo = FC.Get("txb_Bank_BankNo");
                    N.AB.AccountNo = FC.Get("txb_Bank_AccountNo");
                    N.AB.BID = Convert.ToInt32(FC.Get("ddl_Bank_BID"));

                    N.Banks.ForEach(q => q.Selected = false);
                    N.Banks.First(q => q.Value == FC.Get("ddl_Bank_BID")).Selected = true;

                    //入組意願
                    N.JoinGroupType = Convert.ToInt32(FC.Get("rbut_GroupFlag"));
                    for (int i = 0; i < 2; i++)
                    {
                        foreach (var _cJGW in N.cJGWs.Where(q => q.JoinType == (i + 1)).OrderBy(q => q.SortNo))
                        {
                            _cJGW.SelectFalg = GetViewCheckBox(FC.Get("cbox_JoinGroupWish" + i));
                            if (!string.IsNullOrEmpty(FC.Get(_cJGW.ddl_Weekly.ControlName)))
                            {
                                _cJGW.ddl_Weekly.ddlList.ForEach(q => q.Selected = false);
                                _cJGW.ddl_Weekly.ddlList.First(q => q.Value == FC.Get(_cJGW.ddl_Weekly.ControlName)).Selected = true;
                            }
                            if (!string.IsNullOrEmpty(FC.Get(_cJGW.ddl_Time.ControlName)))
                            {
                                _cJGW.ddl_Time.ddlList.ForEach(q => q.Selected = false);
                                _cJGW.ddl_Time.ddlList.First(q => q.Value == FC.Get(_cJGW.ddl_Time.ControlName)).Selected = true;
                            }

                        }
                    }
                    if (N.JoinGroupType == 2)
                    {
                        N.AC.GroupType = N.GroupNo = FC.Get("txb_GroupNo");
                    }
                    else
                    {
                        N.AC.GroupType = N.JoinGroupType == 0 ? "無意願" : "有意願-願分發";
                    }
                    //家庭狀況
                    foreach (var cF in N.cFs.OrderBy(q => q.SortNo1).ThenBy(q => q.SortNo2))
                    {
                        switch (cF.SortNo1)
                        {
                            default:
                                cF.Name = FC.Get(cF.ControlName);
                                break;

                            case 2://配偶
                            case 3://緊急聯絡人
                                {
                                    cF.Name = FC.Get(cF.ControlName);
                                    cF.Title = string.IsNullOrEmpty(FC.Get(cF.ControlTitle)) ? "" : FC.Get(cF.ControlTitle);

                                    cF.Con.C.ContectValue = FC.Get(cF.Con.ControlName2);
                                    cF.Con.C.ZID = Convert.ToInt32(FC.Get(cF.Con.ControlName1));
                                    cF.Con.Zips.ForEach(q => q.Selected = false);
                                    cF.Con.Zips.FirstOrDefault(q => q.Value == cF.Con.C.ZID.ToString()).Selected = true;
                                }
                                break;

                            case 4:
                                {
                                    cF.Name = FC.Get(cF.ControlName);
                                    cF.Title = FC.Get(cF.ControlTitle);
                                }
                                break;
                        }
                    }
                    //其他備註事項
                    N.AC.BackUsedFlag = GetViewCheckBox(FC.Get("cbox_BackUsedFlag"));
                    N.AC.ActiveFlag = GetViewCheckBox(FC.Get("cbox_ActiveFlag"));
                    N.AC.TeacherFlag = GetViewCheckBox(FC.Get("cbox_TeacherFlag"));
                    N.AC.NightLeaderFlag = GetViewCheckBox(FC.Get("cbox_NightLeaderFlag"));
                }
                #endregion
            }
            #endregion
            return N;
        }

        [HttpGet]
        public ActionResult Account_Aldult_Edit(int ID)
        {
            GetViewBag();
            if (ACID != 1)
                SetAlert("此頁面只有Admin可以開啟", 2, "/Admin/Home/Index");
            return View(GetAccountData(ID, null));
        }
        [HttpPost]

        public ActionResult Account_Aldult_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            var N = GetAccountData(ID, FC);
            if (ID == 0)
                Error += "目前不允許自後台新增會友";
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                //會員資料更新
                N.AC.UpdDate = DT;
                N.AC.SaveACID = ACID;
                DC.SubmitChanges();

                //主日聚會點
                if (N.MLs.FirstOrDefault(q => q.Selected) != null)
                {
                    int MLID = Convert.ToInt32(N.MLs.First(q => q.Selected).Value);
                    if (MLID > 0)
                    {
                        var ML = DC.M_ML_Account.FirstOrDefault(q => q.MLID == MLID && q.ACID == ID);
                        if (ML != null)
                        {
                            ML.MLID = MLID;
                            ML.DeleteFlag = false;
                            ML.UpdDate = DT;
                            ML.SaveACID = N.AC.SaveACID;
                            DC.SubmitChanges();
                        }
                        else
                        {
                            ML = new M_ML_Account();
                            ML.MLID = MLID;
                            ML.ACID = N.AC.ACID;
                            ML.DeleteFlag = false;
                            ML.UpdDate = ML.CreDate = DT;
                            ML.SaveACID = N.AC.SaveACID;
                            DC.M_ML_Account.InsertOnSubmit(ML);
                            DC.SubmitChanges();
                        }
                    }

                }
                else
                {
                    var MLs = from q in DC.M_ML_Account.Where(q => q.ACID == ID && !q.DeleteFlag)
                              join p in DC.Meeting_Location_Set.Where(q => q.SetType == 0)
                              on q.MLID equals p.MLID
                              select q;
                    foreach (var ML in MLs)
                    {
                        ML.DeleteFlag = true;
                        ML.UpdDate = DT;
                        ML.SaveACID = ACID;
                        DC.SubmitChanges();
                    }
                }
                //社群
                foreach (var CM in N.Coms)
                {
                    if (!string.IsNullOrEmpty(CM.InputData))
                    {
                        Community Com = new Community();
                        Com.ACID = N.AC.ACID;
                        Com.CommunityType = CM.SortNo;
                        Com.CommunityValue = CM.InputData;
                        DC.Community.InsertOnSubmit(Com);
                        DC.SubmitChanges();
                    }
                }
                //退款

                if (N.AB.BID == 0)
                {
                    DC.Account_Bank.InsertOnSubmit(N.AB);
                    DC.SubmitChanges();
                }
                else
                    DC.SubmitChanges();
                //領夜
                if (N.AC.NightLeaderFlag)
                {
                    if (!DC.M_Role_Account.Any(q => q.ActiveFlag && !q.DeleteFlag && q.RID == 24 && q.ACID == ID))
                    {
                        M_Role_Account MRA = new M_Role_Account
                        {
                            ACID = ID,
                            RID = 24,
                            JoinDate = DT,
                            LeaveDate = DT,
                            Note = "",
                            ActiveFlag = true,
                            DeleteFlag = false,
                            CreDate = DT,
                            UpdDate = DT,
                            SaveACID = ACID
                        };
                        DC.M_Role_Account.InsertOnSubmit(MRA);
                        DC.SubmitChanges();
                    }
                }
                else
                {
                    var MRA = DC.M_Role_Account.FirstOrDefault(q => q.RID == 24 && q.ACID == ID);
                    if (MRA != null)
                    {
                        MRA.ActiveFlag = false;
                        MRA.UpdDate = DT;
                        MRA.SaveACID = ACID;
                        DC.SubmitChanges();
                    }
                }

                SetAlert("完成", 1, "/Admin/AccountSet/Account_Aldult_List/0");
            }

            return View(N);
        }
        #endregion
        #region 牧養名單成人-檢視
        [HttpGet]
        public ActionResult Account_Aldult_Info(int ID)
        {
            GetViewBag();
            return View(GetAccountData(ID, null));
        }
        #endregion
        #region 牧養成人-檢視-更新中低收入戶
        public void SavePoot(int ID)
        {
            int Check = GetQueryStringInInt("Check");
            string Note = GetQueryStringInString("Note");
            var N = DC.Account_Note.FirstOrDefault(q => q.ACID == ID && q.NoteType == 4);
            if (N == null)
            {
                if (Check == 1)//有勾選
                {
                    N = new Account_Note();
                    N.ACID = ID;
                    N.OIID = 0;
                    N.NoteType = 4;
                    N.Note = Note;
                    N.DeleteFlag = false;
                    N.CreDate = N.UpdDate = DT;
                    N.SaveACID = ACID;
                    DC.Account_Note.InsertOnSubmit(N);
                    DC.SubmitChanges();
                }
                else
                {

                }
            }
            else
            {
                if (Check == 1)//有勾選
                {
                    N.DeleteFlag = false;
                    N.Note = Note;
                    N.UpdDate = DT;
                    N.SaveACID = ACID;
                    DC.SubmitChanges();
                }
                else//沒勾選
                {
                    N.DeleteFlag = true;
                    N.Note = Note;
                    N.UpdDate = DT;
                    N.SaveACID = ACID;
                    DC.SubmitChanges();
                }
            }
        }
        #endregion
        #region 牧養成人-檢視-更新帳戶資訊
        public void SaveAcctFlag(int ID)
        {
            int iActiveFlag = GetQueryStringInInt("Act");
            int iBackUserFlag = GetQueryStringInInt("Bac");
            int iTeacherFlag = GetQueryStringInInt("Tea");
            int iNightLeaderFlag = GetQueryStringInInt("Nig");
            var N = DC.Account.FirstOrDefault(q => q.ACID == ID);
            if (N != null)
            {
                N.ActiveFlag = iActiveFlag == 1;
                N.BackUsedFlag = iBackUserFlag == 1;
                N.TeacherFlag = iTeacherFlag == 1;
                N.NightLeaderFlag = iNightLeaderFlag == 1;
                N.UpdDate = DT;
                N.SaveACID = ACID;
                DC.SubmitChanges();

                var T = DC.Teacher.FirstOrDefault(q => q.ACID == N.ACID);
                if (T != null)
                {
                    if (N.TeacherFlag)
                    {
                        T.ActiveFlag = true;
                        T.DeleteFlag = false;
                    }
                    else
                    {
                        T.ActiveFlag = false;
                        T.DeleteFlag = true;
                    }
                    T.UpdDate = DT;
                    T.SaveACID = ACID;
                    DC.SubmitChanges();
                }
                else if (N.TeacherFlag)
                {
                    T = new Teacher
                    {
                        ACID = N.ACID,
                        Title = N.Name_First + N.Name_Last,
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
            }
        }
        #endregion
        #region 牧養名單兒童-列表
        [HttpGet]
        public ActionResult Account_Childen_List()
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(null);
            cAL.cTL = GetAccountTable(2, null);

            return View(cAL);
        }
        [HttpPost]
        public ActionResult Account_Childen_List(FormCollection FC)
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(FC);
            cAL.cTL = GetAccountTable(2, FC);

            return View(cAL);
        }
        #endregion
        #region 牧養名單兒童-檢視
        [HttpGet]
        public ActionResult Account_Childen_Info(int ID)
        {
            GetViewBag();
            return View(GetAccountData(ID, null));
        }
        #endregion
        #region 牧養名單新人-列表
        [HttpGet]
        public ActionResult Account_New_List()
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(null);
            cAL.cTL = GetAccountTable(3, null);

            return View(cAL);
        }
        [HttpPost]
        public ActionResult Account_New_List(FormCollection FC)
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(FC);
            cAL.cTL = GetAccountTable(3, FC);

            return View(cAL);
        }
        #endregion
        #region 牧養名單新人-編輯
        public class cAccount_New_Edit
        {
            public int ACID = 0;
            public string Name = "";
            public string PhonoNo = "";
            public ListSelect OIddl = new ListSelect();
            public string InputOIData = "";
            public string InputControlName = "txb_OI";
        }
        public cAccount_New_Edit GetAccount_New_Edit(int ID, FormCollection FC)
        {
            cAccount_New_Edit N = new cAccount_New_Edit();
            var AC = DC.Account.FirstOrDefault(q => q.ACID == ID && !q.DeleteFlag);
            if (AC == null)
                SetAlert("查無此會友,請重新操作", 2, "/Admin/AccountSet/Account_New_List/0");
            else
            {
                N.ACID = AC.ACID;
                N.Name = (AC.Name_First + AC.Name_Last);
                var Con = DC.Contect.Where(q => q.TargetID == AC.ACID && q.TargetType == 2 && q.ContectType == 1 && q.ContectValue != "").OrderByDescending(q => q.CID).FirstOrDefault();
                if (Con != null)
                    N.PhonoNo = Con.ContectValue;
                else
                    N.PhonoNo = "--";
                N.OIddl = new ListSelect() { Title = "1.", SortNo = 0, ControlName = "ddl_OI", ddlList = new List<SelectListItem>() };
                //會友可接受的聚會時間
                //實體
                var JGWs_1 = DC.JoinGroupWish.Where(q => q.ACID == AC.ACID && q.WeeklyNo > 0 && q.TimeNo > 0 && q.JoinType == 1);
                //線上
                var JGWs_2 = DC.JoinGroupWish.Where(q => q.ACID == AC.ACID && q.WeeklyNo > 0 && q.TimeNo > 0 && q.JoinType == 2);

                if (JGWs_1.Count() > 0)
                {
                    //小組聚會點
                    //實體
                    var MLs_1 = from q in DC.Meeting_Location_Set.Where(q => q.SetType == 1 && q.ActiveFlag && !q.DeleteFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag)
                                join p in DC.Location.Where(q => q.TargetType == 3 && q.ZipCode.GroupName != "網路")
                                on q.MLID equals p.TargetID
                                select q;
                    //目前符合的小組名單
                    //實體
                    var MLSs_1 = from q in MLs_1
                                 join p in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 8)
                                 on q.OIID equals p.OIID
                                 select new { q.MLSID, q.Meeting_Location.Title, q.WeeklyNo, q.TimeNo, OITitle = p.Title, p.OIID };
                    var OIs = from q in JGWs_1
                              join p in MLSs_1
                              on new { q.WeeklyNo, q.TimeNo } equals new { p.WeeklyNo, p.TimeNo }
                              select p;

                    foreach (var OI in OIs.OrderByDescending(q => q.OIID))
                        N.OIddl.ddlList.Add(new SelectListItem { Text = "[實體]" + OI.OITitle, Value = OI.OIID.ToString() });
                }
                else if (JGWs_2.Count() > 0)
                {
                    //小組聚會點
                    //線上
                    var MLs_2 = from q in DC.Meeting_Location_Set.Where(q => q.SetType == 1 && q.ActiveFlag && !q.DeleteFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag)
                                join p in DC.Location.Where(q => q.TargetType == 3 && q.ZipCode.GroupName == "網路")
                                on q.MLID equals p.TargetID
                                select q;

                    //目前符合的小組名單
                    //線上
                    var MLSs_2 = from q in MLs_2
                                 join p in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 8)
                                 on q.OIID equals p.OIID
                                 select new { q.MLSID, q.Meeting_Location.Title, q.WeeklyNo, q.TimeNo, OITitle = p.Title, p.OIID };

                    var OIs = from q in JGWs_2
                              join p in MLSs_2
                              on new { q.WeeklyNo, q.TimeNo } equals new { p.WeeklyNo, p.TimeNo }
                              select p;

                    foreach (var OI in OIs.OrderByDescending(q => q.OIID))
                        N.OIddl.ddlList.Add(new SelectListItem { Text = "[線上]" + OI.OITitle, Value = OI.OIID.ToString() });
                }

                if (N.OIddl.ddlList.Count > 0)
                {
                    N.OIddl.ddlList = (List<SelectListItem>)N.OIddl.ddlList.OrderByDescending(q => q.Value);//比較新的小組放前面
                    if (FC != null)
                    {
                        N.OIddl.ddlList.ForEach(q => q.Selected = false);
                        N.OIddl.ddlList.Find(q => q.Value == FC.Get(N.OIddl.ControlName)).Selected = true;
                    }
                    else
                        N.OIddl.ddlList[0].Selected = true;
                }
                else
                    N.OIddl.ddlList.Add(new SelectListItem { Text = "無推薦名單", Value = "0", Selected = true });

                if (FC != null)
                {
                    N.InputOIData = FC.Get(N.InputControlName);
                    string sddlID = FC.Get(N.OIddl.ControlName);
                    if ((sddlID == "0" && N.InputOIData == "") || (sddlID != "0" && N.InputOIData != ""))
                        SetAlert("未選擇小組，請重新選擇或輸入指定小組", 2);
                    else if (sddlID != "0")
                    {
                        var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 8 && q.OIID.ToString() == sddlID);
                        if (OI != null)
                        {
                            M_OI_Account M = new M_OI_Account
                            {
                                OIID = OI.OIID,
                                ACID = AC.ACID,
                                JoinDate = DT,
                                LeaveDate = DT,
                                ActiveFlag = true,
                                DeleteFlag = false,
                                CreDate = DT,
                                UpdDate = DT,
                                SaveACID = ACID
                            };
                            DC.M_OI_Account.InsertOnSubmit(M);
                            DC.SubmitChanges();

                            SetAlert("分發完成", 1, "/Admin/AccountSet/Account_New_List/0");
                        }
                        else
                            SetAlert("小組不存在，請重新選擇或輸入指定小組", 2);
                    }
                    else if (N.InputOIData.Contains(')'))
                    {
                        string[] str = N.InputOIData.Split(')');
                        if (str.Length == 2)
                        {
                            var OI = DC.OrganizeInfo.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 8 && q.OIID.ToString() == str[0].Replace("(", ""));
                            if (OI != null)
                            {
                                M_OI_Account M = new M_OI_Account
                                {
                                    OIID = OI.OIID,
                                    ACID = AC.ACID,
                                    JoinDate = DT,
                                    LeaveDate = DT,
                                    ActiveFlag = true,
                                    DeleteFlag = false,
                                    CreDate = DT,
                                    UpdDate = DT,
                                    SaveACID = ACID
                                };
                                DC.M_OI_Account.InsertOnSubmit(M);
                                DC.SubmitChanges();

                                SetAlert("分發完成", 1, "/Admin/AccountSet/Account_New_List/0");
                            }
                            else
                                SetAlert("小組不存在，請重新選擇或輸入指定小組...", 2);
                        }
                        else
                            SetAlert("小組不存在，請重新選擇或輸入指定小組..", 2);
                    }
                    else
                        SetAlert("小組不存在，請重新選擇或輸入指定小組.", 2);
                }
            }

            return N;
        }
        [HttpGet]
        public ActionResult Account_New_Edit(int ID)
        {
            GetViewBag();
            return View(GetAccount_New_Edit(ID, null));
        }
        [HttpPost]

        public ActionResult Account_New_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            return View(GetAccount_New_Edit(ID, FC));
        }
        #endregion
        #region 牧養名單待受洗-列表
        [HttpGet]
        public ActionResult Account_Baptized_List()
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(null);
            cAL.cTL = GetAccountTable(4, null);
            cAL.LS_Baptized.ddlList[0].Text = "全部";
            cAL.LS_Baptized.ddlList[1].Text = "已指定受洗日期";
            //cAL.LS_Baptized.ddlList[2].Text = "無指定受洗日期";
            cAL.LS_Baptized.ddlList.RemoveAt(2);

            return View(cAL);
        }
        [HttpPost]
        public ActionResult Account_Baptized_List(FormCollection FC)
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(FC);
            cAL.cTL = GetAccountTable(4, FC);
            cAL.LS_Baptized.ddlList[0].Text = "全部";
            cAL.LS_Baptized.ddlList[1].Text = "已指定受洗日期";
            cAL.LS_Baptized.ddlList[2].Text = "無指定受洗日期";

            return View(cAL);
        }
        [HttpGet]
        public void Account_Baptized_SetDate(int ACID)
        {
            var B = DC.Baptized.FirstOrDefault(q => q.ACID == ACID);
            if (B != null)
            {
                if (!B.ImplementFlag)
                {
                    if (B.BaptismDate == B.CreDate)
                        B.BaptismDate = DT;
                    B.ImplementFlag = true;
                    B.UpdDate = DT;
                    B.Account.BaptizedType = 1;
                    B.Account.UpdDate = DT;
                    DC.SubmitChanges();
                }
            }
        }
        #endregion
        #region 牧養名單待受洗-新增
        public class cAccount_Baptized_Edit
        {
            public int ACID = 0;
            public string Name = "";
            public string PhonoNo = "";
            public string OITitle = "";
            public OrganizeInfo OI = new OrganizeInfo();
            public string OldDate = "";
            public string NewDate = "";
            public string InputControlName = "txb_NewDate";
        }
        public cAccount_Baptized_Edit GetAccount_Baptized_Edit(int ID, FormCollection FC)
        {
            cAccount_Baptized_Edit N = new cAccount_Baptized_Edit();
            var AC = DC.Account.FirstOrDefault(q => q.ACID == ID);
            if (AC == null)
                SetAlert("資料錯誤,請重新操作", 2, "/Admin/AccountSet/Account_Baptized_Edit/0");
            else
            {
                N.ACID = AC.ACID;
                N.Name = (AC.Name_First + AC.Name_Last);
                var Con = DC.Contect.Where(q => q.ContectType == 1 && q.TargetType == 2 && q.TargetID == AC.ACID).OrderByDescending(q => q.CID).FirstOrDefault();
                if (Con != null)
                    N.PhonoNo = Con.ContectValue;
                else
                    N.PhonoNo = "--";
                var MOI = GetMOIAC(8, 0, AC.ACID).OrderByDescending(q => q.CreDate).FirstOrDefault();
                if (MOI != null)
                {
                    N.OITitle = MOI.OrganizeInfo.Title + MOI.OrganizeInfo.Organize.Title;
                    N.OI = MOI.OrganizeInfo;
                }
                else
                {
                    N.OITitle = "--";
                    Error = "請先分派小組後再行進行受洗設定";
                    SetAlert(Error, 4, "/Admin/AccountSet/Account_New_Edit/" + AC.ACID);
                }
                N.OldDate = "";
                var B = AC.Baptized.Where(q => !q.DeleteFlag).OrderByDescending(q => q.BID).FirstOrDefault();
                if (B == null)//還沒有受洗資料,新增
                {
                    ViewBag._Title = "待受洗名單-新增日期";
                }
                else//已有受洗資料,預計改期
                {
                    ViewBag._Title = "待受洗名單-改期";
                    N.OldDate = B.BaptismDate.ToString(DateFormat);
                }
                if (FC != null && Error == "")
                {
                    if (string.IsNullOrEmpty(FC.Get(N.InputControlName)))
                        Error += "請輸入受洗日期";
                    else
                    {
                        DateTime DT_ = DT;
                        try
                        {
                            DT_ = Convert.ToDateTime(FC.Get(N.InputControlName));
                            N.NewDate = DT_.ToString(DateFormat);
                        }
                        catch
                        {
                            Error += "受洗日期請輸入西元年月日";
                        }
                    }
                    if (Error != "")
                        SetAlert(Error, 2);
                }
            }

            return N;
        }
        [HttpGet]
        public ActionResult Account_Baptized_Edit(int ID)
        {
            GetViewBag();
            return View(GetAccount_Baptized_Edit(ID, null));
        }
        [HttpPost]

        public ActionResult Account_Baptized_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            var N = GetAccount_Baptized_Edit(ID, FC);
            if (Error == "")
            {
                Baptized B = new Baptized();
                B.OIID = N.OI.OIID;
                B.ACID = ID;
                B.BaptismDate = Convert.ToDateTime(N.NewDate);
                B.ImplementFlag = false;
                B.DeleteFlag = false;

                B.UpdDate = B.CreDate = DT;
                B.SaveACID = ACID;
                DC.Baptized.InsertOnSubmit(B);
                DC.SubmitChanges();
                SetAlert("手洗日期指定完成", 1, "/Admin/AccountSet/Account_Baptized_List/0");
            }
            return View(N);
        }
        #endregion
        #region 牧養名單-轉組申請列表
        [HttpGet]
        public ActionResult Account_GroupOrder_List()
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(null);
            cAL.cTL = GetAccountTable(6, null);

            return View(cAL);
        }
        [HttpPost]
        public ActionResult Account_GroupOrder_List(FormCollection FC)
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(FC);
            cAL.cTL = GetAccountTable(6, FC);

            return View(cAL);
        }
        #endregion
        #region 按立-列表
        [HttpGet]
        public ActionResult Account_Emtitle_List()
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(null);
            cAL.cTL = GetAccountTable(5, null);

            return View(cAL);
        }
        [HttpPost]
        public ActionResult Account_Emtitle_List(FormCollection FC)
        {
            GetViewBag();
            cAccount_List cAL = sAccount_Aldult_List(FC);
            cAL.cTL = GetAccountTable(5, FC);

            return View(cAL);
        }
        #endregion
        #region 按立-顯示
        [HttpGet]
        public ActionResult Account_Emtitle_Edit(int ID)
        {
            GetViewBag();
            if (ID == 0)
                Error += "參數錯誤,無法新增會友";
            if (Error != "")
                SetAlert(Error, 2);
            return View(GetAccountData(ID, null));
        }
        [HttpGet]

        public string EmtitleAccount(int ACID, int OID)
        {
            Error = "";
            int UID = GetACID();
            var MOA = DC.M_O_Account.FirstOrDefault(q => q.ACID == ACID && q.OID == OID && q.ActiveFlag && !q.DeleteFlag);
            if (MOA == null)//按立
            {
                if (!DC.M_Role_Account.Any(q => q.ACID == ACID && q.RID == 2 && q.ActiveFlag && !q.DeleteFlag))
                    Error += "此人尚未持有會友卡,無法按立";
                else
                {
                    MOA = new M_O_Account
                    {
                        OID = OID,
                        ACID = ACID,
                        ActiveFlag = true,
                        DeleteFlag = false,
                        CreDate = DT,
                        CreUID = UID,
                        UpdDate = DT,
                        UpdUID = UID
                    };
                    DC.M_O_Account.InsertOnSubmit(MOA);
                    DC.SubmitChanges();
                }
            }
            else
            {
                MOA.ActiveFlag = false;
                MOA.UpdDate = DT;
                MOA.UpdUID = UID;
                DC.SubmitChanges();
            }
            return Error;
        }
        #endregion


        #region 牧養組織與職分-列表
        public class cOrganize_Info_List
        {
            public int OID = 0;
            public string OTitle = "";
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
            cOL.sAddURL = "/Admin/AccountSet/Organize_Info_Edit/" + OID + "/" + OIID + "/0";
            cOL.OID = OID;
            cOL.OTitle = sKey;

            cOL.cTL = new cTableList();
            cOL.cTL.Title = "";
            cOL.cTL.NowPage = iNowPage;
            cOL.cTL.NowURL = "/Admin/AccountSet/Organize_Info_List/" + OID + "/" + OIID;
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
            if(OID>=3)
                TopTitles.Add(new cTableCell { Title = "調組織", WidthPX = 100 });
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
                        cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Organize_Info_Edit/" + N.OID + "/" + N.ParentID + "/" + N.OIID, Target = "_self", Value = "編輯" });//
                    else
                        cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Organize_Info_Edit/" + N.OID + "/" + N.ParentID + "/" + N.OIID, Target = "_self", Value = "檢視" });//
                }
                else
                    cTR.Cs.Add(new cTableCell { Value = "" });//控制

                if (OID >= 3)
                {
                    cTR.Cs.Add(new cTableCell { Type = "activebutton", URL = "ShowPopupOI(" + N.OIID + ","+N.OID+", '變更上層組織', '', '');", CSS = "btn btn-primary btn_Table_Gray btn-round btn_Basic", Value = "調組織" });//換組
                }


                    cTR.Cs.Add(new cTableCell { Value = N.OIID.ToString() });//編號(ID)
                if (ParentTitle != "")
                {
                    if (NP != null)
                    {
                        string sTitle = (NP.OID == POID ? NP.Title + NP.Organize.Title : "[" + NP.Title + NP.Organize.Title + "]");
                        cTR.Cs.Add(new cTableCell { Type = "link", URL = "/Admin/AccountSet/Organize_Info_List/" + NP.OID + "/" + NP.ParentID, Target = "_self", Value = sTitle });//
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
                        cTR.Cs.Add(new cTableCell { Type = "link", Target = "_self", CSS = "", URL = "/Admin/AccountSet/Organize_Info_List/" + NOID + "/" + N.OIID, Value = RightOIs.Count.ToString() });//下層組織
                    else if (sKey == "")
                        cTR.Cs.Add(new cTableCell { Value = "0" });
                    if (HaveOldNext)
                    {
                        if (NextOIs.Count > 0)//舊下層仍有資料
                            cTR.Cs.Add(new cTableCell { Type = "link", Target = "_self", CSS = "", URL = "/Admin/AccountSet/Organize_Info_List/" + NextOIs[0].OID + "/" + N.OIID, Value = NextOIs.Count.ToString() });//下層組織
                        else
                            cTR.Cs.Add(new cTableCell { Value = "0" });
                    }

                    //if (bGroup[1])
                    cTR.Cs.Add(new cTableCell { Type = "link", Target = "_self", CSS = "btn_Basic_W", URL = "/Admin/AccountSet/Organize_Info_Edit/" + NOID + "/" + N.OIID + "/0", Value = "新增" + NextTitle });
                    //else
                    //    cTR.Cs.Add(new cTableCell { Value = "" });
                }
                else
                {
                    int Ct = GetMOIAC(0, N.OIID, 0).Count();
                    if (Ct == 0)//沒有新成員
                        cTR.Cs.Add(new cTableCell { Value = "0" });//等待分發名單中
                    else
                        cTR.Cs.Add(new cTableCell { Type = "link", Target = "_black", CSS = "", URL = "/Admin/AccountSet/Organize_Info_Account_List?OID=" + N.OID + "&OIID=" + N.OIID, Value = "(" + Ct + ")" });//組員數量
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

            public int Old_BusinessType = 0;
            public string BusinessNote = "";//外展紀錄
            public List<SelectListItem> ChangeList = new List<SelectListItem>();
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

                //外展存紀錄
                if (cIE.Old_BusinessType != cIE.OI.BusinessType)
                {
                    string sLog = DT.ToString(DateTimeFormat) + " 後台" + ACID + "將" + cIE.OI.Title;
                    var MAs = DC.M_OI_Account.Where(q => q.OIID == OIID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.JoinDate);
                    if (cIE.OI.BusinessType == 0)
                        sLog += "移除外展,當下組員名單(" + MAs.Count() + "):";
                    else
                        sLog += "設定為外展,當下組員名單(" + MAs.Count() + "):";

                    sLog += string.Join(",", MAs.Select(q => q.Account.Name_First + q.Account.Name_Last + "(" + q.ACID + ")"));
                    SaveLog(sLog, "外展小組", OIID.ToString());
                }

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
                SetAlert((OIID == 0 ? "新增" : "更新") + "完成", 1, "/Admin/AccountSet/Organize_Info_List/" + OID + "/" + PID);
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
                    SetAlert("查無資料,請重新操作", 2, "/Admin/AccountSet/Organize_Info_List/" + OID + "/0");
                else
                {
                    cIE.Old_BusinessType = cIE.OI.BusinessType;
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
                        else//層級對,所以要可以換
                        {
                            var OIPs__ = DC.OrganizeInfo.Where(q => q.ParentID == OIP_.ParentID && q.ActiveFlag && !q.DeleteFlag);
                            foreach (var OIP__ in OIPs__)
                                cIE.ChangeList.Add(new SelectListItem { Text = OIP__.Title + OIP__.Organize.Title, Value = OIP__.OIID.ToString(), Selected = OIP__.OIID == cIE.OI.ParentID });

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
                    #region 顯示外展紀錄
                    var Cs = ReadLog("外展小組", OIID.ToString());
                    if (Cs.Count > 0)
                    {
                        cIE.BusinessNote = string.Join("<br/>", Cs.OrderByDescending(q => q.CreDate).ThenByDescending(q => q.SortNo).Select(q => q.CreDate.ToString(DateTimeFormat) + " " + q.LogNote));
                    }
                    #endregion
                }
            }
            else//新增
            {
                var O = DC.Organize.FirstOrDefault(q => q.OID == OID && !q.DeleteFlag);
                if (O == null)
                    SetAlert("查無資料,請重新操作", 2, "/Admin/AccountSet/Organize_Map_List/0");
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
                if (!string.IsNullOrEmpty(FC.Get("ddl_ChangeList")))
                {
                    cIE.ChangeList.ForEach(q => q.Selected = false);
                    cIE.ChangeList.First(q => q.Value == FC.Get("ddl_ChangeList")).Selected = true;
                    cIE.OI.ParentID = Convert.ToInt32(FC.Get("ddl_ChangeList"));
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
            cTL.NowURL = "/Admin/AccountSet/Organize_Info_Account_List/" + OID + "/" + OIID;
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
            TopTitles.Add(new cTableCell { Title = "換組", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "組員ID", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "會員ID", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "姓名" });
            TopTitles.Add(new cTableCell { Title = "加入日期" });
            //TopTitles.Add(new cTableCell { Title = "離開日期" });
            TopTitles.Add(new cTableCell { Title = "職務" });
            TopTitles.Add(new cTableCell { Title = "受洗狀態" });

            cTL.Rs.Add(SetTableRowTitle(TopTitles));

            cTL.TotalCt = Ns.Count();
            cTL.MaxNum = GetMaxNum(cTL.TotalCt, cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.OrganizeInfo.ACID == q.ACID).ThenByDescending(q => q.MID).Skip((iNowPage - 1) * cTL.NumCut).Take(cTL.NumCut);
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_Aldult_Info/" + N.ACID, Target = "_black", Value = "編輯" });//會員資料
                if(N.OrganizeInfo.ACID == N.ACID) 
                    cTR.Cs.Add(new cTableCell { Value = "" });
                else
                    cTR.Cs.Add(new cTableCell { Type = "activebutton", URL = "ShowPopupOI(" + N.ACID + ",8, '搜尋小組', '', '');", CSS = "btn btn-primary btn_Table_Gray btn-round btn_Basic", Value = "換組" });//換組
                
                    

                cTR.Cs.Add(new cTableCell { Value = N.MID.ToString() });//組員ID
                cTR.Cs.Add(new cTableCell { Value = N.ACID.ToString() });//會員ID
                cTR.Cs.Add(new cTableCell { Value = N.Account.Name_First + N.Account.Name_Last });//姓名
                if (N.JoinDate == N.CreDate)
                {
                    cTR.Cs.Add(new cTableCell { Value = N.JoinDate.ToString(DateFormat) + "加入,未落戶" });//加入日期
                    //cTR.Cs.Add(new cTableCell { Value = "" });//離開日期
                }
                else
                {
                    cTR.Cs.Add(new cTableCell { Value = N.JoinDate.ToString(DateFormat) + "已落戶" });//加入日期
                                                                                                   //cTR.Cs.Add(new cTableCell { Value = N.LeaveDate == N.CreDate ? "" : N.LeaveDate.ToString(DateFormat) });//離開日期
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
    }
}