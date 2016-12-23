using Discore;
using Discore.WebSocket;
using System;
using System.Threading;

/*
 * This is purposely broken, have to install nupkg via package manager console
 * install-package Discore.2.0.0.0-Framework.nupkg -Project FrameworkTest
 * This is due to a bug with how Visual Studio handles p2p references
 */

namespace FrameworkTest
{
    class Program
    {
        static DiscordWebSocketApplication app;
        static Shard shard;

        public static void Main(string[] args)
        {
            string token = "MTkyMDYyNzM2OTMzNDUzODI0.CzlXBw.cIGtI0VzNMlkaWLsd-DLm0AJvRM";

            DiscoreLogger.OnLog += DiscoreLogger_OnLog;

            DiscordBotUserToken auth = new DiscordBotUserToken(token);
            app = new DiscordWebSocketApplication(auth);

            shard = app.ShardManager.CreateSingleShard();
            shard.Start();

            TestShard(shard);

            while (shard.IsRunning)
                Thread.Sleep(1000);

            Console.WriteLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void TestShard(Shard shard)
        {
            shard.Gateway.OnMessageCreated += Gateway_OnMessageCreated;
        }

        private static async void Gateway_OnMessageCreated(object sender, MessageEventArgs e)
        {
            if (e.Message.Author != e.Shard.User)
                await e.Message.AddReaction(new DiscordReactionEmoji("👌"));
        }

        private static void DiscoreLogger_OnLog(object sender, DiscoreLogEventArgs e)
        {
            switch (e.Line.Type)
            {
                case DiscoreLogType.Error:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;
                case DiscoreLogType.Warning:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
                case DiscoreLogType.Verbose:
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    break;
                default:
                    Console.ForegroundColor = ConsoleColor.White;
                    break;
            }

            Console.WriteLine($"[{e.Line.Timestamp}] {e.Line.Message}");
        }
    }
}
