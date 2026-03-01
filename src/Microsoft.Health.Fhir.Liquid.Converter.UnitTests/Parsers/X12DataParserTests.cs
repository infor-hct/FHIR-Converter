// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System.Collections.Generic;
using Microsoft.Health.Fhir.Liquid.Converter.DotLiquids;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Models.X12;
using Microsoft.Health.Fhir.Liquid.Converter.Parsers;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests.Parsers
{
    public class X12DataParserTests
    {
        private readonly IDataParser _parser = new X12DataParser();

        public static IEnumerable<object[]> GetNullOrEmptyX12Message()
        {
            yield return new object[] { null };
            yield return new object[] { string.Empty };
            yield return new object[] { " " };
            yield return new object[] { "\n" };
        }

        [Theory]
        [MemberData(nameof(GetNullOrEmptyX12Message))]
        public void GivenNullOrEmptyX12Message_WhenParse_ExceptionShouldBeThrown(string input)
        {
            var exception = Assert.Throws<DataParseException>(() => _parser.Parse(input));
            Assert.Equal(FhirConverterErrorCode.NullOrWhiteSpaceInput, exception.FhirConverterErrorCode);
        }

        [Fact]
        public void GivenInvalidX12Message_WhenParse_ExceptionShouldBeThrown()
        {
            var input = "INVALID MESSAGE WITHOUT ISA HEADER";
            var exception = Assert.Throws<DataParseException>(() => _parser.Parse(input));
            Assert.Equal(FhirConverterErrorCode.InvalidX12Message, exception.FhirConverterErrorCode);
        }

        [Fact]
        public void GivenTooShortX12Message_WhenParse_ExceptionShouldBeThrown()
        {
            var input = "ISA*00*short";
            var exception = Assert.Throws<DataParseException>(() => _parser.Parse(input));
            Assert.Equal(FhirConverterErrorCode.InvalidX12Message, exception.FhirConverterErrorCode);
        }

        [Fact]
        public void GivenValidX12Message_WhenParse_CorrectX12DataShouldBeReturned()
        {
            var input = "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *230101*1200*^*00501*000000001*0*P*:~" +
                        "GS*HS*SENDER*RECEIVER*20230101*1200*1*X*005010X279A1~" +
                        "ST*270*0001*005010X279A1~" +
                        "BHT*0022*13*REF123*20230101*1200~" +
                        "HL*1**20*1~" +
                        "NM1*PR*2*INSURANCE COMPANY*****PI*12345~" +
                        "SE*6*0001~" +
                        "GE*1*1~" +
                        "IEA*1*000000001~";

            var x12Data = _parser.Parse(input) as X12Data;
            Assert.NotNull(x12Data);
            Assert.Equal(9, x12Data.Meta.Count);
            Assert.Equal("ISA", x12Data.Meta[0]);
            Assert.Equal("GS", x12Data.Meta[1]);
            Assert.Equal("ST", x12Data.Meta[2]);
            Assert.Equal("BHT", x12Data.Meta[3]);
            Assert.Equal("HL", x12Data.Meta[4]);
            Assert.Equal("NM1", x12Data.Meta[5]);
            Assert.Equal("SE", x12Data.Meta[6]);
            Assert.Equal("GE", x12Data.Meta[7]);
            Assert.Equal("IEA", x12Data.Meta[8]);
            Assert.Equal(9, x12Data.Data.Count);

            // Verify delimiters are parsed correctly
            Assert.Equal('*', x12Data.Delimiters.ElementSeparator);
            Assert.Equal(':', x12Data.Delimiters.SubElementSeparator);
            Assert.Equal('~', x12Data.Delimiters.SegmentTerminator);

            // Verify ISA segment
            var isaSegment = x12Data.Data[0];
            Assert.Equal("ISA", isaSegment.SegmentId);
            Assert.True(isaSegment.Elements.Count > 1);

            // Verify BHT segment elements
            var bhtSegment = x12Data.Data[3];
            Assert.Equal("BHT", bhtSegment.SegmentId);
            Assert.Equal("BHT", ((X12Element)bhtSegment.Elements[0]).Value);
            Assert.Equal("0022", ((X12Element)bhtSegment.Elements[1]).Value);
            Assert.Equal("13", ((X12Element)bhtSegment.Elements[2]).Value);
            Assert.Equal("REF123", ((X12Element)bhtSegment.Elements[3]).Value);

            // Verify NM1 segment elements
            var nm1Segment = x12Data.Data[5];
            Assert.Equal("NM1", nm1Segment.SegmentId);
            Assert.Equal("PR", ((X12Element)nm1Segment.Elements[1]).Value);
            Assert.Equal("2", ((X12Element)nm1Segment.Elements[2]).Value);
            Assert.Equal("INSURANCE COMPANY", ((X12Element)nm1Segment.Elements[3]).Value);
            Assert.Equal("PI", ((X12Element)nm1Segment.Elements[8]).Value);
            Assert.Equal("12345", ((X12Element)nm1Segment.Elements[9]).Value);
        }

        [Fact]
        public void GivenX12MessageWithSubElements_WhenParse_SubElementsShouldBeParsed()
        {
            var input = "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *230101*1200*^*00501*000000001*0*P*:~" +
                        "GS*HP*SENDER*RECEIVER*20230101*1200*1*X*005010X217~" +
                        "ST*277*0001*005010X212~" +
                        "STC*A1:19:PR*20230101*WQ*500~" +
                        "SE*4*0001~" +
                        "GE*1*1~" +
                        "IEA*1*000000001~";

            var x12Data = _parser.Parse(input) as X12Data;
            Assert.NotNull(x12Data);

            // Verify STC segment with composite element (sub-elements separated by ':')
            var stcSegment = x12Data.Data[3];
            Assert.Equal("STC", stcSegment.SegmentId);
            var stcElement1 = (X12Element)stcSegment.Elements[1];
            Assert.Equal("A1:19:PR", stcElement1.Value);
            // SubElements has null at index 0, then actual values at 1, 2, 3...
            Assert.Equal("A1", (string)stcElement1.SubElements[1]);
            Assert.Equal("19", (string)stcElement1.SubElements[2]);
            Assert.Equal("PR", (string)stcElement1.SubElements[3]);
        }

        [Fact]
        public void GivenX12MessageWithEmptyElements_WhenParse_EmptyElementsShouldBePreserved()
        {
            var input = "ISA*00*          *00*          *ZZ*SENDER         *ZZ*RECEIVER       *230101*1200*^*00501*000000001*0*P*:~" +
                        "GS*HS*SENDER*RECEIVER*20230101*1200*1*X*005010X279A1~" +
                        "ST*270*0001*005010X279A1~" +
                        "NM1*IL*1*SMITH*JANE****MI*ABC123~" +
                        "SE*4*0001~" +
                        "GE*1*1~" +
                        "IEA*1*000000001~";

            var x12Data = _parser.Parse(input) as X12Data;
            Assert.NotNull(x12Data);

            // NM1 with empty elements (positions 5, 6, 7 are empty)
            var nm1Segment = x12Data.Data[3];
            Assert.Equal("NM1", nm1Segment.SegmentId);
            Assert.Equal("IL", ((X12Element)nm1Segment.Elements[1]).Value);
            Assert.Equal("1", ((X12Element)nm1Segment.Elements[2]).Value);
            Assert.Equal("SMITH", ((X12Element)nm1Segment.Elements[3]).Value);
            Assert.Equal("JANE", ((X12Element)nm1Segment.Elements[4]).Value);
            Assert.Equal("MI", ((X12Element)nm1Segment.Elements[8]).Value);
            Assert.Equal("ABC123", ((X12Element)nm1Segment.Elements[9]).Value);
        }

        [Fact]
        public void GivenX12Element_WhenAccessProperties_CorrectValuesShouldBeReturned()
        {
            var element = new X12Element("TestValue", new SafeList<string> { null, "sub1", "sub2" });

            Assert.Equal("TestValue", element["Value"]);
            Assert.NotNull(element["SubElements"]);
            Assert.Equal("sub1", element[1]);
            Assert.Equal("sub2", element[2]);
            Assert.Throws<Exceptions.RenderException>(() => element["nonexistent"]);
        }

        [Fact]
        public void GivenX12Segment_WhenAccessProperties_CorrectValuesShouldBeReturned()
        {
            var elements = new SafeList<X12Element>();
            elements.Add(new X12Element("NM1", new SafeList<string> { null, "NM1" }));
            elements.Add(new X12Element("PR", new SafeList<string> { null, "PR" }));

            var segment = new X12Segment("NM1", "NM1*PR", elements);

            Assert.Equal("NM1*PR", segment["Value"]);
            Assert.Equal("NM1", segment["SegmentId"]);
            Assert.NotNull(segment["Elements"]);
            Assert.Equal("NM1", ((X12Element)segment[0]).Value);
            Assert.Equal("PR", ((X12Element)segment[1]).Value);
            Assert.Throws<Exceptions.RenderException>(() => segment["nonexistent"]);
        }
    }
}
