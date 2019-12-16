using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nefarius.ViGEm.Client;
using Nefarius.ViGEm.Client.Targets;
using Nefarius.ViGEm.Client.Targets.Xbox360;

namespace WiiTUIO.Output.Handlers.Xinput
{
    public class ViGEmBus360Device
    {
        public const int outputResolution = 32767 - (-32768);

        private Xbox360Controller cont;
        public Xbox360Report report;
        public event Action<byte, byte> OnRumble;

        public Xbox360Controller Cont { get => cont; }
        //public Xbox360Report Report { get => report; }

        public ViGEmBus360Device(ViGEmClient client)
        {
            cont = new Xbox360Controller(client);
            report = new Xbox360Report();

            cont.FeedbackReceived += FeedbackProcess;
        }

        private void FeedbackProcess(object sender, Xbox360FeedbackReceivedEventArgs e)
        {
            OnRumble?.Invoke(e.LargeMotor, e.SmallMotor);
        }

        public bool Connect()
        {
            cont.Connect();
            return true;
        }

        public bool Disconnect()
        {
            cont.Disconnect();
            cont.Dispose();
            return true;
        }

        public bool Update()
        {
            cont.SendReport(report);
            return true;
        }

        public void Reset()
        {
            report = new Xbox360Report();
        }
    }
}
