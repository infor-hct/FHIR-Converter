// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;

namespace Microsoft.Health.Fhir.Liquid.Converter.Validators
{
    public class X12DataValidator
    {
        private const string IsaSegmentId = "ISA";
        private const int IsaFixedLength = 106;

        public void ValidateInterchangeHeader(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new DataParseException(FhirConverterErrorCode.NullOrWhiteSpaceInput, Resources.NullOrWhiteSpaceInput);
            }

            var trimmed = message.TrimStart();
            if (trimmed.Length < IsaFixedLength)
            {
                throw new DataParseException(FhirConverterErrorCode.InvalidX12Message, string.Format(Resources.InvalidX12Message, "ISA segment is too short"));
            }

            if (!trimmed.StartsWith(IsaSegmentId, StringComparison.OrdinalIgnoreCase))
            {
                throw new DataParseException(FhirConverterErrorCode.InvalidX12Message, string.Format(Resources.InvalidX12Message, "Message does not start with ISA segment"));
            }
        }
    }
}
