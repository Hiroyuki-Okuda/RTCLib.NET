using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

namespace RTCLib.Fio
{
    /// <summary>
    /// Fast CSV reader for proper format
    /// </summary>
    /// This class is applicable for 
    /// number only CSV file with proper header.
    /// This class do less error check 
    /// to maintain the fast speed reading.
    ///  
    /// Blank cell is loaded as "NaN".
    public class CSVReaderFast
    {
        /// <summary>
        /// Loaded data
        /// </summary>
        public List<double[]> data = new List<double[]>();

        /// <summary>
        /// Dictionary of index with tags
        /// </summary>
        public Dictionary<string, int> tags = new Dictionary<string, int>();

        /// <summary>
        /// If data is loaded with tags or not;
        /// </summary>
        public bool isHeaderLoaded = false;

        private char[] delimiters = new char[] { ',' };

        /// <summary>
        /// Skipping lines, 0 = normal, 1 : every two rows
        /// </summary>
        public int SkipLine = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public CSVReaderFast()
        {
            ClearData();
        }

        /// <summary>
        /// set delimiters
        /// </summary>
        /// <param name="delim">delimiter</param>
        public void SetDelimiter(char delim)
        {
            delimiters = new char[] { delim };
        }

        /// <summary>
        /// set delimiters
        /// </summary>
        /// <param name="delim"></param>
        public void SetDelimiter(char[] delim)
        {
            delimiters = delim;
        }

        /// <summary>
        /// Clear loaded data
        /// </summary>
        public void ClearData()
        {
            if (data != null)
            {
                data.Clear();
            }
            if (tags != null)
            {
                isHeaderLoaded = false;
                tags.Clear();
            }
        }

        /// <summary>
        /// Open csv file and read it
        /// </summary>
        /// <param name="fn">file name to open</param>
        /// <param name="num_of_header_rows">
        /// number of header lines. 
        /// last line is used as tags of columns
        /// </param>
        public void Open(string fn, int num_of_header_rows = 0)
        {
            ClearData();
            try
            {
                OpenImple(fn, num_of_header_rows);
            }
            catch (FormatException o)
            {
                global::System.Windows.Forms.MessageBox.Show( o.Message,"CSV load error");
            }
        }

        /// <summary>
        /// Implement of open csv
        /// </summary>
        /// <param name="fn">filename</param>
        /// <param name="num_of_header_rows">index of tags line</param>
        private void OpenImple(string fn, int num_of_header_rows)
        {
            global::System.IO.FileStream fs = new FileStream(fn, FileMode.Open);

            // open file as stream
            //StreamReader sr = new StreamReader(fn, Encoding.ASCII);
            StreamReader sr = new StreamReader(fs);

            double length = fs.Length;

            int read_line = 0;
            int skipping = SkipLine;
            while (!sr.EndOfStream)
            {
                read_line++;
                string str = sr.ReadLine();

                double cur_pos = sr.BaseStream.Position / length * 100;
                //System.Diagnostics.Debug.WriteLine("pos=" + cur_pos);

                // header lines
                if (read_line == num_of_header_rows)
                {
                    ParseTags(str);
                    continue;
                }

                // before header lines
                if (read_line < num_of_header_rows) continue;

                if (skipping > 0)
                {
                    skipping--;
                    continue;
                }
                skipping = SkipLine;

                string[] temp = str.Split(delimiters, StringSplitOptions.None);

                // for comma termination
                int num_of_columns = temp.Length;
                if (string.IsNullOrWhiteSpace(temp.Last())) num_of_columns--;

                // loaded numbers
                var tmp_d = new double[temp.Length];

                for (var i = 0; i < num_of_columns; ++i)
                {
                    //temp[i] = temp[i].Trim();
                    //if (string.IsNullOrEmpty(temp[i]))
                    //{
                    //    tmp_d[i] = double.NaN;
                    //}
                    //else
                    {
                        //var ret = double.TryParse(temp[i], out tmp_d[i]);
                        //isConversionAllSuccess = isConversionAllSuccess & ret;
                        if (temp[i].ToLower() == "nan")
                        {
                            tmp_d[i] = 0;
                        }
                        else
                        {

                            try
                            {
                                tmp_d[i] = double.Parse(temp[i]);
                            }
                            catch (FormatException)
                            {
                                if (0 == string.Compare(temp[i], "NaN", true))
                                {
                                    tmp_d[i] = double.NaN;
                                    continue;
                                }
                                ClearData();
                                throw new FormatException(
                                    "Load failure : CSV contains illigal format.\r\n Line : "
                                    + read_line.ToString()
                                    );
                            }
                        }
                    }
                }

                data.Add(tmp_d);
            }
        }

        /// <summary>
        /// parse tags
        /// </summary>
        /// <param name="tag_line"></param>
        private void ParseTags(string tag_line)
        {
            string[] temp = tag_line.Split(delimiters, StringSplitOptions.None);
            
            // for comma termination
            int num_of_columns = temp.Length;
            if (string.IsNullOrWhiteSpace(temp.Last())) num_of_columns--;

            for (int i = 0; i < num_of_columns; ++i)
            {
                tags.Add(temp[i].Trim(), i);
            }
            if (tags.Count > 0) isHeaderLoaded = true;
        }

        /// <summary>
        /// indexer for row data
        /// </summary>
        /// <param name="index">index of row to access</param>
        /// <returns>double array of directed row</returns>
        public double[] this[int row]
        {
            get {
                return data[row];
            }
            set {
                data[row] = value;
            }
        }

        /// <summary>
        /// indexer for element
        /// </summary>
        /// <param name="row">index of row to access</param>
        /// <param name="col">index of column to access</param>
        /// <returns>value of (row, col)</returns>
        public double this[int row, int col]
        {
            get{
                return data[row][col];
            }
            set{
                data[row][col] = value;
            }
        }

        /// <summary>
        /// Indexer to access cell with row and tags
        /// </summary>
        /// <param name="row">index of row to access</param>
        /// <param name="tag">tag name of column to access</param>
        /// <returns></returns>
        public double this[int row, string tag]
        {
            get {
                return data[row][tags[tag]];
            }
            set {
                data[row][tags[tag]] = value;
            }
        }

        /// <summary>
        /// Check the directed tag exists or not
        /// </summary>
        /// <param name="key">tag to find</param>
        /// <returns></returns>
        public bool isTagExisting(string key)
        {
            return tags.ContainsKey(key);
        }

        public bool isDataLoaded
        {
            get{
                return data.Count > 0;
            }
        }

        /// <summary>
        /// Get column index of targeting tag
        /// </summary>
        /// <param name="key">tag name to get its index</param>
        /// <returns>index of column</returns>
        public int GetIndexOfTag(string key)
        {
            return tags[key];
        }

        /// <summary>
        /// Getting column vector
        /// </summary>
        /// <param name="col">targeting column number</param>
        /// <returns>column data by double array</returns>
        public double[] GetColumnArray(int col)
        {
            double[] ret = new double[data.Count];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = data[i][col];
            }
            return ret;
        }

        /// <summary>
        /// Getting column vector
        /// </summary>
        /// <param name="key">targeting column tag</param>
        /// <returns>column data by double array</returns>
        public double[] GetColumnArray(string tag)
        {
            return GetColumnArray(tags[tag]);
        }

        /// <summary>
        /// Return number of rows
        /// </summary>
        /// <returns>num of rows</returns>
        public int Rows
        {
            get{
                return data.Count;
            }
        }

        /// <summary>
        /// Return number of columns
        /// </summary>
        /// <returns>num of columns</returns>
        public int Cols
        {
            get
            {
                if (data.Count == 0) return 0;
                return data[0].Length;
            }
        }
    }
}
