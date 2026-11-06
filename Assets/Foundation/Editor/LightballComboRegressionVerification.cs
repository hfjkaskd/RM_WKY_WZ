using System;
using System.IO;
using UnityEngine;

namespace Whitebox.Foundation.Editor
{
    // One source-integrity snapshot covers the restored special strategies and
    // their default lightball regression, including pool reuse between suites.
    public static class LightballComboRegressionVerification
    {
        private static bool includePropellers;
        private static string ReportPath => Path.Combine(Application.dataPath, includePropellers
            ? "../live_lightball_combo_regression_verification.json" : "../live_lightball_combos_verification.json");
        public static void Run() => Start(false);
        public static void RunWithPropellers() => Start(true);
        private static void Start(bool withPropellers)
        {
            includePropellers = withPropellers;
            File.WriteAllText(ReportPath, "{\"passed\":false,\"status\":\"running\"}\n");
            if (includePropellers)
            {
                LightballPropellerComboVerification.OnFinished += PropellerFinished;
                LightballPropellerComboVerification.Run();
            }
            else StartTnt();
        }
        private static void PropellerFinished()
        {
            LightballPropellerComboVerification.OnFinished -= PropellerFinished;
            var result = Read("live_lightball_propeller_combo_verification.json");
            Require(result.passed && result.completed_combos == 4, "Four propeller combinations pass before the TNT/default/rocket regression");
            StartTnt();
        }
        private static void StartTnt()
        {
            LightballTntComboVerification.OnFinished += TntFinished;
            LightballTntComboVerification.Run();
        }
        private static void TntFinished()
        {
            LightballTntComboVerification.OnFinished -= TntFinished;
            var result = Read("live_lightball_tnt_combo_verification.json");
            Require(result.passed && result.completed_combos == 4 && result.normal_lightball_activations == 2, "Four TNT combos and both default lightball paths pass first");
            LightballRocketComboVerification.OnFinished += RocketFinished;
            LightballRocketComboVerification.Run();
        }
        private static void RocketFinished()
        {
            LightballRocketComboVerification.OnFinished -= RocketFinished;
            var tnt = Read("live_lightball_tnt_combo_verification.json"); var rocket = Read("live_lightball_rocket_combo_verification.json");
            Require(rocket.passed && rocket.completed_combos == 8, "All eight rocket combo fixtures pass after the preceding six operations");
            var propellers = includePropellers ? Read("live_lightball_propeller_combo_verification.json").completed_combos : 0;
            int operations = propellers + tnt.completed_combos + tnt.normal_lightball_activations + rocket.completed_combos;
            Require(rocket.left_moves == 30 - operations, "The unchanged formal board consumes one move per operation across all suites");
            File.WriteAllText(ReportPath, JsonUtility.ToJson(new Report { passed = true, propeller_combos = propellers, tnt_combos = tnt.completed_combos,
                default_lightballs = tnt.normal_lightball_activations, rocket_combos = rocket.completed_combos, left_moves = rocket.left_moves,
                scope = "One unchanged formal runtime/board sequentially runs " + operations + " operations through the existing recorded input: " + propellers
                    + " lightball/propeller combos, four lightball/TNT combos, normal lightball tap/swap and eight lightball/rocket combos. Individual reports retain detailed assertions; this transport verifies continuation, move consumption and shared pool reuse. Natural combination creation, physical input, full lifecycle and rendered-original parity remain unverified." }, true) + "\n");
        }
        private static SubResult Read(string name) => JsonUtility.FromJson<SubResult>(File.ReadAllText(Path.Combine(Application.dataPath, "../" + name)));
        private static void Require(bool value, string label) { if (!value) throw new InvalidOperationException(label); }
        [Serializable] private sealed class SubResult { public bool passed; public int completed_combos, normal_lightball_activations, left_moves; }
        [Serializable] private sealed class Report
        {
            public bool passed; public int propeller_combos, tnt_combos, default_lightballs, rocket_combos, left_moves;
            public string timestamp_utc = DateTime.UtcNow.ToString("o");
            public string scope;
        }
    }
}
