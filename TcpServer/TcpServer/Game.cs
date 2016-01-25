using System;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer
{
    public class Game
    {
        public List<Player> players { get; }
        char[] canvas;

        public Game(Player p1, Player p2)
        {
            players = new List<Player>();
            players.Add(p1);
            players.Add(p2);
            int rd = new Random().Next(1, 2);
            switch(rd)
            {
                case 1:
                    p1.SetToken(true);
                    break;
                case 2:
                    p2.SetToken(true);
                    break;
            }
            p1.SetSign('X');
            p2.SetSign('O');
            canvas = "---------".ToCharArray();
        }

        public bool ContainsPlayer(Player player)
        {
            foreach(Player p in players)
            {
                if (p == player)
                    return true;
            }
            return false;
        }

        public bool ContainsSocket(TcpClient client)
        {
            foreach(Player p in players)
            {
                if (p.ContainsSocket(client))
                    return true;
            }
            return false;
        }

        public bool ContainsNickname(string nickname)
        {
            foreach(Player p in players)
            {
                if (p.Nickname == nickname)
                    return true;
            }
            return false;
        }

        public Player WhosGotToken()
        {
            foreach (Player p in players)
            {
                if (p.CanTurn)
                    return p;
            }
            return null;
        }

        public bool ParseGameResult(Player player)
        {
            if (players.Contains(player))
                return ParseGameResultStatement(canvas, player.Sign);
            else
                return false;
        }

        public static bool ParseGameResultStatement(char[] tab, char s)
        {
            bool statement =
            (
                tab[0] == s &&
                tab[1] == s &&
                tab[2] == s
            ) ||
            (
                tab[3] == s &&
                tab[4] == s &&
                tab[5] == s
            ) ||
            (
                tab[6] == s &&
                tab[7] == s &&
                tab[8] == s
            ) ||
            (
                tab[0] == s &&
                tab[3] == s &&
                tab[6] == s
            ) ||
            (
                tab[1] == s &&
                tab[4] == s &&
                tab[7] == s
            ) ||
            (
                tab[2] == s &&
                tab[5] == s &&
                tab[8] == s
            ) ||
            (
                tab[0] == s &&
                tab[4] == s &&
                tab[8] == s
            ) ||
            (
                tab[2] == s &&
                tab[4] == s &&
                tab[6] == s
            );

            return statement;
        }

        public void SetCanvas(char[] tab)
        {
            canvas = tab;
        }

        public Player GetOpponent(Player player)
        {
            if (player == players[0])
                return players[1];
            else if (player == players[1])
                return players[0];
            return null;
        }
    }
}
