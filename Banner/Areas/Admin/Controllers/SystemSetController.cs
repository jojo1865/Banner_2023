using Banner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

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
            public List<ListSelect> LS = new List<ListSelect>();
            public string sAddURL = "/Admin/SystemSet/BackUser_Edit";
            public string Name = "";
            public string Account = "";
            public string CellPhone = "";
            public int Sex = 0;
        }
        
        public ActionResult BackUser_List()
        {
            GetViewBag();
            cBackUser_List cBUL = new cBackUser_List();
            cBUL.LS = GetO();
            cBUL.cTL = GetBackUser(null);


            return View(cBUL);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BackUser_List(FormCollection FC)
        {
            GetViewBag();
            cBackUser_List cBUL = new cBackUser_List();
            cBUL.LS = GetO();
            cBUL.cTL = GetBackUser(FC);
            cBUL.Name = FC.Get("txb_Name");
            cBUL.Account = FC.Get("txb_Account");
            cBUL.CellPhone = FC.Get("txb_CellPhone");
            cBUL.Sex = Convert.ToInt32(FC.Get("rbl_Sex"));

            return View(cBUL);
        }

        private cTableList GetBackUser(FormCollection FC)
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
            cTL.NowURL = "/Admin/SystemSet/BackUser_List";
            cTL.NumCut = iNumCut;
            cTL.Rs = new List<cTableRow>();



            var Ns = DC.Account.Where(q => !q.DeleteFlag && q.BackUsedFlag);
            if (FC != null)
            {
                if (!string.IsNullOrEmpty(FC.Get("txb_Name")))
                    Ns = Ns.Where(q => q.Name.Contains(FC.Get("txb_Name")));
                if (!string.IsNullOrEmpty(FC.Get("txb_Account")))
                    Ns = Ns.Where(q => q.Login.Contains(FC.Get("txb_Account")));
                if (!string.IsNullOrEmpty(FC.Get("txb_CellPhone")))
                    Ns = from q in Ns
                         join p in DC.Contect.Where(q => q.TargetType == 2 && q.ContectType == 1 && q.ContectValue.Contains(FC.Get("txb_CellPhone"))).GroupBy(q => q.TargetID)
                         on q.ACID equals p.Key
                         select q;

                if (!string.IsNullOrEmpty(FC.Get("rbl_Sex")))
                    if (FC.Get("rbl_Sex") != "0")
                        Ns = Ns.Where(q => q.ManFlag == (FC.Get("rbl_Sex") == "1"));


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
                    var IDs = OIs.GroupBy(q => q.OIID).Select(q => q.Key);
                    var Ms = from q in DC.M_OI_Account.Where(q => !q.DeleteFlag && (q.JoinDate >= q.CreDate && q.JoinDate.Date <= DT.Date) && (q.LeaveDate == q.CreDate || q.LeaveDate.Date >= DT.Date))
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
            TopTitles.Add(new cTableCell { Title = "", WidthPX = 100 });
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
                cTR.Cs.Add(new cTableCell { Value = N.Name.ToString() });//姓名
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

        }
        public ActionResult BackUser_Edit(int ID)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            cBackUser_Edit cBUE = ReSetBackUser_Edit(ID, null);
            return View(cBUE);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BackUser_Edit(int ID, FormCollection FC)
        {
            GetViewBag();
            ChangeTitle(ID == 0);
            cBackUser_Edit cBUE = ReSetBackUser_Edit(ID, FC);
            Error = "";
            var N_ = DC.Account.FirstOrDefault(q => q.ACID != ID && !q.DeleteFlag && q.Login == cBUE.Login);
            if (N_ != null)
                Error += "您輸入的帳號與他人重複,請重新輸入</br>";
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
                    N.SaveACID = GetACID();
                    DC.SubmitChanges();

                    var MAs = DC.M_Rool_Account.Where(q => q.ACID == ID && !q.DeleteFlag && (q.JoinDate >= q.CreDate && q.JoinDate.Date <= DT.Date) && (q.LeaveDate == q.CreDate || q.LeaveDate.Date >= DT.Date)).ToList();
                    foreach (var B in cBUE.RList)
                    {
                        var MA = MAs.FirstOrDefault(q => q.RID.ToString() == B.Value);
                        if (MA == null && B.Selected)
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
                                SaveACID = GetACID()
                            };
                            DC.M_Rool_Account.InsertOnSubmit(MA);
                            DC.SubmitChanges();
                        }
                        else if (!MA.ActiveFlag && B.Selected)
                        {
                            MA.ActiveFlag = true;
                            MA.JoinDate = DT;
                            MA.LeaveDate = MA.CreDate;
                            MA.UpdDate = DT;
                            MA.SaveACID = GetACID();
                            DC.SubmitChanges();
                        }
                        else if (MA != null && !B.Selected)
                        {
                            MA.ActiveFlag = false;
                            MA.DeleteFlag = true;
                            MA.LeaveDate = MA.UpdDate = DT;
                            DC.SubmitChanges();
                        }
                    }
                }
                SetAlert((ID == 0 ? "新增" : "更新") + "完成", 1, "/Admin/SystemSet/BackUser_List");
            }

            return View(cBUE);
        }

        private cBackUser_Edit ReSetBackUser_Edit(int ID, FormCollection FC)
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
            var Rs = DC.Rool.Where(q => (q.RoolType == 3 || q.RoolType == 4) && !q.DeleteFlag && q.ActiveFlag).OrderBy(q => q.RID);
            var MAs = DC.M_Rool_Account.Where(q => q.ACID == ID && !q.DeleteFlag && (q.JoinDate >= q.CreDate && q.JoinDate.Date <= DT.Date) && (q.LeaveDate == q.CreDate || q.LeaveDate.Date >= DT.Date)).ToList();
            foreach (var R in Rs)
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


            return cBUE;
        }
        #endregion
    }
}