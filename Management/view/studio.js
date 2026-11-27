// Tab "Studio" của trang quản lý: mọi art đã có bản Simple hay Detail, và bản game đang dùng (dấu ✓ xanh ở góc ảnh).
// Bấm vào ảnh kia là đổi: không hỏi lại, lưu ngay (POST /api/art/choose, Management/tools/art.py choose).
// GreyBox không có ảnh nên không hiện. Dữ liệu: window.CORESIMPLE_ART (data/art.js, sync_page.py sinh từ art.py).
// index.html gọi: CORESIMPLE_VIEWS.studio(pane, { el, add, put, section, setup }).
(function () {
  "use strict";

  var CSS = [
    ".ar-bar { display: flex; flex-wrap: wrap; gap: 6px 8px; align-items: center; margin: 0 0 14px; }",
    ".ar-chip { padding: 4px 10px; border: 1px solid var(--line); border-radius: 999px; background: var(--panel); color: var(--text); font: inherit; font-size: 13px; cursor: pointer; }",
    ".ar-chip span { color: var(--muted); margin-left: 4px; font-variant-numeric: tabular-nums; }",
    ".ar-chip[aria-pressed='true'] { background: var(--accent); border-color: var(--accent); color: var(--panel); }",
    ".ar-chip[aria-pressed='true'] span { color: inherit; opacity: .8; }",
    ".ar-chip:focus-visible, .ar-pic:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }",
    ".ar-find { flex: 1; min-width: 140px; max-width: 260px; padding: 5px 10px; border: 1px solid var(--line); border-radius: 999px; background: var(--bg); color: var(--text); font: inherit; font-size: 13px; }",
    ".ar-hint { margin: 0 0 14px; font-size: 13px; color: var(--muted); }",
    ".ar-hint b { color: var(--ok); }",
    ".ar-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(190px, 1fr)); gap: 12px; }",
    ".ar-card { display: flex; flex-direction: column; gap: 6px; padding: 8px; border: 1px solid var(--line); border-radius: 10px; background: var(--panel); min-width: 0; }",
    ".ar-name { font-size: 13px; font-weight: 600; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }",
    ".ar-name small { font-weight: 400; color: var(--muted); margin-left: 6px; }",
    ".ar-pics { display: grid; grid-template-columns: 1fr 1fr; gap: 6px; }",
    ".ar-pics.one { grid-template-columns: 1fr; }",
    ".ar-pic { position: relative; display: block; padding: 0; border: 2px solid transparent; border-radius: 8px; aspect-ratio: 1; overflow: hidden; cursor: pointer; " +
      "background-color: var(--bg); background-image: linear-gradient(45deg, var(--line) 25%, transparent 25%, transparent 75%, var(--line) 75%), " +
      "linear-gradient(45deg, var(--line) 25%, transparent 25%, transparent 75%, var(--line) 75%); background-size: 12px 12px; background-position: 0 0, 6px 6px; }",
    ".ar-pic img { display: block; width: 100%; height: 100%; object-fit: contain; }",
    ".ar-pic .ar-box { display: flex; width: 100%; height: 100%; align-items: center; justify-content: center; font-size: 18px; font-weight: 600; color: var(--muted); }",
    ".ar-pic.off { opacity: .45; }",
    ".ar-pic.off:hover { opacity: .8; }",
    ".ar-pic.on { border-color: var(--ok); cursor: default; }",
    ".ar-pic.fixed { cursor: default; }",
    ".ar-pic .ar-tick { position: absolute; top: 4px; right: 4px; width: 22px; height: 22px; border-radius: 50%; background: var(--ok); color: var(--panel); " +
      "font-size: 14px; font-weight: 700; line-height: 22px; text-align: center; box-shadow: 0 1px 3px rgba(0,0,0,.3); }",
    ".ar-pic .ar-tier { position: absolute; left: 4px; bottom: 4px; padding: 1px 6px; border-radius: 4px; background: var(--panel); color: var(--muted); font-size: 11px; }",
    ".ar-empty { color: var(--muted); }",
    ".ar-msg { margin: 0 0 10px; font-size: 13px; color: var(--bad); min-height: 1em; }"
  ].join("\n");

  var TIERS = [["simple", "Simple"], ["detail", "Detail"]];

  var build = function (pane, ctx) {
    var el = ctx.el, add = ctx.add, put = ctx.put;
    if (!document.getElementById("ar-css")) {
      var st = document.createElement("style");
      st.id = "ar-css";
      st.textContent = CSS;
      document.head.appendChild(st);
    }
    var data = window.CORESIMPLE_ART || { items: [] };
    var state = { feature: "", detail: false, query: "" };
    var canSave = !!ctx.setup;

    add(pane, el("h1", "", "Studio"));
    var hint = put(pane, el("p", "ar-hint"));
    add(hint, el("b", "", "✓"), document.createTextNode(canSave ? " đang dùng · bấm ảnh kia để đổi" : " đang dùng · mở trang qua server Setup để đổi"));
    var bar = put(pane, el("div", "ar-bar"));
    var msg = put(pane, el("p", "ar-msg"));
    var grid = put(pane, el("div", "ar-grid"));

    var src = function (path) { return (canSave ? "/art/" : "../") + path.split("/").map(encodeURIComponent).join("/"); };

    var choose = function (item, tier) {
      var before = item.use;
      item.use = tier;      // đổi ngay trên trang; lỗi thì trả lại
      msg.textContent = "";
      renderGrid();
      fetch("/api/art/choose", {
        method: "POST", cache: "no-store",
        headers: { "Content-Type": "application/json", "X-Setup-Token": ctx.setup.token },
        body: JSON.stringify({ key: item.key, tier: tier })
      }).then(function (r) {
        if (!r.ok) throw new Error("HTTP " + r.status);
        return r.json();
      }).then(function (res) {
        if (!res.ok) throw new Error(res.error);
        if (res.data) { data = res.data; window.CORESIMPLE_ART = data; }
        renderGrid();
      }, function (e) {
        item.use = before;
        msg.textContent = item.key + ": " + e.message;
        renderGrid();
      });
    };

    var picture = function (item, tier, label) {
      var v = item[tier];
      var both = !!(item.simple && item.detail);
      var on = item.use === tier;
      var b = el("button", "ar-pic " + (on ? "on" : "off") + (both && canSave ? "" : " fixed"));
      b.type = "button";
      b.title = label + (on ? " · đang dùng" : "");
      b.setAttribute("aria-pressed", on ? "true" : "false");
      if (v.preview) {
        var img = put(b, el("img"));
        img.loading = "lazy";
        img.alt = item.id + " " + label;
        img.src = src(v.preview);
      } else {
        add(b, el("span", "ar-box", "3D"));
      }
      if (on) add(b, el("span", "ar-tick", "✓"));
      if (both) add(b, el("span", "ar-tier", label));
      if (both && canSave && !on) b.addEventListener("click", function () { choose(item, tier); });
      return b;
    };

    var visible = function () {
      var q = state.query.trim().toLowerCase();
      return data.items.filter(function (it) {
        return (!state.feature || it.feature === state.feature) && (!state.detail || it.detail) &&
          (!q || it.id.indexOf(q) >= 0 || it.key.toLowerCase().indexOf(q) >= 0);
      });
    };

    var chip = function (label, count, pressed, onClick) {
      var b = el("button", "ar-chip", label);
      b.type = "button";
      if (count !== null) add(b, el("span", "", String(count)));
      b.setAttribute("aria-pressed", pressed ? "true" : "false");
      b.addEventListener("click", onClick);
      return b;
    };

    var renderBar = function () {
      bar.textContent = "";
      var counts = {};
      data.items.forEach(function (it) { counts[it.feature] = (counts[it.feature] || 0) + 1; });
      add(bar, chip("Tất cả", data.items.length, !state.feature, function () { state.feature = ""; renderBar(); renderGrid(); }));
      Object.keys(counts).sort().forEach(function (f) {
        add(bar, chip(f, counts[f], state.feature === f, function () { state.feature = state.feature === f ? "" : f; renderBar(); renderGrid(); }));
      });
      add(bar, chip("Có Detail", data.items.filter(function (it) { return it.detail; }).length, state.detail,
        function () { state.detail = !state.detail; renderBar(); renderGrid(); }));
      var find = put(bar, el("input", "ar-find"));
      find.type = "search";
      find.placeholder = "Tìm art";
      find.value = state.query;
      find.addEventListener("input", function () { state.query = find.value; renderGrid(); });
    };

    var renderGrid = function () {
      grid.textContent = "";
      var list = visible();
      if (!data.items.length) { add(grid, el("p", "ar-empty", "Chưa có art Simple hay Detail: game đang chạy bằng GreyBox.")); return; }
      if (!list.length) { add(grid, el("p", "ar-empty", "Không có art nào khớp.")); return; }
      list.forEach(function (it) {
        var card = put(grid, el("div", "ar-card"));
        var name = put(card, el("div", "ar-name", it.id));
        name.title = it.key;
        if (!state.feature) add(name, el("small", "", it.feature));
        var pics = put(card, el("div", "ar-pics" + (it.simple && it.detail ? "" : " one")));
        TIERS.forEach(function (t) { if (it[t[0]]) add(pics, picture(it, t[0], t[1])); });
      });
    };

    renderBar();
    renderGrid();
  };

  window.CORESIMPLE_VIEWS = window.CORESIMPLE_VIEWS || {};
  window.CORESIMPLE_VIEWS.studio = build;
})();
