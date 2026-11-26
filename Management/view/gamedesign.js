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
  var OVERVIEW_LAST_ROUND = 2;   // bản đồ chung ("Tất cả feature") hiện tới vòng này
  var FOCUS_ROUND = 2;           // bấm node ở vòng này thì chỉ còn nhánh của nó, hiện tới vòng cuối
  var CARD_W = 312, CARD_H = 156, GAP_X = 64, GAP_Y = 14, HEAD_H = 40;
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
  var PICK_KEEP = 8, PICK_DROP = 2;      // điểm nháp của nút "Chọn ý này" trong nhóm ý trái nhau: ý được chọn, các ý còn lại
  var BY_NAMES = { "claude": "Claude", "gpt": "GPT" };   // AI nào đề xuất một ý
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
    ".gd-card { position: absolute; display: flex; flex-direction: column; justify-content: flex-start; align-items: stretch; text-align: left; padding: 9px 12px; border: 1px solid var(--line); border-left: 6px solid var(--line); border-radius: 8px; background: var(--bg); color: var(--text); font: inherit; cursor: pointer; overflow: hidden; }",
    // Viền theo chốt: node giống live viền đủ đậm, node chưa chốt viền cùng màu nhưng trong. Thanh trái là trạng thái.
    ".gd-card.fixed { border-color: var(--muted); }",
    ".gd-card.loose { border-color: color-mix(in srgb, var(--muted) 30%, transparent); }",
    ".gd-card.by-gpt strong { color: var(--gpt); }",
    ".gd-card:hover { border-color: var(--accent); }",
    ".gd-card.sel { border-color: var(--accent); box-shadow: 0 0 0 2px var(--accent); }",
    // Màu của thẻ theo trạng thái xấu nhất của node cộng cả nhánh bên dưới (chỉ là hiển thị, không lưu ở đâu).
    ".gd-card.redo { border-left-color: var(--bad); background: color-mix(in srgb, var(--bad) 7%, var(--bg)); }",
    ".gd-card.wait { border-left-color: var(--clash); background: color-mix(in srgb, var(--clash) 8%, var(--bg)); }",
    ".gd-card.done { border-left-color: var(--ok); }",
    ".gd-card strong { display: block; font-size: 14px; line-height: 1.3; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; padding-right: 30px; }",
    ".gd-card span.sum { display: -webkit-box; -webkit-line-clamp: 2; -webkit-box-orient: vertical; overflow: hidden; font-size: 12.5px; line-height: 1.35; color: var(--muted); margin-top: 3px; }",
    ".gd-card span.low { position: absolute; right: 8px; top: 7px; }",
    // Id của node (đường dẫn trong cây): để gọi tên node khi trao đổi; bấm là chép.
    ".gd-card span.id { display: block; margin-top: 2px; font: 11px/1.3 ui-monospace, SFMono-Regular, Consolas, monospace; color: var(--muted); white-space: nowrap; overflow: hidden; cursor: copy; }",
    ".gd-card span.id:hover { color: var(--accent); }",
    ".gd-card.follow { border-left-color: var(--muted); }",
    ".gd-group select { font: inherit; font-size: 13px; padding: 3px 6px; border: 1px solid var(--line); border-radius: 6px; background: var(--bg); color: var(--text); }",
    ".gd-group select.on { border-color: var(--accent); color: var(--accent); font-weight: 600; }",
    ".gd-tag.later { border-style: dashed; color: var(--muted); }",
    ".gd-tag.deepen { border-color: var(--clash); color: var(--clash); }",
    ".gd-ftable tr.dim { opacity: .35; }",
    ".gd-chip.follow { border-style: dashed; color: var(--muted); text-decoration: none; }",
    ".gd-chip.branch.follow { border-color: var(--muted); color: var(--muted); font-weight: 400; }",
    ".gd-tag.pull { border-color: var(--warn-text); color: var(--warn-text); }",
    ".gd-idline code { user-select: all; }",
    ".gd-chips { position: absolute; display: flex; gap: 4px; overflow: hidden; align-items: center; }",
    ".gd-chips.info .gd-tag { font-size: 11px; padding: 3px 7px; }",
    ".gd-tag.branch { font-weight: 600; }",
    ".gd-chip.branch { border-color: var(--bad); color: var(--bad); font-weight: 600; text-decoration: none; }",
    ".gd-chip.branch.redo { background: color-mix(in srgb, var(--bad) 10%, var(--panel)); }",
    // Ô "Cần tôi": việc của người, xếp theo số feature bị chặn; bấm để tới node.
    ".gd-need { margin: 0 0 16px; padding: 12px 16px; border: 1px solid var(--clash); border-radius: 8px; background: var(--panel); }",
    ".gd-need h3 { margin: 0 0 8px; font-size: 15px; }",
    ".gd-need ol { margin: 0; padding: 0; list-style: none; display: grid; gap: 2px; }",
    ".gd-need-row { display: flex; gap: 10px; align-items: baseline; width: 100%; padding: 5px 8px; border: 0; border-radius: 6px; background: none; color: var(--text); font: inherit; font-size: 14px; text-align: left; cursor: pointer; }",
    ".gd-need-row:hover { background: var(--bg); }",
    ".gd-need-row:focus-visible { outline: 2px solid var(--accent); outline-offset: 1px; }",
    ".gd-need-kind { flex: none; min-width: 84px; font-size: 12px; font-weight: 600; color: var(--clash); }",
    ".gd-need-what { flex: 1; min-width: 0; overflow-wrap: anywhere; }",
    ".gd-need-blocks { flex: none; font-size: 12.5px; color: var(--muted); white-space: nowrap; }",
    ".gd-need-blocks.hot { color: var(--bad); font-weight: 600; }",
    ".gd-need .gd-note { margin-top: 8px; }",
    // Toàn cảnh: lưới thẻ nhỏ, mỗi feature một thẻ; ô tìm dùng chung với chế độ Bảng.
    ".gd-search { display: flex; flex-wrap: wrap; gap: 8px 12px; align-items: center; margin: 0 0 12px; }",
    ".gd-search input { flex: 1; min-width: 0; max-width: 420px; padding: 7px 10px; border: 1px solid var(--line); border-radius: 6px; background: var(--bg); color: var(--text); font: inherit; }",
    ".gd-search span { color: var(--muted); font-size: 13px; }",
    ".gd-root { display: flex; flex-wrap: wrap; gap: 8px; align-items: center; margin: 0 0 12px; }",
    ".gd-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(210px, 1fr)); gap: 8px; }",
    ".gd-mini { display: flex; flex-direction: column; gap: 4px; min-width: 0; padding: 8px 10px; border: 1px solid var(--line); border-left: 5px solid var(--line); border-radius: 8px; background: var(--bg); color: var(--text); font: inherit; text-align: left; cursor: pointer; }",
    ".gd-mini:hover { border-color: var(--accent); }",
    ".gd-mini:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }",
    ".gd-mini.sel { box-shadow: 0 0 0 2px var(--accent); }",
    ".gd-mini.redo { border-left-color: var(--bad); }",
    ".gd-mini.wait { border-left-color: var(--clash); }",
    ".gd-mini.done { border-left-color: var(--ok); }",
    ".gd-mini strong { font-size: 14px; line-height: 1.3; overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }",
    ".gd-mini .gd-mini-tags { display: flex; flex-wrap: wrap; gap: 4px; font-size: 12px; }",
    ".gd-mini .gd-mini-tags span { padding: 0 6px; border-radius: 4px; background: var(--panel); border: 1px solid var(--line); }",
    ".gd-mini .gd-mini-tags .ok { color: var(--ok); }",
    ".gd-mini .gd-mini-tags .bad { color: var(--bad); }",
    ".gd-mini .gd-mini-tags .need { color: var(--clash); font-weight: 600; }",
    // Chế độ Bảng: mỗi feature một dòng.
    ".gd-frame-score { margin: 10px 0; padding: 10px 12px; border: 1px solid var(--line); border-radius: 8px; }",
    ".gd-frame-score textarea { width: 100%; margin-top: 8px; }",
    ".gd-why { margin: 6px 0; font-size: 13.5px; }",
    ".gd-ftable-wrap { overflow-x: auto; background: var(--panel); border: 1px solid var(--line); border-radius: 8px; padding: 8px 12px; }",
    ".gd-ftable { min-width: 860px; }",
    ".gd-ftable th button { padding: 0; border: 0; background: none; color: inherit; font: inherit; letter-spacing: inherit; text-transform: inherit; cursor: pointer; }",
    ".gd-ftable tbody tr { cursor: pointer; }",
    ".gd-ftable tbody tr:hover { background: var(--bg); }",
    ".gd-ftable td small { display: block; color: var(--muted); font-size: 12px; }",
    ".gd-wait-list { display: flex; flex-wrap: wrap; gap: 6px; align-items: center; margin: 8px 0; font-size: 13px; }",
    ".gd-wait-list b { font-weight: 600; margin-right: 2px; }",
    ".gd-wait-list .gd-chip { text-decoration: none; }",
    ".gd-map-legend { display: flex; flex-wrap: wrap; gap: 14px; margin: 0 0 10px; font-size: 12px; color: var(--muted); }",
    ".gd-map-legend i { display: inline-block; width: 10px; height: 10px; border-radius: 2px; margin-right: 5px; vertical-align: -1px; }",
    ".gd-chip, .gd-tag { flex: none; font: inherit; font-size: 12px; line-height: 1; padding: 4px 8px; border-radius: 10px; white-space: nowrap; border: 1px solid var(--line); background: var(--panel); color: var(--muted); }",
    ".gd-chip { cursor: pointer; color: var(--text); text-decoration: underline; }",
    ".gd-chip:hover { border-color: var(--accent); }",
    ".gd-chip.ask { border-color: var(--warn-text); background: var(--warn-bg); color: var(--warn-text); font-weight: 600; }",
    ".gd-chip.go { border-color: var(--accent); background: var(--accent); color: var(--panel); font-weight: 600; text-decoration: none; }",
    ".gd-chip.todo { border-color: var(--bad); color: var(--bad); font-weight: 600; text-decoration: none; }",
    ".gd-chip.tick { width: 24px; height: 24px; padding: 0; border-radius: 12px; font-size: 14px; line-height: 22px; text-align: center; margin: -2px 0; }",
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
    ".gd-table { overflow-x: auto; margin: 0 0 12px; }",
    ".gd-table table { border-collapse: collapse; font-size: 13.5px; }",
    ".gd-table th, .gd-table td { border: 1px solid var(--line); padding: 5px 10px; text-align: left; vertical-align: top; }",
    ".gd-table th { background: var(--bg); font-weight: 600; }",
    ".gd-more p { margin: 0 0 8px; }",
    ".gd-more { margin: 0 0 10px; max-width: 72ch; font-size: 14.5px; line-height: 1.6; color: var(--muted); }",
    ".gd-id { margin: 0 0 8px; font-size: 11.5px; color: var(--muted); }",
    ".gd-id code { font-size: 11.5px; }",
    ".gd-mark { padding: 1px 8px; border: 1px solid var(--line); border-radius: 4px; font-size: 11.5px; line-height: 1.5; color: var(--muted); white-space: nowrap; }",
    // Chữ của ý: đã chốt dùng màu chữ chính (trắng ở giao diện tối); chưa chốt mang màu của AI viết ra nó; trái nhau màu cam.
    ".gd-item .text.claude { color: var(--claude); }",
    ".gd-item .text.gpt { color: var(--gpt); }",
    ".gd-item .text.clash { color: var(--clash); }",
    // Nhóm "Chọn 1": các ý trái nhau nằm chung một khung viền cam, mỗi ý một chữ cái.
    ".gd-pick { border: 1px solid var(--clash); border-radius: 8px; padding: 2px 10px 0; margin: 8px 0; }",
    ".gd-pick .gd-item:last-child { border-bottom: 0; }",
    ".gd-pick-head { margin: 6px 0 2px; font-size: 12.5px; color: var(--muted); }",
    ".gd-pick-head b { color: var(--clash); font-weight: 700; margin-right: 8px; }",
    ".gd-pick-letter { flex: none; font-weight: 700; color: var(--clash); margin-right: 8px; }",
    ".gd-pick-btn { flex: none; margin-left: 8px; padding: 2px 8px; font: inherit; font-size: 12px; border: 1px solid var(--clash); color: var(--clash); background: transparent; border-radius: 6px; cursor: pointer; }",
    ".gd-pick-btn:disabled { opacity: .5; cursor: default; }",
    ".gd-mark.by.claude { border-color: var(--claude); color: var(--claude); }",
    ".gd-mark.by.gpt { border-color: var(--gpt); color: var(--gpt); }",
    ".gd-mark.by.clash { border-color: var(--clash); color: var(--clash); }",
    ".gd-legend { display: flex; flex-wrap: wrap; gap: 6px 14px; margin: 0 0 6px; font-size: 12.5px; color: var(--muted); }",
    ".gd-legend b { font-weight: 600; }",
    ".gd-legend .fixed { color: var(--text); } .gd-legend .claude { color: var(--claude); } .gd-legend .gpt { color: var(--gpt); } .gd-legend .clash { color: var(--clash); }",
    ".gd-source { margin: 8px 0; padding: 8px 10px; border-left: 3px solid var(--gpt); background: var(--panel); border-radius: 0 6px 6px 0; }",
    ".gd-source b { display: block; font-size: 12px; color: var(--gpt); margin-bottom: 2px; }",
    ".gd-source p { margin: 0; white-space: pre-wrap; font-size: 13.5px; line-height: 1.5; }",
    ".gd-tag.gpt { border-color: var(--gpt); color: var(--gpt); }",
    ".gd-tag.clash { border-color: var(--clash); color: var(--clash); font-weight: 600; }",
    ".gd-clash-text { color: var(--clash); font-weight: 600; }",
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
    // So với live: ý mới, ý sửa (chữ cũ gạch, chữ mới tô), ý bị bỏ; ý đổi mà bạn chưa chấm có vạch trái.
    ".gd-tag.diff, .gd-chip.diff { border-color: var(--warn-text); background: var(--warn-bg); color: var(--warn-text); font-weight: 600; text-decoration: none; }",
    ".gd-mark.change { border-color: var(--warn-text); background: var(--warn-bg); color: var(--warn-text); font-weight: 600; }",
    ".gd-item.look { box-shadow: inset 3px 0 0 var(--warn-text); padding-left: 8px; }",
    ".gd-diff { margin: 2px 0 8px; font-size: 13.5px; line-height: 1.5; color: var(--muted); }",
    ".gd-diff del { color: var(--bad); text-decoration: line-through; }",
    ".gd-diff ins { text-decoration: none; background: var(--warn-bg); color: var(--warn-text); border-radius: 3px; padding: 0 2px; }",
    ".gd-diff b { font-weight: 600; color: var(--text); margin-right: 6px; }",
    ".gd-changes { margin: 12px 0 0; padding: 8px 12px; border-radius: 6px; border-left: 3px solid var(--warn-text); background: var(--warn-bg); color: var(--warn-text); font-size: 13px; }",
    ".gd-changes p { margin: 2px 0; }",
    ".gd-changes .gd-btn { margin-top: 6px; padding: 4px 10px; font-size: 12.5px; }",
    ".gd-files { margin: 8px 0 0; color: var(--text); }",
    ".gd-files details { margin: 6px 0; background: var(--panel); border: 1px solid var(--line); border-radius: 6px; }",
    ".gd-files summary { padding: 6px 10px; cursor: pointer; font-size: 13px; }",
    ".gd-files .gd-btn { margin: 0 10px 8px; }",
    ".gd-filediff { margin: 0; padding: 6px 0; overflow-x: auto; font: inherit; font-size: 13px; line-height: 1.55; white-space: pre; }",
    ".gd-filediff div { padding: 0 10px; }",
    ".gd-filediff .del { background: color-mix(in srgb, var(--bad) 12%, transparent); color: var(--bad); }",
    ".gd-filediff .add { background: var(--warn-bg); color: var(--warn-text); }",
    ".gd-filediff .skip { color: var(--muted); font-style: italic; }",
    ".gd-filediff del { text-decoration: line-through; font-weight: 600; }",
    ".gd-filediff ins { text-decoration: none; font-weight: 700; outline: 1px solid currentColor; border-radius: 2px; }",
    ".gd-sbs { display: grid; grid-template-columns: 1fr 1fr; gap: 8px; padding: 0 10px 10px; }",
    ".gd-sbs > div { min-width: 0; }",
    ".gd-sbs b { display: block; font-size: 12px; color: var(--muted); margin-bottom: 4px; }",
    ".gd-sbs pre { margin: 0; padding: 6px 8px; border: 1px solid var(--line); border-radius: 4px; overflow: auto; max-height: 480px; font: inherit; font-size: 13px; line-height: 1.55; white-space: pre-wrap; }",
    ".gd-sbs img { max-width: 100%; border: 1px solid var(--line); border-radius: 4px; background: var(--bg); }",
    "@media (max-width: 720px) { .gd-sbs { grid-template-columns: 1fr; } }",
    ".gd-removed { margin: 8px 0 0; padding: 0; list-style: none; }",
    ".gd-removed li { padding: 6px 0; border-bottom: 1px solid var(--line); color: var(--muted); }",
    ".gd-removed li span { text-decoration: line-through; }",
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
    ".gd-parts { margin: 6px 0; font-size: 12px; }",
    ".gd-parts summary { cursor: pointer; color: var(--muted, inherit); }",
    ".gd-part { display: flex; align-items: center; gap: 8px; margin: 4px 0; }",
    ".gd-art .gd-part img { width: 56px; height: 40px; max-height: 40px; flex: none; }",
    ".gd-art img { display: block; width: 100%; max-height: 200px; object-fit: contain; background: var(--bg); border: 1px solid var(--line); border-radius: 6px; }",
    ".gd-art .gd-art-label { position: absolute; top: 6px; left: 6px; padding: 2px 8px; border-radius: 10px; font-size: 12px; font-weight: 600; background: var(--panel); color: var(--text); border: 1px solid var(--line); }",
    ".gd-art .gd-art-label.stale { background: var(--bad); color: #fff; border-color: var(--bad); }",
    ".gd-art .gd-art-label.wait { border-color: var(--accent); color: var(--accent); }",
    ".gd-art .gd-art-label.ok { background: var(--ok); color: #fff; border-color: var(--ok); }",
    ".gd-art .gd-art-anchor { position: absolute; top: 6px; right: 6px; padding: 2px 8px; border-radius: 10px; font-size: 12px; background: var(--accent); color: var(--panel); }",
    ".gd-art figcaption { font-size: 12.5px; color: var(--muted); margin-top: 4px; }",
    ".gd-art .gd-art-below { position: absolute; bottom: 6px; left: 6px; padding: 2px 8px; border-radius: 10px; font-size: 12px; font-weight: 600; background: var(--bad); color: #fff; }",
    ".gd-art .gd-inline { margin: 6px 0 0; }",
    ".gd-art .gd-art-review { margin-top: 6px; }",
    ".gd-art .gd-art-level { display: flex; justify-content: space-between; align-items: baseline; gap: 8px; font-size: 13px; }",
    ".gd-art .gd-art-saved { font-size: 12px; color: var(--muted); }",
    ".gd-art input[type=range] { width: 100%; margin: 4px 0 0; accent-color: var(--accent); }",
    // Vạch nấc: lề 8px mỗi bên để vạch nằm đúng dưới tâm nút kéo (nút rộng ~16px).
    ".gd-art .gd-art-ticks { display: flex; justify-content: space-between; padding: 0 8px; margin: 0 0 8px; }",
    ".gd-art .gd-art-tick { width: 0; display: flex; flex-direction: column; align-items: center; cursor: pointer; font-size: 11px; color: var(--muted); white-space: nowrap; }",
    ".gd-art .gd-art-tick i { display: block; width: 2px; height: 7px; background: var(--muted); border-radius: 1px; margin-bottom: 2px; }",
    ".gd-art .gd-art-tick:first-child { align-items: flex-start; }",
    ".gd-art .gd-art-tick:first-child span { margin-left: -8px; }",
    ".gd-art .gd-art-tick:last-child { align-items: flex-end; }",
    ".gd-art .gd-art-tick:last-child span { margin-right: -8px; }",
    ".gd-art .gd-art-tick.on { color: var(--text, inherit); font-weight: 600; }",
    ".gd-art .gd-art-tick.on i { background: var(--accent); height: 9px; }",
    ".gd-art .gd-note { margin: 4px 0 0; font-size: 12.5px; }",
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
    ".gd-card.dim, .gd-chips.dim, .gd-mini.dim { opacity: .28; }",
    ".gd-tag.ph { font-weight: 600; }",
    ".gd-tag.cheat { border-color: var(--warn-text); color: var(--warn-text); font-weight: 600; }",
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
    ".gd-cell.redo { border-color: var(--bad); background: transparent; color: var(--bad); }",
    ".gd-reqs { padding: 4px 0 8px; }",
    ".gd-req { display: flex; flex-wrap: wrap; gap: 6px; align-items: baseline; padding: 5px 0; border-bottom: 1px solid var(--line); }",
    ".gd-req-title { flex: 1; min-width: 200px; }",
    ".gd-req-ref { color: var(--muted); font-size: 12px; word-break: break-all; }",
    ".gd-req-more { flex-basis: 100%; color: var(--muted); font-size: 12px; padding-left: 4px; }",
    ".gd-req-state { padding: 1px 8px; border: 1px solid var(--line); border-radius: 4px; font-size: 12px; white-space: nowrap; color: var(--muted); }",
    ".gd-req-state.verified, .gd-req-state.waived { border-color: var(--ok); background: var(--ok); color: var(--panel); }",
    ".gd-req-state.done { border-color: var(--ok); color: var(--ok); }",
    ".gd-req-state.doing { border-color: var(--warn-text); background: var(--warn-bg); color: var(--warn-text); }",
    ".gd-req-state.rejected, .gd-req-state.changed, .gd-req-state.no-test { border-color: var(--bad); color: var(--bad); font-weight: 600; }",
    ".gd-gate { margin: 8px 0 0; font-size: 13.5px; }",
    ".gd-gate.ok { color: var(--ok); }",
    ".gd-gate.bad { color: var(--bad); }",
    ".gd-fold { margin: 18px 0 0; border-top: 1px solid var(--line); padding-top: 10px; }",
    ".gd-fold > summary { cursor: pointer; font-weight: 600; font-size: 15px; }",
    ".gd-fold.gd-debug > summary { color: var(--muted); font-weight: 400; font-size: 13px; }",
    ".gd-fold[open] > summary { margin-bottom: 8px; }",
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
    addStyle();      // other tabs (Studio, Tham chiếu) call it before this tab was ever opened
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
  var layout = function (root, focus, lastRound, order) {
    var placed = [], rows = 0;
    var place = function (node, parent) {
      var kids = shownChildren(node, focus, lastRound);
      if (order) kids = order(kids);
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
  // Chữ trong dòng: `code` và **đậm**; còn lại là chữ thường (không chèn HTML).
  var inline = function (ctx, node, text) {
    var rx = /`([^`]+)`|\*\*(.+?)\*\*|\[([^\]]+)\]\([^)]*\)/g, at = 0, m;
    while ((m = rx.exec(text))) {
      if (m.index > at) node.appendChild(document.createTextNode(text.slice(at, m.index)));
      if (m[1] !== undefined) node.appendChild(ctx.el("code", "", m[1]));
      else if (m[2] !== undefined) node.appendChild(inline(ctx, ctx.el("strong"), m[2]));
      else node.appendChild(document.createTextNode(m[3]));     // link: chỉ giữ chữ
      at = rx.lastIndex;
    }
    if (at < text.length) node.appendChild(document.createTextNode(text.slice(at)));
    return node;
  };
  var cells = function (line) {
    return line.trim().replace(/^\|/, "").replace(/\|$/, "").split("|").map(function (c) { return c.trim(); });
  };
  var renderText = function (ctx, box, text) {
    var lines = (text || "").split("\n"), para = [], list = null, code = null, rows = null;
    var flushTable = function () {
      if (!rows) return;
      var wrap = ctx.put(box, ctx.el("div", "gd-table"));
      var tbl = ctx.put(wrap, ctx.el("table"));
      var sep = rows.length > 1 && /^[\s|:-]+$/.test(rows[1]);
      rows.forEach(function (r, i) {
        if (sep && i === 1) return;
        var tr = ctx.put(tbl, ctx.el("tr"));
        cells(r).forEach(function (c) { ctx.put(tr, inline(ctx, ctx.el(sep && i === 0 ? "th" : "td"), c)); });
      });
      rows = null;
    };
    var flush = function () {
      flushTable();
      if (para.length) ctx.add(box, inline(ctx, ctx.el("p"), para.join(" ")));
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
        flushTable();
        if (para.length) flush();
        if (!list) list = ctx.put(box, ctx.el("ul"));
        ctx.add(list, inline(ctx, ctx.el("li"), bullet[1]));
        return;
      }
      if (/^\s*\|/.test(line)) {
        if (!rows) { flush(); rows = []; }
        rows.push(line);
        return;
      }
      flushTable();
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
    // view: "tree" (bản đồ) | "table" (mỗi feature một dòng); expandAll: bản đồ cả cây, mọi vòng; query: chữ lọc
    // feature (lưới và bảng); needAll: ô "Cần tôi" hiện mọi dòng; sort: cột đang xếp của bảng.
    var state = { table: "live", focus: null, selected: null, message: null, ask: null, phase: null, onlyChanged: false,
                  view: "tree", expandAll: false, query: "", needAll: false, sort: null,
                  filter: { due: "all", review: "all", code: "all" } };
    // Chỉ là tiện lợi của người xem (trình duyệt này): thiếu hay lỗi thì trang vẫn chạy với mặc định.
    var PREFS_KEY = "coresimple.gamedesign.view";
    try {
      var saved = JSON.parse(window.localStorage.getItem(PREFS_KEY) || "null");
      if (saved && typeof saved === "object") {
        if (saved.table === "idea" || saved.table === "live") state.table = saved.table;
        if (saved.view === "table") state.view = "table";
        state.expandAll = saved.expandAll === true;
        state.query = typeof saved.query === "string" ? saved.query : "";
        state.focus = typeof saved.focus === "string" ? saved.focus : null;
        state.selected = typeof saved.selected === "string" ? saved.selected : null;
        ["due", "review", "code"].forEach(function (k) {
          if (saved.filter && typeof saved.filter[k] === "string") state.filter[k] = saved.filter[k];
        });
      }
    } catch (e) { /* không có localStorage */ }
    var savePrefs = function () {
      try {
        window.localStorage.setItem(PREFS_KEY, JSON.stringify({ table: state.table, view: state.view, expandAll: state.expandAll,
                                                               query: state.query, focus: state.focus, selected: state.selected,
                                                               filter: state.filter }));
      } catch (e) { /* không có localStorage */ }
    };

    add(pane, el("h1", "", "Game design"));
    add(pane, el("p", "sub", "Thiết kế từ ý tưởng sơ khai tới quyết định chi tiết. Ô \"Cần tôi\" gom việc của bạn, việc chặn nhiều feature nhất lên đầu. " +
      "Toàn cảnh là lưới feature (vòng " + FOCUS_ROUND + "): bấm một feature để mở nhánh của nó tới vòng cuối, bấm một node để xem, chấm và ghi ý kiến bên dưới. " +
      "Chế độ Bảng: mỗi feature một dòng."));
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
    // Số nhóm ý trái nhau (các ý nối nhau qua it.clash) mà người chưa giải quyết. Một nhóm xong khi người đã chấm
    // mọi ý của nó và nhiều nhất một ý đạt; chưa chấm đủ, hoặc từ hai ý cùng đạt, là còn xung đột.
    var openClashes = function (node) {
      var byId = {}, seen = {}, open = 0;
      node.items.forEach(function (it) { byId[it.id] = it; });
      node.items.forEach(function (it) {
        if (!(it.clash && it.clash.length) || seen[it.id]) return;
        var ids = [], stack = [it.id];
        while (stack.length) {
          var id = stack.pop();
          if (seen[id] || !byId[id]) continue;
          seen[id] = true;
          ids.push(id);
          (byId[id].clash || []).forEach(function (x) { stack.push(x); });
        }
        if (ids.length < 2) return;
        var scored = ids.filter(function (x) { return byId[x].user !== null; }).length;
        var kept = ids.filter(function (x) { return byId[x].user !== null && byId[x].user >= passMin(); }).length;
        if (scored < ids.length || kept > 1) open++;
      });
      return open;
    };
    // Trạng thái duyệt, chỉ để hiển thị (tính lại mỗi lần vẽ, không lưu, không ảnh hưởng tới khoá hay Sync).
    // Của node: "ok" (đạt và giống live), "redo" (việc của @GameDesigner), "todo" (chờ bạn chấm, trả lời hoặc chốt).
    // "ui": chưa đạt chỉ vì frame (việc của @UIDesigner: thiếu, cũ so với ui.md, bị chấm thấp).
    var uiOwed = function (n) { return !!(n.uiWait && n.uiWait.length); };
    // "follow": đạt, khác live chỉ ở dòng refs/based-on vì một node nó tham chiếu đã đổi (gamedesign.follows). Không phải
    // việc của bạn: nó tự chốt khi node gốc được chốt.
    var own = function (n) { return !n.passed ? (n.redo ? "redo" : uiOwed(n) ? "ui" : "todo") : n.state === "same" ? "ok" : n.follow ? "follow" : "todo"; };
    var shortPath = function (path, max) { return !path ? "(gốc)" : path.length > max ? "…" + path.slice(path.length - max + 1) : path; };
    var copyText = function (text, what) {
      var done = function () { state.message = { bad: false, text: "Đã chép " + what + ": " + text }; renderNotice(); };
      if (navigator.clipboard && navigator.clipboard.writeText) navigator.clipboard.writeText(text).then(done, function () { say("Không chép được; id: " + text); });
      else say("Trình duyệt không cho chép; id: " + text);
    };
    // Của nhánh: đếm mọi node bên dưới (kể cả node đang ẩn trên bản đồ) theo trạng thái của chúng.
    var branch = function (n) {
      var out = { total: 0, todo: 0, redo: 0, ui: 0, follow: 0 };
      var visit = function (c) {
        out.total++;
        var st = own(c);
        if (st !== "ok") out[st]++;
        c.children.forEach(visit);
      };
      n.children.forEach(visit);
      return out;
    };
    // Tên đầy đủ của một node: "Feature › Node con › Lá".
    var trail = function (n) {
      var names = [], at = data ? data.idea : null, parts = n.path ? n.path.split("/") : [];
      parts.forEach(function (key) {
        at = at ? at.children.filter(function (c) { return c.key === key; })[0] : null;
        names.push(at ? at.title || key : key);
      });
      return names.length ? names.join(" › ") : (n.title || "(gốc)");
    };
    // Tới một node của bảng đề xuất: mở nhánh feature của nó rồi chọn nó.
    var goTo = function (n) {
      state.table = "idea";
      state.view = "tree";
      state.focus = n.path ? n.path.split("/")[0] : null;
      state.selected = n.path;
      state.message = null;
      state.ask = null;
      render();
    };
    // ---------------------------------------------------------------- feature: hàng của bảng, khoá, "Cần tôi"
    // data.features (gamedesign.feature_rows): mỗi thư mục code của mỗi feature một hàng.
    var GLOSSARY = "glossary";
    var featureRows = function () { return data && Array.isArray(data.features) ? data.features : []; };
    var rowsOf = function (key) { return featureRows().filter(function (r) { return r.key === key; }); };
    var gateOpen = function (key) { return rowsOf(key).some(function (r) { return r.gate.ok; }); };
    // Feature còn khoá code (không thư mục nào của nó được code): thứ một việc của người có thể đang chặn.
    var lockedKeys = function () {
      var out = {};
      featureRows().forEach(function (r) { if (r.key !== GLOSSARY && !gateOpen(r.key)) out[r.key] = true; });
      return out;
    };
    var plain = function (text) {
      return String(text || "").toLowerCase().normalize("NFD").replace(/[\u0300-\u036f]/g, "").replace(/đ/g, "d");
    };
    // Feature khớp chữ lọc: theo tên, key hoặc thư mục code.
    var matches = function (key, title) {
      var q = plain(state.query).trim();
      if (!q) return true;
      var words = [key, title].concat(rowsOf(key).map(function (r) { return r.unity; }));
      return words.some(function (w) { return plain(w).indexOf(q) >= 0; });
    };
    var NEED_KINDS = [
      { key: "clash", label: "Chọn ý" }, { key: "ask", label: "Trả lời" }, { key: "score", label: "Chấm ý" },
      { key: "lock", label: "Chốt" }, { key: "art", label: "Duyệt ảnh" }
    ];
    // Mọi việc của người: [{kind, node, feature, text, blocks, table}], việc chặn nhiều feature (đang khoá code) nhất
    // lên đầu. Một node một dòng, theo việc đứng trước trong NEED_KINDS. "Chặn": feature của node, cộng các feature
    // có node refs trỏ vào node này, vào node cha hay node con của nó; việc ở gốc chặn mọi feature đang khoá.
    var needsMe = function () {
      if (!data || !data.idea) return [];
      var locked = lockedKeys(), refsOf = {};
      data.idea.children.forEach(function (f) {
        var list = refsOf[f.key] = [];
        each(f, function (n) { (n.refs || []).forEach(function (r) { if (r.path) list.push(r.path); }); });
      });
      var blocking = function (path) {
        var own = path ? path.split("/")[0] : "", hit = {};
        Object.keys(locked).forEach(function (k) {
          if (!path || k === own) { hit[k] = true; return; }
          if (refsOf[k] && refsOf[k].some(function (r) { return r === path || path.indexOf(r + "/") === 0 || r.indexOf(path + "/") === 0; })) hit[k] = true;
        });
        return Object.keys(hit).length;
      };
      var out = [];
      var push = function (kind, node, text, tableKey) {
        out.push({ kind: kind, node: node, feature: node.path ? node.path.split("/")[0] : "", text: text,
                   blocks: blocking(node.path), table: tableKey || "idea" });
      };
      var lockable = {};
      syncList().forEach(function (n) { lockable[n.path] = true; });
      each(data.idea, function (n) {
        var clash = openClashes(n);
        if (clash) push("clash", n, clash + " nhóm ý trái nhau");
        else if (n.pending) push("ask", n, n.pending + " câu hỏi");
        else if (own(n) === "todo" && !n.passed) push("score", n, (n.reasons || [])[0] || "chờ bạn chấm");
        else if (lockable[n.path] && !(n.follow && (n.followWait || []).length)) {
          // Node chỉ theo ref không phải việc của bạn khi node gốc còn chờ chốt: nó đi theo node gốc.
          var pulled = n.followers ? n.followers.ready.length : 0;
          push("lock", n, "đạt, chờ chốt · " + (n.follow ? "chỉ đổi dòng refs, node gốc đã chốt" : diffShort(n)) + (pulled ? " · kéo theo " + pulled + " node" : ""));
        }
      });
      if (data.live) {
        each(data.live, function (n) {
          var seen = {}, count = 0;
          (n.art || []).forEach(function (a) {
            var k = (a.tool || "codex") + "/" + a.frame;
            if (seen[k]) return;
            seen[k] = true;
            if (a.reviewable && a.level === null && !a.stale.length) count++;
          });
          if (count) push("art", n, count + " ảnh concept chưa duyệt", "live");
        });
      }
      var order = NEED_KINDS.map(function (k) { return k.key; });
      out.sort(function (a, b) {
        return b.blocks - a.blocks || order.indexOf(a.kind) - order.indexOf(b.kind) || (a.node.path < b.node.path ? -1 : a.node.path > b.node.path ? 1 : 0);
      });
      return out;
    };
    var needCount = function (needs, key) { return needs.filter(function (x) { return x.feature === key; }).length; };
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
    // Ba bộ lọc thêm (Hạn, Duyệt, Code): mỗi bộ một lựa chọn, kết hợp bằng "và". Node không khớp bị làm mờ; node cha
    // vẫn sáng khi bên dưới có node khớp, để còn thấy đường tới nó.
    var reqItems = function (n) { return n.reqs && n.reqs.items ? n.reqs.items.filter(function (r) { return !r.later; }) : []; };
    var hasState = function (n, states) { return reqItems(n).some(function (r) { return states.indexOf(r.state) >= 0; }); };
    var FILTERS = [
      { key: "due", name: "Hạn", hint: "Theo giai đoạn đang làm của project", options: [
        ["all", "Tất cả", null],
        ["due", "Tới hạn", function (n) { return n.due !== false; }],
        ["later", "Để sau", function (n) { return n.due === false; }],
        ["deepen", "Cần đào tới vòng 4", function (n) { return !!n.deepen; }]] },
      { key: "review", name: "Duyệt", hint: "Theo trạng thái duyệt của node (bảng đề xuất)", options: [
        ["all", "Tất cả", null],
        ["root", "Thay đổi gốc", function (n) { return n.passed && n.state !== "same" && !n.follow; }],
        ["follow", "Theo ref", function (n) { return own(n) === "follow"; }],
        ["redo", "Cần làm lại", function (n) { return own(n) === "redo"; }],
        ["score", "Chờ tôi chấm", function (n) { return own(n) === "todo" && !n.passed; }],
        ["ui", "Chờ @UIDesigner", function (n) { return own(n) === "ui"; }],
        ["ok", "Đã duyệt", function (n) { return own(n) === "ok"; }]] },
      { key: "code", name: "Code", hint: "Theo sổ yêu cầu của node lá và cổng code của feature", options: [
        ["all", "Tất cả", null],
        ["open", "Còn việc", function (n) { return hasState(n, ["todo", "doing", "rejected", "changed", "no-test"]); }],
        ["doing", "Đang làm", function (n) { return hasState(n, ["doing"]); }],
        ["done", "Chờ kiểm", function (n) { return hasState(n, ["done"]); }],
        ["back", "Bị trả hoặc đổi thiết kế", function (n) { return hasState(n, ["rejected", "changed", "no-test"]); }],
        ["met", "Xong", function (n) { return reqItems(n).length > 0 && !hasState(n, ["todo", "doing", "rejected", "changed", "no-test", "done"]); }],
        ["later", "Có việc để sau", function (n) { return !!(n.reqs && n.reqs.later); }],
        ["locked", "Khoá code", function (n) { return (n.codeGate || []).some(function (g) { return !g.ok; }); }]] }
    ];
    var filterTest = function (f, value) {
      var opt = f.options.filter(function (o) { return o[0] === value; })[0];
      return opt ? opt[2] : null;
    };
    var filtering = function () { return FILTERS.some(function (f) { return !!filterTest(f, state.filter[f.key]); }); };
    var fits = function (n) {
      return FILTERS.every(function (f) { var t = filterTest(f, state.filter[f.key]); return !t || t(n); });
    };
    var fitsBelow = function (n) { return fits(n) || n.children.some(fitsBelow); };
    // Thứ tự hiện: node tới hạn và đang sáng lên trước (giai đoạn sớm trước), rồi node để sau, cuối cùng là node bị làm
    // mờ. Cùng nhóm thì giữ thứ tự trong cây.
    var dueGroup = function (n) { return !n ? 0 : !needed(n) ? 2 : n.due === false ? 1 : 0; };
    var dueFirst = function (nodes) {
      var group = dueGroup;
      var early = function (n) { var r = rank(n), t = r.top !== null && r.top !== undefined ? r.top : r.effective; return t === null || t === undefined ? 1 : -t; };
      return nodes.map(function (n, i) { return { n: n, i: i, g: group(n), e: early(n) }; })
        .sort(function (a, b) { return a.g - b.g || a.e - b.e || a.i - b.i; })
        .map(function (x) { return x.n; });
    };
    var needed = function (node) {
      var p = state.phase ? phase(state.phase) : null;
      if (p) {      // điểm cao nhất bên dưới, kể cả lá đã xong: nhánh đã làm xong vẫn thuộc giai đoạn của nó
        var r = rank(node), e = r.top !== null && r.top !== undefined ? r.top : r.effective;
        if (!(e !== null && e !== undefined && e >= p.min)) return false;
      }
      return !filtering() || fitsBelow(node);
    };
    // So với live (bảng đề xuất): gamedesign.py tính sẵn n.diff và it.change ("new" | "rewritten" | "same").
    var diffOf = function (n) {
      var d = n.diff || {};
      return { added: d.added || 0, rewritten: d.rewritten || 0, removed: (d.removed || []).length,
               title: d.title !== null && d.title !== undefined, summary: d.summary !== null && d.summary !== undefined, frames: !!d.frames };
    };
    var changedItems = function (n) { return n.items.filter(function (i) { return i.change && i.change !== "same"; }); };
    // Ý đổi so với live mà bạn chưa chấm bản hiện tại: thứ nên xem trước.
    var toLook = function (n) { return changedItems(n).filter(function (i) { return i.user === null; }).length; };
    var diffParts = function (n) {
      if (n.state === "new") return ["node mới, chưa có trong live"];
      var d = diffOf(n), out = [];
      if (d.added) out.push(d.added + " ý mới");
      if (d.rewritten) out.push(d.rewritten + " ý sửa");
      if (d.removed) out.push(d.removed + " ý bỏ");
      if (d.title) out.push("đổi tên");
      if (d.summary) out.push("đổi tóm tắt");
      if (d.frames) out.push("đổi frame");
      var ai = n.items.filter(function (i) { return i.liveAi !== null && i.liveAi !== undefined; }).length;
      if (ai) out.push("điểm AI của " + ai + " ý");
      var word = { added: "thêm", removed: "bỏ", changed: "sửa" };
      ((n.diff || {}).files || []).forEach(function (f) {
        if (f.name === "data.md" && (out.length || ai)) return;      // đã kể qua các ý, tên, tóm tắt, điểm
        out.push(f.name + " " + word[f.status]);
      });
      return out;
    };
    var diffShort = function (n) {
      if (n.state === "new") return "Mới";
      var d = diffOf(n), out = [];
      if (d.added) out.push("+" + d.added);
      if (d.rewritten) out.push("✎" + d.rewritten);
      if (d.removed) out.push("−" + d.removed);
      if (d.title || d.summary || d.frames || !out.length) out.push("…");
      return "Đổi " + out.join(" ");
    };
    // So sánh theo từ (LCS): [{op: "=" | "-" | "+", text}]. Chữ của một ý ngắn nên đủ nhanh.
    var wordDiff = function (a, b) {
      var x = (a || "").split(/(\s+)/), y = (b || "").split(/(\s+)/), n = x.length, m = y.length, i, j;
      if (n * m > 250000) return [{ op: "-", text: a }, { op: "+", text: b }];
      var t = [];
      for (i = 0; i <= n; i++) { t.push(new Array(m + 1).fill(0)); }
      for (i = n - 1; i >= 0; i--) for (j = m - 1; j >= 0; j--) t[i][j] = x[i] === y[j] ? t[i + 1][j + 1] + 1 : Math.max(t[i + 1][j], t[i][j + 1]);
      var out = [], push = function (op, text) {
        var last = out[out.length - 1];
        if (last && last.op === op) last.text += text; else out.push({ op: op, text: text });
      };
      i = 0; j = 0;
      while (i < n && j < m) {
        if (x[i] === y[j]) { push("=", x[i]); i++; j++; }
        else if (t[i + 1][j] >= t[i][j + 1]) { push("-", x[i]); i++; }
        else { push("+", y[j]); j++; }
      }
      for (; i < n; i++) push("-", x[i]);
      for (; j < m; j++) push("+", y[j]);
      return out;
    };
    // So theo dòng: [{op, text}] mỗi phần tử một dòng.
    var lineOps = function (a, b) {
      var x = a === null || a === undefined ? [] : a.split("\n"), y = b === null || b === undefined ? [] : b.split("\n");
      var n = x.length, m = y.length, i, j, t = [];
      if (n * m > 4000000) return x.map(function (l) { return { op: "-", text: l }; }).concat(y.map(function (l) { return { op: "+", text: l }; }));
      for (i = 0; i <= n; i++) t.push(new Int32Array(m + 1));
      for (i = n - 1; i >= 0; i--) for (j = m - 1; j >= 0; j--) t[i][j] = x[i] === y[j] ? t[i + 1][j + 1] + 1 : Math.max(t[i + 1][j], t[i][j + 1]);
      var out = [];
      i = 0; j = 0;
      while (i < n && j < m) {
        if (x[i] === y[j]) { out.push({ op: "=", text: x[i] }); i++; j++; }
        else if (t[i + 1][j] >= t[i][j + 1]) { out.push({ op: "-", text: x[i] }); i++; }
        else { out.push({ op: "+", text: y[j] }); j++; }
      }
      for (; i < n; i++) out.push({ op: "-", text: x[i] });
      for (; j < m; j++) out.push({ op: "+", text: y[j] });
      return out;
    };
    // Khung so dòng: dòng bỏ đỏ (−), dòng thêm vàng (+), một khối bỏ đứng ngay trước khối thêm cùng số dòng thì tô
    // từng chữ; dòng giống nhau chỉ giữ 2 dòng quanh chỗ đổi.
    var lineDiff = function (box, before, after) {
      var ops = lineOps(before, after), pre = put(box, el("div", "gd-filediff")), CONTEXT = 2;
      var near = ops.map(function () { return false; });
      ops.forEach(function (o, k) {
        if (o.op === "=") return;
        for (var q = Math.max(0, k - CONTEXT); q <= Math.min(ops.length - 1, k + CONTEXT); q++) near[q] = true;
      });
      var row = function (o, parts) {
        var line = put(pre, el("div", o.op === "-" ? "del" : o.op === "+" ? "add" : ""));
        line.appendChild(document.createTextNode((o.op === "=" ? "  " : o.op === "-" ? "− " : "+ ")));
        if (!parts) { line.appendChild(document.createTextNode(o.text)); return; }
        parts.forEach(function (p) {
          if (p.op === "=") line.appendChild(document.createTextNode(p.text));
          else if (p.op === (o.op === "-" ? "-" : "+")) add(line, el(o.op === "-" ? "del" : "ins", "", p.text));
        });
      };
      var k = 0;
      while (k < ops.length) {
        if (ops[k].op === "=" && !near[k]) {
          var start = k;
          while (k < ops.length && ops[k].op === "=" && !near[k]) k++;
          add(pre, el("div", "skip", "  … " + (k - start) + " dòng giống …"));
          continue;
        }
        if (ops[k].op === "-") {
          var dels = [], adds = [];
          while (k < ops.length && ops[k].op === "-") dels.push(ops[k++]);
          while (k < ops.length && ops[k].op === "+") adds.push(ops[k++]);
          var paired = dels.length === adds.length;
          var words = paired ? dels.map(function (d, q) { return wordDiff(d.text, adds[q].text); }) : null;
          dels.forEach(function (d, q) { row(d, words ? words[q] : null); });
          adds.forEach(function (a, q) { row(a, words ? words[q] : null); });
          continue;
        }
        row(ops[k]);
        k++;
      }
      return pre;
    };
    var IMAGE_RX = /\.(png|jpe?g|gif|webp|svg)$/i;
    // Mỗi file của node khác live: một khung so sánh. File chữ: so dòng, và nút xem hai bản cạnh nhau. Ảnh: hai ảnh.
    var renderFiles = function (box, node) {
      var files = (node.diff || {}).files || [];
      if (!files.length) return;
      var wrap = put(box, el("div", "gd-files"));
      var word = { added: "thêm mới (live chưa có)", removed: "bỏ (live còn)", changed: "sửa" };
      var src = function (key, name) {
        var t = TABLES.filter(function (x) { return x.key === key; })[0];
        return t.dir + (node.path ? node.path + "/" : "") + name.split("/").map(encodeURIComponent).join("/");
      };
      files.forEach(function (f) {
        var det = put(wrap, el("details"));
        det.open = f.name !== "data.md";
        add(det, el("summary", "", f.name + " · " + word[f.status] + (f.name === "data.md" ? " (toàn văn; các ý đã so ở dưới)" : "")));
        var text = f.live !== null || f.idea !== null;
        if (text) {
          lineDiff(det, f.live, f.idea);
          var sbs = null;
          var both = put(det, button("Xem hai bản cạnh nhau", function () {
            if (sbs) { sbs.hidden = !sbs.hidden; return; }
            sbs = put(det, el("div", "gd-sbs"));
            [["Live", f.live], ["Đề xuất", f.idea]].forEach(function (side) {
              var col = put(sbs, el("div"));
              add(col, el("b", "", side[0]), el("pre", "", side[1] === null ? "(không có file)" : side[1]));
            });
          }));
          both.className = "gd-btn";
        } else if (IMAGE_RX.test(f.name)) {
          var pics = put(det, el("div", "gd-sbs"));
          [["Live", "live", f.status !== "added"], ["Đề xuất", "idea", f.status !== "removed"]].forEach(function (side) {
            var col = put(pics, el("div"));
            add(col, el("b", "", side[0]));
            if (!side[2]) { add(col, el("p", "muted", "(không có ảnh)")); return; }
            var img = put(col, el("img"));
            img.src = src(side[1], f.name);
            img.alt = side[0] + ": " + f.name;
            img.loading = "lazy";
          });
        } else {
          add(det, el("p", "gd-note", "File nhị phân: nội dung đổi, không so được bằng chữ."));
        }
      });
    };
    var diffLine = function (box, label, before, after) {
      var line = put(box, el("p", "gd-diff"));
      add(line, el("b", "", label));
      wordDiff(before, after).forEach(function (part) {
        line.appendChild(part.op === "=" ? document.createTextNode(part.text) : el(part.op === "-" ? "del" : "ins", "", part.text));
      });
      return line;
    };
    // Ý bạn đã chấm dưới mức đạt (dùng cho thẻ ở bảng live).
    var turnedDown = function (node) { return node.items.filter(function (i) { return i.user !== null && i.user < passMin(); }).length; };
    // Node đề xuất đang đạt mà khác live: thứ nút Sync sẽ chép, đi từ gốc xuống và dừng ở node chưa đạt.
    var syncList = function () {
      var out = [];
      var visit = function (node) {
        if (!node.passed) return;
        if (node.state !== "same") out.push(node);
        node.children.forEach(visit);
      };
      if (data && data.idea) visit(data.idea);
      return out;
    };
    var syncable = function () { return syncList().length; };
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
        function (res) {
          var f = res.followed || [];
          return "Đã chốt sang live: " + res.written.join(", ") + (f.length ? ". Tự chốt theo (chỉ đổi dòng refs): " + f.length + " node: " +
            f.map(function (x) { return x.path; }).join(", ") : "");
        });
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

      var views = group("Xem", "Cây: bản đồ node. Bảng: mỗi feature một dòng (vòng, cổng code, yêu cầu, đang chờ ai).");
      add(views, button("Cây", function () { state.view = "tree"; render(); }, state.view === "tree"),
          button("Bảng", function () { state.view = "table"; render(); }, state.view === "table"));

      var map = group("Bản đồ");
      var whole = put(map, button("Tất cả feature", function () { state.focus = null; state.expandAll = false; state.view = "tree"; render(); }));
      whole.title = "Về toàn cảnh: mỗi feature một thẻ nhỏ; bấm một thẻ để mở nhánh của nó";
      whole.disabled = state.view === "tree" && state.focus === null && !state.expandAll;
      var open = put(map, button("Mở tất cả", function () { state.expandAll = !state.expandAll; state.focus = null; state.view = "tree"; render(); },
                                 state.expandAll && state.focus === null));
      open.title = "Vẽ cả cây, mọi feature tới vòng cuối (chậm và dài khi có nhiều feature)";
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

        var stageNow = data.stage ? data.stage.stage : "";
        var stageBox = group("Đang làm 🔒", "Giai đoạn project đang làm. Chỉ node của giai đoạn này và trước đó phải thiết kế tới vòng 4 " +
          "và là việc dev; node của giai đoạn sau chỉ định nghĩa để đó. Đổi cần PIN." + (data.stage && data.stage.at ? " Đặt lúc " + data.stage.at + "." : ""));
        (data.stages || []).forEach(function (key) {
          var ph = phase(key) || { name: key, label: "" };
          var b = put(stageBox, button(ph.name, function () { if (key !== stageNow) ask({ kind: "stage", key: key }); }, stageNow === key));
          b.title = ph.label + (ctx.setup ? ". Đặt giai đoạn đang làm cần PIN." : ". Muốn đặt: mở trang qua setup_server.py.");
          if (!ctx.setup && stageNow !== key) b.disabled = true;
        });
        if (!stageNow) add(stageBox, el("span", "bad-text", "chưa đặt"));

        var modes = group("Mode 🔒", "Cách @GameDesigner làm việc. Đổi mode cần PIN.");
        MODES.forEach(function (m) {
          var b = put(modes, button(m.label, function () { if (m.key !== mode()) ask({ kind: "mode", key: m.key }); }, mode() === m.key));
          b.title = m.hint + (ctx.setup ? " Đổi mode cần PIN." : " Muốn đổi mode: mở trang qua setup_server.py.");
          if (!ctx.setup && mode() !== m.key) b.disabled = true;
        });
      }
      if (data && tree()) {
        var all = [], hit = 0;
        each(tree(), function (n) { all.push(n); });
        var filterBox = group("Lọc", "Làm mờ node không khớp. Mỗi bộ chọn một giá trị, kết hợp bằng \"và\"; node cha vẫn sáng khi bên dưới có node khớp.");
        FILTERS.forEach(function (f) {
          if (f.key === "review" && state.table !== "idea") return;
          add(filterBox, el("span", "gd-group-name", f.name));
          var sel = put(filterBox, el("select", state.filter[f.key] !== "all" ? "on" : ""));
          sel.title = f.hint;
          sel.setAttribute("aria-label", "Lọc theo " + f.name);
          f.options.forEach(function (o) {
            var count = o[2] ? all.filter(o[2]).length : all.length;
            var opt = put(sel, el("option", "", o[1] + " (" + count + ")"));
            opt.value = o[0];
            if (state.filter[f.key] === o[0]) opt.selected = true;
          });
          sel.addEventListener("change", function () { state.filter[f.key] = sel.value; render(); });
        });
        if (filtering()) {
          all.forEach(function (n) { if (fits(n)) hit++; });
          add(filterBox, el("span", "muted", "khớp " + hit + "/" + all.length + " node"));
          put(filterBox, button("Bỏ lọc", function () { state.filter = { due: "all", review: "all", code: "all" }; render(); }));
        }
      }
      add(bar, el("span", "gd-time", data && data.generatedAt ? "Đọc lúc " + data.generatedAt.replace("T", " ") : ""));
    };

    var renderNotice = function () {
      noticeBox.textContent = "";
      if (state.ask && ctx.setup) {
        // Một khung xác nhận dùng chung cho mọi nút cần PIN: nói rõ sẽ làm gì, rồi mới hỏi PIN.
        var target = state.ask.kind === "mode" ? MODES.filter(function (m) { return m.key === state.ask.key; })[0] : null;
        var stageTo = state.ask.kind === "stage" ? (phase(state.ask.key) || { key: state.ask.key, name: state.ask.key, label: "" }) : null;
        var what = stageTo ? "Đặt giai đoạn đang làm: " + stageTo.name + " (" + stageTo.label + "). Node của giai đoạn này và trước đó phải thiết kế " +
            "tới vòng 4 và thành việc dev; node của giai đoạn sau chỉ định nghĩa để đó, yêu cầu code của chúng ghi \"để sau\"."
          : target ? "Đổi sang " + target.label + ": " + target.hint
          : "Sync: chép " + syncable() + " node đang đạt sang live (bảng đã chốt), ghi đè bản cũ của các node đó: " +
            syncList().map(trail).join(" · ") + ".";
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
          if (stageTo) {
            send("/api/gamedesign/stage", { pin: pin.value, stage: stageTo.key }, function () {
              state.ask = null;
              return "Đang làm: " + stageTo.name + ".";
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
        add(confirmBox, button(stageTo ? "Đặt giai đoạn" : target ? "Đổi mode" : "Sync", go, null, "gd-btn main"), button("Thôi", function () { state.ask = null; render(); }));
      }
      if (state.message) add(noticeBox, el("p", state.message.bad ? "bad-text" : "ok-text", state.message.text));
      if (data && data.stages && !(data.stage && data.stage.stage)) {
        add(noticeBox, el("p", "bad-text", "Chưa đặt giai đoạn đang làm (nhóm \"Đang làm\" ở thanh trên): @Manager chưa giao được việc dev cho " +
          "feature, và mọi node đang được coi là phải thiết kế tới vòng 4."));
      }
      renderNeeds();
      // Mọi node khác live mà chưa đạt: AI đã đổi, đang chờ bạn chấm hoặc chờ @GameDesigner làm lại. Gập sẵn: việc
      // của bạn trong đó đã có ở ô "Cần tôi".
      // Thay đổi gốc: node đạt, khác live vì chính nó đổi (không phải vì ghim lại ref). Đây là thứ bạn cần xem; node chỉ
      // theo ref không nằm ở đây và tự chốt khi node gốc của nó được chốt.
      if (state.table === "idea" && data && data.idea) {
        var rootsChanged = [], followCount = 0;
        each(data.idea, function (n) {
          if (n.state !== "same" && n.passed) { if (n.follow) followCount++; else rootsChanged.push(n); }
        });
        if (rootsChanged.length || followCount) {
          var rootFold = put(noticeBox, el("details", "gd-fold"));
          rootFold.open = true;
          add(rootFold, el("summary", "", "Thay đổi gốc chờ bạn chốt (" + rootsChanged.length + ")" + (followCount ? " · " + followCount + " node chỉ theo ref, tự chốt theo" : "")));
          var rootBox = put(rootFold, el("div", "gd-wait-list"));
          rootsChanged.forEach(function (n) {
            var f = n.followers || { ready: [], redo: [] };
            var link = put(rootBox, button((n.path || "(gốc)") + " · " + diffShort(n) + (f.ready.length ? " · kéo theo " + f.ready.length : "") +
              (f.redo.length ? " · " + f.redo.length + " chờ rà" : ""), function () { goTo(n); }, null, "gd-chip diff"));
            link.title = trail(n) + "\nSo với live: " + diffParts(n).join(", ") + "." +
              (f.ready.length ? "\nTự chốt theo node này: " + f.ready.join(", ") : "") + (f.redo.length ? "\nCòn ghim hash cũ: " + f.redo.join(", ") : "");
          });
          if (!rootsChanged.length) add(rootBox, el("p", "gd-note", "Mọi node gốc đã chốt: bấm ✓ ở một node \"Theo ref\" để chốt cả nhóm."));
        }
      }
      if (state.table === "idea" && data && data.idea) {
        var drift = [];
        each(data.idea, function (n) { if (n.state !== "same" && !n.passed) drift.push(n); });
        if (drift.length) {
          var driftFold = put(noticeBox, el("details", "gd-fold"));
          add(driftFold, el("summary", "", "Đổi so với live, chưa đạt (" + drift.length + ")"));
          var driftBox = put(driftFold, el("div", "gd-wait-list"));
          drift.forEach(function (n) {
            var look = toLook(n), redo = own(n) === "redo";
            var link = put(driftBox, button(trail(n) + " · " + diffShort(n) + (look ? " · " + look + " chưa chấm" : "") + (redo ? " · AI làm lại" : ""),
              function () { goTo(n); }, null, "gd-chip diff"));
            link.title = "So với live: " + diffParts(n).join(", ") + ".\n" + (redo ? "Đang chờ @GameDesigner làm lại: " : "Đang chờ bạn: ") + n.reasons.join("\n");
          });
        }
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

    // Ô "Cần tôi": mọi việc của bạn ở một chỗ, việc chặn nhiều feature nhất lên đầu; bấm một dòng để tới node.
    var NEED_SHOWN = 10;
    var renderNeeds = function () {
      var needs = needsMe();
      if (!needs.length) return;
      var box = put(noticeBox, el("div", "gd-need"));
      add(box, el("h3", "", "Cần tôi (" + needs.length + ")"));
      var list = put(box, el("ol"));
      var names = {};
      NEED_KINDS.forEach(function (k) { names[k.key] = k.label; });
      (state.needAll ? needs : needs.slice(0, NEED_SHOWN)).forEach(function (x) {
        var li = put(list, el("li"));
        var row = put(li, button("", function () {
          if (x.table === "live") {
            state.table = "live"; state.view = "tree"; state.focus = x.feature || null; state.selected = x.node.path; state.message = null; render();
          } else goTo(x.node);
          detailBox.scrollIntoView({ behavior: "smooth", block: "start" });
        }, null, "gd-need-row"));
        add(row, el("span", "gd-need-kind", names[x.kind]), el("span", "gd-need-what", trail(x.node) + " · " + x.text),
            el("span", "gd-need-blocks" + (x.blocks ? " hot" : ""), x.blocks ? "chặn " + x.blocks + " feature" : "không chặn code"));
        row.title = "Bấm để tới node " + where(x.node);
      });
      if (needs.length > NEED_SHOWN) {
        add(box, button(state.needAll ? "Thu gọn" : "Xem cả " + needs.length, function () { state.needAll = !state.needAll; render(); }, null, "gd-link"));
      }
      add(box, el("p", "gd-note", "\"Chặn\": số feature còn khoá code mà việc này đứng trước (feature của node, và feature có refs trỏ vào node). " +
        "Bỏ qua yêu cầu của sổ yêu cầu không có ở đây: bạn tự quyết ở node lá, trong mục Yêu cầu."));
    };

    // Ô tìm feature: dùng chung cho lưới và bảng; gõ chỉ vẽ lại phần danh sách, không mất con trỏ.
    var searchBox = function (box, count, refill) {
      var line = put(box, el("div", "gd-search"));
      var input = put(line, el("input"));
      input.type = "search";
      input.placeholder = "Tìm feature (tên, key, thư mục code)";
      input.setAttribute("aria-label", "Tìm feature");
      input.value = state.query;
      var shown = put(line, el("span", "", ""));
      var update = function () { shown.textContent = count(); };
      input.addEventListener("input", function () { state.query = input.value || ""; savePrefs(); refill(); update(); });
      update();
      return input;
    };

    // Toàn cảnh: gốc trên cùng, mỗi feature một thẻ nhỏ trong lưới; bấm thẻ để mở nhánh của nó (bản đồ cột).
    var renderGrid = function (root) {
      var idea = state.table === "idea", needs = idea ? needsMe() : [];
      var head = put(mapBox, el("div", "gd-root"));
      var rootBtn = put(head, button("Gốc · " + (root.title || "(chưa đặt tên)"), function () { state.selected = root.path; render(); },
                                     state.selected === root.path));
      rootBtn.title = root.summary || "";
      if (idea) statusChips(root).forEach(function (c) { add(head, c); });
      var features = root.children.filter(function (c) { return c.round === FOCUS_ROUND; });
      var grid = el("div", "gd-grid");
      var visible = function () { return dueFirst(features.filter(function (f) { return matches(f.key, f.title); })); };
      var fill = function () {
        grid.textContent = "";
        visible().forEach(function (n) {
          var below = idea ? branch(n) : null;
          var kind = !idea ? "" : own(n) === "redo" || below.redo ? " redo" : own(n) === "todo" || own(n) === "ui" || below.todo || below.ui ? " wait" : own(n) === "follow" || below.follow ? " follow" : " done";
          var card = put(grid, button("", function () { select(n); }, null, "gd-mini" + kind + (n.path === state.selected ? " sel" : "") + (needed(n) ? "" : " dim")));
          card.title = n.title + (n.summary ? "\n" + n.summary : "");
          add(card, el("strong", "", n.title || n.key));
          var tags = put(card, el("span", "gd-mini-tags"));
          if (n.key !== GLOSSARY) {
            var rows = rowsOf(n.key);
            var gate = add(tags, el("span", gateOpen(n.key) ? "ok" : "bad", gateOpen(n.key) ? "được code" : "khoá"));
            gate.title = rows.map(function (r) { return (r.unity || "(chưa khai thư mục code)") + ": " + (r.gate.ok ? "ĐƯỢC CODE" : "ĐANG KHOÁ, " + r.gate.reason); }).join("\n");
            var reqs = rows.filter(function (r) { return r.reqs; });
            if (reqs.length) {
              var met = reqs.reduce(function (a, r) { return a + r.reqs.met; }, 0), total = reqs.reduce(function (a, r) { return a + r.reqs.total; }, 0);
              add(tags, el("span", "", "yêu cầu " + met + "/" + total));
            }
          }
          var count = needCount(needs, n.key);
          if (count) add(tags, el("span", "need", count + " cần tôi"));
        });
        if (!grid.children.length) add(grid, el("p", "muted", "Không feature nào khớp."));
      };
      searchBox(mapBox, function () { return visible().length + "/" + features.length + " feature"; }, fill);
      put(put(mapBox, el("div", "gd-scroll")), grid);
      fill();
    };

    // Chế độ Bảng: mỗi thư mục code của mỗi feature một dòng; bấm dòng để mở nhánh trên cây.
    var threads = null;      // /api/threads (chỉ khi mở qua setup_server.py): phiên đang gán cho từng thư mục
    var DESIGN_TEXT = { redo: "cần làm lại", scoring: "chờ chấm, trả lời", lock: "đạt, chờ chốt", live: "đã chốt" };
    var renderTable = function () {
      var rows = featureRows().filter(function (r) { return r.key !== GLOSSARY; });
      var needs = needsMe();
      var withSession = !!ctx.setup;
      if (withSession && threads === null) {
        threads = [];
        api("GET", "/api/threads").then(function (res) { threads = res.threads || []; if (state.view === "table") render(); },
                                       function () { threads = []; });
      }
      var sessionOf = function (r) {
        if (!r.unity) return "";
        var hit = (threads || []).filter(function (t) { return !t.archived && (t.feature === r.unity || t.binding === r.unity); });
        return hit.map(function (t) { return t.title + (t.live ? " · " + (t.live_status || "đang chạy") : ""); }).join(", ");
      };
      var cols = [
        { key: "feature", label: "Feature", value: function (r) { return plain(r.title); } },
        { key: "round", label: "Vòng đề xuất / live", value: function (r) { return r.roundLive * 10 + r.roundIdea; } },
        { key: "phase", label: "Giai đoạn", value: function (r) { var p = phase(r.home || r.phase); return p ? -p.min : 1; } },
        { key: "gate", label: "Cổng code", value: function (r) { return r.gate.ok ? 1 : 0; } },
        { key: "reqs", label: "Yêu cầu", value: function (r) { return r.reqs ? r.reqs.met / Math.max(r.reqs.total, 1) : -1; } },
        { key: "clash", label: "Ý trái nhau", value: function (r) { return r.clash; } },
        { key: "waiting", label: "Đang chờ", value: function (r) { return r.waiting.join(", "); } },
        { key: "needs", label: "Cần tôi", value: function (r) { return needCount(needs, r.key); } }
      ];
      if (withSession) cols.push({ key: "session", label: "Phiên", value: function (r) { return sessionOf(r); } });
      var sort = state.sort || { key: "needs", dir: -1 };
      var by = cols.filter(function (c) { return c.key === sort.key; })[0] || cols[cols.length - 1];
      // Tới hạn trước, rồi để sau, cuối cùng là hàng bị làm mờ; trong mỗi nhóm mới xếp theo cột đang chọn.
      var groupOf = {};
      rows.forEach(function (r) { groupOf[r.path] = dueGroup(data.idea ? find(data.idea, r.path) : null); });
      rows = rows.slice().sort(function (a, b) {
        if (groupOf[a.path] !== groupOf[b.path]) return groupOf[a.path] - groupOf[b.path];
        var x = by.value(a), y = by.value(b);
        if (x < y) return -sort.dir;
        if (x > y) return sort.dir;
        return (a.gate.ok ? 1 : 0) - (b.gate.ok ? 1 : 0) || (plain(a.title) < plain(b.title) ? -1 : 1);
      });
      var tbody = el("tbody");
      var visible = function () { return rows.filter(function (r) { return matches(r.key, r.title); }); };
      var fill = function () {
        tbody.textContent = "";
        visible().forEach(function (r) {
          var rowNode = data.idea ? find(data.idea, r.path) : null;
          var tr = put(tbody, el("tr", rowNode && !needed(rowNode) ? "dim" : ""));
          tr.tabIndex = 0;
          var open = function () {
            var node = data.idea ? find(data.idea, r.path) : null;
            state.view = "tree"; state.expandAll = false;
            if (node) goTo(node); else render();
          };
          tr.addEventListener("click", open);
          tr.addEventListener("keydown", function (e) { if (e.key === "Enter") open(); });
          var cell = function (main, sub, className) {
            var td = put(tr, el("td", className || "", main));
            if (sub) add(td, el("small", "", sub));
            return td;
          };
          cell(r.title, r.unity || "(chưa khai thư mục code)");
          cell(r.roundIdea + " / " + r.roundLive, DESIGN_TEXT[r.design] || "");
          // Giai đoạn của feature (theo điểm cao nhất bên dưới, kể cả phần đã xong); dòng nhỏ: phần còn lại thuộc giai đoạn nào.
          var rp = phase(r.home || r.phase), rest = phase(r.phase);
          cell(rp ? rp.name : "chưa chấm",
               (r.deepen || []).length ? r.deepen.length + " node cần tới vòng 4"
                 : !rp ? "" : !rest ? "đã xong" : r.due === false ? (rest.key !== rp.key ? "phần còn lại: " + rest.name + ", để sau" : "để sau") : rest.key !== rp.key ? "phần còn lại: " + rest.name : "",
               r.due === false ? "muted" : "");
          var gate = cell(r.gate.ok ? "ĐƯỢC CODE" : "ĐANG KHOÁ", "", r.gate.ok ? "ok-text" : "bad-text");
          gate.title = r.gate.reason || "";
          if (r.reqs) {
            var c = r.reqs.counts, extra = [];
            if (c.done) extra.push(c.done + " chờ kiểm");
            if (c.rejected) extra.push(c.rejected + " bị trả");
            if (c.changed) extra.push(c.changed + " đổi thiết kế");
            if (c["no-test"]) extra.push(c["no-test"] + " mất test");
            cell(r.reqs.met + "/" + r.reqs.total, extra.join(", "));
          } else cell("—");
          cell(r.clash ? String(r.clash) : "—", "", r.clash ? "bad-text" : "");
          cell(r.waiting.length ? r.waiting.join(", ") : "—");
          var n = needCount(needs, r.key);
          cell(n ? String(n) : "—", "", n ? "bad-text" : "");
          if (withSession) cell(threads === null ? "…" : sessionOf(r) || "—");
        });
        if (!tbody.children.length) add(put(tbody, el("tr")), el("td", "muted", "Không feature nào khớp."));
      };
      searchBox(mapBox, function () { return visible().length + "/" + rows.length + " dòng"; }, fill);
      var wrap = put(mapBox, el("div", "gd-ftable-wrap"));
      var tableEl = put(wrap, el("table", "gd-ftable"));
      var tr = put(put(tableEl, el("thead")), el("tr"));
      cols.forEach(function (c) {
        var th = put(tr, el("th"));
        var on = sort.key === c.key;
        var b = put(th, button(c.label + (on ? (sort.dir < 0 ? " ↓" : " ↑") : ""), function () {
          state.sort = { key: c.key, dir: on ? -sort.dir : -1 };
          render();
        }, null, "gd-sort"));
        b.title = "Xếp theo " + c.label;
        if (on) th.setAttribute("aria-sort", sort.dir < 0 ? "descending" : "ascending");
      });
      tableEl.appendChild(tbody);
      fill();
      if (!rows.length) add(mapBox, el("p", "muted gap", "Chưa có feature nào (vòng 2 của bảng đề xuất)."));
    };

    // ---------------------------------------------------------------- bản đồ
    var select = function (node) {
      state.selected = node.path;      // bấm thẻ là chọn node; nội dung của nó hiện dưới bản đồ
      if (node.round === FOCUS_ROUND) state.focus = node.path;
      state.message = null;
      render();
    };

    // Hàng trạng thái ở đáy thẻ (bảng đề xuất): việc cần bạn làm tại node, trạng thái duyệt của node, rồi của nhánh.
    var statusChips = function (n) {
      var out = [];
      var open = function () { state.selected = n.path; state.message = null; render(); };
      if (n.pending) {
        var ask = button(n.pending + " câu hỏi", open, null, "gd-chip ask");
        ask.title = "Mở node để trả lời " + n.pending + " câu hỏi của @GameDesigner";
        out.push(ask);
      }
      // Node cần bạn duyệt mang nhãn đỏ "Chưa duyệt" (bấm để mở node). Node đang đạt có thêm dấu ✓: duyệt nhanh,
      // chốt node sang live mà không cần mở. Node chưa đạt vì việc của @GameDesigner thì "cần làm lại".
      var todo = function (why) {
        var b = button("Chưa duyệt", open, null, "gd-chip todo");
        b.title = why;
        return b;
      };
      if (own(n) === "redo") {
        var redo = el("span", "gd-tag bad", "Cần làm lại");
        redo.title = n.reasons.join("\n");
        out.push(redo);
      } else if (own(n) === "ui") {
        var layout = el("span", "gd-tag clash", "Chờ @UIDesigner");
        layout.title = n.uiWait.join("\n");
        out.push(layout);
      } else if (!n.passed) {
        out.push(todo("Đang chờ bạn chấm hoặc trả lời; bấm để mở node.\n" + n.reasons.join("\n")));
      } else if (n.state === "same") {
        var done = el("span", "gd-tag ok", "✓ Đã duyệt");
        done.title = "Node này đạt và giống bản đã chốt (live)";
        out.push(done);
      } else if (n.follow) {
        var roots = n.followOf || [], waits = n.followWait || [];
        var first = roots.length ? find(data.idea, roots[0]) : null;
        var label = !roots.length ? "Chỉ đổi dòng refs" : roots.length === 1 ? "Theo ref: " + (first ? first.title || shortPath(roots[0], 28) : shortPath(roots[0], 28)) : "Theo ref: " + roots.length + " node";
        var tag = button(label, function () { if (first) goTo(first); else open(); }, null, "gd-chip follow");
        tag.title = "Nội dung node không đổi: chỉ dòng refs/based-on được ghim lại theo node đã đổi:\n" + roots.map(function (r) { return "- " + (r || "(gốc)"); }).join("\n") +
          (waits.length ? "\nTự chốt sang live khi bạn chốt: " + waits.join(", ") + ". Bấm để tới node gốc." : "\nNode gốc đã chốt: bấm ✓ để chốt node này (các node theo ref khác đi cùng).");
        out.push(tag);
        if (!waits.length && ctx.setup) {
          var goFollow = button("✓", function () { promote(n, false); }, null, "gd-chip go tick");
          goFollow.title = "Node gốc đã ở live: chốt node này và mọi node theo ref đang chờ";
          goFollow.setAttribute("aria-label", "Chốt node theo ref " + (n.title || where(n)) + " sang live");
          out.push(goFollow);
        }
      } else {
        out.push(todo("Node đạt nhưng chưa chốt sang live" + (ctx.setup ? ": bấm ✓ để duyệt nhanh, hoặc bấm đây để mở node." : ". " + SERVER_HINT)));
        if (ctx.setup) {
          var go = button("✓", function () { promote(n, false); }, null, "gd-chip go tick");
          go.title = "Duyệt: chốt node này sang live (không cần PIN)";
          go.setAttribute("aria-label", "Duyệt và chốt node " + (n.title || where(n)) + " sang live");
          out.push(go);
        }
      }
      if (n.due === false) {
        var ph = phase(rank(n).phase);
        var laterTag = el("span", "gd-tag later", "Để sau · " + (ph ? ph.name : "tuỳ chọn"));
        laterTag.title = "Node của giai đoạn sau giai đoạn đang làm: chỉ định nghĩa để đó, chưa cần đào sâu; yêu cầu code của nó chưa giao.";
        out.push(laterTag);
      } else if (n.deepen) {
        var deepTag = el("span", "gd-tag deepen", "Cần tới vòng 4");
        deepTag.title = "Node tới hạn của giai đoạn đang làm nhưng mới ở vòng " + n.round + ": @GameDesigner phải viết tiếp tới vòng 4.";
        out.push(deepTag);
      }
      var pulls = n.followers || { ready: [], redo: [] };
      if (n.state !== "same" && !n.follow && (pulls.ready.length || pulls.redo.length)) {
        var pull = el("span", "gd-tag pull", "Kéo theo " + (pulls.ready.length + pulls.redo.length));
        pull.title = "Node khác tham chiếu tới node này:\n" +
          (pulls.ready.length ? pulls.ready.length + " node đã rà, tự chốt khi bạn chốt node này:\n" + pulls.ready.map(function (p) { return "- " + p; }).join("\n") + "\n" : "") +
          (pulls.redo.length ? pulls.redo.length + " node còn ghim hash cũ, chờ @GameDesigner rà:\n" + pulls.redo.map(function (p) { return "- " + p; }).join("\n") : "");
        out.push(pull);
      }
      if (n.state !== "same" && !n.follow) {
        var look = toLook(n);
        var diffChip = button(diffShort(n), open, null, "gd-chip diff");
        diffChip.title = "So với live: " + diffParts(n).join(", ") + (look ? ". " + look + " ý mới hoặc sửa bạn chưa chấm." : ".") + " Bấm để xem.";
        out.push(diffChip);
      }
      // Nhánh: mọi node bên dưới. Chỉ là hiển thị; node cha "đã duyệt" mà nhánh còn việc thì thấy ngay ở đây.
      var b = branch(n);
      if (b.total && !b.todo && !b.redo && !b.ui && !b.follow) {
        var all = el("span", "gd-tag ok branch", "Nhánh ✓");
        all.title = "Cả " + b.total + " node bên dưới đã duyệt";
        out.push(all);
      } else if (b.total) {
        var text = "Nhánh: " + [b.redo ? b.redo + " cần làm lại" : "", b.ui ? b.ui + " chờ UI" : "", b.todo ? b.todo + " chưa duyệt" : "", b.follow ? b.follow + " theo ref" : ""].filter(Boolean).join(", ");
        var more = button(text, function () { goTo(n); }, null, "gd-chip branch" + (b.redo ? " redo" : !b.todo && !b.ui ? " follow" : ""));
        more.title = "Trong " + b.total + " node bên dưới: " + b.todo + " chưa duyệt, " + b.redo + " cần làm lại, " + b.follow +
          " chỉ đổi dòng refs theo node khác (tự chốt khi node gốc được chốt). Bấm để mở nhánh.";
        out.push(more);
      }
      return out;
    };
    // Bảng đã chốt: không có khái niệm duyệt; chỉ cảnh báo ý bạn đã chấm thấp.
    var liveChips = function (n) {
      var down = turnedDown(n);
      if (!down) return [];
      var warn = el("span", "gd-tag bad", down + " ý dưới " + passMin());
      warn.title = "Bạn đã chấm " + down + " ý của bản đã chốt dưới mức đạt; node đề xuất tương ứng cần làm lại.";
      return [warn];
    };

    var renderMap = function (root) {
      mapBox.textContent = "";
      var rounds = data.rounds || [];
      var lastRound = state.focus !== null || state.expandAll ? rounds.length : Math.min(OVERVIEW_LAST_ROUND, rounds.length);
      var plan = layout(root, state.focus, lastRound, dueFirst);
      var colX = function (round) { return (round - 1) * (CARD_W + GAP_X); };
      var rowY = function (row) { return HEAD_H + row * (CARD_H + GAP_Y); };
      var width = colX(lastRound) + CARD_W, height = rowY(plan.rows) - GAP_Y;
      var idea = state.table === "idea";

      if (idea) {      // chú giải: màu của thẻ là trạng thái xấu nhất của node cộng nhánh bên dưới
        var legend = put(mapBox, el("div", "gd-map-legend"));
        [["var(--ok)", "Đã duyệt cả node và nhánh"], ["var(--clash)", "Còn node chưa duyệt (ở node này hoặc bên dưới)"],
         ["var(--bad)", "Có node cần @GameDesigner làm lại"]].forEach(function (x) {
          var item = put(legend, el("span"));
          var dot = put(item, el("i"));
          dot.style.background = x[0];
          item.appendChild(document.createTextNode(x[1]));
        });
        add(legend, el("span", "", "Nhãn \"Nhánh\" đếm mọi node bên dưới, kể cả node đang ẩn."));
      }
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
        var nodeFixed = !idea || n.state === "same";      // bảng live: mọi node đã chốt
        var below = idea ? branch(n) : null;
        var kind = (!idea ? "" : own(n) === "redo" || below.redo ? " redo" : own(n) === "todo" || own(n) === "ui" || below.todo || below.ui ? " wait" : own(n) === "follow" || below.follow ? " follow" : " done") + (needed(n) ? "" : " dim") +
          (nodeFixed ? " fixed" : " loose") + (n.nodeBy && !nodeFixed ? " by-gpt" : "");
        var card = el("button", "gd-card" + (n.path === state.selected ? " sel" : "") + kind);
        card.type = "button";
        card.style.left = x + "px";
        card.style.top = y + "px";
        card.style.width = CARD_W + "px";
        card.style.height = CARD_H + "px";
        card.title = n.title + (n.summary ? "\n" + n.summary : "");
        var idLine = el("span", "id", shortPath(n.path, 46));
        idLine.title = "Id của node: " + (n.path || "(gốc)") + " (bấm để chép)";
        idLine.addEventListener("click", function (ev) { ev.stopPropagation(); copyText(n.path || "(gốc)", "id node"); });
        add(card, el("strong", "", n.title || "(chưa đặt tên)"), idLine, el("span", "sum", n.summary || "Chưa có tóm tắt."));
        if (lowest(n) !== null) {
          var low = num(lowest(n), "Điểm thấp nhất của node");
          low.className += " low";
          add(card, low);
        }
        card.addEventListener("click", function () { select(n); });
        map.appendChild(card);
        var status = idea ? statusChips(n) : liveChips(n);
        var chips = [phaseTag(n)];
        if (n.cheat) chips.push(el("span", "gd-tag cheat", "cheat"));
        if (n.by && n.by.gpt) {
          var gptTag = el("span", "gd-tag gpt", "GPT " + n.by.gpt);
          gptTag.title = n.by.gpt + " ý của node do GPT đề xuất";
          chips.push(gptTag);
        }
        if (idea && n.gpt && n.gpt.missing.length) {
          var owedTag = el("span", "gd-tag bad", "GPT thiếu " + n.gpt.missing.length);
          owedTag.title = "GPT đã góp " + n.gpt.missing.length + " ý mà node chưa có; @GameDesigner phải đưa vào, không được bỏ";
          chips.push(owedTag);
        }
        var clashes = idea ? openClashes(n) : 0;
        if (clashes) {
          var clashTag = el("span", "gd-tag clash", "xung đột " + clashes);
          clashTag.title = clashes + " nhóm ý trái nhau chưa chọn: bấm node, chọn 1 ý trong mỗi nhóm. Vòng sau của node này bị khoá tới khi chọn xong";
          chips.push(clashTag);
        }
        if (idea && n.gptOwed) {
          var askTag = el("span", "gd-tag bad", "chưa hỏi GPT");
          askTag.title = "Node vòng 1 và 2 bắt buộc có ít nhất một lần hỏi GPT (qua @DesignAdvisor), trừ feature hạ tầng, test; node này chưa có";
          chips.push(askTag);
        }
        if (n.group) {
          var groupTag = el("span", "gd-tag", n.group === "infra" ? "hạ tầng" : "test");
          groupTag.title = (n.group === "infra" ? "Feature hạ tầng: không cần hỏi GPT" : "Feature test: không hỏi GPT") +
            ". @GameDesigner xếp bằng dòng group: trong data.md (feature mặc định và key đuôi -test tự xếp); sai thì sửa dòng đó";
          chips.push(groupTag);
        }
        if (item.hidden > 0) chips.push(el("span", "gd-tag", "+" + item.hidden + (n.round === FOCUS_ROUND - 1 ? " feature ẩn" : " node con")));
        // Hai hàng ở đáy thẻ: trạng thái (nhìn trước) ở trên, thông tin ở dưới.
        [[status, "gd-chips", 60], [chips, "gd-chips info", 30]].forEach(function (r) {
          if (!r[0].length) return;
          var row = el("div", r[1] + (needed(n) ? "" : " dim"));
          row.style.left = (x + 12) + "px";
          row.style.top = (y + CARD_H - r[2]) + "px";
          row.style.width = (CARD_W - 22) + "px";
          r[0].forEach(function (c) { add(row, c); });
          map.appendChild(row);
        });
      });
    };

    // ---------------------------------------------------------------- các phần của node đang chọn (vẽ dưới bản đồ)
    var renderVerdict = function (body, node) {
      var box = put(body, el("div", "gd-verdict " + (node.passed ? "ok" : "bad")));
      add(box, el("strong", "", node.passed ? "Đạt" : node.redo ? "Chưa đạt, @GameDesigner cần làm lại" :
        uiOwed(node) ? "Chưa đạt, chờ @UIDesigner xếp bố cục" : "Chưa đạt, đang chờ bạn"));
      add(box, el("span", "", node.passed
        ? (node.state === "same" ? "Đã chốt, giống live." : "Chưa ở live.")
        : node.reasons.join(" · ")));
      box.title = node.passed ? "Chép sang live bằng nút Chốt bên dưới hoặc Sync ở thanh trên."
        : "Các node con đang bị khoá với @GameDesigner cho tới khi node này đạt" + (mode() === "user" ? " và được chốt." : ".");
    };

    // Khung "So với live": node đổi gì so với bản đã chốt, và bao nhiêu ý đổi bạn chưa chấm.
    var renderChanges = function (body, node) {
      if (node.state === "same") return;
      var box = put(body, el("div", "gd-changes"));
      add(box, el("p", "", "So với live: " + diffParts(node).join(" · ") + "."));
      var d = node.diff || {};
      if (d.title !== null && d.title !== undefined) diffLine(box, "Tên:", d.title, node.title);
      if (d.summary !== null && d.summary !== undefined) diffLine(box, "Tóm tắt:", d.summary, node.summary);
      var look = toLook(node), changed = changedItems(node).length;
      if (look) add(box, el("p", "", look + " ý mới hoặc sửa bạn chưa chấm (vạch vàng bên trái). Full Auto không bắt chấm, nhưng đây là chỗ AI vừa đổi."));
      renderFiles(box, node);
      if (changed && changed < node.items.length) {
        add(box, button(state.onlyChanged ? "Hiện mọi ý" : "Chỉ hiện " + changed + " ý đổi", function () {
          state.onlyChanged = !state.onlyChanged; render();
        }, state.onlyChanged));
      }
    };

    var renderItems = function (body, node) {
      add(body, el("h3", "", "Ý cốt lõi"));
      if (!node.items.length) { add(body, el("p", "muted", "Node chưa có ý nào trong data.md.")); return; }
      // Chú giải màu chữ: ý đã chốt (có trong live, đúng nội dung) và ý còn là đề xuất của từng AI.
      var legend = put(body, el("p", "gd-legend"));
      [["fixed", "Đã chốt"], ["claude", "Claude đề xuất"], ["gpt", "GPT đề xuất"], ["clash", "Trái nhau: chọn 1"]].forEach(function (x) {
        add(legend, el("b", x[0], x[1]));
      });
      var drafts = [];
      var saveBtn = null, saveNote = null;
      var refresh = function () {
        var changed = drafts.filter(function (d) { return d.changed(); }).length;
        saveBtn.disabled = !changed || !ctx.setup;
        saveNote.textContent = !ctx.setup ? SERVER_HINT : changed ? changed + " ý có thay đổi chưa lưu." : "Chấm điểm hoặc ghi phản hồi rồi bấm Lưu.";
      };
      // Các ý trái nhau (nối với nhau qua it.clash) gom thành nhóm "Chọn 1": một khung, các ý nằm liền nhau ở vị trí
      // của ý đầu tiên trong nhóm, để thấy ngay ý nào trái với ý nào.
      var byId = {}, groupOf = {}, order = [];
      node.items.forEach(function (it) { byId[it.id] = it; });
      node.items.forEach(function (it) {
        if (!(it.clash && it.clash.length) || groupOf[it.id]) return;
        var g = { ids: [], box: null, setters: {} }, stack = [it.id];
        while (stack.length) {
          var id = stack.pop();
          if (groupOf[id] || !byId[id]) continue;
          groupOf[id] = g;
          g.ids.push(id);
          (byId[id].clash || []).forEach(function (x) { stack.push(x); });
        }
        g.ids.sort(function (a, b) { return node.items.indexOf(byId[a]) - node.items.indexOf(byId[b]); });
      });
      node.items.forEach(function (it) {
        var g = groupOf[it.id];
        if (!g || g.ids.length < 2) { order.push(it); return; }
        if (g.placed) return;
        g.placed = true;
        g.ids.forEach(function (id) { order.push(byId[id]); });
      });
      var idea = state.table === "idea";
      order.forEach(function (it) {
        if (idea && state.onlyChanged && it.change === "same" && !groupOf[it.id]) return;
        // Chữ của ý là chính. Dưới nó chỉ có hai khung nhỏ (điểm AI, điểm của bạn). Bấm vào ý mới mở phần cập nhật:
        // hàng nút chấm điểm và ô nhập phản hồi.
        var group = groupOf[it.id] && groupOf[it.id].ids.length > 1 ? groupOf[it.id] : null;
        if (group && !group.box) {
          group.box = put(body, el("div", "gd-pick"));
          var pickHead = put(group.box, el("p", "gd-pick-head"));
          add(pickHead, el("b", "", "Chọn 1 trong " + group.ids.length), el("span", "", "Các ý này trái nhau, chỉ giữ một. " +
            "\"Chọn ý này\" chấm ý đó " + PICK_KEEP + " và các ý còn lại " + PICK_DROP + "; vẫn sửa được, bấm Lưu chấm điểm để ghi."));
        }
        var change = idea && it.change && it.change !== "same" ? it.change : "";
        var look = change && it.user === null;
        var block = put(group ? group.box : body, el("div", "gd-item" + (look ? " look" : "")));
        var draft = { score: it.user, feedback: it.feedback || "" };
        var head = put(block, el("div", "gd-head"));
        head.tabIndex = 0;
        head.title = "Bấm để chấm điểm và ghi phản hồi cho ý này";
        head.setAttribute("role", "button");
        head.setAttribute("aria-expanded", "false");
        // Một hàng: tiêu đề bên trái, rồi ý kiến đã ghi (rút gọn) và hai khung điểm bên phải.
        // Màu chữ: đã chốt (bảng live, hoặc live có ý này đúng nội dung) dùng màu chữ chính; hai ý trái nhau màu cam;
        // còn lại mang màu của AI viết ra nó.
        var who = it.by === "gpt" ? "gpt" : "claude";
        var clash = it.clash && it.clash.length;
        var itemFixed = state.table === "live" || it.fixed;
        if (group) add(head, el("span", "gd-pick-letter", String.fromCharCode(65 + group.ids.indexOf(it.id))));
        add(head, inline(ctx, el("p", "text" + (clash ? " clash" : itemFixed ? "" : " " + who)), it.title));
        var marks = put(head, el("div", "gd-marks"));
        var sources = (it.sources || []).map(function (x) { return x.advice + "#" + x.point; }).join(", ");
        if (change) {
          var changeMark = put(marks, el("span", "gd-mark change", change === "new" ? "Mới" : "Đã sửa"));
          changeMark.title = (change === "new" ? "Ý mới, live chưa có" : "Ý đã sửa so với live: chữ cũ gạch đỏ, chữ mới tô vàng") +
            (it.changedAt ? " · đổi lúc " + it.changedAt : "") + (look ? " · bạn chưa chấm bản này" : "");
        }
        var byMark = put(marks, el("span", "gd-mark by " + who, BY_NAMES[who]));
        byMark.title = who === "gpt" ? "GPT đề xuất (góp ý " + sources + "); Claude chỉ rút gọn câu chữ" : "Claude (@GameDesigner) đề xuất";
        if (it.clarified) {
          var clearMark = put(marks, el("span", "gd-mark by gpt", "GPT làm rõ"));
          clearMark.title = "Ý của Claude, GPT viết lại cho rõ (góp ý " + sources + ")";
        }
        if (clash) {
          var clashMark = put(marks, el("span", "gd-mark by clash", group ? "chọn 1" : "trái nhau"));
          clashMark.title = "Trái với: " + it.clash.map(function (id) { return byId[id] ? byId[id].title : id; }).join(" · ") +
            ". Các ý này không cùng đúng: chấm điểm để loại một bên.";
        }
        var pickBtn = null;
        if (group) {
          pickBtn = put(head, el("button", "gd-pick-btn", "Chọn ý này"));
          pickBtn.type = "button";
          pickBtn.title = "Chấm ý này " + PICK_KEEP + " và các ý còn lại trong nhóm " + PICK_DROP + " (điểm nháp, chưa lưu)";
          if (!ctx.setup) pickBtn.disabled = true;
          pickBtn.addEventListener("click", function () {
            group.ids.forEach(function (id) { group.setters[id](id === it.id ? PICK_KEEP : PICK_DROP); });
          });
        }
        var aiMoved = idea && it.liveAi !== null && it.liveAi !== undefined;
        var aiMark = put(marks, el("span", "gd-mark " + band(it.ai).key, "AI: " + (aiMoved ? it.liveAi + " → " : "") + it.ai));
        aiMark.title = "AI chấm " + it.ai + " · " + band(it.ai).label + (aiMoved ? " · live: " + it.liveAi : "");
        var userMark = put(marks, el("span", "gd-mark", ""));
        var said = put(marks, el("span", "gd-said", ""));
        var showMarks = function () {
          var scored = draft.score !== null && draft.score !== undefined;
          userMark.className = "gd-mark" + (scored ? " " + band(draft.score).key : " none");
          userMark.textContent = "User: " + (scored ? draft.score : "Chưa");
          userMark.title = scored ? "Bạn chấm " + draft.score + " · " + band(draft.score).label : "Bạn chưa chấm ý này";
          said.textContent = editor.hidden ? draft.feedback.trim() : "";
        };
        if (change === "rewritten" && it.live) {
          if (it.live.title !== it.title) diffLine(block, "So với live:", it.live.title, it.title);
          else add(block, el("p", "gd-diff", "Tiêu đề giữ nguyên; phần chi tiết đã sửa (bấm ý để xem)."));
        }
        var editor = put(block, el("div", "gd-edit"));
        editor.hidden = true;
        if (change === "rewritten" && it.live && (it.live.detail || "") !== (it.detail || "")) {
          diffLine(editor, "Chi tiết so với live:", it.live.detail || "", it.detail || "");
        }
        if (it.detail) renderText(ctx, put(editor, el("div", "gd-more")), it.detail);      // chi tiết của ý chỉ hiện khi đã bấm mở
        (it.sources || []).forEach(function (src) {      // lời của chính GPT, không qua tóm gọn của Claude
          if (!src.text) return;
          var box = put(editor, el("div", "gd-source"));
          var kindText = src.kind + (src.target ? " " + src.target : "");
          add(box, el("b", "", "Nguyên văn GPT · " + kindText + " · góp ý " + src.advice + "#" + src.point), el("p", "", src.text));
        });
        var code = put(editor, el("p", "gd-id", "Mã của ý: "));      // mã chỉ hiện khi đã bấm mở
        add(code, el("code", "", it.id));
        var row = put(editor, el("div", "gd-score"));
        var buttons = [];
        var setScore = function (value) {
          draft.score = value;
          buttons.forEach(function (x, i) { x.setAttribute("aria-pressed", i === value ? "true" : "false"); });
          showMarks();
          refresh();
        };
        if (group) group.setters[it.id] = setScore;
        for (var s = 0; s <= 10; s++) (function (value) {
          var b = button(String(value), function () { setScore(value); }, it.user === value, "");
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
        head.addEventListener("click", function (e) {
          if (pickBtn && e && e.target === pickBtn) return;      // nút "Chọn ý này" không mở phần chấm điểm
          toggle();
        });
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
      var removed = idea && node.diff ? node.diff.removed || [] : [];
      if (removed.length) {
        add(body, el("h4", "", "Ý đã bỏ so với live (" + removed.length + ")"));
        var gone = put(body, el("ul", "gd-removed"));
        removed.forEach(function (r) {
          var li = put(gone, el("li"));
          add(li, el("span", "", r.title));
          li.title = "Mã " + r.id + (r.detail ? "\n" + r.detail : "") + "\nChốt node này thì ý này mất khỏi live.";
        });
      }
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

    // Chấm một frame như chấm một ý (node có ui.md): điểm 0-10 và ý kiến, lưu chung với điểm của các ý.
    var renderFrameScore = function (box, node, fr) {
      var it = (node.frameItems || []).filter(function (x) { return x.file === fr.file; })[0];
      if (!it) return;
      var wrap = put(box, el("div", "gd-frame-score"));
      var head = put(wrap, el("p", ""));
      add(head, el("b", "", "Chấm frame này "), num(it.ai, "Điểm AI của @UIDesigner"),
          el("span", "muted", it.user !== null ? " · bạn đã chấm " + it.user : " · bạn chưa chấm"));
      if (it.why && it.why.length) {
        var why = put(wrap, el("ul", "gd-why"));
        it.why.forEach(function (w) { add(why, el("li", "", w)); });
      }
      if (it.previous) add(wrap, el("p", "gd-note", "Bản trước của frame bạn chấm " + (it.previous.score === null ? "–" : it.previous.score) +
        (it.previous.feedback ? " · " + it.previous.feedback : "") + ". Frame đã đổi nên cần chấm lại."));
      var draft = { score: it.user, feedback: it.feedback || "" };
      var row = put(wrap, el("div", "gd-score"));
      var buttons = [];
      for (var s = 0; s <= 10; s++) (function (value) {
        var b = button(String(value), function () {
          draft.score = value;
          buttons.forEach(function (x, i) { x.setAttribute("aria-pressed", i === value ? "true" : "false"); });
          sync();
        }, it.user === value, "");
        b.title = value + " · " + band(value).label;
        if (!ctx.setup) b.disabled = true;
        buttons.push(put(row, b));
      })(s);
      var area = put(wrap, el("textarea"));
      area.value = draft.feedback;
      area.placeholder = "Ý kiến về bố cục (to, nhỏ, xa ngón cái, thiếu gì); @UIDesigner đọc khi làm lại.";
      area.setAttribute("aria-label", "Ý kiến của bạn về frame " + fr.file);
      area.addEventListener("input", function () { grow(area); draft.feedback = area.value || ""; sync(); });
      var foot = put(wrap, el("div", "gd-row gd-save"));
      var note = put(foot, el("span", "", ""));
      var save = put(foot, button("Lưu điểm frame", function () {
        var items = {};
        items[it.id] = { score: draft.score, feedback: draft.feedback.trim() };
        save.disabled = true;
        send("/api/gamedesign/review", { path: node.path, table: state.table, items: items },
          function () { return "Đã lưu điểm frame " + fr.file + " của node " + where(node) + "."; },
          function (text) { note.textContent = "Không lưu: " + text; save.disabled = false; });
      }, null, "gd-btn main"));
      var sync = function () {
        var changed = draft.score !== it.user || draft.feedback.trim() !== (it.feedback || "");
        save.disabled = !changed || !ctx.setup;
        note.textContent = !ctx.setup ? SERVER_HINT : changed ? "Có thay đổi chưa lưu." : "";
      };
      sync();
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
          if (o.by) add(text, el("span", "gd-mark by", (BY_NAMES[o.by] || o.by) + " đề xuất"));
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
        var left = node.reqs ? node.reqs.open : 0;
        var redo = cell.state === "done" && left > 0;
        var label = p.name + ": " + (WORK[cell.state] || cell.state) +
          (redo ? (node.reqs.hasBook ? " → cần làm lại (" + left + " yêu cầu)" : " (chưa đối chiếu " + left + " yêu cầu)") : "");
        var b = put(row, button(label, function () {
          state.message = null;
          send("/api/gamedesign/progress", { path: node.path, phase: p.key, state: next },
            function () { return "Đã ghi tiến độ " + p.name + " của node " + where(node) + ": " + (WORK[next] || next) + "."; });
        }, null, "gd-cell " + cell.state + (redo ? " redo" : "") + (rank(node).phase === p.key ? " own" : "")));
        b.title = (cell.by ? "Đánh dấu bởi " + cell.by + " lúc " + cell.at + ". " : "") + (cell.note ? cell.note + ". " : "") +
          "Bấm để đổi sang: " + (WORK[next] || next) + "." + (rank(node).phase === p.key ? " Đây là giai đoạn của node này theo độ quan trọng." : "");
        if (!ctx.setup) b.disabled = true;
      });
      if (!ctx.setup) add(row, el("span", "gd-id", SERVER_HINT));
    };

    // Sổ yêu cầu của node lá (Management/tools/reqbook.py): mỗi ý, mỗi trường hợp biên và phần thông số là một yêu cầu.
    // Feature ghi doing/done (kèm test), @CodeLead kiểm; người chỉ bỏ qua một yêu cầu, kèm lí do.
    var REQ_ORDER = ["doing", "rejected", "changed", "no-test", "todo", "done", "verified", "waived"];
    var renderRequirements = function (body, node) {
      var book = node.reqs;
      if (!book || !book.items.length) return;
      var names = data.reqStates || {};
      var box = fold(body, "Yêu cầu cho code: " + book.met + "/" + book.items.length + " đã kiểm" +
        (book.open ? " · còn " + book.open : "") + " (" + book.feature + ")");
      if (book.open) box.open = book.items.some(function (r) { return ["rejected", "changed", "no-test"].indexOf(r.state) >= 0; });
      add(box, el("p", "gd-note", "Mỗi ý, mỗi trường hợp biên và phần thông số của node là một yêu cầu. Phiên " + book.feature +
        " ghi đang làm và xong (kèm tên test) bằng gd_req.py; @CodeLead chạy test rồi mới tính đã kiểm. Thiết kế đổi hoặc test mất " +
        "thì yêu cầu tự về chưa xong. Node chỉ được đánh xong khi mọi yêu cầu đã kiểm hoặc đã bỏ qua."));
      var list = put(box, el("div", "gd-reqs"));
      book.items.slice().sort(function (a, b) { return REQ_ORDER.indexOf(a.state) - REQ_ORDER.indexOf(b.state); }).forEach(function (r) {
        var row = put(list, el("div", "gd-req"));
        add(row, el("span", "gd-req-state " + r.state, names[r.state] || r.state));
        var t = put(row, el("span", "gd-req-title", r.title));
        add(t, el("div", "gd-req-ref", r.ref));
        var more = [];
        if (r.why) more.push(r.why);
        if (r.note) more.push("ghi chú: " + r.note);
        if (r.tests && r.tests.length) more.push("test: " + r.tests.join(", "));
        if (r.play) more.push("Play: " + r.play);
        if (r.hint) more.push(r.hint);
        if (r.by) more.push(r.by + " · " + r.at);
        var waived = r.state === "waived";
        var b = put(row, button(waived ? "Bỏ bỏ qua" : "Bỏ qua", function () {
          var note = null;
          if (!waived) {
            note = window.prompt("Lí do bỏ qua yêu cầu " + r.ref + " (bắt buộc):", "");
            if (!note || !note.trim()) return;
          }
          state.message = null;
          send("/api/gamedesign/requirement", { ref: r.ref, action: waived ? "todo" : "waive", note: note },
            function () { return (waived ? "Đã đưa lại yêu cầu " : "Đã bỏ qua yêu cầu ") + r.ref + "."; });
        }, null, "gd-link"));
        b.title = waived ? "Đưa yêu cầu về chưa làm" : "Không đòi code cho yêu cầu này (người quyết, kèm lí do)";
        if (!ctx.setup) b.disabled = true;
        if (more.length) add(row, el("div", "gd-req-more", more.join(" · ")));
      });
      if (!ctx.setup) add(box, el("p", "gd-id", SERVER_HINT));
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

    // Mục phụ (tham chiếu, hướng dẫn, diễn giải): gập sẵn, tiêu đề như h3, bấm để mở.
    var fold = function (body, title) {
      var box = put(body, el("details", "gd-fold"));
      add(box, el("summary", "", title));
      return box;
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
        var box = fold(body, "Bám theo: " + above.length + " node phía trên");
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
      var notes = node.refNotes || [];
      var bad = refs.filter(function (r) { return r.ok !== true; }).length;
      if (refs.length || notes.length) {
        var refBox = fold(body, "Tham chiếu (refs): " + refs.length + (bad ? ", " + bad + " cần rà" : ""));
        var list = put(refBox, el("ul", "gd-refs"));
        refs.forEach(function (r) {
          var li = put(list, el("li", ""));
          var state_ = r.ok === true ? "khớp" : r.ok === false ? "LỆCH: node đích đã đổi, cần rà lại" : r.ok === "missing" ? "nhắc tới mà chưa khai" : "không có node hay từ này";
          var target = r.term ? "glossary" : r.path;
          if (find(root, target)) put(li, button(r.label, function () { pick(target); }, null, "gd-link"));
          else add(li, el("code", "", r.label));
          add(li, el("span", r.ok === true ? "muted" : "gd-bad", " " + (r.title ? r.title + " · " : "") + state_));
        });
        notes.forEach(function (t) { add(refBox, el("p", "gd-note", t)); });
      }
      var gloss = data.glossary && data.glossary[state.table];
      if (gloss && node.path === gloss.path) {
        var ul2 = put(fold(body, "Các từ được dùng ở đâu (" + gloss.terms.length + " từ)"), el("ul", "gd-refs"));
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
        table().dir.replace("game-design/", "") + (node.path ? node.path + "/" : "")));
      var idP = put(body, el("p", "gd-path gd-idline", "Id: "));
      add(idP, el("code", "", node.path || "(gốc)"));
      idP.appendChild(document.createTextNode(" "));
      put(idP, button("Chép id", function () { copyText(node.path || "(gốc)", "id node"); }, null, "gd-link"));
      if (node.summary) add(body, el("p", "", node.summary));
      if (state.table === "idea") {
        var linkList = function (box, paths) {
          var ul = put(box, el("ul", "gd-refs"));
          paths.forEach(function (p) {
            var li = put(ul, el("li"));
            var target = find(data.idea, p);
            if (target) { put(li, button(p || "(gốc)", function () { goTo(target); }, null, "gd-link")); add(li, el("span", "muted", " " + (target.title || ""))); }
            else add(li, el("code", "", p || "(gốc)"));
          });
        };
        if (node.follow) {
          var fw = node.followWait || [];
          add(body, el("p", "gd-gate ok", "Node này chỉ theo ref: nội dung không đổi, chỉ dòng refs/based-on được ghim lại theo node đã đổi dưới đây. " +
            (fw.length ? "Nó tự chốt sang live khi bạn chốt các node đó; không cần duyệt riêng." : "Các node đó đã ở live: bấm ✓ trên thẻ để chốt.")));
          linkList(body, node.followOf || []);
        }
        var fl = node.followers || { ready: [], redo: [] };
        if (node.state !== "same" && !node.follow && (fl.ready.length || fl.redo.length)) {
          var flBox = fold(body, "Node theo thay đổi này (" + (fl.ready.length + fl.redo.length) + ")");
          if (fl.ready.length) { add(flBox, el("p", "gd-note", "Đã rà, chỉ đổi dòng refs: tự chốt khi bạn chốt node này (" + fl.ready.length + ")")); linkList(flBox, fl.ready); }
          if (fl.redo.length) { add(flBox, el("p", "gd-note", "Còn ghim hash cũ: chờ @GameDesigner đọc lại (" + fl.redo.length + ")")); linkList(flBox, fl.redo); }
        }
      }
      // Ý của node do AI nào đề xuất, ý GPT bị loại, và góp ý của GPT chưa được đưa vào node.
      var clashes = state.table === "idea" ? openClashes(node) : 0;
      if (clashes) add(body, el("p", "gd-clash-text", "Xung đột chưa giải quyết: " + clashes + " nhóm ý trái nhau. Trong mỗi khung \"Chọn 1\" bên dưới, " +
        "chấm để chỉ còn một ý đạt (từ " + passMin() + " trở lên), rồi Lưu chấm điểm. Các node con bị khoá với @GameDesigner cho tới khi hết xung đột."));
      if (node.gptOwed) add(body, el("p", "bad-text", "Chưa hỏi GPT: node vòng 1 và 2 bắt buộc có ít nhất một lần hỏi GPT (trừ feature hạ tầng, test). " +
        "@GameDesigner nhắn @DesignAdvisor hỏi về node này."));
      var byCount = node.by || {}, gptSide = node.gpt || { dropped: [], missing: [], clarified: 0 };
      if (byCount.gpt || gptSide.clarified || gptSide.dropped.length || gptSide.missing.length || node.nodeBy) {
        var byParts = ["Claude " + (byCount.claude || 0), "GPT " + (byCount.gpt || 0)];
        if (gptSide.clarified) byParts.push("GPT làm rõ " + gptSide.clarified);
        if (gptSide.dropped.length) byParts.push("GPT bị loại " + gptSide.dropped.length);
        add(body, el("p", "gd-note", "Ý theo AI đề xuất: " + byParts.join(" · ") +
          (node.nodeBy ? ". Node này do GPT đề xuất (lần hỏi " + node.nodeBy.advice + ")" : "") + "."));
        gptSide.dropped.forEach(function (d) {
          add(body, el("p", "gd-note", "Ý của GPT đã bị loại: " + d.title + " (" + d.advice + "#" + d.point + ")"));
        });
        gptSide.missing.forEach(function (m) {
          add(body, el("p", "bad-text", "Góp ý của GPT chưa vào node: " + m.title + " (" + m.advice + "#" + m.point + ")"));
        });
      }
      if (node.cheat) add(body, el("p", "gd-gate ok", "Node cheat: mỗi ý là một state dựng nhanh ở bản dev để test và review. " +
        "Đây là nơi duy nhất đặt ảnh art của feature (tối đa 3 state, mỗi state một frame và một ảnh)."));
      renderPriority(body, node);
      renderProgress(body, node);
      renderRequirements(body, node);
      renderCode(body, node);
      // Ý cốt lõi: bấm vào một ý để hiện chi tiết, hàng nút chấm điểm và ô nhập ý kiến.
      if (state.table === "idea") { renderVerdict(body, node); renderChanges(body, node); }
      renderItems(body, node);
      renderQuestions(body, node);
      renderPromote(body, node);
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
      // Art và Claude concept: ảnh theo frame của node (order _design), nhãn tính từ hash node và hash style.
      // Art: @Codex gen. Claude concept: @ClaudeImage tự vẽ SVG, studio.py đổi ra PNG (đủ cho giai đoạn IDEA).
      [["codex", "Art (ảnh concept)", "@Codex"], ["claudeimage", "Claude concept (ảnh Claude vẽ, đủ cho giai đoạn IDEA)", "@ClaudeImage"]].forEach(function (kind) {
      var artTool = kind[0], artChat = kind[2];
      var arts = (node.art || []).filter(function (a) { return (a.tool || "codex") === artTool; });
      var artPending = (data.artPending || []).filter(function (o) { return o.node === node.path && (o.tool || "codex") === artTool; });
      if (arts.length || artPending.length) {
        add(body, el("h3", "", kind[1]));
        artPending.forEach(function (o) {
          var line = put(body, el("div", "gd-inline"));
          add(line, el("span", "", "Order " + o.ref + ": " + o.title + " · " + o.items.length + " ảnh · style " + (o.style || "?") +
            (o.version ? " · " + artChat + " đang làm lại (bản đang hiện là v" + o.version + ", chấm sau khi có bản mới)" : " · chờ " + artChat + (artTool === "codex" ? " gen" : " vẽ"))));
        });
        var levels = data.artLevels || [];
        var levelText = function (v) { return v === null ? (data.artLevelNone || "Chưa duyệt") : levels[v]; };
        var latest = {};
        arts.forEach(function (a) { if (!latest[a.frame]) latest[a.frame] = a; });
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
          // Claude concept không ai chấm: nhãn chỉ nói đã giao, chờ ghép lại, hay cần vẽ lại.
          var kindOf = function (v) {
            if (artTool === "claudeimage") return a.stale.length ? "stale" : a.reglue ? "wait" : "ok";
            return a.stale.length ? "stale" : v === null ? "wait" : v === 0 ? "stale" : v === 3 ? "ok" : "";
          };
          var tag = put(holder, el("span", "gd-art-label " + kindOf(a.level), a.label));
          if (a.anchor) add(holder, el("span", "gd-art-anchor", "mốc"));
          if (a.below) add(holder, el("span", "gd-art-below", "chưa chuẩn"));
          add(fig, el("figcaption", "", frame + " · v" + a.version + " · " + a.ref + (a.styles.length ? " · style " + a.styles.join(", ") : "") +
            (a.below ? " · " + artChat + " tự kiểm chưa đạt: " + a.below : "")));
          // Claude concept: bảng phần (hình) của frame: kích thước, 9-slice, dùng ở mấy hình, lấy lại từ node nào.
          if (a.parts && a.parts.length) {
            var partBox = put(fig, el("details", "gd-parts"));
            var sliced = a.parts.filter(function (p) { return p.slice; }).length;
            var shared = a.parts.filter(function (p) { return p.shapes.length > 1; }).length;
            add(partBox, el("summary", "", a.parts.length + " phần · 9-slice: " + sliced + " · dùng nhiều chỗ: " + shared));
            a.parts.forEach(function (p) {
              var row = put(partBox, el("div", "gd-part"));
              var thumb = put(row, el("img"));
              thumb.src = "../" + p.file.split("/").map(encodeURIComponent).join("/");
              thumb.alt = p.id;
              thumb.loading = "lazy";
              zoomable(thumb, null, p.id + " · " + p.w + "x" + p.h + "px");
              add(row, el("span", "", p.id + " · " + p.w + "x" + p.h + "px · " +
                (p.slice ? "9-slice viền " + p.slice.join("/") : "không 9-slice") + " · " + p.shapes.length + " hình (" + p.shapes.join(", ") + ")" +
                (p.from ? " · lấy lại từ " + p.from.node + "#" + p.from.part : "")));
            });
          } else if (a.parts) {
            add(fig, el("p", "gd-note", "Frame này không có phần nào phải vẽ (chỉ có chữ, nét đo hoặc hình của node khác)."));
          }
          if (!a.reviewable) {
            if (a.redoing) add(fig, el("p", "gd-note", artChat + " đang làm lại ảnh này và chưa giao bản mới." +
              (artTool === "claudeimage" ? "" : " Thanh chấm hiện khi có bản giao mới.")));
            if (a.feedback) add(fig, el("p", "gd-note", "Phản hồi đã ghi: " + a.feedback));
            return;
          }
          // Chấm: một thanh trượt 5 nấc (nấc trái cùng = chưa duyệt) và một ô phản hồi; lưu ngay, không có nút gửi.
          // Trang chỉ lưu. @GameDesigner đọc giá trị này khi bạn bảo nó cho làm lại.
          var box = put(fig, el("div", "gd-art-review"));
          var head = put(box, el("div", "gd-art-level"));
          var name = put(head, el("strong", "", levelText(a.level)));
          var saved = put(head, el("span", "gd-art-saved", ""));
          var slider = put(box, el("input"));
          slider.type = "range"; slider.min = "0"; slider.max = String(levels.length); slider.step = "1";
          slider.value = String(a.level === null ? 0 : a.level + 1);
          slider.setAttribute("aria-label", "Mức chấm ảnh " + frame);
          // Nấc: 5 vạch có tên ngắn ngay dưới thanh trượt, bấm vào vạch cũng chọn được mức đó.
          var ticks = put(box, el("div", "gd-art-ticks"));
          ticks.setAttribute("aria-hidden", "true");
          var tickEls = ["Chưa duyệt", "Làm lại", "Lỗi, để sau", "Phần lớn", "Đạt"].slice(0, levels.length + 1).map(function (label, i) {
            var t = put(ticks, el("span", "gd-art-tick"));
            add(t, el("i", ""), el("span", "", label));
            t.title = i === 0 ? levelText(null) : levels[i - 1];
            t.addEventListener("click", function () {
              if (slider.disabled || slider.value === String(i)) return;
              slider.value = String(i); paint(); save();
            });
            return t;
          });
          var note = put(box, el("textarea"));
          note.placeholder = "Phản hồi (không bắt buộc; cần có khi muốn gen lại)";
          note.value = a.feedback || "";
          note.setAttribute("aria-label", "Phản hồi cho ảnh " + frame);
          var hint = put(box, el("p", "gd-note", ""));
          var anchorBox = put(box, el("div", "gd-inline"));
          var current = function () { var n = parseInt(slider.value, 10); return n === 0 ? null : n - 1; };
          var paint = function () {
            var v = current();
            name.textContent = levelText(v);
            tickEls.forEach(function (t, i) { t.className = "gd-art-tick" + (String(i) === slider.value ? " on" : ""); });
            tag.textContent = a.stale.length ? a.label : levelText(v);
            tag.className = "gd-art-label " + kindOf(v);
            hint.textContent = v === 0 && !note.value.trim() ? "Cần phản hồi thì " + artChat + " mới làm lại được ảnh này." :
              v === 0 ? "Bảo @GameDesigner cho làm lại: " + artChat + " làm lại ảnh này 1 lượt theo phản hồi." :
              v === 1 ? "Dùng tạm, ghi vào nợ art (tab Studio)." : "";
            anchorBox.textContent = "";
            if (artTool === "codex" && v === 3 && a.level === 3 && !a.stale.length) {
              add(anchorBox, button(a.anchor ? "Bỏ mốc" : "Đặt làm mốc style", function () {
                send("/api/studio/anchor", { ref: a.ref, item: a.frame }, function (res) { return res.message || "Đã đổi mốc."; });
              }));
            }
          };
          var timer = null, sent = null;
          var save = function () {
            if (timer) { clearTimeout(timer); timer = null; }
            var body = { ref: a.ref, item: a.frame, level: current(), text: note.value.trim() };
            var key = JSON.stringify(body);
            if (key === sent) return;
            sent = key;
            saved.textContent = "đang lưu…";
            api("POST", "/api/studio/review", body).then(function (res) {
              if (!res.ok) { sent = null; saved.textContent = "CHƯA LƯU: " + res.error; saved.className = "gd-art-saved bad-text"; return; }
              data = res.data;                         // không vẽ lại trang: giữ con trỏ trong ô phản hồi
              if (res.studio) window.CORESIMPLE_STUDIO = res.studio;
              a.level = body.level; a.feedback = body.text;
              saved.className = "gd-art-saved";
              saved.textContent = "đã lưu";
              paint();
            }, function (e) { sent = null; saved.textContent = "CHƯA LƯU: " + e.message; saved.className = "gd-art-saved bad-text"; });
          };
          sent = JSON.stringify({ ref: a.ref, item: a.frame, level: a.level, text: (a.feedback || "").trim() });
          if (!ctx.setup) {
            slider.disabled = true; note.disabled = true;
          } else {
            slider.addEventListener("input", paint);
            slider.addEventListener("change", save);
            note.addEventListener("input", function () { paint(); saved.textContent = ""; if (timer) clearTimeout(timer); timer = setTimeout(save, 1000); });
            note.addEventListener("blur", save);
          }
          paint();
        });
        var owed = ((window.CORESIMPLE_STUDIO || {}).debts || []).filter(function (d) {
          return d.state === "open" && (d.tool || "codex") === artTool && d.owner === "_design/" + node.path.split("/").join(".");
        });
        if (owed.length) add(body, el("p", "bad-text", owed.length + " phiếu nợ art đang mở cho node này (" +
          owed.map(function (d) { return d.id + " " + d.item; }).join(", ") + "): xem tab Studio."));
        var older = arts.filter(function (a) { return latest[a.frame] !== a; });
        if (older.length) add(body, el("p", "gd-note", older.length + " bản cũ hơn vẫn giữ trong Resources (không ghi đè)."));
        if (artTool === "claudeimage") add(body, el("p", "gd-note", "Claude concept không cần duyệt. Frame đổi mà phần còn vừa thì chỉ chờ ghép lại; chỉ khi phải vẽ hình mới nó mới mang nhãn CẦN LÀM LẠI, và vẽ lại cần lệnh của một Leader (studio.py redo)."));
        else if (!ctx.setup) add(body, el("p", "gd-note", "Chấm ảnh và đặt mốc cần mở trang qua server Setup (python Management/tools/setup_server.py)."));
      }
      });
      // Frame: bố cục dạng chữ trong .frame-concept, đã được gamedesign.py giải số và vẽ sẵn thành SVG.
      if (node.ui) {
        add(body, el("h3", "", "UI cần có (ui.md, của @GameDesigner)"));
        add(body, el("p", "gd-note", "Danh sách phần tử, tương tác và flow; @UIDesigner xếp bố cục trong frame bên dưới. Hash " + node.ui.hash + "."));
        var uiWrap = put(body, el("div", "gd-ftable-wrap"));
        var uiTable = put(uiWrap, el("table"));
        var uiHead = put(put(uiTable, el("thead")), el("tr"));
        ["Id", "Loại", "Mức", "Dùng để", "Trạng thái"].forEach(function (h) { add(uiHead, el("th", "", h)); });
        var uiBody = put(uiTable, el("tbody"));
        node.ui.elements.forEach(function (e) {
          var tr = put(uiBody, el("tr"));
          add(tr, add(el("td"), el("code", "", e.id)), el("td", "", e.kind), el("td", e.level === "chính" ? "ok-text" : "", e.level),
              el("td", "", e.use), el("td", "", e.states.join(", ") || "—"));
        });
        [["Tương tác", node.ui.interactions], ["Flow", node.ui.flow]].forEach(function (x) {
          if (!x[1].length) return;
          add(body, el("h4", "", x[0]));
          var ul = put(body, el("ul"));
          x[1].forEach(function (t) { add(ul, el("li", "", t)); });
        });
      }
      if (node.uiWait && node.uiWait.length) {
        add(body, el("p", "bad-text", "Chờ @UIDesigner:"));
        var waitUl = put(body, el("ul", "gd-frame-warn"));
        node.uiWait.forEach(function (t) { add(waitUl, el("li", "", t)); });
      }
      if (node.frames && node.frames.length || node.frameNotes && node.frameNotes.length || node.frameNone) {
        add(body, el("h3", "", "Frame (bố cục của @UIDesigner)"));
        if (node.frameNone) add(body, el("p", "gd-note", "Node này không có bố cục (frame: none)."));
        if (node.frames && node.frames.length && !node.cheat) add(body, el("p", "gd-note", "Ảnh art không đặt ở đây: order art chỉ đặt ở node cheat của feature."));
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
            renderFrameScore(shown, node, fr);
            var src = put(shown, el("details", "gd-frame-src"));
            add(src, el("summary", "", "Xem nguồn"), el("pre", "", fr.text));
          };
          // Phương án của cùng một màn (alt:) mang nhãn "Phương án A/B…" để bạn chọn một (chấm từ 5).
          var optionLabel = {};
          node.frames.forEach(function (fr) {
            var screen = fr.alt || fr.file.replace(/\.frame$/, "");
            optionLabel[screen] = optionLabel[screen] || [];
            optionLabel[screen].push(fr.file);
          });
          var tabLabel = function (fr) {
            var screen = fr.alt || fr.file.replace(/\.frame$/, ""), list = optionLabel[screen];
            return list.length > 1 ? screen + " · phương án " + "ABCDEFGH".charAt(list.indexOf(fr.file)) : fr.file;
          };
          if (tabs) node.frames.forEach(function (fr, i) {
            var b = put(tabs, el("button", "gd-btn", tabLabel(fr)));
            b.type = "button";
            b.onclick = function () { show(i); };
          });
          show(0);
        }
      }
      // Thông tin phụ: gập sẵn, cuối danh sách.
      renderContext(body, node, root);
      var guide = fold(body, "Hướng dẫn: độ quan trọng, giai đoạn, thang điểm");
      add(guide, el("h4", "", "Độ quan trọng và giai đoạn"));
      var stages = put(guide, el("div", "gd-scale"));
      phases().forEach(function (p) { add(stages, el("span", "", p.min + "–" + p.max + " " + p.name + ": " + p.label)); });
      add(guide, el("p", "gd-note", "AI chỉ chấm ở node lá; node cha lấy điểm cao nhất bên dưới. Điểm bạn chấm cho một node là tuyệt đối, " +
        "và các node con bạn chưa chấm dời theo cùng khoảng cách. Độ quan trọng không làm đổi hash, không ảnh hưởng việc đạt hay chốt."));
      add(guide, el("h4", "", "Thang điểm"));
      var scale = put(guide, el("div", "gd-scale"));
      (data.bands || []).forEach(function (b) { add(scale, el("span", "", b.min + "–" + b.max + ": " + b.label)); });
      add(guide, el("p", "gd-note", MODES.filter(function (m) { return m.key === mode(); })[0].hint.replace("mức đạt", passMin()) +
        " Điểm của bạn là tuyệt đối và dùng chung cho bảng đề xuất và bảng đã chốt."));
      renderText(ctx, fold(body, "Diễn giải (CLAUDE.md)"), node.body.replace(/^# .*\n?/, "") || "Chưa có diễn giải.");
      // Bảng dữ liệu config của node (<node>/.excel, Management/tools/gd_excel.py): dòng 1 tên cột, dòng 2 kiểu, dòng 3 mô tả.
      if ((node.excel || []).length) {
        var xbox = fold(body, "Dữ liệu (Excel): " + node.excel.length + " bảng");
        add(xbox, el("p", "gd-note", "File mở bằng Excel: .excel/" + (node.key || node.path.split("/").pop() || "idea") +
          ".xlsx trong thư mục của node. Sửa xong chạy gd_excel.py sync để bản chữ (csv) theo kịp."));
        node.excel.forEach(function (t) {
          add(xbox, el("h4", "", t.sheet + " · " + t.kind + (t.key ? " · khoá " + t.key : "")));
          if (t.note) add(xbox, el("p", "gd-note", t.note));
          var tbl = put(put(xbox, el("div", "gd-table")), el("table"));
          t.rows.forEach(function (r, i) {
            if (i === 1) return;      // kiểu: ghi nhỏ ngay dưới tên cột
            var tr = put(tbl, el("tr"));
            r.forEach(function (c, x) {
              var cell = put(tr, el(i === 0 ? "th" : "td", i === 2 ? "gd-note" : "", c));
              if (i === 0 && t.rows[1] && t.rows[1][x]) { add(cell, el("br")); add(cell, el("small", "", t.rows[1][x])); }
            });
          });
        });
      }
      // Lịch sử nằm ở node đề xuất; node live dùng lịch sử của node đề xuất cùng đường dẫn.
      var source = state.table === "idea" ? node : find(data.idea, node.path);
      var history = source ? source.history : [];
      // Hash và lịch sử chỉ để debug: gập sẵn, người không cần đọc.
      var debug = put(body, el("details", "gd-fold gd-debug"));
      add(debug, el("summary", "", "Debug: hash, lịch sử cập nhật"));
      add(debug, el("p", "gd-note", "Hash data " + node.dataHash));
      if (history.length) {
        var list = put(debug, el("ul", "gd-history"));
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
      savePrefs();
      if (state.view === "table") { renderTable(); return; }
      if (!root) { add(mapBox, el("p", "muted", table().empty)); return; }
      if (state.focus === null && !state.expandAll) renderGrid(root); else renderMap(root);
      renderTerms();
      renderDetail(root);
    };

    render();
  };

  window.CORESIMPLE_VIEWS = window.CORESIMPLE_VIEWS || {};
  window.CORESIMPLE_VIEWS.gamedesign = build;
  window.CORESIMPLE_VIEWS.lightbox = lightbox;      // dùng chung với tab Studio
})();
