# Invoice Import

A .NET 10 console tool that reads a CSV of invoices and imports it into SQL Server with
EF Core. Small on purpose — it exists to be a clean example of layering a console app
without ceremony.

[![CI](https://github.com/Menzi-Thami/InvoiceImport/actions/workflows/ci.yml/badge.svg)](https://github.com/Menzi-Thami/InvoiceImport/actions/workflows/ci.yml)

## What it does

Prompts for a CSV path, parses each row into an invoice header plus its lines, and writes
them to the database. Paths copied from Explorer with "Copy as path" work as-is — the
surrounding quotes are stripped for you.

Amounts (total, quantity, unit price) must use a decimal point and no thousands
separator, e.g. `1234.56` or `-2.5`. A blank amount is stored as empty (NULL). Any
other value, such as `1,5`, `1.234,56` or `n/a`, stops the import with an error naming
the invoice and column, and nothing from the file is saved.

Invoice dates must be UK format, `dd/MM/yyyy HH:mm` (e.g. `07/04/2024 14:30` is 7 April).
A date in any other format, including US `MM/dd/yyyy`, stops the import. The tool used to
fall back to US format per value, which silently read `04/03/2024` in a US file as
4 March; a file from a US source needs `new DateTimeParser(DateTimeParser.UsFormat)` in
`Program.cs`.

## How it's put together

```
Program.cs            composition root — wires everything by hand, no container
Application/          DataImporter (the use case) + the ICsvReader port
Domain/               InvoiceHeader, InvoiceLine, InvoiceFactory, DateTimeParser
Infrastructure/       CsvReader, InvoiceRepository, InvoiceDbContext, EF configurations
Tests/                unit tests
```

The domain owns the parsing rules that are easy to get wrong — date formats and the
header/line relationship — and `DataImporter` depends only on interfaces, which is what
makes the tests possible without a database.

## Running it

```bash
dotnet run
```

The connection string lives in `appsettings.json` under `ConnectionStrings:InvoiceDb` and
defaults to `localhost` with integrated security. Apply the schema first:

```bash
dotnet ef database update
```

Each invoice number is imported once. Repeats inside a file are skipped with a warning,
invoices already in the database are skipped, and a unique index on `InvoiceNumber`
stops two runs started at the same time from both inserting the same invoice (the
losing run saves nothing and exits with code 1). The whole file is saved in one
transaction, so Ctrl+C during an import cancels it cleanly with nothing written.

### Upgrading the schema: `UniqueInvoiceNumber`

This migration changes `InvoiceHeader.InvoiceNumber` from `nvarchar(max)` to
`nvarchar(50)` and adds a unique index. Older versions of the tool could insert the same
invoice number twice, so the migration checks first and stops, changing nothing, if
the table has duplicate numbers or numbers longer than 50 characters. Find them with:

```sql
SELECT InvoiceNumber, COUNT(*) FROM InvoiceHeader GROUP BY InvoiceNumber HAVING COUNT(*) > 1;
SELECT InvoiceId, InvoiceNumber FROM InvoiceHeader WHERE LEN(InvoiceNumber) > 50;
```

Decide which copy of each duplicate to keep (deleting a header also deletes its lines),
then run `dotnet ef database update` again. The comparison uses the database collation,
so with the default case-insensitive collation `inv-001` and `INV-001` count as the same
invoice.

## Tests

```bash
dotnet test InvoiceImport.sln
```

Unit tests covering CSV parsing, date parsing, and the import flow.

## Licence

[MIT](LICENSE).
