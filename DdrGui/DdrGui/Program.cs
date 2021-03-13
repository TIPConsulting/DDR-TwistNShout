using ArdNet.Server;
using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TIPC.Core.Channels;
using TIPC.Core.Tools;

namespace DdrGui
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);


            var hub = new MessageHub();
            var udp = new ArdNetServerUdpConfig("DDRTwistNShout", 7348);
            var tcp = new ArdNetServerTcpConfig(7348)
            {
                HeartbeatConfig = {
                    HeartbeatInterval = TimeSpan.FromMilliseconds(1000),
                    ForceStrictHeartbeat = true,
                    RespondToHeartbeats = false,
                    HeartbeatToleranceMultiplier = 0
                }
            };
            var config = new ArdNetServerConfig(IPTools.GetLocalIP(), udp, tcp);

            using (var gamepadPort = new SerialPort("COM15", 921600))
            {
                gamepadPort.Open();
                using (ArdNetServer ardServer = ArdNetServer.StartNew(config, hub))
                {
                    Application.Run(new Form1(ardServer, gamepadPort));
                }
            }
        }
    }
}
