/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UninstallTools.Junk.Containers;

namespace UninstallTools.Junk.Cleanup
{
    /// <summary>
    /// Immutable risk evaluation for a cleanup candidate.
    /// </summary>
    public sealed class CleanupRiskAssessment
    {
        public CleanupRiskAssessment(CleanupRiskLevel level, params string[] reasons)
            : this(level, (IEnumerable<string>)reasons)
        {
        }

        public CleanupRiskAssessment(CleanupRiskLevel level, IEnumerable<string> reasons)
        {
            Level = level;
            Reasons = new ReadOnlyCollection<string>((reasons ?? Enumerable.Empty<string>())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.Ordinal)
                .ToList());
        }

        public CleanupRiskLevel Level { get; }
        public IReadOnlyList<string> Reasons { get; }
    }

    /// <summary>
    /// Evaluates operational risk independently from match confidence.
    /// </summary>
    public interface ICleanupRiskClassifier
    {
        CleanupRiskAssessment Classify(IJunkResult result);
    }
}
