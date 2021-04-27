using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Text;

namespace TeamsComBackEnd
{
    public static class MyAppInsights
    {
        private static TelemetryClient mLogger;

        public static TelemetryClient Logger
        {
            get
            {
                if (mLogger == null)
                {
                    var lConfig = new TelemetryConfiguration(Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY"));

                    mLogger = new TelemetryClient(lConfig);
                }

                return mLogger;
            }

            private set { }
        }
    }
}
