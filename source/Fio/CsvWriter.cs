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
    /// <typeparam name="BaseType">書き出すデータ型</typeparam>
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
    /// A.RegisterMember(csv);
    /// 
    /// // 書き出すためのStreamWriterを準備
    /// StreamWriter sw = new StreamWriter("file.csv");
    ///
    /// // swにデータを書き出す(リスト中のものをすべてswに書き出す)
    /// csv.OutputListedData( ref sw, data);
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

    public class CsvWriter<BaseType>
    {
        /// <summary>
        /// デリミタを設定．通常は","．
        /// </summary>
        public void SetDelimiter(string delim) { _delim = delim;  }
        

        /// <summary>
        /// 通常のメンバ変数を書き出せるように登録
        /// </summary>
        public void RegField<T>(Func<BaseType, T> acc, string label)
        {
            acc_list.Add(new CsvAccessor<T>(acc, label));
        }

        /// <summary>
        /// 配列のメンバ変数を書き出せるように登録
        /// </summary>
        public void RegFieldArray<T>(Func<BaseType, T[]> acc, int count, string label)
        {
            acc_list.Add(new CsvAccessorArray<T>(acc, count, label));
        }

        /// <summary>
        /// クラスor構造体メンバ変数を書き出せるように登録
        /// </summary>
        public void RegFieldClass<T>(
                Action<CsvWriter<T>> func,
                Func<BaseType, T> acc,
                string label = ""
            )
        {
            CsvWriter<T> csv = new CsvWriter<T>();
            string tmp = CsvWriter<T>.GetPrefix();
            CsvWriter<T>.SetPrefix(label);
            func(csv);
            CsvWriter<T>.SetPrefix(tmp);

            acc_list.Add(new CsvAccessorClass<T>(csv, acc, label));
        }

        /// <summary>
        /// クラスor構造体配列メンバ変数を書き出せるように登録
        /// </summary>
        public void RegFieldClassArray<T>(
                Action<CsvWriter<T>> func,
                Func<BaseType, T[]> acc,
                int cnt,
                string label
            )
        {
            CsvWriter<T>[] csv = new CsvWriter<T>[cnt];
            for (int i = 0; i < csv.Count(); i++)
            {
                csv[i] = new CsvWriter<T>();
                string tmp = _prefix;//CsvToString<T>.GetPrefix();
                CsvWriter<T>.SetPrefix( tmp + ConvertNumberedString(label, i) ); 
                func(csv[i]);
                CsvWriter<T>.SetPrefix(tmp);
            }
            
            acc_list.Add(new CsvAccessorClassArray<T>(csv, acc, cnt, label));
        }

        /// <summary>
        /// CSVのヘッダ行をStringBuilderに書き込み
        /// </summary>
        /// <param name="sb">書き込み先となるStringBuilder</param>
        public void OutputHeader(ref StringBuilder sb)
        {
            foreach (var tmp in acc_list)
            {
                sb.Append( tmp.GetLabel() );
            }
        }

        /// <summary>
        /// CSVのヘッダ行を文字列として取得
        /// </summary>
        /// <param name="sb">書き込み先となるStringBuilder</param>
        public string GetHeaderString()
        {
            StringBuilder sb = new StringBuilder();
            OutputHeader(ref sb);
            return sb.ToString();
        }
        
        /// <summary>
        /// Output headers for streamwriter
        /// </summary>
        public void OutputHeader()
        {
            sw.WriteLine(GetHeaderString());
        }

        /// <summary>
        /// ターゲットとなるデータに対して一行分だけStringBuilderに出力
        /// </summary>
        public void OutputOneRowData(ref StringBuilder sb, BaseType trg)
        {
            foreach (var tmp in acc_list)
            {
                tmp.GetString(trg, ref sb);
            }
        }

        /// <summary>
        /// ターゲットとなるデータに対して一行分だけ文字列として取得
        /// </summary>
        public string GetOneRowString(BaseType trg)
        {
            StringBuilder sb = new StringBuilder();
            OutputOneRowData(ref sb, trg);
            return sb.ToString();
        }

        /// <summary>
        /// Output one data row for streamwriter
        /// </summary>
        public void OutputOneRowData(BaseType trg)
        {
            if (sw != null)
            {
                try
                {
                    sw.WriteLine(GetOneRowString(trg));
                    sw.Flush();
                }
                catch { 

                }
            }
        }

        /// <summary>
        /// StringBuilderに対してすべての配列orListのものを出力
        /// </summary>
        /// <typeparam name="LT">対象データ配列orListの型(BaseTypeのIEnumerable)</typeparam>
        /// <param name="sb">書き出すStringBuilder</param>
        /// <param name="lt">データのコレクション</param>
        public void OutputListedData<ListType>(ref StringBuilder sb, ListType lt)
            where ListType : IEnumerable<BaseType>
        {
            OutputHeader(ref sb);
            foreach (var t in lt)
            {
                OutputOneRowData(ref sb, t);
                sb.AppendLine();
            }
        }

        /// <summary>
        /// StreamWriterに対してすべての配列orListのものを出力
        /// </summary>
        /// 保存したいファイル名で開いたStreamWriterに対して
        /// 出力することで，CSVファイルに書き出す．
        /// 
        /// <typeparam name="LT">対象データ配列orListの型(BaseTypeのIEnumerable)</typeparam>
        /// <param name="sb">書き出すStreamWriter</param>
        /// <param name="lt">データのコレクション</param>
        public void OutputListedData<ListType>(ref StreamWriter sw, ListType lt)
            where ListType : IEnumerable<BaseType>
        {
            TextWriter tw = (TextWriter)sw;
            OutputListedData(ref tw, lt);
        }       
 
        /// <summary>
        /// TextWriterに対してすべての配列orListのものを出力
        /// </summary>
        /// 文字列構築したい場合にも対応できるよう，
        /// TextWriterに対して対応しておく．
        /// StringWriterを使えば文字列が取得可能．
        /// 
        /// <typeparam name="LT">対象データ配列orListの型(BaseTypeのIEnumerable)</typeparam>
        /// <param name="sb">書き出すStringBuilder</param>
        /// <param name="lt">データのコレクション</param>
        public void OutputListedData<ListType>(ref TextWriter tw, ListType lt)
            where ListType : IEnumerable<BaseType>
        {
            StringBuilder sb = new StringBuilder();
            OutputHeader(ref sb);
            sb.AppendLine();

            foreach (var t in lt)
            {
                OutputOneRowData(ref sb, t);
                tw.WriteLine(sb.ToString());
                sb.Remove(0, sb.Length);
            }
            tw.Flush();
        }

        public void SetStream(Stream s)
        {
            sw = new StreamWriter(s);
        }

        /// <summary>
        /// Open output stream 
        /// </summary>
        public void OpenFileStream(string fn)
        {
            sw = new StreamWriter(fn);
        }

        public void CloseStream()
        {
            if (sw!=null)
            {
                sw.Close();
                sw.Dispose();
                sw = null;
            }
        }


        /// -------------- 以下内部・非公開部分 ---------------------

        /// アクセス用デリゲート
        private delegate Tr Accsessor<Tb, Tr>(Tb hoge);

        /// デリミタ
        private static string _delim = ",";

        /// プレフィックス
        private static string _prefix = "";

        private static void SetPrefix(string str) { _prefix = str; }
        private static string GetPrefix() { return _prefix; }

        /// アクセッサの集合
        private List<CsvAccsesorBase> acc_list = new List<CsvAccsesorBase>();

        /// ストリームも保持しておきましょう
        private StreamWriter sw = null;

        /// <summary>
        /// 数字文字列展開のための関数
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
        /// 型消去のための基底
        /// </summary>
        private abstract class CsvAccsesorBase
        {
            public abstract string GetLabel();
            public abstract void GetString(BaseType trg, ref StringBuilder sb);
        }

        private class CsvAccessor<MemType> : CsvAccsesorBase
        {
            public Accsessor<BaseType, MemType> _acc;
            public string _label;

            /// <summary>
            /// コンストラクト時にアクセッサを渡す
            /// </summary>
            public CsvAccessor(Func<BaseType, MemType> acc, string label)
            {
                _label = _prefix + label + _delim;
                _acc = new Accsessor<BaseType, MemType>(acc);
            }

            /// ラベルを返す
            public override string GetLabel()
            {
                return _label;
            }

            /// <summary>
            /// データを文字列化して渡す
            /// </summary>
            public override void GetString(BaseType trg, ref StringBuilder sb)
            {
                sb.Append((_acc(trg)).ToString()).Append(_delim);
                return;
            }
        }

        private class CsvAccessorArray<MemType> : CsvAccsesorBase
        {
            public Accsessor<BaseType, MemType[]> _acc;
            public string _label;
            public int _cnt;

            /// <summary>
            /// コンストラクト時にアクセッサを渡す
            /// </summary>
            public CsvAccessorArray(Func<BaseType, MemType[]> acc, int cnt, string label)
            {
                _cnt = cnt;
                // #が見つからなかった
                bool f = label.IndexOf("#") == -1;
                _label = "";
                for (int i = 0; i < cnt; i++)
                {
                    if (f)
                        _label += _prefix + label + _delim;
                    else
                        _label += _prefix + ConvertNumberedString(label, i) + _delim;
                }
                _acc = new Accsessor<BaseType, MemType[]>(acc);
            }

            /// ラベルを返す
            public override string GetLabel()
            {
                return _label;
            }
            
            /// <summary>
            /// データを文字列化して渡す
            /// </summary>
            public override void GetString(BaseType trg, ref StringBuilder sb)
            {
                foreach (MemType tmp in _acc(trg))
                {
                    sb.Append(tmp.ToString()).Append(_delim);
                }
                return;
            }
        }

        private class CsvAccessorClass<MemType> : CsvAccsesorBase
        {
            public Accsessor<BaseType, MemType> _acc;
            public string _label;

            CsvWriter<MemType> _csv;

            /// <summary>
            /// コンストラクト時にアクセッサを渡す
            /// </summary>
            public CsvAccessorClass(CsvWriter<MemType> csv, Func<BaseType, MemType> acc, string label="")
            {
                _prefix = _prefix + label;
                _label = label;
                _csv = csv;
                _acc = new Accsessor<BaseType, MemType>(acc);
            }

            /// ラベルを返す
            public override string GetLabel()
            {
                string ret = "";
                string pref = _prefix;
                //_prefix = _label;
                foreach (var tmpmember in _csv.acc_list)
                {
                    ret += tmpmember.GetLabel();
                }
                _prefix = pref;
                return ret;
            }

            /// <summary>
            /// データを文字列化して渡す
            /// </summary>
            public override void GetString(BaseType trg, ref StringBuilder sb)
            {
                MemType tmp = _acc(trg);
                foreach( var tmpmember in _csv.acc_list )
                {
                    tmpmember.GetString(tmp,ref sb); 
                }
                return;
            }
        }

        private class CsvAccessorClassArray<MemType> : CsvAccsesorBase
        {
            public Accsessor<BaseType, MemType[]> _acc;
            public string _label;
            public string _n_prefix;
            public int _cnt;

            CsvWriter<MemType>[] _csv;

            /// <summary>
            /// コンストラクト時にアクセッサを渡す
            /// </summary>
            public CsvAccessorClassArray(CsvWriter<MemType>[] csv, Func<BaseType, MemType[]> acc, int cnt, string label)
            {
                _cnt = cnt;
                _n_prefix = _prefix;
                _label = label;
                _csv = csv;
                _acc = new Accsessor<BaseType, MemType[]>(acc);
            }

            /// ラベルを返す
            public override string GetLabel()
            {
                string ret = "";

                // #が見つからなかった
                bool f = _n_prefix.IndexOf("#") == -1;
                for (int j = 0; j < _csv.Count(); j++)
                {
                    for (int i = 0; i < _csv[j].acc_list.Count; i++)
                    {
                        if (f)
                        {
                            ret += _csv[j].acc_list[i].GetLabel();
                        }
                        else
                        {
                            string pref = ConvertNumberedString(_n_prefix, i);
                            ret += pref + _csv[j].acc_list[i].GetLabel();
                        }
                    }
                }
                return ret;
            }

            /// <summary>
            /// データを文字列化して渡す
            /// </summary>
            public override void GetString(BaseType trg, ref StringBuilder sb)
            {
                MemType[] tmp = _acc(trg);

                for (int i = 0; i < _csv.Count(); i++)
                {
                    MemType tmpp = _acc(trg)[i];
                    foreach (var tmpmember in _csv[i].acc_list)
                    {
                        tmpmember.GetString(tmpp, ref sb);
                    }
                }
                return;
            }
        }


    }
}
