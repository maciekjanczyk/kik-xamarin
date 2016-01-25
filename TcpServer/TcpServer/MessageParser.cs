using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TcpServer
{
    public enum MessageTypes
    {
        Hello,
        Invite,
        Accept,
        Decline,
        Turn,
        Left,
        InviteClient,
        AcceptClient,
        StartClient,
        DeclineClient,
        TurnClient,
        LeftClient,
        TokenClient,
        WinClient,
        LoseClient,
        WelcomeClient,
        PlayerExist,
        SendPlayers,
        ListRequestClient,
        PlayerIsBusy,
        InvalidMessageType
    }

    public class MessageParser
    {
        public static string[] Split(byte[] data, char separator = ';')
        {
            int i = 0;
            for(i=0; i<data.Length; i++)
            {
                if (data[i] == 0)
                    break;
            }
            byte[] cleanedData = new byte[i];
            for (int j = 0; j < i; j++)
                cleanedData[j] = data[j];
            string buffer = System.Text.Encoding.ASCII.GetString(cleanedData);
            return buffer.Split(separator);
        }

        public static MessageTypes ToMessageType(string nap)
        {
            if (nap == "")
                return MessageTypes.InvalidMessageType;
            switch (Convert.ToInt32(nap))
            {
                case 0:
                    return MessageTypes.Hello;
                case 1:
                    return MessageTypes.Invite;
                case 2:
                    return MessageTypes.Accept;
                case 3:
                    return MessageTypes.Decline;
                case 4:
                    return MessageTypes.Turn;
                case 5:
                    return MessageTypes.Left;
                case 6:
                    return MessageTypes.InviteClient;
                case 7:
                    return MessageTypes.AcceptClient;
                case 8:
                    return MessageTypes.StartClient;
                case 9:
                    return MessageTypes.DeclineClient;
                case 10:
                    return MessageTypes.TurnClient;
                case 11:
                    return MessageTypes.LeftClient;
                case 12:
                    return MessageTypes.TokenClient;
                case 13:
                    return MessageTypes.WinClient;
                case 14:
                    return MessageTypes.LoseClient;
                case 15:
                    return MessageTypes.WelcomeClient;
                case 16:
                    return MessageTypes.PlayerExist;
                case 17:
                    return MessageTypes.SendPlayers;
                case 18:
                    return MessageTypes.ListRequestClient;
                case 19:
                    return MessageTypes.PlayerIsBusy;
                default:
                    return MessageTypes.InvalidMessageType;          
            }        
        }
    }
}
