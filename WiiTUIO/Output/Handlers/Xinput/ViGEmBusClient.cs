using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Nefarius.ViGEm.Client;

namespace WiiTUIO.Output.Handlers.Xinput
{
    public class ViGEmBusClient
    {
        private ViGEmClient vigemTestClient = null;
        public ViGEmClient VigemTestClient => vigemTestClient;

        public ViGEmBusClient()
        {
            try
            {
                vigemTestClient = new ViGEmClient();
            }
            catch (Exception) { }

            App.Current.Dispatcher.BeginInvoke(new Action(delegate ()
            {
                App.Current.Exit += OnAppExit;
            }), null);
        }

        private void OnAppExit(object sender, ExitEventArgs e)
        {
            if (vigemTestClient != null)
            {
                Task.Run(() =>
                {
                    Thread.Sleep(1000);

                    vigemTestClient.Dispose();
                    vigemTestClient = null;

                    Thread.Sleep(1000);
                }).Wait();
            }
        }
    }
}
