using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;

namespace RTCLib.Fio
{
    /// <summary>
    /// CSVを書き出すクラス．書き出したいメンバだけ登録する．
    /// </summary>
    /// <typeparam name="TBaseType">書き出すデータ型</typeparam>
    /// 
    /// // 以下使い方．
    /// <code>
    /// // データがあるとする
    /// List<A> data = new List<A>();
    /// ...
    /// 
    /// // インスタンスの作成
    /// CsvWriter<A> csv = new CsvWriter<A>();
    /// 
    /// // 書き出しメンバ登録関数を呼び出し．数が少なければ
    /// // 直接csvインスタンス対してRegField***を呼びだしても良い．
    /// csv.RegField((A o) => o.a, "a");
    /// A.RegisterMember(csv);  // 構造体と一緒に定義しておくとわかりやすいので
    /// 
    /// // 書き出すためのStreamWriterを準備
    /// StreamWriter streamWriter = new StreamWriter("file.csv");
    ///
    /// // swにデータを書き出す(リスト中のものをすべてswに書き出す)
    /// csv.OutputAllData( ref streamWriter, data);
    ///
    /// // ユーザが個別にデータを書き出す
    /// // StreamWriter等に出す場合に，任意のタイミングでフラッシュしたい場合等
    /// StringBuilder sb = new StringBuilder();
    /// csv.OutputHeader( ref sb );
    /// foreach (var tt in data)
    /// {
    ///     csv.OutputOneRowData(ref sb, tt);
    ///     Console.Write( sb.ToString() );
    ///     sb.Remove(0, sb.Length);
    ///     Console.WriteLine();
    /// }
    /// </code>
    /// 
    /// 
    /// <code>
    /// // 以下はメンバの登録の仕方の例．
    /// class A 
    /// {
    ///     public int a = 1;
    ///     public float aa = 3.7f;
    ///     public int[] aaa = {0, 1, 2, 3 };
    ///     public B[] a_b = new B[10];
    /// 
    ///     static public void RegisterMember(CsvWriter<A> a)
    ///     {
    ///         // 通常のフィールドの登録
    ///         a.RegField((A o) => o.a, "a");
    ///         
    ///         // 配列タイプのフィールドの登録，
    ///         // ラベル書き出し時の要素数は事前登録が必要(下では4)
    ///         // 実際のデータは，ここでの要素数に関係なく，配列の要素数分だけ書き出すため
    ///         // 実際とここでの指定が異なるとcsvの列不整合となる．
    ///         // 配列のラベルに"#"を含む場合，これで自動的に番号に展開．
    ///         a.RegFieldArray((A o) => o.aaa, "aaa_#", 4);
    ///         
    ///         // 構造体かクラスのフィールドの登録
    ///         // 以下ではクラスBの静的メソッド B.RegisterMemberを使って
    ///         // メンバーを登録する，ということを指定．
    ///         // 書き出したクラスのラベルには"ab_"のプレフィックスが自動で付加
    ///         a.RegFieldClass( B.RegisterMember, (A o) => o.a_b, "ab_");//
    /// 
    ///         // 構造体かクラスの配列のフィールドの登録(コレクションには未対応)
    ///         // 上記のRegFieldClassの配列版．
    ///         a.RegFieldClassArray(C.RegisterMember, (A t) => t.ac, 10, "ac#_");
    ///     }
    /// }
    /// 
    /// class B 
    /// {
    ///     public int b = 33;
    ///     public double[] bb = {88.888, 99.999 } ;
    ///     public C[] b_c;
    /// 
    ///     // 子クラスにも，下記のように登録用メソッドを準備する．
    ///     // もちろんクラス外にメソッドを置いても静的メソッドならOK．
    ///     static public void RegisterMember(CsvWriter<B> a)
    ///     {
    ///         a.RegField((B o) => o.b);
    ///         a.RegFieldArray((B o) => o.bb);
    /// 
    ///         //return 0;
    ///     }
    /// }
    /// </code>

    public class CsvWriter<TBaseType>
    {
        /// <summary>
        /// Set delimiter. default is ','．
        /// </summary>
        public void SetDelimiter(string delimiter) { _delimiter = delimiter;  }


        /// <summary>
        /// Register member accessor for embedded type to output 
        /// </summary>
        /// <typeparam name="T">Field type</typeparam>
        /// <param name="accessor">Lambda to access member variable</param>
        /// <param name="label">Label for member variable</param>
        public void RegField<T>(Func<TBaseType, T> accessor, string label)
        {
            if (string.IsNullOrEmpty(_prefix)) label = _prefix + "." + label;
            _accList.Add(new CsvAccessor<T>(accessor, label));
        }

        /// <summary>
        /// Register array member for embedded type to output 
        /// </summary>
        public void RegFieldArray<T>(Func<TBaseType, T[]> accessor, int count, string label)
        {
            if (string.IsNullOrEmpty(_prefix)) label = _prefix + "." + label;
            _accList.Add(new CsvAccessorArray<T>(accessor, count, label));
        }

        /// <summary>
        /// Register member for class/struct to output
        /// </summary>
        public void RegFieldClass<T>(
                Action<CsvWriter<T>> function,
                Func<TBaseType, T> accessor,
                string label = ""
            )
        {
            CsvWriter<T> csv = new CsvWriter<T>();
            // string tmp = GetPrefix();
            csv.SetPrefix(label);
            function?.Invoke(csv);
            // csv.SetPrefix(tmp);

            _accList.Add(new CsvAccessorClass<T>(csv, accessor, label));
        }

        /// <summary>
        /// Register array member for class/struct to output
        /// </summary>
        public void RegFieldClassArray<T>(
                Action<CsvWriter<T>> func,
                Func<TBaseType, T[]> acc,
                int cnt,
                string label
            )
        {
            CsvWriter<T>[] csv = new CsvWriter<T>[cnt];
            for (int i = 0; i < csv.Length; i++)
            {
                csv[i] = new CsvWriter<T>();
                string tmp = _prefix;//CsvToString<T>.GetPrefix();
                csv[i].SetPrefix( tmp + ConvertNumberedString(label, i) ); 
                func(csv[i]);
                // CsvWriter<T>.SetPrefix(tmp);
            }
            
            _accList.Add(new CsvAccessorClassArray<T>(csv, acc, cnt, label));
        }

        /// <summary>
        /// Output CSV tags as a header to StringBuilder
        /// </summary>
        /// <param name="sb">targeting StringBuilder</param>
        public void OutputHeader(ref StringBuilder sb)
        {
            if(sb==null)return;
            foreach (var tmp in _accList)
            {
                sb.Append( tmp.GetLabel() );
            }
        }

        /// <summary>
        /// Get CSV header string
        /// </summary>
        public string GetHeaderString()
        {
            StringBuilder sb = new StringBuilder();
            OutputHeader(ref sb);
            return sb.ToString();
        }
        
        /// <summary>
        /// Output headers for StreamWriter
        /// </summary>
        public void OutputHeader(ref StreamWriter streamWriter)
        {
            streamWriter?.WriteLine(GetHeaderString());
        }

        /// <summary>
        /// Output target data to StringBuilder as one-line string
        /// </summary>
        public void OutputOneRowData(ref StringBuilder sb, TBaseType trg)
        {
            foreach (var tmp in _accList)
            {
                tmp.GetString(trg, ref sb);
            }
        }

        /// <summary>
        /// Get a line string of target data
        /// </summary>
        public string GetOneRowString(TBaseType trg)
        {
            StringBuilder sb = new StringBuilder();
            OutputOneRowData(ref sb, trg);
            return sb.ToString();
        }

        /// <summary>
        /// Output one data row for StreamWriter
        /// </summary>
        public void OutputOneRowData(TBaseType trg, ref StreamWriter streamWriter)
        {
            streamWriter?.WriteLine(GetOneRowString(trg));
            // streamWriter.Flush();
        }

        /// <summary>
        /// Output all data in collection to StringBuilder
        /// </summary>
        /// <typeparam name="TListType">Collection type (with IEnumerable&lt;BaseType&gt;)</typeparam>
        /// <param name="sb">StringBuilder to output</param>
        /// <param name="lt">Collection of target data type(TBaseType)</param>
        public void OutputAllData<TListType>(ref StringBuilder sb, TListType lt)
            where TListType : IEnumerable<TBaseType>
        {
            if(sb==null)return;
            OutputHeader(ref sb);
            foreach (var t in lt)
            {
                OutputOneRowData(ref sb, t);
                sb.AppendLine();
            }
        }

        /// <summary>
        /// Output all data in collection to StreamWriter
        /// </summary>
        /// Write all data to StreamWriter to make CSV file
        /// 
        /// <typeparam name="TListType">Collection type (with IEnumerable&lt;BaseType&gt;)</typeparam>
        /// <param name="streamWriter">StreamWriter to output</param>
        /// <param name="lt">Collection of target data type(TBaseType)</param>
        public void OutputAllData<TListType>(ref StreamWriter streamWriter, TListType lt)
            where TListType : IEnumerable<TBaseType>
        {
            if(streamWriter==null)return;
            StringBuilder sb = new StringBuilder();
            OutputAllData(ref sb, lt);
            streamWriter.Write(sb.ToString());
        }

        /// <summary>
        /// Output all data in collection to default StreamWriter 
        /// </summary>
        /// Write all data to StreamWriter to make CSV file
        /// 
        /// <typeparam name="TListType">Collection type (with IEnumerable&lt;BaseType&gt;)</typeparam>
        /// <param name="lt">Collection of target data type(TBaseType)</param>
        public void OutputAllData<TListType>(TListType lt)
            where TListType : IEnumerable<TBaseType>
        {
            OutputAllData(ref _streamWriter, lt);
        }

        /// <summary>
        /// Set stream to output
        /// </summary>
        /// <param name="s"></param>
        public void SetStream(Stream s)
        {
            _streamWriter = new StreamWriter(s);
        }

        /// <summary>
        /// Open output stream 
        /// </summary>
        public void OpenFileStream(string fn)
        {
            _streamWriter = new StreamWriter(fn);
        }

        public void CloseStream()
        {
            if (_streamWriter!=null)
            {
                _streamWriter.Close();
                _streamWriter.Dispose();
                _streamWriter = null;
            }
        }


        /// -------------- 以下内部・非公開部分 ---------------------

        /// Accessor delegate
        private delegate Tr Accessor<Tb, Tr>(Tb hoge);

        /// Delimiter to output
        private string _delimiter = ",";

        /// Prefix
        private string _prefix = "";

        private void SetPrefix(string str) { _prefix = str; }
        private string GetPrefix() { return _prefix; }

        /// アクセッサの集合
        private List<CsvAccessorBase> _accList = new List<CsvAccessorBase>();

        /// ストリームも保持しておきましょう
        private StreamWriter _streamWriter = null;

        /// <summary>
        /// Numbered string from # template
        /// </summary>
        private static string ConvertNumberedString(string template, int i)
        {
            global::System.Text.RegularExpressions.Regex r =
                new global::System.Text.RegularExpressions.Regex(@"#+");
            global::System.Text.RegularExpressions.Match m = r.Match(template);

            if (m.Length == 0) return template;

            string num_str_fmt = m.Value.Replace("#", "0");
            string num_str = i.ToString(num_str_fmt);
            return template.Replace(m.Value, num_str);
        }



        /// <summary>
        /// Accessor interface
        /// </summary>
        /// <remarks>Consider use interface instead of abstract class</remarks>
        private abstract class CsvAccessorBase
        {
            public abstract string GetLabel();
            public abstract void GetString(TBaseType trg, ref StringBuilder sb);
        }

        private class CsvAccessor<TMemType> : CsvAccessorBase
        {
            public Accessor<TBaseType, TMemType> Acc;
            public string Label;

            /// <summary>
            /// Create an accessor with label
            /// </summary>
            public CsvAccessor(Func<TBaseType, TMemType> acc, string label)
            {
                Label = label;
                Acc = new Accessor<TBaseType, TMemType>(acc);
            }

            /// <summary>
            /// Get label
            /// </summary>
            /// <returns></returns>
            public override string GetLabel()
            {
                return Label;
            }

            /// <summary>
            /// Get string converted from data
            /// </summary>
            public override void GetString(TBaseType trg, ref StringBuilder sb)
            {
                sb.Append((Acc(trg)).ToString()).Append(_delimiter);
                return;
            }
        }

        private class CsvAccessorArray<TMemType> : CsvAccessorBase
        {
            public Accessor<TBaseType, TMemType[]> Acc;
            public string Label;
            public int Cnt;

            /// <summary>
            /// Create accessor for array 
            /// </summary>
            public CsvAccessorArray(Func<TBaseType, TMemType[]> acc, int cnt, string label)
            {
                Cnt = cnt;
                // #が見つからなかった
                bool f = label.IndexOf("#") == -1;
                Label = "";
                for (int i = 0; i < cnt; i++)
                {
                    if (f)
                        Label += _prefix + ConvertNumberedString(label+"_#", i) + _delimiter;
                    else
                        Label += _prefix + ConvertNumberedString(label, i) + _delimiter;
                }
                Acc = new Accessor<TBaseType, TMemType[]>(acc);
            }

            /// Get label
            public override string GetLabel()
            {
                return Label;
            }
            
            /// <summary>
            /// Get string converted from data
            /// </summary>
            public override void GetString(TBaseType trg, ref StringBuilder sb)
            {
                foreach (var tmp in Acc(trg))
                {
                    sb.Append(tmp.ToString()).Append(_delimiter);
                }
                return;
            }
        }

        private class CsvAccessorClass<TMemType> : CsvAccessorBase
        {
            public Accessor<TBaseType, TMemType> Acc;
            public string Label;

            private readonly CsvWriter<TMemType> _csv;

            /// <summary>
            /// Create accessor to class/struct data
            /// </summary>
            public CsvAccessorClass(CsvWriter<TMemType> csv, Func<TBaseType, TMemType> acc, string label="")
            {
                _prefix = _prefix + label;
                Label = label;
                _csv = csv;
                Acc = new Accessor<TBaseType, TMemType>(acc);
            }

            /// Get label
            public override string GetLabel()
            {
                string ret = "";
                string pref = _prefix;
                //_prefix = _label;
                foreach (var tmpmember in _csv._accList)
                {
                    ret += tmpmember.GetLabel();
                }
                _prefix = pref;
                return ret;
            }

            /// <summary>
            /// Get string converted from data
            /// </summary>
            public override void GetString(TBaseType trg, ref StringBuilder sb)
            {
                TMemType tmp = Acc(trg);
                foreach( var temp in _csv._accList )
                {
                    temp.GetString(tmp,ref sb); 
                }
                return;
            }
        }

        private class CsvAccessorClassArray<TMemType> : CsvAccessorBase
        {
            public Accessor<TBaseType, TMemType[]> _acc;
            public string _label;
            public string _n_prefix;
            public int _cnt;

            CsvWriter<TMemType>[] _csv;

            /// <summary>
            /// Create accessor to array of class/struct 
            /// </summary>
            public CsvAccessorClassArray(CsvWriter<TMemType>[] csv, Func<TBaseType, TMemType[]> acc, int cnt, string label)
            {
                _cnt = cnt;
                _n_prefix = _prefix;
                _label = label;
                _csv = csv;
                _acc = new Accessor<TBaseType, TMemType[]>(acc);
            }

            /// Get label extending template
            public override string GetLabel()
            {
                string ret = "";

                // #が見つからなかった
                bool f = _n_prefix.IndexOf("#") == -1;
                for (int j = 0; j < _csv.Count(); j++)
                {
                    for (int i = 0; i < _csv[j]._accList.Count; i++)
                    {
                        if (f)
                        {
                            ret += _n_prefix +_csv[j]._accList[i].GetLabel();
                        }
                        else
                        {
                            string pref = ConvertNumberedString(_n_prefix, i);
                            ret += pref + _csv[j]._accList[i].GetLabel();
                        }
                    }
                }
                return ret;
            }

            /// <summary>
            /// Get string converted from data
            /// </summary>
            public override void GetString(TBaseType trg, ref StringBuilder sb)
            {
                TMemType[] tmp = _acc(trg);

                for (int i = 0; i < _csv.Count(); i++)
                {
                    TMemType tmpp = _acc(trg)[i];
                    foreach (var tmpmember in _csv[i]._accList)
                    {
                        tmpmember.GetString(tmpp, ref sb);
                    }
                }
                return;
            }
        }


    }
}
