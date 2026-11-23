const REGION_PATH = "/api/v1/rm-wky-wz/region";
const ALLOWED_COUNTRIES = new Set(["US", "BR", "ID"]);

export default {
  fetch(request, env) {
    if (new URL(request.url).pathname !== REGION_PATH) {
      return new Response(null, { status: 404 });
    }

    if (request.method !== "GET") {
      return new Response(null, { status: 405, headers: { Allow: "GET" } });
    }

    const setting = String(env.COUNTRY_FILTER_ENABLED ?? "").trim().toLowerCase();
    const filterEnabled = setting !== "false" && setting !== "0";
    const vpnSetting = String(env.VPN_DETECTION_ENABLED ?? "").trim().toLowerCase();
    const vpnDetectionEnabled = vpnSetting !== "false" && vpnSetting !== "0";
    const allowed = !filterEnabled || ALLOWED_COUNTRIES.has(request.cf?.country);

    return new Response(null, {
      status: allowed ? 204 : 403,
      headers: {
        "Cache-Control": "no-store",
        "X-Game-Region-Policy": "rm-wky-wz-v1",
        "X-Game-Vpn-Detection": String(vpnDetectionEnabled),
      },
    });
  },
};
