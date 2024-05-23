using Banner.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Web;
using System.Web.Mvc;
using System.Windows.Media;

namespace Banner.Areas.Admin.Controllers
{
    public class QuestionSetController : PublicClass
    {

        #region 問答列表
        public class cGetQuestion_List
        {
            public cTableList cTL = new cTableList();
            public int Type = 0;
        }
        public cGetQuestion_List GetQuestion_List(int Type, FormCollection FC)
        {
            cGetQuestion_List c = new cGetQuestion_List();
            ACID = GetACID();
            #region 前端資料帶入
            int iNumCut = Convert.ToInt32(FC != null ? FC.Get("ddl_ChangePageCut") : "10");
            int iNowPage = Convert.ToInt32(FC != null ? FC.Get("hid_NextPage") : "1");
            #endregion
            c.Type = Type;
            c.cTL = new cTableList();
            c.cTL.Title = "";
            c.cTL.NowPage = iNowPage;
            c.cTL.NumCut = iNumCut;
            c.cTL.Rs = new List<cTableRow>();

            #region 表單頭
            var TopTitles = new List<cTableCell>();
            TopTitles.Add(new cTableCell { Title = "控制", WidthPX = 120 });
            TopTitles.Add(new cTableCell { Title = "順序", WidthPX = 120 });
            TopTitles.Add(new cTableCell { Title = "問題" });
            TopTitles.Add(new cTableCell { Title = "答案" });
            TopTitles.Add(new cTableCell { Title = "狀態", WidthPX = 120 });
            #endregion

            var Ns = DC.Question_Answer.Where(q => !q.DeleteFlag && q.QType == Type);
            #region 前端資料帶入
            if (FC != null)
            {
                Error = "";
                foreach (var N in Ns)
                {
                    //變更排序
                    string sSort = FC.Get("txb_Sort_" + N.QID);
                    if (!string.IsNullOrEmpty(sSort))
                    {
                        int iSort = 0;
                        if (sSort != N.SortNo.ToString() && int.TryParse(sSort, out iSort))
                        {
                            N.SortNo = iSort;
                            N.UpdDate = DT;
                            N.SaveACID = ACID;
                            DC.SubmitChanges();

                            if (Error == "")
                                Error = "更新排序完成";
                        }
                    }
                }
                if (Error != "")
                    SetAlert(Error, 1); 
            }
            
            #endregion

            c.cTL.Rs.Add(SetTableRowTitle(TopTitles));
            c.cTL.TotalCt = Ns.Count();
            c.cTL.MaxNum = GetMaxNum(c.cTL.TotalCt, c.cTL.NumCut);
            Ns = Ns.OrderBy(q => q.SortNo).ThenBy(q => q.QID).Skip((iNowPage - 1) * c.cTL.NumCut).Take(c.cTL.NumCut);

            #region 內容
            foreach (var N in Ns)
            {
                cTableRow cTR = new cTableRow();
                cTR.ID = N.QID;

                cTR.Cs.Add(new cTableCell { Value = "編輯", Type = "button", URL = "ShowQuestion(" + N.QID + ")" });//控制
                cTR.Cs.Add(new cTableCell { Value = N.SortNo.ToString(), Type = "input-number", ControlName = "txb_Sort_" });//順序
                cTR.Cs.Add(new cTableCell { Value = N.Question });//問題
                cTR.Cs.Add(new cTableCell { Value = N.Answer });//答案
                if (N.ActiveFlag)
                    cTR.Cs.Add(new cTableCell { Value = "啟用", CSS = "btn btn-outline-success", Type = "activebutton", URL = (bGroup[2] ? "ChangeActive(this,'Question'," + N.QID + ")" : "javascript:alert('無修改權限')") });//狀態
                else
                    cTR.Cs.Add(new cTableCell { Value = "停用", CSS = "btn btn-outline-danger", Type = "activebutton", URL = (bGroup[2] ? "ChangeActive(this,'Question'," + N.QID + ")" : "javascript:alert('無修改權限')") });//狀態
                c.cTL.Rs.Add(SetTableCellSortNo(cTR));
            }
            #endregion

            return c;
        }
        [HttpGet]
        public ActionResult Question_List(int ID)
        {
            GetViewBag();
            return View(GetQuestion_List(ID, null));
        }
        [HttpPost]
        public ActionResult Question_List(int ID, FormCollection FC)
        {
            GetViewBag();
            return View(GetQuestion_List(ID, FC));
        }
        #endregion
        #region 取得問題
        public string GetQuestion()
        {
            int QID = GetQueryStringInInt("QID");
            var Q = DC.Question_Answer.FirstOrDefault(q => q.QID == QID && !q.DeleteFlag);
            if (Q == null)
                Q = new Question_Answer();

            return JsonConvert.SerializeObject(Q);
        }
        #endregion
        #region 問題存檔
        public class cInput_Question
        {
            public int QID { get; set; }
            public int QType { get; set; }
            public string Question { get; set; }
            public string Answer { get; set; }
        }
        [HttpGet]
        public string SaveQuestion(cInput_Question cQ)
        {
            Error = "";

            ACID = GetACID();
            if (ACID == 0)
                Error = "請先登入";
            else if (string.IsNullOrEmpty(cQ.Question))
                Error = "請輸入問題";
            else if (cQ.Question.Length > 100)
                Error = "問題字數過長";
            else if (string.IsNullOrEmpty(cQ.Answer))
                Error = "請輸入答案";
            if (Error == "")
            {
                var Q = DC.Question_Answer.FirstOrDefault(q => q.QID == cQ.QID && !q.DeleteFlag);
                if (Q == null)
                {
                    int MaxSort = 1;
                    var Qs = DC.Question_Answer.Where(q => q.QType == cQ.QType && !q.DeleteFlag);
                    if (Qs.Count() > 0)
                        MaxSort = Qs.Max(q => q.SortNo) + 1;

                    Q = new Question_Answer
                    {
                        QType = cQ.QType,
                        SortNo = MaxSort,
                        Question = cQ.Question,
                        Answer = cQ.Answer,
                        ActiveFlag = true,
                        DeleteFlag = false,
                        CreDate = DT,
                        UpdDate = DT,
                        SaveACID = ACID
                    };
                    DC.Question_Answer.InsertOnSubmit(Q);
                    DC.SubmitChanges();
                }
                else
                {
                    Q.Question = cQ.Question;
                    Q.Answer = cQ.Answer;
                    DC.SubmitChanges();
                }
                Error = "OK";
            }

            return Error;
        }
        #endregion
    }
}