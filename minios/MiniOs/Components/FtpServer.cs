using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using Microsoft.SPOT.Net;
using System.Text;
using System.IO;

namespace MiniOS
{
    internal class Ftpserver : IDisposable, IComponent
    {
        private OutputPort led { get; set; }
        private const string COMPONENTNAME = "Ftpserver";
        public DeviceComponentType StartOnComponentReady { get { return DeviceComponentType.Network; } }

        public bool isRunning { get; set; }

        public Ftpserver(OutputPort led)
        {
            Device.LogMessage(COMPONENTNAME, "Initializing ftp server");
            this.led = led;
        }

        private void ListenForRequests(Socket sck)
        {
            Device.LogMessage(COMPONENTNAME, "Welcome to the FTP Service.");
            while (true)
            {
                using (Socket client = sck.Accept())
                {
                    Device.LogMessage(COMPONENTNAME, "New session requested");
                    RunSession(client);
                }
            }
        }

        private void RunSession(Socket client)
        {
            DirectoryInfo dir = new DirectoryInfo(@"\SD\");
            string rootdir = string.Empty;
            foreach (FileInfo file in dir.GetFiles())
            {
                rootdir += file.FullName + "\r\n";
            }
            client.Send(Encoding.UTF8.GetBytes(rootdir));

            ////Get clients IP
            //byte[] rbuffer = new byte[2048];
            //string c = string.Empty;
            //while (client.Receive(rbuffer) > 0)
            //{
            //    c += Encoding.UTF8.GetChars(rbuffer).ToString();
            //}

            //IPEndPoint clientIP = client.RemoteEndPoint as IPEndPoint;
            //EndPoint clientEndPoint = client.RemoteEndPoint;
            ////int byteCount = cSocket.Available;
            //int bytesReceived = client.Available;
            //if (bytesReceived > 0)
            //{
            //    //Get request
            //    byte[] buffer = new byte[bytesReceived];
            //    int byteCount = client.Receive(buffer, bytesReceived, SocketFlags.None);
            //    string request = new string(Encoding.UTF8.GetChars(buffer));
            //    Device.LogMessage(COMPONENTNAME, request);

            //    string response = ProcessRequest(request);
            //    //TODO: irgendwie hakts hier... keine Ahnung was kaputt ist, aber der Response kommt net an o_O
            //    client.Send(Encoding.UTF8.GetBytes(response), response.Length, SocketFlags.None);
            //    //Blink the onboard LED
            //    led.Write(true);
            //    Thread.Sleep(150);
            //    led.Write(false);
            //}

        }

        private string ProcessRequest(string request)
        {

            string response = "200 OK";
            return response;
        }

        public void Dispose()
        {
            Device.LogMessage(COMPONENTNAME, "Shutting down ftp server");
            //if (sck != null) sck.Close();
            this.isRunning = false;
        }

        public void StartInstance(params object[] parameters)
        {
            if (this.isRunning == true) return;
            Device.LogMessage(COMPONENTNAME, "Starting ftp server");
            Socket sck = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            sck.Bind(new IPEndPoint(IPAddress.Any, 21));
            sck.Listen(2);
            this.isRunning = true;
            ListenForRequests(sck);

        }
    }
}
