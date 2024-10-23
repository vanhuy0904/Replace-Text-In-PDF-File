using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;

namespace Tokument
{
    class PdfProcessor : TemplateProcessor
    {
		Dictionary<int, List<int>> textChunksToRemove = new Dictionary<int, List<int>>();

		public override void StartMerge(DataSource dataSource, string templateName, string outputFolder, string resultName, string resultExt)
        {
            logger?.Invoke(this, $"*************************************************************************");
			logger?.Invoke(this, $"Starting auto filling on {templateName}\n");

            int rowCount = dataSource.ResultTable.Rows.Count;
			int rowIndx = 0, tagIndex = 0;

			HashSet<string> tags = new HashSet<string>();// = FindTags(templateName);
			tags.Add("sfrattini on LAPCK6H6L3 with DISTILLER");

			// initialize progress bar
			int maxStep = rowCount * tags.Count;
            progresser?.Invoke(this, 0, maxStep);

			bool success = true;
			int replaces = 0;
			List<string> missingTags = new List<string>();

			//Creates new Word document instance for Word processing
			foreach (DataRow row in dataSource.ResultTable.Rows)
            {
				string resultFile;
				if (rowIndx == 0)
					resultFile = string.Format("{0}/{1}.{2}", outputFolder, resultName, resultExt);
				else
					resultFile = string.Format("{0}/{1}-{2}.{3}", outputFolder, resultName, rowIndx, resultExt);

				PdfStamper psStamp = null;
				PdfReader pdfFileReader = new PdfReader(templateName);
				try
				{
					psStamp = new PdfStamper(pdfFileReader, new FileStream(resultFile, FileMode.Create));
				}
				catch(Exception)
                {
					logger?.Invoke(this, $"Error: {templateName} write protected.\n");
					success = false;
					break;
				}

				textChunksToRemove.Clear();
				for (int i = 0; i < pdfFileReader.NumberOfPages; i++)
					textChunksToRemove.Add(i + 1, new List<int>());

				replaces = 0;
				tagIndex = 0;
				missingTags.Clear();
				foreach (string tag in tags)
                {
					if(dataSource.ColumnNames.Contains(tag) != true)
                    {
						logger?.Invoke(this, $"> {tag} not found.");
						missingTags.Add(tag);
					}
                    else
                    {
						string value = row[tag].ToString();

                        ReplaceTag(tag, value, pdfFileReader, psStamp);
                        //ReplaceTag("{{" + tag + "}}", value, pdfFileReader, psStamp);
                        replaces++;
					}
					tagIndex++;
					// step progress bar
					progresser?.Invoke(this, rowIndx * tags.Count + tagIndex, maxStep);
				}

				int numberOfPages = pdfFileReader.NumberOfPages;
				for (int intCurrPage = 1; intCurrPage <= numberOfPages; intCurrPage++)
				{
					PdfContentStreamEditor editor = new TextRemover(textChunksToRemove[intCurrPage]);
					editor.EditPage(psStamp, intCurrPage);
				}

                logger?.Invoke(this, $"{replaces}/{tags.Count} replaced.");
				
				psStamp.Close();
				pdfFileReader.Close();
				logger?.Invoke(this, $"{resultFile} generated.\n");

				rowIndx++;
            }
			logger?.Invoke(this, $"Merging completed\n");
			if (success == true)
				successHandler?.Invoke(this, true, replaces, tags.Count, missingTags);
			else
				successHandler?.Invoke(this, false, 0, 0, null);

			progresser?.Invoke(this, 0, 0);
		}

        public override void StartDetect(string templateName)
        {
			int founds = FindTags(templateName).Count;
			
			detector?.Invoke(this, "pdf", founds);
		}

		private string GetFontName(string postscriptFont)
        {
			string chunkFontName = postscriptFont.ToLower();
			// get family name
			string familyName = "helvetica";
			foreach (string family in FontFactory.RegisteredFamilies)
            {
				if (Regex.Match(chunkFontName, $".*{family}.*").Success)
				{
					familyName = family.ToLower();
					break;
				}
			}
			// set font name
			string selectedFontName = familyName.First().ToString().ToUpper() + familyName.Substring(1);
			foreach(string fontName in FontFactory.RegisteredFonts)
            {
				if(fontName.Contains(familyName))
                {
					string[] tags = fontName.Split('-');
					if (tags.Length == 1)
						continue;
					if (chunkFontName.Contains(tags[1]))
						selectedFontName += ("-" + tags[1].First().ToString().ToUpper() + tags[1].Substring(1));
				}
			}
			return selectedFontName;
        }

        private int ReplaceTag(string tag, string value, PdfReader pdfFileReader, PdfStamper psStamp)
		{
			int replacedCount = 0;
			// calculate page center position
			Rectangle pagesize = pdfFileReader.GetPageSize(1);
			float CenterPosX = (pagesize.Left + pagesize.Right) / 2;

			int numberOfPages = pdfFileReader.NumberOfPages;
			for (int intCurrPage = 1; intCurrPage <= numberOfPages; intCurrPage++)
			{
				LocTextExtractionStrategy lteStrategy = new LocTextExtractionStrategy();

				PdfContentByte pcbUnderContent = psStamp.GetUnderContent(intCurrPage);
				PdfContentByte pcbOverContent = psStamp.GetOverContent(intCurrPage);

				lteStrategy.UndercontentCharacterSpacing = pcbUnderContent.CharacterSpacing;
				lteStrategy.UndercontentHorizontalScaling = pcbUnderContent.HorizontalScaling;

				PdfTextExtractor.GetTextFromPage(pdfFileReader, intCurrPage, lteStrategy);
				
				List<TextBlockInfo> lstMatches = lteStrategy.GetTextLocations(tag, StringComparison.CurrentCultureIgnoreCase);
				foreach(TextBlockInfo blockInfo in lstMatches)
                {
					foreach(LocTextExtractionStrategy.TextChunk chunk in blockInfo.TextChunks)
                    {
						textChunksToRemove[intCurrPage].Add(chunk.ChunkIndex);
                    }
                }
				
				PdfLayer pdLayer = new PdfLayer("Overwrite", psStamp.Writer);

				foreach (TextBlockInfo chunkInfo in lstMatches)
				{
                    pcbOverContent.BeginLayer(pdLayer);

                    PdfGState pgState = new PdfGState();
                    pcbOverContent.SetGState(pgState);

                    pcbOverContent.BeginText();

					
					string replaceText = chunkInfo.BlockText;
					// calcualte width of character
					float chWidth = chunkInfo.RectBound.Width / replaceText.Length;
					replaceText = replaceText.Replace(tag, value);
					// calculate new text width and original chunk center 
					float textWidth = chWidth * replaceText.Length;
					float deltaWidth = (chunkInfo.RectBound.Width - textWidth) / 2;
					float chunkCenterX = (chunkInfo.RectBound.Left + chunkInfo.RectBound.Right) / 2;
					// calculate bounding box when center aligned
					if (chunkInfo.OneBlockInLine == true)
					{
						if (Math.Abs(CenterPosX - chunkCenterX) < 10.0) // center aligned
                        {
							chunkInfo.RectBound.Left += deltaWidth;
							chunkInfo.RectBound.Right = chunkInfo.RectBound.Left + textWidth;
						}
						else
                        {
							if(CenterPosX < chunkCenterX) // right aligned
                            {
								chunkInfo.RectBound.Left = chunkInfo.RectBound.Right - textWidth;
							}
                        }
					}
					pcbOverContent.SetTextMatrix(chunkInfo.RectBound.Left, chunkInfo.RectBound.Bottom + chunkInfo.RectBound.Height / 4);

                    string fontName = GetFontName(chunkInfo.DocFont.PostscriptFontName);

                    BaseFont bf = BaseFont.CreateFont(fontName, chunkInfo.DocFont.Encoding, true);
                    pcbOverContent.SetFontAndSize(bf, chunkInfo.FontSize);

                    pcbOverContent.ShowText(replaceText);

                    pcbOverContent.EndText();

                    pcbOverContent.EndLayer();

                    replacedCount++;
				}
			}

			return replacedCount;
		}

		private HashSet<string> FindTags(string templateName)
        {
			PdfReader pdfFileReader = new PdfReader(templateName);

			List<string> data = new List<string>();
            HashSet<string> foundTags = new HashSet<string>();

            int numberOfPages = pdfFileReader.NumberOfPages;
			for (int intCurrPage = 1; intCurrPage <= numberOfPages; intCurrPage++)
			{
				LocTextExtractionStrategy lteStrategy = new LocTextExtractionStrategy();
				string currentText = PdfTextExtractor.GetTextFromPage(pdfFileReader, intCurrPage, lteStrategy);
				data.Add(currentText);
			}

			foreach (string line in data)
			{
				var matches = System.Text.RegularExpressions.Regex.Matches(line, @"{{(.*?)}}");
				foreach (Match match in matches)
				{
					string tag = match.Value;
					if (tag.Length < 4) continue;
					tag = tag.Substring(2, tag.Length - 4);
					foundTags.Add(tag);
				}
			}
			pdfFileReader.Close();

			return foundTags;
        }
	}
}
