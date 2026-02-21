// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using DotLiquid;
using Microsoft.Extensions.Logging;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Models.X12;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Microsoft.Health.MeasurementUtility;
using Newtonsoft.Json.Linq;

namespace Microsoft.Health.Fhir.Liquid.Converter.Processors
{
    public class FhirToX12Processor : BaseProcessor
    {
        private readonly IDataParser _parser = new JsonDataParser();

        public FhirToX12Processor(ProcessorSettings processorSettings, ILogger<FhirToX12Processor> logger)
            : base(processorSettings, logger)
        {
        }

        protected override string InternalConvert(string data, string rootTemplate, ITemplateProvider templateProvider, TraceInfo traceInfo = null)
        {
            object jsonData;
            using (ITimed inputDeserializationTime =
                Performance.TrackDuration(duration => LogTelemetry(FhirConverterMetrics.InputDeserializationDuration, duration)))
            {
                jsonData = _parser.Parse(data);
            }

            var result = InternalConvertFromObject(jsonData, rootTemplate, templateProvider, traceInfo);

            var x12Message = GenerateX12Message(JObject.Parse(result));

            return x12Message;
        }

        public string GenerateX12Message(JObject transformations)
        {
            if (transformations["x12Definition"] == null)
            {
                throw new RenderException(
                    FhirConverterErrorCode.PropertyNotFound,
                    "The x12Definition JSON property was not found in the conversion template");
            }

            var sb = new StringBuilder();
            var delimiters = new X12Delimiters();

            if (transformations["delimiters"] is JObject delimitersObj)
            {
                if (delimitersObj["elementSeparator"] != null)
                {
                    delimiters.ElementSeparator = delimitersObj["elementSeparator"].ToString()[0];
                }

                if (delimitersObj["subElementSeparator"] != null)
                {
                    delimiters.SubElementSeparator = delimitersObj["subElementSeparator"].ToString()[0];
                }

                if (delimitersObj["segmentTerminator"] != null)
                {
                    delimiters.SegmentTerminator = delimitersObj["segmentTerminator"].ToString()[0];
                }
            }

            foreach (var segment in transformations["x12Definition"])
            {
                var segmentObj = segment as JObject;
                if (segmentObj == null)
                {
                    continue;
                }

                var segmentName = segmentObj.Properties().First().Name;
                var elements = segmentObj[segmentName] as JArray;
                if (elements == null)
                {
                    continue;
                }

                sb.Append(segmentName);
                foreach (var element in elements)
                {
                    sb.Append(delimiters.ElementSeparator);
                    sb.Append(element?.ToString() ?? string.Empty);
                }

                sb.Append(delimiters.SegmentTerminator);
                sb.AppendLine();
            }

            return sb.ToString();
        }

        protected override Context CreateContext(ITemplateProvider templateProvider, IDictionary<string, object> data, string rootTemplate)
        {
            var cancellationToken = Settings.TimeOut > 0 ? new CancellationTokenSource(Settings.TimeOut).Token : CancellationToken.None;
            var context = new Context(
                environments: new List<Hash> { Hash.FromDictionary(data) },
                outerScope: new Hash(),
                registers: Hash.FromDictionary(new Dictionary<string, object> { { "file_system", templateProvider.GetTemplateFileSystem() } }),
                errorsOutputMode: ErrorsOutputMode.Rethrow,
                maxIterations: Settings.MaxIterations,
                formatProvider: CultureInfo.InvariantCulture,
                cancellationToken: cancellationToken);

            context.AddFilters(typeof(Filters));

            return context;
        }
    }
}
