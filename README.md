# Invoice Import

A .NET 10 console tool that reads a CSV of invoices and imports it into SQL Server with
EF Core. Small on purpose — it exists to be a clean example of layering a console app
without ceremony.

[![CI](https://github.com/Menzi-Thami/InvoiceImport/actions/workflows/ci.yml/badge.svg)](https://github.com/Menzi-Thami/InvoiceImport/actions/workflows/ci.yml)

## What it does

Prompts for a CSV path, parses each row into an invoice header plus its lines, and writes
them to the database. Paths copied from Explorer with "Copy as path" work as-is — the
surrounding quotes are stripped for you.

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
makes the 16 tests possible without a database.

## Running it

```bash
dotnet run
```

The connection string lives in `appsettings.json` under `ConnectionStrings:InvoiceDb` and
defaults to `localhost` with integrated security. Apply the schema first:

```bash
dotnet ef database update
```

## Tests

```bash
dotnet test InvoiceImport.sln
```

16 unit tests covering CSV parsing, date parsing, and the import flow.

## Licence

[MIT](LICENSE).
