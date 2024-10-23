using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ExcelDataReader;
using iTextSharp.text.pdf;

namespace Tokument
{
    abstract class TemplateProcessor
    {
        public abstract void StartMerge(DataSource dataSource, string templateName, string outputFolder, string resultName, string resultExt);
        public abstract void StartDetect(string templateName);

        public delegate void ProgressHandler(TemplateProcessor processor, int step, int maxstep);
        public ProgressHandler progresser;

        public delegate void LogHandler(TemplateProcessor processor, string log);
        public LogHandler logger;

        public delegate void SuccessHandler(TemplateProcessor processor, bool success, int replaced, int total, List<string> missingTags);
        public SuccessHandler successHandler;

        public delegate void TagDetectHandler(TemplateProcessor processor, string templateType, int detectedCount);
        public TagDetectHandler detector;
    }
}
