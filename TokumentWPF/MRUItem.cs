using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Tokument
{
    class MRUItem
    {
        public MRUItem(string compactPath, string fullPath)
        {
            CompactPath = compactPath;
            FullPath = fullPath;
        }

        public string FullPath { get; set; } = "";
        public string CompactPath { get; set; } = "";
    }

}
