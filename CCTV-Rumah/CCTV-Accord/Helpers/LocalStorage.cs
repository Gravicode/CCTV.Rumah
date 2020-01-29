using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCTV_Accord.Helpers
{
    public class LocalStorage
    {
        static string TempPath = "";
        public static string GetTempFileName()
        {
            if (string.IsNullOrEmpty(TempPath))
            {
                SetTempPath(); 
            }
            var tempFile = $"{TempPath}\\{shortid.ShortId.Generate(true,false,10)}";

            //TempPath = $"{TempPath}\\{Guid.NewGuid().ToString().Replace("-", "_")}";
            return tempFile;
        }
        static void SetTempPath()
        {
            TempPath = ConfigurationManager.AppSettings["TempPath"];
            if (string.IsNullOrEmpty(TempPath))
            {
                TempPath = Path.Combine(ObjectDetector.GetAbsolutePath(""), "images", "input");
            }
            if(!Directory.Exists(TempPath))
                Directory.CreateDirectory(TempPath);
        }
        public static bool CleanTempFolder()
        {

            if (string.IsNullOrEmpty(TempPath))
            {
                SetTempPath();
            }
            foreach (var file in Directory.GetFiles(TempPath))
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {

                }
            }
            return true;

        }
    }
}
