using Microsoft.Office.Interop.Word;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;

namespace Tokument
{
    class DocProcessor : TemplateProcessor
    {
        public override void StartDetect(string templateName)
        {
            int founds = FindTags(templateName).Count;

            detector?.Invoke(this, "docx", founds);
        }

        public override void StartMerge(DataSource dataSource, string templateName, string outputFolder, string resultName, string resultExt)
        {
            logger?.Invoke(this, $"*************************************************************************");
            logger?.Invoke(this, $"Starting auto filling on {templateName}\n");

            int rowCount = dataSource.ResultTable.Rows.Count;
            int rowIndx = 0, tagIndex = 0;

            HashSet<string> tags = FindTags(templateName);

            // initialize progress bar
            int maxStep = rowCount * tags.Count;
            progresser?.Invoke(this, 0, maxStep);

            bool success = true;
            int replaces = 0;
            List<string> missingTags = new List<string>();
            //Creates new Word document instance for Word processing
            foreach (DataRow row in dataSource.ResultTable.Rows)
            {
                var templateExt = Path.GetExtension(templateName).ToLower();
                string resultFilePath;
                if(rowIndx == 0)
                    resultFilePath = string.Format("{0}/{1}.{2}", outputFolder, resultName, resultExt);
                else
                    resultFilePath = string.Format("{0}/{1}-{2}.{3}", outputFolder, resultName, rowIndx, resultExt);

                var wordApp = new Microsoft.Office.Interop.Word.Application();
                var doc = wordApp.Documents.Open(templateName, false, true);

                replaces = 0;
                tagIndex = 0;
                missingTags.Clear();
                foreach (string tag in tags)
                {
                    if (dataSource.ColumnNames.Contains(tag) != true)
                    {
                        logger?.Invoke(this, $"> {tag} not found.");
                        missingTags.Add(tag);
                    }
                    else
                    {
                        string value = row[tag].ToString();

                        ReplaceTag(doc, "{{" + tag + "}}", value);
                        replaces++;
                    }
                    tagIndex++;
                    // step progress bar
                    progresser?.Invoke(this, rowIndx * tags.Count + tagIndex, maxStep); 
                }
                logger?.Invoke(this, $"{replaces}/{tags.Count} replaced.");

                object oMissing = System.Reflection.Missing.Value;
                if (templateExt.Contains(resultExt))
                {
                    try
                    {
                        doc.SaveAs(resultFilePath);
                        doc.Close();
                    }
                    catch(Exception)
                    {
                        success = false;
                    }
                }
                else
                {
                    object saveOption = Microsoft.Office.Interop.Word.WdSaveOptions.wdDoNotSaveChanges;
                    object originalFormat = Microsoft.Office.Interop.Word.WdOriginalFormat.wdOriginalDocumentFormat;
                    object routeDocument = false;
                    try
                    {
                        doc.ExportAsFixedFormat(resultFilePath, WdExportFormat.wdExportFormatPDF, false, WdExportOptimizeFor.wdExportOptimizeForOnScreen,
                            WdExportRange.wdExportAllDocument, 1, 1, WdExportItem.wdExportDocumentContent, true, true,
                            WdExportCreateBookmarks.wdExportCreateHeadingBookmarks, true, true, false, ref oMissing);
                        doc.Close(ref saveOption, ref originalFormat, ref routeDocument);
                    }
                    catch (Exception)
                    {
                        success = false;
                    }
                }

                wordApp.Quit();
                if (success == true)
                {
                    logger?.Invoke(this, $"{resultFilePath} generated.\n");
                }
                else
                {
                    logger?.Invoke(this, $"Error: {resultFilePath} write protected.\n");
                    success = false;
                    break;
                }

                rowIndx++;
            }

            Thread.Sleep(1000);

            logger?.Invoke(this, $"Merging completed\n");
            if (success == true)
                successHandler?.Invoke(this, true, replaces, tags.Count, missingTags);
            else
                successHandler?.Invoke(this, false, 0, 0, null);
            progresser?.Invoke(this, 0, 0);
        }

        private HashSet<string> FindTags(string templateName)
        {
            var wordApp = new Microsoft.Office.Interop.Word.Application();
            var doc = wordApp.Documents.Open(templateName, false, true);

            List<string> data = new List<string>();
            foreach (Paragraph objParagraph in doc.Paragraphs)
                data.Add(objParagraph.Range.Text.Trim());

            object saveOption = Microsoft.Office.Interop.Word.WdSaveOptions.wdDoNotSaveChanges;
            object originalFormat = Microsoft.Office.Interop.Word.WdOriginalFormat.wdOriginalDocumentFormat;
            object routeDocument = false;
            doc.Close(ref saveOption, ref originalFormat, ref routeDocument);

            wordApp.Quit();

            if (doc != null)
                System.Runtime.InteropServices.Marshal.ReleaseComObject(doc);
            if (wordApp != null)
                System.Runtime.InteropServices.Marshal.ReleaseComObject(wordApp);
            doc = null;
            wordApp = null;
            
            GC.Collect(); // force final cleanup!

            HashSet<string> foundTags = new HashSet<string>();
            foreach (string line in data)
            {
                var matches = System.Text.RegularExpressions.Regex.Matches(line, @"{{(.*?)}}");
                foreach(Match match in matches)
                {
                    string tag = match.Value;
                    if (tag.Length < 4) continue;
                    tag = tag.Substring(2, tag.Length - 4);
                    foundTags.Add(tag);
                }
            }
            
            return foundTags;
        }

        private int ReplaceTag(Document doc, string tag, string value)
        {
            int replaces = 0;
            while (doc.Content.Find.Execute(FindText: tag,
                                        MatchCase: false,
                                        MatchWholeWord: false,
                                        MatchWildcards: false,
                                        MatchSoundsLike: false,
                                        MatchAllWordForms: false,
                                        Forward: true, //this may be the one
                                        Wrap: false,
                                        Format: false,
                                        ReplaceWith: value,
                                        Replace: WdReplace.wdReplaceOne
                                        ))
            {
                replaces++;
            }
            return replaces;
        }
    }
}
