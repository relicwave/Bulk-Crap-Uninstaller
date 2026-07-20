/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UninstallTools.Junk.Containers;

namespace UninstallTools.Junk.Cleanup
{
    /// <summary>
    /// Conservative filesystem risk classifier. It only returns Low when a
    /// candidate is contained by the target application's registered install
    /// location and no other known application shares that location.
    /// </summary>
    public sealed class FileSystemCleanupRiskClassifier : ICleanupRiskClassifier
    {
        private readonly IReadOnlyList<ApplicationUninstallerEntry> _applications;
        private readonly IReadOnlyList<string> _criticalRoots;

        public FileSystemCleanupRiskClassifier(IEnumerable<ApplicationUninstallerEntry> applications)
            : this(applications, GetDefaultCriticalRoots())
        {
        }

        internal FileSystemCleanupRiskClassifier(IEnumerable<ApplicationUninstallerEntry> applications,
            IEnumerable<string> criticalRoots)
        {
            _applications = (applications ?? throw new ArgumentNullException(nameof(applications))).ToList();
            _criticalRoots = (criticalRoots ?? throw new ArgumentNullException(nameof(criticalRoots)))
                .Select(NormalizePath)
                .Where(x => x != null)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public CleanupRiskAssessment Classify(IJunkResult result)
        {
            if (result == null)
                throw new ArgumentNullException(nameof(result));

            if (result is not FileSystemJunk filesystemResult || filesystemResult.Path == null)
                return new CleanupRiskAssessment(CleanupRiskLevel.High,
                    "No filesystem-specific risk classification is available.");

            var candidatePath = NormalizePath(filesystemResult.Path.FullName);
            if (candidatePath == null)
                return new CleanupRiskAssessment(CleanupRiskLevel.Critical,
                    "The candidate path is empty or invalid.");

            if (IsFilesystemRoot(candidatePath))
                return new CleanupRiskAssessment(CleanupRiskLevel.Critical,
                    "The candidate is a filesystem root.");

            var protectedRoot = _criticalRoots.FirstOrDefault(root => PathsEqual(candidatePath, root));
            if (protectedRoot != null)
                return new CleanupRiskAssessment(CleanupRiskLevel.Critical,
                    "The candidate is a protected Windows or profile root.");

            if (_criticalRoots.Any(root => IsDescendant(root, candidatePath)))
                return new CleanupRiskAssessment(CleanupRiskLevel.Critical,
                    "The candidate is an ancestor of a protected Windows or profile root.");

            var target = result.Application;
            var targetInstallLocation = NormalizePath(target?.InstallLocation);
            if (targetInstallLocation == null)
                return new CleanupRiskAssessment(CleanupRiskLevel.High,
                    "The application has no registered installation location.");

            if (!IsSameOrDescendant(candidatePath, targetInstallLocation))
                return new CleanupRiskAssessment(CleanupRiskLevel.High,
                    "The candidate is outside the application's registered installation location.");

            var sharingApplications = _applications
                .Where(x => !ReferenceEquals(x, target))
                .Where(x => IsSameOrDescendant(candidatePath, NormalizePath(x.InstallLocation)) ||
                            IsSameOrDescendant(NormalizePath(x.InstallLocation), candidatePath))
                .ToList();

            if (sharingApplications.Count > 0)
                return new CleanupRiskAssessment(CleanupRiskLevel.Critical,
                    "The candidate overlaps another application's registered installation location.");

            if (PathsEqual(candidatePath, targetInstallLocation))
                return new CleanupRiskAssessment(CleanupRiskLevel.Medium,
                    "The candidate is the application's installation root and requires review.");

            return new CleanupRiskAssessment(CleanupRiskLevel.Low,
                "The candidate is contained by an exclusive registered installation location.");
        }

        private static IEnumerable<string> GetDefaultCriticalRoots()
        {
            yield return Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.System);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.SystemX86);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            yield return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }

        internal static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            try
            {
                var fullPath = Path.GetFullPath(path.Trim().Trim('"'));
                var root = Path.GetPathRoot(fullPath);
                if (root != null && PathsEqual(fullPath, root))
                    return root.TrimEnd(Path.AltDirectorySeparatorChar);

                return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException ||
                                       ex is PathTooLongException || ex is System.Security.SecurityException)
            {
                return null;
            }
        }

        private static bool IsFilesystemRoot(string path)
        {
            var root = Path.GetPathRoot(path);
            return root != null && PathsEqual(path, root.TrimEnd(Path.AltDirectorySeparatorChar));
        }

        private static bool IsSameOrDescendant(string path, string possibleParent)
        {
            return path != null && possibleParent != null &&
                   (PathsEqual(path, possibleParent) || IsDescendant(path, possibleParent));
        }

        private static bool IsDescendant(string path, string possibleParent)
        {
            if (path == null || possibleParent == null || PathsEqual(path, possibleParent))
                return false;

            var parentWithSeparator = possibleParent + Path.DirectorySeparatorChar;
            return path.StartsWith(parentWithSeparator, StringComparison.OrdinalIgnoreCase);
        }

        private static bool PathsEqual(string left, string right)
        {
            return string.Equals(left?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                right?.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase);
        }
    }
}
