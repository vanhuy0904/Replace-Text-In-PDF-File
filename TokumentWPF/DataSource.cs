using ExcelDataReader;
using System.Collections.Generic;
using System.Data;
using System.IO;

namespace Tokument
{
    class DataSource
    {
        public bool LoadDataSource(string dataSourceName)
        {
            if (File.Exists(dataSourceName) != true)
                return false;

            string extension = Path.GetExtension(dataSourceName).ToLower();
            File.SetAttributes(dataSourceName, FileAttributes.Normal);
            try
            {
                using (var stream = File.Open(dataSourceName, FileMode.Open, FileAccess.Read))
                {
                    IExcelDataReader reader;
                    if (extension.Equals(".csv"))
                    {
                        reader = ExcelReaderFactory.CreateCsvReader(stream);
                    }
                    else
                    {
                        reader = ExcelReaderFactory.CreateReader(stream);
                    }

                    DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (data) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true,
                        }
                    });

                    // clear data table
                    ResultTable.Clear();
                    ResultTable = result.Tables[0]; // get first worksheet

                    // fill column names
                    ColumnNames.Clear();
                    foreach (DataColumn col in ResultTable.Columns)
                    {
                        ColumnNames.Add(col.ToString());
                    }

                    reader.Close();
                    return true;
                }
            }
            catch (System.Exception ex)
            {
                System.Console.WriteLine(ex.ToString());
                return false;
            }
        }

        // data table
        public DataTable ResultTable { get; set; } = new DataTable();

        // table column names
        public List<string> ColumnNames { get; set; } = new List<string>();
    }
}
