using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Agot2Server
{
    public partial class AGOT2Service : ServiceBase
    {
        private EventWaitHandle _eWH = new AutoResetEvent(false);
        private Thread _MainThread;

        private Service _MainService = new Service();

        public AGOT2Service()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            this._MainThread = new Thread(MainProcedure);
            this._MainThread.IsBackground = true;
            this._MainThread.Start();
        }

        protected override void OnStop()
        {
            _MainService.Dispose();

            this._eWH.Set();
            base.OnStop();
        }

        private void MainProcedure()
        {
            _MainService.Start();

            this._eWH.WaitOne();
        }
    }
}
