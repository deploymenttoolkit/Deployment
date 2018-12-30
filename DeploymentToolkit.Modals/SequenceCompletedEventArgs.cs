﻿using System;
using System.Collections.Generic;

namespace DeploymentToolkit.Modals
{
    public class SequenceCompletedEventArgs : EventArgs
    {
        public bool InstallSuccessful { get; set; }
        public int ReturnCode { get; set; }

        public int CountErrors { get => SequenceErrors?.Count ?? 0; }
        public int CountWarnings { get => SequenceWarnings?.Count ?? 0; }

        public List<Exception> SequenceErrors { get; set; } = new List<Exception>();
        public List<Exception> SequenceWarnings { get; set; } = new List<Exception>();
    }
}