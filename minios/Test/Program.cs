using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.Collections;
using System.IO;
using System.Text;


namespace Test
{
    public class Program
    {
        public static void Main()
        {
            // write your code here


        }

    }

    public class WebHeaderCollection : ICollection
    {
        private class NameValuePair
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }

        public class WebHeaderEnumerator : IEnumerator
        {
            private WebHeaderCollection headerCollection;
            private int index = -1;

            public WebHeaderEnumerator(WebHeaderCollection collection)
            {
                headerCollection = collection;
            }

            #region IEnumerator Members

            public object Current
            {
                get
                {
                    return headerCollection[index];
                }
            }

            public bool MoveNext()
            {
                index++;
                if (index < headerCollection.Count)
                {
                    return true;
                }
                else
                {
                    index = -1;
                    return false;
                }

            }

            public void Reset()
            {
                index = -1;
            }

            #endregion
        }

        private ArrayList headers = new ArrayList();
        private object syncRoot = new object();

        public string this[int index]
        {
            get { return Get(index); }
        }

        public string this[string name]
        {
            get { return Get(name); }
            set { Set(name, value); }
        }

        public string[] AllKeys
        {
            get { return (string[])GetKeys().ToArray(typeof(string)); }
        }

        public ArrayList Keys
        {
            get { return GetKeys(); }
        }

        public void Add(string name, string value)
        {
            AddWithoutValidate(name, value);
        }

        protected void AddWithoutValidate(string name, string value)
        {
            headers.Add(new NameValuePair() { Name = name, Value = value });
        }

        public void Clear()
        {
            headers.Clear();
        }

        public string Get(int index)
        {
            return ((NameValuePair)headers[index]).Value;
        }

        public string Get(string name)
        {
            for (int i = 0; i < Count; i++)
            {
                NameValuePair nvp = (NameValuePair)headers[i];
                if (nvp.Name == name)
                {
                    return nvp.Value;
                }
            }
            return null;
        }

        public string GetKey(int index)
        {
            return ((NameValuePair)headers[index]).Name;
        }

        public void Remove(string name)
        {
            for (int i = 0; i < Count; i++)
            {
                NameValuePair nvp = (NameValuePair)headers[i];
                if (nvp.Name == name)
                {
                    headers.RemoveAt(i);
                    break;
                }
            }
        }

        public void Set(string name, string value)
        {
            for (int i = 0; i < Count; i++)
            {
                NameValuePair nvp = (NameValuePair)headers[i];
                if (nvp.Name == name)
                {
                    nvp.Value = value;
                    return;
                }
            }
            Add(name, value);
        }

        private ArrayList GetKeys()
        {
            ArrayList list = new ArrayList();
            foreach (NameValuePair nvp in headers)
            {
                list.Add(nvp.Name);
            }
            return list;
        }

        #region ICollection Members

        public void CopyTo(Array array, int index)
        {
            headers.CopyTo(array, index);
        }

        public int Count
        {
            get { return headers.Count; }
        }

        public bool IsSynchronized
        {
            get { return false; }
        }

        public object SyncRoot
        {
            get { return syncRoot; }
        }

        #endregion

        #region IEnumerable Members

        public IEnumerator GetEnumerator()
        {
            return new WebHeaderEnumerator(this);
        }

        #endregion
    }
    
    public class WebResponse
    {
        public virtual WebHeaderCollection Headers
        {
            get { throw new NotImplementedException(); }
            protected set { throw new NotImplementedException(); }
        }

        public virtual string ContentType
        {
            get { throw new NotImplementedException(); }
            protected set { throw new NotImplementedException(); }
        }

        public virtual long ContentLength
        {
            get { throw new NotImplementedException(); }
            protected set { throw new NotImplementedException(); }
        }

        public virtual Uri ResponseUri
        {
            get { throw new NotImplementedException(); }
            protected set { throw new NotImplementedException(); }
        }

        public virtual void Close()
        {
            throw new NotImplementedException();
        }

        public virtual Stream GetResponseStream()
        {
            throw new NotImplementedException();
        }

    }

    public abstract class WebRequest
    {
        public virtual string Method
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual WebHeaderCollection Headers
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual string ContentType
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual long ContentLength
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual int Timeout
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public virtual Uri RequestUri
        {
            get { throw new NotImplementedException(); }
            protected set { throw new NotImplementedException(); }
        }


        public static WebRequest Create(string requestUriString)
        {
            if (requestUriString == null)
            {
                throw new ArgumentNullException("requestUriString");
            }

            return Create(new Uri(requestUriString));
        }

        public static WebRequest Create(Uri requestUri)
        {
            if (requestUri == null)
            {
                throw new ArgumentNullException("requestUri");
            }

            if (requestUri.Scheme.ToLower() == "http")
            {
                return new HttpWebRequest(requestUri);
            }

            throw new NotSupportedException("The request scheme specified in requestUri is not registered.");
        }

        public virtual WebResponse GetResponse()
        {
            throw new NotImplementedException();
        }

        public virtual Stream GetRequestStream()
        {
            throw new NotImplementedException();
        }
    }

    public class HttpWebResponse : WebResponse
    {
        private string response;

        public override long ContentLength { get; protected set; }
        public override string ContentType { get; protected set; }
        public override WebHeaderCollection Headers { get; protected set; }
        public override Uri ResponseUri { get; protected set; }
        public HttpStatusCode StatusCode { get; protected set; }

        internal HttpWebResponse(Uri responseUri)
        {
            this.ResponseUri = responseUri;
            Headers = new WebHeaderCollection();
        }

        internal void ReadStream(StreamReader reader)
        {
            string line;
            bool chunked = false;

            // Read response
            line = reader.ReadLine();
            string[] statusLine = line.Split(' ');
            if (statusLine[0].Substring(0, 4).ToUpper() != "HTTP")
            {
                throw new WebException(WebExceptionStatus.ServerProtocolViolation, this, string.Concat("Invalid reponse line received: ", line));
            }
            int statusCode = Convert.ToInt32(statusLine[1]);

            StatusCode = (HttpStatusCode)statusCode;

            // read headers 
            while (!(line = reader.ReadLine()).IsNullOrEmpty())
            {
                int index = line.IndexOf(':');
                if (index < 0)
                {
                    throw new WebException(WebExceptionStatus.ServerProtocolViolation, this, string.Concat("Invalid header received: ", line));
                }
                string name = line.Substring(0, index).Trim();
                string value = line.Substring(index + 1).Trim();
                switch (name.ToLower())
                {
                    case "content-length":
                        ContentLength = Convert.ToInt64(value);
                        break;
                    case "content-type":
                        ContentType = value;
                        break;
                    case "transfer-encoding":
                        chunked = (value.ToLower() == "chunked");
                        Headers.Add(name, value);
                        break;
                    default:
                        Headers.Add(name, value);
                        break;
                }
            }

            if (!chunked)
            {
                char[] buffer = new char[(int)ContentLength];
                int read = reader.Read(buffer, 0, (int)ContentLength);
                if (read != (int)ContentLength)
                {
                    throw new WebException(WebExceptionStatus.ReceiveFailure, this);
                }
                response = new string(buffer);
            }
            else
            {
                response = string.Empty;

                // concat lines until length is 0 (yes, this implementation is fragile,
                // but it looks like it works).  Of course, this class is a stop gap
                // messure until the real HttpWebResponse is release.
                while ((line = reader.ReadLine()).Trim()[0] != '0')
                {
                    line = reader.ReadLine();
                    response = string.Concat(response, line);
                }
            }
        }

        public override Stream GetResponseStream()
        {
            return new MemoryStream(Encoding.UTF8.GetBytes(response));
        }
    }

    public class HttpWebRequest : WebRequest
    {
        private HttpWebResponse response;
        private MemoryStream requestStream;
        private long contentLength;

        public override WebHeaderCollection Headers { get; set; }
        public override Uri RequestUri { get; protected set; }
        public override long ContentLength
        {
            get
            {
                return contentLength;
            }
            set
            {
                if (response != null)
                {
                    throw new InvalidOperationException("Request already submitted.");
                }
                if (value < 0L)
                {
                    throw new ArgumentOutOfRangeException();
                }
                contentLength = value;
            }
        }
        public override string ContentType { get; set; }
        public override string Method { get; set; }
        public override int Timeout { get; set; }
        public string UserAgent { get; set; }

        internal HttpWebRequest(Uri requestUri)
        {
            this.RequestUri = requestUri;
            Method = "GET";
            Headers = new WebHeaderCollection();
            response = null;
            requestStream = new MemoryStream();
            contentLength = -1;
        }

        public override WebResponse GetResponse()
        {
            IPHostEntry host = Dns.GetHostEntry(RequestUri.Host);
            if (host.AddressList.Length == 0)
            {
                throw new WebException(WebExceptionStatus.NameResolutionFailure, null);
            }

            int index = 0;
            while (index < host.AddressList.Length && host.AddressList[index] == null)
            {
                index++;
            }
            if (index == host.AddressList.Length)
            {
                throw new WebException(WebExceptionStatus.NameResolutionFailure, null);
            }

            IPEndPoint endpoint = new IPEndPoint(host.AddressList[index], RequestUri.Port);

            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
            {
                if (response == null)
                {
                    socket.SendTimeout = Timeout;
                    socket.ReceiveTimeout = Timeout;
                    socket.Connect(endpoint);

                    if (contentLength < 0L)
                    {
                        contentLength = requestStream.Length;
                    }

                    MemoryStream testStream = new MemoryStream();


                    using (NetworkStream netStream = new NetworkStream(socket))
                    using (StreamWriter writer = new StreamWriter(netStream))
                    using (StreamReader reader = new StreamReader(netStream))
                    {

                        // Send Request
                        writer.WriteLine(string.Concat(Method, " ", RequestUri.AbsolutePath, " HTTP/1.1"));
                        writer.Write("Host: ");
                        writer.WriteLine(RequestUri.Host);
                        if (!UserAgent.IsNullOrEmpty())
                        {
                            writer.Write("User-Agent: ");
                            writer.WriteLine(UserAgent);
                        }
                        if (!ContentType.IsNullOrEmpty())
                        {
                            writer.Write("Content-Type: ");
                            writer.WriteLine(ContentType);
                        }
                        if (ContentLength != -1)
                        {
                            writer.Write("Content-Length: ");
                            writer.WriteLine(ContentLength);
                        }
                        for (int i = 0; i < Headers.Count; i++)
                        {
                            writer.WriteLine(string.Concat(Headers.Keys[i], ": ", Headers[i]));
                        }
                        writer.WriteLine();
                        writer.Flush();

                        requestStream.Seek(0L, SeekOrigin.Begin);
                        requestStream.WriteTo(netStream);
                        //requestStream.Flush();
                        //writer.Write(requestStream.ToArray());
                        writer.WriteLine();
                        writer.Flush();

                        // Receive response
                        response = new HttpWebResponse(RequestUri);
                        response.ReadStream(reader);
                    }

                    return response;
                }
            }
            return base.GetResponse();
        }

        public override System.IO.Stream GetRequestStream()
        {
            return requestStream;
        }

        private byte[] StringToArray(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }
    }

    public enum WebExceptionStatus
    {
        Success,
        NameResolutionFailure,
        ConnectFailure,
        ReceiveFailure,
        SendFailure,
        PipelineFailure,
        RequestCanceled,
        ProtocolError,
        ConnectionClosed,
        TrustFailure,
        SecureChannelFailure,
        ServerProtocolViolation,
        KeepAliveFailure,
        Pending,
        Timeout,
        ProxyNameResolutionFailure
    }

    public class WebException : InvalidOperationException
    {
        public WebExceptionStatus Status { get; set; }
        public WebResponse Response { get; set; }

        public WebException(WebExceptionStatus status, WebResponse response)
        {
            this.Status = status;
            this.Response = response;
        }

        public WebException(WebExceptionStatus status, WebResponse response, string message)
            : base(message)
        {
            this.Status = status;
            this.Response = response;
        }

        public WebException(WebExceptionStatus status, WebResponse response, string message, Exception innerException)
            : base(message, innerException)
        {
            this.Status = status;
            this.Response = response;
        }
    }
    
    public enum HttpStatusCode
    {
        Accepted = 202,
        Ambiguous = 300,
        BadGateway = 502,
        BadRequest = 400,
        Conflict = 409,
        Continue = 100,
        Created = 201,
        ExpectationFailed = 417,
        Forbidden = 403,
        Found = 302,
        GatewayTimeout = 504,
        Gone = 410,
        HttpVersionNotSupported = 505,
        InternalServerError = 500,
        LengthRequired = 411,
        MethodNotAllowed = 405,
        Moved = 301,
        MovedPermanently = 301,
        MultipleChoices = 300,
        NoContent = 204,
        NonAuthoritativeInformation = 203,
        NotAcceptable = 406,
        NotFound = 404,
        NotImplemented = 501,
        NotModified = 304,
        OK = 200,
        PartialContent = 206,
        PaymentRequired = 402,
        PreconditionFailed = 412,
        ProxyAuthenticationRequired = 407,
        Redirect = 302,
        RedirectKeepVerb = 307,
        RedirectMethod = 303,
        RequestedRangeNotSatisfiable = 416,
        RequestEntityTooLarge = 413,
        RequestTimeout = 408,
        RequestUriTooLong = 414,
        ResetContent = 205,
        SeeOther = 303,
        ServiceUnavailable = 503,
        SwitchingProtocols = 101,
        TemporaryRedirect = 307,
        Unauthorized = 401,
        UnsupportedMediaType = 415,
        Unused = 306,
        UseProxy = 305
    }

}
