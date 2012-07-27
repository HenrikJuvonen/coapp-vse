namespace CoApp.VSE
{
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
