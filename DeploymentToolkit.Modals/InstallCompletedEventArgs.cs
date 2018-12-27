using System;
using System.Collections.Generic;

namespace DeploymentToolkit.Modals
{
    public class InstallCompletedEventArgs : EventArgs
    {
        public bool InstallSuccessful { get; set; }

        public int CountErrors { get; set; }
        public int CountWarnings { get; set; }

        public List<Exception> InstallErrors { get; set; } = new List<Exception>();
        public List<Exception> InstallWarnings { get; set; } = new List<Exception>();
    }
}
