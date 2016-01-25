using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpTestClient
{
    class Program
    {
        static void Main(string[] args)
        {
            string address = "rotfl-online.pl";
            TcpClient client = new TcpClient(address, 8081);
            NetworkStream stream = client.GetStream();            
            string str = "";
            Thread th = new Thread(new ThreadStart(() => {
                while(true)
                {
                    byte[] buff = new byte[1024];
                    stream.Read(buff, 0, buff.Length);
                    int i = 0;
                    for (i = 0; i < buff.Length; i++)
                    {
                        if (buff[i] == 0)
                            break;
                    }
                    byte[] cleanedData = new byte[i];
                    for (int j = 0; j < i; j++)
                        cleanedData[j] = buff[j];
                    Console.WriteLine(System.Text.Encoding.ASCII.GetString(cleanedData));
                }
            }));
            th.Start();
            while (str != "quit")
            {
                str = Console.ReadLine();
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(str);
                stream.Write(buffer, 0, buffer.Length);
            }
            client.Close();
        }
    }
}
