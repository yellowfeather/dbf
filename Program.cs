using System;
using System.IO;
using System.Linq;
using System.Text;
using CommandLine;
using DbfDataReader;

namespace Dbf
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            return Parser.Default.ParseArguments<Options>(args)
                .MapResult(RunAndReturnExitCode, _ => 1);
        }

        public static int RunAndReturnExitCode(Options options)
        {
            if (options.Csv)
                PrintCsv(options);
            else if (options.Schema)
                PrintSchema(options);
            else
                PrintSummaryInfo(options);

            return 0;
        }

        private static void PrintSummaryInfo(Options options)
        {
            var encoding = GetEncoding();
            using (var dbfTable = new DbfTable(options.Filename, encoding))
            {
                var header = dbfTable.Header;

                Console.WriteLine($"Filename: {options.Filename}");
                Console.WriteLine($"Type: {header.VersionDescription}");
                Console.WriteLine($"Memo File: {dbfTable.Memo != null}");
                Console.WriteLine($"Records: {header.RecordCount}");
                Console.WriteLine();
                Console.WriteLine("Fields:");
                Console.WriteLine("Name             Type       Length     Decimal");
                Console.WriteLine("------------------------------------------------------------------------------");

                foreach (var dbfColumn in dbfTable.Columns)
                {
                    var name = dbfColumn.ColumnName;
                    var columnType = ((char) dbfColumn.ColumnType).ToString();
                    var length = dbfColumn.Length.ToString();
                    var decimalCount = dbfColumn.DecimalCount;
                    Console.WriteLine(
                        $"{name.PadRight(16)} {columnType.PadRight(10)} {length.PadRight(10)} {decimalCount}");
                }
            }
        }

        private static void PrintCsv(Options options)
        {
            var encoding = GetEncoding();
            using (var dbfTable = new DbfTable(options.Filename, encoding))
            {
                var columnNames = string.Join(",", dbfTable.Columns.Select(c => c.ColumnName));
                if (!options.SkipDeleted) columnNames += ",Deleted";

                Console.WriteLine(columnNames);

                var dbfRecord = new DbfRecord(dbfTable);

                while (dbfTable.Read(dbfRecord))
                {
                    if (options.SkipDeleted && dbfRecord.IsDeleted) continue;

                    var values = string.Join(",", dbfRecord.Values.Select(v => EscapeValue(v)));
                    if (!options.SkipDeleted) values += $",{dbfRecord.IsDeleted}";

                    Console.WriteLine(values);
                }
            }
        }

        private static void PrintSchema(Options options)
        {
            var encoding = GetEncoding();
            using (var dbfTable = new DbfTable(options.Filename, encoding))
            {
                var tableName = Path.GetFileNameWithoutExtension(options.Filename);
                Console.WriteLine($"CREATE TABLE [dbo].[{tableName}]");
                Console.WriteLine("(");

                foreach (var dbfColumn in dbfTable.Columns)
                {
                    var columnSchema = ColumnSchema(dbfColumn);
                    Console.Write($"  {columnSchema}");

                    if (dbfColumn.ColumnOrdinal < dbfTable.Columns.Count ||
                        !options.SkipDeleted)
                        Console.Write(",");
                    Console.WriteLine();
                }

                if (!options.SkipDeleted) Console.WriteLine("  [deleted] [bit] NULL DEFAULT ((0))");

                Console.WriteLine(")");
            }
        }

        private static Encoding GetEncoding()
        {
            return Encoding.GetEncoding(1252);
        }

        private static string ColumnSchema(DbfColumn dbfColumn)
        {
            var schema = string.Empty;
            switch (dbfColumn.ColumnType)
            {
                case DbfColumnType.Boolean:
                    schema = $"[{dbfColumn.ColumnName}] [bit] NULL DEFAULT ((0))";
                    break;
                case DbfColumnType.Character:
                    schema = $"[{dbfColumn.ColumnName}] [nvarchar]({dbfColumn.Length})  NULL";
                    break;
                case DbfColumnType.Currency:
                    schema =
                        $"[{dbfColumn.ColumnName}] [decimal]({dbfColumn.Length + dbfColumn.DecimalCount},{dbfColumn.DecimalCount}) NULL DEFAULT (NULL)";
                    break;
                case DbfColumnType.Date:
                    schema = $"[{dbfColumn.ColumnName}] [date] NULL DEFAULT (NULL)";
                    break;
                case DbfColumnType.DateTime:
                    schema = $"[{dbfColumn.ColumnName}] [datetime] NULL DEFAULT (NULL)";
                    break;
                case DbfColumnType.Double:
                    schema =
                        $"[{dbfColumn.ColumnName}] [decimal]({dbfColumn.Length + dbfColumn.DecimalCount},{dbfColumn.DecimalCount}) NULL DEFAULT (NULL)";
                    break;
                case DbfColumnType.Float:
                    schema =
                        $"[{dbfColumn.ColumnName}] [decimal]({dbfColumn.Length + dbfColumn.DecimalCount},{dbfColumn.DecimalCount}) NULL DEFAULT (NULL)";
                    break;
                case DbfColumnType.General:
                    schema = $"[{dbfColumn.ColumnName}] [nvarchar]({dbfColumn.Length})  NULL";
                    break;
                case DbfColumnType.Memo:
                    schema = $"[{dbfColumn.ColumnName}] [ntext]  NULL";
                    break;
                case DbfColumnType.Number:
                    if (dbfColumn.DecimalCount > 0)
                        schema =
                            $"[{dbfColumn.ColumnName}] [decimal]({dbfColumn.Length + dbfColumn.DecimalCount},{dbfColumn.DecimalCount}) NULL DEFAULT (NULL)";
                    else
                        schema = $"[{dbfColumn.ColumnName}] [int] NULL DEFAULT (NULL)";
                    break;
                case DbfColumnType.SignedLong:
                    schema = $"[{dbfColumn.ColumnName}] [int] NULL DEFAULT (NULL)";
                    break;
            }

            return schema;
        }

        private static string EscapeValue(IDbfValue dbfValue)
        {
            var value = dbfValue.ToString();
            if (dbfValue is DbfValueString)
                if (value.Contains(","))
                    value = $"\"{value}\"";

            return value;
        }
    }
}