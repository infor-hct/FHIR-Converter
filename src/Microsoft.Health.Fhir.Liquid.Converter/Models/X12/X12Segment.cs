// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using DotLiquid;
using Microsoft.Health.Fhir.Liquid.Converter.DotLiquids;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;

namespace Microsoft.Health.Fhir.Liquid.Converter.Models.X12
{
    public class X12Segment : Drop
    {
        public X12Segment(string segmentId, SafeList<X12Element> elements)
        {
            SegmentId = segmentId;
            Value = string.Empty;
            Elements = elements;
        }

        public X12Segment(string segmentId, string rawValue, SafeList<X12Element> elements)
        {
            SegmentId = segmentId;
            Value = rawValue;
            Elements = elements;
        }

        public string SegmentId { get; set; }

        public string Value { get; set; }

        public SafeList<X12Element> Elements { get; set; }

        public override object this[object index]
        {
            get
            {
                if (!(index is string || index is int))
                {
                    throw new RenderException(FhirConverterErrorCode.PropertyNotFound, string.Format(Resources.PropertyNotFound, index, this.GetType().Name));
                }

                var indexString = index.ToString();

                if (string.Equals(indexString, "Value", StringComparison.InvariantCultureIgnoreCase))
                {
                    return Value;
                }

                if (string.Equals(indexString, "SegmentId", StringComparison.InvariantCultureIgnoreCase))
                {
                    return SegmentId;
                }

                if (string.Equals(indexString, "Elements", StringComparison.InvariantCultureIgnoreCase))
                {
                    return Elements;
                }

                if (int.TryParse(indexString, out int result))
                {
                    return (X12Element)Elements[result];
                }

                throw new RenderException(FhirConverterErrorCode.PropertyNotFound, string.Format(Resources.PropertyNotFound, indexString, this.GetType().Name));
            }
        }
    }
}
