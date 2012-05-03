/***************************************************************************

Copyright (c) Microsoft Corporation. All rights reserved.
This code is licensed under the Visual Studio SDK license terms.
THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.

***************************************************************************/

using System;
using System.Collections;
using System.Text;
using System.Reflection;
using Microsoft.VsSDK.UnitTestLibrary;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VSSDK.Tools.VsIdeTesting;
using CoApp.VsExtension;

namespace VsExtension_UnitTests.ConsoleWindowTest
{
    /// <summary>
    ///This is a test class for MyConsoleWindowTest and is intended
    ///to contain all MyConsoleWindowTest Unit Tests
    ///</summary>
    [TestClass()]
    public class ConsoleWindowTest
    {

        /// <summary>
        ///MyConsoleWindow Constructor test
        ///</summary>
        [TestMethod()]
        public void ConsoleWindowConstructorTest()
        {

            ConsoleWindow target = new ConsoleWindow();
            Assert.IsNotNull(target, "Failed to create an instance of MyConsoleWindow");

            MethodInfo method = target.GetType().GetMethod("get_Content", BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(method.Invoke(target, null), "MyControl object was not instantiated");

        }

        /// <summary>
        ///Verify the Content property is valid.
        ///</summary>
        [TestMethod()]
        public void WindowPropertyTest()
        {
            ConsoleWindow target = new ConsoleWindow();
            Assert.IsNotNull(target.Content, "Content property was null");
        }

    }
}
