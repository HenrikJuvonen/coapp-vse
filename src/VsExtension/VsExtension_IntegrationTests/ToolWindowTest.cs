using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VsSDK.IntegrationTestLibrary;
using Microsoft.VSSDK.Tools.VsIdeTesting;

namespace VsExtension_IntegrationTests
{

    [TestClass()]
    public class ConsoleWindowTest
    {
        private delegate void ThreadInvoker();

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        /// <summary>
        ///A test for showing the ConsoleWindow
        ///</summary>
        [TestMethod()]
        [HostType("VS IDE")]
        public void ShowConsoleWindow()
        {
            UIThreadInvoker.Invoke((ThreadInvoker)delegate()
            {
                CommandID ConsoleWindowCmd = new CommandID(CoApp.VsExtension.GuidList.guidVsExtensionCmdSet, (int)CoApp.VsExtension.PkgCmdIDList.console);

                TestUtils testUtils = new TestUtils();
                testUtils.ExecuteCommand(ConsoleWindowCmd);

                Assert.IsTrue(testUtils.CanFindConsoleWindow(new Guid(CoApp.VsExtension.GuidList.guidConsoleWindowPersistanceString)));

            });
        }

    }
}
