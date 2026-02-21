// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using DotLiquid;

namespace Microsoft.Health.Fhir.Liquid.Converter.Models.X12
{
    public class X12Delimiters : Drop
    {
        public X12Delimiters()
        {
            ElementSeparator = '*';
            SubElementSeparator = ':';
            SegmentTerminator = '~';
            RepetitionSeparator = '^';
        }

        public char ElementSeparator { get; set; }

        public char SubElementSeparator { get; set; }

        public char SegmentTerminator { get; set; }

        public char RepetitionSeparator { get; set; }
    }
}
