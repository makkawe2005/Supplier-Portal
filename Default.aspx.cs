using System;
using SupplierPortal.lib;

namespace SupplierPortal
{
    public partial class DefaultPage : BasePage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // BoundField HTML-encodes by default, so values from the database are shown as text.
            gridTasks.DataSource = Requests.MyTasks(Me);
            gridTasks.DataBind();
            gridMine.DataSource = Requests.MyRequests(Me);
            gridMine.DataBind();
        }
    }
}
