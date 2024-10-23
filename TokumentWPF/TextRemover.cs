using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokument
{
    class TextRemover : PdfContentStreamEditor
    {
        public TextRemover(List<int> chunkIndexes)
        {
            ChunkIndexes = chunkIndexes;
        }


        protected override void Write(PdfContentStreamProcessor processor, PdfLiteral operatorLit, List<PdfObject> operands)
        {
            List<PdfObject> newOpernads = new List<PdfObject>();
            if (TEXT_SHOWING_OPERATORS.Contains(operatorLit.ToString()))
            {
                foreach(PdfObject obj in operands)
                {
                    if(obj.IsArray())
                    {
                        List<PdfObject> subObjArray = ((PdfArray)obj).ArrayList;
                        PdfArray newSubObjArray = new PdfArray();
                        foreach(PdfObject subObj in subObjArray)
                        {
                            if(subObj.IsString())
                            {
                                ChunkIndex++;
                                if (!ChunkIndexes.Contains(ChunkIndex))
                                {
                                    newSubObjArray.Add(subObj);
                                }
                            }
                            else
                            {
                                newSubObjArray.Add(subObj);
                            }
                        }
                        newOpernads.Add(newSubObjArray);
                    }
                    else if(obj.IsString())
                    {
                        ChunkIndex++;
                        if (!ChunkIndexes.Contains(ChunkIndex))
                        {
                            newOpernads.Add(obj);
                        }
                            
                    }
                    else
                    {
                        newOpernads.Add(obj);
                    }
                }
            }
            else
            {
                newOpernads = operands;
            }
            base.Write(processor, operatorLit, newOpernads);
        }
        List<string> TEXT_SHOWING_OPERATORS = new List<string> { "Tj", "'", "\"", "TJ" };
        int ChunkIndex = -1;
        List<int> ChunkIndexes = null;
    }
}
