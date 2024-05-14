using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BatchEveryMinute
{
    internal class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine("Start-Every min");
            do
            {
                try
                {
                    //瀏覽紀錄整理
                    #region 瀏覽紀錄整理

                    bool SuccessFlag = false;

                    string _NewsURL = "https://web-banner.viuto-aiot.com/Web/Home/Batch_EveryMin";
                    //string _NewsURL = "http://localhost:8001/Web/Home/Batch_EveryMin";

                    HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(_NewsURL);
                    request.Timeout = 60000;
                    using (var response = request.GetResponse())
                    using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                    {
                        string str = sr.ReadToEnd();
                        SuccessFlag = str.Contains("OK");
                    }
                    string WriteMsg = " 批次執行-執行" + (SuccessFlag ? "成功" : "失敗");

                    #endregion
                    string LogFile = "Log_EveryMin_" + DateTime.Now.ToString("yyyy_MM_dd") + ".txt";
                    string LogFilePath = System.Environment.CurrentDirectory + "\\" + LogFile;
                    StreamWriter SW = new StreamWriter(LogFilePath, true);
                    SW.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm") + WriteMsg);
                    SW.Close();
                }
                catch (Exception e)
                {
                    string LogFile = "Log_EveryDay_" + DateTime.Now.ToString("yyyy") + ".txt";
                    StreamWriter SW = new StreamWriter(System.Environment.CurrentDirectory + "\\" + LogFile, true);
                    SW.WriteLine(DateTime.Now.ToString("yyyy/MM/dd HH:mm") + e.Message);
                    SW.Close();
                    break;
                }
                Thread.Sleep(new TimeSpan(0, 1, 0));//等一分鐘
            } while (true);
            Console.WriteLine("End");

        }
    }
}
