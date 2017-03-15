using System;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;

namespace ApplicationInsightsDataROI
{
    class DependencyFilteringTelemetryProcessor : ITelemetryProcessor
    {
        private readonly ITelemetryProcessor _next;
        private readonly Metric _numberOfDependencies;
        private readonly Metric _dependenciesDuration;

        public DependencyFilteringTelemetryProcessor(ITelemetryProcessor next, TelemetryConfiguration configuraiton)
        {
            _next = next;

            MetricManager manager = new MetricManager(new TelemetryClient(configuraiton));
            _numberOfDependencies = manager.CreateMetric("# of dependencies");
            _dependenciesDuration = manager.CreateMetric("dependencies duration (ms)");
        }

        public void Process(ITelemetry item)
        {
            // check telemetry type
            if (item is DependencyTelemetry)
            {
                var d = item as DependencyTelemetry;

                // increment counters
                _numberOfDependencies.Track(1);
                _dependenciesDuration.Track(d.Duration.TotalMilliseconds);

                if (d.Duration < TimeSpan.FromMilliseconds(100))
                {
                    // if dependency duration > 100 msec then stop telemetry  
                    // processing and return from the pipeline
                    return;
                }
            }

            this._next.Process(item);
        }
    }
}
