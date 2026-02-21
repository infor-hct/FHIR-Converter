// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Health.Fhir.Liquid.Converter.DotLiquids;
using Microsoft.Health.Fhir.Liquid.Converter.Models.X12;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests.FilterTests
{
    public class X12SegmentFiltersTests
    {
        // A realistic X12 270 (Eligibility Inquiry) message with HL hierarchy
        private const string TestDataContent =
            "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *200101*1253*^*00501*000000905*0*P*:~" +
            "GS*HS*SENDER*RECEIVER*20200101*1253*1*X*005010X279A1~" +
            "ST*270*0001*005010X279A1~" +
            "BHT*0022*13*10001234*20200101*1319~" +
            "HL*1**20*1~" +
            "NM1*PR*2*ABC INSURANCE*****PI*12345~" +
            "HL*2*1*21*1~" +
            "NM1*1P*1*DOE*JOHN****XX*1234567890~" +
            "REF*TJ*123456789~" +
            "HL*3*2*22*0~" +
            "NM1*IL*1*SMITH*JANE****MI*ABC123456~" +
            "REF*6P*GRP001~" +
            "DTP*291*D8*20200101~" +
            "DTP*307*D8*20201231~" +
            "EQ*30~" +
            "SE*14*0001~" +
            "GE*1*1~" +
            "IEA*1*000000905~";

        private static readonly X12Data TestData = LoadTestData();

        [Fact]
        public void GivenAnX12Data_WhenGetFirstX12Segments_CorrectResultShouldBeReturned()
        {
            // Empty data returns empty dictionary
            Assert.Empty(Filters.GetFirstX12Segments(new X12Data(), "NM1"));

            // Empty segment ID content returns empty
            Assert.Empty(Filters.GetFirstX12Segments(TestData, string.Empty));

            // Get first segments for multiple types
            var segments = Filters.GetFirstX12Segments(TestData, "ISA|GS|ST|BHT|HL|NM1|REF|DTP|EQ");
            Assert.Equal("ISA", segments["ISA"].SegmentId);
            Assert.Equal("GS", segments["GS"].SegmentId);
            Assert.Equal("ST", segments["ST"].SegmentId);
            Assert.Equal("BHT", segments["BHT"].SegmentId);

            // Should return only the FIRST HL segment (HL*1)
            Assert.Equal("HL", segments["HL"].SegmentId);
            Assert.Equal("1", ((X12Element)segments["HL"].Elements[1]).Value);

            // Should return only the FIRST NM1 segment (PR - payer)
            Assert.Equal("NM1", segments["NM1"].SegmentId);
            Assert.Equal("PR", ((X12Element)segments["NM1"].Elements[1]).Value);

            // Should return only the FIRST REF segment
            Assert.Equal("REF", segments["REF"].SegmentId);
            Assert.Equal("TJ", ((X12Element)segments["REF"].Elements[1]).Value);

            // Should return only the FIRST DTP segment
            Assert.Equal("DTP", segments["DTP"].SegmentId);
            Assert.Equal("291", ((X12Element)segments["DTP"].Elements[1]).Value);

            // Segment ID not in data should not be in result
            var segs2 = Filters.GetFirstX12Segments(TestData, "ZZZ");
            Assert.True(!segs2.ContainsKey("ZZZ"));

            // Null X12Data should throw
            Assert.Throws<NullReferenceException>(() => Filters.GetFirstX12Segments(null, "NM1"));
            Assert.Throws<NullReferenceException>(() => Filters.GetFirstX12Segments(new X12Data(), null));
        }

        [Fact]
        public void GivenAnX12Data_WhenGetX12SegmentLists_CorrectResultShouldBeReturned()
        {
            // Empty data returns empty
            Assert.Empty(Filters.GetX12SegmentLists(new X12Data(), "NM1"));

            // Empty segment ID returns empty
            Assert.Empty(Filters.GetX12SegmentLists(TestData, string.Empty));

            // Get segment lists for types with multiple occurrences
            var segments = Filters.GetX12SegmentLists(TestData, "HL|NM1|REF|DTP|EQ|ZZZ");
            Assert.Equal(3, segments["HL"].Count);
            Assert.Equal(3, segments["NM1"].Count);
            Assert.Equal(2, segments["REF"].Count);
            Assert.Equal(2, segments["DTP"].Count);
            Assert.Single(segments["EQ"]);
            Assert.True(!segments.ContainsKey("ZZZ"));

            // Null arguments should throw
            Assert.Throws<NullReferenceException>(() => Filters.GetX12SegmentLists(null, "NM1"));
            Assert.Throws<NullReferenceException>(() => Filters.GetX12SegmentLists(new X12Data(), null));
        }

        [Fact]
        public void GivenAnX12Data_WhenGetX12SegmentsByType_CorrectResultShouldBeReturned()
        {
            // Empty data returns empty list
            Assert.Empty(Filters.GetX12SegmentsByType(new X12Data(), "NM1"));

            // Get all NM1 segments
            var nm1Segments = Filters.GetX12SegmentsByType(TestData, "NM1");
            Assert.Equal(3, nm1Segments.Count);
            Assert.Equal("PR", ((X12Element)nm1Segments[0].Elements[1]).Value);
            Assert.Equal("1P", ((X12Element)nm1Segments[1].Elements[1]).Value);
            Assert.Equal("IL", ((X12Element)nm1Segments[2].Elements[1]).Value);

            // Get all HL segments
            var hlSegments = Filters.GetX12SegmentsByType(TestData, "HL");
            Assert.Equal(3, hlSegments.Count);

            // Get all DTP segments
            var dtpSegments = Filters.GetX12SegmentsByType(TestData, "DTP");
            Assert.Equal(2, dtpSegments.Count);

            // Non-existent segment type returns empty
            Assert.Empty(Filters.GetX12SegmentsByType(TestData, "ZZZ"));

            // Case-insensitive matching
            var hlLower = Filters.GetX12SegmentsByType(TestData, "hl");
            Assert.Equal(3, hlLower.Count);

            // Null X12Data should throw
            Assert.Throws<NullReferenceException>(() => Filters.GetX12SegmentsByType(null, "NM1"));
        }

        [Fact]
        public void GivenAnX12Data_WhenGetX12RelatedSegmentList_CorrectResultShouldBeReturned()
        {
            // Empty data returns empty
            Assert.Empty(Filters.GetX12RelatedSegmentList(new X12Data(), null, null));

            // Get REF segments related to the second NM1 segment (1P - provider)
            var nm1Segments = Filters.GetX12SegmentsByType(TestData, "NM1");
            var providerNm1 = nm1Segments[1]; // 1P segment

            var refSegments = Filters.GetX12RelatedSegmentList(TestData, providerNm1, "REF");
            Assert.True(refSegments.ContainsKey("REF"));
            Assert.Single(refSegments["REF"]);
            Assert.Equal("TJ", ((X12Element)refSegments["REF"][0].Elements[1]).Value);

            // Get DTP segments related to the subscriber NM1 segment (IL)
            var subscriberNm1 = nm1Segments[2]; // IL segment
            var dtpSegments = Filters.GetX12RelatedSegmentList(TestData, subscriberNm1, "DTP");
            Assert.True(dtpSegments.ContainsKey("DTP"));
            Assert.Equal(2, dtpSegments["DTP"].Count);

            // Non-existent child segment returns empty
            var noResult = Filters.GetX12RelatedSegmentList(TestData, providerNm1, "ZZZ");
            Assert.True(!noResult.ContainsKey("ZZZ"));

            // Null X12Data should throw
            Assert.Throws<NullReferenceException>(() => Filters.GetX12RelatedSegmentList(null, null, null));
        }

        [Fact]
        public void GivenAnX12Data_WhenGetX12LoopId_CorrectResultShouldBeReturned()
        {
            // HL segments themselves should return the loop level code
            var hlSegments = Filters.GetX12SegmentsByType(TestData, "HL");

            // HL*1**20*1 -> Information Source (level 20)
            Assert.Equal("20", ((X12Element)hlSegments[0].Elements[3]).Value);

            // HL*2*1*21*1 -> Information Receiver (level 21)
            Assert.Equal("21", ((X12Element)hlSegments[1].Elements[3]).Value);

            // HL*3*2*22*0 -> Subscriber (level 22)
            Assert.Equal("22", ((X12Element)hlSegments[2].Elements[3]).Value);

            // NM1 after HL*1 should be in loop 20
            var nm1Segments = Filters.GetX12SegmentsByType(TestData, "NM1");
            var loopId1 = Filters.GetX12LoopId(TestData, nm1Segments[0]); // PR NM1 after HL*1
            Assert.Equal("20", loopId1);

            // NM1 after HL*2 should be in loop 21
            var loopId2 = Filters.GetX12LoopId(TestData, nm1Segments[1]); // 1P NM1 after HL*2
            Assert.Equal("21", loopId2);

            // NM1 after HL*3 should be in loop 22
            var loopId3 = Filters.GetX12LoopId(TestData, nm1Segments[2]); // IL NM1 after HL*3
            Assert.Equal("22", loopId3);

            // Segment before any HL should return empty
            var bhts = Filters.GetX12SegmentsByType(TestData, "BHT");
            var bhtLoopId = Filters.GetX12LoopId(TestData, bhts[0]);
            Assert.Equal(string.Empty, bhtLoopId);

            // Segment not in data should return empty
            var fakeSegment = new X12Segment("ZZZ", new SafeList<X12Element>());
            Assert.Equal(string.Empty, Filters.GetX12LoopId(TestData, fakeSegment));
        }

        [Fact]
        public void GivenAnX12Data_WhenHasX12Segments_CorrectResultShouldBeReturned()
        {
            // Empty data returns false
            Assert.False(Filters.HasX12Segments(new X12Data(), "NM1"));

            // Empty segment ID returns false
            Assert.False(Filters.HasX12Segments(TestData, string.Empty));

            // Single segment type present
            Assert.True(Filters.HasX12Segments(TestData, "NM1"));
            Assert.True(Filters.HasX12Segments(TestData, "HL"));

            // Multiple segment types all present
            Assert.True(Filters.HasX12Segments(TestData, "NM1|HL|REF|DTP"));

            // One segment type missing makes it false
            Assert.False(Filters.HasX12Segments(TestData, "NM1|HL|ZZZ"));

            // Non-existent segment type
            Assert.False(Filters.HasX12Segments(TestData, "ZZZ"));

            // Null should throw
            Assert.Throws<NullReferenceException>(() => Filters.HasX12Segments(null, "NM1"));
            Assert.Throws<NullReferenceException>(() => Filters.HasX12Segments(new X12Data(), null));
        }

        [Fact]
        public void GivenAnX12Data_WhenSplitX12ByHlLoop_CorrectResultShouldBeReturned()
        {
            // Split by HL loop
            var loops = Filters.SplitX12ByHlLoop(TestData);

            // Should have 4 parts: pre-HL, HL*1, HL*2, HL*3
            Assert.Equal(4, loops.Count);

            // First group: segments before HL (ISA, GS, ST, BHT)
            Assert.Equal(4, loops[0].Meta.Count);
            Assert.Equal("ISA", loops[0].Meta[0]);
            Assert.Equal("GS", loops[0].Meta[1]);
            Assert.Equal("ST", loops[0].Meta[2]);
            Assert.Equal("BHT", loops[0].Meta[3]);
            Assert.NotNull(loops[0].Delimiters);

            // Second group: HL*1 loop (HL, NM1)
            Assert.Equal(2, loops[1].Meta.Count);
            Assert.Equal("HL", loops[1].Meta[0]);
            Assert.Equal("NM1", loops[1].Meta[1]);
            Assert.NotNull(loops[1].Delimiters);

            // Third group: HL*2 loop (HL, NM1, REF)
            Assert.Equal(3, loops[2].Meta.Count);
            Assert.Equal("HL", loops[2].Meta[0]);
            Assert.Equal("NM1", loops[2].Meta[1]);
            Assert.Equal("REF", loops[2].Meta[2]);

            // Fourth group: HL*3 loop (HL, NM1, REF, DTP, DTP, EQ, SE, GE, IEA)
            Assert.Equal(9, loops[3].Meta.Count);
            Assert.Equal("HL", loops[3].Meta[0]);
            Assert.Equal("NM1", loops[3].Meta[1]);
            Assert.Equal("REF", loops[3].Meta[2]);
            Assert.Equal("DTP", loops[3].Meta[3]);
            Assert.Equal("DTP", loops[3].Meta[4]);
            Assert.Equal("EQ", loops[3].Meta[5]);

            // Data and Meta counts should match in each loop
            foreach (var loop in loops)
            {
                Assert.Equal(loop.Meta.Count, loop.Data.Count);
            }

            // Empty data should return empty list
            var emptyResult = Filters.SplitX12ByHlLoop(new X12Data());
            Assert.Empty(emptyResult);
        }

        [Fact]
        public void GivenAnX12Data_WhenSplitByHlLoop_DelimitersShouldBePreserved()
        {
            var loops = Filters.SplitX12ByHlLoop(TestData);

            // All loops should have the same delimiters as the original
            foreach (var loop in loops)
            {
                Assert.Equal(TestData.Delimiters.ElementSeparator, loop.Delimiters.ElementSeparator);
                Assert.Equal(TestData.Delimiters.SubElementSeparator, loop.Delimiters.SubElementSeparator);
                Assert.Equal(TestData.Delimiters.SegmentTerminator, loop.Delimiters.SegmentTerminator);
            }
        }

        [Fact]
        public void GivenNoHlSegments_WhenSplitX12ByHlLoop_SingleGroupReturned()
        {
            // Create data with no HL segments
            var noHlData = new X12Data
            {
                Delimiters = new X12Delimiters(),
            };
            noHlData.Meta.Add("ISA");
            noHlData.Data.Add(new X12Segment("ISA", new SafeList<X12Element>()));
            noHlData.Meta.Add("GS");
            noHlData.Data.Add(new X12Segment("GS", new SafeList<X12Element>()));
            noHlData.Meta.Add("ST");
            noHlData.Data.Add(new X12Segment("ST", new SafeList<X12Element>()));

            var result = Filters.SplitX12ByHlLoop(noHlData);
            Assert.Single(result);
            Assert.Equal(3, result[0].Meta.Count);
        }

        private static X12Data LoadTestData()
        {
            var parser = new X12DataParser();
            var data = parser.Parse(TestDataContent);
            return data as X12Data;
        }
    }
}
