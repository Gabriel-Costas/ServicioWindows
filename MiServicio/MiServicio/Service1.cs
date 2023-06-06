using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.ServiceProcess;
using System.Threading;

namespace MiServicio
{
    public partial class Service1 : ServiceBase
    {

        public Service1()
        {
            InitializeComponent();
            
        }


        Servicio s;


        protected override void OnStart(string[] args)
        {
            s = new Servicio();
            s.eventWriter("Comienza la ejecución");
            Thread t=new Thread(s.init); 
            t.IsBackground = true;
            t.Start();

        }

        protected override void OnStop()
        {
            s.serv.Close();
        }





        




    }


}
