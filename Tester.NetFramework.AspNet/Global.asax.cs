using Prometheus;
using System;
using System.Web.Http;

namespace Tester.NetFramework.AspNet
{
    public class Global : System.Web.HttpApplication
    {
        protected void Application_Start(object sender, EventArgs e)
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            AspNetMetricServer.RegisterRoutes(GlobalConfiguration.Configuration);
        }
    }
}