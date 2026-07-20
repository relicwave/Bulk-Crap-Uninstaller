/*
    Copyright (c) 2017 Marcin Szeniak (https://github.com/Klocman/)
    Apache License Version 2.0
*/

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UninstallTools.Junk.Confidence;
using UninstallTools.Junk.Containers;

namespace UninstallTools.Junk.Cleanup
{
    /// <summary>
    /// Describes the potential impact of removing a cleanup candidate.
    /// Risk is intentionally independent from confidence: an item can be a
    /// confident match while still being dangerous because it is shared.
    /// </summary>
    public enum CleanupRiskLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Describes how a cleanup candidate should be presented to the user.
    /// </summary>
    public enum CleanupSelection
    {
        Automatic,
        ReviewRequired,
        Blocked
    }

    /// <summary>
    /// A single reason contributing to the attribution of a cleanup candidate.
    /// </summary>
    public sealed class CleanupEvidence
    {
        public CleanupEvidence(int weight, string reason)
        {
            Weight = weight;
            Reason = reason ?? string.Empty;
        }

        public int Weight { get; }
        public string Reason { get; }
    }

    /// <summary>
    /// Read-only description of one item discovered by the junk scanners.
    /// It contains no destructive operations; deletion remains owned by the
    /// underlying <see cref="IJunkResult"/> until transactional execution is added.
    /// </summary>
    public sealed class CleanupCandidate
    {
        internal CleanupCandidate(IJunkResult result, CleanupRiskLevel risk)
        {
            Result = result ?? throw new ArgumentNullException(nameof(result));
            Risk = risk;
            Confidence = result.Confidence.GetConfidence();
            DisplayName = result.GetDisplayName();
            Evidence = new ReadOnlyCollection<CleanupEvidence>(result.Confidence.ConfidenceParts
                .Select(x => new CleanupEvidence(x.Change, x.Reason))
                .ToList());
            Selection = CleanupSafetyPolicy.Evaluate(Confidence, Risk);
        }

        public IJunkResult Result { get; }
        public string DisplayName { get; }
        public ConfidenceLevel Confidence { get; }
        public CleanupRiskLevel Risk { get; }
        public CleanupSelection Selection { get; }
        public IReadOnlyList<CleanupEvidence> Evidence { get; }
    }

    /// <summary>
    /// Immutable dry-run representation of a cleanup operation.
    /// </summary>
    public sealed class CleanupPlan
    {
        internal CleanupPlan(IEnumerable<CleanupCandidate> candidates)
        {
            var candidateList = candidates?.ToList() ?? throw new ArgumentNullException(nameof(candidates));
            Candidates = new ReadOnlyCollection<CleanupCandidate>(candidateList);
            AutomaticCandidates = new ReadOnlyCollection<CleanupCandidate>(candidateList
                .Where(x => x.Selection == CleanupSelection.Automatic)
                .ToList());
            ReviewCandidates = new ReadOnlyCollection<CleanupCandidate>(candidateList
                .Where(x => x.Selection == CleanupSelection.ReviewRequired)
                .ToList());
            BlockedCandidates = new ReadOnlyCollection<CleanupCandidate>(candidateList
                .Where(x => x.Selection == CleanupSelection.Blocked)
                .ToList());
        }

        public IReadOnlyList<CleanupCandidate> Candidates { get; }
        public IReadOnlyList<CleanupCandidate> AutomaticCandidates { get; }
        public IReadOnlyList<CleanupCandidate> ReviewCandidates { get; }
        public IReadOnlyList<CleanupCandidate> BlockedCandidates { get; }
    }

    /// <summary>
    /// Central safety rule used by GUI and console cleanup planning.
    /// Automatic selection requires both very high confidence and explicitly low risk.
    /// </summary>
    public static class CleanupSafetyPolicy
    {
        public static CleanupSelection Evaluate(ConfidenceLevel confidence, CleanupRiskLevel risk)
        {
            if (risk == CleanupRiskLevel.Critical || confidence == ConfidenceLevel.Bad)
                return CleanupSelection.Blocked;

            if (risk == CleanupRiskLevel.Low && confidence == ConfidenceLevel.VeryGood)
                return CleanupSelection.Automatic;

            return CleanupSelection.ReviewRequired;
        }
    }

    /// <summary>
    /// Converts scanner results into a non-destructive cleanup plan.
    /// Until dedicated risk classifiers are implemented, the default builder
    /// treats every candidate as high risk and therefore never auto-selects it.
    /// </summary>
    public static class CleanupPlanBuilder
    {
        public static CleanupPlan Build(IEnumerable<IJunkResult> results)
        {
            return Build(results, _ => CleanupRiskLevel.High);
        }

        public static CleanupPlan Build(IEnumerable<IJunkResult> results,
            Func<IJunkResult, CleanupRiskLevel> riskResolver)
        {
            if (results == null)
                throw new ArgumentNullException(nameof(results));
            if (riskResolver == null)
                throw new ArgumentNullException(nameof(riskResolver));

            return new CleanupPlan(results.Select(result =>
                new CleanupCandidate(result, riskResolver(result))));
        }
    }
}
