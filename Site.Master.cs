using System;
using SupplierPortal.lib;

namespace SupplierPortal
{
    public partial class SiteMaster : System.Web.UI.MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            var me = CurrentUser.Get();
            litUser.Text = Server.HtmlEncode(me.UserName + (me.IsKnown ? " · " + me.Role : ""));
            lnkNew.Visible = me.CanSubmit();
            lnkAdmin.Visible = Requests.IsAdmin(me);
        }
    }
}
