using ArdNet.Nano;
using ArdNet.Nano.Client;
using System;
using System.Collections;
using System.Diagnostics;
using System.Threading;

namespace DdrSpinmaster
{
    public class Program
    {
        //LED
        private static PinController.LedBlinker _led = null;

        //Vibrator
        private static PinController.LedBlinker _vibe = null;

        //ArdClient
        private static ArdNetClientManager _ardManager;
        private static ManualResetEvent _serverMsgHandle = new ManualResetEvent(initialState: false);

        //MPU
        private static Mpu6050 _mpu;
        private static ArrayList _mpuArgList;
        private static DateTime _mpuLastSend = DateTime.MinValue;

        public static void Main()
        {
            _mpu = Mpu6050.StartNew(2);
            _mpuArgList = new ArrayList() { "", "", "" };
            _led = new PinController.LedBlinker(2);
            _vibe = new PinController.LedBlinker(13);

            Debug.WriteLine("Init");
            _led.On();
            Thread.Sleep(100);
            _led.Off();


            var AppID = "DDRTwistNShout";
            var serverUdpPort = 7348;
            var nanoConfig = new ArdNetClientConfig(AppID, null, serverUdpPort);
            nanoConfig.TCP.HeartbeatConfig.HeartbeatInterval = TimeSpan.FromMilliseconds(2000);
            nanoConfig.TCP.HeartbeatConfig.ForceStrictHeartbeat = true;
            nanoConfig.TCP.HeartbeatConfig.RespondToHeartbeats = false;

            _ardManager = new ArdNetClientManager(SystemConfig.WiFiCredentials, nanoConfig);
            _ardManager.TcpEndpointConnected += ArdManager_TcpEndpointConnected;
            _ardManager.TcpEndpointDisconnected += NanoClient_ServerDisconnected;
            _ardManager.StartWorkerThread();
            _ardManager.CommandReceived += ArdManager_CommandReceived;

            MpuValue data;
            var spinCmd = TcpRequest.CreateOutbound("SPIN", _mpuArgList);
            DateTime spinStartTime = DateTime.MinValue;

            while (true)
            {
                data = _mpu.GetData();
                //gyro data
                //x, y, z
                //_mpuArgList[0] = data.GyroX.ToString();
                //_mpuArgList[1] = data.GyroY.ToString();
                //_mpuArgList[2] = data.GyroZ.ToString();
                //Debug.WriteLine($"[{data.GyroX} {data.GyroY} {data.GyroZ}]");

                if (abs(data.GyroY) > 15000 && spinStartTime.Equals(DateTime.MinValue))
                {
                    spinStartTime = DateTime.UtcNow;
                }
                else if (abs(data.GyroY) < 5000 && !spinStartTime.Equals(DateTime.MinValue))
                {
                    var spinTime = (DateTime.UtcNow - spinStartTime).TotalMilliseconds;
                    Debug.WriteLine($"SPIN: {spinTime}");
                    spinStartTime = DateTime.MinValue;

                    if (spinTime > 800)
                    {
                        _ardManager.EnqueueTask(x => x.SendCommand(spinCmd));
                    }
                }


                Thread.Sleep(10);
            }
        }

        private static void ArdManager_CommandReceived(IArdNetSystem Sender, IConnectedSystemEndpoint ConnectedSystem, TcpRequest Message)
        {
            Debug.WriteLine($"CMD: {Message.Request}");
            if (Message.Request == "VIBE")
            {
                _vibe.Blink(500);
            }
        }

        private static void ArdManager_TcpEndpointConnected(IArdNetSystem Sender, IConnectedSystemEndpoint e)
        {
            _ = _serverMsgHandle.Set();
            Debug.WriteLine("ArdNet Connected");
            _led.On();
        }

        private static void NanoClient_ServerDisconnected(IArdNetSystem Sender, ISystemEndpoint e)
        {
            _ = _serverMsgHandle.Reset();
            Debug.WriteLine("ArdNet Disconnected");
            _led.Off();
        }

        static int abs(int val)
        {
            int mask = val >> (sizeof(short) * 8 - 1);
            return ((val + mask) ^ mask);
        }
    }
}
