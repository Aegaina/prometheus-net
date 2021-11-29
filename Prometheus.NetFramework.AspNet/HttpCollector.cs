using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Prometheus
{
    /// <summary>
    /// Collecting metrics about the HTTP requests
    /// </summary>
    /// <remarks>
    /// Reference code: https://github.com/rocklan/prometheus-net.AspNet/blob/master/src/Prometheus.AspNet/Classes/PrometheusHttpRequestModule.cs
    /// </remarks>
    public class HttpCollector : IHttpModule
    {
        private const string REQUEST_TIMER_KEY = "Prometheus.HttpRequest.Timer";

        #region static

        private static readonly Gauge httpRequestsInProgress = Metrics
            .CreateGauge("http_requests_in_progress", "The number of HTTP requests currently in progress");

        private static readonly Gauge httpRequestsTotal = Metrics
            .CreateGauge("http_requests_received_total", "Provides the count of HTTP requests that have been processed by this app",
                new GaugeConfiguration { LabelNames = new[] { "code", "method", "path" } });

        private static readonly Histogram httpRequestsDuration = Metrics
            .CreateHistogram("http_request_duration_seconds", "The duration of HTTP requests processed by this app.",
                new HistogramConfiguration { LabelNames = new[] { "code", "method", "path" } });

        #endregion

        /// <summary>
        /// Track whether Dispose has been called.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Destructor
        /// </summary>
        ~HttpCollector()
        {
            // Do not re-create Dispose clean-up code here.
            // Calling Dispose(false) is optimal in terms of
            // readability and maintainability.
            Dispose(false);
        }

        /// <summary>
        /// Initializes this module and prepares it to handle requests.
        /// </summary>
        /// <param name="application">An HttpApplication that provides access to the methods, properties, and events common to all application objects within an ASP.NET application.</param>
        public void Init(HttpApplication application)
        {
            if (application != null)
            {
                application.BeginRequest += Application_BeginRequest;
                application.EndRequest += Application_EndRequest;
            }
        }

        /// <summary>
        /// Occurs as the first event in the HTTP pipeline chain of execution when ASP.NET responds to a request.
        /// </summary>
        private void Application_BeginRequest(object sender, EventArgs e)
        {
            httpRequestsInProgress.Inc();

            if (HttpContext.Current != null)
            {
                //Record the time of the begin request event
                Stopwatch timer = new Stopwatch();
                HttpContext.Current.Items[REQUEST_TIMER_KEY] = timer;
                timer.Start();
            }
        }

        /// <summary>
        /// Occurs as the last event in the HTTP pipeline chain of execution when ASP.NET responds to a request.
        /// </summary>
        public void Application_EndRequest(object sender, EventArgs e)
        {
            httpRequestsInProgress.Dec();

            if (HttpContext.Current != null)
            {
                Stopwatch timer = HttpContext.Current.Items[REQUEST_TIMER_KEY] as Stopwatch;
                if (timer != null)
                {
                    try
                    {
                        timer.Stop();

                        string code = HttpContext.Current.Response.StatusCode.ToString();
                        string method = HttpContext.Current.Request.HttpMethod;
                        string path = HttpContext.Current.Request.AppRelativeCurrentExecutionFilePath;

                        httpRequestsTotal.WithLabels(code, method, path).Inc();

                        double timeTakenSecs = timer.ElapsedMilliseconds / 1000d;
                        httpRequestsDuration.WithLabels(code, method, path).Observe(timeTakenSecs);
                    }
                    finally
                    {
                        HttpContext.Current.Items.Remove(REQUEST_TIMER_KEY);
                    }
                }
            }
        }

        #region IDisposable

        /// <summary>
        /// Implement IDisposable.
        /// </summary>
        /// <remark>
        /// Do not make this method virtual.
        /// A derived class should not be able to override this method.
        /// </remark>
        public void Dispose()
        {
            Dispose(true);
            // This object will be cleaned up by the Dispose method.
            // Therefore, you should call GC.SupressFinalize to
            // take this object off the finalization queue
            // and prevent finalization code for this object
            // from executing a second time.
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose(bool disposing) executes in two distinct scenarios.
        /// </summary>
        /// <param name="disposing">
        /// If disposing equals true, the method has been called directly
        /// or indirectly by a user's code. Managed and unmanaged resources
        /// can be disposed.
        /// If disposing equals false, the method has been called by the
        /// runtime from inside the finalizer and you should not reference
        /// other objects. Only unmanaged resources can be disposed.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            // Check to see if Dispose has already been called.
            if (!this.disposed)
            {
                // If disposing equals true, dispose all managed
                // and unmanaged resources.
                if (disposing)
                {
                    #region Dispose managed resources

                    if (HttpContext.Current != null && HttpContext.Current.ApplicationInstance != null)
                    {
                        HttpContext.Current.ApplicationInstance.BeginRequest -= Application_BeginRequest;
                        HttpContext.Current.ApplicationInstance.EndRequest -= Application_EndRequest;
                    }

                    #endregion
                }

                // Call the appropriate methods to clean up
                // unmanaged resources here.
                // If disposing is false,
                // only the following code is executed.
                //ToDo:

                // Note disposing has been done.
                disposed = true;
            }
        }

        #endregion
    }
}