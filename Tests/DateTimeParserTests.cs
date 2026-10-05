using System;
using InvoiceImporter.Domain.Services;
using Shouldly;
using Xunit;

namespace InvoiceImporter.Tests
{
    public class DateTimeParserTests
    {
        private readonly DateTimeParser _parser = new();

        [Fact]
        public void ParseDateTime_ParsesUkFormat()
        {
            var result = _parser.ParseDateTime("07/04/2024 14:30");

            result.ShouldBe(new DateTime(2024, 4, 7, 14, 30, 0));
        }

        [Fact]
        public void ParseDateTime_DoesNotFallBackToUsFormat()
        {
            // Previously accepted as 13 December via an MM/dd fallback, while 04/03 in the
            // same US file was silently read as 4 March. One format per file, so it throws.
            Should.Throw<ArgumentException>(() => _parser.ParseDateTime("12/13/2024 09:05"));
        }

        [Fact]
        public void ParseDateTime_DefaultFormatIsUk()
        {
            _parser.ParseDateTime("04/03/2024 10:00").ShouldBe(new DateTime(2024, 3, 4, 10, 0, 0));
        }

        [Fact]
        public void ParseDateTime_UsFormat_ReadsAmbiguousDateAsUs()
        {
            var parser = new DateTimeParser(DateTimeParser.UsFormat);

            parser.ParseDateTime("04/03/2024 10:00").ShouldBe(new DateTime(2024, 4, 3, 10, 0, 0));
            parser.ParseDateTime("12/13/2024 09:05").ShouldBe(new DateTime(2024, 12, 13, 9, 5, 0));
            Should.Throw<ArgumentException>(() => parser.ParseDateTime("13/12/2024 09:05"));
        }

        [Theory]
        [InlineData("not-a-date")]
        [InlineData("2024-04-07")]
        [InlineData("")]
        public void ParseDateTime_ThrowsOnUnparseableInput(string value)
        {
            Should.Throw<ArgumentException>(() => _parser.ParseDateTime(value));
        }
    }
}
