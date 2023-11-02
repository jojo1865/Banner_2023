using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data.Linq.SqlClient;
using System.Collections;
using System.Data;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
using System.Web.UI.WebControls;
using ZXing;
using Banner.Models;
using System.Net.Mail;
using System.Net;

using OfficeOpenXml;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Data.SqlTypes;
using System.Web.Helpers;

namespace Banner
{
    public class PublicClass : Controller
    {
        public DataClassesDataContext DC { get; set; }
        public DateTime DT = DateTime.Now;
        public string[] sWeeks = new string[] { "請選擇", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六", "星期日" };
        public string[] sTimeSpans = new string[] { "上午", "下午", "晚上" };
        public string sPhotoFileName = "Photo";//圖片存檔資料夾名稱
        public string sNoPhoto = "/Photo/NoPhoto.jpg";
        public string sFileName = "Files";//下載檔案資料夾名稱
        public string sAdminPhotoFilePath = "../../../";//從後台最底層到圖片存檔資料夾的回推路徑
        public string DateFormat = "yyyy-MM-dd";
        public string DateTimeFormat = "yyyy-MM-dd HH:mm";
        public string CompanyTitle = "【全球旌旗資訊網】";
        public bool bUsedNewName = true;
        public bool[] bGroup = new bool[] { false, false, false, false, false, false }; //權限
        public static List<SelectListItem> ddl_EducationTypes = new List<SelectListItem> {
                new SelectListItem{ Text="國小",Value="0",Selected=true},
                new SelectListItem{ Text="國中",Value="1"},
                new SelectListItem{ Text="高中職",Value="2"},
                new SelectListItem{ Text="大專",Value="3"},
                new SelectListItem{ Text="研究所",Value="4"}
            };
        public static List<SelectListItem> ddl_JobTypes = new List<SelectListItem> {
                new SelectListItem{ Text="一般職業",Value="0",Selected=true},
                new SelectListItem{ Text="農牧業",Value="1"},
                new SelectListItem{ Text="漁業",Value="2"},
                new SelectListItem{ Text="木材森林業",Value="3"},
                new SelectListItem{ Text="礦業採石業",Value="4"},
                new SelectListItem{ Text="交通運輸業",Value="5"},
                new SelectListItem{ Text="餐旅業",Value="6"},
                new SelectListItem{ Text="建築工程業",Value="7"},
                new SelectListItem{ Text="製造業",Value="8"},
                new SelectListItem{ Text="新聞廣告業",Value="9"},
                new SelectListItem{ Text="衛生保健業",Value="10"},
                new SelectListItem{ Text="娛樂業",Value="11"},
                new SelectListItem{ Text="文教機關",Value="12"},
                new SelectListItem{ Text="宗教團體",Value="13"},
                new SelectListItem{ Text="公共事業",Value="14"},
                new SelectListItem{ Text="一般商業",Value="15"},
                new SelectListItem{ Text="服務業",Value="16"},
                new SelectListItem{ Text="家庭管理",Value="17"},
                new SelectListItem{ Text="治安人員",Value="18"},
                new SelectListItem{ Text="軍人",Value="19"},
                new SelectListItem{ Text="資訊業",Value="20"},
                new SelectListItem{ Text="運動人員",Value="21"},
                new SelectListItem{ Text="學生",Value="22"},
                new SelectListItem{ Text="其他",Value="23"},
            };
        public string[] CommunityTitle = new string[] { "其他", "LineID", "InstagramID", "WeChat" };
        public string[] JoinTitle = new string[] { "無意願", "已入組未落戶", "跟進中(已分發)", "被退回(未分發)", "跟進中(未分發)" };
        public string[] FamilyTitle = new string[] { "父親", "母親", "配偶", "緊急聯絡人", "子女" };
        public string[] BaptizedType = new string[] { "未受洗", "已受洗(旌旗)", "已受洗(非旌旗)" };
        public string[] sCourseType = new string[] {"不限制", "實體", "線上" };
        public string Error = "";
        public int iChildAge = 12;
        public int ACID = 0;
        public PublicClass()
        {
            DC = new DataClassesDataContext(System.Web.Configuration.WebConfigurationManager.ConnectionStrings["db_BannerConnectionString"].ConnectionString);
        }
        #region 一般程式
        //取得登入者ID
        public int GetACID()
        {
            string sACID = "", cACID = "";
            try
            {
                sACID = GetSession("ACID");
                cACID = GetCookie("ACID");
                if (sACID == "" && cACID != "")//Session遺失但Cookie還在
                {
                    Session.Add("ACID", cACID);
                    SetCookie("ACID", cACID);
                    sACID = cACID;
                }
                else if (cACID == "" && sACID != "")//Cookie遺失但Session還在
                    SetCookie("ACID", sACID);
                else if (cACID != "" && sACID == "")//Session遺失但Cookie還在
                    Session.Add("ACID", cACID);
                else if (cACID == sACID)
                {
                    if (sACID != "")//Session/Cookie都還在
                        SetCookie("ACID", cACID);
                    else//Session/Cookie都遺失
                    {
                    }
                }
            }
            catch { sACID = ""; }

            int ACID = 0;
            try { ACID = Convert.ToInt32(sACID); }
            catch { }
            return ACID;
        }
        //取得網站暫存資料
        public string GetBrowserData(string Title = "ACID")
        {
            string sStr = "", cStr = "";
            sStr = GetSession(Title);
            cStr = GetCookie(Title);
            if (cStr == "" && cStr != "")//Session遺失但Cookie還在
            {
                Session.Add(Title, cStr);
                SetCookie(Title, cStr);
                sStr = cStr;
            }
            else if (cStr == "" && sStr != "")//Cookie遺失但Session還在
            {
                cStr = sStr;
                SetCookie(Title, sStr);
            }
            else if (cStr != "" && sStr == "")//Session遺失但Cookie還在
            {
                SetSession(Title, cStr);
            }
            else if (cStr == sStr)
            {
                if (sStr != "")//Session/Cookie都還在
                    SetCookie(Title, cStr);
                else//Session/Cookie都遺失
                {
                }
            }
            else { }
            return cStr;
        }
        //存入網站暫存資料
        public void SetBrowserData(string Title, string sData)
        {
            SetSession(Title, sData);
            SetCookie(Title, sData);
        }
        public void DelBrowserData(string Title)
        {
            DelSession(Title);
            DelCookie(Title);
        }
        //登入前台
        public void LogInAC(long ACID)
        {
            SetSession("ACID", ACID.ToString());
            SetCookie("ACID", ACID.ToString());
        }
        //登出前台
        public void LogOutAC()
        {
            DelSession("ACID");
            DelCookie("ACID");
        }
        //檢查是否為Admin權限
        public bool CheckAdmin(int ACID)
        {
            if (ACID == 1)
                return true;
            else
            {
                var PA = GetMRAC(0, ACID).FirstOrDefault(q => q.Rool.RoolType == 4);
                return PA != null;
            }
        }
        //取得被加密的名子
        public string CutName(string sName)
        {
            if (sName.Length == 2)
                sName = sName.Substring(0, 1) + new string('O', 1);
            else if (sName.Length > 2)
                sName = sName.Substring(0, 1) + new string('O', (sName.Length - 2 > 0 ? sName.Length - 2 : 0)) + sName.Substring(sName.Length - 1, 1);
            else { }
            return sName;
        }
        //設定Session
        public void SetSession(string sTitle, string sValue)
        {
            if (Session[sTitle] == null)
                Session.Add(sTitle, sValue);
            else
                Session[sTitle] = sValue;
        }
        //取得Session
        public string GetSession(string sTitle)
        {
            string sValue = "";
            if (Session[sTitle] != null)
                if (Session[sTitle].ToString() != "")
                    sValue = Session[sTitle].ToString();
            return sValue;
        }
        //移除Session
        public void DelSession(string sTitle)
        {
            if (Session[sTitle] != null)
                Session[sTitle] = null;
        }
        //設定Cookie
        public void SetCookie(string sTitle, string sValue)
        {
            Response.Cookies[sTitle].Value = sValue;
            Response.Cookies[sTitle].Expires = DateTime.Now.AddDays(1);
        }
        //取得Cookie
        public string GetCookie(string sTitle)
        {
            string sValue = "";
            if (Request.Cookies[sTitle] != null)
                if (Request.Cookies[sTitle].Value != "")
                    sValue = Request.Cookies[sTitle].Value;
            return sValue;
        }
        //移除Cookie
        public void DelCookie(string sTitle)
        {
            Response.Cookies[sTitle].Value = "";
            Response.Cookies[sTitle].Expires = DateTime.Now.AddDays(-1);
        }
        //取得QuertString並檢查是否為long
        public long GetQueryStringInLong(string sTitle)
        {
            long l = 0;
            if (Request.QueryString[sTitle] != null)
            {
                try { l = Convert.ToInt64(Request.QueryString[sTitle].ToString()); }
                catch { l = 0; }
            }
            return l;
        }
        //取得QuertString並檢查是否為int
        public int GetQueryStringInInt(string sTitle)
        {
            int i = 0;
            if (Request.QueryString[sTitle] != null)
            {
                try { i = Convert.ToInt32(Request.QueryString[sTitle].ToString()); }
                catch { i = 0; }
            }
            return i;
        }
        //取得QuertString並檢查是否為string
        public string GetQueryStringInString(string sTitle)
        {
            string s = "";
            if (Request.QueryString[sTitle] != null)
                s = Request.QueryString[sTitle].ToString().Replace("'", "\"");
            return s;
        }
        //檢查Email格式
        public bool CheckEmail(string strIn)
        {
            // Return true if strIn is in valid e-mail format.
            if (strIn != "" && strIn != null)
                return Regex.IsMatch(strIn, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
            else
                return false;
        }
        //檢查手機
        public bool CheckCellPhone(string strIn)
        {
            bool bOK = false;
            if (strIn.Replace(" ", "") != "")
            {
                try
                {
                    long CellPhone = Convert.ToInt64(strIn);
                    if (CellPhone.ToString().Length == 9 && CellPhone.ToString().StartsWith("9"))
                    {
                        bOK = true;
                    }
                }
                catch
                {

                }
            }
            return bOK;
        }
        //檢查身分證字號
        public bool CheckSSN(string sID)
        {
            if (string.IsNullOrEmpty(sID))
                return false;   //沒有輸入，回傳 ID 錯誤
            sID = sID.ToUpper();
            var regex = new Regex("^[A-Z]{1}[0-9]{9}$");
            if (!regex.IsMatch(sID))
                return false;   //Regular Expression 驗證失敗，回傳 ID 錯誤

            int[] seed = new int[10];       //除了檢查碼外每個數字的存放空間
            string[] charMapping = new string[] { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K", "L", "M", "N", "P", "Q", "R", "S", "T", "U", "V", "X", "Y", "W", "Z", "I", "O" };
            //A=10 B=11 C=12 D=13 E=14 F=15 G=16 H=17 J=18 K=19 L=20 M=21 N=22
            //P=23 Q=24 R=25 S=26 T=27 U=28 V=29 X=30 Y=31 W=32  Z=33 I=34 O=35
            string target = sID.Substring(0, 1);
            for (int index = 0; index < charMapping.Length; index++)
            {
                if (charMapping[index] == target)
                {
                    index += 10;
                    seed[0] = index / 10;       //10進制的高位元放入存放空間
                    seed[1] = (index % 10) * 9; //10進制的低位元*9後放入存放空間
                    break;
                }
            }
            for (int index = 2; index < 10; index++)
            {   //將剩餘數字乘上權數後放入存放空間
                seed[index] = Convert.ToInt32(sID.Substring(index - 1, 1)) * (10 - index);
            }
            //檢查是否符合檢查規則，10減存放空間所有數字和除以10的餘數的個位數字是否等於檢查碼
            //(10 - ((seed[0] + .... + seed[9]) % 10)) % 10 == 身分證字號的最後一碼
            return (10 - (seed.Sum() % 10)) % 10 == Convert.ToInt32(sID.Substring(9, 1));
        }
        //檢查密碼-可檢查長度/大寫英文/小寫英文/數字/符號
        public bool CheckPasswork(string Input)
        {
            char[] Cs = Input.ToCharArray();
            bool English = false;
            bool Number = false;
            bool Up_English = false;
            bool Low_Englich = false;
            bool Length8 = Cs.Length >= 8;
            bool Symbol = false;
            for (int i = 0; i < Cs.Length; i++)
            {
                if (!Number && char.IsDigit(Cs[i]))
                    Number = true;
                else if (!Up_English && char.IsUpper(Cs[i]))
                    Up_English = true;
                else if (!Low_Englich && char.IsLower(Cs[i]))
                    Low_Englich = true;
                else if (!Symbol && char.IsSymbol(Cs[i]))
                    Symbol = true;

                if (!English && (Up_English || Low_Englich))
                    English = true;
            }


            return Length8 && Up_English && Low_Englich && Number;
        }
        //判斷這天星期幾
        public int GetDayNo(DateTime ThisDate)
        {
            if (ThisDate.DayOfWeek == DayOfWeek.Monday)
                return 1;
            else if (ThisDate.DayOfWeek == DayOfWeek.Tuesday)
                return 2;
            else if (ThisDate.DayOfWeek == DayOfWeek.Wednesday)
                return 3;
            else if (ThisDate.DayOfWeek == DayOfWeek.Thursday)
                return 4;
            else if (ThisDate.DayOfWeek == DayOfWeek.Friday)
                return 5;
            else if (ThisDate.DayOfWeek == DayOfWeek.Saturday)
                return 6;
            else// if (ThisDate.DayOfWeek == DayOfWeek.Sunday)
                return 7;
        }
        //取得台灣農曆日期
        public DateTime GetChineseDate(DateTime ThisChineseDate)
        {
            DateTime DT_ = DateTime.Now;
            TaiwanLunisolarCalendar tlc = new TaiwanLunisolarCalendar();
            // 取得目前支援的農曆日曆到幾年幾月幾日( 2051-02-10 )
            if (DT.Date < tlc.MaxSupportedDateTime.Date)
            {
                // 取得今天的農曆年月日
                DT_ = Convert.ToDateTime(tlc.GetYear(DateTime.Now).ToString() + "/" + tlc.GetMonth(DateTime.Now).ToString() + "/" + tlc.GetDayOfMonth(DateTime.Now).ToString());
            }
            return DT_;
        }
        //回傳當天是星期幾(中文)
        public string GetChineseDayNo(DateTime ThisDate, int iType = 3)
        {
            string sOutput = "";
            int DayNo = GetDayNo(ThisDate);
            string[] No_1 = new string[] { "", "一", "二", "三", "四", "五", "六", "日" };
            if (iType == 1)
                sOutput = No_1[DayNo];
            else if (iType == 2)
                sOutput = "週" + No_1[DayNo];
            else
                sOutput = "星期" + No_1[DayNo];
            return sOutput;
        }
        //產生QRCode
        public string Create_QRCode(string TargetURL, string LogoURL = "", int QR_size = 300)
        {
            if (LogoURL != "" && QR_size < 170)
            {
                return "Error:包含Logo的QRCode尺寸不能小於170!";
            }
            else
            {
                BarcodeWriter bw = new BarcodeWriter();
                bw.Format = BarcodeFormat.QR_CODE;
                bw.Options.Width = QR_size;
                bw.Options.Height = QR_size;
                Bitmap bp = bw.Write(TargetURL);

                if (LogoURL != "")
                {
                    //Logo圖
                    System.Drawing.Image img = System.Drawing.Image.FromFile(Server.MapPath(LogoURL));
                    Bitmap bp_img = new Bitmap(img);

                    using (Graphics g = Graphics.FromImage(bp))
                    {
                        Point imgPoint = new Point((bp.Width - (bp_img.Width / 2)) / 2, (bp.Height - (bp_img.Height / 2)) / 2);
                        g.DrawImage(bp_img, imgPoint.X, imgPoint.Y, (bp_img.Width / 2), (bp_img.Height / 2));
                    }
                }

                //存檔
                string sPath = Server.MapPath("../Photo/QRCode/"); //存檔位置
                CheckFileExist(sPath);
                string NewPath = (LogoURL != "" ? "Logo_" : "") + "Code" + "." + System.Drawing.Imaging.ImageFormat.Png;
                bp.Save(sPath + NewPath);

                return "../Photo/QRCode/" + NewPath;
            }
        }
        //Linq->DataTable
        static DataTable LinqQueryToDataTable<T>(IEnumerable<T> query)
        {
            DataTable tbl = new DataTable();
            PropertyInfo[] props = null;
            foreach (T item in query)
            {
                if (props == null) //尚未初始化
                {
                    Type t = item.GetType();
                    props = t.GetProperties();
                    foreach (PropertyInfo pi in props)
                    {
                        Type colType = pi.PropertyType;
                        //針對Nullable<>特別處理
                        if (colType.IsGenericType
                            && colType.GetGenericTypeDefinition() == typeof(Nullable<>))
                            colType = colType.GetGenericArguments()[0];
                        //建立欄位
                        tbl.Columns.Add(pi.Name, colType);
                    }
                }
                DataRow row = tbl.NewRow();
                foreach (PropertyInfo pi in props)
                    row[pi.Name] = pi.GetValue(item, null) ?? DBNull.Value;
                tbl.Rows.Add(row);
            }
            return tbl;
        }

        //取得郵遞區號祖宗18代(由樹枝回朔到樹幹)
        public List<ZipCode> GetOldZip(int ZID, int ActiveType = 1, List<ZipCode> Zs = null)
        {
            var C = DC.ZipCode.FirstOrDefault(q => q.ZID == ZID && (ActiveType == 0 ? true : q.ActiveFlag == (ActiveType == 1)));
            if (C != null)
            {
                if (C.ParentID > 0)
                {
                    Zs = GetOldZip(C.ParentID, ActiveType, Zs);
                    Zs.Add(C);
                }
                else
                {
                    if (Zs == null)
                        Zs = new List<ZipCode>();
                    Zs.Add(C);
                }
            }
            return Zs;
        }
        //由郵遞區號找前面的地址(限台灣
        public string GetZipData(int ZID, bool ShowZipCodeFlag = false)
        {
            string sOutput = "";
            if (ZID > 1)
            {
                var Z = DC.ZipCode.FirstOrDefault(q => q.ZID == ZID);

                var Z0 = DC.ZipCode.FirstOrDefault(q => q.ZID == Z.ParentID);
                if (Z0 != null)
                    sOutput = Z.Code + Z0.Title + Z.Title;
                else
                    sOutput = Z.Code + Z.Title;

            }
            return sOutput;
        }

        //換算日期格式,失敗回傳今天
        public DateTime CnahgeDateTime(string Input)
        {
            DateTime D = DateTime.Now;
            if (DateTime.TryParse(Input, out D))
                return D;
            else
                return DT;
        }
        //換算數字,失敗回傳0
        public int CnahgeInt(string Input)
        {
            int i = 0;
            if (int.TryParse(Input, out i))
                return i;
            else
                return 0;
        }
        //換算浮點數,失敗回傳0
        public float CnahgeFloat(string Input)
        {
            float f = 0;
            if (float.TryParse(Input, out f))
                return f;
            else
                return 0;
        }
        public Encoding CheckCode(string FilePath)
        {
            Stream reader = System.IO.File.Open(FilePath, FileMode.Open, FileAccess.Read);
            Encoding encoder = null;
            byte[] header = new byte[4];
            // 讀取前四個Byte
            reader.Read(header, 0, 4);
            if (header[0] == 0xFF && header[1] == 0xFE)
            {
                // UniCode File
                reader.Position = 2;
                encoder = Encoding.Unicode;
            }
            else if (header[0] == 0xEF && header[1] == 0xBB && header[2] == 0xBF)
            {
                // UTF-8 File
                reader.Position = 3;
                encoder = Encoding.UTF8;
            }
            else
            {
                // Default Encoding File
                reader.Position = 0;
                encoder = Encoding.Default;
            }
            /*byte[] buffer = new byte[1024];
            int source = reader.Read(buffer, 0, 1024);
            string sSource = string.Empty;
            while (source > 0)
            {
                sSource += encoder.GetString(buffer, 0, source);
                source = reader.Read(buffer, 0, 1024);
            }*/
            reader.Close();
            return encoder;
        }
        //16進位色碼轉換
        public System.Drawing.Color HexColor(String hex)
        {
            //將井字號移除
            hex = hex.Replace("#", "");

            byte a = 255;
            byte r = 255;
            byte g = 255;
            byte b = 255;
            int start = 0;

            //處理ARGB字串 
            if (hex.Length == 8)
            {
                a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                start = 2;
            }

            // 將RGB文字轉成byte
            r = byte.Parse(hex.Substring(start, 2), System.Globalization.NumberStyles.HexNumber);
            g = byte.Parse(hex.Substring(start + 2, 2), System.Globalization.NumberStyles.HexNumber);
            b = byte.Parse(hex.Substring(start + 4, 2), System.Globalization.NumberStyles.HexNumber);

            return System.Drawing.Color.FromArgb(a, r, g, b);
        }

        //檢查上傳檔案副檔名是否為真的檔案(文件類)
        public bool CheckOfficeFile(FileUpload fu)
        {
            bool CheckByte = false;

            Stream s = fu.PostedFile.InputStream;
            byte[] buffer = new byte[s.Length];
            s.Read(buffer, 0, Convert.ToInt32(s.Length - 1));

            /*只檢查前4個檔頭*/
            string FilesNum = "";
            for (int i = 0; (s.Length < 4 ? i < s.Length - 1 : i < 4); i++)
            {
                FilesNum = FilesNum + (buffer[i]).ToString();
            }

            switch (FilesNum) //16進位--->10進位
            {
                case "20820717224": //doc,ppt,xls
                    CheckByte = true;
                    break;
                case "807534": //docx,pptx,xlsx,pdf,zip,odt,odf
                    CheckByte = true;
                    break;
                case "829711433": //rar
                    CheckByte = true;
                    break;
                default:
                    break;
            }

            return CheckByte;
        }

        //撿查信用卡(銀行ID,卡別)
        public string CheckCard(string CardNo)
        {
            CardNo = CardNo.Replace("-", "");
            string sOutput = "";
            if (CardNo.Length > 6)
            {
                long Top6 = Convert.ToInt64(CardNo.Replace("-", "").Substring(0, 6));

                if (CardNo.StartsWith("4") && !CardNo.StartsWith("4903") && !CardNo.StartsWith("4905") && !CardNo.StartsWith("4911") && !CardNo.StartsWith("4936"))//Visa
                    sOutput = "VISA";
                else if ((Top6 / 10000) >= 51 && (Top6 / 10000) <= 55)//萬事達
                    sOutput = "Master";
                else if (CardNo.StartsWith("34") || CardNo.StartsWith("37"))//美國運通
                    sOutput = "American Express";
                else if (CardNo.StartsWith("62"))//中國銀聯
                    sOutput = "China UnionPay";
                else if ((Top6 / 100) >= 3528 && (Top6 / 100) <= 3589)//JCB
                    sOutput = "JCB";
                else if (((Top6 / 1000) >= 300 && (Top6 / 1000) <= 305) || ((Top6 / 100) >= 2014 && (Top6 / 100) <= 305))//大來卡
                    sOutput = "Diners Club Carte Blanche";
                else { }
            }
            return sOutput;
        }
        //把HTML轉String
        public string ChangeHTMLToString(string sInput)
        {
            string sOutput = "";
            string[] slist = (sInput.Replace("<br>", "\n").Replace("<br />", "\n").Replace("<br/>", "\n").Replace("< /br>", "\n").Replace("</br>", "\n").Replace("&nbsp;", " ")).Split('<');
            if (slist.Length > 1)
            {
                for (int k = 0; k < slist.Length; k++)
                {
                    try
                    {
                        sOutput += slist[k].Split('>')[1];
                    }
                    catch
                    {
                    }
                }
            }
            else
                sOutput = sInput;

            return sOutput;
        }
        //把16進位碼改為10進位
        public int[] Change16To10(string sInput)
        {

            if (sInput.Length % 2 == 1)
            {
                sInput = "0" + sInput;
            }
            string[] Hex = new string[sInput.Length / 2];
            int[] iOutPut = new int[sInput.Length / 2];
            for (int i = 0; i < Hex.Length; i++)
            {
                Hex[i] = sInput.Substring(2 * i, 2);
                iOutPut[i] = Convert.ToInt32(Hex[i], 16);
            }
            return iOutPut;
        }
        //取得亂數(特別規則)
        public int GetRand()
        {
            int Rentdoms = 0;
            for (int x = 0; x <= 10; x++)
            {
                byte[] randomNumber = new byte[1];
                RNGCryptoServiceProvider Gen = new RNGCryptoServiceProvider();
                Gen.GetBytes(randomNumber);
                int rand = Convert.ToInt32(randomNumber[0]);
                Rentdoms = Rentdoms * 10 + (rand % 6 + 1);
            }

            return Math.Abs(Rentdoms % 1000000);
        }
        //取亂數(0~Max-1)
        public int GetRand(int Max)
        {
            int Rentdoms = 0;
            for (int x = 0; x <= 10; x++)
            {
                byte[] randomNumber = new byte[1];
                RNGCryptoServiceProvider Gen = new RNGCryptoServiceProvider();
                Gen.GetBytes(randomNumber);
                int rand = Convert.ToInt32(randomNumber[0]);
                Rentdoms = Rentdoms * 10 + (rand % 6 + 1);
            }
            return Math.Abs(Rentdoms % Max);
        }
        // 計算兩個日期時間差
        public static int[] TimespanToDate(DateTime BigOne, DateTime SmallOne)
        {
            int[] Times = new int[6];
            // 因為只需取量，不決定誰大誰小，所以如果self < target時要交換將大的擺前面
            if (BigOne < SmallOne)
            {
                DateTime tmp = SmallOne;
                SmallOne = BigOne;
                BigOne = tmp;
            }
            // 將年轉換成月份以便用來計算
            Times[1] = 12 * (BigOne.Year - SmallOne.Year) + (BigOne.Month - SmallOne.Month);
            // 如果天數要相減的量不夠時要向月份借天數補滿該月再來相減
            if (BigOne.Day < SmallOne.Day)
            {
                Times[1]--;
                Times[2] = DateTime.DaysInMonth(SmallOne.Year, SmallOne.Month) - SmallOne.Day + BigOne.Day;
            }
            else
            {
                Times[2] = BigOne.Day - SmallOne.Day;
            }

            // 天數計算完成後將月份轉成年
            Times[0] = Times[1] / 12;
            Times[1] = Times[1] % 12;

            TimeSpan TS = BigOne.Subtract(SmallOne).Duration();

            Times[3] = TS.Hours;
            Times[4] = TS.Minutes;
            Times[5] = TS.Seconds;
            return Times;
        }
        //產生圖形驗證碼
        public string GenerateCheckCode()
        {
            int number;
            char code;
            string checkCode = String.Empty;

            System.Random random = new Random();

            /*for (int i = 0; i < 5; i++)
            {
                number = random.Next();

                if (number % 2 == 0)
                    code = (char)('0' + (char)(number % 10));
                else
                    code = (char)('A' + (char)(number % 26));

                checkCode += code.ToString();
            }*/

            /*for (int i = 0; i < 6; i++)
            {
                number = random.Next();

                if (number % 2 == 0)
                    code = (char)('0' + (char)(number % 10));
                else
                    code = (char)('A' + (char)(number % 26));

                if (code != '0' && code != 'O')
                    checkCode += code.ToString();
                else
                    i--;
            }*/
            for (int i = 0; i < 6; i++)
            {
                number = random.Next();
                code = (char)('0' + (char)(number % 10));
                checkCode += code.ToString();
            }



            return checkCode;
        }
        //計算分頁最高頁數
        public int GetMaxNum(int TotalCt, int NumCut)
        {
            return NumCut > 0 ? (TotalCt % NumCut > 0 ? (TotalCt / NumCut) + 1 : TotalCt / NumCut) : 0;
        }
        //發Email
        public string SendMail(string toMail, string toName, string subject, string body)
        {
            try
            {
                var fromAddress = new MailAddress("bannerchurch22@gmail.com", "教會系統服務");
                var fromPassword = "rgwwqagxmmbqvkkf";

                //這邊改成 忘記帳號人的Request.Mail跟 對應到的User.Name
                //var ToMail = Request.Mail;
                //var ToName = select Name from User where Email1 = '"+Request.Mail+"';


                var toAddress = new MailAddress(toMail, toName);
                //---------------------


                //var fromAddress = new MailAddress("allens0426@gmail.com", "教會系統服務");
                //var toAddress = new MailAddress("allens0426@gmail.com", "ALLEN");
                //const string fromPassword = "lxeyiqdmzieppnhj";//"Bannerchurch@2022";
                //const string subject = "忘記帳號";
                //const string body = "您的帳號=>OOOOO";

                var smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(fromAddress.Address, fromPassword)
                };
                var message = new MailMessage(fromAddress, toAddress)
                {
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };
                smtp.Send(message);
                return "";


            }
            catch (Exception ex)
            {
                return ex.Message;
            }

        }
        public string SendSNS(string toNumber, string title, string body)
        {
            try
            {
                var PhoneNumber = toNumber;
                if (PhoneNumber.First() == '+')
                {
                    PhoneNumber = toNumber.Substring(1);
                }
                else if (PhoneNumber.First() == '0')
                {
                    PhoneNumber = "886" + PhoneNumber.Substring(1);
                }

                var sms = new SMSHttp();
                var smsId = "gamma";
                var smsPw = "Admin@1234";
                sms.sendSMS(smsId, smsPw, title, body, "",
                            "+" + PhoneNumber, "", "", "", false, false, false);
                return "";
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }
        #endregion
        #region 簡訊發送
        /// <summary>
        /// SMS
        /// </summary>
        public class SMSHttp
        {
            private string sendSMSUrl = "https://api.e8d.tw/API21/HTTP/sendSMS.ashx";
            private string sendParamSMSUrl = "https://api.e8d.tw/API21/HTTP/SendParamSMS.ashx";
            private string sendMMSUrl = "https://api.e8d.tw/API21/HTTP/MMS/sendMMS.ashx";
            private string getCreditUrl = "https://api.e8d.tw/API21/HTTP/getCredit.ashx";
            private string processMsg = "";
            private string batchID = "";
            private double credit = 0;

            public SMSHttp()
            {
            }

            /// <summary>
            /// 傳送簡訊
            /// </summary>
            /// <param name="userID">帳號</param>
            /// <param name="password">密碼</param>
            /// <param name="subject">簡訊主旨，主旨不會隨著簡訊內容發送出去。用以註記本次發送之用途。可傳入空字串。</param>
            /// <param name="content">簡訊發送內容(SMS一般、SMS參數、MMS一般簡訊需填寫)</param>
            /// <param name="param">簡訊Param內容(參數、個人化(專屬)簡訊需填寫Json格式)</param>
            /// <param name="mobile">接收人之手機號碼。格式為: +886900000001。多筆接收人時，請以半形逗點隔開( , )，如+886900000001,+886900000002。</param>
            /// <param name="sendTime">簡訊預定發送時間。-立即發送：請傳入空字串。-預約發送：請傳入預計發送時間，若傳送時間小於系統接單時間，將不予傳送。格式為YYYYMMDDhhmnss；例如:預約2009/01/31 15:30:00發送，則傳入20090131153000。若傳遞時間已逾現在之時間，將立即發送。</param>
            /// <param name="attachment">image base64</param>
            /// <param name="type">圖檔副檔名</param>
            /// <param name="isParam">是否為SMS參數簡訊</param>
            /// <param name="isPersonal">是否為SMS個人化(專屬)簡訊</param>
            /// <param name="isMMS">是否為MMS一般簡訊</param>
            /// <returns>true:傳送成功；false:傳送失敗</returns>
            public bool sendSMS(string userID, string password, string subject, string content, string param, string mobile, string sendTime, string attachment, string type, bool isParam, bool isPersonal, bool isMMS)
            {
                bool success = false;
                StringBuilder postDataSb = new StringBuilder();
                string resultString = string.Empty;
                string[] split = null;

                try
                {
                    #region UrlEncode
                    subject = !isParam && !isPersonal ? HttpUtility.UrlEncode(subject) : subject;
                    content = !isParam && !isPersonal ? HttpUtility.UrlEncode(content) : content;
                    mobile = !isParam && !isPersonal ? HttpUtility.UrlEncode(mobile) : mobile;
                    attachment = isMMS ? HttpUtility.UrlEncode(attachment) : attachment;
                    #endregion

                    #region 檢查時間格式
                    if (!string.IsNullOrEmpty(sendTime))
                    {
                        try
                        {
                            //檢查傳送時間格式是否正確
                            DateTime checkDt = DateTime.ParseExact(sendTime, "yyyyMMddHHmmss", System.Globalization.CultureInfo.CurrentCulture);
                            if (!sendTime.Equals(checkDt.ToString("yyyyMMddHHmmss")))
                            {
                                processMsg = "傳送時間格式錯誤";
                                return success;
                            }
                        }
                        catch
                        {
                            processMsg = "傳送時間格式錯誤";
                            return success;
                        }
                    }
                    #endregion

                    #region SMS一般簡訊
                    if (!isParam && !isPersonal && !isMMS)
                    {
                        postDataSb.Append("UID=").Append(userID);
                        postDataSb.Append("&PWD=").Append(password);
                        postDataSb.Append("&SB=").Append(subject);
                        postDataSb.Append("&MSG=").Append(content);
                        postDataSb.Append("&DEST=").Append(mobile);
                        postDataSb.Append("&ST=").Append(sendTime);
                        resultString = httpPost(sendSMSUrl, postDataSb.ToString(), false);
                    }
                    #endregion

                    #region SMS參數簡訊
                    if (isParam)
                    {
                        //「發送內容」範例(msg)：測試%field1%%field2%
                        //「Param內容」範例(param)：[{"Name":"test_A","Mobile":"+886900000001","Email":"testA@test.com.tw","SendTime":"29990109083000","Param":"A1|A2|||"},{"Name":"test_B","Mobile":"+886900000002","Email":"testB@test.com.tw","SendTime":"29990125173000","Param":"B1|B2|||"}]
                        postDataSb.Append("{\"UID\":\"").Append(userID).Append("\",");
                        postDataSb.Append("\"PWD\":\"").Append(password).Append("\",");
                        postDataSb.Append("\"SB\":\"").Append(subject).Append("\",");
                        postDataSb.Append("\"MSG\":\"").Append(content).Append("\",");
                        postDataSb.Append("\"RecipientDataList\":").Append(param).Append("}");
                        resultString = httpPost(sendParamSMSUrl, postDataSb.ToString(), isParam);
                    }
                    #endregion

                    #region SMS個人化(專屬)簡訊
                    if (isPersonal)
                    {
                        //「Param內容」範例(param)：[{"Name":"test_A","Mobile":"+886900000001","Email":"testA@test.com.tw","SendTime":"29990109083000","Param":"測試A1A2"},{"Name":"test_B","Mobile":"+886900000002","Email":"testB@test.com.tw","SendTime":"29990125173000","Param":"測試B1B2"}]
                        postDataSb.Append("{\"UID\":\"").Append(userID).Append("\",");
                        postDataSb.Append("\"PWD\":\"").Append(password).Append("\",");
                        postDataSb.Append("\"SB\":\"").Append(subject).Append("\",");
                        postDataSb.Append("\"RecipientDataList\":").Append(param).Append("}");
                        resultString = httpPost(sendParamSMSUrl, postDataSb.ToString(), isPersonal);
                    }
                    #endregion

                    #region MMS一般簡訊
                    if (isMMS)
                    {
                        postDataSb.Append("UID=").Append(userID);
                        postDataSb.Append("&PWD=").Append(password);
                        postDataSb.Append("&SB=").Append(subject);
                        postDataSb.Append("&MSG=").Append(content);
                        postDataSb.Append("&DEST=").Append(mobile);
                        postDataSb.Append("&ST=").Append(sendTime);
                        postDataSb.Append("&ATTACHMENT=").Append(attachment);
                        postDataSb.Append("&TYPE=").Append(type);
                        resultString = httpPost(sendMMSUrl, postDataSb.ToString(), false);
                    }
                    #endregion

                    processMsg = resultString;

                    #region SMS、MMS一般簡訊發送結果
                    if (!isParam && !isPersonal && !resultString.StartsWith("-"))
                    {
                        /* 
                         * 傳送成功 回傳字串內容格式為：CREDIT,SENDED,COST,UNSEND,BATCH_ID，各值中間以逗號分隔。
                         * CREDIT：發送後剩餘點數。負值代表發送失敗，系統無法處理該命令
                         * SENDED：發送通數。
                         * COST：本次發送扣除點數
                         * UNSEND：無額度時發送的通數，當該值大於0而剩餘點數等於0時表示有部份的簡訊因無額度而無法被發送。
                         * BATCH_ID：批次識別代碼。為一唯一識別碼，可藉由本識別碼查詢發送狀態。格式範例：220478cc-8506-49b2-93b7-2505f651c12e
                         */
                        split = resultString.Split(',');
                        credit = Convert.ToDouble(split[0]);
                        batchID = split[4];
                        return success = true;
                    }
                    #endregion

                    #region SMS參數、個人化(專屬)簡訊發送結果
                    if ((isParam || isPersonal) && resultString.Contains("true"))
                    {
                        /* 
                         * 傳送成功 回傳字串內容格式為：{"Result":true,"Status":"0","Msg":"CREDIT,SENDED,COST,UNSEND,BATCH_ID"}
                         * CREDIT：發送後剩餘點數。負值代表發送失敗，系統無法處理該命令
                         * SENDED：發送通數。
                         * COST：本次發送扣除點數
                         * UNSEND：無額度時發送的通數，當該值大於0而剩餘點數等於0時表示有部份的簡訊因無額度而無法被發送。
                         * BATCH_ID：批次識別代碼。為一唯一識別碼，可藉由本識別碼查詢發送狀態。格式範例：220478cc-8506-49b2-93b7-2505f651c12e
                         */
                        int s = resultString.IndexOf("Msg") + 6;
                        int e = resultString.Length - 2;
                        split = resultString.Substring(s, e - s).Split(',');
                        credit = Convert.ToDouble(split[0]);
                        batchID = split[4];
                        return success = true;
                    }
                    #endregion

                    //傳送失敗
                    processMsg = resultString;

                }
                catch (Exception ex)
                {
                    processMsg = ex.ToString();
                }
                return success;
            }

            /// <summary>
            /// 取得帳號餘額
            /// </summary>
            /// <returns>true:取得成功；false:取得失敗</returns>
            public bool getCredit(string userID, string password)
            {
                bool success = false;
                try
                {
                    StringBuilder postDataSb = new StringBuilder();
                    postDataSb.Append("UID=").Append(userID);
                    postDataSb.Append("&PWD=").Append(password);

                    string resultString = httpPost(getCreditUrl, postDataSb.ToString(), false);
                    if (!resultString.StartsWith("-"))
                    {
                        credit = Convert.ToDouble(resultString);
                        success = true;
                    }
                    else
                    {
                        processMsg = resultString;
                    }
                }
                catch (Exception ex)
                {
                    processMsg = ex.ToString();
                }
                return success;
            }

            private string httpPost(string url, string postData, bool isSendParamSMS)
            {
                string result = "";
                try
                {

                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(new Uri(url));
                    request.Method = "POST";
                    if (!isSendParamSMS)
                    {
                        request.ContentType = "application/x-www-form-urlencoded";
                    }
                    else
                    {
                        request.ContentType = "application/json";
                    }
                    byte[] bs = System.Text.Encoding.UTF8.GetBytes(postData);
                    request.ContentLength = bs.Length;
                    request.GetRequestStream().Write(bs, 0, bs.Length);
                    //取得 WebResponse 的物件 然後把回傳的資料讀出
                    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                    StreamReader sr = new StreamReader(response.GetResponseStream());
                    result = sr.ReadToEnd();
                }
                catch (Exception ex)
                {
                    processMsg = ex.ToString();
                }
                return result;
            }

            public string ProcessMsg
            {
                get
                {
                    return processMsg;
                }
            }

            public string BatchID
            {
                get
                {
                    return batchID;
                }
            }

            public double Credit
            {
                get
                {
                    return credit;
                }
            }
        }
        #endregion
        #region 檢驗圖檔

        public static FileExtension CheckPhoroFile(string fileName)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read);
            System.IO.BinaryReader br = new System.IO.BinaryReader(fs);
            string fileType = string.Empty; ;
            try
            {
                byte data = br.ReadByte();
                fileType += data.ToString();
                data = br.ReadByte();
                fileType += data.ToString();
                FileExtension extension;
                try
                {
                    extension = (FileExtension)Enum.Parse(typeof(FileExtension), fileType);
                }
                catch
                {
                    extension = FileExtension.VALIDFILE;
                }
                return extension;
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (fs != null)
                {
                    fs.Close();
                    br.Close();
                }
            }
        }
        public enum FileExtension
        {
            //圖檔
            BMP = 6677,
            JPG = 255216,
            GIF = 7173,
            PNG = 13780,
            //Flash
            /*SWF = 6787,
            */
            //壓縮檔
            /*RAR = 8297,
            ZIP = 8075,
            _7Z = 55122,*/
            VALIDFILE = 9999999
        }
        #endregion
        #region 圖片處理
        //判斷檔案是否被綁住
        public bool IsFileLocked(string file)
        {
            try
            {
                using (System.IO.File.Open(file, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    return false;
                }
            }
            catch (IOException exception)
            {
                var errorCode = Marshal.GetHRForException(exception) & 65535;
                return errorCode == 32 || errorCode == 33;
            }
            catch (Exception)
            {
                return false;
            }
        }
        //儲存檔案的前置操作(產生資料夾)
        public void CheckFileExist(string OutputPaht)
        {
            if (!Directory.Exists(OutputPaht))//不存在則自動建立相對應的目錄。
                Directory.CreateDirectory(OutputPaht);
        }
        //儲存上傳的照片(順便圖片縮放)

        public string[] UpLoadSizePhoto(FileUpload Uploading, string sPath, string sNewPath, int iWidth, int iHeight)
        {
            string[] strReturn = new string[4] { "Error", "0", "0", "" };

            //若有檔案即進行存檔
            if (Uploading.HasFile)
            {
                strReturn[3] = Uploading.FileName;
                //=================== 處理上傳檔案 ===================
                //Guid 類別的 NewGuid 方法可產生具唯一性的字串, 作為照片的檔名
                //System.IO.Path.GetExtension 方法可取得上傳檔案的副檔名,ServerFilename 變數為照片在伺服器上的檔名
                //int Top = (DateTime.Now.Year - 2000) * 10000 + (DateTime.Now.Month) * 100 + (DateTime.Now.Day);
                //int Rentdoms = Top * (10 * s.Length) + Convert.ToInt32(s);
                //string Rentdoms = DateTime.Now.ToString("yyyyMMddHHmm") + "_" + GetRand().ToString();
                string Rentdoms = bUsedNewName ? DT.ToString("yyyyMMddHHmm") + "_" + GetRand() : strReturn[3];
                //string ServerFilename = Rentdoms + Path.GetExtension(Uploading.FileName);
                string ServerFilename = bUsedNewName ? Rentdoms + Path.GetExtension(Uploading.FileName) : strReturn[3];
                //Page.MapPath() 方法可以取得檔案或目錄在伺服器上的絕對路徑,ServerPathFilename 變數為照片在伺服器上的絕對路徑
                string ServerPathFilename = sPath + ServerFilename;
                //將上傳的檔案儲存到 ServerPathFilename 所指定的路徑
                //byte[] FileByte = Uploading.FileBytes;
                Uploading.SaveAs(ServerPathFilename);

                //檢查檔案
                string sFileType = CheckPhoroFile(ServerPathFilename).ToString().ToLower();

                string RealName = ServerFilename.Split('.')[0] + "." + sFileType.ToLower();
                if (sFileType == "jpg" || sFileType == "png" || sFileType == "bmp")
                {
                    //以 System.Drawing.Image 類別建立 Photo 物件, 然後將原始圖讀入 Photo 物件
                    FileStream FS = System.IO.File.OpenRead(ServerPathFilename);
                    System.Drawing.Image Photo = System.Drawing.Image.FromStream(FS);
                    // 縮圖運算
                    int[] Size = SetImgSize(Photo.Height, Photo.Width, iHeight, iWidth);

                    #region 大小調整
                    //生成新图=新建一个bmp图片
                    System.Drawing.Image newImage = new System.Drawing.Bitmap(Size[1], Size[0]);
                    /*if (bHighType)
                    {*/
                    //新建一个画板
                    System.Drawing.Graphics newG = System.Drawing.Graphics.FromImage(newImage);
                    //画图
                    newG.DrawImage(Photo, new Rectangle(0, 0, Size[1], Size[0]), new Rectangle(0, 0, Photo.Width, Photo.Height), GraphicsUnit.Pixel);
                    //newG.DrawImage(Photo, new Rectangle(0, 0, Size[1], Size[0]), new Rectangle(0, 0, Size[1], Size[0]), GraphicsUnit.Pixel);
                    newG.Dispose();
                    //newImage.Dispose();
                    //}

                    #endregion
                    strReturn[1] = Size[0].ToString();
                    strReturn[2] = Size[1].ToString();
                    Photo.Dispose();
                    FS.Close();
                    //保存
                    if (!System.IO.File.Exists(sNewPath + Path.ChangeExtension(ServerFilename, sFileType)))
                    {
                        if (System.IO.File.Exists(sNewPath + RealName))
                            System.IO.File.Delete(sNewPath + RealName);

                        newImage.Save(sNewPath + RealName);
                        newImage.Dispose();

                        if (System.IO.File.Exists(sPath + ServerFilename))
                            System.IO.File.Delete(sPath + ServerFilename);
                    }
                    ChangePhotoSize(130, sNewPath + RealName, ServerPathFilename);

                    strReturn[0] = Path.ChangeExtension(RealName, sFileType.ToLower());
                }
                else if (sFileType == "gif")
                {
                    #region GIF

                    //以 System.Drawing.Image 類別建立 Photo 物件, 然後將原始圖讀入 Photo 物件
                    //Uploading.SaveAs(sNewPath + ServerFilename);
                    Uploading.SaveAs(sNewPath + RealName);
                    FileStream FS = System.IO.File.OpenRead(sNewPath + RealName);
                    System.Drawing.Image Photo = System.Drawing.Image.FromStream(FS);
                    strReturn[1] = Photo.Height.ToString();
                    strReturn[2] = Photo.Width.ToString();
                    Photo.Dispose();
                    FS.Close();
                    //保存
                    if (!System.IO.File.Exists(sPath + Path.ChangeExtension(RealName, sFileType)))
                    {
                        System.IO.File.Copy(sPath + ServerFilename, sPath + Path.ChangeExtension(RealName, sFileType));
                        if (System.IO.File.Exists(sPath + RealName))
                            System.IO.File.Delete(sPath + RealName);
                    }
                    strReturn[0] = Path.ChangeExtension(ServerFilename, sFileType.ToLower());

                    #endregion
                }
                else
                {
                    if (System.IO.File.Exists(ServerPathFilename))
                        System.IO.File.Delete(ServerPathFilename);
                }
                return strReturn;
            }
            else
            {
                return strReturn;
            }
        }
        //儲存上傳的照片 
        public string[] UploadPhoto(FileUpload Uploading, string sPath)
        {
            string[] strReturn = new string[4] { "Error", "0", "0", "" };
            //若有檔案即進行存檔
            if (Uploading.HasFile)
            {
                strReturn[3] = Uploading.FileName;
                //=================== 處理上傳檔案 ===================
                //Guid 類別的 NewGuid 方法可產生具唯一性的字串, 作為照片的檔名
                //System.IO.Path.GetExtension 方法可取得上傳檔案的副檔名,ServerFilename 變數為照片在伺服器上的檔名

                //int Top = (DateTime.Now.Year - 2000) * 10000 + (DateTime.Now.Month) * 100 + (DateTime.Now.Day);
                //int Rentdoms = Top * (10 * s.Length) + Convert.ToInt32(s);
                string ServerFilename = "";
                if (bUsedNewName)
                    ServerFilename = DateTime.Now.ToString("yyyyMMddHHmm") + "_" + GetRand().ToString() + Path.GetExtension(Uploading.FileName);
                else
                    ServerFilename = strReturn[3];
                //Page.MapPath() 方法可以取得檔案或目錄在伺服器上的絕對路徑,ServerPathFilename 變數為照片在伺服器上的絕對路徑
                string ServerPathFilename = sPath + ServerFilename;
                //將上傳的檔案儲存到 ServerPathFilename 所指定的路徑
                byte[] FileByte = Uploading.FileBytes;
                Uploading.SaveAs(ServerPathFilename);

                //檢查檔案
                string sFileType = CheckPhoroFile(ServerPathFilename).ToString();
                if (sFileType == "JPG" || sFileType == "GIF" || sFileType == "PNG" || sFileType == "BMP")
                {
                    //以 System.Drawing.Image 類別建立 Photo 物件, 然後將原始圖讀入 Photo 物件
                    //System.Drawing.Image Photo = System.Drawing.Image.FromFile(ServerPathFilename);
                    FileStream FS = System.IO.File.OpenRead(ServerPathFilename);
                    System.Drawing.Image Photo = System.Drawing.Image.FromStream(FS);

                    strReturn[1] = Photo.Height.ToString();
                    strReturn[2] = Photo.Width.ToString();
                    Photo.Dispose();
                    FS.Close();
                    //保存
                    if (!System.IO.File.Exists(sPath + Path.ChangeExtension(ServerFilename, sFileType)))
                    {
                        System.IO.File.Copy(sPath + ServerFilename, sPath + Path.ChangeExtension(ServerFilename, sFileType));
                        if (System.IO.File.Exists(sPath + ServerFilename))
                            System.IO.File.Delete(sPath + ServerFilename);
                    }
                    strReturn[0] = Path.ChangeExtension(ServerFilename, sFileType.ToLower());
                }
                else
                {
                    if (System.IO.File.Exists(ServerPathFilename))
                        System.IO.File.Delete(ServerPathFilename);
                }
                return strReturn;
            }
            else
            {
                return strReturn;
            }
        }
        //上傳圖片(單一尺寸)
        public string[] UploadPhoto(FileUpload Uploading, string FileName, string OldPath)
        {
            string SavePhotoPath = "~/" + sPhotoFileName + "/" + FileName + "/";
            string saveFilePath = Server.MapPath(SavePhotoPath);
            CheckFileExist(saveFilePath);
            string[] ReturnStr = UploadPhoto(Uploading, saveFilePath);
            return ReturnStr;
        }
        //上傳圖片(單一尺寸)
        public string[] UploadPhoto(FileUpload Uploading, string FileName, string OldPath, int iSize)
        {
            string[] ReturnStr = new string[] { "Error", "0", "0" };
            string SavePhotoPath = "~/" + sPhotoFileName + "/" + FileName + "/";
            string SaveOldPhoto = "~/" + sPhotoFileName + "/OldPhoto/";
            string sOldFilePath = Server.MapPath(SaveOldPhoto);
            string saveFilePath = Server.MapPath(SavePhotoPath);
            CheckFileExist(sOldFilePath);
            CheckFileExist(saveFilePath);
            if (OldPath != "" && OldPath != null)
            {
                if (System.IO.File.Exists(OldPath))
                    System.IO.File.Delete(OldPath);
            }

            string[] sFileName = UpLoadSizePhoto(Uploading, sOldFilePath, saveFilePath, iSize, iSize);

            if (sFileName[0] != "Error")
            {

                ReturnStr[0] = SavePhotoPath + sFileName[0];
                ReturnStr[1] = sFileName[1];
                ReturnStr[2] = sFileName[2];

                string Path = Server.MapPath(ReturnStr[0]);

                int H = Convert.ToInt32(sFileName[1]);
                int W = Convert.ToInt32(sFileName[2]);
                if (H <= 200 || W <= 200)
                    FBSizePhoto(Path, Server.MapPath(SavePhotoPath + sFileName[0].Replace(".", "_f.")));
            }
            return ReturnStr;
        }
        //上傳圖片(3種尺寸)
        public string[] UploadPhoto(FileUpload Uploading, string FileName, string OldPath, int iBig, int iMiddle, int iSmall)
        {
            string[] ReturnStr = new string[] { "Error", "0", "0" };
            string SavePhotoPath = "~/" + sPhotoFileName + "/" + FileName + "/";
            string SaveOldPhoto = "~/" + sPhotoFileName + "/OldPhoto/";
            string sOldFilePath = Server.MapPath(SaveOldPhoto);
            string saveFilePath = Server.MapPath(SavePhotoPath);
            CheckFileExist(sOldFilePath);
            CheckFileExist(saveFilePath);
            if (OldPath != "" && OldPath != null)
            {
                if (System.IO.File.Exists(OldPath))
                    System.IO.File.Delete(OldPath);
                if (System.IO.File.Exists(OldPath.Replace(".", "_m.")))
                    System.IO.File.Delete(OldPath.Replace(".", "_m."));
                if (System.IO.File.Exists(OldPath.Replace(".", "_s.")))
                    System.IO.File.Delete(OldPath.Replace(".", "_s."));
                if (System.IO.File.Exists(OldPath.Replace(".", "_f.")))
                    System.IO.File.Delete(OldPath.Replace(".", "_f."));
            }

            string[] sFileName = UpLoadSizePhoto(Uploading, sOldFilePath, saveFilePath, iBig, iBig);

            if (sFileName[0] != "Error")
            {

                ReturnStr[0] = SavePhotoPath + sFileName[0];
                ReturnStr[1] = sFileName[1];
                ReturnStr[2] = sFileName[2];

                string Path = Server.MapPath(ReturnStr[0]);
                if (iMiddle > 0)
                    CopySizePhoto(Path, Server.MapPath(SavePhotoPath + sFileName[0].Replace(".", "_m.")), iMiddle, iMiddle);
                if (iSmall > 0)
                    CopySizePhoto(Path, Server.MapPath(SavePhotoPath + sFileName[0].Replace(".", "_s.")), iSmall, iSmall);
                int H = Convert.ToInt32(sFileName[1]);
                int W = Convert.ToInt32(sFileName[2]);
                if (H <= 200 || W <= 200)
                    FBSizePhoto(Path, Server.MapPath(SavePhotoPath + sFileName[0].Replace(".", "_f.")));
            }
            return ReturnStr;
        }
        //上傳圖片(可以區別3種尺寸的長寬)
        public string[] UploadPhoto(FileUpload Uploading, string FileName, string OldPath, int iBig_H, int iBig_W, int iMiddle_H, int iMiddle_W, int iSmall_H, int iSmall_W)
        {
            string[] ReturnStr = new string[] { "Error", "0", "0" };
            string SavePhotoPath = "~/" + sPhotoFileName + "/" + FileName + "/";
            string SaveOldPhoto = "~/" + sPhotoFileName + "/OldPhoto/";
            string sOldFilePath = Server.MapPath(SaveOldPhoto);
            string saveFilePath = Server.MapPath(SavePhotoPath);

            CheckFileExist(sOldFilePath);
            CheckFileExist(saveFilePath);
            if (OldPath != "" && OldPath != null)
            {
                if (System.IO.File.Exists(Server.MapPath(OldPath)))
                    System.IO.File.Delete(Server.MapPath(OldPath));
                if (System.IO.File.Exists(Server.MapPath(OldPath.Replace(".", "_m."))))
                    System.IO.File.Delete(Server.MapPath(OldPath.Replace(".", "_m.")));
                if (System.IO.File.Exists(Server.MapPath(OldPath.Replace(".", "_s."))))
                    System.IO.File.Delete(Server.MapPath(OldPath.Replace(".", "_s.")));
            }

            string[] sFileName = UpLoadSizePhoto(Uploading, sOldFilePath, saveFilePath, iBig_W, iBig_H);

            if (sFileName[0] != "Error")
            {
                ReturnStr[0] = SavePhotoPath + sFileName[0];
                ReturnStr[1] = sFileName[1];
                ReturnStr[2] = sFileName[2];

                string Path = Server.MapPath(ReturnStr[0]);
                if (iMiddle_H > 0)
                    CopySizePhoto(Path, Server.MapPath(SavePhotoPath + sFileName[0].Replace(".", "_m.")), iMiddle_W, iMiddle_H);
                if (iSmall_H > 0)
                    CopySizePhoto(Path, Server.MapPath(SavePhotoPath + sFileName[0].Replace(".", "_s.")), iSmall_W, iSmall_H);
            }
            return ReturnStr;
        }
        //上傳圖片(單一尺寸)
        public string[] UploadPhoto(FileUpload Uploading, string FileName, string OldPath, int iMaxHeitht, int iMaxWidth)
        {
            string[] ReturnStr = new string[] { "Error", "0", "0" };
            string SavePhotoPath = "~/" + sPhotoFileName + "/" + FileName + "/";
            string SaveOldPhoto = "~/" + sPhotoFileName + "/OldPhoto/";
            string sOldFilePath = Server.MapPath(SaveOldPhoto);
            string saveFilePath = Server.MapPath(SavePhotoPath);
            CheckFileExist(sOldFilePath);
            CheckFileExist(saveFilePath);
            if (OldPath != "" && OldPath != null)
            {
                if (System.IO.File.Exists(OldPath))
                    System.IO.File.Delete(OldPath);
            }





            return ReturnStr;
        }

        #region 切圖與縮放


        /// <summary>  
        /// 剪裁 -- 用GDI+   
        /// </summary>  
        /// <param name="b">原始Bitmap</param>  
        /// <param name="StartX">開始座標X</param>  
        /// <param name="StartY">開始座標Y</param>  
        /// <param name="iWidth">寬度</param>  
        /// <param name="iHeight">高度</param>  
        /// <returns>剪裁後的Bitmap</returns>  
        public Bitmap CutImage(Bitmap b, int StartX, int StartY, int iWidth, int iHeight)
        {
            if (b == null)
            {
                return null;
            }
            int w = b.Width;
            int h = b.Height;
            if (StartX >= w || StartY >= h)
            {
                return null;
            }
            if (StartX + iWidth > w)
            {
                iWidth = w - StartX;
            }
            if (StartY + iHeight > h)
            {
                iHeight = h - StartY;
            }
            try
            {
                Bitmap bmpOut = new Bitmap(iWidth, iHeight, PixelFormat.Format24bppRgb);
                Graphics g = Graphics.FromImage(bmpOut);
                g.DrawImage(b, new Rectangle(0, 0, iWidth, iHeight), new Rectangle(StartX, StartY, iWidth, iHeight), GraphicsUnit.Pixel);
                g.Dispose();
                g = null;
                return bmpOut;
            }
            catch
            {
                return null;
            }
        }
        #endregion

        //取得大圖路徑
        public string GetBigPhoto(string Input)
        {
            if (Input.Replace(" ", "") != "")
                return Input.Replace(".", "_f.").Replace("_f._f.", "..").Replace("~/", "../");
            else return sNoPhoto;
        }
        //取得中圖路徑
        public string GetMiddlePhoto(string Input)
        {
            if (Input.Replace(" ", "") != "")
                return Input.Replace(".", "_m.").Replace("_m._m.", "..").Replace("~/", "../");
            else return sNoPhoto;
        }
        //取得小圖路徑
        public string GetSmallPhoto(string Input)
        {
            if (Input.Replace(" ", "") != "")
                return Input.Replace(".", "_s.").Replace("_s._s.", "..").Replace("~/", "../");
            else
                return sNoPhoto;
        }

        //直接取得圖片的size
        public int[] GetPhotoFileSize(string FilePath)
        {
            int[] Size = new int[] { 0, 0 };
            FileStream FS = System.IO.File.OpenRead(FilePath);
            System.Drawing.Image Photo = System.Drawing.Image.FromStream(FS);

            Size[0] = Photo.Height;
            Size[1] = Photo.Width;
            Photo.Dispose();
            FS.Close();

            return Size;
        }
        //圖片縮放後轉存
        public void CopySizePhoto(string sPath, string sNewPath, int iWidth, int iHeight)
        {
            // 縮圖運算
            FileStream FS = System.IO.File.OpenRead(sPath);
            System.Drawing.Image Photo = System.Drawing.Image.FromStream(FS);
            int ThumbWidth = iWidth, ThumbHeight = iHeight;
            int[] Size = SetImgSize(Photo.Height, Photo.Width, iHeight, iWidth);
            ThumbWidth = Size[1];
            ThumbHeight = Size[0];

            System.Drawing.Image newImage = new System.Drawing.Bitmap((int)ThumbWidth, (int)ThumbHeight);

            System.Drawing.Graphics newG = System.Drawing.Graphics.FromImage(newImage);
            newG.DrawImage(Photo, new Rectangle(0, 0, ThumbWidth, ThumbHeight), new Rectangle(0, 0, Photo.Width, Photo.Height), GraphicsUnit.Pixel);
            newG.Dispose();
            Photo.Dispose();
            FS.Close();

            if (System.IO.File.Exists(sNewPath))
                System.IO.File.Delete(sNewPath);
            newImage.Save(sNewPath);
            newImage.Dispose();


        }
        //圖片放大到FB尺寸後轉存
        public void FBSizePhoto(string sPath, string sNewPath)
        {
            int iHeight = 200;
            int iWidth = 200;
            #region 縮圖運算
            FileStream FS = System.IO.File.OpenRead(sPath);
            System.Drawing.Image Photo = System.Drawing.Image.FromStream(FS);
            //System.Drawing.Image Photo = System.Drawing.Image.FromFile(sPath);
            int ThumbWidth = iWidth, ThumbHeight = iHeight;
            int[] Size = OutImgSize(Photo.Height, Photo.Width, iHeight, iWidth);
            ThumbWidth = Size[1];
            ThumbHeight = Size[0];

            #endregion
            #region 畫質調整
            System.Drawing.Image newImage = new System.Drawing.Bitmap((int)ThumbWidth, (int)ThumbHeight);
            /*if (bHighType)
            {
                //生成新图=新建一个bmp图片

                //新建一个画板
                System.Drawing.Graphics newG = System.Drawing.Graphics.FromImage(newImage);
                //设置质量
                newG.InterpolationMode = InterpolationMode.HighQualityBicubic;
                newG.SmoothingMode = SmoothingMode.HighQuality;
                //画图
                newG.DrawImage(Photo, new Rectangle(0, 0, ThumbWidth, ThumbHeight), new Rectangle(0, 0, Photo.Width, Photo.Height), GraphicsUnit.Pixel);
                newG.Dispose();
            }
            else
            {*/
            System.Drawing.Graphics newG = System.Drawing.Graphics.FromImage(newImage);
            newG.DrawImage(Photo, new Rectangle(0, 0, ThumbWidth, ThumbHeight), new Rectangle(0, 0, Photo.Width, Photo.Height), GraphicsUnit.Pixel);
            newG.Dispose();
            //}
            #endregion
            //保存
            try
            {
                if (System.IO.File.Exists(sNewPath))
                    System.IO.File.Delete(sNewPath);
                newImage.Save(sNewPath);
            }
            catch
            {
                if (System.IO.File.Exists(sNewPath))
                    System.IO.File.Delete(sNewPath);
                System.IO.File.Copy(sPath, sNewPath);
            }
            newImage.Dispose();
            Photo.Dispose();
            FS.Close();
        }
        //上傳檔案
        public string UpLoadFile(FileUpload Uploading, string sPath, string sOldName)
        {
            if (Uploading.HasFile == true)
            {
                string ServerFilename = "";
                if (sOldName == null || sOldName == "")
                {
                    string s = GetRand().ToString();
                    //int Top = (DateTime.Now.Year - 2000) * 10000 + (DateTime.Now.Month) * 100 + (DateTime.Now.Day);
                    //int Rentdoms = Top * (10 * s.Length) + Convert.ToInt32(s);
                    string Rentdoms = DT.ToString("yyyyMMddHHmm") + "_" + s;

                    //ServerFilename = Rentdoms + Path.GetExtension(Uploading.FileName);
                    ServerFilename = bUsedNewName ? Rentdoms + Path.GetExtension(Uploading.FileName) : Uploading.FileName;
                }
                else
                    ServerFilename = sOldName;

                Uploading.SaveAs(sPath + "/" + ServerFilename);
                return ServerFilename;
            }
            else
            {
                return "Error";
            }
        }
        //移除檔案
        public void RemoveFile(string FilePath)
        {
            bool bCheck = true;
            do
            {
                try
                {
                    if (System.IO.File.Exists(FilePath))
                        System.IO.File.Delete(FilePath);
                    bCheck = false;
                }
                catch
                {
                }
            }
            while (bCheck);
        }
        #region 縮圖處理
        #region 依尺寸縮圖
        //計算縮放圖尺寸 高/寬
        //將圖縮在尺寸內
        public int[] SetImgSize(int OldSize_H, int OldSize_W, int InSize_H, int InSize_W)
        {
            int[] iSize = new Int32[2] { 0, 0 };//High/Width
            double dImgH = InSize_H, dImgW = InSize_W;
            if (InSize_H > OldSize_H && InSize_W > OldSize_W)//若原圖比設定框小,不變形
            {
                iSize[0] = OldSize_H;
                iSize[1] = OldSize_W;
            }
            else
            {
                try
                {
                    double dPhotoH = Convert.ToDouble(OldSize_H);
                    double dPhotoW = Convert.ToDouble(OldSize_W);
                    double dPhotoScale = dPhotoH / dPhotoW;
                    double dImgScale = dImgH / dImgW;
                    if (OldSize_H * 2.5 <= dPhotoH && OldSize_W * 2.5 <= dPhotoW)
                    {
                        iSize[0] = Convert.ToInt32(OldSize_H * 2.5);
                        iSize[1] = Convert.ToInt32(OldSize_W * 2.5);
                    }
                    else if (dImgScale > 1)
                    {
                        if (dPhotoH > dImgH)
                        {
                            if (dPhotoW > dImgW)
                            {
                                if (dPhotoScale >= dImgScale)
                                {
                                    iSize[0] = Convert.ToInt32(dImgH);
                                    iSize[1] = Convert.ToInt32(dPhotoW * (dImgH / dPhotoH));
                                }
                                else
                                {
                                    iSize[0] = Convert.ToInt32(dPhotoH * (dImgW / dPhotoW));
                                    iSize[1] = Convert.ToInt32(dImgW);
                                }
                            }
                            else//(dPhotoW <= dImgW)
                            {
                                iSize[0] = Convert.ToInt32(dImgH);
                                iSize[1] = Convert.ToInt32(dPhotoW * (dImgH / dPhotoH));
                            }
                        }
                        else if (dPhotoH == dImgH)
                        {
                            if (dPhotoW > dImgW)
                            {
                                iSize[0] = Convert.ToInt32(dPhotoH * (dImgW / dPhotoW));
                                iSize[1] = Convert.ToInt32(dImgW);
                            }
                            else if (dPhotoW == dImgW)
                            {
                                iSize[0] = Convert.ToInt32(dImgH);
                                iSize[1] = Convert.ToInt32(dImgW);
                            }
                            else//(dPhotoW < dImgW)
                            {
                                iSize[0] = Convert.ToInt32(dImgH);
                                iSize[1] = Convert.ToInt32(dPhotoW * (dImgH / dPhotoH));
                            }
                        }
                        else//(dPhotoH < dImgH)
                        {
                            if (dPhotoW > dImgW)
                            {
                                iSize[0] = Convert.ToInt32(dPhotoH * (dImgW / dPhotoW));
                                iSize[1] = Convert.ToInt32(dImgW);
                            }
                            else if (dPhotoW == dImgW)
                            {
                                iSize[0] = Convert.ToInt32(dPhotoH);
                                iSize[1] = Convert.ToInt32(dPhotoW);
                            }
                            else//(dPhotoW < dImgW)
                            {
                                if (dPhotoScale > dImgScale)
                                {
                                    iSize[0] = Convert.ToInt32(dImgH);
                                    iSize[1] = Convert.ToInt32(dPhotoW * (dImgH / dPhotoH));
                                }
                                else
                                {
                                    iSize[0] = Convert.ToInt32(dPhotoH * (dImgW / dPhotoW));
                                    iSize[1] = Convert.ToInt32(dImgW);
                                }
                            }
                        }
                    }
                    else//(dImgScale <= 1)
                    {
                        if (dPhotoH > dImgH)
                        {
                            if (dPhotoW > dImgW)
                            {
                                if (dPhotoScale >= dImgScale)
                                {
                                    iSize[0] = Convert.ToInt32(dImgH);
                                    iSize[1] = Convert.ToInt32(dPhotoW * (dImgH / dPhotoH));
                                }
                                else
                                {
                                    iSize[0] = Convert.ToInt32(dPhotoH * (dImgW / dPhotoW));
                                    iSize[1] = Convert.ToInt32(dImgW);
                                }
                            }
                            else//(dPhotoW <= dImgW)
                            {
                                iSize[0] = Convert.ToInt32(dImgH);
                                iSize[1] = Convert.ToInt32(dPhotoW * (dImgH / dPhotoH));
                            }
                        }
                        else//(dPhotoH <= dImgH)
                        {
                            if (dPhotoW >= dImgW)
                            {
                                iSize[0] = Convert.ToInt32(dPhotoH * (dImgW / dPhotoW));
                                iSize[1] = Convert.ToInt32(dImgW);
                            }
                            else//(dPhotoW < dImgW)
                            {
                                if (dPhotoScale >= dImgScale)
                                {
                                    iSize[0] = Convert.ToInt32(dImgH);
                                    iSize[1] = Convert.ToInt32(dPhotoW * (dImgH / dPhotoH));
                                }
                                else
                                {
                                    iSize[0] = Convert.ToInt32(dPhotoH * (dImgW / dPhotoW));
                                    iSize[1] = Convert.ToInt32(dImgW);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    iSize[0] = Convert.ToInt32(dImgH);
                    iSize[1] = Convert.ToInt32(dImgW);
                }
            }
            return iSize;
        }
        //將圖限制等比例放到佔滿目標框,長/寬多的需CSS裁掉
        private int[] OutImgSize(int Input_H, int Input_W, int Box_H, int Box_W)
        {
            int[] Size = new int[2];
            Size[1] = Box_W;
            Size[0] = (int)(Input_H * ((double)Box_W / (double)Input_W));

            if (Size[0] < Box_H)
            {
                Size[0] = Box_H;
                Size[1] = (int)(Input_W * ((double)Box_H / (double)Input_H));
            }

            return Size;
        }
        //將圖限制等比例放到佔滿目標框,長多的需CSS裁掉
        private int[] OutImgSize_W(int Input_H, int Input_W, int Box_H, int Box_W)
        {
            int[] Size = new int[2];
            Size[1] = Box_W;
            Size[0] = (int)(Input_H * ((double)Box_W / (double)Input_W));
            return Size;
        }
        //將圖限制等比例放到佔滿目標框,寬多的需CSS裁掉
        private int[] OutImgSize_H(int Input_H, int Input_W, int Box_H, int Box_W)
        {
            int[] Size = new int[2];
            Size[0] = Box_H;
            Size[1] = (int)(Input_W * ((double)Box_H / (double)Input_H));
            return Size;
        }
        #endregion
        #region 依大小縮圖
        //容量調整
        public void ChangePhotoSize(int MaxSize, string OldPath, string NewPath)
        {
            FileInfo fInfo = new FileInfo(OldPath);
            if ((fInfo.Length / 1024) <= MaxSize)
            { }
            else
            {
                for (int i = 100; i >= 10; i -= 10)
                {
                    if (System.IO.File.Exists(NewPath))
                        System.IO.File.Delete(NewPath);
                    Bitmap source = new Bitmap(OldPath);
                    long[] quality = new long[1];
                    quality[0] = i;

                    EncoderParameters Parameters = new EncoderParameters();
                    EncoderParameter Parameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality);
                    Parameters.Param[0] = Parameter;
                    source.Save(NewPath, ImageCodecInfo.GetImageEncoders()[1], Parameters);
                    source.Dispose();
                    Parameters.Dispose();
                    Parameter.Dispose();

                    fInfo = new FileInfo(NewPath);
                    if ((fInfo.Length / 1024) <= MaxSize)
                    {
                        if (System.IO.File.Exists(OldPath))
                            System.IO.File.Delete(OldPath);
                        System.IO.File.Copy(NewPath, OldPath);
                        if (System.IO.File.Exists(NewPath))
                            System.IO.File.Delete(NewPath);

                        break;
                    }
                    else if (i <= 10 && (fInfo.Length / 1024) > MaxSize)
                    {

                        if (System.IO.File.Exists(OldPath))
                            System.IO.File.Delete(OldPath);
                        System.IO.File.Copy(NewPath, OldPath);
                        if (System.IO.File.Exists(NewPath))
                            System.IO.File.Delete(NewPath);

                    }
                    else { }
                }
            }
        }
        //容量調整後順便縮圖
        public void ChangePhotoSize(int MaxSize, string OldPath, string NewPath, int Height, int Width)
        {
            FileInfo fInfo = new FileInfo(OldPath);
            if ((fInfo.Length / 1024) <= MaxSize)
            { }
            else
            {
                for (int i = 100; i >= 10; i -= 10)
                {
                    RemoveFile(NewPath);
                    Bitmap source = new Bitmap(OldPath);
                    long[] quality = new long[1];
                    quality[0] = i;
                    using (EncoderParameters Parameters = new EncoderParameters())
                    {
                        using (EncoderParameter Parameter = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, quality))
                        {
                            Parameters.Param[0] = Parameter;
                            source.Save(NewPath, ImageCodecInfo.GetImageEncoders()[1], Parameters);
                            source.Dispose();
                        }
                    }
                    fInfo = new FileInfo(NewPath);
                    if ((fInfo.Length / 1024) <= MaxSize)
                    {
                        RemoveFile(OldPath);
                        CopySizePhoto(NewPath, OldPath, Width, Height);
                        RemoveFile(NewPath);
                        System.IO.File.Copy(OldPath, NewPath);
                        break;
                    }
                    else if (i == 10 && (fInfo.Length / 1024) > MaxSize)
                    {
                        RemoveFile(OldPath);
                        CopySizePhoto(NewPath, OldPath, Width, Height);
                        RemoveFile(NewPath);
                        System.IO.File.Copy(OldPath, NewPath);
                        ChangePhotoSize(MaxSize, OldPath, NewPath, Height, Width);
                    }
                    else { }
                }
            }
        }
        #endregion
        #endregion
        #endregion
        #region Excel控制
        //塞Excel用的物件
        public class ExcelCell
        {
            public string sValue = "";//內容
            public string sToolTip = "";//說明視窗
            public bool bColor = false;//是否套用顏色屬性
            public System.Drawing.Color cBagColor = System.Drawing.Color.White;//背景色
            public System.Drawing.Color cFontColor = System.Drawing.Color.Black;//文字顏色
        }
        //將string[]轉成ExcelCell[]
        public void WriteExcelFromString(string Title, ArrayList AL)
        {
            ArrayList AL2 = new ArrayList();
            bool bTop = true;
            foreach (string[] str in AL)
            {
                ExcelCell[] EC = new ExcelCell[str.Length];
                for (int i = 0; i < str.Length; i++)
                {
                    EC[i] = new ExcelCell();
                    EC[i].sValue = str[i];
                    if (bTop)//標題改顏色
                    {
                        EC[i].bColor = true;
                        EC[i].cBagColor = HexColor("#d5dbee");
                    }
                }
                bTop = false;
                AL2.Add(EC);
            }
            WriteExcel(Title, AL2);
        }

        public void WriteCSVFromString(string Title, ArrayList AL, Encoding Code)
        {
            ArrayList AL2 = new ArrayList();
            foreach (string[] str in AL)
            {
                ExcelCell[] EC = new ExcelCell[str.Length];
                for (int i = 0; i < str.Length; i++)
                {
                    EC[i] = new ExcelCell();
                    EC[i].sValue = str[i];
                }
                AL2.Add(EC);
            }
            WriteCSV(Title, AL2, Code);
        }

        public void WriteExcel(string sTitle, ArrayList AL)
        {
            // 設定儲存檔名，不用設定副檔名，系統自動判斷 excel 版本，產生 .xls 或 .xlsx 副檔名
            //string pathFile = @"D:\test";
            string FileName = sTitle + "-" + DateTime.Now.ToString("yyyyMMddHHmmss") + ".xlsx";
            string FilePath = "/Files/Down/";
            CheckFileExist(Server.MapPath(FilePath));
            string pathFile = Server.MapPath(FilePath + FileName);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            OfficeOpenXml.ExcelPackage p = new OfficeOpenXml.ExcelPackage();
            OfficeOpenXml.ExcelWorksheet sheet = p.Workbook.Worksheets.Add(sTitle);
            for (int i = 0; i < AL.Count; i++)
            {

                ExcelCell[] ECs = (ExcelCell[])AL[i];

                for (int j = 0; j < ECs.Length; j++)
                {
                    var cell = sheet.Cells[i + 1, j + 1, i + 1, j + 1];
                    if (ECs[j].bColor)
                    {
                        cell.Style.Fill.PatternType = OfficeOpenXml.Style.ExcelFillStyle.Solid;
                        cell.Style.Fill.BackgroundColor.SetColor(ECs[j].cBagColor);
                        cell.Style.Font.Color.SetColor(ECs[j].cFontColor);
                    }
                    cell.Value = ECs[j].sValue;
                    if (ECs[j].sToolTip != "")
                        cell.AddComment(ECs[j].sToolTip, "");
                }
            }
            Byte[] bin = p.GetAsByteArray();
            System.IO.File.WriteAllBytes(pathFile, bin);
            string URL = Request.Url.Scheme + "://" + Request.Url.Host + ":" + Request.Url.Port + "/" + FilePath.Replace("../", "");
            //Response.Write(string.Format("<script>window.open('{0}', '" + FileName + "','');</script>", URL + FileName));//下載

            Response.ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            Response.AppendHeader("Content-Disposition", "Attachment; FileName=" + FileName);
            Response.TransmitFile(FilePath + FileName);
            Response.End();

        }
        //匯出CSV的方法
        public void WriteCSV(string sTitle, ArrayList AL, Encoding Code)
        {
            string FilePath = Server.MapPath("/Files/Down/");
            CheckFileExist(FilePath);
            string FileName = DateTime.Now.ToString("yyyyMMddHHmmss") + sTitle + ".csv";
            FileStream myFileStream = new FileStream(FilePath + FileName, FileMode.OpenOrCreate, FileAccess.Write);
            StreamWriter myStreamWriter = new StreamWriter(myFileStream, Code);//Encoding.Default  Encoding.GetEncoding("big5")
            for (int i = 0; i < AL.Count; i++)
            {
                ExcelCell[] ECs = (ExcelCell[])AL[i];
                string str = "";
                for (int j = 0; j < ECs.Length; j++)
                {
                    str += (j == 0 ? "" : ",") + ECs[j].sValue.ToString();
                }
                str += '\n';
                myStreamWriter.Write(str);
            }
            myStreamWriter.Close();
            myFileStream.Close();

            Response.ContentType = "application/vnd.ms-excel";
            Response.AppendHeader("Content-Disposition", "Attachment; FileName=" + FileName);
            Response.TransmitFile(FilePath + FileName);
            Response.End();
        }
        //讀取檔案,路徑需用相對路徑
        public ArrayList ReadExcel(string FilePath)
        {
            string pathFile = Server.MapPath(FilePath);
            FileStream fs = new FileStream(pathFile, FileMode.Open, FileAccess.Read);
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            OfficeOpenXml.ExcelPackage p = new OfficeOpenXml.ExcelPackage(fs);
            OfficeOpenXml.ExcelWorksheet sheet = p.Workbook.Worksheets[0];
            ArrayList AL = new ArrayList();

            int i = 1, j = 0, CellCt = 0;
            string[] Values = new string[0];
            do
            {
                j = 1;
                string s = sheet.Cells[i, j].Text;
                if (s == null || (s != null ? s == string.Empty : false))
                    break;
                else
                {
                    if (CellCt > 0)
                        Values = new string[CellCt];
                    do
                    {
                        s = sheet.Cells[i, j].Text;
                        if (i == 1)
                        {
                            CellCt++;
                        }
                        else
                        {
                            if (j - 1 < Values.Length)
                                Values[j - 1] = s;
                        }
                        j++;
                        if (s == null || (s != null ? s == string.Empty : false))
                        {
                            if (i == 1)
                                CellCt--;
                            break;
                        }

                    } while (true);
                }
                if (i > 1)
                    AL.Add(Values);
                i++;

            } while (true);
            fs.Close();
            return AL;
        }
        #endregion
        #region MVC工具
        public void GetViewBag()
        {
            ViewBag._Title = "旌旗教會";
            ViewBag._KeyWord = "";
            ViewBag._Description = "";
            ViewBag._UserName = "未知使用者";
            ViewBag._Logo = "";
            ViewBag._SysMsg = "";//用於系統後台系統提示訊息
            ViewBag._UserID = "0";
            ViewBag._Power = bGroup;
            ViewBag._Login = "";
            ViewBag._GroupTitle = "小組資訊";


            string NowURL = Request.Url.AbsolutePath;
            ACID = GetACID();
            var AC = DC.Account.FirstOrDefault(q => q.ACID == ACID);
            if (AC != null)
            {
                ViewBag._UserName = AC.Name_First + AC.Name_Last;
                ViewBag._UserID = ACID;
                ViewBag._Login = AC.Login;
                
                #region 上層
                string GroupMapTitle = "";
                var OIs = DC.OrganizeInfo.Where(q => q.ACID == AC.ACID && q.OID == 8 && q.ActiveFlag && !q.DeleteFlag);
                string sOIID = GetBrowserData("TargetGroupID");
                if (sOIID != "")
                    OIs = OIs.Where(q => q.OIID.ToString() == sOIID);
                var OI = OIs.OrderByDescending(q => q.OIID).FirstOrDefault();
                while (OI != null)
                {
                    if (OI.OID == 8)
                        GroupMapTitle = "<span class='font-bold'>" + OI.Title + OI.Organize.Title + (OI.BusinessType == 1 ? "(外展)" : "") + "</span>";
                    else
                        GroupMapTitle = OI.Title + OI.Organize.Title + (GroupMapTitle == "" ? "" : "/" + GroupMapTitle);
                    OI = DC.OrganizeInfo.FirstOrDefault(q => q.OIID == OI.ParentID && q.ActiveFlag && !q.DeleteFlag && q.OID > 1);
                }
                ViewBag._GroupTitle = GroupMapTitle;
                #endregion
            }
            else if (!NowURL.ToLower().Contains("/home/login") || !NowURL.ToLower().Contains("/home/logout"))
                SetAlert("請先登入", 2, (NowURL.ToLower().StartsWith("/web/") ? "/Web/Home/Login" : "/Admin/Home/Login"));
            ViewBag._OIID = 0;

            ViewBag._CSS1 = "";
            ViewBag._URL = NowURL;
            string ShortURL = Request.RawUrl.Split('?')[0];
            if (ShortURL.ToLower().Contains("/admin/"))
            {
                ViewBag._CSS1 = "/Areas/Admin/Content/CSS/" + (ShortURL.Contains("_List") ? "list.css" : "form.css");
                string[] ShortURLs = ShortURL
                    .Replace("_Edit_1", "_List?CID=1")
                    .Replace("_Edit_2", "_List?CID=2")
                    .Replace("_Edit_3", "_List?CID=3")
                    .Replace("_Edit", "_List").Split('/');
                string NewShortURL = "";
                for (int i = 0; i < ShortURLs.Length; i++)
                {
                    if (i < 4)
                        NewShortURL += ShortURLs[i] + "/";
                }
                if (NewShortURL.EndsWith("/"))
                    NewShortURL = NewShortURL.Substring(0, NewShortURL.Length - 1);

                var M = DC.Menu.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && (q.URL == ShortURL || q.URL.StartsWith(NewShortURL)));
                if (M != null)
                {
                    ViewBag._Title = M.Title;
                    //取得權限
                    if (CheckAdmin(ACID))//此使用者擁有系統管理者權限
                        bGroup = new bool[] { true, true, true, true, true, true };
                    else
                    {
                        /*var MACs = from q in DC.M_Rool_Account.Where(q => q.ACID == ACID && q.ActiveFlag && (q.Rool.RoolType == 3 || q.Rool.RoolType == 4) && !q.DeleteFlag && (q.JoinDate == q.CreDate || q.JoinDate.Date <= DT.Date) && (q.LeaveDate == q.CreDate || q.LeaveDate.Date >= DT.Date))
                                   join p in DC.M_Rool_Menu.Where(q => q.MID == M.MID)
                                   on q.RID equals p.RID
                                   select p;*/
                        var MACs = from q in GetMRAC(0, ACID).Where(q => q.Rool.RoolType == 3 || q.Rool.RoolType == 4)
                                   join p in DC.M_Rool_Menu.Where(q => q.MID == M.MID)
                                   on q.RID equals p.RID
                                   select p;
                        foreach (var MAC in MACs)
                        {
                            if (!bGroup[0] && MAC.ShowFlag)
                                bGroup[0] = MAC.ShowFlag;
                            if (!bGroup[1] && MAC.AddFlag)
                                bGroup[1] = MAC.AddFlag;
                            if (!bGroup[2] && MAC.EditFlag)
                                bGroup[2] = MAC.EditFlag;
                            if (!bGroup[3] && MAC.DeleteFlag)
                                bGroup[3] = MAC.DeleteFlag;
                            if (!bGroup[4] && MAC.PrintFlag)
                                bGroup[4] = MAC.PrintFlag;
                            if (!bGroup[5] && MAC.UploadFlag)
                                bGroup[5] = MAC.UploadFlag;
                        }
                    }

                    ViewBag._Power = bGroup;
                }
                else if (CheckAdmin(ACID))//此使用者擁有系統管理者權限
                    bGroup = new bool[] { true, true, true, true, true, true };
            }

            //SetResponseVerify();

            TempData["UID"] = ACID;
            TempData["msg"] = "";
            TempData["msgtype"] = "";
            TempData["url"] = "";
            TempData["_url"] = NowURL;
            TempData["OID"] = "0";//組織層級ID
            TempData["OITitle"] = "";//組織搜尋用關鍵字

            //CheckVCookie();
        }
        /*private void SetResponseVerify()
        {
            var res = Response.Cookies["__RequestVerificationToken"];
            var req = Request.Cookies["__RequestVerificationToken"];
            if (res == null || string.IsNullOrEmpty(res.Value))//set Response
            {
                if (req != null && !string.IsNullOrEmpty(req.Value))
                {
                    res = new HttpCookie("__RequestVerificationToken", req.Value);
                    Response.SetCookie(res);
                }
            }
        }*/
        /// <summary>
        /// 修正 需要的反仿冒 Cookie "__RequestVerificationToken" 不存在。 錯誤
        /// </summary>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public string CheckVCookie()
        {
            try
            {
                const string __RequestVerificationToken = "__RequestVerificationToken";
                HttpCookie cookie = Request.Cookies[__RequestVerificationToken];

                // check for null
                if (cookie == null)
                {
                    // no cookie found, create it... or respond with an error
                    throw new NotImplementedException();
                }

                // grab the cookie
                AntiForgery.GetTokens(
                    Request.Form[__RequestVerificationToken],
                    out string cookieToken,
                    out string formToken);

                if (!String.IsNullOrEmpty(cookieToken))
                {
                    // update the cookie value(s)
                    cookie.Values[__RequestVerificationToken] = cookieToken;
                    //...
                }

                // update the expiration timestamp
                cookie.Expires = DateTime.UtcNow.AddDays(30);

                // overwrite the cookie
                Response.Cookies.Add(cookie);

                // return the NEW token!
                return cookieToken;
            }
            catch { return ""; }
        }
        public void ChangeTitle(bool AddFlag)
        {
            ViewBag._Title += " " + (AddFlag ? "新增" : "編輯");
        }
        /// <summary>
        /// 設定錯誤訊息彈跳視窗
        /// </summary>
        /// <param name="Msg">錯誤訊息內容</param>
        /// <param name="URL">跳轉網址</param>
        /// <param name="Type">1=成功/2=失敗/3=注意/4=訊息/5=問題</param>
        public void SetAlert(string Msg, int Type = 0, string URL = "")
        {
            //success=成功=1
            //error=失敗=2
            //warning=注意=3
            //info=訊息=4
            //question=問題=5
            string[] sMsgType = new string[] { "", "success", "error", "warning", "info", "question" };
            if (Msg != "")
                TempData["msg"] = Msg.Replace("\\n", "<br/>").Replace("\n", "<br/>");
            if (URL != "")
                TempData["url"] = URL;
            if (Type > 0)
                TempData["msgtype"] = sMsgType[Type];
        }

        public cTableRow SetTableRowTitle(List<cTableCell> Cs)
        {
            cTableRow cTR = new cTableRow();
            cTR.CSS = "Bgn";
            cTR.ID = 0;
            cTR.SortNo = 0;
            cTR.UpdDate = DT;
            for (int i = 0; i < Cs.Count; i++)
                Cs[i].SortNo = i;
            cTR.Cs = Cs;
            return cTR;
        }
        public cTableRow SetTableCellSortNo(cTableRow cTR)
        {
            for (int i = 0; i < cTR.Cs.Count; i++)
                cTR.Cs[i].SortNo = i;
            return cTR;
        }
        /// <summary>
        /// 檢查View回來的checkbox的值
        /// </summary>
        /// <param name="Input"></param>
        /// <returns></returns>
        public bool GetViewCheckBox(string Input)
        {
            if (Input == null)
                return false;
            else if (Input.Contains(','))
            {
                string[] str = Input.Split(',');
                if (str[0].ToLower().Contains("true"))
                    return true;
                else return false;
            }
            else
            {
                bool bCheck = false;
                if (!bool.TryParse(Input, out bCheck))
                    bCheck = Input == "on";

                return bCheck;
            }

        }

        #endregion
        #region 取得檢查碼

        public void CreateCheckCodeImage(string checkCode)
        {
            if (checkCode == null || checkCode.Trim() == String.Empty)
                return;

            System.Drawing.Bitmap image = new System.Drawing.Bitmap((int)Math.Ceiling((checkCode.Length * 12.5)), 25);
            System.Drawing.Graphics g = Graphics.FromImage(image);

            try
            {
                Random random = new Random();
                g.Clear(Color.White);
                for (int i = 0; i < 25; i++)
                {
                    int x1 = random.Next(image.Width);
                    int x2 = random.Next(image.Width);
                    int y1 = random.Next(image.Height);
                    int y2 = random.Next(image.Height);

                    g.DrawLine(new Pen(Color.Silver), x1, y1, x2, y2);
                }

                Font font = new System.Drawing.Font("Arial", 15, (System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic));
                System.Drawing.Drawing2D.LinearGradientBrush brush = new System.Drawing.Drawing2D.LinearGradientBrush(new Rectangle(0, 0, image.Width, image.Height), Color.Green, Color.DarkGreen, 1.2f, true);
                g.DrawString(checkCode, font, brush, 2, 2);
                for (int i = 0; i < 100; i++)
                {
                    int x = random.Next(image.Width);
                    int y = random.Next(image.Height);

                    image.SetPixel(x, y, Color.FromArgb(random.Next()));
                }
                g.DrawRectangle(new Pen(Color.Silver), 0, 0, image.Width - 1, image.Height - 1);

                System.IO.MemoryStream ms = new System.IO.MemoryStream();
                image.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
                Response.ClearContent();
                Response.ContentType = "image/Gif";
                Response.BinaryWrite(ms.ToArray());
            }
            finally
            {
                g.Dispose();
                image.Dispose();
            }
        }
        #endregion


        #region 旌旗
        //取得某組織與旗下所有組織
        public List<OrganizeInfo> GetThisOIsFromTree(ref List<OrganizeInfo> OIs, int PID)
        {
            return GetThisOIsFromTree(PID);
        }
        public List<OrganizeInfo> GetThisOIsFromTree(int PID)
        {
            List<OrganizeInfo> OIs = new List<OrganizeInfo>();
            var OIs_ = DC.OrganizeInfo.Where(q => !q.DeleteFlag && q.ActiveFlag).ToList();
            var OIs__ = OIs_.Where(q => q.ParentID == PID).ToList();
            while (OIs__.Count > 0)
            {
                OIs.AddRange(OIs__);
                OIs__ = (from q in OIs__
                         join p in OIs_
                         on q.OIID equals p.ParentID
                         select p).ToList();
            };

            return OIs;
        }


        //取得全部組織組織下拉選單
        public List<ListSelect> GetO(int OIID = 0)
        {
            List<ListSelect> LSs = new List<ListSelect>();
            var Os = DC.Organize.Where(q => q.ActiveFlag && !q.DeleteFlag && q.ItemID == "Shepherding").ToList();
            var OIs = DC.OrganizeInfo.Where(q => q.ActiveFlag && !q.DeleteFlag).ToList();
            int[] OIIDs = new int[10];
            if (OIID > 0)
            {
                var OI_ = OIs.FirstOrDefault(q => q.OIID == OIID && !q.DeleteFlag && q.ActiveFlag);
                if (OI_ != null)
                {
                    for (int i = OI_.OID; i >= 0; i--)
                    {
                        OIIDs[i] = OI_.OIID;
                        OI_ = OIs.FirstOrDefault(q => q.OIID == OI_.ParentID && !q.DeleteFlag && q.ActiveFlag);
                        if (OI_ == null)
                            break;
                    }
                }
            }


            int PID = 0;
            int SortNo = 0;
            do
            {
                var O = Os.FirstOrDefault(q => q.ParentID == PID);
                if (O == null)
                    break;
                else
                {
                    PID = O.OID;
                    ListSelect LS = new ListSelect();
                    LS.Title = O.Title;
                    LS.ControlName = "ddl_" + SortNo;
                    LS.SortNo = SortNo;
                    LS.ddlList = new List<SelectListItem>();
                    if (OIID == 0)
                    {
                        if (PID == 1)
                            LS.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "-1", Selected = true });
                        if (PID < 2)
                            LS.ddlList.AddRange((from q in OIs.Where(q => q.OID == O.OID).OrderBy(q => q.OIID)
                                                 select new SelectListItem { Text = q.Title + q.Organize.Title, Value = q.OIID.ToString() }).ToList());
                    }
                    else
                    {
                        LS.ddlList.Add(new SelectListItem { Text = "請選擇", Value = "-1" });
                        LS.ddlList.AddRange((from q in OIs.Where(q => q.OID == O.OID && q.ParentID == OIIDs[O.OID - 1]).OrderBy(q => q.OIID)
                                             select new SelectListItem { Text = q.Title + q.Organize.Title, Value = q.OIID.ToString(), Selected = q.OIID == OIIDs[q.OID] }).ToList());
                    }
                    LSs.Add(LS);

                    SortNo++;
                }
            } while (true);
            return LSs;

        }
        //取得會員或組織的對應名單
        public IQueryable<M_OI_Account> GetMOIAC(int OID = 0, int OIID = 0, int ACID = 0)
        {
            var MAs = DC.M_OI_Account.Where(q => !q.DeleteFlag &&
            q.ActiveFlag &&
            !q.OrganizeInfo.DeleteFlag &&
            !q.OrganizeInfo.Organize.DeleteFlag &&
            !q.Account.DeleteFlag &&
            (q.JoinDate == q.CreDate || q.JoinDate.Date <= DT.Date) &&
            (q.LeaveDate == q.CreDate || q.LeaveDate.Date >= DT.Date));
            if (OID > 0)
                MAs = MAs.Where(q => q.OrganizeInfo.OID == OID);
            if (OIID > 0)
                MAs = MAs.Where(q => q.OIID == OIID);
            if (ACID > 0)
                MAs = MAs.Where(q => q.ACID == ACID);
            return MAs;
        }
        //取得會員是哪個旌旗
        public IQueryable<M_OI2_Account> GetMOI2AC(int OIID = 0, int ACID = 0)
        {
            var MAs = DC.M_OI2_Account.Where(q =>
            !q.DeleteFlag &&
            q.ActiveFlag &&
            q.OrganizeInfo.ActiveFlag &&
            !q.OrganizeInfo.DeleteFlag
            );
            if (OIID > 0)
                MAs = MAs.Where(q => q.OIID == OIID);
            if (ACID > 0)
                MAs = MAs.Where(q => q.ACID == ACID);
            return MAs;
        }

        
        public IQueryable<M_Rool_Account> GetMRAC(int RID = 0, int ACID = 0)
        {
            var MAs = DC.M_Rool_Account.Where(q => !q.DeleteFlag &&
            q.ActiveFlag &&
            !q.Rool.DeleteFlag &&
            !q.Account.DeleteFlag &&
            (q.JoinDate == q.CreDate || q.JoinDate.Date <= DT.Date) &&
            (q.LeaveDate == q.CreDate || q.LeaveDate.Date >= DT.Date));
            if (RID > 0)
                MAs = MAs.Where(q => q.RID == RID);
            if (ACID > 0)
                MAs = MAs.Where(q => q.ACID == ACID);

            return MAs;
        }
        //取得地址的字串
        public string GetLocationString(Location L)
        {
            string str = "";
            if (L != null)
            {
                switch (L.ZipCode.GroupName)
                {
                    case "鄉鎮市區":
                        {
                            str = GetZipData(L.ZID, true) + L.Address;
                        }
                        break;
                    case "洲":
                        {
                            str = "海外" + L.Address.Replace("%", "</br>");
                        }
                        break;
                    case "網路":
                        {
                            str = L.ZipCode.Title + L.Address;
                        }
                        break;
                    default:
                        {
                            str = L.Address;
                        }
                        break;
                }
            }

            return str;
        }

        public string GetStringValue(string sReturn, FormCollection FC, string sKey)
        {
            if (FC.AllKeys.FirstOrDefault(q => q == sKey) != null)
                sReturn = FC.Get(sKey);

            return sReturn;
        }
        #endregion
    }
}