using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
namespace backup
{
    class ProcessFile
    {
        public Boolean checkFileExisted(string filePath)
        {
            Boolean kq = false;
            if (File.Exists(filePath))
            {
                Console.WriteLine($"The file [{filePath}] exists.");
                kq = true;
            }
            else
            {
                //  Console.WriteLine("The file does not exist.");
            }
            return kq;
        }
    }
}
