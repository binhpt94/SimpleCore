// Tab "Game design" của trang quản lý: bản đồ node từ ý tưởng sơ khai tới quyết định chi tiết.
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
    ".gd-bar { display: flex; flex-wrap: wrap; gap: 8px 10px; align-items: center; margin-bottom: 16px; }",
    ".gd-bar .gd-time { color: var(--muted); font-size: 13px; margin-left: auto; }",
    ".gd-btn { padding: 7px 14px; border: 1px solid var(--line); border-radius: 6px; background: var(--panel); color: var(--text); font: inherit; font-size: 14px; cursor: pointer; }",
    ".gd-btn:hover { border-color: var(--accent); }",
    ".gd-btn:focus-visible, .gd-card:focus-visible, .gd-chip:focus-visible, .gd-score button:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }",
    ".gd-btn[aria-pressed='true'] { background: var(--accent); border-color: var(--accent); color: var(--panel); font-weight: 600; }",
    ".gd-btn:disabled { opacity: .5; cursor: default; }",
    ".gd-btn.main { border-color: var(--accent); color: var(--accent); font-weight: 600; }",
    ".gd-group { display: flex; flex-wrap: wrap; gap: 4px; align-items: center; padding: 4px 6px 4px 10px; border: 1px solid var(--line); border-radius: 8px; }",
    ".gd-group-name { font-size: 11px; letter-spacing: .05em; text-transform: uppercase; color: var(--muted); margin-right: 4px; white-space: nowrap; }",
    ".gd-group .gd-btn { padding: 5px 11px; font-size: 13.5px; }",
    ".gd-confirm { padding: 10px 14px; border: 1px solid var(--accent); border-radius: 8px; }",
    ".gd-confirm span { flex: 1; min-width: 240px; }",
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
    ".gd-frame, .gd-lightbox-svg { --fr-bg: var(--bg); --fr-ink: var(--text); --fr-line: var(--line); --fr-accent: var(--accent); --fr-box: var(--panel); --fr-accent-soft: var(--panel); max-width: 760px; border: 1px solid var(--line); border-radius: 8px; overflow: hidden; }",
    ".gd-frame-tabs { display: flex; flex-wrap: wrap; gap: 6px; margin: 0 0 8px; }",
    ".gd-frame-tabs .gd-btn.on { border-color: var(--accent); color: var(--accent); }",
    ".gd-frame-warn { margin: 8px 0 0; padding-left: 20px; font-size: 13px; color: var(--bad); }",
    ".gd-frame-src { margin-top: 6px; font-size: 13px; }",
    ".gd-frame-src pre { white-space: pre; overflow-x: auto; font-size: 12.5px; }",
    ".gd-zoomable { cursor: zoom-in; }",
    ".gd-lightbox { position: fixed; inset: 0; z-index: 1000; background: rgba(0,0,0,.82); display: flex; align-items: center; justify-content: center; padding: 24px; cursor: zoom-out; }",
    ".gd-lightbox img, .gd-lightbox .gd-lightbox-svg { max-width: 96vw; max-height: 90vh; object-fit: contain; background: var(--bg); border-radius: 6px; }",
    ".gd-lightbox .gd-lightbox-svg { width: 96vw; }",
    ".gd-lightbox-close { position: fixed; top: 12px; right: 16px; font-size: 28px; line-height: 1; color: #fff; background: none; border: 0; cursor: pointer; }",
    ".gd-lightbox-cap { position: fixed; bottom: 12px; left: 0; right: 0; text-align: center; color: #eee; font-size: 13px; }",
    ".gd-art { display: flex; flex-wrap: wrap; gap: 14px; }",
    ".gd-art figure { margin: 0; width: 280px; }",
    ".gd-art .gd-art-pic { position: relative; }",
    ".gd-art img { display: block; width: 100%; max-height: 200px; object-fit: contain; background: var(--bg); border: 1px solid var(--line); border-radius: 6px; }",
    ".gd-art .gd-art-label { position: absolute; top: 6px; left: 6px; padding: 2px 8px; border-radius: 10px; font-size: 12px; font-weight: 600; background: var(--panel); color: var(--text); border: 1px solid var(--line); }",
    ".gd-art .gd-art-label.stale { background: var(--bad); color: #fff; border-color: var(--bad); }",
    ".gd-art .gd-art-label.wait { border-color: var(--accent); color: var(--accent); }",
    ".gd-art .gd-art-label.ok { background: var(--ok); color: #fff; border-color: var(--ok); }",
    ".gd-art .gd-art-anchor { position: absolute; top: 6px; right: 6px; padding: 2px 8px; border-radius: 10px; font-size: 12px; background: var(--accent); color: var(--panel); }",
    ".gd-art figcaption { font-size: 12.5px; color: var(--muted); margin-top: 4px; }",
    ".gd-art .gd-art-below { position: absolute; bottom: 6px; left: 6px; padding: 2px 8px; border-radius: 10px; font-size: 12px; font-weight: 600; background: var(--bad); color: #fff; }",
    ".gd-art .gd-inline { margin: 6px 0 0; }",
    ".gd-art textarea { width: 100%; min-height: 54px; font: inherit; font-size: 13px; }",
    ".gd-styles { display: grid; gap: 12px; }",
    ".gd-style { border: 1px solid var(--line); border-radius: 8px; padding: 10px 12px; }",
    ".gd-style h4 { margin: 0 0 4px; }",
    ".gd-style p { white-space: pre-wrap; margin: 0 0 8px; font-size: 13.5px; color: var(--muted); }",
    ".gd-images { display: flex; flex-wrap: wrap; gap: 12px; }",
    ".gd-images figure { margin: 0; width: 220px; }",
    ".gd-images img { display: block; width: 100%; max-height: 180px; object-fit: contain; background: var(--bg); border: 1px solid var(--line); border-radius: 6px; }",
    ".gd-images figcaption { font-size: 12.5px; color: var(--muted); margin-top: 4px; }",
    ".gd-history { margin: 0; padding: 0; list-style: none; font-size: 13.5px; }",
    ".gd-history li { padding: 8px 0; border-bottom: 1px solid var(--line); }",
    ".gd-history time { display: block; font-size: 12px; color: var(--muted); }",
    ".gd-history .out { color: var(--bad); }",
    ".gd-card.dim, .gd-chips.dim { opacity: .28; }",
    ".gd-tag.ph { font-weight: 600; }",
    ".gd-tag.ph-idea { border-color: var(--accent); background: var(--accent); color: var(--panel); }",
    ".gd-tag.ph-prototype { border-color: var(--accent); color: var(--accent); }",
    ".gd-tag.ph-full { border-color: var(--muted); color: var(--muted); }",
    ".gd-tag.ph-optional, .gd-tag.ph-none { border-style: dashed; font-weight: 400; }",
    ".gd-rank { margin: 8px 0 0; border-top: 1px solid var(--line); }",
    ".gd-tag.ph-done { opacity: .75; }",
    ".gd-work { display: flex; flex-wrap: wrap; gap: 6px; align-items: center; padding: 7px 0; border-bottom: 1px solid var(--line); }",
    ".gd-work-name { flex: 1; min-width: 120px; }",
    ".gd-cell { padding: 3px 10px; border: 1px solid var(--line); border-radius: 4px; background: transparent; color: var(--muted); font: inherit; font-size: 12px; cursor: pointer; }",
    ".gd-cell:hover { border-color: var(--accent); }",
    ".gd-cell:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }",
    ".gd-cell.own { font-weight: 700; color: var(--text); }",
    ".gd-cell.doing { border-color: var(--warn-text); background: var(--warn-bg); color: var(--warn-text); }",
    ".gd-cell.done { border-color: var(--ok); background: var(--ok); color: var(--panel); }",
    ".gd-cell:disabled { opacity: .6; cursor: default; }",
    ".gd-gate { margin: 8px 0 0; font-size: 13.5px; }",
    ".gd-gate.ok { color: var(--ok); }",
    ".gd-gate.bad { color: var(--bad); }",
    ".gd-context { margin: 10px 0 0; font-size: 13.5px; }",
    ".gd-context summary { cursor: pointer; color: var(--accent); }",
    ".gd-context ul, .gd-refs { margin: 4px 0 8px; padding-left: 20px; font-size: 13px; }",
    ".gd-bad { color: var(--bad); }",
    ".gd-score .gd-link { margin-left: 10px; font-size: 12px; }",
    ".gd-score .gd-link:disabled { opacity: .4; cursor: default; }",
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

  // Bấm một ảnh (hoặc hình frame) để xem to: lớp phủ toàn màn, đóng bằng Esc, bấm nền hoặc nút ×.
  var lightbox = function (source, caption) {
    var box = document.createElement("div");
    box.className = "gd-lightbox";
    box.setAttribute("role", "dialog");
    var shown;
    if (typeof source === "string") {
      shown = document.createElement("img");
      shown.src = source;
      shown.alt = caption || "";
    } else {
      shown = document.createElement("div");
      shown.className = "gd-lightbox-svg";
      shown.innerHTML = source.innerHTML;
    }
    box.appendChild(shown);
    var close = document.createElement("button");
    close.className = "gd-lightbox-close";
    close.type = "button";
    close.textContent = "×";
    close.setAttribute("aria-label", "Đóng");
    box.appendChild(close);
    if (caption) {
      var cap = document.createElement("div");
      cap.className = "gd-lightbox-cap";
      cap.textContent = caption;
      box.appendChild(cap);
    }
    var done = function () { box.remove(); document.removeEventListener("keydown", key); };
    var key = function (e) { if (e.key === "Escape") done(); };
    box.addEventListener("click", function (e) { if (e.target === box || e.target === close || e.target === shown) done(); });
    document.addEventListener("keydown", key);
    document.body.appendChild(box);
  };
  var zoomable = function (node, source, caption) {
    node.classList.add("gd-zoomable");
    node.title = "Bấm để xem to";
    node.addEventListener("click", function () { lightbox(source || node.src, caption); });
    return node;
  };

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
    // ask: việc đang chờ nhập PIN để xác nhận ({kind: "sync"} hoặc {kind: "mode", key}).
    // phase: giai đoạn đang lọc trên bản đồ (khoá trong data.phases), null là tất cả.
    var state = { table: "live", focus: null, selected: null, message: null, ask: null, phase: null };

    add(pane, el("h1", "", "Game design"));
    add(pane, el("p", "sub", "Bản đồ thiết kế, từ ý tưởng sơ khai tới quyết định chi tiết. Bấm một node, phần dưới bản đồ hiện các ý cốt lõi của nó: bấm vào từng ý để xem chi tiết, chấm điểm và ghi ý kiến; bấm một feature (vòng " +
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
    // Độ quan trọng: giai đoạn của một điểm, nhãn "IDEA 9", và node có cần cho giai đoạn đang lọc không.
    var phases = function () { return data && data.phases || []; };
    var phase = function (key) { return phases().filter(function (p) { return p.key === key; })[0] || null; };
    var rank = function (node) { return node.priority || { ai: null, user: null, base: null, effective: null, offset: 0, from: "", phase: "", why: "", leaf: true }; };
    var phaseTag = function (node) {
      var r = rank(node), p = phase(r.phase);
      // Lá đã xong vẫn giữ điểm của nó (thêm dấu ✓); node cha mà mọi lá bên dưới đã xong thì chỉ ghi "xong".
      var text = p ? p.name + " " + r.effective + (r.done ? " ✓" : "") : (r.done ? "xong ✓" : "chưa xếp");
      var tag = el("span", "gd-tag ph" + (p ? " ph-" + p.key : " ph-none") + (r.done ? " ph-done" : ""), text);
      tag.title = (p ? "Độ quan trọng " + r.effective + ": cần cho giai đoạn " + p.name + " (" + p.label + ")"
        : r.done ? "Mọi node lá bên dưới đã xong" : "Chưa có điểm độ quan trọng") + (r.done && p ? ". Đã xong." : "");
      return tag;
    };
    // Lọc theo giai đoạn là cộng dồn: chọn PROTOTYPE thì node của IDEA cũng sáng.
    var needed = function (node) {
      var p = state.phase ? phase(state.phase) : null;
      if (!p) return true;
      var e = rank(node).effective;
      return e !== null && e !== undefined && e >= p.min;
    };
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
    // Thanh trên chia nhóm theo việc: xem gì (bảng, bản đồ, lọc giai đoạn) rồi tới thay đổi (chốt, mode).
    // Nút thay đổi dữ liệu chung (Sync, đổi mode) có dấu khoá và phải nhập PIN mới chạy.
    var renderBar = function () {
      bar.textContent = "";
      var group = function (name, hint) {
        var g = put(bar, el("div", "gd-group"));
        g.setAttribute("role", "group");
        g.setAttribute("aria-label", name);
        var label = put(g, el("span", "gd-group-name", name));
        if (hint) label.title = hint;
        return g;
      };
      var ask = function (next) { state.ask = next; state.message = null; render(); };

      var tables = group("Bảng", "Bảng đã chốt (live) hay bảng AI đề xuất (idea)");
      TABLES.forEach(function (t) {
        add(tables, button(t.label, function () { state.table = t.key; state.message = null; render(); }, state.table === t.key));
      });

      var map = group("Bản đồ");
      var whole = put(map, button("Tất cả feature", function () { state.focus = null; render(); }));
      whole.title = "Bỏ chế độ xem một nhánh, hiện lại mọi feature";
      whole.disabled = state.focus === null;
      var again = put(map, button("Reload", reload));
      again.title = "Đọc lại dữ liệu từ Management/game-design";

      if (data && phases().length) {
        var filter = group("Giai đoạn", "Làm mờ các node không cần cho giai đoạn được chọn");
        var all = put(filter, button("Tất cả", function () { state.phase = null; render(); }, state.phase === null));
        all.title = "Không lọc theo giai đoạn";
        phases().filter(function (ph) { return ph.min > 1; }).forEach(function (ph) {
          var b = put(filter, button(ph.name, function () { state.phase = ph.key; render(); }, state.phase === ph.key));
          b.title = "Làm mờ các node không cần cho giai đoạn " + ph.name + " (độ quan trọng dưới " + ph.min + "). " + ph.label + ".";
        });
      }

      if (data) {
        var live = group("Chốt 🔒", "Chép sang bảng đã chốt. Cần PIN.");
        var ready = syncable();
        var sync = put(live, button(ready ? "Sync (" + ready + ")" : "Sync", function () { ask({ kind: "sync" }); }, null, ready ? "gd-btn main" : "gd-btn"));
        sync.title = "Chép sang live mọi node đề xuất đang đạt mà khác live, từ gốc xuống, dừng ở node chưa đạt. Cần PIN." +
          (ctx.setup ? "" : " Cần mở trang qua setup_server.py.");
        sync.disabled = !ready || !ctx.setup;

        var modes = group("Mode 🔒", "Cách @GameDesigner làm việc. Đổi mode cần PIN.");
        MODES.forEach(function (m) {
          var b = put(modes, button(m.label, function () { if (m.key !== mode()) ask({ kind: "mode", key: m.key }); }, mode() === m.key));
          b.title = m.hint + (ctx.setup ? " Đổi mode cần PIN." : " Muốn đổi mode: mở trang qua setup_server.py.");
          if (!ctx.setup && mode() !== m.key) b.disabled = true;
        });
      }
      add(bar, el("span", "gd-time", data && data.generatedAt ? "Đọc lúc " + data.generatedAt.replace("T", " ") : ""));
    };

    var renderNotice = function () {
      noticeBox.textContent = "";
      if (state.ask && ctx.setup) {
        // Một khung xác nhận dùng chung cho mọi nút cần PIN: nói rõ sẽ làm gì, rồi mới hỏi PIN.
        var target = state.ask.kind === "mode" ? MODES.filter(function (m) { return m.key === state.ask.key; })[0] : null;
        var art = state.ask.kind === "art" ? state.ask.order : null;
        var what = target ? "Đổi sang " + target.label + ": " + target.hint
          : art ? "Duyệt gen " + art.ref + ": " + art.items.length + " ảnh (" + art.items.join(", ") + "), style " + (art.style || "?") +
            ". Mỗi ảnh tối đa 2 lần chạy Codex; tốn lượt Codex."
          : "Sync: chép " + syncable() + " node đang đạt sang live (bảng đã chốt), ghi đè bản cũ của các node đó.";
        var confirmBox = put(noticeBox, el("div", "gd-confirm gd-inline"));
        add(confirmBox, el("span", "", what));
        var pin = put(confirmBox, el("input"));
        pin.type = "password";
        pin.autocomplete = "new-password";
        pin.placeholder = "PIN (không lưu)";
        pin.setAttribute("aria-label", "PIN để xác nhận");
        var go = function () {
          if (!pin.value) { pin.focus(); return; }
          var failed = function (text) { say("Không thực hiện: " + text); };
          if (art) {
            send("/api/studio/approve", { pin: pin.value, ref: art.ref }, function (res) {
              state.ask = null;
              return res.message || "Đã duyệt gen.";
            }, failed);
          } else if (target) {
            send("/api/gamedesign/mode", { pin: pin.value, mode: target.key }, function () {
              state.ask = null;
              return "Đã đổi sang " + target.label + ".";
            }, failed);
          } else {
            send("/api/gamedesign/sync", { pin: pin.value }, function (res) {
              state.ask = null;
              return res.written.length ? "Đã chép sang live: " + res.written.join(", ") : "Không có node nào để chép sang live.";
            }, failed);
          }
        };
        pin.addEventListener("keydown", function (e) { if (e && e.key === "Enter") go(); });
        add(confirmBox, button(target ? "Đổi mode" : art ? "Duyệt gen" : "Sync", go, null, "gd-btn main"), button("Thôi", function () { state.ask = null; render(); }));
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
      var waitingArt = (data && data.artPending || []).filter(function (o) { return !o.approved; });
      if (waitingArt.length) {
        add(noticeBox, el("p", "", waitingArt.length + " order ảnh art đang chờ bạn duyệt gen (mỗi lần gen tốn lượt Codex):"));
        var artList = put(noticeBox, el("div", "gd-inline"));
        waitingArt.forEach(function (o) {
          add(artList, button(o.node + " · " + o.title + " (" + o.items.length + " ảnh)", function () {
            state.selected = o.node; state.message = null; render();
          }));
        });
      }
      var missing = data && state.table === "idea" ? data.missing || [] : [];
      if (missing.length) {
        add(noticeBox, el("p", "", "Vòng 2 còn thiếu " + missing.length + " feature hạ tầng (việc của @GameDesigner):"));
        var owed = put(noticeBox, el("ul", "gd-problems"));
        missing.forEach(function (p) { add(owed, el("li", "", p.path + ": " + p.text)); });
      }
      var noCode = data && state.table === "idea" ? data.missingCode || [] : [];
      if (noCode.length) {
        add(noticeBox, el("p", "", noCode.length + " feature chưa có code.json nên chưa code được (việc của @GameDesigner, tên tự đặt theo key; chốt lại sang live sau khi ghi):"));
        var nc = put(noticeBox, el("ul", "gd-problems"));
        noCode.forEach(function (p) { add(nc, el("li", "", p.path + ": " + p.text)); });
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
      var out = [phaseTag(n)];
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
        var kind = (!idea ? "" : n.redo ? " redo" : n.passed && n.state === "same" ? " done" : " wait") + (needed(n) ? "" : " dim");
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
        var row = el("div", needed(n) ? "gd-chips" : "gd-chips dim");
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
    // Độ quan trọng của node: một hàng; bấm vào mới mở hàng nút 0-10. Bấm một điểm là lưu ngay.
    var renderPriority = function (body, node) {
      var r = rank(node);
      var block = put(body, el("div", "gd-rank gd-item"));
      var head = put(block, el("div", "gd-head"));
      head.tabIndex = 0;
      head.title = "Bấm để chấm độ quan trọng của node này";
      head.setAttribute("role", "button");
      head.setAttribute("aria-expanded", "false");
      add(head, el("p", "text", "Độ quan trọng"));
      var marks = put(head, el("div", "gd-marks"));
      add(marks, phaseTag(node));
      var aiMark = put(marks, el("span", "gd-mark" + (r.base === null ? " none" : ""),
        (r.leaf ? "AI: " : "Cao nhất bên dưới: ") + (r.base === null ? "Chưa" : r.base)));
      aiMark.title = r.leaf ? "Điểm AI chấm cho node lá này" + (r.why ? ": " + r.why : "") : "Node cha lấy điểm cao nhất của các node bên dưới";
      add(marks, el("span", "gd-mark" + (r.user === null ? " none" : ""), "User: " + (r.user === null ? "Chưa" : r.user)));
      var editor = put(block, el("div", "gd-edit"));
      editor.hidden = true;
      if (r.leaf && r.why) add(editor, el("p", "gd-more", "AI: " + r.why));
      if (r.user === null && r.offset) {
        add(editor, el("p", "gd-id", (r.offset > 0 ? "+" : "") + r.offset + " từ " + r.from + " (bạn đã chấm node đó; node này giữ khoảng cách so với nó)."));
      }
      var row = put(editor, el("div", "gd-score"));
      var save = function (score) {
        state.message = null;
        send("/api/gamedesign/priority", { path: node.path, score: score }, function () {
          return score === null ? "Đã bỏ điểm độ quan trọng của bạn ở node " + where(node) + "."
            : "Đã lưu độ quan trọng " + score + " cho node " + where(node) + ".";
        });
      };
      for (var s = 0; s <= 10; s++) (function (value) {
        var b = button(String(value), function () { save(value); }, r.user === value, "");
        var p = phases().filter(function (x) { return value >= x.min && value <= x.max; })[0];
        b.title = value + (p ? " · " + p.name + ": " + p.label : "");
        if (!ctx.setup) b.disabled = true;
        put(row, b);
      })(s);
      var clear = put(row, button("Bỏ điểm của tôi", function () { save(null); }, null, "gd-link"));
      clear.disabled = r.user === null || !ctx.setup;
      if (!ctx.setup) add(editor, el("p", "gd-id", SERVER_HINT));
      var toggle = function () {
        editor.hidden = !editor.hidden;
        head.setAttribute("aria-expanded", editor.hidden ? "false" : "true");
      };
      head.addEventListener("click", toggle);
      head.addEventListener("keydown", function (e) {
        if (!e || (e.key !== "Enter" && e.key !== " ")) return;
        if (e.preventDefault) e.preventDefault();
        toggle();
      });
    };

    // Tiến độ của node lá: một ô cho mỗi giai đoạn; bấm ô để đổi chưa làm → đang làm → xong (lưu ngay).
    var WORK = { todo: "chưa làm", doing: "đang làm", done: "xong" };
    var renderProgress = function (body, node) {
      if (!rank(node).leaf || !node.progress) return;
      var states = data.workStates || ["todo", "doing", "done"];
      var row = put(body, el("div", "gd-work"));
      add(row, el("span", "gd-work-name", "Tiến độ"));
      phases().filter(function (p) { return node.progress[p.key]; }).forEach(function (p) {
        var cell = node.progress[p.key];
        var next = states[(states.indexOf(cell.state) + 1) % states.length];
        var b = put(row, button(p.name + ": " + (WORK[cell.state] || cell.state), function () {
          state.message = null;
          send("/api/gamedesign/progress", { path: node.path, phase: p.key, state: next },
            function () { return "Đã ghi tiến độ " + p.name + " của node " + where(node) + ": " + (WORK[next] || next) + "."; });
        }, null, "gd-cell " + cell.state + (rank(node).phase === p.key ? " own" : "")));
        b.title = (cell.by ? "Đánh dấu bởi " + cell.by + " lúc " + cell.at + ". " : "") + (cell.note ? cell.note + ". " : "") +
          "Bấm để đổi sang: " + (WORK[next] || next) + "." + (rank(node).phase === p.key ? " Đây là giai đoạn của node này theo độ quan trọng." : "");
        if (!ctx.setup) b.disabled = true;
      });
      if (!ctx.setup) add(row, el("span", "gd-id", SERVER_HINT));
    };

    // Node feature: thư mục code đã khai và có được code chưa (đúng điều kiện guard áp cho phiên feature).
    var renderCode = function (body, node) {
      var gates = node.codeGate || [];
      if (!gates.length) {
        if (node.round === 2) add(body, el("p", "gd-note", "Node feature này chưa khai thư mục code (code.json): chưa phiên feature nào được code theo nó."));
        return;
      }
      gates.forEach(function (g) {
        var line = put(body, el("p", "gd-gate " + (g.ok ? "ok" : "bad")));
        add(line, el("code", "", g.unity), el("span", "", g.ok ? " được code: thiết kế đã tới vòng 4 trong live." : " đang khoá code: " + g.reason + "."));
      });
    };

    // Ngữ cảnh ghép lúc đọc: các node phía trên, các node và từ mà refs trỏ tới (node không chép lại nội dung cha).
    var pick = function (path) { state.selected = path; render(); };
    var renderContext = function (body, node, root) {
      var above = [], cur = root, keys = node.path ? node.path.split("/") : [];
      keys.forEach(function (k) {
        if (!cur) return;
        above.push(cur);
        cur = cur.children.filter(function (c) { return c.key === k; })[0] || null;
      });
      if (above.length) {
        var box = put(body, el("details", "gd-context"));
        add(box, el("summary", "", "Bám theo: " + above.length + " node phía trên"));
        above.forEach(function (a) {
          var line = put(box, el("p", ""));
          var link = put(line, button((a.path || "(gốc)") + " · vòng " + a.round + " · " + a.title, function () { pick(a.path); }, null, "gd-link"));
          link.title = "Chọn node này";
          if (a.summary) add(box, el("p", "gd-note", a.summary));
          var ul = put(box, el("ul", ""));
          a.items.forEach(function (it) { add(ul, el("li", "", it.title)); });
        });
      }
      var refs = (node.refs || []).concat((node.refMissing || []).map(function (r) { return { label: r.label, path: r.path, term: r.term, ok: "missing" }; }));
      if (refs.length) {
        add(body, el("h3", "", "Tham chiếu (refs)"));
        var list = put(body, el("ul", "gd-refs"));
        refs.forEach(function (r) {
          var li = put(list, el("li", ""));
          var state_ = r.ok === true ? "khớp" : r.ok === false ? "LỆCH: node đích đã đổi, cần rà lại" : r.ok === "missing" ? "nhắc tới mà chưa khai" : "không có node hay từ này";
          var target = r.term ? "glossary" : r.path;
          if (find(root, target)) put(li, button(r.label, function () { pick(target); }, null, "gd-link"));
          else add(li, el("code", "", r.label));
          add(li, el("span", r.ok === true ? "muted" : "gd-bad", " " + (r.title ? r.title + " · " : "") + state_));
        });
      }
      (node.refNotes || []).forEach(function (t) { add(body, el("p", "gd-note", t)); });
      var gloss = data.glossary && data.glossary[state.table];
      if (gloss && node.path === gloss.path) {
        add(body, el("h3", "", "Các từ được dùng ở đâu"));
        var ul2 = put(body, el("ul", "gd-refs"));
        gloss.terms.forEach(function (t) {
          add(ul2, el("li", "", t.term + ": " + (t.usedBy.length ? t.usedBy.join(", ") : "chưa node nào khai refs tới")));
        });
      }
    };

    // Dưới bản đồ: lời nhắc về thuật ngữ (không chặn gì): chưa có glossary, định nghĩa còn rải ở node khác, từ trùng.
    var renderTerms = function () {
      var notes = (data.termNotes && data.termNotes[state.table]) || [];
      if (!notes.length) return;
      var box = put(mapBox, el("details", "gd-context"));
      add(box, el("summary", "", "Thuật ngữ: " + notes.length + " lời nhắc (node glossary là nơi duy nhất định nghĩa từ)"));
      var ul = put(box, el("ul", ""));
      notes.forEach(function (n) { add(ul, el("li", "", n.path + ": " + n.text)); });
    };

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
      renderContext(body, node, root);
      renderPriority(body, node);
      renderProgress(body, node);
      renderCode(body, node);
      // Ý cốt lõi: bấm vào một ý để hiện chi tiết, hàng nút chấm điểm và ô nhập ý kiến.
      if (state.table === "idea") renderVerdict(body, node);
      renderItems(body, node);
      renderQuestions(body, node);
      renderPromote(body, node);
      add(body, el("h3", "", "Độ quan trọng và giai đoạn"));
      var stages = put(body, el("div", "gd-scale"));
      phases().forEach(function (p) { add(stages, el("span", "", p.min + "–" + p.max + " " + p.name + ": " + p.label)); });
      add(body, el("p", "gd-note", "AI chỉ chấm ở node lá; node cha lấy điểm cao nhất bên dưới. Điểm bạn chấm cho một node là tuyệt đối, " +
        "và các node con bạn chưa chấm dời theo cùng khoảng cách. Độ quan trọng không làm đổi hash, không ảnh hưởng việc đạt hay chốt."));
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
          zoomable(pic, null, img.file + (img.note ? ": " + img.note : ""));
          add(fig, el("figcaption", "", img.file + (img.note ? ": " + img.note : "")));
        });
      }
      // Style (vòng 1): .art-style của node gốc, tính vào hash của node gốc.
      if (node.styles && node.styles.length) {
        add(body, el("h3", "", "Style (phong cách hình ảnh)"));
        var styles = put(body, el("div", "gd-styles"));
        node.styles.forEach(function (s) {
          var card = put(styles, el("div", "gd-style"));
          add(card, el("h4", "", s.title + " (" + s.id + ")"), el("p", "", s.text));
          if (s.images.length) {
            var row = put(card, el("div", "gd-images"));
            s.images.forEach(function (name) {
              var fig = put(row, el("figure"));
              var pic = put(fig, el("img"));
              pic.src = table().dir + ".art-style/" + encodeURIComponent(name);
              pic.alt = name;
              pic.loading = "lazy";
              zoomable(pic, null, s.title + ": " + name);
              add(fig, el("figcaption", "", name));
            });
          }
        });
      }
      // Art: ảnh @Codex gen theo frame của node (order _design), nhãn tính từ hash node và hash style.
      var artPending = (data.artPending || []).filter(function (o) { return o.node === node.path; });
      if ((node.art && node.art.length) || artPending.length) {
        add(body, el("h3", "", "Art (ảnh concept)"));
        artPending.forEach(function (o) {
          var line = put(body, el("div", "gd-inline"));
          add(line, el("span", "", "Order " + o.ref + ": " + o.title + " · " + o.items.length + " ảnh · style " + (o.style || "?") +
            (o.approved ? " · đã duyệt, chờ @Codex gen" : o.state === "rejected" ? " · bạn đã trả, chờ duyệt lại" : " · chờ bạn duyệt gen")));
          if (!o.approved && ctx.setup) {
            add(line, button("Duyệt gen", function () { state.ask = { kind: "art", order: o }; state.message = null; render(); window.scrollTo(0, 0); }, null, "gd-btn main"));
          }
        });
        var latest = {};
        (node.art || []).forEach(function (a) { if (!latest[a.frame]) latest[a.frame] = a; });
        var gallery = put(body, el("div", "gd-art"));
        Object.keys(latest).forEach(function (frame) {
          var a = latest[frame];
          var fig = put(gallery, el("figure"));
          var holder = put(fig, el("div", "gd-art-pic"));
          var pic = put(holder, el("img"));
          pic.src = "../" + a.file.split("/").map(encodeURIComponent).join("/");
          pic.alt = frame;
          pic.loading = "lazy";
          zoomable(pic, null, frame + " · v" + a.version + " · " + a.label);
          var kind = a.stale.length ? "stale" : a.state === "chờ nhận" ? "wait" : a.state === "đang dùng" ? "ok" : "";
          add(holder, el("span", "gd-art-label " + kind, a.label));
          if (a.anchor) add(holder, el("span", "gd-art-anchor", "mốc"));
          if (a.below) add(holder, el("span", "gd-art-below", "chưa chuẩn"));
          add(fig, el("figcaption", "", frame + " · v" + a.version + " · " + a.ref + (a.styles.length ? " · style " + a.styles.join(", ") : "") +
            (a.below ? " · chưa chuẩn: " + a.below : "")));
          if (!ctx.setup) return;
          var acts = put(fig, el("div", "gd-inline"));
          if (a.state === "chờ nhận") {
            add(acts, button("Nhận", function () {
              send("/api/studio/accept", { ref: a.ref }, function (res) { return res.message || "Đã nhận."; });
            }, null, "gd-btn main"));
          }
          if (a.state === "chờ nhận" || a.state === "đang dùng") {
            add(acts, button("Trả", function () {
              acts.textContent = "";
              var note = put(acts, el("textarea"));
              note.placeholder = "Ảnh nào sai, sai gì, cần thế nào";
              add(acts, button("Gửi trả", function () {
                if (!note.value.trim()) { note.focus(); return; }
                send("/api/studio/reject", { ref: a.ref, text: note.value }, function (res) { return res.message || "Đã trả."; });
              }, null, "gd-btn main"), button("Thôi", function () { render(); }));
              note.focus();
            }));
          }
          if (a.state === "đang dùng" && !a.stale.length) {
            add(acts, button(a.anchor ? "Bỏ mốc" : "Đặt làm mốc", function () {
              send("/api/studio/anchor", { ref: a.ref, item: a.frame }, function (res) { return res.message || "Đã đổi mốc."; });
            }));
          }
        });
        var owed = ((window.CORESIMPLE_STUDIO || {}).debts || []).filter(function (d) {
          return d.state === "open" && d.owner === "_design/" + node.path.split("/").join(".");
        });
        if (owed.length) add(body, el("p", "bad-text", owed.length + " phiếu nợ art đang mở cho node này (" +
          owed.map(function (d) { return d.id + " " + d.item; }).join(", ") + "): xem tab Studio."));
        var older = (node.art || []).filter(function (a) { return latest[a.frame] !== a; });
        if (older.length) add(body, el("p", "gd-note", older.length + " bản cũ hơn vẫn giữ trong Resources (không ghi đè)."));
        if (!ctx.setup) add(body, el("p", "gd-note", "Nhận, trả, đặt mốc và duyệt gen cần mở trang qua server Setup (python Management/tools/setup_server.py)."));
      }
      // Frame: bố cục dạng chữ trong .frame-concept, đã được gamedesign.py giải số và vẽ sẵn thành SVG.
      if (node.frames && node.frames.length || node.frameNotes && node.frameNotes.length || node.frameNone) {
        add(body, el("h3", "", "Frame (bố cục ước lượng)"));
        if (node.frameNone) add(body, el("p", "gd-note", "Node này không có bố cục (frame: none)."));
        (node.frameNotes || []).forEach(function (t) { add(body, el("p", "gd-note", t)); });
        if (node.frames && node.frames.length) {
          var tabs = node.frames.length > 1 ? put(body, el("div", "gd-frame-tabs")) : null;
          var shown = put(body, el("div"));
          var show = function (i) {
            var fr = node.frames[i];
            shown.textContent = "";
            if (tabs) Array.prototype.forEach.call(tabs.children, function (b, j) { b.className = "gd-btn" + (j === i ? " on" : ""); });
            add(shown, el("p", "gd-note", fr.file + (fr.title ? ": " + fr.title : "") + " · " +
              (fr.unit === "px" ? "màn " + fr.view[2] + "x" + fr.view[3] + " px" : "chiến trường, đơn vị u") +
              ". Di chuột lên một hình để xem số."));
            if (!document.getElementById("gd-frame-css")) {
              var fs = document.createElement("style");
              fs.id = "gd-frame-css";
              fs.textContent = (window.CORESIMPLE_GAMEDESIGN || {}).frameCss || "";
              document.head.appendChild(fs);
            }
            var box = put(shown, el("div", "gd-frame"));
            box.innerHTML = fr.svg;
            zoomable(box, box, fr.file + (fr.title ? ": " + fr.title : ""));
            var notes = fr.warnings.concat(fr.errors);
            if (notes.length) {
              var ul = put(shown, el("ul", "gd-frame-warn"));
              notes.forEach(function (w) { add(ul, el("li", "", w)); });
            }
            var src = put(shown, el("details", "gd-frame-src"));
            add(src, el("summary", "", "Xem nguồn"), el("pre", "", fr.text));
          };
          if (tabs) node.frames.forEach(function (fr, i) {
            var b = put(tabs, el("button", "gd-btn", fr.file));
            b.type = "button";
            b.onclick = function () { show(i); };
          });
          show(0);
        }
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
      renderTerms();
      renderDetail(root);
    };

    render();
  };

  window.CORESIMPLE_VIEWS = window.CORESIMPLE_VIEWS || {};
  window.CORESIMPLE_VIEWS.gamedesign = build;
  window.CORESIMPLE_VIEWS.lightbox = lightbox;      // dùng chung với tab Studio
})();
