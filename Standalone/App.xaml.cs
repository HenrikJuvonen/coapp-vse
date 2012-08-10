namespace CoApp.VSE
{
    using Core;

    public partial class App
    {
        public App()
        {
            Module.Initialize();
            Startup += Module.OnStartup;
            Exit += Module.OnExit;
        }
    }
}
