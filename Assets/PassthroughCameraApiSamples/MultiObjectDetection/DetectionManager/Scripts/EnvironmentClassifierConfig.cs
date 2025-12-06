// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace PassthroughCameraSamples.MultiObjectDetection
{
    [CreateAssetMenu(fileName = "EnvironmentClassifierConfig", menuName = "Passthrough/Environment Classifier Config", order = 0)]
    public class EnvironmentClassifierConfig : ScriptableObject
    {
        [System.Serializable]
        public class Rule
        {
            public string Label;
            public float Weight = 1f;
        }

        [System.Serializable]
        public class EnvironmentProfile
        {
            public string EnvironmentName;
            public float MinScore = 1f;
            public List<Rule> Rules = new();
        }

        [SerializeField] private List<EnvironmentProfile> m_profiles = new();
        [SerializeField] private string m_unknownEnvironmentLabel = "Unknown";

        public bool TryClassify(IReadOnlyList<string> labels, out string environment, out float score)
        {
            environment = m_unknownEnvironmentLabel;
            score = 0f;

            if (labels == null || labels.Count == 0 || m_profiles.Count == 0)
            {
                return false;
            }

            var labelCounts = new Dictionary<string, int>();
            for (var i = 0; i < labels.Count; i++)
            {
                var label = labels[i];
                if (string.IsNullOrWhiteSpace(label))
                {
                    continue;
                }

                var key = label.Trim();
                if (labelCounts.TryGetValue(key, out var existing))
                {
                    labelCounts[key] = existing + 1;
                }
                else
                {
                    labelCounts[key] = 1;
                }
            }

            if (labelCounts.Count == 0)
            {
                return false;
            }

            var bestScore = 0f;
            string bestEnvironment = m_unknownEnvironmentLabel;
            var found = false;

            foreach (var profile in m_profiles)
            {
                if (profile == null || string.IsNullOrWhiteSpace(profile.EnvironmentName))
                {
                    continue;
                }

                var profileScore = 0f;
                for (var r = 0; r < profile.Rules.Count; r++)
                {
                    var rule = profile.Rules[r];
                    if (rule == null || string.IsNullOrWhiteSpace(rule.Label))
                    {
                        continue;
                    }

                    var ruleLabel = rule.Label.Trim();
                    if (labelCounts.TryGetValue(ruleLabel, out var count))
                    {
                        profileScore += count * Mathf.Max(0f, rule.Weight);
                    }
                }

                if (profileScore < Mathf.Max(0f, profile.MinScore))
                {
                    continue;
                }

                if (!found || profileScore > bestScore)
                {
                    bestScore = profileScore;
                    bestEnvironment = profile.EnvironmentName.Trim();
                    found = true;
                }
            }

            if (found)
            {
                environment = bestEnvironment;
                score = bestScore;
                return true;
            }

            return false;
        }

        public string GetUnknownLabel()
        {
            return m_unknownEnvironmentLabel;
        }
    }
}
