using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DeploymentToolkit.Actions.Utils
{
    public static class User
    {
        private const string _allUserProfile = @"C:\Users\All Users";
        private const string _defaultUserProfile = @"C:\Users\Default User";

        private static List<string> _userFolders;
        internal static List<string> GetUserFolders(bool includeDefaultProfile, bool includePublicProfile)
        {
            if (_userFolders == null)
            {
                var userDirectories = Directory.GetDirectories(@"C:\Users").ToList();

                if (userDirectories.Contains(_allUserProfile))
                    userDirectories.Remove(_allUserProfile);
                if (userDirectories.Contains(_defaultUserProfile))
                    userDirectories.Remove(_defaultUserProfile);

                _userFolders = userDirectories;
            }

            if (!includeDefaultProfile && includePublicProfile)
                return _userFolders;

            var copy = _userFolders.ToList();
            if (includeDefaultProfile)
                copy.Add(@"C:\Users\Default");
            if (includePublicProfile)
                copy.Add(@"C:\Users\Public");

            return copy;
        }
    }
}
