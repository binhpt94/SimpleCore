// Tab "Đang chạy" của trang quản lý (người duyệt 2026-11-27): mọi agent đang chạy ở một chỗ - Leader, thread tool, worker,
// tester và subagent của chúng; model, lượt chạy bao lâu, đang làm gì (tool gọi gần nhất), token. Chỉ xem, không bấm gì.
// Dữ liệu: GET /api/activity của setup_server.py, tự làm mới. Mở thẳng từ file thì chỉ báo.
// index.html gọi: CORESIMPLE_VIEWS.activity(pane, { el, add, put, section, setup }).
(function () {
  "use strict";

  var CSS = [
    ".ac-sum { display: flex; flex-wrap: wrap; gap: 6px 18px; padding: 10px 14px; border: 1px solid var(--line); border-radius: 10px; background: var(--panel); margin: 0 0 14px; font-size: 14px; }",
    ".ac-sum b { font-size: 18px; }",
    ".ac-wrap { overflow-x: auto; border: 1px solid var(--line); border-radius: 10px; background: var(--panel); margin: 0 0 16px; }",
    ".ac-table { width: 100%; border-collapse: collapse; font-size: 13.5px; }",
    ".ac-table th, .ac-table td { padding: 6px 10px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; white-space: nowrap; }",
    ".ac-table th { font-weight: 600; color: var(--muted); font-size: 12.5px; }",
    ".ac-table tr.ac-main { cursor: pointer; }",
    ".ac-table tr.ac-main:hover { background: var(--bg); }",
    ".ac-table td.ac-doing { white-space: normal; min-width: 220px; max-width: 460px; word-break: break-word; }",
    ".ac-table td.ac-num { text-align: right; font-variant-numeric: tabular-nums; }",
    ".ac-sub td:first-child { padding-left: 26px; color: var(--muted); }",
    ".ac-detail td { background: var(--bg); white-space: normal; }",
    ".ac-detail pre { margin: 4px 0 8px; white-space: pre-wrap; word-break: break-word; font: inherit; font-size: 13px; }",
    ".ac-detail ol { margin: 4px 0 4px 18px; padding: 0; font-size: 13px; }",
    ".ac-st { font-weight: 600; }",
    ".ac-st.run { color: var(--ok); }",
    ".ac-st.wait { color: var(--accent); }",
    ".ac-st.idle { color: var(--muted); font-weight: 400; }",
    ".ac-warn { color: var(--bad); }",
    ".ac-open { color: var(--accent); }",
    ".ac-done { font-size: 13.5px; }",
    ".ac-done li { margin: 0 0 4px; }"
  ].join("\n");

  var REFRESH_MS = 3000;

  var dur = function (s) {
    s = Math.max(0, Math.round(s || 0));
    if (s < 60) return s + "s";
    if (s < 3600) return Math.floor(s / 60) + "m" + (s % 60 < 10 ? "0" : "") + (s % 60) + "s";
    return Math.floor(s / 3600) + "h" + (Math.floor(s / 60) % 60 < 10 ? "0" : "") + (Math.floor(s / 60) % 60) + "m";
  };
  var tok = function (n) {
    if (!n) return "—";
    if (n >= 1e6) return (n / 1e6).toFixed(1).replace(".", ",") + "M";
    if (n >= 1e3) return Math.round(n / 1e3) + "k";
    return String(n);
  };
  var money = function (v) { return "$" + (v || 0).toFixed(2).replace(".", ","); };
  var stClass = function (s) {
    return s === "chạy" ? "run" : (s === "rảnh" ? "idle" : "wait");
  };

  var build = function (pane, ctx) {
    var el = ctx.el, add = ctx.add, put = ctx.put;
    if (!document.getElementById("ac-css")) {
      var st = document.createElement("style");
      st.id = "ac-css";
      st.textContent = CSS;
      document.head.appendChild(st);
    }
    add(pane, el("h1", "", "Đang chạy"));
    if (!ctx.setup) {
      add(pane, el("p", "muted", "Tab này chỉ có khi mở trang qua python Management/tools/setup_server.py."));
      return;
    }
    add(pane, el("p", "sub", "Mọi agent đang chạy: Leader, tool, worker, tester và subagent. Token quy đổi theo giá input, như tab Setup. Bấm một dòng để xem việc và các tool gọi gần nhất."));
    var msgBox = put(pane, el("p", ""));
    var sumBox = put(pane, el("div", "ac-sum"));
    var tableBox = put(pane, el("div", "ac-wrap"));
    var doneBox = put(pane, el("div", ""));
    var open = {};      // id -> mở chi tiết

    var get = function (path) {
      return fetch(path, { headers: { "X-Setup-Token": ctx.setup.token }, cache: "no-store" }).then(function (r) {
        if (!r.ok) throw new Error("HTTP " + r.status);
        return r.json();
      });
    };

    var doingCell = function (r, now) {
      var td = el("td", "ac-doing");
      if (r.hang) { add(td, el("span", "ac-warn", "⚠ " + r.hang)); return td; }
      if (!r.doing) { add(td, el("span", "muted", r.state === "chạy" ? "đang nghĩ" : "—")); return td; }
      add(td, el("span", r.doing_open ? "ac-open" : "", r.doing));
      if (r.state === "chạy" && r.quiet >= 180) add(td, el("span", "ac-warn", " · im " + dur(r.quiet)));
      return td;
    };

    var line = function (r, now, sub) {
      var tr = el("tr", sub ? "ac-sub" : "ac-main");
      var running = r.state === "chạy" && r.start;
      add(tr,
        el("td", "", (sub ? "└ " : "") + r.name),
        el("td", "", r.model ? r.model + (r.effort ? " · " + r.effort : "") : "—"),
        el("td", "ac-num", running ? dur(now - r.start) : "—"),
        doingCell(r, now),
        el("td", "ac-num", tok(r.turn_tokens)),
        el("td", "ac-num", tok(r.tokens) + (r.cost ? " · " + money(r.cost) : "")),
        add(el("td"), el("span", "ac-st " + stClass(r.state), r.state + (r.where && r.where !== "nền" ? " (" + r.where + ")" : ""))));
      return tr;
    };

    var detail = function (r, now) {
      var td = el("td");
      td.colSpan = 7;
      if (r.id) add(td, el("div", "muted", "id: " + r.id));
      if (r.task) { add(td, el("b", "", "Việc")); add(td, el("pre", "", r.task)); }
      if (r.asks && r.asks.length) add(td, el("div", "", "Chờ người trả lời: " + r.asks.join(", ")));
      if (r.job_seconds) add(td, el("div", "muted", "Các lượt đã xong của việc: " + dur(r.job_seconds) + " · " + money(r.cost)));
      if (r.events && r.events.length) {
        add(td, el("b", "", "Server đã làm"));
        var ev = put(td, el("ol"));
        r.events.forEach(function (e) { add(ev, el("li", "", e.text + " · " + dur(now - e.at) + " trước")); });
      }
      if (r.tools && r.tools.length) {
        add(td, el("b", "", "Tool gọi gần nhất"));
        var ol = put(td, el("ol"));
        r.tools.forEach(function (t) {
          add(ol, el("li", t.open ? "ac-open" : "", t.text + " · " + dur(now - t.at) + " trước" + (t.open ? " · đang chạy" : "")));
        });
      } else if (!r.task) {
        add(td, el("div", "muted", "Chưa có tool nào trong lượt này."));
      }
      return add(el("tr", "ac-detail"), td);
    };

    var render = function (v) {
      var now = v.now, T = v.totals;
      sumBox.textContent = "";
      add(sumBox,
        add(el("span"), el("b", "", String(T.running)), document.createTextNode(" đang chạy")),
        el("span", "", "worker " + T.workers + "/" + T.slots + " slot"),
        el("span", "", "worker hôm nay " + money(T.cost_today)),
        el("span", "", "thread đang mở " + tok(T.tokens_live) + " token"));

      tableBox.textContent = "";
      if (!v.rows.length) {
        add(tableBox, el("p", "muted", "  Không có agent nào đang chạy."));
      } else {
        var table = put(tableBox, el("table", "ac-table"));
        add(table, add(el("thead"), add.apply(null, [el("tr")].concat(
          ["Ai", "Model", "Chạy", "Đang làm gì", "Token lượt", "Token tổng", "Trạng thái"].map(function (n) { return el("th", "", n); })))));
        var body = put(table, el("tbody"));
        v.rows.forEach(function (r) {
          var key = r.id || r.name;
          var tr = put(body, line(r, now, false));
          tr.addEventListener("click", function () { open[key] = !open[key]; render(v); });
          if (open[key]) add(body, detail(r, now));
          r.subs.forEach(function (s) { add(body, line(s, now, true)); });
        });
      }

      doneBox.textContent = "";
      if (v.done.length) {
        add(doneBox, el("b", "", "Xong hôm nay"));
        var ul = put(doneBox, el("ul", "ac-done"));
        v.done.forEach(function (d) {
          var li = put(ul, el("li"));
          var lastEv = d.events && d.events.length ? d.events[d.events.length - 1] : null;
          add(li, el("b", "", d.name), el("span", "", " · " + d.state + " · " + dur(d.seconds) + " · " + money(d.cost) + " · " + d.model + (d.effort ? " · " + d.effort : "")));
          if (lastEv) add(li, el("div", lastEv.key.indexOf("editor-test-ok") === 0 ? "ok-text" : "muted", "→ " + lastEv.text));
          if (d.summary) add(li, el("div", "muted", d.summary));
        });
      }
    };

    var last = null;
    var refresh = function () {
      return get("/api/activity").then(function (v) { last = v; msgBox.textContent = ""; render(v); },
        function (e) { msgBox.className = "bad-text"; msgBox.textContent = "Không tải được: " + e.message; });
    };
    refresh();
    var timer = setInterval(function () {
      if (!pane.isConnected) { clearInterval(timer); return; }
      if (!document.hidden && pane.offsetParent !== null) refresh();
    }, REFRESH_MS);
  };

  window.CORESIMPLE_VIEWS = window.CORESIMPLE_VIEWS || {};
  window.CORESIMPLE_VIEWS.activity = build;
})();
