// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Health.Fhir.Liquid.Converter.Models.X12
{
    public class X12TraceInfo : TraceInfo
    {
        public X12TraceInfo()
        {
            UnusedSegments = new List<string>();
        }

        public List<string> UnusedSegments { get; set; }

        public static X12TraceInfo CreateTraceInfo(object data)
        {
            var traceInfo = new X12TraceInfo();
            if (data is X12Data x12Data)
            {
                for (var i = 0; i < x12Data.Data.Count; i++)
                {
                    traceInfo.UnusedSegments.Add(x12Data.Meta[i]);
                }
            }

            return traceInfo;
        }
    }
}
