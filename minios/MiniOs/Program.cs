using Microsoft.SPOT.Hardware;
using SecretLabs.NETMF.Hardware.NetduinoPlus;
using System.IO;

namespace MiniOS
{
    public class Program
    {
        static OutputPort led = new OutputPort(Pins.ONBOARD_LED, false);

        public static void Main()
        {
            // write a simple upload form to the web server dir... :)
            //Directory.CreateDirectory(@"\SD\webserver");
            //TextWriter str = new StreamWriter(@"\SD\webserver\upload.html");
            //string content = "<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\"><html xmlns=\"http://www.w3.org/1999/xhtml\" ><head><title>Upload files</title></head><body><form enctype=\"multipart/form-data\" action=\"\" method=\"post\">file senden: <input name=\"thefile\" type=\"file\" /><input type=\"submit\" value=\"senden\" /></form></body></html>";
            //str.Write(content);
            //str.Flush();
            //str.Close();

            // initialize the device class with the number of components
            using (Device dev = new Device(2, @"\SD\system.log"))
            {
                // register components
                dev.RegisterComponent(typeof(Webserver), led);
                //dev.RegisterComponent(typeof(Ftpserver), led);

                // start the device
                dev.Start();

                while (true)
                {

                }
            }
        }
    }
}

