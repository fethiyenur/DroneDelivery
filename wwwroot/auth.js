// ── Sabitler ────────────────────────────────────────────────────────────────
const BASE = "https://localhost:53374";

// ── Token yönetimi ───────────────────────────────────────────────────────────
function getToken()   { return localStorage.getItem("accessToken"); }
function getUser()    { return JSON.parse(localStorage.getItem("user") || "{}"); }
function isLoggedIn() { return !!getToken(); }

function requireAuth() {
  if (!isLoggedIn()) { window.location.href = "/login.html"; return false; }
  return true;
}

function requireAdmin() {
  if (!requireAuth()) return false;
  if (getUser().role !== "Admin") { window.location.href = "/dashboard.html"; return false; }
  return true;
}

// ── API çağrısı (token otomatik eklenir) ────────────────────────────────────
async function apiFetch(path, options = {}) {
  options.headers = options.headers || {};
  options.headers["Content-Type"] = "application/json";
  options.headers["Authorization"] = "Bearer " + getToken();

  var res = await fetch(BASE + path, options);

  // Token süresi dolmuşsa refresh dene
  if (res.status === 401) {
    var refreshed = await tryRefresh();
    if (refreshed) {
      options.headers["Authorization"] = "Bearer " + getToken();
      res = await fetch(BASE + path, options);
    } else {
      logout();
      return null;
    }
  }
  return res;
}

async function tryRefresh() {
  var rt = localStorage.getItem("refreshToken");
  if (!rt) return false;
  try {
    var res = await fetch(BASE + "/api/Auth/refresh", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ refreshToken: rt })
    });
    if (!res.ok) return false;
    var data = await res.json();
    localStorage.setItem("accessToken", data.accessToken);
    localStorage.setItem("refreshToken", data.refreshToken);
    localStorage.setItem("user", JSON.stringify(data.user));
    return true;
  } catch { return false; }
}

async function logout() {
  var rt = localStorage.getItem("refreshToken");
  if (rt) {
    try {
      await fetch(BASE + "/api/Auth/logout", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ refreshToken: rt })
      });
    } catch {}
  }
  localStorage.clear();
  window.location.href = "/login.html";
}

// ── Navbar render ────────────────────────────────────────────────────────────
function renderNavbar(activePage) {
  var user = getUser();
  var isAdmin = user.role === "Admin";

  var navItems = [
    { href: "/dashboard.html", label: "Dashboard",      id: "dashboard" },
    { href: "/map.html",       label: "Canli Harita",   id: "map"       },
    { href: "/drone3d.html",   label: "3D Gorunum",     id: "drone3d"   },
    { href: "/history.html",   label: "Gecmis Veriler", id: "history"   },
    { href: "/profile.html",   label: "Profilim",       id: "profile"   },
  ];
  if (isAdmin) {
    navItems.push({ href: "/admin.html", label: "Admin Paneli", id: "admin" });
  }

  var roleBg = { Guest: "#21262d", Subscriber: "#1a3a2a", Admin: "#2d1a3a" };
  var roleColor = { Guest: "#8b949e", Subscriber: "#3fb950", Admin: "#a371f7" };
  var role = user.role || "Guest";

  var navHtml = `
  <nav style="background:#161b22;border-bottom:1px solid #30363d;padding:0 20px;display:flex;align-items:center;justify-content:space-between;height:56px;position:sticky;top:0;z-index:100;">
    <div style="display:flex;align-items:center;gap:24px;">
      <span style="font-size:16px;font-weight:700;color:#58a6ff;">Drone Kurye</span>
      <div style="display:flex;gap:4px;">
        ${navItems.map(item => `
          <a href="${item.href}" style="padding:6px 14px;border-radius:6px;font-size:13px;text-decoration:none;
            background:${activePage===item.id?"#21262d":"transparent"};
            color:${activePage===item.id?"#e6edf3":"#8b949e"};
            border:1px solid ${activePage===item.id?"#30363d":"transparent"};">
            ${item.label}
          </a>
        `).join("")}
      </div>
    </div>
    <div style="display:flex;align-items:center;gap:10px;">
      <span style="font-size:13px;color:#e6edf3;">${user.fullName || user.email || ""}</span>
      <span style="font-size:11px;padding:2px 8px;border-radius:20px;background:${roleBg[role]||"#21262d"};color:${roleColor[role]||"#8b949e"};">${role}</span>
      <button onclick="logout()" style="background:transparent;border:1px solid #30363d;border-radius:6px;padding:5px 12px;color:#f85149;font-size:12px;cursor:pointer;">Cikis</button>
    </div>
  </nav>`;

  document.getElementById("navbar-container").innerHTML = navHtml;
}
