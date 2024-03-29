﻿using CSharpTools.Pipes;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace CSharpTools.Tests.Pipes
{
    internal class Testing
    {
        private static readonly string ipcName = "test_pipe";
        private static readonly int bufferSize = Helpers.ComputeBufferSizeOf<int>();
        private static readonly int numberOfClientsToCreate = 2;

        public static async void Main()
        {
#if false
            PipeServerManager pipeServerManager = new PipeServerManager(ipcName, bufferSize);
            pipeServerManager.OnConnect += PipeServerManager_OnConnect;
            pipeServerManager.OnMessage += PipeServerManager_OnMessage;
            pipeServerManager.OnDispose += PipeServerManager_OnDispose;

            PipeClient client1 = CreateClient(1);
            PipeClient client2 = CreateClient(2);
#endif
#if true
#if NET6_0_OR_GREATER || NET48_OR_GREATER
            PipeSecurity pipeSecurity = new();
            //Allow local users to read and write to the pipe.
            pipeSecurity.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalSid, null),
                PipeAccessRights.ReadWrite, AccessControlType.Allow));
            //Deny network users access to the pipe.
            pipeSecurity.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.NetworkSid, null),
                PipeAccessRights.FullControl, AccessControlType.Deny));
#endif

            PipeServerManager pipeServerManager = new(ipcName, bufferSize
#if NET6_0_OR_GREATER || NET48_OR_GREATER
                , pipeSecurity: pipeSecurity
#endif
            );
            pipeServerManager.OnConnect += PipeServerManager_OnConnect;
            pipeServerManager.OnMessage += PipeServerManager_OnMessage;
            pipeServerManager.OnDispose += PipeServerManager_OnDispose;
            
            List<PipeClient> clients = new();
            for (int i = 0; i < numberOfClientsToCreate; i++)
            {
                PipeClient client = CreateClient(i);
                clients.Add(client);
                client.WaitForConnection();
            }
            for (int i = 0; i < numberOfClientsToCreate; i++) clients[i].SendMessage(Helpers.Serialize(i));

            pipeServerManager.BroadcastMessage(Helpers.Serialize(pipeServerManager.PipeServers.Count));
            pipeServerManager.SendMessage(pipeServerManager.PipeServers.First().Key, Helpers.Serialize(4));

            //Console.ReadLine();
            
            for (int i = 0; i < numberOfClientsToCreate; i++) clients[i].Dispose();
            pipeServerManager.Dispose();
#endif
        }

        private static PipeClient CreateClient(int i)
        {
            PipeClient client = new(ipcName, bufferSize);
            client.OnConnect += () => Client_OnConnect(i);
            client.OnMessage += (data) => Client_OnMessage(i, data);
            client.OnDispose += () => Client_OnDispose(i);
            return client;
        }

        private static void PipeServerManager_OnConnect(Guid id) => Console.WriteLine($"SERVER: New client '{id}'");

        private static void PipeServerManager_OnMessage(Guid id, ReadOnlyMemory<byte> data)
        {
            int formattedData = Helpers.Deserialize<int>(data.ToArray());
            Console.WriteLine($"SERVER: ({id}) '{formattedData}'");
        }

        private static void PipeServerManager_OnDispose(Guid id) => Console.WriteLine($"SERVER: Client disconnected '{id}'");

        private static void Client_OnConnect(int i) => Console.WriteLine($"CLIENT: ({i}) Connected");

        private static void Client_OnMessage(int i, ReadOnlyMemory<byte> data)
        {
            int formattedData = Helpers.Deserialize<int>(data.ToArray());
            Console.WriteLine($"CLIENT: ({i}) '{formattedData}'");
        }

        private static void Client_OnDispose(int i) => Console.WriteLine($"CLIENT: ({i}) Disconnected");
    }
}
