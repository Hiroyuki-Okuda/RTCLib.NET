using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using System.IO;
using System.Net.Mime;

namespace RTCLib.Fio
{
    /// <summary>
    /// Fast CSV reader for proper format
    /// </summary>
    /// This class is applicable for 
    /// number(double) only CSV file with proper header.
    /// This class do no error check 
    /// to maintain the fast speed reading.
    ///  
    /// Blank cell is loaded as "NaN".
    /// 
    public class CsvReader
    {
        /// <summary>
        /// Loaded data
        /// </summary>
        public List<double[]> Data = new List<double[]>();

        /// <summary>
        /// Dictionary of index with tags
        /// </summary>
        public Dictionary<string, int> Tags = new Dictionary<string, int>();

        /// <summary>
        /// If data is loaded with tags or not;
        /// </summary>
        public bool IsHeaderLoaded => (Tags != null && Tags.Count > 0);

        private char[] _delimiters = new char[] { ',' };

        /// <summary>
        /// Skipping lines, 0 = normal, 1 : every two rows
        /// </summary>
        public int SkipLine = 0;

        /// <summary>
        /// Constructor
        /// </summary>
        public CsvReader()
        {
            ClearData();
        }

        /// <summary>
        /// set delimiters
        /// </summary>
        /// <param name="delim">delimiter</param>
        public void SetDelimiter(char delim)
        {
            _delimiters = new char[] { delim };
        }

        /// <summary>
        /// set delimiters
        /// </summary>
        /// <param name="delim"></param>
        public void SetDelimiter(char[] delim)
        {
            _delimiters = delim;
        }

        /// <summary>
        /// Clear loaded data
        /// </summary>
        public void ClearData()
        {
            if(Data==null)
                Data = new List<double[]>();
            else
               Data.Clear();
            if (Tags== null)
                Tags = new Dictionary<string, int>();
            else
                Tags.Clear();
        }

        /// <summary>
        /// Open csv file and read it
        /// </summary>
        /// <param name="fileName">file name to open</param>
        /// <param name="numOfHeaderRows">
        /// number of header lines. 
        /// last line is used as tags of columns
        /// </param>
        /// <param name="encoding">Text encoding</param>
        public void Open(string fileName, int numOfHeaderRows = 0, System.Text.Encoding encoding = null)
        {
            ClearData();
            try
            {
                OpenImple(fileName, numOfHeaderRows, encoding);
            }
            catch (FormatException o)
            {
                Debug.WriteLine("CSV format error @" + fileName);
            }
        }

        /// <summary>
        /// Implement of open csv
        /// </summary>
        /// <param name="fileName">filename</param>
        /// <param name="numOfHeaderRows">index of tags line</param>
        /// <param name="encoding">Text encoding</param>
        private void OpenImple(string fileName, int numOfHeaderRows, System.Text.Encoding encoding)
        {
            global::System.IO.FileStream fs = new FileStream(fileName, FileMode.Open);

            // open file as stream
            //StreamReader sr = new StreamReader(fileName, Encoding.ASCII);
            using StreamReader sr = encoding==null ? new StreamReader(fs) : new StreamReader(fs, encoding);

            double length = fs.Length;

            int readLine = 0;   //! Read complete lines 
            int skipping = SkipLine;

            // read headers 
            if (numOfHeaderRows != 0)
            {
                while (!sr.EndOfStream)
                {
                    readLine++; // first line is 1
                    string str = sr.ReadLine();

                    // discard lines before header line
                    if (readLine < numOfHeaderRows) continue;

                    // header lines
                    if (readLine == numOfHeaderRows)
                    {
                        ParseTags(str);
                        continue;
                    }
                }
            }

            while (!sr.EndOfStream)
            {
                readLine++;
                string str = sr.ReadLine();

                double curPos = sr.BaseStream.Position / length * 100;
                //System.Diagnostics.Debug.WriteLine("pos=" + cur_pos);

                if (skipping > 0)
                {
                    skipping--;
                    continue;
                }
                skipping = SkipLine;

                string[] splitStr = str.Split(_delimiters, StringSplitOptions.None);

                // for comma termination
                int numOfColumns = splitStr.Length;
                // remove last term if null or empty
                if (string.IsNullOrWhiteSpace(splitStr.Last())) numOfColumns--;

                // loaded numbers
                var tmpD = new double[numOfColumns];

                for (var i = 0; i < numOfColumns; ++i)
                {
                    try
                    {
                        tmpD[i] = double.Parse(splitStr[i]);
                    }
                    catch (FormatException)
                    {
                        if (0 == String.Compare(splitStr[i], "NaN", StringComparison.OrdinalIgnoreCase))
                        {
                            tmpD[i] = double.NaN;
                            continue;
                        }
                        throw new FormatException(
                            $"Load failure : CSV contains illegal format.\r\n Line {readLine} : "
                            + str
                        );
                    }
                }

                Data.Add(tmpD);
            }
        }


        /// <summary>
        /// parse tags
        /// </summary>
        /// <param name="tagLine"></param>
        private void ParseTags(string tagLine)
        {
            string[] temp = tagLine.Split(_delimiters, StringSplitOptions.None);
            
            // for comma termination
            int numOfColumns = temp.Length;
            if (string.IsNullOrWhiteSpace(temp.Last())) numOfColumns--;

            for (int i = 0; i < numOfColumns; ++i)
            {
                Tags.Add(temp[i].Trim(), i);
            }
        }

        /// <summary>
        /// indexer for row data
        /// </summary>
        /// <param name="index">index of row to access</param>
        /// <returns>double array of directed row</returns>
        public double[] this[int index]
        {
            get => Data[index];
            set => Data[index] = value;
        }

        /// <summary>
        /// indexer for element
        /// </summary>
        /// <param name="row">index of row to access</param>
        /// <param name="col">index of column to access</param>
        /// <returns>value of (row, col)</returns>
        public double this[int row, int col]
        {
            get => Data[row][col];
            set => Data[row][col] = value;
        }

        /// <summary>
        /// Indexer to access cell with row and tags
        /// </summary>
        /// <param name="row">index of row to access</param>
        /// <param name="tag">tag name of column to access</param>
        /// <returns></returns>
        public double this[int row, string tag]
        {
            get => Data[row][Tags[tag]];
            set => Data[row][Tags[tag]] = value;
        }

        /// <summary>
        /// Check the directed tag exists or not
        /// </summary>
        /// <param name="key">tag to find</param>
        /// <returns></returns>
        public bool IsTagExisting(string key) => Tags.ContainsKey(key);

        /// <summary>
        /// Return true if the data is loaded
        /// </summary>
        public bool IsDataLoaded => Data.Count > 0;

        /// <summary>
        /// Get column index of targeting tag
        /// </summary>
        /// <param name="key">tag name to get its index</param>
        /// <returns>index of column</returns>
        public int GetIndexOfTag(string key) => Tags[key];

        /// <summary>
        /// Getting column vector
        /// </summary>
        /// <param name="col">targeting column number</param>
        /// <returns>column data by double array</returns>
        public double[] GetColumn(int col)
        {
            double[] ret = new double[Data.Count];
            for (int i = 0; i < ret.Length; i++)
            {
                ret[i] = Data[i][col];
            }
            return ret;
        }

        /// <summary>
        /// Getting column vector
        /// </summary>
        /// <param name="tag">Tag name to get</param>
        /// <returns>column data by double array</returns>
        public double[] GetColumn(string tag)
        {
            return GetColumn(Tags[tag]);
        }

        /// <summary>
        /// Return number of rows
        /// </summary>
        /// <returns>num of rows</returns>
        public int Rows => Data.Count;

        /// <summary>
        /// Get number of columns
        /// </summary>
        /// <param name="rowIndex">Row index to check.</param>
        /// <returns>
        /// The number of columns in the row.
        /// Number of columns can be different for each rows
        /// for Jagged array
        /// </returns>
        public int Cols(int rowIndex = 0)
        {
            if (rowIndex >= Data.Count) return 0;
            return Data[rowIndex].Length;
        }
    }
}
