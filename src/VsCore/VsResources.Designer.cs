﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.269
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace CoApp.VisualStudio.VsCore {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    public class VsResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal VsResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("CoApp.VisualStudio.VsCore.VsResources", typeof(VsResources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        public static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to CoApp for Visual Studio.
        /// </summary>
        public static string DialogTitle {
            get {
                return ResourceManager.GetString("DialogTitle", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The project &apos;{0}&apos; is unsupported.
        /// </summary>
        public static string DTE_ProjectUnsupported {
            get {
                return ResourceManager.GetString("DTE_ProjectUnsupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Checking for missing packages....
        /// </summary>
        public static string PackageRestoreCheckingMessage {
            get {
                return ResourceManager.GetString("PackageRestoreCheckingMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Finished restoring packages..
        /// </summary>
        public static string PackageRestoreCompleted {
            get {
                return ResourceManager.GetString("PackageRestoreCompleted", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Downloading package &apos;{0}&apos; failed..
        /// </summary>
        public static string PackageRestoreDownloadPackageFailed {
            get {
                return ResourceManager.GetString("PackageRestoreDownloadPackageFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occurred while trying to restore packages..
        /// </summary>
        public static string PackageRestoreErrorMessage {
            get {
                return ResourceManager.GetString("PackageRestoreErrorMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Following packages were not restored:.
        /// </summary>
        public static string PackageRestoreFollowingPackages {
            get {
                return ResourceManager.GetString("PackageRestoreFollowingPackages", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Installing packages....
        /// </summary>
        public static string PackageRestoreInstallingMessage {
            get {
                return ResourceManager.GetString("PackageRestoreInstallingMessage", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No missing packages found..
        /// </summary>
        public static string PackageRestoreNoMissingPackages {
            get {
                return ResourceManager.GetString("PackageRestoreNoMissingPackages", resourceCulture);
            }
        }
    }
}
