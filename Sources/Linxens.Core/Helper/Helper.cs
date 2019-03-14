using System;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Linxens.Core.Helper
{
    public static class Helper
    {
        public static bool HasWritePermissionOnDir(string path)
        {
            bool writeAllow = false;
            bool writeDeny = false;
            DirectorySecurity accessControlList = Directory.GetAccessControl(path);
            if (accessControlList == null) return false;
            AuthorizationRuleCollection accessRules = accessControlList.GetAccessRules(true, true,
                typeof(SecurityIdentifier));
            if (accessRules == null) return false;

            foreach (FileSystemAccessRule rule in accessRules)
            {
                if ((FileSystemRights.Write & rule.FileSystemRights) != FileSystemRights.Write) continue;

                if (rule.AccessControlType == AccessControlType.Allow)
                    writeAllow = true;
                else if (rule.AccessControlType == AccessControlType.Deny) writeDeny = true;
            }

            return writeAllow && !writeDeny;
        }

        public static string ToString(this Enum val, int nbrChar)
        {
            string returnValue = "";
            string strVal = val.ToString();

            if (strVal.Length < nbrChar)
            {
                returnValue = strVal;
                for (int i = strVal.Length; i < nbrChar; i++) returnValue += " ";

                return returnValue;
            }

            return strVal.Substring(0, nbrChar);
        }
    }
}