using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Banner.Areas.Web.Controllers
{
    public class OutputController : PublicClass
    {
        // GET: Web/Output
        public ActionResult Index()
        {
            return View();
        }
        #region 登入
        [HttpGet]
        public ActionResult Login()
        {
            GetViewBag();

            TempData["login"] = "";
            TempData["pw"] = "";
            TempData["json"] = "";
            TempData["rurl"] = "";
            if (!CheckJWT(0))
                SetAlert("您不被授權使用登入工具", 2, "/Web/Home/Login");
            return View();
        }
        [HttpPost]
        
        public ActionResult Login(FormCollection FC)
        {
            GetViewBag();

            string Login = FC.Get("txb_Login");
            string PW = FC.Get("txb_Password");
            string ValidateCode = FC.Get("txb_ValidateCode");
            TempData["login"] = Login;
            TempData["pw"] = PW;
            TempData["json"] = "";
            TempData["rurl"] = "";
            string sReturn = ReturnJWT0();
            if (Login.Replace(" ", "") == string.Empty)
                Error += "請輸入帳號</br>";
            if (PW.Replace(" ", "") == string.Empty)
                Error += "請輸入密碼</br>";
            if (ValidateCode.Replace(" ", "") == string.Empty)
                Error += "請輸入驗證碼</br>";
            else if (ValidateCode != GetSession("VNum"))
                Error += "驗證碼輸入錯誤</br>";

            //if (sReturn == "")
            //    Error += "Token驗證成功但無返回網址</br>";
            if (Error != "")
                SetAlert(Error, 2);
            else
            {
                string EncPW = HSM.Enc_1(PW);
                var AC = DC.Account.FirstOrDefault(q => q.ActiveFlag && !q.DeleteFlag && (q.Login == Login || q.IDNumber == Login) && q.Password == EncPW);
                if (AC == null)
                    SetAlert("此帳號/身分證字號不存在", 2);
                else
                {

                    DelSession("LoginCt");
                    DelSession("LoginAccount");
                    ReturnACID R = new ReturnACID();
                    R.Status = "OK";
                    R.ID = HSM.Enc_1(AC.ACID.ToString());
                    R.Name = AC.Name_First + AC.Name_Last;
                    TempData["json"] = JsonConvert.SerializeObject(R);
                    TempData["rurl"] = sReturn;
                }
            }
            return View();
        }
        public class ReturnACID
        {
            public string Status = "Error";
            public string ErrorMsg = "";
            public string ID = "";
            public string Name = "";
        }
        #endregion
    }
}