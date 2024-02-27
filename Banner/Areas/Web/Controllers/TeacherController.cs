using Banner.Models;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
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
            public cTableList cTL = new cTableList();
        }
        public cGetStudent_List GetStudent_List(int ID, FormCollection FC)
        {
            cGetStudent_List c = new cGetStudent_List();
            c.PCID = ID;
            c.cTL = new cTableList();
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            c.cTL.Title = "學員清單";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "學員名稱", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "打卡" });
            TopTitles.Add(new cTableCell { Title = "結業", WidthPX = 100 });
            TopTitles.Add(new cTableCell { Title = "轉班" });
            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));

            if (FC != null)
            {
                c.Name = FC.Get("txb_Name");
            }
            ACID = GetACID();
            var T = DC.Teacher.FirstOrDefault(q => q.ACID == ACID && q.ActiveFlag && !q.DeleteFlag);
            if (T == null)
            {

                SetAlert("您目前並未具備講師資格,不能使用本功能", 2, "/Web/Home/Index");
            }
            else
            {
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
                foreach (var N in Ns)
                {
                    var OP = OPs.FirstOrDefault(q => q.Order_Header.ACID == N.ACID);
                    int OJ_Ct = DC.Order_Join.Count(q => q.OPID == OP.OPID);
                    cTableRow cTR = new cTableRow();
                    cTR.Cs.Add(new cTableCell { Value = N.Name_First + N.Name_Last });//學員名稱

                    cTR.Cs.Add(new cTableCell
                    {
                        Value = (OJ_Ct + "/" + OP.Product_Class.Product_ClassTime.Count),
                        Type = "linkbutton",
                        URL = "/Web/Teacher/Student_JoinLog",
                        Target = "_blank"
                    });//打卡

                    cTR.Cs.Add(new cTableCell { Value = OP.Graduation_Flag ? "V" : "" });//結業

                    if (OP.Graduation_Flag)//已結業
                        cTR.Cs.Add(new cTableCell { Value = "" });//轉班
                    else
                        cTR.Cs.Add(new cTableCell { 
                            Value = "申請轉班",
                            Type = "linkbutton",
                            URL = "javascript:alert('!!')"
                        });//轉班
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

    }
}