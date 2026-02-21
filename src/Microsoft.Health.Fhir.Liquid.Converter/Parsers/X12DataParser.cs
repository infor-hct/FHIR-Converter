// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Liquid.Converter.DotLiquids;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Models.X12;
using Microsoft.Health.Fhir.Liquid.Converter.Validators;

namespace Microsoft.Health.Fhir.Liquid.Converter.Parsers
{
    public class X12DataParser : IDataParser
    {
        private static readonly X12DataValidator Validator = new X12DataValidator();

        public object Parse(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new DataParseException(FhirConverterErrorCode.NullOrWhiteSpaceInput, Resources.NullOrWhiteSpaceInput);
            }

            try
            {
                var trimmedMessage = message.TrimStart();
                Validator.ValidateInterchangeHeader(trimmedMessage);

                var delimiters = ParseDelimiters(trimmedMessage);
                var result = new X12Data(message)
                {
                    Delimiters = delimiters,
                };

                var segments = SplitIntoSegments(trimmedMessage, delimiters);

                foreach (var segmentRaw in segments)
                {
                    var segment = segmentRaw.Trim();
                    if (string.IsNullOrWhiteSpace(segment))
                    {
                        continue;
                    }

                    var elements = ParseElements(segment, delimiters);
                    var segmentId = elements.Count > 0 ? elements[0]?.Value ?? string.Empty : string.Empty;
                    var x12Segment = new X12Segment(segmentId, segment, elements);

                    result.Meta.Add(segmentId);
                    result.Data.Add(x12Segment);
                }

                return result;
            }
            catch (DataParseException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new DataParseException(FhirConverterErrorCode.InputParsingError, string.Format(Resources.InputParsingError, ex.Message), ex);
            }
        }

        private static X12Delimiters ParseDelimiters(string message)
        {
            // ISA segment has fixed positions:
            // ISA*... where position 3 is the element separator
            // Position 104 is the sub-element separator
            // Position 105 is the segment terminator
            var delimiters = new X12Delimiters
            {
                ElementSeparator = message[3],
                SubElementSeparator = message.Length > 104 ? message[104] : ':',
                SegmentTerminator = message.Length > 105 ? message[105] : '~',
            };

            // Repetition separator is at ISA11 (element 11 of ISA)
            var isaElements = message.Substring(0, Math.Min(message.Length, 106)).Split(delimiters.ElementSeparator);
            if (isaElements.Length > 11 && !string.IsNullOrEmpty(isaElements[11]))
            {
                delimiters.RepetitionSeparator = isaElements[11][0];
            }

            return delimiters;
        }

        private static List<string> SplitIntoSegments(string message, X12Delimiters delimiters)
        {
            return message.Split(delimiters.SegmentTerminator)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .ToList();
        }

        private static SafeList<X12Element> ParseElements(string segment, X12Delimiters delimiters)
        {
            var elements = new SafeList<X12Element>();
            var elementValues = segment.Split(delimiters.ElementSeparator);

            foreach (var elementValue in elementValues)
            {
                if (!string.IsNullOrEmpty(elementValue))
                {
                    var subElements = ParseSubElements(elementValue, delimiters);
                    var element = new X12Element(elementValue.Trim(), subElements);
                    elements.Add(element);
                }
                else
                {
                    elements.Add(new X12Element());
                }
            }

            return elements;
        }

        private static SafeList<string> ParseSubElements(string elementValue, X12Delimiters delimiters)
        {
            var subElements = new SafeList<string>();
            // Add null at index 0 for 1-based indexing compatibility
            subElements.Add(null);

            var subValues = elementValue.Split(delimiters.SubElementSeparator);
            foreach (var subValue in subValues)
            {
                subElements.Add(subValue.Trim());
            }

            return subElements;
        }

    }
}
