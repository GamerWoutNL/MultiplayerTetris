﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using ServerProject.GameLogics;
using ServerProject.Data;
using System.IO;

namespace ServerProject.Communication
{
	public class ServerClient
	{
		private TcpClient client;
		private Server server;
		private NetworkStream stream;
		private byte[] buffer;
        private string weapon;
        private string name;
		private static object lockObject = new object();
		public string Name { get; set; }
		public string PlayerID { get; set; }
		public int Wins { get; set; }

		public ServerClient(TcpClient client, Server server, string playerID)
		{
			this.client = client;
			this.server = server;
			this.stream = this.client.GetStream();
			this.buffer = new byte[1024];
			this.PlayerID = playerID;

			this.stream.BeginRead(this.buffer, 0, this.buffer.Length, new AsyncCallback(OnRead), null);
		}

		private void OnRead(IAsyncResult ar)
		{
			try
			{
				int count = this.stream.EndRead(ar);
				string input = Encrypter.Decrypt(this.buffer.SubArray(0, count), "password123");

				string regex = "##";

				while (input.Contains(regex))
				{
					string packet = input.Substring(0, input.IndexOf(regex));
					input = input.Substring(input.IndexOf(regex) + regex.Length);


					this.HandlePacket(packet);
				}

				this.stream.BeginRead(this.buffer, 0, this.buffer.Length, new AsyncCallback(OnRead), null);
			}
			catch (IOException)
			{
				this.server.clients.Remove(this);
				this.Disconnect();

				Console.WriteLine("Client disconnected");
			}
			
		}

		public void HandlePacket(string packet)
		{
			Console.WriteLine($"Received from client: {packet}");

			if (packet.Contains("player"))
			{
				string[] nameAndAnswer = packet.Split(new[] { "::" }, StringSplitOptions.None);
                name = nameAndAnswer[0];
                weapon = nameAndAnswer[1];

				lock (lockObject)
				{
					this.server.attackchoice.Add(name, (Weapon)Enum.Parse(typeof(Weapon), weapon));
				}

				Console.WriteLine($"Name: {name} answered: {weapon}");
                Console.WriteLine(server.attackchoice.Count);
			}
			else if (packet.Contains("name"))
			{
				this.Name = packet.Split(new[] { "::" }, StringSplitOptions.None)[1];
			}
		}

		public void Write(string message)
		{
			string regex = "##";
			byte[] bytes = Encrypter.Encrypt($"{message}{regex}", "password123");
			this.stream.Write(bytes, 0, bytes.Length);
			this.stream.Flush();
		}

		public void Disconnect()
		{
			this.stream.Close();
			this.client.Close();
		}
	}
}
