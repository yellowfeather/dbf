using CommandLine;
using DbfDataReader;
using System;
using System.Linq;

namespace Dbf
{
    public class Program
    {
        public static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<Options>(args)
                .MapResult(
                options => RunAndReturnExitCode(options),
                _ => 1);
        }

        public static int RunAndReturnExitCode(Options options)
        {
            if (options.Csv)
            {
                PrintCsv(options);
            }
            else
            {
                PrintSummaryInfo(options);
            }

            return 0;
        }

        private static void PrintSummaryInfo(Options options)
        {
            using (var dbfTable = new DbfTable(options.Filename))
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
                    var name = dbfColumn.Name;
                    var columnType = ((char)dbfColumn.ColumnType).ToString();
                    var length = dbfColumn.Length.ToString();
                    var decimalCount = dbfColumn.DecimalCount;
                    Console.WriteLine($"{name.PadRight(16)} {columnType.PadRight(10)} {length.PadRight(10)} {decimalCount}");
                }
            }
        }

        private static void PrintCsv(Options options)
        {
            using (var dbfTable = new DbfTable(options.Filename))
            {
                var columnNames = string.Join(",", dbfTable.Columns.Select(c => c.Name));
                if (!options.SkipDeleted)
                {
                    columnNames += ",Deleted";
                }

                Console.WriteLine(columnNames);
                
                var dbfRecord = new DbfRecord(dbfTable);

                while (dbfTable.Read(dbfRecord))
                {
                    if (options.SkipDeleted && dbfRecord.IsDeleted)
                    {
                        continue;
                    }

                    var values = string.Join(",", dbfRecord.Values.Select(v => EscapeValue(v)));
                    if (!options.SkipDeleted)
                    {
                        values += $",{dbfRecord.IsDeleted}";
                    }

                    Console.WriteLine(values);
                }
            }
        }

        private static string EscapeValue(IDbfValue dbfValue)
        {
            var value = dbfValue.ToString();
            if (dbfValue is DbfValueString)
            {
                if (value.Contains(","))
                {
                    value = $"\"{value}\"";
                }
            }

            return value;
        }
    }
}
