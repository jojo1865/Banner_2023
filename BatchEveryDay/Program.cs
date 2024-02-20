// See https://aka.ms/new-console-template for more information
using System.Net;

Console.WriteLine("開始執行");

try
{
    string WriteMsg = "";
    //瀏覽紀錄整理
    #region 瀏覽紀錄整理

    bool SuccessFlag = false;
    string _NewsURL = "https://web-banner.viuto-aiot.com/Web/Home/Batch_EveryDay";
    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(_NewsURL);
    request.Timeout = 600000;
    using (var response = request.GetResponse())
    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
    {
        string str = sr.ReadToEnd();
        SuccessFlag = str.Contains("OK");
    }
    WriteMsg += " 每日批次執行-執行" + (SuccessFlag ? "成功" : "失敗");

    #endregion
    string LogFile = "Log_EveryDay_" + DateTime.Now.ToString("yyyy") + ".txt";
    StreamWriter SW = new StreamWriter(System.Environment.CurrentDirectory + "\\" + LogFile, true);
    SW.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm") + WriteMsg);
    SW.Close();
}
catch(Exception e)
{
    string LogFile = "Log_EveryDay_" + DateTime.Now.ToString("yyyy") + ".txt";
    StreamWriter SW = new StreamWriter(System.Environment.CurrentDirectory + "\\" + LogFile, true);
    SW.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm") + e.Message);
    SW.Close();
}
Console.ReadLine();
Console.WriteLine("結束執行");
