using System.Globalization;
using common;
using common.resources;
using GameServer.realm;
using NLog;

namespace GameServer
{
    static class Program
    {
        internal static ServerConfig Config;
        internal static Resources Resources;

        static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly ManualResetEvent Shutdown = new(false);

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += LogUnhandledException;

            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.Name = "Entry";

            Config = args.Length > 0 ? ServerConfig.ReadFile(args[0]) : ServerConfig.ReadFile("gameServer.json");

            LogManager.Configuration.Variables["logDirectory"] = Config.serverSettings.logFolder + "/game";
            LogManager.Configuration.Variables["buildConfig"] = Utils.GetBuildConfig();

            Config.serverInfo.maxPlayers = Config.serverSettings.maxPlayers;

            using (Resources = new Resources(Config.serverSettings.resourceFolder, true))
            using (var db = new Database(
                Config.dbInfo.host,
                Config.dbInfo.port,
                Config.dbInfo.auth,
                Config.dbInfo.index,
                Resources))
            {
                var marketSweeper = new MarketSweeper(db);
                marketSweeper.Run();

                var manager = new RealmManager(Resources, db, Config);
                manager.Run();

                var server = new Server(manager,
                    Config.serverInfo.port,
                    Config.serverSettings.maxConnections);

                Console.CancelKeyPress += delegate { Shutdown.Set(); };

                Shutdown.WaitOne();

                Log.Info("Terminating...");
                manager.Stop();
                server.Stop();
                Log.Info("Server terminated.");
            }
        }

        public static void Stop(Task task = null)
        {
            if (task != null)
                Log.Fatal(task.Exception);

            Shutdown.Set();
        }

        private static void LogUnhandledException(object sender, UnhandledExceptionEventArgs args)
        {
            Log.Fatal((Exception)args.ExceptionObject);
        }
    }
}