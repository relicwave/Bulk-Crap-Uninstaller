/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System.Collections.Generic;

namespace UninstallTools.Junk.Cleanup
{
    /// <summary>
    /// Entry point for non-destructive leftover discovery.
    /// </summary>
    public static class CleanupPlanManager
    {
        public static CleanupPlan FindCleanupPlan(IEnumerable<ApplicationUninstallerEntry> targets,
            ICollection<ApplicationUninstallerEntry> allUninstallers,
            ListGenerationProgress.ListGenerationCallback progressCallback)
        {
            return CleanupPlanBuilder.Build(JunkManager.FindJunk(targets, allUninstallers, progressCallback));
        }
    }
}
