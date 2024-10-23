using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Tokument
{
    class Utils
    {
        public static string Base64Encode(string p_strPlainText)
        {
            var w_strPlainTextBytes = System.Text.Encoding.UTF8.GetBytes(p_strPlainText);
            return System.Convert.ToBase64String(w_strPlainTextBytes);
        }

        public static string Base64Decode(string p_strBase64EncodedData)
        {
            var w_strBase64EncodedBytes = System.Convert.FromBase64String(p_strBase64EncodedData);
            return System.Text.Encoding.UTF8.GetString(w_strBase64EncodedBytes);
        }

        public static void Compress(string[] fileNames, string resultantFileName)
        {
            List<byte> bytesToWrite = new List<byte>();

            //add metadata about the number of files
            int filesLength = fileNames.Length;
            bytesToWrite.AddRange(BitConverter.GetBytes(filesLength));



            foreach (string fileName in fileNames)
            {
                bytesToWrite.AddRange(BitConverter.GetBytes(Path.GetFileNameWithoutExtension(fileName).Length));
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(Path.GetFileNameWithoutExtension(fileName));
                bytesToWrite.AddRange(buffer);
            }



            List<byte[]> files = new List<byte[]>();
            foreach (string fileName in fileNames)
            {
                byte[] bytes;
                try
                {
                    bytes = File.ReadAllBytes(fileName);

                    //add metadata about the size of each file
                    bytesToWrite.AddRange(BitConverter.GetBytes(bytes.Length));
                    files.Add(bytes);
                }
                catch(Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    return;
                }
            }

            foreach (byte[] bytes in files)
            {
                //write the actual files itself
                bytesToWrite.AddRange(bytes);
            }
            File.WriteAllBytes(resultantFileName, bytesToWrite.ToArray());
        }

        //public static void Decompress(string fileName)
        //{
        //    List<byte> bytes = new List<byte>(File.ReadAllBytes(fileName));
        //    //this int represents the number of files in the byte array
        //    int filesLength = BitConverter.ToInt32(bytes.ToArray(), 0);


        //    int startBytes = 4;
        //    List<string> fileNames = new List<string>();
        //    for (int i = 0; i < filesLength; i++)
        //    {
        //        int fileNameSize = BitConverter.ToInt32(bytes.ToArray(), startBytes);
        //        startBytes += 4;

        //        string indivFileName = System.Text.Encoding.UTF8.GetString(bytes.ToArray(), startBytes, fileNameSize);
        //        fileNames.Add(DBManager.Instance.m_strDatabasePath + indivFileName);
        //        startBytes += fileNameSize;
        //    }




        //    List<int> sizes = new List<int>();
        //    //get the size of each file
        //    for (int i = 0; i < filesLength; i++)
        //    {
        //        //the first 4 bytes represent the number of files
        //        //then each succeding int represents the size of each file
        //        int size = BitConverter.ToInt32(bytes.ToArray(), startBytes);
        //        sizes.Add(size);

        //        startBytes += 4;
        //    }

        //    //now read all the files
        //    for (int i = 0; i < filesLength; i++)
        //    {
        //        File.WriteAllBytes(fileNames[i] + ".db", bytes.GetRange(startBytes, sizes[i]).ToArray());
        //        startBytes += sizes[i];
        //    }
        //}

        //public static void ListViewToCSV(ListView listView, string filePath, bool includeHidden)
        //{
        //    //make header string
        //    StringBuilder result = new StringBuilder();
        //    WriteCSVRow(result, listView.Columns.Count, i => includeHidden || listView.Columns[i].Width > 0, i => listView.Columns[i].Text);

        //    //export data rows
        //    foreach (ListViewItem listItem in listView.Items)
        //        WriteCSVRow(result, listView.Columns.Count, i => includeHidden || listView.Columns[i].Width > 0, i => listItem.SubItems[i].Text);

        //    File.WriteAllText(filePath, result.ToString());
        //}

        //private static void WriteCSVRow(StringBuilder result, int itemsCount, Func<int, bool> isColumnNeeded, Func<int, string> columnValue)
        //{
        //    bool isFirstTime = true;
        //    for (int i = 0; i < itemsCount; i++)
        //    {
        //        if (!isColumnNeeded(i))
        //            continue;

        //        if (!isFirstTime)
        //            result.Append(",");
        //        isFirstTime = false;

        //        result.Append(String.Format("\"{0}\"", columnValue(i)));
        //    }
        //    result.AppendLine();
        //}

        //public static byte[] ReadAllBytes(string fileName)
        //{
        //    byte[] buffer = null;
        //    using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        //    {
        //        buffer = new byte[fs.Length];
        //        fs.Read(buffer, 0, (int)fs.Length);
        //    }
        //    return buffer;
        //}

        //public static DataTable GetTableColumNames(string p_strTableName)
        //{
        //    string w_strSql = string.Format("PRAGMA table_info({0})", p_strTableName);
        //    DataTable w_dtQueryResult = DBManager.Instance.ExecuteQuery(w_strSql);

        //    return w_dtQueryResult;
        //}

        //public static void SetTableColumns(ListView p_ctrlListView, DataTable p_dtColumns)
        //{
        //    var headerList = new List<string>();

        //    p_ctrlListView.Columns.Clear();

        //    foreach (DataRow record in p_dtColumns.Rows)
        //    {
        //        if(!record.ItemArray[1].ToString().Equals("ID") && !record.ItemArray[1].ToString().Equals("Deleted"))
        //            headerList.Add(record.ItemArray[1].ToString());
        //    }
        //    headerList.Insert(0, "No");
        //    headerList.ForEach(x => p_ctrlListView.Columns.Add(x));

        //    p_ctrlListView.Columns[0].Width = 50;
        //    for(int i = 1; i < p_ctrlListView.Columns.Count; i++)
        //        p_ctrlListView.Columns[i].Width = 150;
        //}

        //public static void ShowNotification(string p_strTitle, string p_strBody, int p_nDuration, string p_strSQLScript, string p_strDBVersion)
        //{
        //    int duration = p_nDuration;

        //    if (p_nDuration <= 0)
        //    {
        //        p_nDuration = -1;
        //    }

        //    var animationMethod = FormAnimator.AnimationMethod.Slide;
        //    var animationDirection = FormAnimator.AnimationDirection.Up;

        //    var toastNotification = new Notifications(p_strTitle, p_strBody, duration, animationMethod, animationDirection, p_strSQLScript, p_strDBVersion);
        //    PlayNotificationSound("normal");
        //    toastNotification.Show();
        //}

        private static void PlayNotificationSound(string sound)
        {
            var soundsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sounds");
            var soundFile = Path.Combine(soundsFolder, sound + ".wav");

            using (var player = new System.Media.SoundPlayer(soundFile))
            {
                player.Play();
            }
        }
    }
}
