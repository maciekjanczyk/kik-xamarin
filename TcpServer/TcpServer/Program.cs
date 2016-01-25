using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

public class SimpleTcpSrvr
{    
    public static void Main()
    {
        TcpServer.MainServer server = new TcpServer.MainServer(8081);
        string cmd = "";
        while (true)
        {
            cmd = Console.ReadLine();
            switch(cmd)
            {
            case "list_players":
                server.ListPlayers();
                break;
            }
        }
    }
}
