// Tab "Game design" của trang quản lý: bản đồ node từ ý tưởng sơ khai tới yêu cầu kĩ thuật.
// Code thuần, không AI, không thư viện. Dữ liệu: window.CORESIMPLE_GAMEDESIGN (data/gamedesign.js, do
// tools/sync_page.py sinh từ Management/game-design; cấu trúc cây và luật đạt/chốt do tools/gamedesign.py quy định,
// trang chỉ hiển thị kết quả đã tính sẵn: passed, reasons, stale, approved, state...).
// Số cột = số vòng trong data.rounds: thêm vòng ở gamedesign.py thì bản đồ tự thêm cột.
// Mọi thao tác nằm ngay trên node: nút nhỏ trên thẻ, và khu dưới bản đồ của node đang chọn (không có panel bên).
// index.html gọi: CORESIMPLE_VIEWS.gamedesign(pane, { el, add, put, section, setup }).
(function () {
  "use strict";

  // ---------------------------------------------------------------- cấu hình
  var OVERVIEW_LAST_ROUND = 3;   // bản đồ chung hiện tới vòng này
  var FOCUS_ROUND = 2;           // bấm node ở vòng này thì chỉ còn nhánh của nó, hiện tới vòng cuối
  var CARD_W = 240, CARD_H = 112, GAP_X = 64, GAP_Y = 14, HEAD_H = 40;
  // `dir`: thư mục của bảng, tính từ Management/ (để nạp ảnh và ghi đường dẫn).
  var TABLES = [
    { key: "live", label: "Đã chốt (live)", dir: "game-design/live/", empty: "Chưa có gì được chốt. Chốt node từ bảng Đề xuất (idea)." },
    { key: "idea", label: "Đề xuất (idea)", dir: "game-design/ai/idea/", empty: "Bảng đề xuất còn trống. Phiên @GameDesigner viết vào Management/game-design/ai/idea." }
  ];
  var MODES = [
    { key: "auto", label: "Full Auto", hint: "AI tự quyết. Node đạt khi mọi ý có điểm từ mức đạt trở lên (điểm của bạn nếu đã chấm, chưa thì điểm AI); bạn bấm Sync hoặc Chốt để chép node đạt sang live." },
    { key: "user", label: "User control", hint: "AI hỏi, bạn quyết. Node đạt khi bạn chấm mọi ý từ mức đạt trở lên và trả lời hết câu hỏi; bạn bấm Sync hoặc Chốt; vòng sau bị khoá tới khi vòng trước được chốt." }
  ];
  var STATES = { "new": "chưa có trong live", "changed": "khác live", "same": "giống live" };
  var DATA_FILE = "data/gamedesign.js";
  var SERVER_HINT = "Mở trang qua python Management/tools/setup_server.py để lưu.";
  // Ô nhập cao theo nội dung, để người gõ thấy hết phần mình gõ.
  var grow = function (area) {
    if (!area.scrollHeight) return;
    area.style.height = "auto";
    area.style.height = area.scrollHeight + "px";
  };

  var CSS = [
    ".gd-bar { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin-bottom: 16px; }",
    ".gd-bar .gd-time { color: var(--muted); font-size: 13px; margin-left: auto; }",
    ".gd-btn { padding: 7px 14px; border: 1px solid var(--line); border-radius: 6px; background: var(--panel); color: var(--text); font: inherit; font-size: 14px; cursor: pointer; }",
    ".gd-btn:hover { border-color: var(--accent); }",
    ".gd-btn:focus-visible, .gd-card:focus-visible, .gd-chip:focus-visible, .gd-score button:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }",
    ".gd-btn[aria-pressed='true'] { background: var(--accent); border-color: var(--accent); color: var(--panel); font-weight: 600; }",
    ".gd-btn:disabled { opacity: .5; cursor: default; }",
    ".gd-btn.main { border-color: var(--accent); color: var(--accent); font-weight: 600; }",
    ".gd-sep { width: 1px; align-self: stretch; background: var(--line); margin: 0 4px; }",
    ".gd-inline { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin: 0 0 16px; }",
    ".gd-inline input { padding: 7px 10px; border: 1px solid var(--line); border-radius: 6px; background: var(--bg); color: var(--text); font: inherit; }",
    ".gd-problems { margin: 0 0 16px; padding-left: 20px; }",
    ".gd-scroll { overflow-x: auto; background: var(--panel); border: 1px solid var(--line); border-radius: 8px; padding: 16px; }",
    ".gd-map { position: relative; }",
    ".gd-map svg { position: absolute; left: 0; top: 0; pointer-events: none; }",
    ".gd-map path { fill: none; stroke: var(--line); stroke-width: 2; }",
    ".gd-map path.on { stroke: var(--accent); }",
    ".gd-col { position: absolute; top: 0; font-size: 12px; letter-spacing: .04em; text-transform: uppercase; color: var(--muted); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }",
    ".gd-card { position: absolute; display: block; text-align: left; padding: 9px 12px; border: 1px solid var(--line); border-left: 4px solid var(--line); border-radius: 8px; background: var(--bg); color: var(--text); font: inherit; cursor: pointer; overflow: hidden; }",
    ".gd-card:hover { border-color: var(--accent); }",
    ".gd-card.sel { border-color: var(--accent); box-shadow: 0 0 0 2px var(--accent); }",
    ".gd-card.redo { border-left-color: var(--bad); }",
    ".gd-card.wait { border-left-color: var(--warn-text); }",
    ".gd-card.done { border-left-color: var(--ok); }",
    ".gd-card strong { display: block; font-size: 14px; line-height: 1.3; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; padding-right: 30px; }",
    ".gd-card span.sum { display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; font-size: 12.5px; line-height: 1.35; color: var(--muted); margin-top: 3px; }",
    ".gd-card span.low { position: absolute; right: 8px; top: 7px; }",
    ".gd-chips { position: absolute; display: flex; gap: 4px; overflow: hidden; }",
    ".gd-chip, .gd-tag { flex: none; font: inherit; font-size: 12px; line-height: 1; padding: 4px 8px; border-radius: 10px; white-space: nowrap; border: 1px solid var(--line); background: var(--panel); color: var(--muted); }",
    ".gd-chip { cursor: pointer; color: var(--text); text-decoration: underline; }",
    ".gd-chip:hover { border-color: var(--accent); }",
    ".gd-chip.ask { border-color: var(--warn-text); background: var(--warn-bg); color: var(--warn-text); font-weight: 600; }",
    ".gd-chip.go { border-color: var(--accent); background: var(--accent); color: var(--panel); font-weight: 600; text-decoration: none; }",
    ".gd-tag.bad { border-color: var(--bad); color: var(--bad); font-weight: 600; }",
    ".gd-tag.ok { border-color: var(--ok); color: var(--ok); }",
    ".gd-num { display: inline-block; min-width: 24px; padding: 2px 6px; border-radius: 10px; font-size: 12px; font-weight: 600; line-height: 1.3; text-align: center; border: 1px solid var(--line); color: var(--muted); }",
    ".gd-num.good { border-color: var(--ok); background: var(--ok); color: var(--panel); }",
    ".gd-num.fine { border-color: var(--ok); color: var(--ok); }",
    ".gd-num.bad { border-color: var(--warn-text); background: var(--warn-bg); color: var(--warn-text); }",
    ".gd-num.off { border-color: var(--bad); background: var(--bad); color: var(--panel); }",
    ".gd-link { padding: 0; border: 0; background: none; color: var(--accent); font: inherit; font-size: 13px; text-decoration: underline; cursor: pointer; }",
    ".gd-link:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }",
    ".gd-said { font-size: 13px; color: var(--muted); margin-left: 8px; white-space: pre-wrap; }",
    ".gd-detail { margin-top: 16px; }",
    ".gd-detail h3 { margin: 24px 0 4px; font-size: 13px; letter-spacing: .05em; text-transform: uppercase; color: var(--muted); }",
    ".gd-detail h4 { margin: 14px 0 4px; font-size: 14px; }",
    ".gd-detail p, .gd-detail ul { margin: 0 0 10px; }",
    ".gd-detail pre { margin: 0 0 10px; padding: 10px 12px; background: var(--bg); border: 1px solid var(--line); border-radius: 6px; font: 13px/1.5 Consolas, monospace; white-space: pre-wrap; overflow-x: auto; }",
    ".gd-detail .gd-path { color: var(--muted); font-size: 13px; overflow-wrap: anywhere; }",
    ".gd-verdict { margin: 12px 0 0; padding: 6px 10px; border-radius: 6px; border-left: 3px solid var(--line); font-size: 12.5px; color: var(--muted); }",
    ".gd-verdict.ok { border-left-color: var(--ok); }",
    ".gd-verdict.bad { border-left-color: var(--bad); }",
    ".gd-verdict strong { margin-right: 8px; color: var(--text); font-weight: 600; }",
    ".gd-item { border-bottom: 1px solid var(--line); }",
    ".gd-head { display: flex; gap: 10px; align-items: baseline; padding: 7px 0; cursor: pointer; border-radius: 6px; }",
    ".gd-head:hover .text { color: var(--accent); }",
    ".gd-head:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }",
    ".gd-item .text { flex: 1; min-width: 0; margin: 0; font-size: 15px; line-height: 1.5; color: var(--text); }",
    ".gd-marks { flex: none; display: flex; gap: 6px; align-items: center; max-width: 55%; }",
    ".gd-more { white-space: pre-wrap; margin: 0 0 10px; max-width: 72ch; font-size: 14.5px; line-height: 1.6; color: var(--muted); }",
    ".gd-id { margin: 0 0 8px; font-size: 11.5px; color: var(--muted); }",
    ".gd-id code { font-size: 11.5px; }",
    ".gd-mark { padding: 1px 8px; border: 1px solid var(--line); border-radius: 4px; font-size: 11.5px; line-height: 1.5; color: var(--muted); white-space: nowrap; }",
    ".gd-mark.good { border-color: var(--ok); background: var(--ok); color: var(--panel); }",
    ".gd-mark.fine { border-color: var(--ok); color: var(--ok); }",
    ".gd-mark.bad { border-color: var(--warn-text); background: var(--warn-bg); color: var(--warn-text); }",
    ".gd-mark.off { border-color: var(--bad); background: var(--bad); color: var(--panel); }",
    ".gd-mark.none { border-style: dashed; }",
    ".gd-marks .gd-said { order: -1; font-size: 12.5px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; min-width: 0; }",
    ".gd-edit { padding: 2px 0 14px; }",
    ".gd-score { display: flex; flex-wrap: wrap; gap: 3px; align-items: center; }",
    ".gd-score button { width: 26px; height: 24px; padding: 0; border: 1px solid var(--line); border-radius: 4px; background: transparent; color: var(--muted); font: inherit; font-size: 12px; line-height: 1; cursor: pointer; }",
    ".gd-score button:hover { border-color: var(--accent); color: var(--text); }",
    ".gd-score button[aria-pressed='true'] { background: var(--accent); border-color: var(--accent); color: var(--panel); font-weight: 700; }",
    ".gd-score button:disabled { opacity: .5; cursor: default; }",
    ".gd-q { padding: 12px 0; border-bottom: 1px solid var(--line); }",
    ".gd-save { position: sticky; bottom: 0; margin: 0; padding: 8px 0; background: var(--panel); border-top: 1px solid var(--line); }",
    ".gd-save .gd-btn { padding: 4px 10px; font-size: 12.5px; }",
    ".gd-detail textarea { display: block; width: 100%; min-height: 40px; margin-top: 6px; padding: 8px 10px; border: 1px solid var(--line); border-radius: 6px; background: var(--bg); color: var(--text); font: inherit; resize: none; overflow: hidden; }",
    ".gd-note { font-size: 12.5px; color: var(--muted); margin: 6px 0 0; }",
    ".gd-row { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin-top: 12px; }",
    ".gd-row span { flex: 1; min-width: 120px; font-size: 13px; color: var(--muted); }",
    ".gd-q h4 { margin: 0 0 4px; font-size: 15px; }",
    ".gd-q .why { margin: 0 0 10px; font-size: 13px; color: var(--muted); }",
    ".gd-q .kind { font-size: 12px; color: var(--muted); margin: 0 0 6px; }",
    ".gd-q label { display: flex; gap: 10px; align-items: flex-start; padding: 8px 10px; margin-bottom: 6px; border: 1px solid var(--line); border-radius: 6px; cursor: pointer; }",
    ".gd-q label:hover { border-color: var(--accent); }",
    ".gd-q label input { margin-top: 4px; flex: none; }",
    ".gd-q label small { display: block; color: var(--muted); font-size: 12.5px; }",
    ".gd-q.done h4::after { content: ' ✓'; color: var(--ok); }",
    ".gd-images { display: flex; flex-wrap: wrap; gap: 12px; }",
    ".gd-images figure { margin: 0; width: 220px; }",
    ".gd-images img { display: block; width: 100%; max-height: 180px; object-fit: contain; background: var(--bg); border: 1px solid var(--line); border-radius: 6px; }",
    ".gd-images figcaption { font-size: 12.5px; color: var(--muted); margin-top: 4px; }",
    ".gd-history { margin: 0; padding: 0; list-style: none; font-size: 13.5px; }",
    ".gd-history li { padding: 8px 0; border-bottom: 1px solid var(--line); }",
    ".gd-history time { display: block; font-size: 12px; color: var(--muted); }",
    ".gd-history .out { color: var(--bad); }",
    ".gd-scale { display: flex; flex-wrap: wrap; gap: 6px 14px; font-size: 12.5px; color: var(--muted); margin: 0 0 4px; }"
  ].join("\n");

  var styled = false;
  var addStyle = function () {
    if (styled) return;
    styled = true;
    var s = document.createElement("style");
    s.textContent = CSS;
    document.head.appendChild(s);
  };
  var SVG_NS = "http://www.w3.org/2000/svg";

  // ---------------------------------------------------------------- cây
  var find = function (node, path) {
    if (!node) return null;
    if (node.path === path) return node;
    for (var i = 0; i < node.children.length; i++) {
      var hit = find(node.children[i], path);
      if (hit) return hit;
    }
    return null;
  };
  var each = function (node, fn) { if (node) { fn(node); node.children.forEach(function (c) { each(c, fn); }); } };

  // Con được hiện của một node, theo chế độ: chung (tới OVERVIEW_LAST_ROUND) hay một nhánh (tới vòng cuối).
  var shownChildren = function (node, focus, lastRound) {
    return node.children.filter(function (c) {
      if (c.round > lastRound) return false;
      return !(focus !== null && c.round === FOCUS_ROUND && c.path !== focus);
    });
  };

  // Xếp chỗ: mỗi vòng một cột; lá chiếm một hàng, cha nằm giữa các con.
  var layout = function (root, focus, lastRound) {
    var placed = [], rows = 0;
    var place = function (node, parent) {
      var kids = shownChildren(node, focus, lastRound);
      var item = { node: node, parent: parent, hidden: node.children.length - kids.length, row: 0 };
      placed.push(item);
      if (!kids.length) { item.row = rows++; return item; }
      var rowsOfKids = kids.map(function (k) { return place(k, item).row; });
      item.row = (rowsOfKids[0] + rowsOfKids[rowsOfKids.length - 1]) / 2;
      return item;
    };
    place(root, null);
    return { items: placed, rows: rows };
  };

  // ---------------------------------------------------------------- markdown tối thiểu (chỉ để đọc)
  var renderText = function (ctx, box, text) {
    var lines = (text || "").split("\n"), para = [], list = null, code = null;
    var flush = function () {
      if (para.length) ctx.add(box, ctx.el("p", "", para.join(" ")));
      para = [];
      list = null;
    };
    lines.forEach(function (line) {
      if (/^\s*```/.test(line)) {
        if (code === null) { flush(); code = []; } else { ctx.add(box, ctx.el("pre", "", code.join("\n") || " ")); code = null; }
        return;
      }
      if (code !== null) { code.push(line); return; }
      var head = /^(#{1,6})\s+(.*)$/.exec(line), bullet = /^\s*[-*]\s+(.*)$/.exec(line);
      if (head) { flush(); ctx.add(box, ctx.el("h4", "", head[2])); return; }
      if (bullet) {
        if (para.length) flush();
        if (!list) list = ctx.put(box, ctx.el("ul"));
        ctx.add(list, ctx.el("li", "", bullet[1]));
        return;
      }
      if (/^\s*\|/.test(line)) { flush(); ctx.add(box, ctx.el("pre", "", line)); return; }
      if (!line.trim()) { flush(); return; }
      if (list) list = null;
      para.push(line.trim());
    });
    if (code !== null) ctx.add(box, ctx.el("pre", "", code.join("\n")));
    flush();
  };

  // ---------------------------------------------------------------- tab
  var build = function (pane, ctx) {
    addStyle();
    var el = ctx.el, add = ctx.add, put = ctx.put;
    var data = window.CORESIMPLE_GAMEDESIGN || null;
    // selected: path của node đang chọn; mọi thứ của node đó (duyệt ý, câu hỏi, chốt, diễn giải) hiện dưới bản đồ.
    // modeAsk: mode đang chờ nhập PIN để đổi.
    var state = { table: "live", focus: null, selected: null, message: null, modeAsk: null };

    add(pane, el("h1", "", "Game design"));
    add(pane, el("p", "sub", "Bản đồ thiết kế, từ ý tưởng sơ khai tới yêu cầu kĩ thuật. Bấm một node, phần dưới bản đồ hiện các ý cốt lõi của nó: bấm vào từng ý để xem chi tiết, chấm điểm và ghi ý kiến; bấm một feature (vòng " +
      FOCUS_ROUND + ") thì bản đồ chỉ còn nhánh của nó tới vòng cuối."));
    var bar = put(pane, el("div", "gd-bar"));
    var noticeBox = put(pane, el("div"));
    var mapBox = put(pane, el("div"));
    var detailBox = put(pane, el("div"));      // dưới bản đồ: mọi thứ của node đang chọn

    var api = function (method, path, body) {
      return fetch(path, {
        method: method,
        headers: { "Content-Type": "application/json", "X-Setup-Token": ctx.setup.token },
        body: body ? JSON.stringify(body) : undefined,
        cache: "no-store"
      }).then(function (r) {
        if (!r.ok) throw new Error("HTTP " + r.status);
        return r.json();
      });
    };
    // Gọi server rồi nhận dữ liệu mới; `done(res)` trả về câu báo thành công.
    var send = function (path, body, done, failed) {
      api("POST", path, body).then(function (res) {
        if (!res.ok) { (failed || say)(res.error); return; }
        data = res.data;
        state.message = { bad: false, text: done(res) };
        render();
      }, function (e) { (failed || say)("Lỗi gọi server: " + e.message); });
    };
    var say = function (text) { state.message = { bad: true, text: text }; renderNotice(); };

    var button = function (label, onClick, pressed, className) {
      var b = el("button", className || "gd-btn", label);
      b.type = "button";
      if (pressed !== undefined && pressed !== null) b.setAttribute("aria-pressed", pressed ? "true" : "false");
      b.addEventListener("click", onClick);
      return b;
    };

    var tree = function () { return data ? data[state.table] : null; };
    var mode = function () { return data && data.mode === "user" ? "user" : "auto"; };
    var table = function () { return TABLES.filter(function (t) { return t.key === state.table; })[0]; };
    var passMin = function () { return data && typeof data.passMin === "number" ? data.passMin : 5; };
    var band = function (score) {
      var hit = (data && data.bands || []).filter(function (b) { return score >= b.min && score <= b.max; })[0];
      return hit || { key: "", label: "" };
    };
    var num = function (score, title) {
      var b = score === null || score === undefined ? null : band(score);
      var n = el("span", "gd-num" + (b ? " " + b.key : ""), b ? String(score) : "–");
      n.title = (title ? title + ": " : "") + (b ? score + " · " + b.label : "chưa chấm");
      return n;
    };
    var unscored = function (node) { return node.items.filter(function (i) { return i.user === null; }).length; };
    var lowest = function (node) {
      return node.items.length ? Math.min.apply(null, node.items.map(function (i) { return i.effective; })) : null;
    };
    var failingBelow = function (node) { var n = 0; each(node, function (x) { if (!x.passed) n++; }); return n; };
    // Ý bạn đã chấm dưới mức đạt (dùng cho thẻ ở bảng live).
    var turnedDown = function (node) { return node.items.filter(function (i) { return i.user !== null && i.user < passMin(); }).length; };
    // Node đề xuất đang đạt mà khác live: thứ nút Sync sẽ chép, đi từ gốc xuống và dừng ở node chưa đạt.
    var syncable = function () {
      var n = 0;
      var visit = function (node) {
        if (!node.passed) return;
        if (node.state !== "same") n++;
        node.children.forEach(visit);
      };
      if (data && data.idea) visit(data.idea);
      return n;
    };
    // Ô nhập ẩn sau một chữ gạch dưới; bấm mới hiện. Trả về textarea (đã gắn tự giãn).
    var hiddenInput = function (box, label, value, placeholder, onInput, line) {
      line = line || put(box, el("div"));
      var link = put(line, button(label, function () { toggle(area.hidden); }, null, "gd-link"));
      var said = put(line, el("span", "gd-said", value || ""));
      var area = put(box, el("textarea"));
      area.value = value || "";
      area.placeholder = placeholder;
      area.hidden = true;
      area.setAttribute("aria-label", placeholder);
      var toggle = function (show) {
        area.hidden = !show;
        said.hidden = show;
        link.setAttribute("aria-expanded", show ? "true" : "false");
        if (show) { grow(area); area.focus(); } else { said.textContent = (area.value || "").trim(); }
      };
      link.setAttribute("aria-expanded", "false");
      area.addEventListener("input", function () { grow(area); onInput(area.value || ""); });
      return area;
    };
    var where = function (node) { return node.path || "(gốc)"; };

    var promote = function (node, deep) {
      state.message = null;
      send("/api/gamedesign/promote", { path: node.path, deep: deep },
        function (res) { return "Đã chốt sang live: " + res.written.join(", "); });
    };

    var reload = function () {
      state.message = null;
      var done = function (fresh) { data = fresh || null; render(); };
      var failed = function (e) { state.message = { bad: true, text: "Không tải lại được: " + e.message }; render(); };
      if (ctx.setup) { api("GET", "/api/gamedesign").then(done, failed); return; }
      // Mở thẳng từ file: nạp lại file dữ liệu (Stop hook sinh lại nó sau mỗi lượt chat).
      var tag = document.createElement("script");
      tag.src = DATA_FILE + "?t=" + Date.now();
      tag.onload = function () { tag.remove(); done(window.CORESIMPLE_GAMEDESIGN); };
      tag.onerror = function () { tag.remove(); failed(new Error("không đọc được " + DATA_FILE)); };
      document.head.appendChild(tag);
    };

    // ---------------------------------------------------------------- thanh trên và thông báo
    var renderBar = function () {
      bar.textContent = "";
      TABLES.forEach(function (t) {
        add(bar, button(t.label, function () { state.table = t.key; state.message = null; render(); }, state.table === t.key));
      });
      if (state.focus !== null) add(bar, button("← Tất cả feature", function () { state.focus = null; render(); }));
      add(bar, button("Reload", reload));
      if (data) {
        var ready = syncable();
        var sync = button(ready ? "Sync (" + ready + ")" : "Sync", function () {
          state.message = null;
          send("/api/gamedesign/sync", {}, function (res) {
            return res.written.length ? "Đã chép sang live: " + res.written.join(", ") : "Không có node nào để chép sang live.";
          });
        }, null, ready ? "gd-btn main" : "gd-btn");
        sync.title = "Chép sang live mọi node đề xuất đang đạt mà khác live, từ gốc xuống, dừng ở node chưa đạt. Không có gì tự sang live." +
          (ctx.setup ? "" : " Cần mở trang qua setup_server.py.");
        sync.disabled = !ready || !ctx.setup;
        add(bar, sync);
        add(bar, el("span", "gd-sep"));
        MODES.forEach(function (m) {
          var b = button(m.label, function () {
            if (m.key === mode()) return;
            state.modeAsk = m.key; state.message = null; render();
          }, mode() === m.key);
          b.title = m.hint + (ctx.setup ? "" : " Muốn đổi mode: mở trang qua setup_server.py.");
          if (!ctx.setup && mode() !== m.key) b.disabled = true;
          add(bar, b);
        });
      }
      add(bar, el("span", "gd-time", data && data.generatedAt ? "Đọc lúc " + data.generatedAt.replace("T", " ") : ""));
    };

    var renderNotice = function () {
      noticeBox.textContent = "";
      if (state.modeAsk && ctx.setup) {
        var target = MODES.filter(function (m) { return m.key === state.modeAsk; })[0];
        var ask = put(noticeBox, el("div", "gd-inline"));
        add(ask, el("span", "", "Đổi sang " + target.label + ": " + target.hint));
        var modePin = put(ask, el("input"));
        modePin.type = "password";
        modePin.autocomplete = "new-password";
        modePin.placeholder = "PIN (không lưu)";
        modePin.setAttribute("aria-label", "PIN để đổi mode");
        add(ask, button("Đổi mode", function () {
          if (!modePin.value) { modePin.focus(); return; }
          send("/api/gamedesign/mode", { pin: modePin.value, mode: target.key }, function () {
            state.modeAsk = null;
            return "Đã đổi sang " + target.label + ".";
          }, function (text) { say("Không đổi mode: " + text); });
        }), button("Thôi", function () { state.modeAsk = null; render(); }));
      }
      if (state.message) add(noticeBox, el("p", state.message.bad ? "bad-text" : "ok-text", state.message.text));
      if (state.table === "live" && data && data.idea) {
        var waiting = 0;
        each(data.idea, function (n) { if (n.pending || (mode() === "user" && unscored(n)) || (n.passed && n.state !== "same")) waiting++; });
        if (waiting) {
          add(put(noticeBox, el("div", "gd-inline")), button(waiting + " node ở bảng Đề xuất đang chờ bạn", function () {
            state.table = "idea"; state.message = null; render();
          }));
        }
      }
      var list = data && data.problems ? data.problems[state.table] || [] : [];
      if (!list.length) return;
      add(noticeBox, el("p", "bad-text", list.length + " lỗi cấu trúc trong bảng " + state.table + ":"));
      var ul = put(noticeBox, el("ul", "gd-problems"));
      list.forEach(function (p) { add(ul, el("li", "", p.path + ": " + p.text)); });
    };

    // ---------------------------------------------------------------- bản đồ
    var select = function (node) {
      state.selected = node.path;      // bấm thẻ là chọn node; nội dung của nó hiện dưới bản đồ
      if (node.round === FOCUS_ROUND) state.focus = node.path;
      state.message = null;
      render();
    };

    // Các nút nhỏ ở đáy thẻ (bảng đề xuất): việc cần bạn làm tại node đó, và trạng thái của node.
    var chipsFor = function (n) {
      var out = [];
      var open = function () { state.selected = n.path; state.message = null; render(); };
      if (state.table === "live") {      // bản đã chốt: vẫn chấm được; ý bị chấm thấp thì cảnh báo
        var down = turnedDown(n);
        if (down) {
          var warn = el("span", "gd-tag bad", down + " ý dưới " + passMin());
          warn.title = "Bạn đã chấm " + down + " ý của bản đã chốt dưới mức đạt; node đề xuất tương ứng cần làm lại.";
          out.push(warn);
        }
        return out;
      }
      if (n.pending) {
        var ask = button(n.pending + " câu hỏi", open, null, "gd-chip ask");
        ask.title = "Mở node để trả lời " + n.pending + " câu hỏi của @GameDesigner";
        out.push(ask);
      }
      if (!n.passed) {
        // redo: việc của @GameDesigner; còn lại là node đang chờ bạn chấm hoặc trả lời.
        var redo = el("span", n.redo ? "gd-tag bad" : "gd-tag", n.redo ? "cần làm lại" : "chờ bạn");
        redo.title = n.reasons.join("\n");
        out.push(redo);
      } else if (n.state === "same") {
        out.push(el("span", "gd-tag ok", "đã chốt"));
      } else if (ctx.setup) {
        var go = button("Chốt", function () { promote(n, false); }, null, "gd-chip go");
        go.title = "Chốt node này sang live (không cần PIN)";
        out.push(go);
      } else {
        out.push(el("span", "gd-tag", "chờ chốt"));
      }
      return out;
    };

    var renderMap = function (root) {
      mapBox.textContent = "";
      var rounds = data.rounds || [];
      var lastRound = state.focus !== null ? rounds.length : Math.min(OVERVIEW_LAST_ROUND, rounds.length);
      var plan = layout(root, state.focus, lastRound);
      var colX = function (round) { return (round - 1) * (CARD_W + GAP_X); };
      var rowY = function (row) { return HEAD_H + row * (CARD_H + GAP_Y); };
      var width = colX(lastRound) + CARD_W, height = rowY(plan.rows) - GAP_Y;
      var idea = state.table === "idea";

      var map = put(put(mapBox, el("div", "gd-scroll")), el("div", "gd-map"));
      map.style.width = width + "px";
      map.style.height = height + "px";
      for (var r = 1; r <= lastRound; r++) {
        var col = put(map, el("div", "gd-col", "Vòng " + r + " · " + (rounds[r - 1] || "")));
        col.style.left = colX(r) + "px";
        col.style.width = CARD_W + "px";
      }
      var svg = document.createElementNS(SVG_NS, "svg");
      svg.setAttribute("width", width);
      svg.setAttribute("height", height);
      svg.setAttribute("aria-hidden", "true");
      map.appendChild(svg);

      plan.items.forEach(function (item) {
        var n = item.node, x = colX(n.round), y = rowY(item.row);
        if (item.parent) {
          var x0 = colX(item.parent.node.round) + CARD_W, y0 = rowY(item.parent.row) + CARD_H / 2;
          var y1 = y + CARD_H / 2, mid = (x0 + x) / 2;
          var line = document.createElementNS(SVG_NS, "path");
          line.setAttribute("d", "M" + x0 + " " + y0 + " C" + mid + " " + y0 + " " + mid + " " + y1 + " " + x + " " + y1);
          if (n.path === state.selected) line.setAttribute("class", "on");
          svg.appendChild(line);
        }
        var kind = !idea ? "" : n.redo ? " redo" : n.passed && n.state === "same" ? " done" : " wait";
        var card = el("button", "gd-card" + (n.path === state.selected ? " sel" : "") + kind);
        card.type = "button";
        card.style.left = x + "px";
        card.style.top = y + "px";
        card.style.width = CARD_W + "px";
        card.style.height = CARD_H + "px";
        card.title = n.title + (n.summary ? "\n" + n.summary : "");
        add(card, el("strong", "", n.title || "(chưa đặt tên)"), el("span", "sum", n.summary || "Chưa có tóm tắt."));
        if (lowest(n) !== null) {
          var low = num(lowest(n), "Điểm thấp nhất của node");
          low.className += " low";
          add(card, low);
        }
        card.addEventListener("click", function () { select(n); });
        map.appendChild(card);
        var chips = chipsFor(n);
        if (item.hidden > 0) chips.push(el("span", "gd-tag", "+" + item.hidden + (n.round === FOCUS_ROUND - 1 ? " feature ẩn" : " node con")));
        if (!chips.length) return;
        var row = el("div", "gd-chips");
        row.style.left = (x + 10) + "px";
        row.style.top = (y + CARD_H - 30) + "px";
        row.style.width = (CARD_W - 20) + "px";
        chips.forEach(function (c) { add(row, c); });
        map.appendChild(row);
      });
    };

    // ---------------------------------------------------------------- các phần của node đang chọn (vẽ dưới bản đồ)
    var renderVerdict = function (body, node) {
      var box = put(body, el("div", "gd-verdict " + (node.passed ? "ok" : "bad")));
      add(box, el("strong", "", node.passed ? "Đạt" : node.redo ? "Chưa đạt, @GameDesigner cần làm lại" : "Chưa đạt, đang chờ bạn"));
      add(box, el("span", "", node.passed
        ? (node.state === "same" ? "Đã chốt, giống live." : "Chưa ở live.")
        : node.reasons.join(" · ")));
      box.title = node.passed ? "Chép sang live bằng nút Chốt bên dưới hoặc Sync ở thanh trên."
        : "Các node con đang bị khoá với @GameDesigner cho tới khi node này đạt" + (mode() === "user" ? " và được chốt." : ".");
    };

    var renderItems = function (body, node) {
      add(body, el("h3", "", "Ý cốt lõi"));
      if (!node.items.length) { add(body, el("p", "muted", "Node chưa có ý nào trong data.md.")); return; }
      var drafts = [];
      var saveBtn = null, saveNote = null;
      var refresh = function () {
        var changed = drafts.filter(function (d) { return d.changed(); }).length;
        saveBtn.disabled = !changed || !ctx.setup;
        saveNote.textContent = !ctx.setup ? SERVER_HINT : changed ? changed + " ý có thay đổi chưa lưu." : "Chấm điểm hoặc ghi phản hồi rồi bấm Lưu.";
      };
      node.items.forEach(function (it) {
        // Chữ của ý là chính. Dưới nó chỉ có hai khung nhỏ (điểm AI, điểm của bạn). Bấm vào ý mới mở phần cập nhật:
        // hàng nút chấm điểm và ô nhập phản hồi.
        var block = put(body, el("div", "gd-item"));
        var draft = { score: it.user, feedback: it.feedback || "" };
        var head = put(block, el("div", "gd-head"));
        head.tabIndex = 0;
        head.title = "Bấm để chấm điểm và ghi phản hồi cho ý này";
        head.setAttribute("role", "button");
        head.setAttribute("aria-expanded", "false");
        // Một hàng: tiêu đề bên trái, rồi ý kiến đã ghi (rút gọn) và hai khung điểm bên phải.
        add(head, el("p", "text", it.title));
        var marks = put(head, el("div", "gd-marks"));
        var aiMark = put(marks, el("span", "gd-mark " + band(it.ai).key, "AI: " + it.ai));
        aiMark.title = "AI chấm " + it.ai + " · " + band(it.ai).label;
        var userMark = put(marks, el("span", "gd-mark", ""));
        var said = put(marks, el("span", "gd-said", ""));
        var showMarks = function () {
          var scored = draft.score !== null && draft.score !== undefined;
          userMark.className = "gd-mark" + (scored ? " " + band(draft.score).key : " none");
          userMark.textContent = "User: " + (scored ? draft.score : "Chưa");
          userMark.title = scored ? "Bạn chấm " + draft.score + " · " + band(draft.score).label : "Bạn chưa chấm ý này";
          said.textContent = editor.hidden ? draft.feedback.trim() : "";
        };
        var editor = put(block, el("div", "gd-edit"));
        editor.hidden = true;
        if (it.detail) add(editor, el("p", "gd-more", it.detail));      // chi tiết của ý chỉ hiện khi đã bấm mở
        var code = put(editor, el("p", "gd-id", "Mã của ý: "));      // mã chỉ hiện khi đã bấm mở
        add(code, el("code", "", it.id));
        var row = put(editor, el("div", "gd-score"));
        var buttons = [];
        for (var s = 0; s <= 10; s++) (function (value) {
          var b = button(String(value), function () {
            draft.score = value;
            buttons.forEach(function (x, i) { x.setAttribute("aria-pressed", i === value ? "true" : "false"); });
            showMarks();
            refresh();
          }, it.user === value, "");
          b.title = value + " · " + band(value).label;
          if (!ctx.setup) b.disabled = true;
          buttons.push(put(row, b));
        })(s);
        var area = put(editor, el("textarea"));
        area.value = draft.feedback;
        area.placeholder = "Ý kiến của bạn về ý này (vì sao điểm này, muốn sửa gì).";
        area.setAttribute("aria-label", "Ý kiến của bạn về ý: " + it.title);
        area.addEventListener("input", function () { grow(area); draft.feedback = area.value || ""; refresh(); });
        var toggle = function () {
          editor.hidden = !editor.hidden;
          head.setAttribute("aria-expanded", editor.hidden ? "false" : "true");
          block.className = editor.hidden ? "gd-item" : "gd-item open";
          showMarks();
          if (!editor.hidden) grow(area);
        };
        head.addEventListener("click", toggle);
        head.addEventListener("keydown", function (e) {
          if (!e || (e.key !== "Enter" && e.key !== " ")) return;
          if (e.preventDefault) e.preventDefault();
          toggle();
        });
        showMarks();
        if (it.previous) {
          add(block, el("p", "gd-note", "Bản trước của ý này bạn chấm " + (it.previous.score === null ? "–" : it.previous.score) +
            (it.previous.feedback ? " · " + it.previous.feedback : "") + ". Ý đã được viết lại nên cần chấm lại."));
        }
        drafts.push({
          id: it.id,
          changed: function () { return draft.score !== it.user || draft.feedback.trim() !== (it.feedback || ""); },
          value: function () { return { score: draft.score, feedback: draft.feedback.trim() }; }
        });
      });
      var foot = put(body, el("div", "gd-row gd-save"));
      saveNote = put(foot, el("span", "", ""));
      saveBtn = put(foot, button("Lưu chấm điểm", function () {
        var items = {};
        drafts.forEach(function (d) { if (d.changed()) items[d.id] = d.value(); });
        saveBtn.disabled = true;
        send("/api/gamedesign/review", { path: node.path, table: state.table, items: items },
          function (res) { return "Đã lưu điểm cho " + res.saved + " ý của node " + where(node) + "."; },
          function (text) { saveNote.textContent = "Không lưu: " + text; saveBtn.disabled = false; });
      }, null, "gd-btn main"));
      refresh();
    };

    var renderQuestions = function (body, node) {
      if (state.table !== "idea" || !node.questions.length) return;
      add(body, el("h3", "", "Câu hỏi cần bạn chọn"));
      var ok = null, status = null;
      var readers = node.questions.map(function (q) {
        var block = put(body, el("div", "gd-q"));
        add(block, el("h4", "", q.text));
        if (q.why) add(block, el("p", "why", q.why));
        add(block, el("p", "kind", q.multi ? "Chọn một hoặc nhiều" : "Chọn một"));
        var inputs = q.options.map(function (o) {
          var label = put(block, el("label"));
          var input = put(label, el("input"));
          input.type = q.multi ? "checkbox" : "radio";
          input.name = "gd-" + node.path + "-" + q.id;
          input.value = o.id;
          input.checked = q.chosen.indexOf(o.id) >= 0;
          var text = put(label, el("div"));
          add(text, el("span", "", o.label));
          if (o.detail) add(text, el("small", "", o.detail));
          input.addEventListener("change", function () {
            // Radio trong DOM thật tự bỏ chọn ô khác; làm tay để không phụ thuộc trình duyệt.
            if (!q.multi && input.checked) inputs.forEach(function (i) { if (i !== input) i.checked = false; });
            update();
          });
          return input;
        });
        var other = hiddenInput(block, "ý kiến khác", q.other || "", "Ý kiến khác cho câu: " + q.text, function () { update(); });
        return {
          id: q.id, block: block,
          value: function () {
            return { chosen: inputs.filter(function (i) { return i.checked; }).map(function (i) { return i.value; }),
                     other: (other.value || "").trim() };
          }
        };
      });
      var update = function () {
        var left = 0;
        readers.forEach(function (r) {
          var v = r.value(), done = v.chosen.length > 0 || v.other.length > 0;
          r.block.className = done ? "gd-q done" : "gd-q";
          if (!done) left++;
        });
        ok.disabled = left > 0 || !ctx.setup;
        status.textContent = !ctx.setup ? SERVER_HINT : left ? "Còn " + left + "/" + readers.length + " câu chưa chọn." : "Đã chọn đủ.";
      };
      var foot = put(body, el("div", "gd-row"));
      status = put(foot, el("span", "", ""));
      ok = put(foot, button("Lưu câu trả lời", function () {
        var answers = {};
        readers.forEach(function (r) { answers[r.id] = r.value(); });
        ok.disabled = true;
        send("/api/gamedesign/answers", { path: node.path, answers: answers },
          function (res) { return "Đã lưu " + res.saved + " câu trả lời của node " + where(node) + ". Phiên @GameDesigner sẽ đọc và cập nhật node."; },
          function (text) { status.textContent = "Không lưu: " + text; ok.disabled = false; });
      }, null, "gd-btn main"));
      update();
    };

    var renderPromote = function (body, node) {
      if (state.table !== "idea") return;
      add(body, el("h3", "", "Chốt sang live"));
      if (!ctx.setup) { add(body, el("p", "muted", "Mở trang qua python Management/tools/setup_server.py để chốt.")); return; }
      if (!node.dirty) { add(body, el("p", "muted", "Node này và các node con đã giống live.")); return; }
      var failing = failingBelow(node);
      var row = put(body, el("div", "gd-row"));
      var one = put(row, button("Chốt node này", function () { promote(node, false); }, null, "gd-btn main"));
      var all = put(row, button("Chốt cả nhánh", function () { promote(node, true); }));
      one.title = "Chỉ chép data.md, CLAUDE.md và .images của node này; các node con trong live giữ nguyên.";
      all.title = "Chép node này và mọi node con; nhánh tương ứng trong live bị thay bằng bản đề xuất.";
      one.disabled = !node.passed || node.state === "same";
      all.disabled = failing > 0;
      add(body, el("p", "gd-note", !node.passed ? "Node chưa đạt nên chưa chốt được."
        : failing ? failing + " node con chưa đạt: chỉ chốt riêng node này được."
        : "Không cần PIN. Nút Sync ở thanh trên chốt mọi node đang đạt cùng lúc."));
    };

    var eventText = function (e) {
      if (e.event === "review") {
        return { out: e.rejected === true, text: "Bạn chấm ý " + e.item + (e.table === "live" ? " (ở bảng đã chốt)" : "") + ": " + (e.score === null || e.score === undefined ? "không điểm" : e.score) +
          (e.feedback ? " · " + e.feedback : "") + (e.rejected ? " (bị loại: " + e.text + ")" : "") };
      }
      if (e.event === "data") {
        var parts = [];
        if ((e.added || []).length) parts.push("thêm " + e.added.join(", "));
        (e.rewritten || []).forEach(function (r) { parts.push("viết lại " + r.id + " (trước: " + r.old + ")"); });
        (e.removed || []).forEach(function (r) { parts.push("bỏ " + r.id + " (" + r.text + ")"); });
        return { out: false, text: "data.md đổi" + (parts.length ? ": " + parts.join("; ") : "") + " · hash " + e.hash };
      }
      if (e.event === "answer") {
        return { out: false, text: "Bạn trả lời \"" + e.text + "\": " + (e.chosen || []).join(", ") + (e.other ? " · ý kiến khác: " + e.other : "") };
      }
      if (e.event === "promote") return { out: false, text: "Chốt sang live bởi " + e.by + " · hash " + e.hash };
      if (e.event === "removed") return { out: true, text: "Xoá node con " + e.key + (e.title ? " (" + e.title + ")" : "") };
      return { out: false, text: e.event };
    };

    // ---------------------------------------------------------------- dưới bản đồ: mọi thứ của node đang chọn
    var renderDetail = function (root) {
      detailBox.textContent = "";
      var node = state.selected === null ? null : find(root, state.selected);
      if (!node) { add(detailBox, el("p", "muted gap", "Bấm một node trên bản đồ để xem và duyệt các ý cốt lõi của nó.")); return; }
      var body = put(detailBox, ctx.section(""));
      body.className = "gd-detail";
      add(body, el("h2", "", node.title || "(chưa đặt tên)"));
      add(body, el("p", "gd-path", "Vòng " + node.round + " · " + ((data.rounds || [])[node.round - 1] || "") + " · " +
        table().dir.replace("game-design/", "") + (node.path ? node.path + "/" : "") + " · hash data " + node.dataHash));
      if (node.summary) add(body, el("p", "", node.summary));
      // Ý cốt lõi: bấm vào một ý để hiện chi tiết, hàng nút chấm điểm và ô nhập ý kiến.
      if (state.table === "idea") renderVerdict(body, node);
      renderItems(body, node);
      renderQuestions(body, node);
      renderPromote(body, node);
      add(body, el("h3", "", "Thang điểm"));
      var scale = put(body, el("div", "gd-scale"));
      (data.bands || []).forEach(function (b) { add(scale, el("span", "", b.min + "–" + b.max + ": " + b.label)); });
      add(body, el("p", "gd-note", MODES.filter(function (m) { return m.key === mode(); })[0].hint.replace("mức đạt", passMin()) +
        " Điểm của bạn là tuyệt đối và dùng chung cho bảng đề xuất và bảng đã chốt."));
      add(body, el("h3", "", "Diễn giải (CLAUDE.md)"));
      renderText(ctx, body, node.body.replace(/^# .*\n?/, "") || "Chưa có diễn giải.");
      if (node.images.length) {
        add(body, el("h3", "", "Ảnh"));
        var figs = put(body, el("div", "gd-images"));
        node.images.forEach(function (img) {
          var fig = put(figs, el("figure"));
          var pic = put(fig, el("img"));
          pic.src = table().dir + (node.path ? node.path + "/" : "") + ".images/" + encodeURIComponent(img.file);
          pic.alt = img.note || img.file;
          pic.loading = "lazy";
          add(fig, el("figcaption", "", img.file + (img.note ? ": " + img.note : "")));
        });
      }
      // Lịch sử nằm ở node đề xuất; node live dùng lịch sử của node đề xuất cùng đường dẫn.
      var source = state.table === "idea" ? node : find(data.idea, node.path);
      var history = source ? source.history : [];
      if (history.length) {
        add(body, el("h3", "", "Lịch sử (mới nhất trước)"));
        var list = put(body, el("ul", "gd-history"));
        history.forEach(function (e) {
          var t = eventText(e), li = put(list, el("li", t.out ? "out" : ""));
          add(li, el("time", "", e.at || ""), el("span", "", t.text));
        });
      }
    };

    var render = function () {
      var root = tree();
      if (root && state.focus !== null && !find(root, state.focus)) state.focus = null;
      if (root && state.selected !== null && !find(root, state.selected)) state.selected = null;
      if (!root) { state.focus = null; state.selected = null; }
      renderBar();
      renderNotice();
      mapBox.textContent = "";
      detailBox.textContent = "";
      if (!data) {
        add(mapBox, el("p", "muted", "Chưa có " + DATA_FILE + ". Chạy: python Management/tools/sync_page.py (Stop hook tự chạy sau mỗi lượt chat)."));
        return;
      }
      if (!root) { add(mapBox, el("p", "muted", table().empty)); return; }
      renderMap(root);
      renderDetail(root);
    };

    render();
  };

  window.CORESIMPLE_VIEWS = window.CORESIMPLE_VIEWS || {};
  window.CORESIMPLE_VIEWS.gamedesign = build;
})();
