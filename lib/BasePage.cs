using System;
using System.Web;
using System.Web.UI;

namespace SupplierPortal.lib
{
    /// <summary>
    /// Every page derives from this. It ties ViewState to the user's session (protects postbacks
    /// against cross-site request forgery) and turns away users who aren't in DW_MDM_APP_USERS.
    /// </summary>
    public class BasePage : Page
    {
        internal CurrentUser Me { get { return CurrentUser.Get(); } }

        protected override void OnInit(EventArgs e)
        {
            Session["SP.Init"] = true;                 // keeps the session id stable
            ViewStateUserKey = Session.SessionID;
            base.OnInit(e);

            if (!Me.IsKnown) Deny("Your account isn't set up for the Supplier Portal. Please contact the MDM team.");
        }

        protected void Deny(string message)
        {
            Response.Clear();
            Response.StatusCode = 403;
            Response.ContentType = "text/plain";
            Response.Write(message);
            Context.ApplicationInstance.CompleteRequest();
            Response.End();
        }

        protected int QueryInt(string key)
        {
            int v;
            return int.TryParse(Request.QueryString[key], out v) ? v : 0;
        }
    }
}
