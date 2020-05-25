using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Microsoft.VisualBasic.FileIO;

namespace RTCLib.Fio
{
    public class CsvReaderObsolute
    {
        public List<string> column_titles = new List<string>();
        public List<List<double>> data = new List<List<double>>();

        // 間引き
        public int SkipLine = 1;

        public CsvReaderObsolute()
        {

        }

        public double this[int i,int j]
        {
            set
            {
                if (i < 0 || i >= data.Count)
                {
                    throw new global::System.ArgumentOutOfRangeException();
                }
                this.data[i][j] = value;
            }
            get {
                if (i < 0 || i >= data.Count)
                {
                    throw new global::System.ArgumentOutOfRangeException();
                }
                return this.data[i][j];
            }
        }

        int Rows() { return data.Count; }
        int Cols() {
            if (Rows() > 0)
                return data[0].Count;
            else
                return 0;
        }

        /// <summary>
        /// 指定したヘッダ文字列を持つ列の，列番号を取得
        /// </summary>
        /// <param name="col_header">検索する列番号</param>
        /// <param name="ignore_lu">true(default):大文字・小文字を無視</param>
        public int IndexOf(string col_header, bool ignore_lu = true)
        {
            int ret;

            ret = column_titles.FindIndex(str => str.ToLower() == col_header.ToLower());

            //ret = column_titles.IndexOf(col_header);
            if (ret == -1) throw new IndexOutOfRangeException("対応するヘッダ[" + col_header + "]が見つかりません．");
            return ret;
        }

        /// <summary>
        /// CSVファイルからのデータ読み込み
        /// </summary>
        /// データブロックは，正方であることを推奨．
        /// ただし，各行ごとに列数が違うのは読み込めるかも．
        /// ヘッダは無し，あるいは指定行目にヘッダが存在する場合のみ．
        /// (数行のデータと無関係な行があり，ヘッダ無しでデータが始まるものには未対応)
        /// データブロック内は数字しか受け付けない．文字列があるとダメ．
        /// デリミタは一応サポートしているが，動作は保障しない．
        /// <param name="filename"></param>
        /// <param name="delim"></param>
        /// <param name="num_of_header_rows"></param>
        public bool LoadFromFile(string filename, string delim=",", int num_of_header_rows = 1)
        {
            TextFieldParser parser;
            try
            {
                parser = new TextFieldParser(filename, global::System.Text.Encoding.GetEncoding("Shift_JIS"));
            }
            catch
            {
                return false;
            }
            parser.TextFieldType = FieldType.Delimited;

            parser.SetDelimiters(delim);

            // ヘッダ行の読み込み
            for (int i = 0; i < num_of_header_rows; i++)
            {
                if (parser.EndOfData) break;


                if (i == num_of_header_rows - 1)
                {
                    column_titles.Clear();
                    string[] tmp_titles = parser.ReadFields();

                    // リストに追加
                    column_titles.InsertRange(0, tmp_titles);

                    // 文字をトリム
                    for (int j = 0; j < column_titles.Count; j++)
                    {
                        column_titles[j] = column_titles[j].Trim();
                    }
                }
                else
                {
                    parser.ReadLine();
                }
            }

            // データ行の読み込み
            int skipping = 1;
            while (!parser.EndOfData)
            {
                string[] row;
                // 行スキップが無いか，表示行の場合
                if (skipping > 1)
                {
                    parser.ReadLine();
                    skipping--;
                    continue;
                }
                else
                {
                    row = parser.ReadFields(); // 1行読み込み
                    skipping = SkipLine;
                }

                List<double> row_data = new List<double>(row.Length);
                // 配列rowの要素は読み込んだ行の各フィールドの値
                for (int i=0; i < row.Length; i++)
                {
                    string trim = row[i].Trim();
                    if (trim == "") continue;
                    if (trim.ToLower() == "nan")
                    {
                        row_data.Add(double.NaN);
                        continue;
                    }
                    //if()
                    try
                    {
                        row_data.Add(double.Parse(row[i]));
                    }
                    catch
                    {
                        row_data.Add(0);
                    }
                }
                data.Add(row_data);
                global::System.Threading.Thread.Sleep(0);
            }

            return true;
        }


    }
}
