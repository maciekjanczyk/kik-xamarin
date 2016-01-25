using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer
{
    public class Utilities
    {
        public static void OldMain()
        {
            TcpListener server = new TcpListener(IPAddress.Any, 8081);
            server.Start();
            Console.WriteLine("Nasluchuje...");
            TcpClient client = server.AcceptTcpClient();
            Console.WriteLine("Mam!");
            NetworkStream stream = client.GetStream();
            while (true)
            {
                byte[] buffers = new byte[1024];
                stream.Read(buffers, 0, 1024);
                if (buffers.Length != 0)
                {
                    stream.Write(buffers, 0, 1024);
                    Console.WriteLine(System.Text.Encoding.ASCII.GetString(buffers));
                }
            }
        }
    }
}
