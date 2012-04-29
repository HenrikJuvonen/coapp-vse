using System;

namespace CoApp.Vsp
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Console.WriteLine("coapp-vsp-prototype");
            Handler.Start();
        }
    }
}
