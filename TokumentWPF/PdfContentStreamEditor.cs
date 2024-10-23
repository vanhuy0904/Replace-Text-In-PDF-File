using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tokument
{
    public class PdfContentStreamEditor : PdfContentStreamProcessor
    {
        /**
         * This method edits the immediate contents of a page, i.e. its content stream.
         * It explicitly does not descent into form xobjects, patterns, or annotations.
         */
        public void EditPage(PdfStamper pdfStamper, int pageNum)
        {
            PdfReader pdfReader = pdfStamper.Reader;
            PdfDictionary page = pdfReader.GetPageN(pageNum);
            byte[] pageContentInput = ContentByteUtils.GetContentBytesForPage(pdfReader, pageNum);
            page.Remove(PdfName.CONTENTS);
            EditContent(pageContentInput, page.GetAsDict(PdfName.RESOURCES), pdfStamper.GetUnderContent(pageNum));
        }

        /**
         * This method processes the content bytes and outputs to the given canvas.
         * It explicitly does not descent into form xobjects, patterns, or annotations.
         */
        public void EditContent(byte[] contentBytes, PdfDictionary resources, PdfContentByte canvas)
        {
            this.canvas = canvas;
            ProcessContent(contentBytes, resources);
            this.canvas = null;
        }

        /**
         * This method writes content stream operations to the target canvas. The default
         * implementation writes them as they come, so it essentially generates identical
         * copies of the original instructions the {@link ContentOperatorWrapper} instances
         * forward to it.
         *
         * Override this method to achieve some fancy editing effect.
         */
        protected virtual void Write(PdfContentStreamProcessor processor, PdfLiteral operatorLit, List<PdfObject> operands)
        {
            int index = 0;

            foreach (PdfObject pdfObject in operands)
            {
                pdfObject.ToPdf(canvas.PdfWriter, canvas.InternalBuffer);
                canvas.InternalBuffer.Append(operands.Count > ++index ? (byte)' ' : (byte)'\n');
            }
        }

        //
        // constructor giving the parent a dummy listener to talk to 
        //
        public PdfContentStreamEditor() : base(new DummyRenderListener())
        {
        }

        //
        // Overrides of PdfContentStreamProcessor methods
        //
        public override IContentOperator RegisterContentOperator(String operatorString, IContentOperator newOperator)
        {
            ContentOperatorWrapper wrapper = new ContentOperatorWrapper();
            wrapper.setOriginalOperator(newOperator);
            IContentOperator formerOperator = base.RegisterContentOperator(operatorString, wrapper);
            return formerOperator is ContentOperatorWrapper ? ((ContentOperatorWrapper)formerOperator).getOriginalOperator() : formerOperator;
        }

        public override void ProcessContent(byte[] contentBytes, PdfDictionary resources)
        {
            this.resources = resources;
            base.ProcessContent(contentBytes, resources);
            this.resources = null;
        }

        //
        // members holding the output canvas and the resources
        //
        protected PdfContentByte canvas = null;
        protected PdfDictionary resources = null;

        //
        // A content operator class to wrap all content operators to forward the invocation to the editor
        //
        class ContentOperatorWrapper : IContentOperator
        {
            public IContentOperator getOriginalOperator()
            {
                return originalOperator;
            }

            public void setOriginalOperator(IContentOperator originalOperator)
            {
                this.originalOperator = originalOperator;
            }

            public void Invoke(PdfContentStreamProcessor processor, PdfLiteral oper, List<PdfObject> operands)
            {
                if (originalOperator != null && !"Do".Equals(oper.ToString()))
                {
                    originalOperator.Invoke(processor, oper, operands);
                }
                ((PdfContentStreamEditor)processor).Write(processor, oper, operands);
            }

            private IContentOperator originalOperator = null;
        }

        //
        // A dummy render listener to give to the underlying content stream processor to feed events to
        //
        class DummyRenderListener : IRenderListener
        {
            public void BeginTextBlock() { }

            public void RenderText(TextRenderInfo renderInfo) { }

            public void EndTextBlock() { }

            public void RenderImage(ImageRenderInfo renderInfo) { }
        }
    }
}
