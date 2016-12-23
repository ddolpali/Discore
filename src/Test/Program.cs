using Discore;
using Discore.WebSocket;
using System;
using System.IO;
using System.Threading;

namespace DiscoreBotTest
{
    class Program
    {
        static DiscordWebSocketApplication app;
        static Shard shard;

        public static void Main(string[] args)
        {
            string token = File.ReadAllText("token.txt").Trim();

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

            /*
            DiscordChannel channelT;
            shard.Cache.Channels.TryGetValue(new Snowflake(155423524293443586), out channelT);

            DiscordGuildVoiceChannel channelVC = channelT as DiscordGuildVoiceChannel;
            shard.Voice.ConnectToVoice(channelVC);
            */
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
