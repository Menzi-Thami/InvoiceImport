using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CSVFile;
using InvoiceImporter.Application;

namespace InvoiceImporter.Infrastructure
{
    public class CsvReader : ICsvReader
    {
        public List<string[]> ReadCsv(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("A CSV file path is required.", nameof(filePath));
            }

            try
            {
                var result = new List<string[]>();

                var settings = new CSVSettings()
                {
                    FieldDelimiter = ',',
                    TextQualifier = '"',
                    ForceQualifiers = true,
                    // The library reads the header itself and yields data rows only; this is
                    // its default, set here so callers don't skip the header a second time.
                    HeaderRowIncluded = true,
                    LineSeparator = "\n"
                };

                // CSVFile splits rows on exactly one LineSeparator (default "\r\n"), so an LF file
                // ran its rows together and a "\n" separator left a stray '\r' on CRLF rows.
                // Normalising CRLF and lone CR to LF first accepts Windows, Unix and mixed files.
                // String.ReplaceLineEndings is not used: it also splits on U+2028, NEL and form feed.
                var text = File.ReadAllText(filePath, Encoding.UTF8)
                    .Replace("\r\n", "\n", StringComparison.Ordinal)
                    .Replace('\r', '\n');

                using (var cr = CSVReader.FromString(text, settings))
                {
                    foreach (string[] line in cr)
                    {
                        result.Add(line);
                    }
                }

                return result;
            }
            catch (Exception ex)
            {
                throw new CsvReadException($"Error reading CSV file '{filePath}'.", ex);
            }
        }
    }
}
