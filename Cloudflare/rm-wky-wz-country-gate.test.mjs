import assert from "node:assert/strict";
import test from "node:test";
import worker from "./rm-wky-wz-country-gate.mjs";

const url = "https://example.test/api/v1/rm-wky-wz/region";

test("VPN switch is independent of country access and never cached", () => {
  const settings = [
    [undefined, "true"], ["true", "true"], ["1", "true"],
    ["false", "false"], ["0", "false"], [" FALSE ", "false"],
    ["", "true"], ["invalid", "true"],
  ];
  for (const [setting, expected] of settings) {
    for (const country of ["US", "BR", "ID", "FR", undefined]) {
      for (const countryFilter of ["true", "false"]) {
        const request = new Request(url);
        Object.defineProperty(request, "cf", { value: { country } });
        const response = worker.fetch(request, {
          VPN_DETECTION_ENABLED: setting,
          COUNTRY_FILTER_ENABLED: countryFilter,
        });
        const allowed = countryFilter === "false" || ["US", "BR", "ID"].includes(country);
        assert.equal(response.status, allowed ? 204 : 403);
        assert.equal(response.headers.get("X-Game-Vpn-Detection"), expected);
        assert.equal(response.headers.get("X-Game-Region-Policy"), "rm-wky-wz-v1");
        assert.equal(response.headers.get("Cache-Control"), "no-store");
      }
    }
  }
});

test("changing the switch is reflected in the next request", () => {
  const request = new Request(url);
  const env = { COUNTRY_FILTER_ENABLED: "false", VPN_DETECTION_ENABLED: "true" };
  for (const enabled of ["true", "false", "true"]) {
    env.VPN_DETECTION_ENABLED = enabled;
    assert.equal(worker.fetch(request, env).headers.get("X-Game-Vpn-Detection"), enabled);
  }
});

test("unrelated paths and methods remain rejected", () => {
  assert.equal(worker.fetch(new Request("https://example.test/other"), {}).status, 404);
  const response = worker.fetch(new Request(url, { method: "POST" }), {});
  assert.equal(response.status, 405);
  assert.equal(response.headers.get("Allow"), "GET");
});
