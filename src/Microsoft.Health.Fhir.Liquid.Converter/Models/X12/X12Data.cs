// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using DotLiquid;

namespace Microsoft.Health.Fhir.Liquid.Converter.Models.X12
{
    public class X12Data : Drop
    {
        public X12Data(string value = null)
        {
            Value = value;
            Meta = new List<string>();
            Data = new List<X12Segment>();
        }

        public string Value { get; set; }

        public X12Delimiters Delimiters { get; set; }

        public List<string> Meta { get; set; }

        public List<X12Segment> Data { get; set; }
    }
}
