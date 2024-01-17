using Banner.Models;
using Microsoft.Ajax.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Security.Policy;
using System.Web;
using System.Web.Mvc;
using System.Web.WebPages.Html;
using SelectListItem = System.Web.Mvc.SelectListItem;

namespace Banner.Areas.Admin.Controllers
{
    public class IncludeController : PublicClass
    {
        #region 選單
        // GET: Admin/Shared
        public PartialViewResult _LeftMenu()
        {
            string sURL = GetShortURL()
                .Replace("Event_Edit_1", "Event_List?CID=1")
                .Replace("Event_Edit_2", "Event_List?CID=2")
                .Replace("Event_Edit_3", "Event_List?CID=3")
                .Replace("ProductClass_BatchAdd", "Product_List")
                .Replace("_Edit", "_List")
                .Replace("ProductClass_", "Product_")
                .Replace("ProductClassTeacher_", "Product_")
                .Replace("ProductAllowAccount_", "Product_").Split('?')[0];
            if (!sURL.Contains("_Info_List"))
                sURL = sURL.Replace("_Info", "_List");

            int iID = 0;
            string[] sURLs = Request.RawUrl.Split('/');
            int.TryParse(sURLs[sURLs.Length - 1], out iID);

            if (sURL.EndsWith("/"))
                sURL = sURL.Remove(sURL.Length - 1);
            
            cMenu Ms = new cMenu();
            int ACID = GetACID();
            if (ACID <= 0)
                SetAlert("請先登入", 2, "/Admin/Home/Login");
            else
            {
                if (CheckAdmin(ACID))//此使用者擁有系統管理者權限
                {
                    Ms.Items = GetMenu(null, sURL, 0);//選單全開
                }
                else
                {
                    var Rs = from q in DC.Rool.Where(q => q.ActiveFlag && !q.DeleteFlag && (q.RoolType == 3 || q.RoolType == 4))
                             join p in GetMRAC(0, ACID)
                             on q.RID equals p.RID
                             select q;
                    Ms.Items = GetMenu(Rs.ToList(), sURL, 0);

                    //事功團主責補充
                    if(DC.M_Staff_Account.Any(q=>q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID && q.LeaderFlag))
                    {
                        var Menu_2 = Ms.Items.FirstOrDefault(q => q.MenuID == 2);
                        if (Menu_2 == null)
                        {
                            var M = DC.Menu.FirstOrDefault(q => q.MID == 2);
                            Menu_2 = new cMenu();
                            Menu_2.MenuID = M.MID;
                            Menu_2.Title = M.Title;
                            Menu_2.Url = M.URL;
                            Menu_2.SortNo = M.SortNo;
                            Menu_2.ImgUrl = string.IsNullOrEmpty(M.ImgURL) ? "" : M.ImgURL;
                            Menu_2.Items = new List<cMenu>();
                        }
                        var M_Staff = DC.Menu.First(q => q.MID == 60);//事工團主責用的選單
                        int i = (Menu_2.Items.Count() > 0 ? Menu_2.Items.Max(q => q.SortNo) : 0) + 1;
                        var Ls = DC.M_Staff_Account.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID && q.LeaderFlag);
                        foreach (var L in Ls.OrderBy(q => q.OIID).ThenBy(q => q.Staff.SCID).ThenBy(q => q.SID))
                        {
                            cMenu cM_ = new cMenu();
                            cM_.MenuID = M_Staff.MID;
                            cM_.Title = L.OrganizeInfo.Title + "/" + (L.Staff.ChildrenFlag ? "[兒童]" : "") +L.Staff.Title;
                            cM_.Url = M_Staff.URL + "/" + L.SID;
                            cM_.SortNo = i++;
                            cM_.ImgUrl = M_Staff.ImgURL;
                            cM_.Items = new List<cMenu>();

                            if(sURL == "/Admin/StaffSet/StaffAccount_List" && L.SID == iID)
                            {
                                cM_.SelectFlag = true;
                                Menu_2.SelectFlag = true;
                            }
                            else
                                cM_.SelectFlag = false;
                                
                            Menu_2.Items.Add(cM_);
                        }

                        if (!Menu_2.Items.Any(q => q.Url == "/Admin/StaffSet/OI_Staff_List") && sURL.Contains("OI_Staff_List") && Menu_2.Items.Count > 0)
                            SetAlert("", 1, Menu_2.Items[0].Url);
                        Ms.Items.Add(Menu_2);
                    }
                }
            }
            if (Ms.Items == null)
                Ms.Items = new List<cMenu>();
            else
                Ms.Items = Ms.Items.OrderBy(q => q.SortNo).ToList();
            return PartialView(Ms);
        }
        //取得選單可以查的網址
        private string GetShortURL()
        {
            string sURL = Request.RawUrl;//目前完整網址
            if (sURL.ToLower().Contains("/admin/"))
            {
                string[] NewURL = sURL.Split('/');
                sURL = "";
                if (NewURL.Length >= 4)
                    for (int i = 0; i < 4; i++)
                        sURL += NewURL[i] + (NewURL[i].Contains("?") ? "" : "/");
            }
            return sURL.Split('?')[0];
        }

        private List<cMenu> GetMenu(List<Rool> Rs, string sURL, int MID)
        {
            List<cMenu> Ms = new List<cMenu>();
            var Ns = DC.Menu.Where(q => q.ActiveFlag && !q.DeleteFlag && q.MenuType == 0 && q.ParentID == MID).ToList();
            if (Rs != null)
            {
                var MPs = from q in (from q in DC.M_Rool_Menu.Where(q => q.ShowFlag && q.Menu.MenuType == 0).ToList()
                                     join p in Rs
                                     on q.RID equals p.RID
                                     select new { q.MID })
                          group q by new { q.MID } into g
                          select new { g.Key.MID };

                Ns = (from q in MPs.ToList()
                      join p in Ns
                      on q.MID equals p.MID
                      select p).ToList();
            }
            Ns = Ns.OrderBy(q => q.SortNo).ToList();
            if (Ns.Count() > 0)
            {
                int ACID = GetACID();
                foreach (var N in Ns)
                {
                    cMenu cM = new cMenu();
                    cM.MenuID = N.MID;
                    cM.Title = N.Title;
                    cM.Url = N.URL;
                    cM.SortNo = N.SortNo;
                    cM.ImgUrl = string.IsNullOrEmpty(N.ImgURL) ? "" : N.ImgURL;
                    cM.Items = GetMenu(Rs, sURL, N.MID);

                    if (!string.IsNullOrEmpty(N.URL))
                        cM.SelectFlag = N.URL.StartsWith(sURL);
                    else if (cM.Items.Find(q => q.SelectFlag) != null)
                        cM.SelectFlag = true;
                    else
                        cM.SelectFlag = false;
                    Ms.Add(cM);
                }
            }
            return Ms;
        }
        #endregion
        #region Alert控制
        public PartialViewResult _AlertMsg()
        {
            return PartialView();
        }
        #endregion
        #region 列表顯示
        public PartialViewResult _TableList(cTableList cTL)
        {
            return PartialView(cTL);
        }
        #endregion
        #region 取SiteMap
        public PartialViewResult _SiteMap()
        {
            int iSort = 0;
            List<cMenu> Ms = new List<cMenu>();
            string sURL = GetShortURL();
            string sID = Request.Url.Segments[Request.Url.Segments.Length - 1];
            int i = 0;
            if (!sID.ToLower().Contains("list") && !sID.ToLower().Contains("edit"))
            {
                if (int.TryParse(sID, out i))
                    sID = i.ToString();
                else
                    sID = "0";
            }

            if (sURL.Contains("_Edit"))
            {
                cMenu cM = new cMenu();
                cM.MenuID = 0;
                cM.Title = (sID == "0" ? "新增" : "編輯");
                cM.Url = "";
                cM.SortNo = iSort;
                cM.ImgUrl = "";
                cM.SelectFlag = true;
                cM.Items = null;
                Ms.Add(cM);

                iSort++;
            }

            sURL = sURL.Replace("_Edit", "_List");
            if (sURL.EndsWith("/"))
                sURL = sURL.Substring(0, sURL.Length - 1);
            var N = DC.Menu.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.MenuType == 0 && q.URL.StartsWith(sURL));
            while (N != null)
            {
                cMenu cM = new cMenu();
                cM.MenuID = N.MID;
                cM.Title = N.Title;
                cM.Url = iSort == 0 ? "" : N.URL;
                cM.SortNo = iSort;
                cM.ImgUrl = "";
                cM.SelectFlag = true;
                cM.Items = null;
                Ms.Add(cM);

                iSort++;
                N = DC.Menu.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && q.MenuType == 0 && q.MID == N.ParentID);
            }

            return PartialView(Ms);
        }

        #endregion
        #region 篩選組織用選單
        public PartialViewResult _OrganizeFilter(int OID, string OTitle, bool LockFlag = false)
        {
            List<SelectListItem> OList = new List<SelectListItem>();

            OList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = OID == 0 });
            var Os = DC.Organize.Where(q => !q.DeleteFlag).ToList();
            int PID = 0;
            while (true)
            {
                var O = Os.FirstOrDefault(q => q.ParentID == PID);
                if (O == null)
                    break;
                else
                {
                    OList.Add(new SelectListItem { Text = O.Title, Value = O.OID.ToString(), Selected = OID == O.OID, Disabled = !O.ActiveFlag });
                    PID = O.OID;
                }
            };
            ViewBag._LockFlag = LockFlag ? 1 : 0;
            ViewBag._OTitle = OTitle;
            return PartialView(OList);
        }

        #endregion
        #region 篩選電話用選單
        public PartialViewResult _PhoneNoFilter(string PhoneNo, string PhoneTitle, int ZID)
        {
            List<SelectListItem> ZipList = new List<SelectListItem>();

            ZipList.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = ZID == 0 });
            var Ns = DC.ZipCode.Where(q => q.ActiveFlag && q.GroupName == "國").OrderBy(q => q.ZID).ToList();
            foreach (var N in Ns)
                ZipList.Add(new SelectListItem { Text = N.Title + "(" + N.Code + ")", Value = N.ZID.ToString(), Selected = ZID == N.ZID });

            ViewBag._PhoneNo = PhoneNo;
            ViewBag._PhoneTitle = PhoneTitle;
            return PartialView(ZipList);
        }
        #endregion
        #region 取得/設定聯絡方式

        public PartialViewResult _ContectEdit(Contect C)
        {
            c_ContectEdit cN = new c_ContectEdit();
            if (C == null)
            {
                C = new Contect
                {
                    TargetType = 0,
                    TargetID = 0,
                    ZID = 10,
                    ContectType = 0,
                    ContectValue = ""
                };
            }
            cN.C = C;
            cN.SLIs = new List<SelectListItem>();
            cN.SLIs.Add(new SelectListItem { Text = "請選擇", Value = "0", Selected = C.ZID == 0 });
            var Ns = DC.ZipCode.Where(q => q.ActiveFlag && q.GroupName == "國").OrderBy(q => q.ZID).ToList();
            foreach (var N in Ns)
                cN.SLIs.Add(new SelectListItem { Text = N.Title + "(" + N.Code + ")", Value = N.ZID.ToString(), Selected = C.ZID == N.ZID });


            if (C.ContectType == 0)
                cN.InputNote = "請輸入電話號碼";
            else if (C.ContectType == 1)
                cN.InputNote = "請輸入手機";
            else
                cN.InputNote = "請輸入Email";
            return PartialView(cN);
        }
        #endregion
        #region 地址-聚會點
        public cLocation SetLocation_Meeting(int LID, FormCollection FC)
        {
            int ZID = 46;
            string Address = "";
            if (LID > 0)
            {
                var L = DC.Location.FirstOrDefault(q => q.LID == LID);
                if (L != null)
                {
                    ZID = L.ZID;
                    Address = L.Address;
                }
            }
            cLocation cL = new cLocation();
            cL.Z0List.Add(new SelectListItem { Text = "台灣", Value = "10" });
            cL.Z0List.Add(new SelectListItem { Text = "國外", Value = "2" });
            cL.Z0List.Add(new SelectListItem { Text = "線上", Value = "670" });

            var Z1s = DC.ZipCode.Where(q => q.ActiveFlag && q.GroupName == "縣市").OrderBy(q => q.Title).ToList();
            var Z3s = DC.ZipCode.Where(q => q.ActiveFlag && q.GroupName == "國" && q.ZID != 10).OrderBy(q => q.ParentID).ThenBy(q => q.ZID).ToList();
            var Z = DC.ZipCode.FirstOrDefault(q => q.ZID == ZID);
            if (FC != null)
                if (FC.Keys.Count == 0)
                    FC = null;

            if (FC != null)
            {

                cL.Z0List.First(q => q.Value == FC.Get("ddl_Zip0")).Selected = true;
                cL.Address0 = FC.Get("txb_Address0");
                cL.Address1_1 = FC.Get("txb_Address1_1");
                cL.Address1_2 = FC.Get("txb_Address1_2");
                cL.Address2 = FC.Get("txb_Address2");
                //本國

                foreach (var Z1 in Z1s)
                    cL.Z1List.Add(new SelectListItem { Text = Z1.Title, Value = Z1.ZID.ToString(), Selected = Z1.ZID.ToString() == FC.Get("ddl_Zip1") });

                var Z2s = DC.ZipCode.Where(q => q.ActiveFlag && q.ParentID == Z1s.First().ParentID).OrderBy(q => q.Code);
                foreach (var Z2 in Z2s)
                    cL.Z2List.Add(new SelectListItem { Text = Z2.Code + " " + Z2.Title, Value = Z2.ZID.ToString(), Selected = Z2.ZID.ToString() == FC.Get("ddl_Zip2") });

                //外國

                foreach (var Z3 in Z3s)
                    cL.Z3List.Add(new SelectListItem { Text = Z3.Title, Value = Z3.ZID.ToString(), Selected = Z3.ZID.ToString() == FC.Get("ddl_Zip3") });
            }
            else if (Z != null)
            {
                if (Z.GroupName == "國")
                    cL.Z0List[1].Selected = true;
                else if (Z.Title == "線上")
                    cL.Z0List[2].Selected = true;
                else
                    cL.Z0List[0].Selected = true;

                //本國
                if (cL.Z0List[0].Selected)
                {
                    cL.Address0 = Address;
                    foreach (var Z1 in Z1s)
                        cL.Z1List.Add(new SelectListItem { Text = Z1.Title, Value = Z1.ZID.ToString(), Selected = Z1.ZID == Z.ParentID });

                    var Z2s = DC.ZipCode.Where(q => q.ActiveFlag && q.ParentID == Z.ParentID).OrderBy(q => q.Title);
                    foreach (var Z2 in Z2s)
                        cL.Z2List.Add(new SelectListItem { Text = Z2.Code + " " + Z2.Title, Value = Z2.ZID.ToString(), Selected = Z2.ZID == Z.ZID });
                }
                else
                {
                    foreach (var Z1 in Z1s)
                        cL.Z1List.Add(new SelectListItem { Text = Z1.Title, Value = Z1.ZID.ToString(), Selected = Z1 == Z1s.First() });

                    var Z2s = DC.ZipCode.Where(q => q.ActiveFlag && q.ParentID == Z1s.First().ZID).OrderBy(q => q.Title);
                    foreach (var Z2 in Z2s)
                        cL.Z2List.Add(new SelectListItem { Text = Z2.Code + " " + Z2.Title, Value = Z2.ZID.ToString(), Selected = Z2 == Z2s.First() });
                }

                //外國
                if (cL.Z0List[1].Selected)
                {
                    string[] str = Address.Split('%');
                    cL.Address1_1 = str[0];
                    cL.Address1_2 = str.Length == 2 ? str[1] : "";
                    foreach (var Z3 in Z3s)
                        cL.Z3List.Add(new SelectListItem { Text = Z3.Title, Value = Z3.ZID.ToString(), Selected = Z3.ZID == ZID });
                }
                else
                {
                    foreach (var Z3 in Z3s)
                        cL.Z3List.Add(new SelectListItem { Text = Z3.Title, Value = Z3.ZID.ToString(), Selected = Z3 == Z3s.First() });
                }
                //線上
                if (cL.Z0List[2].Selected)
                    cL.Address2 = Address;
            }

            return cL;
        }
        public PartialViewResult _Location_Meeting(int LID, FormCollection FC = null)
        {
            return PartialView(SetLocation_Meeting(LID, FC));
        }
        #endregion
        #region 地址-使用者
        public cLocation SetLocation_User(int LID, FormCollection FC)
        {
            int ZID = 46;
            string Address = "";
            if (LID > 0)
            {
                var L = DC.Location.FirstOrDefault(q => q.LID == LID);
                if (L != null)
                {
                    ZID = L.ZID;
                    Address = L.Address;
                }
            }
            cLocation cL = new cLocation();
            cL.LID = LID;
            cL.Z0List.Add(new SelectListItem { Text = "台灣", Value = "10" });
            cL.Z0List.Add(new SelectListItem { Text = "國外", Value = "2" });

            var Z1s = DC.ZipCode.Where(q => q.ActiveFlag && q.GroupName == "縣市").OrderBy(q => q.Title).ToList();
            var Z3s = DC.ZipCode.Where(q => q.ActiveFlag && q.GroupName == "國" && q.ZID != 10).OrderBy(q => q.ParentID).ThenBy(q => q.ZID).ToList();
            var Z = DC.ZipCode.FirstOrDefault(q => q.ZID == ZID);
            if (FC != null)
                if (FC.Keys.Count == 0)
                    FC = null;

            if (FC != null)
            {
                try
                {
                    cL.Z0List.First(q => q.Value == FC.Get("ddl_Zip0")).Selected = true;
                    cL.Address0 = FC.Get("txb_Address0");
                    cL.Address1_1 = FC.Get("txb_Address1_1");
                    cL.Address1_2 = FC.Get("txb_Address1_2");
                }
                catch { }
                cL.Address2 = "";
                //本國

                foreach (var Z1 in Z1s)
                    cL.Z1List.Add(new SelectListItem { Text = Z1.Title, Value = Z1.ZID.ToString(), Selected = Z1.ZID.ToString() == FC.Get("ddl_Zip1") });

                var Z2s = DC.ZipCode.Where(q => q.ActiveFlag && q.ParentID == Z1s.First().ParentID).OrderBy(q => q.Title);
                foreach (var Z2 in Z2s)
                    cL.Z2List.Add(new SelectListItem { Text = Z2.Code + " " + Z2.Title, Value = Z2.ZID.ToString(), Selected = Z2.ZID.ToString() == FC.Get("ddl_Zip2") });

                //外國

                foreach (var Z3 in Z3s)
                    cL.Z3List.Add(new SelectListItem { Text = Z3.Title, Value = Z3.ZID.ToString(), Selected = Z3.ZID.ToString() == FC.Get("ddl_Zip3") });
            }
            else if (Z != null)
            {
                if (Z.GroupName == "國")
                    cL.Z0List[1].Selected = true;
                else
                    cL.Z0List[0].Selected = true;

                //本國
                if (cL.Z0List[0].Selected)
                {
                    cL.Address0 = Address;
                    foreach (var Z1 in Z1s)
                        cL.Z1List.Add(new SelectListItem { Text = Z1.Title, Value = Z1.ZID.ToString(), Selected = Z1.ZID == Z.ParentID });

                    var Z2s = DC.ZipCode.Where(q => q.ActiveFlag && q.ParentID == Z.ParentID).OrderBy(q => q.Title);
                    foreach (var Z2 in Z2s)
                        cL.Z2List.Add(new SelectListItem { Text = Z2.Code + " " + Z2.Title, Value = Z2.ZID.ToString(), Selected = Z2.ZID == Z.ZID });
                }
                else
                {
                    foreach (var Z1 in Z1s)
                        cL.Z1List.Add(new SelectListItem { Text = Z1.Title, Value = Z1.ZID.ToString(), Selected = Z1 == Z1s.First() });

                    var Z2s = DC.ZipCode.Where(q => q.ActiveFlag && q.ParentID == Z1s.First().ZID).OrderBy(q => q.Title);
                    foreach (var Z2 in Z2s)
                        cL.Z2List.Add(new SelectListItem { Text = Z2.Code + " " + Z2.Title, Value = Z2.ZID.ToString(), Selected = Z2 == Z2s.First() });
                }

                //外國
                if (cL.Z0List[1].Selected)
                {
                    string[] str = Address.Split('%');
                    cL.Address1_1 = str[0];
                    cL.Address1_2 = str.Length == 2 ? str[1] : "";
                    foreach (var Z3 in Z3s)
                        cL.Z3List.Add(new SelectListItem { Text = Z3.Title, Value = Z3.ZID.ToString(), Selected = Z3.ZID == ZID });
                }
                else
                {
                    foreach (var Z3 in Z3s)
                        cL.Z3List.Add(new SelectListItem { Text = Z3.Title, Value = Z3.ZID.ToString(), Selected = Z3 == Z3s.First() });
                }
            }

            return cL;
        }
        public PartialViewResult _Location_User(int LID, FormCollection FC = null)
        {
            return PartialView(SetLocation_User(LID, FC));
        }
        #endregion
        #region 地址-使用者
        public cLocation SetLocation_OnLine(int LID, FormCollection FC)
        {
            cLocation cL = new cLocation();
            cL.LID = LID;
            if (FC != null)
                if (FC.Keys.Count == 0)
                    FC = null;

            if (FC != null)
                cL.Address2 = FC.Get("txb_Address2");

            return cL;
        }
        #endregion
        #region 目前組織架構參考表
        public PartialViewResult _OrganizeTopList(int OID = 0)
        {
            List<ListInput> SLs = new List<ListInput>();
            var O = DC.Organize.FirstOrDefault(q => q.ParentID == 0 && !q.DeleteFlag);
            while (O != null)
            {
                string CSS = "";
                if (!O.ActiveFlag)
                    CSS = "lab_gray";
                if (OID != 0)
                {
                    if (O.OID == OID)
                        CSS = "lab_DarkBlue";
                }


                SLs.Add(new ListInput { Title = O.Title + (string.IsNullOrEmpty(O.JobTitle) ? "" : "-" + O.JobTitle), ControlName = CSS });
                O = DC.Organize.FirstOrDefault(q => q.ParentID == O.OID);
            }

            return PartialView(SLs);
        }
        #endregion
        #region 後臺批次開課程
        public PartialViewResult _ProductClass_BatchAddCell(cClassCell CC)
        {
            return PartialView(CC);
        }

        #endregion
        #region 會員搜尋彈出視窗
        public PartialViewResult _SearchAccount()
        {
            return PartialView();
        }
        #endregion
        public PartialViewResult _HeadInclude()
        {
            return PartialView();
        }


    }
}