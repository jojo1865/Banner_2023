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
                "Admin_Item",
                "Admin/{controller}/{action}/{ItemID}/{ID}",
                new { controller = "Home", action = "Index", ItemID = UrlParameter.Optional, ID = UrlParameter.Optional }
            );

            context.MapRoute(
                "Organize_Info_List",
                "Admin/{controller}/{action}/{ItemID}/{OID}/{OIID}",
                new { controller = "OrganizeSet", action = "Organize_Info_List", ItemID = UrlParameter.Optional, OID = UrlParameter.Optional, OIID = UrlParameter.Optional }
            );

            context.MapRoute(
                "Organize_Info_Edit",
                "Admin/{controller}/{action}/{ItemID}/{OID}/{PID}/{OIID}",
                new { controller = "OrganizeSet", action = "Organize_Info_Edit", ItemID = UrlParameter.Optional, OID = UrlParameter.Optional, PID = UrlParameter.Optional, OIID = UrlParameter.Optional }
            );

            
        }

    }
}