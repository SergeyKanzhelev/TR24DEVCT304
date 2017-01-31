using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;

namespace ApplicationInsightsDataROI
{
    class DefaultTelemetryInitializer : ITelemetryInitializer
    {
        string userId = Guid.NewGuid().ToString();
        string sessionId = Guid.NewGuid().ToString();

        public void Initialize(ITelemetry telemetry)
        {
            telemetry.Context.Component.Version = "1.2.3";
            telemetry.Context.Cloud.RoleName = "ConsoleApp";
            telemetry.Context.User.Id = userId;
            telemetry.Context.Session.Id = sessionId;
        }
    }
}
