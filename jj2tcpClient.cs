using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Windows.Forms;
using System.Drawing;
namespace Jj2
{

    
    struct Server {
        public IPAddress ip;
        public int port;
        public string location;
        public bool priv;
        public int gameMode;
        public string gameModeStr;
        public int maxScore;
        public string version;
        public DateTime startTime;
        public int playerCount;
        public int maxPlayers;
        public string levelName;
        public string serverName;
        public string tileSetNamme;
        public int customMode;
        public int startHealth;
        public int maxHealth;
        public int plusOnly;
        public int friendlyFire;
        public int noMovement;
        public int noBlink;
        public int numOfPlayers;

        public Server(IPAddress ip,int port) {
            this.ip=ip;
            this.port = port;
            location = "";
            port = 10052;
            priv = false;
            gameMode = -1;
            gameModeStr = "";
            maxScore = 5;
            version = "24  ";
            startTime = DateTime.Now;
            playerCount = 0;
            maxPlayers = 32;
            serverName = "serverName";
            levelName = "levelName";
            tileSetNamme = "tilesetName";
            customMode = 0;
            startHealth = 3;
            maxHealth = 3;
            plusOnly = 0;
            friendlyFire = 0;
            noMovement = 0;
            noBlink = 0;
            numOfPlayers = 1;
        }
    }


    struct Player { 
        public byte sockID;
        public byte playerID;
        public int pchar;
        public int pTeam;
        public int[] fur;
        public string pName;
        public bool connected;

        public Player(string name) {
            sockID = 0x00;
            playerID = 0x00;
            pchar = 0;
            pTeam = 0;
            pName = name;
            fur = new int[4];
            connected = false;
        }
    }
    class jj2tcpClient
    {
        //---------------------------------ALL TCP PACKETS NEEDED-----------------------------------------//
        //CLIENT ----> SERVER
        
        //Joining request: { packetLength, 0x0F, udpBind[2] , jj2Version , numberOfPlayersJoining}
        Byte[] joinRequest = new Byte[] { 0x09, 0x0F, 0x06, 0xdc, 0x32, 0x34, 0x20, 0x20, 0x01 };


        //0x3f packet (plus packet)
        Byte[] plusPacket = new Byte[] { 0x08, 0x3f, 0x20, 0x03, 0x06, 0x00, 0x05, 0x00 };


        /*
        joiningDetails 
        {packetLength, 0x0E, numberOfPlayers, playerID, teamAndChar, furColor[4], 0x11, myID, 0x0A, 0x0D ,0x00 ,0x00, playerName[], 0x00} -->proveri da ovo nije onaj plus[4]
        */


        /*
        Send message to chat
        {0x00,msgLength,0x00,0x1B,myID,0x00,text[]}
        */
        


        //Spectate on/off
        Byte[] spectateon = new Byte[] { 0x03, 0x42, 0x21 };
        Byte[] spectateoff = new Byte[] { 0x03, 0x42, 0x20 };

        /*
        getReady packet
        byte[] ready = new byte[] { 0x00, 0x09, 0x00, 0x01b, myID, 0x00, 0x2f, 0x072, 0x65, 0x61, 0x64, 0x79 };
        */

        //SERVER ------> CLIENT

        /*
         Server details - 0x10
         {packetLength, 0x10, socketID, playerID,levelFileNameLength,levelFileName, levelCRC(4bytes), tilesetCRC(4bytes), gameMode,maxScore}
         */





        //---------------------------------ALL TCP PACKETS NEEDED-----------------------------------------//



        Socket jj2tcpSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
        EndPoint server;
        string playerName;
        byte myID=0x01,mySockID;
        public byte[] magicNumbers={0x00,0x00,0x00,0x00};
        public Server serv = new Server(IPAddress.Parse("127.0.0.1"), 10052);
        Player[] players = new Player[32];
        RichTextBox chatLog,playerList,sender;
        bool spec = false;

        //Constructor
        public jj2tcpClient(IPAddress ip, int port,string name,RichTextBox chatLog,RichTextBox players,RichTextBox sender) {
            server = new IPEndPoint(ip,port);
            playerName = name;
            this.chatLog = chatLog;
            this.playerList = players;
            this.sender = sender;
            sender.KeyDown += new KeyEventHandler(sender_keyDown);
            Join();
        }

        public void connect() {
            jj2tcpSocket.Connect(server);
            startListening();
        }

        public void sendJoinRequest() {//C...
            jj2tcpSocket.Send(joinRequest);
            addLog("Connecting to "+serv.ip+":"+serv.port+Environment.NewLine,Color.Blue);
        }

        public void sendPName()//2.Join
        {
            int plength = Convert.ToInt32(playerName.Length);
            Byte[] playername = Encoding.ASCII.GetBytes(playerName);//playername in bytes
            Byte[] joindetailspre = new Byte[] { (byte)(plength + 17), 0x0E, 0x01, myID, 0x01, 0x01, 0x40, 0x20, 0x48, 0x50, 0x11, myID, 0x0A ,0x0D, 0x00,0x00 };
            Byte[] join = new Byte[playername.Length + 16];//final infopacket

            Array.Copy(joindetailspre, 0, join, 0, joindetailspre.Length);//forming packet
            Array.Copy(playername, 0, join, joindetailspre.Length, playername.Length);

            
            jj2tcpSocket.Send(plusPacket);
            jj2tcpSocket.Send(join);
            byte[] zeroByte = { 0x00 };
            jj2tcpSocket.Send(zeroByte);
        }

        public void Join() {
            connect();
            sendJoinRequest();
            sendPName();
        }
        public void startListening() {
            byte[] buffer = new byte[500];
            jj2tcpSocket.BeginReceiveFrom(buffer,0,buffer.Length,SocketFlags.None,ref server,new AsyncCallback(onReceive),buffer);
        }

        public void sendMessage() {
            string msg = sender.Text;
            int msglength = 3 + msg.Length;
            Byte[] messagefinal = new Byte[msglength + 3];
            Byte[] always = new Byte[] { 0x00, (byte)msglength, 0x00, 0x1B, myID, 0x00 };
            Byte[] message = Encoding.ASCII.GetBytes(msg);
            Array.Copy(always, 0, messagefinal, 0, always.Length);
            Array.Copy(message, 0, messagefinal, always.Length, message.Length);
            jj2tcpSocket.Send(messagefinal);
            Color myC = Color.White;
            if (getMyTeam() == 1) { myC = Color.Red; }
            else { myC = Color.Blue; }
            chatLog.SelectionFont = new Font("Fatboy Slim", 9, FontStyle.Bold);
            chatLog.SelectionColor = myC;
            addLog(playerName + ": ",myC);
            addMsg(msg);
            sender.Clear();
        }
        private void onReceive(IAsyncResult aResult) {
            int size = jj2tcpSocket.EndReceiveFrom(aResult, ref server);
            byte[] receivedData = new byte[size];
            receivedData = (byte[])aResult.AsyncState;
            int poslRbr = 0;
            int packetL;
            if (receivedData[0] != 0x00) packetL = receivedData[0];
            else packetL = receivedData[1]+3;
            while (packetL > 0)
            {
                byte[] subPacket = new byte[packetL];
                for (int i = 0; i < packetL; i++)
                    subPacket[i] = receivedData[i + poslRbr];
                checkOnGivenPacket(subPacket);
                poslRbr += packetL;
                if (receivedData[poslRbr] != 0x00) packetL = receivedData[poslRbr];
                else packetL = receivedData[1]+3;
                if (poslRbr+packetL > 499) break;
            }
            if(jj2tcpSocket.Connected)startListening();
        }


        public void checkOnGivenPacket(byte[] packet) { 
         int p=2;
         byte packetID=0x00;
        if(packet.Length>3)
		if(packet[3]==0x1B || packet[3]==0x40 || packet[3]==0x0D)packetID=packet[3];
		else packetID=packet[1];
		int size=packet.Length;
		string packetStr=System.Text.Encoding.UTF8.GetString(packet);
		switch(packetID){
			case 0x10:
				int k=packet[0]-18;
				magicNumbers[0]=packet[k];
				magicNumbers[1]=packet[k+1];
				magicNumbers[2]=packet[k+2];
				magicNumbers[3]=packet[k+3];
				mySockID=packet[p++];
				myID=packet[p++];
				serv.levelName=packetStr.Substring(5,(int)packet[p++]);
				p+=serv.levelName.Length;
				p+=8;
				serv.gameMode=packet[p++];
				serv.maxScore=packet[p++];
				addLog("Current map: "+serv.levelName+Environment.NewLine,Color.Green);
                sender.Enabled = true;
			break;

			case 0x12:
            serv.numOfPlayers = packet[p];
            int br = 0;
				while(br<serv.numOfPlayers){
					p++;//0x00
                    byte psockID = packet[p++];
					byte playerID=packet[p++];
					byte pChar=packet[p++];
					byte pTeam=packet[p++];
					int[]fur={packet[p++],packet[p++],packet[p++],packet[p++]};
					p+=5;//unknownData[6]
					string pName="";
					while(packet[p++ +1]!=0x00 && p<packet.Length)pName+=Convert.ToChar(packet[p]);
                    pName = pName.Replace("|","");

					Player newp=new Player(pName);
					newp.playerID=playerID;
					newp.sockID=psockID;
					newp.pchar=pChar;
					newp.pTeam=pTeam;
					newp.fur=fur;
					newp.connected=true;
					players[playerID]=newp;
                    br++;
				}
				refreshPlayerList();
                byte[] addPing = formPlusPacket();
                jj2tcpSocket.Send(addPing);
			break;

			case 0x13://wow took me some time to realize, this used to be disconnect packet or smth in previous plus versions (facepalm)
			break;

			case 0x3F:
				//serv.version=packetStr.Substring(p,4);
				p+=4;//skipping unknown data
				serv.customMode=packet[p++];
				serv.startHealth=packet[p++];
				serv.maxHealth=packet[p++];
				byte plusByte=packet[p++];
				serv.plusOnly=((int)plusByte)&1;
				serv.friendlyFire=(plusByte>>1)&1;
				serv.noMovement=(plusByte>>2)&1;
				serv.noBlink=(plusByte>>3)&1;
			break;

			case 0x0D:
			if(packet[5]==mySockID || mySockID==-1){
				addLog("You have been disconnected. ( "+disconnectMessage(packet[4])+" )",Color.Red);
                sender.Enabled = false;
				jj2tcpSocket.Dispose();
				return;
			}
			else{
				byte sid=packet[5];
                int ppid = getPlayerIDfromSockID(sid);
				players[ppid].connected=false;
				refreshPlayerList();
                addLog(players[ppid].pName + " has left the server. ( "  +disconnectMessage(packet[4])+" )",Color.Green);
			}
			break;

			case 0x1B:
				int pid1=packet[4],indexer=6;
                string msg = "";
                while (indexer < packet.Length) msg += Convert.ToChar(packet[indexer++]);
                //msg = msg.Replace("|","");
                if (players[pid1].pTeam == 1) { addLog(players[pid1].pName + ": ", Color.Red); }
                if (players[pid1].pTeam == 0) { addLog(players[pid1].pName + ": ", Color.Blue); }
				addMsg(msg);
			break;

			case 0x40:
                msg = "";
                indexer=6;
                while (indexer < packet.Length && packet[indexer]!=0x00) msg += Convert.ToChar(packet[indexer++]);
                //msg = msg.Replace("|","");
                if(msg.Length>1)
                if (msg[0] == '2') msg = msg.Substring(1);
                addLog("Console: ", Color.Gray);
				addMsg(msg);
			break;
            
            case 0x11:
                byte ppsockID = packet[p++];
                byte pnumOfPlayers = packet[p++];
                int pbr = 0;
                while (pbr++ < pnumOfPlayers) { //in case splitscreen joins
                    byte pID = packet[p++];
                    p += 2;//unknownData[2]
                    byte pTeamAndChar = packet[p++];
                    int[] fur = { packet[p++],packet[p++],packet[p++],packet[p++]};
                    p += 5;
                    string ppname = "";
                    while (packet[p] != 0x00) ppname += Convert.ToChar(packet[p++]);
                    Player newp = new Player(ppname);
                    newp.sockID = ppsockID;
                    newp.playerID = pID;
                    newp.pTeam = (pTeamAndChar & 16) / 16;
                    newp.pchar = pTeamAndChar & 3;
                    newp.fur = fur;
                    newp.connected = true;
                    players[pID] = newp;
                    addLog(ppname + " has joined the server.",Color.Green);
                    refreshPlayerList();
                }//while
            break;
		}//switch
        }//checkOnGivenPacket()
        void sender_keyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Enter)
            {
                sendMessage();
            }
        }
        public int findExtraDataStart(byte[] receivedData) {//this packet stops C... and gives you ping in the game
            bool found = false;
            int i = 1;
            try
            {
                while (found == false)
                {
                    if (receivedData[i] == 0x6A && receivedData[i + 1] == 0x32 && receivedData[i + 2] == 0x6C) { found = true; i--; i += 12; } i += 2;
                }
            }
            catch { i = 1; }
            return i;
        }

        public byte[] formPlusPacket() {
            return new byte[] { 0x06, 0x1A, magicNumbers[0],magicNumbers[1],magicNumbers[2],magicNumbers[3]};
        }

        public static string disconnectMessage(byte msg)
        {
            string message = "Unknown reason.";
            switch (msg)
            {
                case 0x01: message = "Server is full";
                    break;
                case 0x02: message = "Version different";
                    break;
                case 0x04: message = "Error during handshaking";
                    break;
                case 0x05: message = "Feature not supported in shareware";
                    break;
                case 0x06: message = "Error downloading level";
                    break;
                case 0x07: message = "Connection lost";
                    break;
                case 0x08: message = "Winsock error";
                    break;
                case 0x09: message = "Connection timed out";
                    break;
                case 0x0A: message = "Server stopped";
                    break;
                case 0x0B: message = "Kicked off";
                    break;
                case 0x0C: message = "Banned";
                    break;
                case 0x0D: message = "Denied";
                    break;
                case 0x0E: message = "Version of JJ2+ is different";
                    break;
                case 0x0F: message = "Server kicked you for idling";
                    break;
                case 0x10: message = "No downloads allowed";
                    break;
                case 0x11: message = "Unauthorized file request";
                    break;
                case 0x12: message = "No splitscreeners allowed";
                    break;
                default: message = "Unknown error";
                    break;
            }
            return message;
        }
        void addLog(string msg,Color c) {
            chatLog.SelectionFont = new Font("Fatboy Slim", 9, FontStyle.Bold);
            chatLog.SelectionColor = c;
            chatLog.AppendText(msg);
            chatLog.SelectionColor = Color.White;
        }
        public void addMsg(string msg) {
            Color[] colors = new Color[7];
            colors[0] = Color.White;
            colors[1] = Color.Chartreuse;
            colors[2] = Color.Red;
            colors[3] = Color.Aqua;
            colors[4] = Color.Yellow;
            colors[5] = Color.DeepPink;
            colors[6] = Color.Gray;
            int p = 0;
            for (int i = 0; i < msg.Length; i++) {
                if (msg[i] == '|') p=(p+1)%7;
                else {
                    chatLog.SelectionColor = colors[p];
                    chatLog.AppendText(msg.Substring(i,1));//jj2 chat colors :D
                }
            }
            chatLog.AppendText(Environment.NewLine);
        }
       /* public void sender_KeyDown(object sender, KeyEventArgs e) {
            sendMessage();
        }*/
        void addPlayer(int i) {
            Color C=Color.White;
            if (players[i].pTeam == 1) C = Color.Red;
            if (players[i].pTeam == 0) C = Color.Blue;
            playerList.SelectionColor = C;
            playerList.AppendText(players[i].playerID + 1 + ". " + players[i].pName +Environment.NewLine);
        }
        void refreshPlayerList() {
            playerList.Clear();
            for (int i = 0; i < 32; i++) {
                if (players[i].connected == true) {
                    addPlayer(i);
                }
            }//for
        }//refreshPlayerList()
        public byte getPlayerIDfromSockID(byte sockID) {
            for (int i = 1; i < 32; i++) {
                if (players[i].sockID == sockID) return players[i].playerID;
            }
            return 0;
        }
        public bool isConnected() {
            return jj2tcpSocket.Connected;
        }
        public int getMyTeam(){
            return players[myID].pTeam;
        }
        public void getReady() {
            byte[] ready = new byte[] { 0x00, 0x09, 0x00, 0x01b, myID, 0x00, 0x2f, 0x072, 0x65, 0x61, 0x64, 0x79 };
            jj2tcpSocket.Send(ready);
        }
        public void spectate() {
            if (spec)
            {
                jj2tcpSocket.Send(spectateoff);
            }
            else {
                jj2tcpSocket.Send(spectateon);
            }
        }
        //I've faced the problem that packets get merged, so I can really get playerList only by checking packetID as receivedData[1]. I've fixed it with this function:
        //nvm fixed it the other way \3
      /*  int checkForPlayerList(byte[]packets,ref byte packet) {
            for (int i = 0; i < 500; i++) {
                if (packets[i] == 0x12 && packets[i + 2] == 0x00 && packets[i + 3] == 0x00 && packets[i + 4] == 0x01) {
                    packet = 0x12;
                    return i+1;
                }
            }
            return 2;
        }
        int checkForLevelInfo(byte[] packets, ref byte packet) {
            for (int i = 0; i < 450; i++) {
                if (packets[i] == 0x10 && packets[i + 1] == 0x01 && packets[i + 2] == 0x01 && packets[i + 3] == 0x0B) {
                    packet = 0x10;
                    return i+1;
                }
            }
            return 2;
        }*/

        //Event listeners:

    }//class
}//namespace
