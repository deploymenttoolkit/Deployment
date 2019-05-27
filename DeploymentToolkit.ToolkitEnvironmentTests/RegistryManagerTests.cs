using Microsoft.VisualStudio.TestTools.UnitTesting;
using DeploymentToolkit.ToolkitEnvironment;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace DeploymentToolkit.ToolkitEnvironment.Tests
{
    [TestClass()]
    public class RegistryManagerTests
    {
        [TestMethod()]
        public void GetInstalledMSIProgramsTest()
        {
            var programs = RegistryManager.GetInstalledMSIPrograms();

            Assert.IsNotNull(programs, "Exception getting programs");
            Assert.IsTrue(programs.Count > 0, "No programs found");

            foreach(var program in programs)
            {
                Assert.IsTrue(program.UninstallString.ToLower().Contains("msiexec"), "Invalid MSI");
                Assert.IsTrue(Guid.TryParse(program.ProductId, out var guid), "Invalid ProductId");
                Assert.IsNotNull(guid, "Invalid parsed ProductId");

                Debug.WriteLine($"[{program.ProductId}][{program.DisplayVersion}] {program.DisplayName}");
            }
        }

        [TestMethod()]
        public void GetInstalledProgramByNameTest()
        {
            var programs = RegistryManager.GetInstalledMSIProgramsByName("Visual Studio*");

            Assert.IsNotNull(programs, "Exception getting programs");
            Assert.IsTrue(programs.Count > 0, "Visual Studio not found");

            foreach(var program in programs)
            {
                Assert.IsTrue(program.UninstallString.ToLower().Contains("msiexec"), "Invalid MSI");
                Assert.IsTrue(Guid.TryParse(program.ProductId, out var guid), "Invalid ProductId");
                Assert.IsNotNull(guid, "Invalid parsed ProductId");

                Debug.WriteLine($"[{program.ProductId}][{program.DisplayVersion}] {program.DisplayName}");
            }
        }

        [TestMethod()]
        public void GetInstallProgramByNameExactTest()
        {
            var programName = "Microsoft .NET Framework 4.7.2 SDK";
            var programs = RegistryManager.GetInstalledMSIProgramsByName(programName, true);

            Assert.IsNotNull(programs, $"Exception getting {programName}");
            Assert.IsTrue(programs.Count == 1, $"{programName} not found");

            foreach (var program in programs)
            {
                Assert.IsTrue(program.UninstallString.ToLower().Contains("msiexec"), "Invalid MSI");
                Assert.IsTrue(Guid.TryParse(program.ProductId, out var guid), "Invalid ProductId");
                Assert.IsNotNull(guid, "Invalid parsed ProductId");

                Debug.WriteLine($"[{program.ProductId}][{program.DisplayVersion}] {program.DisplayName}");
            }
        }
    }
}