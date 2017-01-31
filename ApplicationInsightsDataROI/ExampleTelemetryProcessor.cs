using Microsoft.ApplicationInsights.Extensibility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;

namespace ApplicationInsightsDataROI
{
    class ExampleTelemetryProcessor : ITelemetryProcessor
    {
        private ITelemetryProcessor _next;

        public ExampleTelemetryProcessor(ITelemetryProcessor next)
        {
            this._next = next;
        }

        public void Process(ITelemetry item)
        {
            if (item is RequestTelemetry)
            {
                var r = item as RequestTelemetry;
                if (r.Duration > TimeSpan.FromSeconds(5))
                {
                    ((ISupportSampling)item).SamplingPercentage = 100;
                }
            }
            this._next.Process(item);
        }
    }
}
