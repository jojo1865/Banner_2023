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

namespace Banner
{
    public class PublicClass : Controller
    {
        public DataClassesDataContext DC { get; set; }
        public DateTime DT = DateTime.Now;
        public string[] sWeeks = new string[] { "星期日", "星期一", "星期二", "星期三", "星期四", "星期五", "星期六" };
        public string sPhotoFileName = "Photo";//圖片存檔資料夾名稱
        public string sNoPhoto = "/Photo/NoPhoto.jpg";
        public string sFileName = "Files";//下載檔案資料夾名稱
        public string sAdminPhotoFilePath = "../../../";//從後台最底層到圖片存檔資料夾的回推路徑
        public string DateTimeFormat = "yyyy-MM-dd HH:mm";
        public bool bUsedNewName = true;
        public bool[] bGroup = new bool[] { false, false, false, false, false, false }; //權限
        public string Error = "";

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
                SetCookie(Title, sStr);
            else if (cStr != "-1" && sStr == "-1")//Session遺失但Cookie還在
                SetSession(Title, cStr);
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
            //       tlc.MaxSupportedDateTime.ToShortDateString();
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
        public List<ZipCode> GetOldZip(long ZID, int ActiveType = 1, List<ZipCode> Zs = null)
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
        public string GetZipData(long ZID, bool ShowZipCodeFlag = false)
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
        //取得前台網頁名稱
        public string GetFrontPageName()
        {
            string PagePath = System.IO.Path.GetFileName(Request.PhysicalPath);
            var Menus = DC.Menu.Where(q => q.ActiveFlag && !q.DeleteFlag && q.MenuType > 1).ToList();
            if (Menus.Count > 0)
                return Menus[0].Title;
            else return "";
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
            return NumCut > 0 ? (TotalCt % NumCut == 0 ? (TotalCt / NumCut) + 1 : TotalCt / NumCut) : 0;
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
        public void WriteExcelFromString(string Title, ArrayList AL, bool bAdmin)
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
            WriteExcel(Title, AL2, bAdmin);
        }

        public void WriteCSVFromString(string Title, ArrayList AL, bool bAdmin, Encoding Code)
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
            WriteCSV(Title, AL2, bAdmin, Code);
        }

        public void WriteExcel(string sTitle, ArrayList AL, bool bAdmin)
        {
            // 設定儲存檔名，不用設定副檔名，系統自動判斷 excel 版本，產生 .xls 或 .xlsx 副檔名
            //string pathFile = @"D:\test";
            string FileName = DateTime.Now.ToString("yyyyMMddHHmmss") + sTitle + ".xlsx";
            string s = bAdmin ? "../../" : "../";
            string FilePath = s + "Files/Down/";
            CheckFileExist(Server.MapPath(FilePath));
            string pathFile = Server.MapPath(FilePath + FileName);


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
                        //cell.Style.Fill.BackgroundColor.SetColor(Color.Red);//ECs[j].cBagColor
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
            Response.Write(string.Format("<script>window.open('{0}', '" + FileName + "','');</script>", URL + FileName));//下載

        }
        //匯出CSV的方法
        public void WriteCSV(string sTitle, ArrayList AL, bool bAdmin, Encoding Code)
        {
            string s = bAdmin ? "../../" : "../";
            string FilePath = Server.MapPath(s + "Files/Down/");
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

            OfficeOpenXml.ExcelPackage p = new OfficeOpenXml.ExcelPackage(fs);
            OfficeOpenXml.ExcelWorksheet sheet = p.Workbook.Worksheets[1];
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
            ViewBag._Title = "旌旗教會 管理系統";
            ViewBag._KeyWord = "";
            ViewBag._Description = "";

            string NowURL = Request.Url.AbsoluteUri;
            ViewBag._URL = NowURL;

            if (NowURL.ToLower().Contains("/admin/"))
            {
                ViewBag._Css1 = "";
                string NowPath = Request.RawUrl.Split('?')[0];
                /*var M = DC.Menu.FirstOrDefault(q => q.ItemID == "M__" && q.ActiveFlag && !q.DeleteFlag && q.URL == NowPath);
                if (M != null)
                    ViewBag._Title = M.Title;*/
            }
            else
                ViewBag._Css1 = "";
            ViewBag._Css2 = "";

            ViewBag._Logo = "";
            ViewBag._SysMsg = "";//用於系統後台系統提示訊息
            ViewBag._UserName = "";
            int ACID = GetACID();
            if (ACID > 0)
            {
                var U = DC.Account.FirstOrDefault(q => q.ACID == ACID);
                if (U != null)
                    ViewBag._UserName = U.Name;
            }
            TempData["UID"] = ACID;
            TempData["msg"] = "";
            TempData["msgtype"] = "";
            TempData["url"] = "";
            TempData["_url"] = NowURL;
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
                bool.TryParse(Input, out bCheck);
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

    }
}