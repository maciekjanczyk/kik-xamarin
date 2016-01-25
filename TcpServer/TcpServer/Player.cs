using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer
{    
    public class Player
    {
        public TcpClient client;
        private NetworkStream stream;
        public int Wins { get; private set; }
        public int Loses { get; private set; }
        public string Nickname { get; private set; }
        public bool haveThread = false;
        public bool IsBusy { get; private set; }
        public Game Table { get; private set; }
        public bool CanTurn { get; private set; }
        public char Sign { get; private set; }

        public Player(TcpClient client, string nickname)
        {
            this.client = client;
            stream = this.client.GetStream();
            Wins = 0;
            Loses = 0;
            Nickname = nickname;
            CanTurn = false;
            Sign = '-';
        }

        ~Player()
        {
            Disconnect();
        }

        public int SendData(MessageTypes type, string data)
        {
            try
            {                
                byte[] buffer = System.Text.Encoding.ASCII.GetBytes(string.Format("{0};{1}", Convert.ToInt32(type), data));
                stream.Write(buffer, 0, buffer.Length);
                return 0;
            }
            catch(SocketException e)
            {
                Console.WriteLine(e.Message);
                return -1;
            }
        }

        public bool ContainsSocket(TcpClient client)
        {
            return this.client == client ? true : false;
        }

        public void Disconnect()
        {
            if(client.Connected)
                client.Close();
        }

        public void SetBusy()
        {
            IsBusy = true;
        }

        public void SetFree()
        {
            IsBusy = false;
            Table = null;
        }

        public void SetGame(Game game)
        {
            Table = game;
        }

        public void SetToken(bool boolean)
        {
            CanTurn = boolean;
        }

        public void SetSign(char sign)
        {
            Sign = sign;
        }

        public void Win()
        {
            Wins++;
        }

        public void Lose()
        {
            Loses++;
        }
    }
}
