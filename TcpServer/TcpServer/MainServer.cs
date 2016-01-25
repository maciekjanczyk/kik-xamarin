using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace TcpServer
{
    public class MainServer
    {
        public const int MAX_LENGTH = 1024;
        public int Port { get; }
        List<Player> players;
        List<Game> tables; 
        TcpListener listener;
        Thread thread = null;
        List<Thread> playersThreads = null;

        void LookForClients()
        {
            while (true)
            {
                bool czyIstieje = false;
                TcpClient client = listener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();
                byte[] buffer = new byte[MAX_LENGTH];
                string[] messageData;
                do
                {
                    stream.Read(buffer, 0, buffer.Length);
                    messageData = MessageParser.Split(buffer);
                }
                while (MessageParser.ToMessageType(messageData[0]) != MessageTypes.Hello);
                foreach (Player p in players)
                {
                    if (p.Nickname == messageData[1])
                    {
                        buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.PlayerExist), "null"));
                        stream.Write(buffer, 0, buffer.Length);
                        czyIstieje = true;
                        break;
                    }
                }
                if (czyIstieje)
                    continue;
                players.Add(new Player(client, messageData[1]));
                Console.WriteLine(string.Format("Player {0} connected.", messageData[1]));
                playersThreads.Add(new Thread(new ThreadStart(PlayerThreadProcedure)));
                string serializedPlayerList = "";
                foreach (Player p in players)
                {
                    serializedPlayerList += p.Nickname + ",";
                    serializedPlayerList += string.Format("{0},{1}:", p.Wins, p.Loses);
                }
                // usuwanie niepotrzebnego ":" na koncu
                if (serializedPlayerList != "")
                    serializedPlayerList = serializedPlayerList.Remove(serializedPlayerList.Length - 1);

                buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.WelcomeClient), serializedPlayerList));
                stream.Write(buffer, 0, buffer.Length);
                buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.SendPlayers), serializedPlayerList));
                foreach (Player p in players)
                    p.client.GetStream().Write(buffer, 0, buffer.Length);
                playersThreads.Last().Start();
            }
        }

        void PlayerThreadProcedure()
        {
            Player player = null;
            foreach (Player p in players)
            {
                if (!p.haveThread)
                {
                    player = p;
                    p.haveThread = true;
                    break;
                }
            }

            string serializedPlayerList = "";

            while(player.client.Connected)
            {
                byte[] buffer = new byte[MAX_LENGTH];
                NetworkStream stream = player.client.GetStream();
                try
                { stream.Read(buffer, 0, buffer.Length); }
                catch (IOException e)
                {
                    if (player.Table != null)
                    {
                        if (players.Contains(player.Table.GetOpponent(player)))
                        {
                            buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.LeftClient), player.Nickname));
                            player.Table.GetOpponent(player).client.GetStream().Write(buffer, 0, buffer.Length);
                            player.Table.GetOpponent(player).Win();
                            player.Table.GetOpponent(player).SetFree();
                        }
                        tables.Remove(player.Table);
                    }
                    player.client.Close();
                    int k = 0;
                    for (k = 0; k < players.Count; k++)
                        if (players[k] == player)
                            break;
                    Console.WriteLine("Player {0} disconnected.", player.Nickname);
                    players.RemoveAt(k);
                    serializedPlayerList = "";
                    foreach (Player p in players)
                    {
                        serializedPlayerList += p.Nickname + ",";
                        serializedPlayerList += string.Format("{0},{1}:", p.Wins, p.Loses);
                    }
                    // usuwanie niepotrzebnego ":" na koncu
                    if (serializedPlayerList != "")
                        serializedPlayerList = serializedPlayerList.Remove(serializedPlayerList.Length - 1);
                    buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.SendPlayers), serializedPlayerList));
                    foreach (Player p in players)
                        p.client.GetStream().Write(buffer, 0, buffer.Length);
                    return;
                }
                string[] data = MessageParser.Split(buffer);
                if(data.Length == 1)
                {
                    if (player.Table != null)
                    {
                        if (players.Contains(player.Table.GetOpponent(player)))
                        {
                            buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.LeftClient), player.Nickname));
                            player.Table.GetOpponent(player).client.GetStream().Write(buffer, 0, buffer.Length);
                            player.Table.GetOpponent(player).Win();
                            player.Table.GetOpponent(player).SetFree();
                        }
                        tables.Remove(player.Table);
                    }
                    player.client.Close();
                    int k = 0;
                    for (k = 0; k < players.Count; k++)
                        if (players[k] == player)
                            break;
                    Console.WriteLine("Player {0} disconnected.", player.Nickname);
                    players.RemoveAt(k);
                    serializedPlayerList = "";
                    foreach (Player p in players)
                    {
                        serializedPlayerList += p.Nickname + ",";
                        serializedPlayerList += string.Format("{0},{1}:", p.Wins, p.Loses);
                    }
                    // usuwanie niepotrzebnego ":" na koncu
                    if (serializedPlayerList != "")
                        serializedPlayerList = serializedPlayerList.Remove(serializedPlayerList.Length - 1);
                    buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.SendPlayers), serializedPlayerList));
                    foreach (Player p in players)
                        p.client.GetStream().Write(buffer, 0, buffer.Length);
                    return;
                }
                Player opponent = null;
                buffer = new byte[MAX_LENGTH];
                Game tmp_game = null;
                switch (MessageParser.ToMessageType(data[0]))
                {
                    case MessageTypes.Invite:
                        opponent = null;
                        foreach (Player p in players)
                        {
                            if (p.Nickname == data[1])
                            {
                                opponent = p;
                                break;
                            }
                        }
                        if (opponent == null)
                            break;
                        if (opponent.IsBusy)
                        {
                            buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.PlayerIsBusy), opponent.Nickname));
                            player.client.GetStream().Write(buffer, 0, buffer.Length);
                            break;
                        }

                        buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.InviteClient), player.Nickname));
                        opponent.client.GetStream().Write(buffer, 0, buffer.Length);
                        player.client.GetStream().Write(buffer, 0, buffer.Length);
                        break;
                    case MessageTypes.Accept:
                        opponent = null;
                        foreach (Player p in players)
                        {
                            if (p.Nickname == data[1])
                            {
                                opponent = p;
                                break;
                            }
                        }
                        tmp_game = new Game(opponent, player);
                        tables.Add(tmp_game);
                        buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1},{2},{3}", Convert.ToInt32(MessageTypes.StartClient), player.Nickname, opponent.Nickname, tmp_game.WhosGotToken().Nickname));
                        opponent.client.GetStream().Write(buffer, 0, buffer.Length);
                        player.client.GetStream().Write(buffer, 0, buffer.Length);
                        opponent.SetBusy();
                        opponent.SetGame(tmp_game);
                        player.SetBusy();
                        player.SetGame(tmp_game);
                        break;
                    case MessageTypes.Decline:
                        opponent = null;
                        foreach (Player p in players)
                        {
                            if (p.Nickname == data[1])
                            {
                                opponent = p;
                                break;
                            }
                        }
                        buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.DeclineClient), player.Nickname));
                        opponent.client.GetStream().Write(buffer, 0, buffer.Length);
                        break;
                    case MessageTypes.ListRequestClient:
                        serializedPlayerList = "";
                        foreach (Player p in players)
                        {
                            serializedPlayerList += p.Nickname + ",";
                            serializedPlayerList += string.Format("{0},{1}:", p.Wins, p.Loses);
                        }
                        // usuwanie niepotrzebnego ":" na koncu
                        if (serializedPlayerList != "")
                            serializedPlayerList = serializedPlayerList.Remove(serializedPlayerList.Length - 1);
                        buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.SendPlayers), serializedPlayerList));
                        stream.Write(buffer, 0, buffer.Length);
                        break;
                    case MessageTypes.Turn:
                        if (player.Table == null)
                            break;
                        opponent = player.Table.GetOpponent(player);

                        //buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.TurnClient), data[1]));
                        //opponent.client.GetStream().Write(buffer, 0, buffer.Length);
                        player.Table.SetCanvas(data[1].ToCharArray());
                        player.SetToken(false);
                        opponent.SetToken(true);
                        
                        if (player.Table.ParseGameResult(opponent))
                        {
                            player.Win();
                            opponent.Lose();
                            buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.WinClient), player.Nickname));
                            opponent.client.GetStream().Write(buffer, 0, buffer.Length);
                            player.client.GetStream().Write(buffer, 0, buffer.Length);
                            tmp_game = player.Table;
                            player.SetGame(null);
                            opponent.SetGame(null);
                            player.SetFree();
                            opponent.SetFree();
                            tables.Remove(tmp_game);
                            serializedPlayerList = "";
                            foreach (Player p in players)
                            {
                                serializedPlayerList += p.Nickname + ",";
                                serializedPlayerList += string.Format("{0},{1}:", p.Wins, p.Loses);
                            }
                            // usuwanie niepotrzebnego ":" na koncu
                            if (serializedPlayerList != "")
                                serializedPlayerList = serializedPlayerList.Remove(serializedPlayerList.Length - 1);
                            buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.SendPlayers), serializedPlayerList));
                            foreach (Player p in players)
                                p.client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        else if (player.Table.ParseGameResult(player))
                        {
                            opponent.Win();
                            player.Lose();
                            buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.WinClient), opponent.Nickname));
                            opponent.client.GetStream().Write(buffer, 0, buffer.Length);
                            player.client.GetStream().Write(buffer, 0, buffer.Length);
                            tmp_game = player.Table;
                            player.SetGame(null);
                            opponent.SetGame(null);
                            player.SetFree();
                            opponent.SetFree();
                            tables.Remove(tmp_game);
                            serializedPlayerList = "";
                            foreach (Player p in players)
                            {
                                serializedPlayerList += p.Nickname + ",";
                                serializedPlayerList += string.Format("{0},{1}:", p.Wins, p.Loses);
                            }
                            // usuwanie niepotrzebnego ":" na koncu
                            if (serializedPlayerList != "")
                                serializedPlayerList = serializedPlayerList.Remove(serializedPlayerList.Length - 1);
                            buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.SendPlayers), serializedPlayerList));
                            foreach (Player p in players)
                                p.client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        else
                        {
                            buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.TurnClient), data[1]));
                            opponent.client.GetStream().Write(buffer, 0, buffer.Length);
                        }
                        break;
                    case MessageTypes.Left:
                        if (player.Table == null)
                            break;
                        opponent = player.Table.GetOpponent(player);                        
                        buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.LeftClient), player.Nickname));
                        opponent.client.GetStream().Write(buffer, 0, buffer.Length);
                        buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.WinClient), opponent.Nickname));
                        stream.Write(buffer, 0, buffer.Length);
                        tmp_game = player.Table;                        
                        player.SetFree();
                        player.Lose();
                        opponent.Win();
                        opponent.SetFree();
                        int k = 0;
                        for(k=0; k<tables.Count; k++)
                        {
                            if (tables[k] == tmp_game)
                            {
                                tables.RemoveAt(k);
                                break;
                            }
                        }
                        serializedPlayerList = "";
                        foreach (Player p in players)
                        {
                            serializedPlayerList += p.Nickname + ",";
                            serializedPlayerList += string.Format("{0},{1}:", p.Wins, p.Loses);
                        }
                        // usuwanie niepotrzebnego ":" na koncu
                        if (serializedPlayerList != "")
                            serializedPlayerList = serializedPlayerList.Remove(serializedPlayerList.Length - 1);
                        buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(MessageTypes.SendPlayers), serializedPlayerList));
                        foreach (Player p in players)
                            p.client.GetStream().Write(buffer, 0, buffer.Length);
                        break;
                }
            }
        }

        public MainServer(int port)
        {
            Port = port;
            players = new List<Player>();
            tables = new List<Game>();
            listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();
            thread = new Thread(new ThreadStart(LookForClients));
            playersThreads = new List<Thread>();
            thread.Start();
            Console.WriteLine("Server started on port {0}...", Port);
        }

        ~MainServer()
        {
            foreach (Player p in players)
                p.Disconnect();
            listener.Stop();
            Console.WriteLine("Server stopped.");            
        }

        public void ListPlayers()
        {
            Console.WriteLine("PLAYER LIST:");
            foreach (Player p in players)
            {
                Console.WriteLine("NICK: {0}   WINS: {1}   LOSES: {2}   BUSY: {3}", p.Nickname, p.Wins, p.Loses, p.IsBusy);                
            }
        }
    }
}
