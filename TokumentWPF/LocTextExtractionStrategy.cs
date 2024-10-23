using iTextSharp.text.pdf;
using iTextSharp.text;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Tokument
{
	public class LocTextExtractionStrategy : ITextExtractionStrategy
	{
		public LocTextExtractionStrategy()
		{
			this._UndercontentCharacterSpacing = 0;
			this._UndercontentHorizontalScaling = 0;
			this.locationalResult = new List<LocTextExtractionStrategy.TextChunk>();
			this.ThisPdfDocFonts = new SortedList<string, DocumentFont>();
		}

		public virtual void BeginTextBlock()
		{
		}

		public virtual void EndTextBlock()
		{
		}

		private bool StartsWithSpace(string str)
		{
			if (str.Length == 0)
				return false;
			return str[0] == ' ';
		}

		private bool EndsWithSpace(string str)
		{
			if (str.Length == 0)
				return false;
			return str[str.Length - 1] == ' ';
		}

		public float UndercontentCharacterSpacing
		{
			get
			{
				return this._UndercontentCharacterSpacing;
			}
			set
			{
				this._UndercontentCharacterSpacing = value;
			}
		}

		public float UndercontentHorizontalScaling
		{
			get
			{
				return this._UndercontentHorizontalScaling;
			}
			set
			{
				this._UndercontentHorizontalScaling = value;
			}
		}

		public virtual string GetResultantText()
		{
			this.locationalResult.Sort();
			StringBuilder sb = new StringBuilder();
			LocTextExtractionStrategy.TextChunk lastChunk = null;
			foreach (LocTextExtractionStrategy.TextChunk chunk in this.locationalResult)
			{
				if (lastChunk == null)
					sb.Append(chunk.text);
				else
				{
					if (chunk.SameLine(lastChunk))
					{
						float dist = chunk.DistanceFromEndOf(lastChunk);
						if (dist < -chunk.charSpaceWidth)
							sb.Append(' ');
						else if (dist > chunk.charSpaceWidth / 2f && !this.StartsWithSpace(chunk.text) && !this.EndsWithSpace(lastChunk.text))
						{
							sb.Append(' ');
						}
						sb.Append(chunk.text);
					}
					else
					{
						sb.Append('\n');
						sb.Append(chunk.text);
					}
				}
				lastChunk = chunk;
			}
			return sb.ToString();
		}

		public List<TextBlockInfo> GetTextLocations(string pSearchString, StringComparison pStrComp)
		{
			List<TextBlockInfo> FoundMatches = new List<TextBlockInfo>();
			StringBuilder sb = new StringBuilder();
			List<LocTextExtractionStrategy.TextChunk> ThisLineChunks = new List<LocTextExtractionStrategy.TextChunk>();
			bool bStart = false;
			bool bEnd = false;
			LocTextExtractionStrategy.TextChunk FirstChunk = null;
			LocTextExtractionStrategy.TextChunk LastChunk = null;

			// add dummy chunk for one chunk problem
			TextChunk emptyChunk = new TextChunk(" ", new Vector(1.0f, 0, 0), new Vector(1.0f, 1.0f, 0), 0.1f, BaseColor.WHITE, 0);
			this.locationalResult.Add(emptyChunk);
			this.locationalResult.Sort();
			this.locationalResult.Add(emptyChunk);

            foreach (LocTextExtractionStrategy.TextChunk chunk in this.locationalResult)
			{
				if (ThisLineChunks.Count > 0 && !chunk.SameLine(ThisLineChunks.Last<LocTextExtractionStrategy.TextChunk>()))
				{
					string testString = sb.ToString();
					if (sb.ToString().IndexOf(pSearchString, pStrComp) > -1)
					{
						string sLine = sb.ToString();
						int iCount = 0;
						for (int lPos = sLine.IndexOf(pSearchString, 0, pStrComp); lPos > -1; lPos = sLine.IndexOf(pSearchString, lPos, pStrComp))
						{
							iCount++;
							if (lPos + pSearchString.Length > sLine.Length)
								break;
							lPos += pSearchString.Length;
						}
						int curPos = 0;
						int num = iCount;
						for (int i = 1; i <= num; i++)
						{
							int iFromChar = sLine.IndexOf(pSearchString, curPos, pStrComp);
							curPos = iFromChar;
							int iToChar = iFromChar + pSearchString.Length - 1;
							string sCurrentText = null;
							string sTextInUsedChunks = null;
							List<TextChunk> foundChunks = new List<TextChunk>();

							foreach (LocTextExtractionStrategy.TextChunk chk in ThisLineChunks)
							{
								sCurrentText += chk.text;
								if (!bStart && sCurrentText.Length - 1 >= iFromChar)
								{
									FirstChunk = chk;
									foundChunks.Add(chk);
									bStart = true;
								}
								if (bStart & !bEnd)
								{
									sTextInUsedChunks += chk.text;
									if(!foundChunks.Contains(chk))
										foundChunks.Add(chk);
								}
								if (!bEnd && sCurrentText.Length - 1 >= iToChar)
								{
									LastChunk = chk;
									if (!foundChunks.Contains(chk))
										foundChunks.Add(chk);
									bEnd = true;
								}
								if (bStart && bEnd)
								{
									Rectangle rect = new Rectangle(FirstChunk.PosLeft, FirstChunk.PosBottom, LastChunk.PosRight, FirstChunk.PosTop);
									//Rectangle rect = this.GetRectangleFromText(FirstChunk, LastChunk, pSearchString, sTextInUsedChunks, iFromChar, iToChar, pStrComp);
									DocumentFont font = this.ThisPdfDocFonts.ElementAt(FirstChunk.FontIndex).Value;
									float fontSize = FirstChunk.CurFontSize;
									string blockText = "";
									foreach(TextChunk cc in foundChunks)
                                    {
										blockText += cc.text;
                                    }
									string strLine = sb.ToString().Trim();
									bool centerAlign = false;
									if(strLine == blockText)
										centerAlign = true;
									FoundMatches.Add(new TextBlockInfo(font, fontSize, rect, blockText, centerAlign, foundChunks));
									curPos += pSearchString.Length;
									bStart = false;
									bEnd = false;
									break;
								}
							}
							
						}
					}
					sb.Clear();
					ThisLineChunks.Clear();
				}
				ThisLineChunks.Add(chunk);
				sb.Append(chunk.text);
			}
            return FoundMatches;
		}

		private Rectangle GetRectangleFromText(LocTextExtractionStrategy.TextChunk FirstChunk, LocTextExtractionStrategy.TextChunk LastChunk, string pSearchString, string sTextinChunks, int iFromChar, int iToChar, StringComparison pStrComp)
		{
			float LineRealWidth = LastChunk.PosRight - FirstChunk.PosLeft;
			float LineTextWidth = this.GetStringWidth(sTextinChunks, LastChunk.CurFontSize, LastChunk.charSpaceWidth, this.ThisPdfDocFonts.Values.ElementAt(LastChunk.FontIndex));
			float TransformationValue = LineRealWidth / LineTextWidth;
			int iStart = sTextinChunks.IndexOf(pSearchString, pStrComp);
			int iEnd = iStart + pSearchString.Length - 1; 
			
			string sLeft;
			if(iStart == 0)
				sLeft = null;
			else
				sLeft = sTextinChunks.Substring(0, iStart);
			
			string sRight;
			if(iEnd == sTextinChunks.Length - 1)
				sRight = null;
			else
				sRight = sTextinChunks.Substring(iEnd + 1, sTextinChunks.Length - iEnd - 1);

			float LeftWidth = 0f;
			if(iStart > 0)
            {
				LeftWidth = this.GetStringWidth(sLeft, LastChunk.CurFontSize, LastChunk.charSpaceWidth, this.ThisPdfDocFonts.Values.ElementAt(LastChunk.FontIndex));
                LeftWidth *= TransformationValue;
			}
			
			float RightWidth = 0f;
			if (iEnd < checked(sTextinChunks.Length - 1))
			{
				RightWidth = this.GetStringWidth(sRight, LastChunk.CurFontSize, LastChunk.charSpaceWidth, this.ThisPdfDocFonts.Values.ElementAt(LastChunk.FontIndex));
				RightWidth *= TransformationValue;
			}
			float LeftOffset = FirstChunk.distParallelStart + LeftWidth;
			float RightOffset = LastChunk.distParallelEnd - RightWidth;
			return new Rectangle(LeftOffset, FirstChunk.PosBottom, RightOffset, FirstChunk.PosTop);
		}

		private float GetStringWidth(string str, float curFontSize, float pSingleSpaceWidth, DocumentFont pFont)
		{
			char[] chars = str.ToCharArray();
			float totalWidth = 0f;
			foreach (char c in chars)
			{
				float w = (float)((double)pFont.GetWidth(c) / 1000.0);
				totalWidth += (w * curFontSize + this.UndercontentCharacterSpacing) * this.UndercontentHorizontalScaling / 100;
			}
			return totalWidth;
		}

		public virtual void RenderText(TextRenderInfo renderInfo)
		{
			string str = renderInfo.GetText();

			LineSegment segment = renderInfo.GetBaseline();
			LocTextExtractionStrategy.TextChunk location = 
				new LocTextExtractionStrategy.TextChunk(
					renderInfo.GetText(), 
					segment.GetStartPoint(), 
					segment.GetEndPoint(), 
					renderInfo.GetSingleSpaceWidth(),
					renderInfo.GetFillColor(),
					this.locationalResult.Count
				);
			LocTextExtractionStrategy.TextChunk textChunk = location;

			textChunk.PosLeft = renderInfo.GetDescentLine().GetStartPoint()[0];
			textChunk.PosRight = renderInfo.GetAscentLine().GetEndPoint()[0];
			textChunk.PosBottom = renderInfo.GetDescentLine().GetStartPoint()[1];
			textChunk.PosTop = renderInfo.GetAscentLine().GetEndPoint()[1];
			textChunk.CurFontSize = textChunk.PosTop - textChunk.PosBottom;// segment.GetStartPoint()[1];
			string StrKey = renderInfo.GetFont().PostscriptFontName + textChunk.CurFontSize.ToString();
			if (!this.ThisPdfDocFonts.ContainsKey(StrKey))
				this.ThisPdfDocFonts.Add(StrKey, renderInfo.GetFont());
			
			textChunk.FontIndex = this.ThisPdfDocFonts.IndexOfKey(StrKey);
			this.locationalResult.Add(location);
		}

		public void RenderImage(ImageRenderInfo renderInfo)
		{
		}

		private float _UndercontentCharacterSpacing;

		private float _UndercontentHorizontalScaling;

		private SortedList<string, DocumentFont> ThisPdfDocFonts;

		private List<LocTextExtractionStrategy.TextChunk> locationalResult;

		public class TextChunk : IComparable<LocTextExtractionStrategy.TextChunk>
		{
			public int FontIndex;
			public float PosLeft;
			public float PosRight;
			public float PosTop;
			public float PosBottom;
			public float CurFontSize;
			public BaseColor FillColor;
			public int ChunkIndex;

			public TextChunk(string str, Vector startLocation, Vector endLocation, float charSpaceWidth, BaseColor fillColor, int chunkIdex)
			{
				this.text = str;
				this.startLocation = startLocation;
				this.endLocation = endLocation;
				this.charSpaceWidth = charSpaceWidth;
				
				Vector oVector = endLocation.Subtract(startLocation);
				if (oVector.Length == 0f)
					oVector = new Vector(1f, 0f, 0f);
				
				this.orientationVector = oVector.Normalize();
				this.orientationMagnitude = (int)(Math.Atan2((double)this.orientationVector[1], (double)this.orientationVector[0]) * 1000.0);

				Vector origin = new Vector(0f, 0f, 1f);
				this.distPerpendicular = (int)Math.Round((double)startLocation.Subtract(origin).Cross(this.orientationVector)[2]);

				this.distParallelStart = this.orientationVector.Dot(startLocation);
				this.distParallelEnd = this.orientationVector.Dot(endLocation);
				FillColor = fillColor;
				ChunkIndex = chunkIdex;
			}

			public void PrintDiagnostics()
			{
				Console.WriteLine(string.Concat(new string[]
				{
					"Text (@",
					Convert.ToString(this.startLocation),
					" -> ",
					Convert.ToString(this.endLocation),
					"): ",
					this.text
				}));
				Console.WriteLine("orientationMagnitude: " + this.orientationMagnitude);
				Console.WriteLine("distPerpendicular: " + this.distPerpendicular);
				Console.WriteLine("distParallel: " + this.distParallelStart);
			}

			public bool SameLine(LocTextExtractionStrategy.TextChunk a)
			{
				bool SameLine;
				if (this.orientationMagnitude != a.orientationMagnitude)
					SameLine = false;
				else
					SameLine = !(this.distPerpendicular != a.distPerpendicular);
				return SameLine;
			}

			public float DistanceFromEndOf(LocTextExtractionStrategy.TextChunk other)
			{
				return this.distParallelStart - other.distParallelEnd;
			}

			public int CompareTo(LocTextExtractionStrategy.TextChunk rhs)
			{
				int CompareTo;
				if (this == rhs)
					CompareTo = 0;
				else
				{
					int rslt = LocTextExtractionStrategy.TextChunk.CompareInts(this.orientationMagnitude, rhs.orientationMagnitude);
					if (rslt != 0)
						CompareTo = rslt;
					else
					{
						rslt = LocTextExtractionStrategy.TextChunk.CompareInts(this.distPerpendicular, rhs.distPerpendicular);
						if (rslt != 0)
							CompareTo = rslt;
						else
						{
							rslt = ((this.distParallelStart < rhs.distParallelStart) ? -1 : 1);
							CompareTo = rslt;
						}
					}
				}
				return CompareTo;
			}

			private static int CompareInts(int int1, int int2)
			{
				return (int1 == int2) ? 0 : ((int1 < int2) ? -1 : 1);
			}

			// the text of the chunk
			internal string text;

			// the starting location of the chunk
			internal Vector startLocation;

			// the ending location of the chunk
			internal Vector endLocation;

			// unit vector in the orientation of the chunk
			internal Vector orientationVector;

			// the orientation as a scalar for quick sorting
			internal int orientationMagnitude;

			// perpendicular distance to the orientation unit vector (i.e. the Y position in an unrotated coordinate system)
			// we round to the nearest integer to handle the fuzziness of comparing floats
			internal int distPerpendicular;

			// distance of the start of the chunk parallel to the orientation unit vector (i.e. the X position in an unrotated coordinate system)
			internal float distParallelStart;

			// distance of the end of the chunk parallel to the orientation unit vector (i.e. the X position in an unrotated coordinate system)
			internal float distParallelEnd;

			// the width of a single space character in the font of the chunk
			internal float charSpaceWidth;

		}

	}
}
