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


        public void eventWriter(string msg)
        {
            string nombre = "MiServicio";
            string logDestino = "Application";
            if (!EventLog.SourceExists(nombre))
            {
                EventLog.CreateEventSource(nombre, logDestino);
            }
            EventLog.WriteEntry(nombre, msg);
        }




        protected override void OnStart(string[] args)
        {
            eventWriter("Comienza la ejecución");
            Thread t=new Thread(init); 
            t.IsBackground = true;
            t.Start();

        }

        protected override void OnStop()
        {
            serv.Close();
            eventWriter("MiServicio ha sido detenido");
        }





        int port = -1;
        Socket serv;
        bool close = false;
        string passw = "cerrar";
        static readonly object l = new object();

        public void init()
        {
            string path = Environment.GetEnvironmentVariable("PROGRAMDATA") + "/Puerto.txt";


            try
            {

                using (StreamReader sr = new StreamReader(path))
                {
                    string tryPort = sr.ReadToEnd();
                    bool isNum = int.TryParse(tryPort, out port);
                    if (!isNum || port < 1 || port > 65535)
                    {
                        port = 31416;
                        eventWriter("Puerto no reconocido");
                    }
                }
            }
            catch (FileNotFoundException)
            {
                port = 31416;
                eventWriter("Error al leer el archivo");
            }
            eventWriter(String.Format("Puerto seleccionado{0}\nContraseña de cierre: {1}", port, passw));


            IPEndPoint ipe = new IPEndPoint(IPAddress.Any, port);
            serv = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                serv.Bind(ipe);

            }
            catch (SocketException)
            {
                try
                {
                    ipe.Port++;
                    serv.Bind(ipe);

                }
                catch (SocketException)
                {
                    Console.WriteLine("Occupied port");
                }
            }

            try
            {
                serv.Listen(10);
                while (!close)
                {
                    Socket sClient = serv.Accept();
                    Thread hilo = new Thread(hiloCliente);
                    hilo.IsBackground = true;
                    hilo.Start(sClient);
                }

                Console.WriteLine("Conection closed");
                serv.Close();

            }
            catch (SocketException)
            {
                Console.WriteLine("Server closed");
            }
        }

        public void hiloCliente(Object s)
        {

            Socket sClient = (Socket)s;

            IPEndPoint ipeClient = (IPEndPoint)sClient.RemoteEndPoint;
            Console.WriteLine("Client {0} connected at port {1}", ipeClient.Address, ipeClient.Port);

            using (NetworkStream ns = new NetworkStream(sClient))
            using (StreamReader sr = new StreamReader(ns))
            using (StreamWriter sw = new StreamWriter(ns))
            {

                sw.AutoFlush = true;

                sw.WriteLine("Welcome\nOptions: TIME, DATE, ALL, CLOSE");
                string msg = "";
                try
                {
                    msg = sr.ReadLine();
                    if (msg != null)
                    {

                        switch (msg.ToUpper())
                        {
                            case "TIME":
                                sw.WriteLine(DateTime.Now.TimeOfDay);
                                break;

                            case "DATE":
                                DateTime dt = DateTime.Today;
                                sw.WriteLine(dt.ToString("dd/MM/yyyy"));
                                break;

                            case "ALL":
                                sw.WriteLine(DateTime.Now);
                                break;

                            case string st when st.StartsWith("CLOSE"):
                                try
                                {
                                    string tryPass = msg.Substring(6);
                                    //sw.WriteLine("msg: {0} - st: {1} - tryPass: {2}",msg,st,tryPass);
                                    if (tryPass == passw.Trim())
                                    {
                                        while (!close)
                                        {
                                            lock (l)
                                            {
                                                if (!close)
                                                {
                                                    close = true;  //lock
                                                    serv.Close();

                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        //sw.WriteLine("{0}-{1}", tryPass, passw);
                                        sw.WriteLine("Incorrect password");
                                    }

                                }
                                catch (ArgumentOutOfRangeException)
                                {
                                    sw.WriteLine("Close command must be followed by the password");
                                }

                                break;

                            default:
                                sw.WriteLine("Command not valid");
                                break;
                        }
                        sClient.Close();
                    }
                }
                catch (IOException)
                {
                    sClient.Close();
                }
            }
        }




    }


}
