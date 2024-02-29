using Antlr.Runtime.Tree;
using Banner.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Web.Controllers
{
    public class TeacherController : PublicClass
    {
        #region 老師開放的打卡ORCode
        [HttpGet]
        public ActionResult Index(int ID)
        {
            GetViewBag();
            ACID = GetACID();
            ViewBag._CSS1 = "/Areas/Web/Content/css/form.css";

            string sQRCode_URL = Create_QRCode("/Web/Teacher/Class_Join/" + ID);
            TempData["QRCode_URL"] = sQRCode_URL;
            TempData["JoinGroup_URL"] = ("/Web/AccountPlace/Class_Join/" + ID);

            var MLS = (from q in DC.M_Product_Teacher.Where(q => q.PCID == ID)
                       join p in DC.Teacher.Where(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag)
                       on q.TID equals p.TID
                       select q).FirstOrDefault();
            if (MLS != null)
            {
                var CT = MLS.Product_Class.Product_ClassTime.FirstOrDefault(q => q.ClassDate.Date == DT.Date);
                if (CT != null)
                {
                    DateTime STime = ChangeTimeSpanToDateTime(CT.ClassDate, CT.STime);
                    DateTime ETime = ChangeTimeSpanToDateTime(CT.ClassDate, CT.ETime);
                    string str = "課程上課時間:" + STime.ToString(DateTimeFormat) + " ~ " + ETime.ToString(DateTimeFormat);
                    if (STime.AddMinutes(EventQrCodeSTimeAdd) < DT && ETime.AddMinutes(EventQrCodeETimeAdd) > DT)
                    {
                        TempData["QRCode_Show"] = true;
                        TempData["QRCode_Msg"] = str;
                    }
                    else
                    {
                        TempData["QRCode_Show"] = false;
                        TempData["QRCode_Msg"] = "現在並非上課時間...<br/>" + str;
                    }

                }
                else
                {
                    TempData["QRCode_Show"] = false;
                    TempData["QRCode_Msg"] = "查無班級上課時間...";
                }

            }
            else
            {
                TempData["QRCode_Show"] = false;
                TempData["QRCode_Msg"] = "查無班級上課時間...";
            }

            return View();
        }
        #endregion
        #region 打卡
        [HttpGet]
        public ActionResult Class_Join(int ID)
        {
            GetViewBag();
            ACID = GetACID();
            if (ACID <= 0)
                SetAlert("請先登入再打卡", 2, "/Web/Home/Login");
            else
            {
                var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag);
                if (AC == null)
                    SetAlert("您的帳號已被關閉或移除,請洽系統管理員", 2, "/Web/Home/Logout");
                else
                {
                    var O = DC.Order_Product.FirstOrDefault(q => q.PCID == ID && q.Order_Header.ACID == AC.ACID && q.Order_Header.Order_Type == 2 && !q.Order_Header.DeleteFlag);
                    if (O == null)
                        SetAlert("您並未訂購此課程或尚未完成繳費,暫時不允許打卡", 2, "/Web/MyClass/MyOrder_List");
                    else
                    {
                        var CT = DC.Product_ClassTime.FirstOrDefault(q => q.ClassDate.Date == DT.Date && q.PCID == ID);
                        if (CT == null)
                            SetAlert("現在並非上課時間...", 2, "/Web/Home/Index");
                        else
                        {
                            DateTime STime = ChangeTimeSpanToDateTime(CT.ClassDate, CT.STime);
                            DateTime ETime = ChangeTimeSpanToDateTime(CT.ClassDate, CT.ETime);
                            string str = "課程上課時間:" + STime.ToString(DateTimeFormat) + " ~ " + ETime.ToString(DateTimeFormat);
                            if (STime.AddMinutes(EventQrCodeSTimeAdd) < DT && ETime.AddMinutes(EventQrCodeETimeAdd) > DT)
                            {
                                Order_Join OJ = new Order_Join
                                {
                                    Order_Product = O,
                                    Product_ClassTime = CT,
                                    DeleteFlag = false,
                                    CreDate = DT,
                                    UpdDate = DT,
                                    SaveACID = ACID
                                };
                                DC.Order_Join.InsertOnSubmit(OJ);
                                DC.SubmitChanges();
                                SetAlert("您已完成打卡", 1, "/Web/Home/Index");
                            }
                            else
                                SetAlert("現在並非上課時間...<br/>" + str, 2, "/Web/Home/Index");
                        }
                    }
                }
            }
            return View();
        }
        #endregion
        #region 學生列表
        public class cGetStudent_List
        {
            public string Name = "";
            public string ClassTitle = "";
            public int PCID = 0;
            public string GraduationDate = "";
            public cTableList cTL = new cTableList();
        }
        public cGetStudent_List GetStudent_List(int ID, FormCollection FC)
        {
            cGetStudent_List c = new cGetStudent_List();
            c.PCID = ID;

            var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == ID);
            if (PC != null)
            {
                c.ClassTitle = PC.Product.Title + " " + PC.Product.SubTitle + " " + PC.Title;
                c.GraduationDate = "請於" + PC.GraduateDate.ToString(DateFormat) + "前完成結業操作";
            } 
            else
                SetAlert("查無此課程", 2, "/Web/Home/Index");

            c.cTL = new cTableList();
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.cTL.Title = "學員清單";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "選擇", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "學員名稱", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "打卡紀錄" });
            TopTitles.Add(new cTableCell { Title = "結業", WidthPX = 100 });
            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));

            var OPs = DC.Order_Product.Where(q => q.Order_Header.Order_Type == 2 && !q.Order_Header.DeleteFlag && q.PCID == ID);
            var Ns = from q in DC.Account.Where(q => !q.DeleteFlag)
                     join p in OPs.GroupBy(q => q.Order_Header.ACID)
                     on q.ACID equals p.Key
                     select q;

            if (!string.IsNullOrEmpty(c.Name))
                Ns = Ns.Where(q => (q.Name_First + q.Name_Last).Contains(c.Name));

            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderByDescending(q => q.Name_First).ThenByDescending(q => q.Name_Last).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);
            ACID = GetACID();

            if (FC != null)
            {
                c.Name = FC.Get("txb_Name");
                int iCt = 0;
                if (FC.Get("btn_Graduation") != null)
                {
                    if (PC != null)
                    {
                        if (PC.GraduateDate.Date < DT.Date)
                            SetAlert("可設定結業的時間已經過了");
                        else
                        {
                            foreach (var OP in OPs)
                            {
                                if (GetViewCheckBox(FC.Get("cbox_" + OP.Order_Header.ACID)))
                                {
                                    if (!OP.Graduation_Flag)
                                    {
                                        OP.Graduation_Flag = true;
                                        OP.Graduation_Date = DT;
                                        OP.Graduation_ACID = ACID;
                                        DC.SubmitChanges();

                                        iCt++;
                                    }
                                }
                            }
                            if (iCt > 0)
                                SetAlert(iCt + "位學員結業成功", 1);
                            else
                                SetAlert("學員結業失敗", 2);
                        }
                    }
                }
            }

            var T = DC.Teacher.FirstOrDefault(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag);
            if (T == null)
                SetAlert("您目前並未具備講師資格,不能使用本功能", 2, "/Web/Home/Index");
            else
            {
                foreach (var N in Ns)
                {
                    var OP = OPs.FirstOrDefault(q => q.Order_Header.ACID == N.ACID);
                    int OJ_Ct = DC.Order_Join.Count(q => q.OPID == OP.OPID);
                    cTableRow cTR = new cTableRow();
                    cTR.ID = N.ACID;
                    cTR.Cs.Add(new cTableCell
                    {
                        Type = "checkbox",
                        ControlName = "cbox_"
                    });//選擇
                    cTR.Cs.Add(new cTableCell { Value = N.Name_First + N.Name_Last });//學員名稱

                    cTR.Cs.Add(new cTableCell
                    {
                        Value = (OJ_Ct + "/" + OP.Product_Class.Product_ClassTime.Count),
                        Type = "linkbutton",
                        URL = "/Web/Teacher/Student_JoinLog/" + OP.PCID + "?ACID=" + N.ACID,
                        Target = "_blank"
                    });//打卡

                    cTR.Cs.Add(new cTableCell { Value = OP.Graduation_Flag ? "已結業" : "尚未結業" });//結業
                    c.cTL.Rs.Add(SetTableCellSortNo(cTR));
                }

            }

            return c;
        }
        [HttpGet]
        public ActionResult Student_List(int ID)
        {
            GetViewBag();
            return View(GetStudent_List(ID, null));
        }
        [HttpPost]
        public ActionResult Student_List(int ID, FormCollection FC)
        {
            GetViewBag();
            return View(GetStudent_List(ID, FC));
        }
        #endregion
        #region 學生列表
        public class cGetStudent_JoinLog
        {
            public string ClassTitle = "";
            public int PCID = 0;
            public int ACID = 0;
            public cTableList cTL = new cTableList();
        }
        public cGetStudent_JoinLog GetStudent_JoinLog(int ID, FormCollection FC)
        {
            cGetStudent_JoinLog c = new cGetStudent_JoinLog();
            c.ACID = GetQueryStringInInt("ACID");
            c.PCID = ID;

            var PC = DC.Product_Class.FirstOrDefault(q => q.PCID == ID);
            if (PC != null)
                c.ClassTitle = PC.Product.Title + " " + PC.Product.SubTitle + " " + PC.Title;

            c.cTL = new cTableList();
            //int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            //int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.cTL.Title = "學員清單";
            c.cTL.NowPage = 1;
            c.cTL.NumCut = 0;
            c.cTL.Rs = new List<cTableRow>();
            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "選擇", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "上課日期" });
            TopTitles.Add(new cTableCell { Title = "上課時間" });
            TopTitles.Add(new cTableCell { Title = "打卡時間" });
            TopTitles.Add(new cTableCell { Title = "打卡人" });
            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));

            var Ns = DC.Product_ClassTime.Where(q => q.PCID == ID).OrderBy(q => q.ClassDate).ThenBy(q => q.STime);
            c.cTL.TotalCt = Ns.Count();
            int UID = GetACID();
            if (FC != null)
            {
                int iCt = 0;
                if (FC.Get("btn_AddJoin") != null)
                {
                    foreach (var N in Ns)
                    {
                        if (GetViewCheckBox(FC.Get("cbox_" + N.PCTID)))
                        {
                            var OJT = DC.Order_Join.FirstOrDefault(q => q.Product_ClassTime.PCID == ID && q.Order_Product.Order_Header.ACID == ACID && !q.DeleteFlag && q.PCTID == N.PCTID);
                            if (OJT == null)
                            {
                                var OP = DC.Order_Product.FirstOrDefault(q => q.Order_Header.ACID == c.ACID && q.Order_Header.Order_Type == 2 && !q.Order_Header.DeleteFlag);
                                if (OP != null)
                                {
                                    OJT = new Order_Join
                                    {
                                        Order_Product = OP,
                                        PCTID = N.PCTID,
                                        DeleteFlag = false,
                                        CreDate = DT,
                                        UpdDate = DT,
                                        SaveACID = UID
                                    };
                                    DC.Order_Join.InsertOnSubmit(OJT);
                                    DC.SubmitChanges();

                                    iCt++;
                                }
                            }
                        }
                    }
                    if (iCt > 0)
                        SetAlert("補打卡" + iCt + "成功", 1);
                    else
                        SetAlert("補打卡失敗", 2);
                }
            }
            ACID = GetACID();
            var T = DC.Teacher.FirstOrDefault(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag);
            if (T == null)
                SetAlert("您目前並未具備講師資格,不能使用本功能", 2, "/Web/Home/Index");
            else
            {
                #region 列表
                foreach (var N in Ns)
                {
                    var OJT = DC.Order_Join.FirstOrDefault(q => q.Product_ClassTime.PCID == ID && q.Order_Product.Order_Header.ACID == c.ACID && !q.DeleteFlag && q.PCTID == N.PCTID);
                    cTableRow cTR = new cTableRow();
                    cTR.ID = N.PCTID;
                    cTR.Cs = new List<cTableCell>();
                    if (OJT != null)
                    {
                        if (!OJT.Order_Product.Graduation_Flag)//尚未結業
                        {
                            cTR.Cs.Add(new cTableCell
                            {
                                Type = "checkbox",
                                ControlName = "cbox_"
                            });//選擇
                        }
                        else
                            cTR.Cs.Add(new cTableCell { Value = "" });
                    }
                    else
                    {
                        cTR.Cs.Add(new cTableCell
                        {
                            Type = "checkbox",
                            ControlName = "cbox_"
                        });//選擇
                    }

                    cTR.Cs.Add(new cTableCell { Value = N.ClassDate.ToString(DateFormat) });//上課日期
                    cTR.Cs.Add(new cTableCell { Value = GetTimeSpanToString(N.STime) + "~" + GetTimeSpanToString(N.ETime) });//上課時間
                    if (OJT != null)
                    {
                        cTR.Cs.Add(new cTableCell { Value = OJT.CreDate.ToString(DateTimeFormat) });//打卡時間
                        cTR.Cs.Add(new cTableCell { Value = (OJT.ACID == ACID ? "本人" : "講師代打") });//打卡人
                    }
                    else
                    {
                        cTR.Cs.Add(new cTableCell { Value = "--" });//打卡時間
                        cTR.Cs.Add(new cTableCell { Value = "--" });//打卡人
                    }
                    c.cTL.Rs.Add(SetTableCellSortNo(cTR));
                }
                #endregion
            }

            return c;
        }
        [HttpGet]
        public ActionResult Student_JoinLog(int ID)
        {
            GetViewBag();
            return View(GetStudent_JoinLog(ID, null));
        }
        [HttpPost]
        public ActionResult Student_JoinLog(int ID, FormCollection FC)
        {
            GetViewBag();
            return View(GetStudent_JoinLog(ID, FC));
        }
        #endregion
    }
}