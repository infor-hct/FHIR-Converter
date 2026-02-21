// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Liquid.Converter.Models.X12;

namespace Microsoft.Health.Fhir.Liquid.Converter
{
    /// <summary>
    /// Filters for X12 EDI conversion
    /// </summary>
    public partial class Filters
    {
        public static Dictionary<string, X12Segment> GetFirstX12Segments(X12Data x12Data, string segmentIdContent)
        {
            var result = new Dictionary<string, X12Segment>();
            var segmentIds = segmentIdContent.Split(@"|");
            for (var i = 0; i < x12Data.Meta.Count; ++i)
            {
                if (segmentIds.Contains(x12Data.Meta[i]) && !result.ContainsKey(x12Data.Meta[i]))
                {
                    result[x12Data.Meta[i]] = x12Data.Data[i];
                }
            }

            return result;
        }

        public static Dictionary<string, List<X12Segment>> GetX12SegmentLists(X12Data x12Data, string segmentIdContent)
        {
            var segmentIds = segmentIdContent.Split(@"|");
            return GetX12SegmentListsInternal(x12Data, segmentIds);
        }

        public static List<X12Segment> GetX12SegmentsByType(X12Data x12Data, string segmentId)
        {
            var result = new List<X12Segment>();
            for (var i = 0; i < x12Data.Meta.Count; ++i)
            {
                if (string.Equals(x12Data.Meta[i], segmentId, StringComparison.InvariantCultureIgnoreCase))
                {
                    result.Add(x12Data.Data[i]);
                }
            }

            return result;
        }

        public static Dictionary<string, List<X12Segment>> GetX12RelatedSegmentList(X12Data x12Data, X12Segment parentSegment, string childSegmentId)
        {
            var result = new Dictionary<string, List<X12Segment>>();
            var segments = new List<X12Segment>();
            var parentFound = false;
            var childIndex = -1;

            for (var i = 0; i < x12Data.Meta.Count; ++i)
            {
                if (ReferenceEquals(x12Data.Data[i], parentSegment))
                {
                    parentFound = true;
                }
                else if (string.Equals(x12Data.Meta[i], childSegmentId, StringComparison.InvariantCultureIgnoreCase) && parentFound)
                {
                    childIndex = i;
                    break;
                }
            }

            if (childIndex > -1)
            {
                while (childIndex < x12Data.Meta.Count && string.Equals(x12Data.Meta[childIndex], childSegmentId, StringComparison.InvariantCultureIgnoreCase))
                {
                    segments.Add(x12Data.Data[childIndex]);
                    childIndex++;
                }

                result[childSegmentId] = segments;
            }

            return result;
        }

        public static string GetX12LoopId(X12Data x12Data, X12Segment segment)
        {
            // Determine the loop ID based on the HL segment hierarchy
            for (var i = 0; i < x12Data.Data.Count; ++i)
            {
                if (ReferenceEquals(x12Data.Data[i], segment))
                {
                    // Look backwards for the nearest HL segment to determine the loop
                    for (var j = i; j >= 0; j--)
                    {
                        if (string.Equals(x12Data.Meta[j], "HL", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var hlSegment = x12Data.Data[j];
                            // HL03 contains the level code
                            if (hlSegment.Elements.Count > 3)
                            {
                                return hlSegment.Elements[3]?.Value ?? string.Empty;
                            }

                            break;
                        }
                    }

                    break;
                }
            }

            return string.Empty;
        }

        public static bool HasX12Segments(X12Data x12Data, string segmentIdContent)
        {
            var segmentIds = segmentIdContent.Split(@"|");
            var segmentLists = GetX12SegmentListsInternal(x12Data, segmentIds);
            return segmentIds.All(segmentLists.ContainsKey);
        }

        public static List<X12Data> SplitX12ByHlLoop(X12Data x12Data)
        {
            var results = new List<X12Data>();
            X12Data current = null;

            for (var i = 0; i < x12Data.Meta.Count; ++i)
            {
                if (string.Equals(x12Data.Meta[i], "HL", StringComparison.InvariantCultureIgnoreCase))
                {
                    if (current != null)
                    {
                        results.Add(current);
                    }

                    current = new X12Data
                    {
                        Delimiters = x12Data.Delimiters,
                    };
                }

                if (current == null)
                {
                    current = new X12Data
                    {
                        Delimiters = x12Data.Delimiters,
                    };
                }

                current.Meta.Add(x12Data.Meta[i]);
                current.Data.Add(x12Data.Data[i]);
            }

            if (current != null && current.Meta.Count > 0)
            {
                results.Add(current);
            }

            return results;
        }

        private static Dictionary<string, List<X12Segment>> GetX12SegmentListsInternal(X12Data x12Data, string[] segmentIds)
        {
            var result = new Dictionary<string, List<X12Segment>>();
            for (var i = 0; i < x12Data.Meta.Count; ++i)
            {
                if (segmentIds.Contains(x12Data.Meta[i]))
                {
                    if (result.ContainsKey(x12Data.Meta[i]))
                    {
                        result[x12Data.Meta[i]].Add(x12Data.Data[i]);
                    }
                    else
                    {
                        result[x12Data.Meta[i]] = new List<X12Segment> { x12Data.Data[i] };
                    }
                }
            }

            return result;
        }
    }
}
