using iTextSharp.text.pdf;
using iTextSharp.text;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Tokument
{
	public class TextBlockInfo
	{
		public TextBlockInfo(DocumentFont docFont, float fontSize, Rectangle rectBound, string blockText, bool isOneLine, List<LocTextExtractionStrategy.TextChunk> textChunks)
		{
			DocFont = docFont;
			FontSize = fontSize;
			RectBound = rectBound;
			BlockText = blockText;
			OneBlockInLine = isOneLine;
			TextChunks = textChunks;
		}

		public DocumentFont DocFont;
		public float FontSize;
		public Rectangle RectBound;
		public string BlockText;
		public List<LocTextExtractionStrategy.TextChunk> TextChunks;
		public bool OneBlockInLine; // true : when only this block is in the line
	}

}
