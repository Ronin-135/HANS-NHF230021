using System.IO;
using System.Net;
using System.Text;

namespace Machine
{
    public class HttpClient
    {
        #region // 字段

        private object lockGet = new object();
        private object lockPost = new object();
        private object lockPut = new object();
        private object lockDelete = new object();

        #endregion


        #region // 请求方式

        public string Get(string uri)
        {
            //创建Web访问对  象
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            //通过Web访问对象获取响应内容
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //通过响应内容流创建StreamReader对象，因为StreamReader更高级更快
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            //string returnXml = HttpUtility.UrlDecode(reader.ReadToEnd());//如果有编码问题就用这个方法
            string returnXml = reader.ReadToEnd();//利用StreamReader就可以从响应内容从头读到尾
            reader.Close();
            response.Close();
            return returnXml;
        }

        public string PostRaw(string Url, string data)
        {
            //lock (this.lockPost)
            //{
            //    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
            //    request.Method = "POST";
            //    request.ContentType = "application/json";
            //    Stream stream = request.GetRequestStream();
            //    byte[] bs = /*System.Text.*/Encoding.UTF8.GetBytes(data);
            //    stream.Write(bs, 0, bs.Length);
            //    stream.Flush();

            //    HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //    StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            //    string returnXml = sr.ReadToEnd();
            //    stream.Close();
            //    response.Close();
            //    return returnXml;
            //}

            lock (this.lockPost)
            {
                //把用户传过来的数据转成“UTF-8”的字节流
                byte[] buf = System.Text.Encoding.UTF8.GetBytes(data);
                //创建Web访问对象
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Url);
                request.Method = "POST";
                request.ContentLength = buf.Length;
                request.ContentType = "application/json";
                request.MaximumAutomaticRedirections = 1;
                request.AllowAutoRedirect = true;
                //发送请求
                Stream stream = request.GetRequestStream();
                stream.Write(buf, 0, buf.Length);
                stream.Close();

                //获取接口返回值
                //通过Web访问对象获取响应内容
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //通过响应内容流创建StreamReader对象，因为StreamReader更高级更快
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                //string returnXml = HttpUtility.UrlDecode(reader.ReadToEnd());//如果有编码问题就用这个方法
                string returnXml = reader.ReadToEnd();//利用StreamReader就可以从响应内容从头读到尾
                reader.Close();
                response.Close();
                return returnXml;
            }

        }

        public string Post(string uri, string data)
        {
            lock (this.lockPost)
            {
                //把用户传过来的数据转成“UTF-8”的字节流
                byte[] buf = System.Text.Encoding.UTF8.GetBytes(data);
                //创建Web访问对象
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
                request.Method = "POST";
                request.ContentLength = buf.Length;

                // text/xml             XML格式(text/xml忽略xml头所指定编码格式而默认采用us-ascii编码)
                // application/xml      XML数据格式(application/xml会根据xml头指定的编码格式来编码)
                // application/json
                // application/x-www-form-urlencoded
                request.ContentType = "application/x-www-form-urlencoded";
                request.MaximumAutomaticRedirections = 1;
                request.AllowAutoRedirect = true;
                request.Timeout = 5000;

                //发送请求
                Stream stream = request.GetRequestStream();
                stream.Write(buf, 0, buf.Length);
                stream.Close();

                //获取接口返回值
                //通过Web访问对象获取响应内容
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                //通过响应内容流创建StreamReader对象，因为StreamReader更高级更快
                StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
                //string returnXml = HttpUtility.UrlDecode(reader.ReadToEnd());//如果有编码问题就用这个方法
                string returnXml = reader.ReadToEnd();//利用StreamReader就可以从响应内容从头读到尾
                reader.Close();
                response.Close();
                return returnXml;
            }
        }

        public string Put(string uri, string data)
        {
            //把用户传过来的数据转成“UTF-8”的字节流
            byte[] buf = System.Text.Encoding.UTF8.GetBytes(data);
            //创建Web访问对象
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "PUT";
            request.ContentLength = buf.Length;
            request.ContentType = "application/json";
            request.MaximumAutomaticRedirections = 1;
            request.AllowAutoRedirect = true;
            request.Timeout = 5000;

            //发送请求
            Stream stream = request.GetRequestStream();
            stream.Write(buf, 0, buf.Length);
            stream.Close();

            //获取接口返回值
            //通过Web访问对象获取响应内容
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //通过响应内容流创建StreamReader对象，因为StreamReader更高级更快
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            //string returnXml = HttpUtility.UrlDecode(reader.ReadToEnd());//如果有编码问题就用这个方法
            string returnXml = reader.ReadToEnd();//利用StreamReader就可以从响应内容从头读到尾
            reader.Close();
            response.Close();
            return returnXml;

        }

        public string Delete(string uri, string data)
        {
            //把用户传过来的数据转成“UTF-8”的字节流
            byte[] buf = System.Text.Encoding.UTF8.GetBytes(data);
            //创建Web访问对象
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            request.Method = "DELETE";
            request.ContentLength = buf.Length;
            request.ContentType = "application/json";
            request.MaximumAutomaticRedirections = 1;
            request.AllowAutoRedirect = true;
            request.Timeout = 5000;

            //发送请求
            Stream stream = request.GetRequestStream();
            stream.Write(buf, 0, buf.Length);
            stream.Close();

            //获取接口返回值
            //通过Web访问对象获取响应内容
            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            //通过响应内容流创建StreamReader对象，因为StreamReader更高级更快
            StreamReader reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            //string returnXml = HttpUtility.UrlDecode(reader.ReadToEnd());//如果有编码问题就用这个方法
            string returnXml = reader.ReadToEnd();//利用StreamReader就可以从响应内容从头读到尾
            reader.Close();
            response.Close();
            return returnXml;

        }

        #endregion
    }
}