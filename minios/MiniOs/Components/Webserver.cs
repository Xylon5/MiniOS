using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;
using System.Collections;
using NetMf.CommonExtensions;

namespace MiniOS
{
    internal class Webserver : IDisposable, IComponent
    {
        private string localpath = @"\SD\webserver\";
        private string localcache = @"\SD\cache";
        private OutputPort led { get; set; }
        private const string COMPONENTNAME = "Webserver";
        private Socket sck { get; set; }
        public DeviceComponentType StartOnComponentReady { get { return DeviceComponentType.Network; } }
        public bool isRunning { get; set; }

        protected Hashtable _getArguments = new Hashtable();

        public Webserver(OutputPort led)
        {
            Device.LogMessage(COMPONENTNAME, "Initializing web server");
            this.led = led;
            Directory.CreateDirectory(this.localcache);
        }

        private void ListenForRequests()
        {
            Device.LogMessage(COMPONENTNAME, "Listening for requests");
            while (true)
            {
                using (Socket clientSocket = sck.Accept())
                {
                    //string cachedRequestFile = CacheRequest(clientSocket);
                    //ProcessRequest(request, clientSocket);

                    //Get clients IP
                    IPEndPoint clientIP = clientSocket.RemoteEndPoint as IPEndPoint;
                    EndPoint clientEndPoint = clientSocket.RemoteEndPoint;
                    //int byteCount = cSocket.Available;
                    int bytesReceived = clientSocket.Available;
                    if (bytesReceived > 0)
                    {
                        //Get request
                        byte[] buffer = new byte[bytesReceived];
                        int byteCount = clientSocket.Receive(buffer, bytesReceived, SocketFlags.None);
                        string request = new string(Encoding.UTF8.GetChars(buffer));
                        Device.LogMessage(COMPONENTNAME, request);
                        
                        ProcessRequest(request, clientSocket);
                    }
                }
            }

        }

        private string CacheRequest(Socket clientSocket)
        {
            string fname = this.localcache + "\req_" + Guid.NewGuid().ToString().Replace("-", "");
            FileStream cache = new FileStream(fname, FileMode.CreateNew);
            byte[] buffer = new byte[2048];
            while (clientSocket.Receive(buffer) > 0)
            {
                cache.Write(buffer, 0, buffer.Length);
            }
            cache.Close();
            return fname;
        }

        private void ProcessRequest(string request, Socket clientSocket)
        {
            string firstLine = request.Substring(0, request.IndexOf('\n'));
            string[] firstLineWords = firstLine.Split(' ');
            string _method = firstLineWords[0];
            string[] urlAndGets = firstLineWords[1].Split('?');
            string url = urlAndGets[0].Substring(1); // Substring to ignore the '/'

            if (urlAndGets.Length > 1)
            {
                FillGETHashtable(urlAndGets[1]);
            }

            byte[] response;
            if (url == "RebootDevice")
            {
                response = Encoding.UTF8.GetBytes("Rebooting device. Please stay tuned :)");
                string header = StringUtility.Format("HTTP/1.0 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {0}\r\nConnection: close\r\n\r\n", response.Length);
                clientSocket.Send(Encoding.UTF8.GetBytes(header), header.Length, SocketFlags.None);
                clientSocket.Send(response, response.Length, SocketFlags.None);
                clientSocket.Close();
                
                Device.RequestDeviceReboot(COMPONENTNAME, "Requested by remote user");
            }
            else if (url == "logfile")
            {
                Device.CloseLogfile();
                StreamFileToClient(@"\SD\system.log", clientSocket);
                Device.OpenLogfile();
            }
            else if (IsFileUpload(request))
            {
                CacheRequest(clientSocket);

                //string fname = string.Empty;
                //byte[] buffer = new byte[2048];
                //clientSocket.Receive(buffer);
                //string tmp = new string(Encoding.UTF8.GetChars(buffer));
                //Debug.GC(true);
                //int start = tmp.IndexOf("filename=\"");
                //int end = tmp.IndexOf("\"", start);
                //fname = tmp.Substring(start, end - start);
                //FileStream str = new FileStream(localpath, FileMode.Open);
                //byte[] fbuffer = new byte[4096];
                //while (clientSocket.Receive(fbuffer) > 0)
                //{
                //    str.Write(fbuffer, 0, buffer.Length);
                //}
                //str.Close();
                response = Encoding.UTF8.GetBytes("Done.");
                string header = StringUtility.Format("HTTP/1.0 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {0}\r\nConnection: close\r\n\r\n", response.Length);
                clientSocket.Send(Encoding.UTF8.GetBytes(header), header.Length, SocketFlags.None);
                clientSocket.Send(response, response.Length, SocketFlags.None);
            }
            else if (url == "ls")
            {
                DirectoryInfo dir = new DirectoryInfo(@"\SD\");
                string resp = "<table><thead><th>Filename</th><th>Filesize</th></thead>";
                resp += GetDir(dir);
                resp += "</table>";
                response = Encoding.UTF8.GetBytes(resp);
                string header = StringUtility.Format("HTTP/1.0 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {0}\r\nConnection: close\r\n\r\n", response.Length);
                clientSocket.Send(Encoding.UTF8.GetBytes(header), header.Length, SocketFlags.None);
                clientSocket.Send(response, response.Length, SocketFlags.None);
            }
            else
            {
                string mappedfile = localpath + url;
                if (!File.Exists(mappedfile))
                {
                    response = Encoding.UTF8.GetBytes("url '" + url + "' does not exist on this server!");
                    string header = StringUtility.Format("HTTP/1.0 404 NOT FOUND\r\nContent-Type: text/html; charset=utf-8\r\nContent-Length: {0}\r\nConnection: close\r\n\r\n", response.Length);
                    clientSocket.Send(Encoding.UTF8.GetBytes(header), header.Length, SocketFlags.None);
                    clientSocket.Send(response, response.Length, SocketFlags.None);
                    
                }
                else
                {
                    StreamFileToClient(mappedfile, clientSocket);
                }
            }
        }

        private string GetDir(DirectoryInfo dir)
        {
            string resp = string.Empty;
            resp += StringUtility.Format("<tr><td colspan='2'>{0}</td></tr>", dir.FullName);
            foreach (FileInfo file in dir.GetFiles())
            {
                resp += StringUtility.Format("<tr><td><a href='{2}'>{0}</a></td><td>{1}</td></tr>", file.Name, file.Length, file.FullName.TrimStart(localpath.ToCharArray()));
            }
            foreach (DirectoryInfo subdir in dir.GetDirectories())
            {
                resp += GetDir(subdir);
            }
            return resp;
        }

        private void FillGETHashtable(string url)
        {
            _getArguments = new Hashtable();

            string[] urlArguments = url.Split('&');
            string[] keyValuePair;

            for (int i = 0; i < urlArguments.Length; i++)
            {
                keyValuePair = urlArguments[i].Split('=');
                _getArguments.Add(keyValuePair[0], keyValuePair[1]);
            }
        }

        private bool IsFileUpload(string request)
        {
            // quick hack... not very robust :>
            if (request.IndexOf("Content-Type: multipart/form-data") > 0) return true;
            else return false;
        }

        private void StreamFileToClient(string filename, Socket client)
        {
            SendHeaderToClient(filename, client);
            
            // use 75% of available ram for buffer
            // do not use this with .net MF 4.1 .... the garbage collector has two serious bugs -.-
            //uint buffersize = (uint)System.Math.Round(Device.AvailableMemory() * 0.25);
            
            // 2kb buffer :)
            // this buffer size can later on be set to the maximum amount of available ram...
            byte[] buffer = new byte[2048];            
            FileStream file = new FileStream(filename, FileMode.Open);
            while (file.Read(buffer, 0, buffer.Length) > 0)
            {
                try
                {
                    client.Send(buffer);
                }
                catch (SocketException ex)
                {
                    Device.LogMessage(COMPONENTNAME, "SocketException, ErrorCode: {0}\tClient:{1}\tFilename:{2}", ex.ErrorCode, ((IPEndPoint)client.RemoteEndPoint).Address, filename);
                }
            }
            file.Close();            
        }

        private void SendHeaderToClient(string filename, Socket client)
        {
            FileInfo fi =new FileInfo(filename);
            string ext = fi.Extension.TrimStart(new char[] { '.' });
            long length = fi.Length;
            string ctype = GetContentTypeFromFileExtension(ext);
            string header = StringUtility.Format("HTTP/1.0 200 OK\r\nContent-Type: {0}; charset=utf-8\r\nContent-Length: {1}\r\n", ctype, length);
            if (ctype == "application/octet-stream")
            {
                header += StringUtility.Format("Content-Disposition: attachment; filename={0}\r\n", fi.Name);
            }
            header += "Connection: close\r\n\r\n";

            client.Send(Encoding.UTF8.GetBytes(header), header.Length, SocketFlags.None);
        }

        private string GetContentTypeFromFileExtension(string ext)
        {
            string ctype = "application/octet-stream";
            switch (ext)
            {
                case "htm": ctype = "text/html"; break;
                case "html": ctype = "text/html"; break;

                case "gif": ctype = "image/gif"; break;
                case "jpeg": ctype = "image/jpeg"; break;
                case "jpg": ctype = "image/jpeg"; break;
                case "png": ctype = "image/png"; break;

                case "pdf": ctype = "application/pdf"; break;
                case "js": ctype = "application/javascript"; break;
            }
            return ctype;
        }

        public void Dispose()
        {
            Device.LogMessage(COMPONENTNAME, "Shutting down web server");
            if (sck != null) sck.Close();
            this.isRunning = false;
        }

        public void StartInstance(params object[] parameters)
        {
            if (this.isRunning == true) return;
            Device.LogMessage(COMPONENTNAME, "Starting web server");
            Socket sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sck.Bind(new IPEndPoint(IPAddress.Any, 80));
            sck.Listen(2);
            this.sck = sck;
            this.isRunning = true;
            ListenForRequests();

        }
    }
}
