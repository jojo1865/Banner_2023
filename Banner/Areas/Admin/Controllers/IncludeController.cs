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
            string sURL = GetShortURL().Replace("_Edit", "_List");
            if (sURL.EndsWith("/"))
                sURL = sURL.Remove(sURL.Length - 1);
            cMenu Ms = new cMenu();
            int ACID = GetACID();
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
            }
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
                        sURL += NewURL[i] + "/";
            }
            return sURL;
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
            try
            {
                int i = Convert.ToInt32(sID);
            }
            catch
            {
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
            };

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
        #region 聚會點
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
            {
                cL.Z0List.First(q => q.Value == FC.Get("ddl_Zip0")).Selected = true;
                cL.Address0 = FC.Get("txb_Address0");
                cL.Address1_1 = FC.Get("txb_Address1_1");
                cL.Address1_2 = FC.Get("txb_Address1_2");
                cL.Address2 = FC.Get("txb_Address2");
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
        public PartialViewResult _Location_Meeting(int LID)
        {
            return PartialView(SetLocation_Meeting(LID, null));
        }
        [HttpPost]
        public PartialViewResult _Location_Meeting(int LID, FormCollection FC)
        {
            return PartialView(SetLocation_Meeting(LID, FC));
        }
        #endregion
        #region 地址
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
        public PartialViewResult _Location_User(int LID)
        {
            return PartialView(SetLocation_User(LID, null));
        }
        [HttpPost]
        public PartialViewResult _Location_User(int LID, FormCollection FC)
        {
            return PartialView(SetLocation_User(LID, FC));
        }
        #endregion
        #region 目前組織架構參考表
        public PartialViewResult _OrganizeTopList()
        {
            List<ListInput> SLs = new List<ListInput>();
            var OI = DC.Organize.FirstOrDefault(q => q.ParentID == 0 && !q.DeleteFlag);
            while (OI != null)
            {
                SLs.Add(new ListInput { Title = OI.Title + (string.IsNullOrEmpty(OI.JobTitle) ? "" : "-" + OI.JobTitle) });
                OI = DC.Organize.FirstOrDefault(q => q.ParentID == OI.OID);
            }
            
            return PartialView(SLs);
        }
        #endregion
        public PartialViewResult _HeadInclude()
        {
            return PartialView();
        }
        

    }
}