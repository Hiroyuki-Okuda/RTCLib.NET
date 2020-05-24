using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using System.Windows.Forms;

namespace RTCLib.Fio
{
    /// <summary>
    /// パラメータをXMLファイルから読み込み，書き込みするヘルパ．
    /// </summary>
    public static class ParameterLoader
    {
        /// <summary>
        /// XMLファイルからパラメータを読み込む
        /// </summary>
        /// <typeparam name="T">
        /// 読み込むパラメータ型．シリアライズ可能なものである必要がある．
        /// </typeparam>
        /// <param name="trg">出力引数．読み込んだデータを設定する対象．</param>
        /// <param name="fn">XMLのファイル名</param>
        static public void LoadFromXmlFile<T>(out T trg, string fn)
        {
            try
            {
                //読み込むファイルを開く
                global::System.IO.FileStream fs = new global::System.IO.FileStream(
                    fn, global::System.IO.FileMode.Open, global::System.IO.FileAccess.Read);

                //XmlSerializerオブジェクトを作成
                global::System.Xml.Serialization.XmlSerializer serializer =
                    new global::System.Xml.Serialization.XmlSerializer(typeof(T));
                //XMLファイルから読み込み、逆シリアル化する
                T obj = (T)serializer.Deserialize(fs);
                //ファイルを閉じる
                fs.Close();

                // modified 2014, Apr 30 by Okuda,
                // Is it OK??
                //trg = DeepClone<T>(obj);
                trg = obj;
            }
            catch
            {
                trg = default(T);
            }
        }

        /// <summary>
        /// Streamからパラメータを読み込む
        /// </summary>
        /// <typeparam name="T">
        /// 読み込むパラメータ型．シリアライズ可能なものである必要がある．
        /// </typeparam>
        /// <param name="trg">出力引数．読み込んだデータを設定する対象．</param>
        /// <param name="stream">読み込むStream</param>
        static public void LoadFromStream<T>(out T trg, global::System.IO.TextReader s)
        {
            try
            {
                //XmlSerializerオブジェクトを作成
                global::System.Xml.Serialization.XmlSerializer serializer =
                    new global::System.Xml.Serialization.XmlSerializer(typeof(T));
                //streamファイルから読み込み、逆シリアル化する
                T obj = (T)serializer.Deserialize(s);
                //ファイルを閉じる
                s.Close();

                // modified 2014, Apr 30 by Okuda,
                // Is it OK??
                //trg = DeepClone<T>(obj);
                trg = obj;
            }
            catch
            {
                trg = default(T);
            }
        }

        /// <summary>
        /// パラメータファイルをXMLファイルにシリアライズして書き出す．
        /// </summary>
        /// <typeparam name="T">パラメータ型．シリアライズ可能な型が必要．</typeparam>
        /// <param name="trg">書き出す対象となるパラメータクラスor構造体</param>
        /// <param name="fn">書き出すXMLファイル名</param>
        static public void WriteToXmlFile<T>(T trg, string fn)
        {
            //XmlSerializerオブジェクトを作成
            //オブジェクトの型を指定する
            global::System.Xml.Serialization.XmlSerializer serializer =
                new global::System.Xml.Serialization.XmlSerializer(typeof(T));
            try
            {
                //書き込むファイルを開く
                global::System.IO.FileStream fs = new global::System.IO.FileStream(
                    fn, global::System.IO.FileMode.Create);
                //シリアル化し、XMLファイルに保存する
                serializer.Serialize(fs, trg);
                //ファイルを閉じる
                fs.Close();
            }
            catch( Exception  )
            {
                global::System.Windows.Forms.MessageBox.Show("設定ファイル" + fn + "が書き込みモードで開けません．", "Error");
            }
        }

        /// <summary>
        /// パラメータファイルをstreamにシリアライズして書き出す．
        /// </summary>
        /// <typeparam name="T">パラメータ型．シリアライズ可能な型が必要．</typeparam>
        /// <param name="trg">書き出す対象となるパラメータクラスor構造体</param>
        /// <param name="stream">書き出すStream</param>
        static public void WriteToStream<T>(T trg, global::System.IO.TextWriter s)
        {
            //XmlSerializerオブジェクトを作成
            //オブジェクトの型を指定する
            global::System.Xml.Serialization.XmlSerializer serializer =
                new global::System.Xml.Serialization.XmlSerializer(typeof(T));

            //シリアル化し、XMLファイルに保存する
            serializer.Serialize(s, trg);
            //ファイルを閉じる
            s.Close();
        }

        static public string ParameterToString<T>(T trg)
        {
            global::System.IO.MemoryStream ms = new global::System.IO.MemoryStream();

            WriteToStream(trg, new global::System.IO.StreamWriter(ms));

            byte[] buf = ms.ToArray();
            string paramsstr = global::System.Text.Encoding.UTF8.GetString(buf);
            return paramsstr;
        }



        /// <summary>
        /// クラスのディープコピーを行う
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <returns></returns>
        private static T DeepClone<T>(this T target)
        {
            Type t = typeof(T);
            return (T)DeepClone(target, t);
        }

        /// <summary>
        /// クラスのディープコピーを行う
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="target"></param>
        /// <returns></returns>
        private static object DeepClone(this object target, Type t)
        {
            //戻り値用定義
            object ret = Activator.CreateInstance(t);

            //プロパティのコピー
            PropertyInfo[] pis = t.GetProperties();
            foreach (PropertyInfo pi in pis)
            {
                if (pi.CanRead == false) continue;
                if (pi.CanWrite == false) continue;
                //値のタイプ取得
                Type piType = pi.PropertyType;
                //target→retに値のコピー
                if ((piType.IsValueType) || (piType == typeof(string)))
                {
                    object value = pi.GetValue(target, null);
                    pi.SetValue(ret, value, null);
                }
                else if (piType.IsClass)
                {
                    object value = pi.GetValue(target, null);
                    object copy = DeepClone(value, piType);
                    pi.SetValue(ret, copy, null);
                }
                else
                {
                    throw new Exception();
                }
            }

            //フィールドのコピー
            var fis = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo fi in fis)
            {
                //値のタイプ取得
                Type fiType = fi.FieldType;
                //target→retに値のコピー
                if ((fiType.IsValueType) || (fiType == typeof(string)))
                {
                    object value = fi.GetValue(target);
                    fi.SetValue(ret, value);
                }
                else if (fiType.IsClass)
                {
                    object value = fi.GetValue(target);
                    object copy = DeepClone(value, fiType);
                    fi.SetValue(ret, copy);
                }
                else
                {
                    throw new Exception();
                }
            }

            return ret;
        }

    }
}
