using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using System.Web;

namespace RTCLib.Fio
{
    /// <summary>
    /// Search file
    /// </summary>
    public class FilePath
    {
        // base path
        private string base_path;

        // 参照パス
        List<string> search_paths = new List<string>();

        public FilePath()
        {
            base_path = Directory.GetCurrentDirectory();
        }

        public FilePath(string basepath)
        {
            base_path = basepath;
        }

        public string BasePath{
            set { this.base_path = value; }
            get { return this.base_path; }
        }

        public void ClearSearchPath()
        {
            search_paths = new List<string>();
        }

        public void RegisterPath(string search_path)
        {
            Uri here = new Uri(GetPathWithEndSeparator(base_path));
            Uri path = new Uri(here, search_path);

            DirectoryInfo info = new DirectoryInfo(here.AbsolutePath);
            DirectoryInfo info1 = new DirectoryInfo(Directory.GetCurrentDirectory());
            DirectoryInfo info2 = new DirectoryInfo(here.LocalPath);
            
            search_paths.Add( GetPathWithEndSeparator(path.LocalPath.ToString()));
        }

        public string FindFullPath(string filename)
        {
            string ret;
            if( File.Exists( filename ))
            {
                ret = global::System.IO.Path.GetFullPath(filename);
                return ret;
            }
            foreach(var x in search_paths)
            {
                Uri from = new Uri(x);
                Uri path = new Uri(from, filename);

                string p = path.LocalPath;
                
                DirectoryInfo info = Directory.GetParent(x);
                FileInfo[] a = info.GetFiles();

                //string p = Path.Combine(x, filename);
                if (File.Exists(p))
                {
                    ret = p;
                    return ret;
                }
            }

            throw new FileNotFoundException("File " + filename + " is not found.");
        }

        public static string GetPathRelative(string origin, string relative)
        {
            origin = GetPathWithEndSeparator(origin);
            Uri org = new Uri(origin);
            Uri path = new Uri(org, relative);
            return path.ToString();
        }

        public static string GetPathWithEndSeparator(string origin)
        {

            if (origin.EndsWith("\\") | origin.EndsWith("/") | origin.EndsWith(Path.DirectorySeparatorChar.ToString() ))
            {
            }
            else
            {
                origin = origin + Path.DirectorySeparatorChar;
            }
            return origin;
        }

    }

    public class FileName
    {
        public static string DateExpression = "yyyy_MM_dd";
        public static string TimeExpression = "HH_mm_ss";

        /// <summary>
        /// Replace template by date string
        /// </summary>
        /// <param name="template">filename including special words</param>
        /// <returns>replaced filename</returns>
        /// This allow following special replacement word;
        /// "$DATE$" : explain date, replaced by "yyyy_MM_dd".
        /// "$TIME$" : explain time, replaced by "HH_mm_ss".
        public static string GetFilenameWithDateTime(string template)
        {
            DateTime dt = DateTime.Now;
            string date_str = dt.ToString(DateExpression);
            string time_str = dt.ToString(TimeExpression);

            string tmp = template.Replace("$DATE$", date_str);
            tmp = tmp.Replace("$TIME$", time_str);
            return tmp;
        }

        /// <summary>
        /// Replace template by number
        /// </summary>
        /// <param name="template">filename including special words</param>
        /// <param name="num">replacing number</param>
        /// <param name="digits">0 fixing digits</param>
        /// <returns>replaced filename</returns>
        /// This allow following special replacement word;
        /// "$SEQ$" : explain sequencial number, replaced by i.ToString().
        public static string GetNumberedFilename(string template, int num, int digits=3)
        {
            return template.Replace("$SEQ$", num.ToString("D"+digits.ToString()));
        }

        public static string GetNumberedFilenameNonExist(string template)
        {
            int i = 0;
            for( i = 0; i < 999; i++)
            {
                string fn = GetNumberedFilename(template, i);
                if (!global::System.IO.File.Exists(fn))
                {
                    return fn;
                }
            }
            throw new ArgumentOutOfRangeException();
        }

    }
}
