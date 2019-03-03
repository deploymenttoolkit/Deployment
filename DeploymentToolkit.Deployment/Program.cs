using DeploymentToolkit.DeploymentEnvironment;
using DeploymentToolkit.Installer.Executable;
using DeploymentToolkit.Installer.MSI;
using DeploymentToolkit.Modals;
using DeploymentToolkit.Modals.Settings;
using DeploymentToolkit.Modals.Settings.Install;
using DeploymentToolkit.Modals.Settings.Uninstall;
using DeploymentToolkit.ToolkitEnvironment;
using DeploymentToolkit.ToolkitEnvironment.Exceptions;
using DeploymentToolkit.Uninstaller.Executable;
using DeploymentToolkit.Uninstaller.MSI;
using NLog;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace DeploymentToolkit.Deployment
{
    class Program
    {
        public static int GlobalExitCode = (int)ExitCode.ExitOK;

        private static string _namespace;
        internal static string Namespace
        {
            get
            {
                if (string.IsNullOrEmpty(_namespace))
                    _namespace = typeof(Program).Namespace;
                return _namespace;
            }
        }

        private static Version _version;
        internal static Version Version
        {
            get
            {
                if (_version == null)
                    _version = Assembly.GetExecutingAssembly().GetName().Version;
                return _version;
            }
        }

        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private static MainSequence _mainSequence;
        private static bool _sequenceCompleted = false;

        static void Main(string[] args)
        {
            try
            {
                Logging.LogManager.Initialize("Deployment");
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Failed to initialize logger: {ex}");
                Console.ReadKey();
                Environment.Exit((int)ExitCode.FailedToInitilizeLogger);
            }

            _logger.Info($"Initialized {Namespace} v{Version}");

            _logger.Trace("Trying to read settings...");

            if(!File.Exists("settings.xml"))
            {
                ExitInstallation("settings.xml is missing!", ExitCode.SettingsFileMissing);
            }

            EnvironmentVariables.Configuration = ReadXml<Configuration>("settings.xml");

            _logger.Trace("Successfully read settings");

            _logger.Info("Verifying install dependencies ...");
            _logger.Info($"IsAdministrator: {EnvironmentVariables.IsAdministrator}");
            _logger.Info($"IsElevated: {EnvironmentVariables.IsElevated}");
            if (!EnvironmentVariables.IsElevated)
            {
                ExitInstallation("Program has to be run as Administrator to function properly!", ExitCode.NotElevated);
            }

            try
            {
                _logger.Info($"DT-Installation Path: {EnvironmentVariables.DeploymentToolkitInstallPath}");
            }
            catch(DeploymentToolkitInstallPathNotFoundException)
            {
                ExitInstallation($"Could not get installation path of the deployment toolkit", ExitCode.DeploymentToolkitInstallPathNotFound);
            }

            _logger.Trace("Parsing command line arguments...");

            var arguments = args.ToList();
            var isInstallation = arguments.Any(argument => argument.ToLower() == "--install" || argument.ToLower() == "-i");
            if(isInstallation)
            {
                Install();
            }
            else
            {
                var isUninstallation = arguments.Any(argument => argument.ToLower() == "--uninstall" || argument.ToLower() == "-u");
                if (isUninstallation)
                {
                    Uninstall();
                }
                else
                {
                    ExitInstallation("Failed to install or uninstall. Neither install nor uninstall command line has been specified", ExitCode.MissingRequiredParameter);
                }
            }

            _logger.Info($"Ended {Namespace} v{Version}");
#if DEBUG && DEBUG_GUI
            Thread.Sleep(10000);
#endif
            Environment.Exit(GlobalExitCode);
        }

        private static T ReadXml<T>(string fileName)
        {
            try
            {
                return XML.ReadXml<T>(fileName);
            }
            catch (UnauthorizedAccessException ex)
            {
                ExitInstallation(ex, $"Failed to read {fileName}. Access to the file denied", ExitCode.FailedToReadSettings);
            }
            catch (InvalidOperationException ex)
            {
                ExitInstallation(ex, $"Failed to deserialized {fileName}. Verify that {fileName} is a valid xml file", ExitCode.FailedToReadSettings);
            }
            catch (Exception ex)
            {
                ExitInstallation(ex, $"Failed to read {fileName}", ExitCode.FailedToReadSettings);
            }
            return default(T);
        }

        private static void ExitInstallation(string message, ExitCode exitCode)
        {
            _logger.Fatal(message);
#if DEBUG
            Console.ReadKey();
#endif
            Environment.Exit((int)exitCode);
        }

        private static void ExitInstallation(Exception exception, string message, ExitCode exitCode)
        {
            _logger.Fatal(exception, message);
#if DEBUG
            Console.ReadKey();
#endif
            Environment.Exit((int)exitCode);
        }
        
        public static void Uninstall()
        {
            _logger.Info("Detected uninstall command line. Selected 'Uninstall' as deployment");
            if(EnvironmentVariables.Configuration.UninstallSettings == null)
            {
                _logger.Trace("No uninstall arguments specified in settings.xml. Looking for uninstall.xml");
                if (!File.Exists("uninstall.xml"))
                    ExitInstallation("uninstall.xml is missing", ExitCode.UninstallFileMissing);

                _logger.Trace("Found uninstall.xml. Reading...");
                EnvironmentVariables.UninstallSettings = ReadXml<UninstallSettings>("uninstall.xml");
                _logger.Trace("Successfully read uninstall.xml");
            }
            else
            {
                _logger.Trace("Uninstall options specified inside settings.xml");
                EnvironmentVariables.UninstallSettings = EnvironmentVariables.Configuration.UninstallSettings;
            }

            _logger.Trace("Read uninstall settings. Starting uninstallation...");
            _logger.Trace("Checking CommandLine Path...");

            var fullPath = Path.GetFullPath(EnvironmentVariables.UninstallSettings.CommandLine);
            if(EnvironmentVariables.UninstallSettings.CommandLine.Length != fullPath.Length)
            {
                _logger.Trace("Not a absolute path specified. Searching for file in 'Files' folder");
                var path = Path.Combine(DeploymentEnvironmentVariables.FilesDirectory, EnvironmentVariables.UninstallSettings.CommandLine);
                _logger.Trace($"Changed path from {EnvironmentVariables.UninstallSettings.CommandLine} to {path}");
                EnvironmentVariables.UninstallSettings.CommandLine = path;
            }

            _logger.Trace("Verifiying that file specified in CommandLine exists...");
            // CommandLine should either specify an exe file or an msi file. Either way the file has to exist
            if (!File.Exists(EnvironmentVariables.UninstallSettings.CommandLine))
            {
                ExitInstallation($"File specified in CommandLine does not exists ({EnvironmentVariables.UninstallSettings.CommandLine}). Aborting uninstallation", ExitCode.InvalidCommandLineSpecified);
            }

            // Detecting installation type
            try
            {
                var sequence = default(IInstallUninstallSequence);
                if (EnvironmentVariables.UninstallSettings.CommandLine.ToLower().EndsWith(".msi"))
                {
                    // Microsoft Installer
                    sequence = new MSIUninstaller(EnvironmentVariables.UninstallSettings);
                }
                else
                {
                    // Unknwon / EXE installer
                    sequence = new ExeUninstaller(EnvironmentVariables.UninstallSettings);
                }

                _mainSequence = new MainSequence(sequence);
                _mainSequence.OnSequenceCompleted += OnSequenceCompleted;
                _mainSequence.SequenceBegin();

                do
                {
                    Thread.Sleep(1000);
                }
                while (!_sequenceCompleted);
            }
            catch (Exception ex)
            {
                ExitInstallation(ex, "Error during uninstallation", ExitCode.ErrorDuringUninstallation);
            }

            _logger.Info("Uninstall sequence completed");
        }

        public static void Install()
        {
            _logger.Info("Detected install command line. Selected 'Install' as deployment");
            if (EnvironmentVariables.Configuration.InstallSettings == null)
            {
                _logger.Trace("No installation arguments specified in settings.xml. Looking for install.xml");
                if (!File.Exists("install.xml"))
                    ExitInstallation("install.xml is missing", ExitCode.InstallFileMissing);

                _logger.Trace("Found install.xml. Reading...");
                EnvironmentVariables.InstallSettings = ReadXml<InstallSettings>("install.xml");
                _logger.Trace("Successfully read install.xml");
            }
            else
            {
                _logger.Trace("Install options specified inside settings.xml");
                EnvironmentVariables.InstallSettings = EnvironmentVariables.Configuration.InstallSettings;
            }

            _logger.Info("Read install settings. Starting installation...");
            _logger.Trace("Checking CommandLine Path...");

            // If you specify a full path, then the length should stay the same
            var fullPath = Path.GetFullPath(EnvironmentVariables.InstallSettings.CommandLine);
            if (EnvironmentVariables.InstallSettings.CommandLine.Length != fullPath.Length)
            {
                _logger.Trace("Not a absolute path specified. Searching for file in 'Files' folder");
                var path = Path.Combine(DeploymentEnvironmentVariables.FilesDirectory, EnvironmentVariables.InstallSettings.CommandLine);
                _logger.Trace($"Changed path from {EnvironmentVariables.InstallSettings.CommandLine} to {path}");
                EnvironmentVariables.InstallSettings.CommandLine = path;
            }

            _logger.Trace("Verifiying that file specified in CommandLine exists...");
            // CommandLine should either specify an exe file or an msi file. Either way the file has to exist
            if (!File.Exists(EnvironmentVariables.InstallSettings.CommandLine))
            {
                ExitInstallation($"File specified in CommandLine does not exists ({EnvironmentVariables.InstallSettings.CommandLine}). Aborting installation", ExitCode.InvalidCommandLineSpecified);
            }

            // Detecting installation type
            try
            {
                var sequence = default(IInstallUninstallSequence);
                if (EnvironmentVariables.InstallSettings.CommandLine.ToLower().EndsWith(".msi"))
                {
                    // Microsoft Installer
                    sequence = new MSIInstaller(EnvironmentVariables.InstallSettings);
                }
                else
                {
                    // Unknwon / EXE installer
                    sequence = new ExeInstaller(EnvironmentVariables.InstallSettings);
                }

                _mainSequence = new MainSequence(sequence);
                _mainSequence.OnSequenceCompleted += OnSequenceCompleted;
                _mainSequence.SequenceBegin();

                do
                {
                    Thread.Sleep(1000);
                }
                while (!_sequenceCompleted);
            }
            catch (Exception ex)
            {
                ExitInstallation(ex, "Error during installation", ExitCode.ErrorDuringInstallation);
            }

            _logger.Info("Install sequence completed");
        }

        private static void OnSequenceCompleted(object sender, SequenceCompletedEventArgs e)
        {
            try
            {
                _logger.Info($"Exit code {e.ReturnCode}");
                GlobalExitCode = e.ReturnCode;

                _logger.Info("Sequence completed. Cleaning up...");
                // TODO: Cleanup tasks??
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error during validation of sequence completion");
            }
            finally
            {
                _sequenceCompleted = true;
            }
        }
    }
}
