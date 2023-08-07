using Banner.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using static Banner.Areas.Admin.Controllers.IncludeController;

namespace Banner.Areas.Web.Controllers
{
    public class IncludeController : PublicClass
    {
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
        #region 上選單
        public string GetThisController()
        {
            string[] ThisPath = Request.Url.Segments;
            string ThisController = "";
            for (int i = 0; i < ThisPath.Length; i++)
            {
                if (ThisPath[i].ToLower() == "web/")
                {
                    ThisController = "/Web/" + ThisPath[i + 1].Replace("/", "");
                    break;
                }
            }
            return ThisController;
        }
        public string GetThisAction()
        {
            string s = "";
            string[] sPath = Request.Url.Segments;
            for (int i = 0; i < 4; i++)
                s += (i == 3 ? sPath[i].Replace("/", "") : sPath[i]);
            return s;
        }
        public int GetOIID()
        {
            int OIID = 0;
            string[] sPath = Request.Url.Segments;
            if (sPath[2].ToLower() == "groupplace/")
            {
                if (GetBrowserData("OIID") != "")
                    OIID = Convert.ToInt32(GetBrowserData("OIID"));
                if (sPath.Length == 5)
                {
                    if (sPath[3].ToLower() == "index/" || sPath[3].ToLower().Contains("_list/"))
                        OIID = Convert.ToInt32(sPath[4]);
                }
            }
            return OIID;
        }
        public PartialViewResult _TopMenu()
        {
            var Ms = DC.Menu.Where(q => q.ParentID == 0 && q.ActiveFlag && !q.DeleteFlag);
            int ACID = GetACID();
            var MOIs = GetMOIAC(0, 0, ACID);
            bool bGroupLeaderFlag = false;
            if (MOIs.Count() > 0)//是否已有小組
                bGroupLeaderFlag = MOIs.Count(q => q.OrganizeInfo.OID == 8) > 0;//是否為小組長

            if (bGroupLeaderFlag)
                Ms = Ms.Where(q => q.MenuType == 1 || q.MenuType == 2);
            else
                Ms = Ms.Where(q => q.MenuType == 1);

            Ms = Ms.OrderBy(q => q.SortNo);

            int OIID = GetOIID();

            string ThisController = GetThisController();
            string ThisActive = GetThisAction();
            List<cMenu> cMs = new List<cMenu>();
            foreach (var M in Ms)
            {
                cMenu cM = new cMenu
                {
                    MenuID = M.MID,
                    Title = M.Title,
                    Url = M.URL,
                    ImgUrl = M.ImgURL,
                    SortNo = M.SortNo,
                    SelectFlag = M.URL.StartsWith(ThisController),
                    Items = new List<cMenu>()
                };
                if (M.MenuType == 2)
                {


                    var OIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID && q.OID == 8).OrderBy(q => q.OIID);
                    foreach (var OI in OIs)
                    {
                        cM = new cMenu
                        {
                            MenuID = M.MID,
                            Title = OI.Title + M.Title,
                            Url = M.URL + "/" + OI.OIID,
                            ImgUrl = M.ImgURL,
                            SortNo = M.SortNo,
                            SelectFlag = M.URL.StartsWith(ThisController) && OI.OIID == OIID,
                            Items = new List<cMenu>()
                        };
                        cMs.Add(cM);
                    }
                }
                else
                    cMs.Add(cM);

            }

            if (ACID == 0)
                SetAlert("請先登入", 3, "/Web/Home/Login");
            return PartialView(cMs);
        }
        public PartialViewResult _TopMenu_New1()
        {
            var Ms = DC.Menu.Where(q => q.ParentID == 0 && q.ActiveFlag && !q.DeleteFlag);
            int ACID = GetACID();
            var MOIs = GetMOIAC(0, 0, ACID);
            bool bGroupLeaderFlag = false;
            if (MOIs.Count() > 0)//是否已有小組
                bGroupLeaderFlag = MOIs.Count(q => q.OrganizeInfo.OID == 8) > 0;//是否為小組長

            if (bGroupLeaderFlag)
                Ms = Ms.Where(q => q.MenuType == 1 || q.MenuType == 2);
            else
                Ms = Ms.Where(q => q.MenuType == 1);

            Ms = Ms.OrderBy(q => q.SortNo);

            int OIID = GetOIID();

            string ThisController = GetThisController();
            string ThisActive = GetThisAction();
            List<cMenu> cMs = new List<cMenu>();
            foreach (var M in Ms)
            {
                cMenu cM = new cMenu
                {
                    MenuID = M.MID,
                    Title = M.Title,
                    Url = M.URL,
                    ImgUrl = M.ImgURL,
                    SortNo = M.SortNo,
                    SelectFlag = M.URL.StartsWith(ThisController),
                    Items = new List<cMenu>()
                };

                if (M.MenuType == 2 && M.Title == "小組資訊")
                {
                    var OIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ACID == ACID && q.OID == 8).OrderBy(q => q.OIID);
                    foreach (var OI in OIs)
                    {
                        var cM1 = new cMenu
                        {
                            MenuID = M.MID,
                            Title = OI.Title,
                            Url = M.URL + "/" + OI.OIID,
                            ImgUrl = M.ImgURL,
                            SortNo = M.SortNo,
                            SelectFlag = M.URL.StartsWith(ThisController) && OI.OIID == OIID,
                            Items = GetSubItem(M.MID, bGroupLeaderFlag, OI.OIID)
                        };
                        cM.Items.Add(cM1);
                    }
                    cMs.Add(cM);
                }
                else
                {
                    cM.Items = GetSubItem(M.MID, bGroupLeaderFlag, OIID);
                    cMs.Add(cM);
                }
            }

            if (ACID == 0)
                SetAlert("請先登入", 3, "/Web/Home/Login");
            return PartialView(cMs);
        }

        private List<cMenu> GetSubItem(int MID, bool bGroupLeaderFlag, int OIID)
        {
            string NowShortPath = GetThisAction().Replace("_Edit", "").Replace("_List", "").Replace("_Info", "").Replace("_Remove", "");
            string ThisController = GetThisController();
            List<cMenu> Items = new List<cMenu>();
            var Ms = DC.Menu.Where(q => q.ParentID == MID && q.ActiveFlag && !q.DeleteFlag);
            if (bGroupLeaderFlag)
                Ms = Ms.Where(q => q.MenuType == 1 || q.MenuType == 2);
            else
                Ms = Ms.Where(q => q.MenuType == 1);

            Ms = Ms.OrderBy(q => q.SortNo);

            foreach (var M in Ms)
            {
                var CM_ = DC.Menu.FirstOrDefault(q => q.ParentID == M.MID && q.URL.StartsWith(NowShortPath));
                cMenu cM = new cMenu
                {
                    MenuID = M.MID,
                    Title = M.Title,
                    Url = M.URL + (OIID > 0 ? "/" + OIID : ""),
                    ImgUrl = M.ImgURL,
                    SortNo = M.SortNo,
                    SelectFlag = M.URL.StartsWith(ThisController) || M.URL.StartsWith(NowShortPath) || CM_ != null,
                    Items = GetSubItem(M.MID, bGroupLeaderFlag, OIID)
                };

                Items.Add(cM);
            }


            return Items;
        }
        #endregion
        #region 左側選單
        public PartialViewResult _LeftMenu()
        {
            List<cMenu> cMs = new List<cMenu>();

            int OIID = GetOIID();

            string[] ThisPath = Request.Url.Segments;
            string ThisController = GetThisController();
            var PM = DC.Menu.FirstOrDefault(q => q.ParentID == 0 && q.ActiveFlag && !q.DeleteFlag && q.URL.StartsWith(ThisController));
            if (PM != null)
            {
                var Ms = DC.Menu.Where(q => q.ParentID == PM.MID && q.ActiveFlag && !q.DeleteFlag);
                int ACID = GetACID();
                var MOIs = GetMOIAC(0, 0, ACID);
                if (MOIs.Count() > 0)//是否已有小組
                {
                    if (MOIs.Count(q => q.OrganizeInfo.OID == 8) > 0)//是否為小組長
                        Ms = Ms.Where(q => q.MenuType == 1 || q.MenuType == 2);
                    else
                        Ms = Ms.Where(q => q.MenuType == 1);
                }
                else
                    Ms = Ms.Where(q => q.MenuType == 1);
                Ms = Ms.OrderBy(q => q.SortNo);
                string NowShortPath = GetThisAction().Replace("_Edit", "").Replace("_List", "").Replace("_Info", "").Replace("_Remove", "");
                //NowShortPath = NowShortPath.Replace("_Aldult", "").Replace("_Baptized", "").Replace("_New", "");
                foreach (var M in Ms)
                {

                    var CM_ = DC.Menu.FirstOrDefault(q => q.ParentID == M.MID && q.URL.StartsWith(NowShortPath));
                    cMenu cM = new cMenu
                    {
                        MenuID = M.MID,
                        Title = M.Title,
                        Url = M.URL + (OIID > 0 ? "/" + OIID : ""),
                        ImgUrl = M.ImgURL,
                        SortNo = M.SortNo,
                        SelectFlag = M.URL.StartsWith(NowShortPath) || CM_ != null,
                        Items = new List<cMenu>()
                    };
                    //2023/7/19 取消左側多層次選單
                    /*var CMs = DC.Menu.Where(q => q.ParentID == M.MID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.SortNo);
                    foreach (var CM in CMs)
                    {
                        cMenu cCM = new cMenu
                        {
                            MenuID = CM.MID,
                            Title = CM.Title,
                            Url = CM.URL,
                            ImgUrl = CM.ImgURL,
                            SortNo = CM.SortNo,
                            SelectFlag = CM.URL.StartsWith(NowShortPath),
                            Items = new List<cMenu>()
                        };
                        var CCMs = DC.Menu.Where(q => q.ParentID == CM.MID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.SortNo);
                        foreach (var CCM in CCMs)
                        {
                            cMenu cCCM = new cMenu
                            {
                                MenuID = CCM.MID,
                                Title = CCM.Title,
                                Url = CCM.URL,
                                ImgUrl = CCM.ImgURL,
                                SortNo = CCM.SortNo,
                                SelectFlag = CCM.URL.StartsWith(NowShortPath),
                                Items = new List<cMenu>()
                            };
                            if (cCCM.SelectFlag && !cCM.SelectFlag)
                                cCM.SelectFlag = true;
                            cCM.Items.Add(cCCM);
                        }
                        if (cCM.SelectFlag && !cM.SelectFlag)
                            cM.SelectFlag = true;
                        cM.Items.Add(cCM);
                    }*/
                    cMs.Add(cM);
                }
                if (cMs.FirstOrDefault(q => q.SelectFlag) == null && cMs.Count() > 0)
                    cMs[0].SelectFlag = true;
            }


            return PartialView(cMs);
        }
        public PartialViewResult _LeftMenu_New1()
        {
            return PartialView();
        }
        #endregion

        public PartialViewResult _HeadInclude()
        {
            return PartialView();
        }
        #region 會友基本資料上方進度條(目前隱藏不啟用)

        public PartialViewResult _AccountPlaceTop()
        {
            return PartialView();
        }
        #endregion
        #region 小組長用次選單
        public PartialViewResult _GroupTopMenu()
        {
            string ThisAction = GetThisAction().Replace("_Edit", "").Replace("_Info", "");
            List<cMenu> cMs = new List<cMenu>();
            var Ms = DC.Menu.Where(q => q.ParentID == 33 && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.SortNo);
            foreach (var M in Ms)
            {
                cMenu cM = new cMenu
                {
                    MenuID = M.MID,
                    Title = M.Title,
                    Url = M.URL,
                    ImgUrl = M.ImgURL,
                    SortNo = M.SortNo,
                    SelectFlag = M.URL.StartsWith(ThisAction),
                    Items = new List<cMenu>()
                };

                var MCs = DC.Menu.Where(q => q.ParentID == M.MID && q.ActiveFlag && !q.DeleteFlag).OrderBy(q => q.SortNo);
                if (MCs.Count() > 0)
                {
                    foreach (var MC in MCs)
                    {
                        cMenu cMC = new cMenu
                        {
                            MenuID = MC.MID,
                            Title = MC.Title,
                            Url = MC.URL,
                            ImgUrl = MC.ImgURL,
                            SortNo = MC.SortNo,
                            SelectFlag = MC.URL.StartsWith(ThisAction),
                            Items = new List<cMenu>()
                        };
                        if (cMC.SelectFlag && !cM.SelectFlag)
                            cM.SelectFlag = true;
                        cM.Items.Add(cMC);
                    }
                }
                cMs.Add(cM);
            }
            return PartialView(cMs);
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
                if (Z.ZID == 10 || Z.GroupName == "鄉鎮市區" || Z.GroupName == "縣市")
                    cL.Z0List[0].Selected = true;
                else
                    cL.Z0List[1].Selected = true;

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
        #region 取得/設定聯絡方式
        public PartialViewResult _ContectEdit(Contect C, bool required = false)
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
                    ContectValue = "",
                };
            }
            cN.C = C;
            cN.required = required;
            cN.SLIs = new List<SelectListItem>();
            //cN.SLIs.Add(new SelectListItem { Text = "-國碼-", Value = "1", Selected = C.ZID == 0, Disabled = true });
            var Ns = DC.ZipCode.Where(q => q.ActiveFlag && q.GroupName == "國").OrderBy(q => q.ZID).ToList();
            foreach (var N in Ns)
                cN.SLIs.Add(new SelectListItem { Text = N.Title + "(" + N.Code + ")", Value = N.ZID.ToString(), Selected = C.ZID == N.ZID });


            if (C.ContectType == 0)
                cN.InputNote = "請輸入電話號碼";
            else if (C.ContectType == 1)
                cN.InputNote = "請輸入手機";
            else
            {
                cN.ControlName2 = "txb_Email";
                cN.InputNote = "請輸入Email";
            }

            return PartialView(cN);
        }
        #endregion
    }
}