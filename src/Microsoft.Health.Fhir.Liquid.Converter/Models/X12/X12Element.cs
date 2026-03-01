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
    public class X12Element : Drop
    {
        public X12Element(string value, SafeList<string> subElements)
        {
            Value = value;
            SubElements = subElements;
        }

        public X12Element()
        {
            Value = string.Empty;
            SubElements = new SafeList<string>();
        }

        public string Value { get; set; }

        public SafeList<string> SubElements { get; set; }

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

                if (string.Equals(indexString, "SubElements", StringComparison.InvariantCultureIgnoreCase))
                {
                    return SubElements;
                }

                if (int.TryParse(indexString, out int result))
                {
                    return (string)SubElements[result];
                }

                throw new RenderException(FhirConverterErrorCode.PropertyNotFound, string.Format(Resources.PropertyNotFound, indexString, this.GetType().Name));
            }
        }
    }
}
