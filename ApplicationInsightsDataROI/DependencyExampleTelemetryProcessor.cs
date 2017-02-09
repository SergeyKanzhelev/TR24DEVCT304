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
    class DependencyExampleTelemetryProcessor : ITelemetryProcessor
    {
        private ITelemetryProcessor _next;

        public DependencyExampleTelemetryProcessor(ITelemetryProcessor next)
        {
            this._next = next;
        }

        public void Process(ITelemetry item)
        {
            if (item is DependencyTelemetry)
            {
                var r = item as DependencyTelemetry;
                if (r.Duration > TimeSpan.FromMilliseconds(100))
                {
                    ((ISupportSampling)item).SamplingPercentage = 100;
                }
            }
            this._next.Process(item);
        }
    }
}
