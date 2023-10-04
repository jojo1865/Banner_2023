using Banner.Models;
using Microsoft.Ajax.Utilities;
using Newtonsoft.Json;
using System;
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
using static Banner.Areas.Admin.Controllers.OrganizeSetController;

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
            cTL.ItemID = "";
            cTL.NumCut = iNumCut;
            cTL.Rs = new List<cTableRow>();

            var Ns = DC.Account.Where(q => !q.DeleteFlag);
            switch (iType)
            {
                case 1://成人
                    {
                        cTL.NowURL = "/Admin/AccountSet/Account_Aldult_List";
                        Ns = Ns.Where(q => DT.Year - q.Birthday.Year > iChildAge && q.Birthday != q.CreDate);
                        #region 後台全職同工可檢視旌旗資料判斷
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
                                        var v_ACIDs = from q in DC.v_GetAC_O2_OI
                                                      join p in DC.M_OI2_Account.Where(q => q.ACID == ACID)
                                                      on q.OIID equals p.OIID
                                                      select q;

                                        Ns = from q in v_ACIDs
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
                        #endregion
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
                        var M_OI_AGs = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag)
                                       group q by new { q.ACID } into g
                                       select new { g.Key.ACID, MaxID = g.Max(q => q.MID), Ct = g.Count() };
                        var M_OI_As = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag)
                                      join p in M_OI_AGs
                                      on q.MID equals p.MaxID
                                      select q;
                        var AIDs = (Ns.Select(q => q.ACID)).Except(M_OI_As.Where(q => q.OIID > 1 && q.JoinDate != q.CreDate).Select(q => q.ACID));
                        Ns = (from q in AIDs
                              join p in Ns.Where(q => DT.Year - q.Birthday.Year > iChildAge)
                              on q equals p.ACID
                              select p);
                    }
                    break;

                case 4://受洗
                    {
                        cTL.NowURL = "/Admin/AccountSet/Account_Baptized_List";
                        //Ns = Ns.Where(q => DT.Year - q.Birthday.Year > iChildAge && q.BaptizedType == 0);//小朋友不能受洗
                        //Ns = Ns.Where(q => q.BaptizedType == 0);//小朋友可以受洗

                        //小朋友不在受洗名單範圍內,且不考慮未入組名單
                        Ns = from q in Ns.Where(q => DT.Year - q.Birthday.Year > iChildAge && q.BaptizedType == 0)
                             join p in GetMOIAC().Where(q => q.OIID > 1).GroupBy(q => q.ACID).Select(q => q.Key)
                             on q.ACID equals p
                             select q;
                        //排除未被指定受洗日期的會員
                        Ns = from q in DC.Baptized.Where(q => !q.ImplementFlag && !q.DeleteFlag && q.BaptismDate != q.CreDate).GroupBy(q => q.ACID).Select(q => q.Key)
                             join p in Ns
                             on q equals p.ACID
                             select p;
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
                if (FC.Get("txb_OTitle") != null)
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
                        {
                            if (FC.Get("ddl_Baptized") != "0")
                                Ns = from q in Ns
                                     join p in DC.Baptized.Where(q => q.ImplementFlag == (FC.Get("ddl_Baptized") == "1") && !q.DeleteFlag).GroupBy(q => q.ACID).Select(q => q.Key)
                                     on q.ACID equals p
                                     select q;
                        }
                        break;

                    case 2://兒童
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
                    TopTitles.Add(new cTableCell { Title = "選擇", WidthPX = 50 });
                    TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
                    TopTitles.Add(new cTableCell { Title = "牧區" });
                    TopTitles.Add(new cTableCell { Title = "督區" });
                    TopTitles.Add(new cTableCell { Title = "區" });
                    TopTitles.Add(new cTableCell { Title = "小組" });
                    TopTitles.Add(new cTableCell { Title = "姓名" });
                    TopTitles.Add(new cTableCell { Title = "性別", WidthPX = 50 });
                    TopTitles.Add(new cTableCell { Title = "受洗狀態" });
                    TopTitles.Add(new cTableCell { Title = "行動電話" });
                    break;

                case 2://兒童
                    TopTitles.Add(new cTableCell { Title = "選擇", WidthPX = 50 });
                    TopTitles.Add(new cTableCell { Title = "操作", WidthPX = 100 });
                    TopTitles.Add(new cTableCell { Title = "姓名" });
                    TopTitles.Add(new cTableCell { Title = "性別", WidthPX = 50 });
                    TopTitles.Add(new cTableCell { Title = "受洗狀態" });
                    break;

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
                switch (iType)
                {
                    case 1://成人
                        {
                            cTR.Cs.Add(new cTableCell { Type = "checkbox", Value = "false", ControlName = "cbox_S" + N.ACID, CSS = "form-check-input cbox_S" });//選擇
                            //cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_Aldult_Edit/" + N.ACID, Target = "_black", Value = "編輯" });
                            cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_Aldult_Info/" + N.ACID, Target = "_self", Value = "檢視" });//檢視
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

                            cTR.Cs.Add(new cTableCell { Value = (N.Name_First + N.Name_Last) });//姓名
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

                    case 2://兒童
                        {
                            cTR.Cs.Add(new cTableCell { Type = "checkbox", Value = "false", ControlName = "cbox_S" + N.ACID, CSS = "form-check-input cbox_S" });//選擇
                            cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_Childen_Info/" + N.ACID, Target = "_self", Value = "檢視" });//檢視
                            cTR.Cs.Add(new cTableCell { Value = (N.Name_First + N.Name_Last) });//姓名
                            cTR.Cs.Add(new cTableCell { Value = N.ManFlag ? "男" : "女" });//性別
                            var B = DC.Baptized.Where(q => q.ACID == N.ACID && !q.DeleteFlag).OrderByDescending(q => q.BID).FirstOrDefault();
                            if (B == null)
                                cTR.Cs.Add(new cTableCell { Value = "--" });//受洗狀態
                            else if (!B.ImplementFlag)
                                cTR.Cs.Add(new cTableCell { Value = "預計於" + B.BaptismDate.ToString(DateFormat) + "受洗" });//受洗狀態
                            else
                                cTR.Cs.Add(new cTableCell { Value = "已於" + B.BaptismDate.ToString(DateFormat) + "受洗" });//受洗狀態
                        }
                        break;

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
                            if (iJoinType == 3 || iJoinType == 4)
                                cTR.Cs.Add(new cTableCell { Type = "linkbutton", URL = "/Admin/AccountSet/Account_New_Edit/" + N.ACID, Target = "_black", Value = "分發" });//操作
                            else
                                cTR.Cs.Add(new cTableCell { Value = "" });//操作
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
        }
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
        public cAccount_Aldult_Edit GerAccountData(int ID, FormCollection FC)
        {
            var N = new cAccount_Aldult_Edit();
            #region 物件初始化

            //主日聚會點初始化
            N.MLs.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = true });
            N.MLs.AddRange((from q in DC.M_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag && q.SetType == 0)
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
                              join p in DC.M_Location_Set.Where(q => q.ActiveFlag && !q.DeleteFlag && q.SetType == 0)
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
                N.JoinGroupType = N.AC.GroupType == "有意願" ? 1 : (N.AC.GroupType == "無意願" ? 0 : 2);
                switch (N.AC.GroupType)
                {
                    case "有意願":
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



                /*var MOIACs = GetMOIAC(0, 0, ID);
                if (MOIACs.Count() == 0)
                    N.JoinGroupType = 0;
                else
                {
                    var Js = DC.JoinGroupWish.Where(q => q.ACID == ID).ToList();
                    var MOIAC = MOIACs.OrderByDescending(q => q.MID).First();
                    if (MOIAC.OIID == 1)
                    {
                        N.JoinGroupType = 1;
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
                    else
                    {
                        N.JoinGroupType = 2;
                       
                    }

                }*/
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
                    if (ID == 0)
                    {
                        N.AC.Login = FC.Get("txb_Login");
                        if (!CheckPasswork(FC.Get("txb_New1")))
                            Error += "密碼必須為包含大小寫英文與數字的8碼以上字串</br>";
                        else if (FC.Get("txb_New2") != FC.Get("txb_New1"))
                            Error += "新密碼與重複輸入的不同</br>";
                        else
                            N.AC.Password = HSM.Enc_1(FC.Get("txb_New1"));
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
                        N.AC.GroupType = N.JoinGroupType == 0 ? "無意願" : "有意願";
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
            return View(GerAccountData(ID, null));
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Account_Aldult_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            var N = GerAccountData(ID, FC);
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
                else
                {
                    var MLs = from q in DC.M_ML_Account.Where(q => q.ACID == ID && !q.DeleteFlag)
                              join p in DC.M_Location_Set.Where(q => q.SetType == 0)
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
            }

            return View(N);
        }
        #endregion
        #region 牧養名單成人-檢視
        [HttpGet]
        public ActionResult Account_Aldult_Info(int ID)
        {
            GetViewBag();
            return View(GerAccountData(ID, null));
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
            var N = DC.Account.FirstOrDefault(q => q.ACID == ID);
            if (N != null)
            {
                N.ActiveFlag = iActiveFlag == 1;
                N.BackUsedFlag = iBackUserFlag == 1;
                N.TeacherFlag = iTeacherFlag == 1;
                N.UpdDate = DT;
                N.SaveACID = ACID;
                DC.SubmitChanges();

                var T = DC.Teacher.FirstOrDefault(q => q.ACID == N.ACID);
                if(T!=null)
                {
                    if(N.TeacherFlag)
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
                else if(N.TeacherFlag)
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
            return View(GerAccountData(ID, null));
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
                    var MLs_1 = from q in DC.M_Location_Set.Where(q => q.SetType == 1 && q.ActiveFlag && !q.DeleteFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag)
                                join p in DC.Location.Where(q => q.TargetType == 3 && q.ZipCode.GroupName != "網路")
                                on q.MLID equals p.TargetID
                                select q;
                    //目前符合的小組名單
                    //實體
                    var MLSs_1 = from q in MLs_1
                                 join p in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 8)
                                 on q.OIID equals p.OIID
                                 select new { q.MID, q.Meeting_Location.Title, q.WeeklyNo, q.TimeNo, OITitle = p.Title, p.OIID };
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
                    var MLs_2 = from q in DC.M_Location_Set.Where(q => q.SetType == 1 && q.ActiveFlag && !q.DeleteFlag && q.Meeting_Location.ActiveFlag && !q.Meeting_Location.DeleteFlag)
                                join p in DC.Location.Where(q => q.TargetType == 3 && q.ZipCode.GroupName == "網路")
                                on q.MLID equals p.TargetID
                                select q;

                    //目前符合的小組名單
                    //線上
                    var MLSs_2 = from q in MLs_2
                                 join p in DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.OID == 8)
                                 on q.OIID equals p.OIID
                                 select new { q.MID, q.Meeting_Location.Title, q.WeeklyNo, q.TimeNo, OITitle = p.Title, p.OIID };

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
        [ValidateAntiForgeryToken]
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
        [ValidateAntiForgeryToken]
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


    }
}