using System;
using System.IO;

namespace DdrGui.Resources
{
    /// <summary>
    /// Directory paths for accessing static resource files
    /// </summary>
    public static class ResourcePaths
    {
        /// <summary>
        /// Base folder for all Plywood resources
        /// </summary>
        public static string Base => Path.Combine(AppContext.BaseDirectory, "Resources/");

        /// <summary>
        /// Base folder for Plywood images
        /// </summary>
        public static string Images => Path.Combine(AppContext.BaseDirectory, "Resources/Images/");

    }
}
