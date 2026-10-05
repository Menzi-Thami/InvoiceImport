using Shouldly;
using Xunit;

namespace InvoiceImporter.Tests
{
    public class ProgramTests
    {
        [Theory]
        [InlineData(@"""\\srv\share\a.csv""", @"\\srv\share\a.csv")]
        [InlineData(@"""C:\data\invoices.csv""", @"C:\data\invoices.csv")]
        [InlineData(@"  C:\data\invoices.csv  ", @"C:\data\invoices.csv")]
        [InlineData(@"C:\data\invoices.csv", @"C:\data\invoices.csv")]
        public void NormalisePath_StripsWhitespaceAndCopyAsPathQuotes_KeepingSeparators(string input, string expected)
        {
            Program.NormalisePath(input).ShouldBe(expected);
        }
    }
}
