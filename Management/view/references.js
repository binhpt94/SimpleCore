// Tab "Tham chiếu" của trang quản lý: ảnh người nạp để tham chiếu (bố cục của game khác, style art), mỗi ảnh một
// lời nhận định của người bên dưới, kèm loại và tag. @UIDesigner đọc ảnh "bố cục" trước khi xếp một màn,
// @GameDesigner đọc ảnh "style art" khi viết style vòng 1. Dữ liệu: window.CORESIMPLE_REFERENCES (data/references.js,
// sync_page.py sinh từ Management/references); nạp, sửa, xoá chỉ qua setup_server.py (Management/tools/references.py).
// index.html gọi: CORESIMPLE_VIEWS.references(pane, { el, add, put, section, setup }).
(function () {
  "use strict";

  var CSS = [
    ".rf-drop { display: flex; flex-wrap: wrap; gap: 10px 14px; align-items: center; padding: 18px; border: 2px dashed var(--line); border-radius: 10px; background: var(--panel); margin: 0 0 16px; }",
    ".rf-drop.over { border-color: var(--accent); background: color-mix(in srgb, var(--accent) 8%, var(--panel)); }",
    ".rf-drop p { margin: 0; flex: 1; min-width: 220px; color: var(--muted); }",
    ".rf-bar { display: flex; flex-wrap: wrap; gap: 8px 12px; align-items: center; margin: 0 0 14px; }",
    ".rf-bar input, .rf-card input, .rf-card select { padding: 6px 9px; border: 1px solid var(--line); border-radius: 6px; background: var(--bg); color: var(--text); font: inherit; font-size: 14px; }",
    ".rf-bar input { flex: 1; min-width: 0; max-width: 360px; }",
    ".rf-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(280px, 1fr)); gap: 14px; }",
    ".rf-card { display: flex; flex-direction: column; gap: 8px; padding: 10px; border: 1px solid var(--line); border-radius: 10px; background: var(--panel); min-width: 0; }",
    ".rf-card .rf-img { display: block; width: 100%; aspect-ratio: 16 / 10; object-fit: contain; background: var(--bg); border-radius: 6px; cursor: zoom-in; }",
    ".rf-card textarea { width: 100%; min-height: 72px; resize: vertical; padding: 8px 10px; border: 1px solid var(--line); border-radius: 6px; background: var(--bg); color: var(--text); font: inherit; font-size: 14px; }",
    ".rf-row { display: flex; flex-wrap: wrap; gap: 6px 8px; align-items: center; font-size: 13px; color: var(--muted); }",
    ".rf-row input { flex: 1; min-width: 0; }",
    ".rf-saved { font-size: 12px; color: var(--muted); min-height: 1em; }",
    ".rf-btn { padding: 6px 12px; border: 1px solid var(--line); border-radius: 6px; background: var(--panel); color: var(--text); font: inherit; font-size: 13.5px; cursor: pointer; }",
    ".rf-btn:hover { border-color: var(--accent); }",
    ".rf-btn:disabled { opacity: .5; cursor: default; }",
    ".rf-btn[aria-pressed='true'] { background: var(--accent); border-color: var(--accent); color: var(--panel); font-weight: 600; }",
    ".rf-btn.danger { color: var(--bad); }",
    ".rf-btn:focus-visible, .rf-card .rf-img:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }"
  ].join("\n");

  var build = function (pane, ctx) {
    var el = ctx.el, add = ctx.add, put = ctx.put;
    if (!document.getElementById("rf-css")) {
      var st = document.createElement("style");
      st.id = "rf-css";
      st.textContent = CSS;
      document.head.appendChild(st);
    }
    var data = window.CORESIMPLE_REFERENCES || { items: [], kinds: ["bố cục", "style art"], dir: "references/" };
    var state = { kind: "", query: "", message: null };

    add(pane, el("h1", "", "Tham chiếu"));
    add(pane, el("p", "sub", "Ảnh bạn nạp để tham chiếu: bố cục màn của game khác, hoặc style art muốn hướng tới. Ghi nhận định dưới mỗi ảnh " +
      "(cái gì hay, cái gì không theo). @UIDesigner xem ảnh \"bố cục\" cùng tag trước khi xếp một màn và ghi trong frame mình theo ảnh nào; " +
      "@GameDesigner xem ảnh \"style art\" khi viết style vòng 1."));
    var top = put(pane, el("div"));
    var listBox = put(pane, el("div"));

    var api = function (path, body) {
      return fetch(path, {
        method: "POST", cache: "no-store",
        headers: { "Content-Type": "application/json", "X-Setup-Token": ctx.setup.token },
        body: JSON.stringify(body)
      }).then(function (r) {
        if (!r.ok) throw new Error("HTTP " + r.status);
        return r.json();
      });
    };
    var say = function (text, bad) { state.message = { text: text, bad: !!bad }; renderTop(); };
    var button = function (label, onClick, className) {
      var b = el("button", "rf-btn" + (className ? " " + className : ""), label);
      b.type = "button";
      b.addEventListener("click", onClick);
      return b;
    };

    // Nạp: đọc file trong trình duyệt thành base64 rồi gửi server; nhiều ảnh thì gửi lần lượt.
    var uploadFiles = function (files) {
      var list = Array.prototype.filter.call(files || [], function (f) { return /^image\/(png|jpeg|webp)$/.test(f.type); });
      if (!list.length) { say("Chỉ nhận ảnh png, jpg hoặc webp.", true); return; }
      var done = 0, failed = [];
      var next = function () {
        if (!list.length) {
          say(failed.length ? "Đã nạp " + done + " ảnh; không nạp được: " + failed.join("; ") : "Đã nạp " + done + " ảnh. Ghi nhận định dưới từng ảnh.", failed.length > 0);
          renderList();
          return;
        }
        var file = list.shift();
        var reader = new FileReader();
        reader.onload = function () {
          var b64 = String(reader.result || "").split(",")[1] || "";
          api("/api/references/upload", { name: file.name, data: b64, kind: state.kind || data.kinds[0], tags: [] }).then(function (res) {
            if (res.ok) { data = res.data; done++; } else failed.push(file.name + ": " + res.error);
            next();
          }, function (e) { failed.push(file.name + ": " + e.message); next(); });
        };
        reader.onerror = function () { failed.push(file.name + ": không đọc được file"); next(); };
        say("Đang nạp " + file.name + "…");
        reader.readAsDataURL(file);
      };
      next();
    };

    var renderTop = function () {
      top.textContent = "";
      if (ctx.setup) {
        var drop = put(top, el("div", "rf-drop"));
        add(drop, el("p", "", "Kéo thả ảnh vào đây, hoặc chọn từ máy (png, jpg, webp; mỗi ảnh tối đa 8 MB). Ảnh mới mang loại đang lọc bên dưới."));
        var input = el("input");
        input.type = "file";
        input.accept = "image/png,image/jpeg,image/webp";
        input.multiple = true;
        input.hidden = true;
        input.addEventListener("change", function () { uploadFiles(input.files); input.value = ""; });
        add(drop, input, button("Chọn ảnh", function () { input.click(); }));
        ["dragenter", "dragover"].forEach(function (ev) {
          drop.addEventListener(ev, function (e) { e.preventDefault(); drop.className = "rf-drop over"; });
        });
        ["dragleave", "drop"].forEach(function (ev) {
          drop.addEventListener(ev, function (e) { e.preventDefault(); drop.className = "rf-drop"; });
        });
        drop.addEventListener("drop", function (e) { uploadFiles(e.dataTransfer && e.dataTransfer.files); });
      } else {
        add(top, el("p", "muted", "Nạp ảnh, ghi nhận định, sửa và xoá cần mở trang qua python Management/tools/setup_server.py. Ở đây chỉ xem."));
      }
      if (state.message) add(top, el("p", state.message.bad ? "bad-text" : "ok-text", state.message.text));
      var bar = put(top, el("div", "rf-bar"));
      var all = put(bar, button("Tất cả", function () { state.kind = ""; renderTop(); renderList(); }));
      all.setAttribute("aria-pressed", state.kind ? "false" : "true");
      data.kinds.forEach(function (k) {
        var b = put(bar, button(k, function () { state.kind = k; renderTop(); renderList(); }));
        b.setAttribute("aria-pressed", state.kind === k ? "true" : "false");
      });
      var q = put(bar, el("input"));
      q.type = "search";
      q.placeholder = "Lọc theo tag hoặc chữ trong nhận định";
      q.setAttribute("aria-label", "Lọc ảnh tham chiếu");
      q.value = state.query;
      q.addEventListener("input", function () { state.query = q.value || ""; renderList(); });
    };

    var matches = function (x) {
      if (state.kind && x.kind !== state.kind) return false;
      var q = state.query.trim().toLowerCase();
      return !q || (x.note + " " + x.tags.join(" ") + " " + x.id).toLowerCase().indexOf(q) >= 0;
    };

    var renderList = function () {
      listBox.textContent = "";
      var items = (data.items || []).filter(matches);
      add(listBox, el("p", "muted", items.length + "/" + (data.items || []).length + " ảnh"));
      if (!items.length) {
        add(listBox, el("p", "muted", (data.items || []).length ? "Không ảnh nào khớp bộ lọc." : "Chưa có ảnh nào."));
        return;
      }
      var grid = put(listBox, el("div", "rf-grid"));
      items.forEach(function (x) {
        var card = put(grid, el("div", "rf-card"));
        var img = put(card, el("img", "rf-img"));
        img.src = data.dir + encodeURIComponent(x.file);
        img.alt = x.note ? x.note.slice(0, 120) : "Ảnh tham chiếu " + x.id;
        img.loading = "lazy";
        img.tabIndex = 0;
        var zoom = function () {
          var box = (window.CORESIMPLE_VIEWS || {}).lightbox;
          if (box) box(img.src, x.note || x.id);
        };
        img.addEventListener("click", zoom);
        img.addEventListener("keydown", function (e) { if (e.key === "Enter") zoom(); });
        var note = put(card, el("textarea"));
        note.value = x.note;
        note.placeholder = "Nhận định của bạn: cái gì hay (vị trí nút, cỡ, khoảng thở, màu…), cái gì không theo.";
        note.setAttribute("aria-label", "Nhận định về ảnh " + x.id);
        var row = put(card, el("div", "rf-row"));
        var kind = put(row, el("select"));
        kind.setAttribute("aria-label", "Loại ảnh");
        data.kinds.forEach(function (k) {
          var o = put(kind, el("option", "", k));
          o.value = k;
          if (k === x.kind) o.selected = true;
        });
        var tags = put(row, el("input"));
        tags.placeholder = "tag, cách nhau dấu phẩy (HUD trận, popup thưởng…)";
        tags.setAttribute("aria-label", "Tag của ảnh");
        tags.value = x.tags.join(", ");
        var foot = put(card, el("div", "rf-row"));
        var saved = put(foot, el("span", "rf-saved", x.at ? "Nạp lúc " + x.at.replace("T", " ") : ""));
        saved.style.flex = "1";
        var del = put(foot, button("Xoá", function () {
          if (del.dataset.armed !== "1") { del.dataset.armed = "1"; del.textContent = "Bấm lần nữa để xoá"; return; }
          api("/api/references/delete", { id: x.id }).then(function (res) {
            if (!res.ok) { say("Không xoá được: " + res.error, true); return; }
            data = res.data;
            say("Đã xoá ảnh " + x.id + ".");
            renderList();
          }, function (e) { say("Lỗi gọi server: " + e.message, true); });
        }, "danger"));
        if (!ctx.setup) { note.readOnly = true; kind.disabled = true; tags.readOnly = true; del.disabled = true; return; }
        // Gõ là lưu (sau một nhịp ngừng), như ô phản hồi ảnh art.
        var timer = null;
        var save = function () {
          var list = (tags.value || "").split(",").map(function (t) { return t.trim(); }).filter(Boolean);
          saved.textContent = "Đang lưu…";
          api("/api/references/note", { id: x.id, note: note.value || "", kind: kind.value, tags: list }).then(function (res) {
            if (!res.ok) { saved.textContent = "Không lưu: " + res.error; return; }
            data = res.data;
            x.note = note.value || "";
            x.kind = kind.value;
            x.tags = list;
            saved.textContent = "Đã lưu";
          }, function (e) { saved.textContent = "Lỗi gọi server: " + e.message; });
        };
        var later = function () { clearTimeout(timer); timer = setTimeout(save, 700); };
        note.addEventListener("input", later);
        tags.addEventListener("input", later);
        kind.addEventListener("change", save);
      });
    };

    renderTop();
    renderList();
  };

  window.CORESIMPLE_VIEWS = window.CORESIMPLE_VIEWS || {};
  window.CORESIMPLE_VIEWS.references = build;
})();
