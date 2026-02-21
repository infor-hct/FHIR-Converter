// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;
using DotLiquid;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Models.X12;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;
using Microsoft.Health.MeasurementUtility;

namespace Microsoft.Health.Fhir.Liquid.Converter.Processors
{
    public class X12Processor : BaseProcessor
    {
        private readonly IDataParser _parser = new X12DataParser();

        public X12Processor(ProcessorSettings processorSettings, ILogger<X12Processor> logger)
            : base(processorSettings, logger)
        {
        }

        protected override string DataKey { get; set; } = "x12Data";

        protected override DataType DataType { get; set; } = DataType.X12;

        protected override string InternalConvert(string data, string rootTemplate, ITemplateProvider templateProvider, TraceInfo traceInfo = null)
        {
            object x12Data;
            using (ITimed inputDeserializationTime =
                Performance.TrackDuration(duration => LogTelemetry(FhirConverterMetrics.InputDeserializationDuration, duration)))
            {
                x12Data = _parser.Parse(data);
            }

            return InternalConvertFromObject(x12Data, rootTemplate, templateProvider, traceInfo);
        }

        protected override Context CreateContext(ITemplateProvider templateProvider, IDictionary<string, object> data, string rootTemplate)
        {
            var context = base.CreateContext(templateProvider, data, rootTemplate);
            return context;
        }

        protected override void CreateTraceInfo(object data, Context context, TraceInfo traceInfo)
        {
            if (traceInfo is X12TraceInfo x12TraceInfo)
            {
                x12TraceInfo.UnusedSegments = X12TraceInfo.CreateTraceInfo(data as X12Data).UnusedSegments;
            }
        }
    }
}
