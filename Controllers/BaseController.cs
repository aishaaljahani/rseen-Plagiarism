using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Plagiarism.Controllers
{
    public class BaseController : Controller
    {
        public int GetCurrentUserId()
        {
            if (Request.IsAuthenticated && HttpContext != null)
            {
                int.TryParse(HttpContext.Request.Cookies.Get("UserId")?.Value?.ToString(), out int userId);
                return userId;
            }
            else
            {
                return 0;
            }
        }
        public string GetCurrentUserRoles()
        {
            if (Request.IsAuthenticated && HttpContext != null)
            {
                return HttpContext.Request.Cookies.Get("UserRoles")?.Value?.ToString() ?? "";
            }
            else
            {
                return "";
            }
        }
    }
}