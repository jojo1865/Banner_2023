using System.Web.Mvc;

namespace Banner.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName
        {
            get
            {
                return "Admin";
            }
        }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute(
                "Admin_default",
                "Admin/{controller}/{action}/{ID}",
                new { controller = "Home", action = "Index", ID = UrlParameter.Optional }
            );

            context.MapRoute(
                "Organize_Info_List",
                "Admin/{controller}/{action}/{OID}/{ID}",
                new { controller = "OrganizeSet", action = "Organize_Info_List", OID = UrlParameter.Optional, ID = UrlParameter.Optional }
            );

            context.MapRoute(
                "Organize_Info_Edit",
                "Admin/{controller}/{action}/{OID}/{PID}/{OIID}",
                new { controller = "OrganizeSet", action = "Organize_Info_Edit",  OID = UrlParameter.Optional, PID = UrlParameter.Optional, OIID = UrlParameter.Optional }
            );

            
        }

    }
}