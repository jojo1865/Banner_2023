using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Banner.Models
{
    public class PublicModel
    {
    }

    /// <summary>
    /// 選單
    /// </summary>
    public class cMenu
    {
        public int MenuID { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string ImgUrl { get; set; }
        public int SortNo { get; set; }
        public bool SelectFlag { get; set; }
        public List<cMenu> Items { get; set; }
    }

    /// <summary>
    /// 表格
    /// </summary>
    public class cTableList
    {
        public int NumCut = 10;//分頁數字一次顯示幾個
        public int MaxNum = 0;//分頁數量最多多少
        public int TotalCt = 0;//全部共多少資料
        public int NowPage = 1;//目前所在頁數
        public string NowURL = "";
        public int CID = 0;
        public int ATID = 0;
        public string Title = "";
        public List<cTableRow> Rs = null;
        public bool ShowFloor = true;
    }
    public class cTableRow
    {
        public string CSS = "border-dark";
        public int ID = 0;
        public int SortNo = 0;
        public DateTime UpdDate = DateTime.Now;
        public List<cTableCell> Cs = new List<cTableCell>();
    }
    public class cTableCell
    {
        //public int TargetID = 0;
        public int WidthPX = 0;
        public string Type = "string";//string/link/Media/img/linkimg/input-string/input-number/checkbox/textarea
        public string ControlName = "";//有控制項時使用
        public string Title = "";
        public string URL = "";
        public string ImgURL = "";
        public string Target = "_self";
        public string CSS = "";
        public string Value = "";
        public bool Disabled = false;
        public int SortNo = 0;
        public List<cTableCell> cTCs = new List<cTableCell>();
    }
    //下拉選單物件
    public class ListSelect
    {
        public string Title = "";
        public string ControlName = "";
        public int SortNo = 0;
        public List<SelectListItem> ddlList = new List<SelectListItem>();
    }
    public class ListInput
    {
        public string Title = "";
        public string ControlName = "";
        public int SortNo = 0;
        public string InputData = "";
    }

    public class cLocation
    {
        public int LID = 0;
        public List<SelectListItem> Z0List = new List<SelectListItem>();
        public List<SelectListItem> Z1List = new List<SelectListItem>();
        public List<SelectListItem> Z2List = new List<SelectListItem>();
        public List<SelectListItem> Z3List = new List<SelectListItem>();
        public string Address0 = "", Address1_1 = "", Address1_2 = "", Address2 = "";
    }
    public class c_ContectEdit
    {
        public Contect C = new Contect();
        public string ControlName1 = "ddl_PhoneZip";
        public string ControlName2 = "txb_PhoneNo";
        public string InputNote = "";
        public bool required = false;
        public bool doublecheck = false;
        public List<SelectListItem> SLIs = new List<SelectListItem>();
    }
    /// <summary>
    /// 樹狀圖結構
    /// </summary>
    public class cTree
    {
        public string name = "";
        public string title = "";
        public string url = "";
        public string css = "";
        public int SortNo = 0;
        public int LV = 0;
        public List<cTree> children = new List<cTree>();
    }
    //講師
    public class cTeacher_List
    {
        public int PID = 0;
        public int ActiveType = -1;
        public string sKey = "";
        public string sGroupKey = "";
        public cTableList cTL = new cTableList();
    }

    public class cCheckType
    {
        public string Title = "";
        public int SortNo = 0;
        public string URL;
    }
    //批次課程
    public class cClassCell
    {
        public int OIID { get; set; } = 0;//課程所屬旌旗
        public string SortNo { get; set; } = "%";//流水號 內定值=%
        public string ClassTitle { get; set; } = "";//課堂名稱
        public string ClassSDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");//開課日期
        public string minClassSDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd");//開課日期
        public int ClassCutDay { get; set; } = 7;//相隔週期
        public int ClassCt { get; set; } = 10;//上課堂數
        public string ClassSTime { get; set; } = "09:00";//上課時間-始
        public string ClassETime { get; set; } = "12:00";//上課時間-末
        public string ClassPhoneNo { get; set; } = "";//連絡電話
        public string ClassLocationName { get; set; } = "";//地標名稱
        public string ClassMeetURL { get; set; } = "";//網址
        public string ClassAddress { get; set; } = "";//上課地點
        public int ClassPeopleCt { get; set; } = 0;//人數限制
        public int ClassGraduateDate { get; set; } = 7;//結業準備天數
        public int ClassTeacher_ID { get; set; } = 0;//講師ID
        public int ProductType { get; set; } = 0;//商品類型	0:不限制/1:實體/2:線上
        public string ClassTeacher_Name { get; set; } = "請搜尋";//講師搜尋按鈕顯示文字
        public List<SelectListItem> cTs { get; set; } = new List<SelectListItem>();//講師下拉選單
    }
}