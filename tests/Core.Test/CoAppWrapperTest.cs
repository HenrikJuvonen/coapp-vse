using System.Linq;
using System.Threading;
using CoApp.Packaging.Common;
using CoApp.VisualStudio;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Core.Test
{
    /// <summary>This is a test class for CoAppWrapper and is intended to contain all CoAppWrapper Unit Tests.</summary>
    [TestClass()]
    public class CoAppWrapperTest
    {
        private TestContext testContextInstance;

        /// <summary>Gets or sets the test context which provides information about and functionality for the current test run.</summary>
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

        /// <summary>Initializes CoAppWrapper</summary>
        [ClassInitialize()]
        public static void CoAppWrapperInitialize(TestContext testContext)
        {
            CoAppWrapper.Initialize();
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion

        /// <summary>A test for InstallPackage</summary>
        [TestMethod()]
        public void InstallPackageTest()
        {
            IPackage package = CoAppWrapper.GetPackages(new string[] { "coapp.devtools" }).FirstOrDefault();
            Assert.IsNotNull(package, "Package not found.");
            CoAppWrapper.InstallPackage(package);

            IPackage package2 = CoAppWrapper.GetPackages("installed").FirstOrDefault(n => n.CanonicalName.PackageName == package.CanonicalName.PackageName);
            Assert.IsNotNull(package2, "Package not installed.");
        }

        /// <summary>A test for RemovePackage</summary>
        [TestMethod()]
        public void RemovePackageTest()
        {
            IPackage package = CoAppWrapper.GetPackages(new string[] { "coapp.devtools" }).FirstOrDefault();
            Assert.IsNotNull(package, "Package not found.");
            CoAppWrapper.RemovePackage(package);

            IPackage package2 = CoAppWrapper.GetPackages("installed").FirstOrDefault(n => n.CanonicalName.PackageName == package.CanonicalName.PackageName);
            Assert.IsNull(package2, "Package not removed.");
        }
    }
}
