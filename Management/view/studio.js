// Tab Studio của trang quản lý: order ảnh, art và model 3D, số lượt đã dùng, hàng chưa chuẩn và phiếu nợ art.
// Code thuần, không AI, không thư viện. Dữ liệu: window.CORESIMPLE_STUDIO (data/studio.js, do
// Management/tools/sync_page.py sinh từ studio.py). Mở qua setup_server.py thì đóng được phiếu nợ.
// index.html gọi: CORESIMPLE_VIEWS.studio(pane, { el, add, put, section, setup }).
(function () {
  "use strict";

  var CSS = [
    ".st-limits { color: var(--muted); font-size: 13.5px; margin: 0 0 16px; }",
    ".st-cards { display: grid; grid-template-columns: repeat(auto-fill, minmax(260px, 1fr)); gap: 12px; margin: 0 0 20px; }",
    ".st-card { border: 1px solid var(--line); border-left: 4px solid var(--bad); border-radius: 8px; padding: 10px 12px; background: var(--panel); }",
    ".st-card.closed { border-left-color: var(--line); opacity: .8; }",
    ".st-card img { display: block; width: 100%; max-height: 150px; object-fit: contain; background: var(--bg); border: 1px solid var(--line); border-radius: 6px; margin: 0 0 8px; cursor: zoom-in; }",
    ".st-card h4 { margin: 0 0 4px; font-size: 14.5px; }",
    ".st-card p { margin: 0 0 6px; font-size: 13px; color: var(--muted); overflow-wrap: anywhere; }",
    ".st-card .st-reason { color: var(--text); }",
    ".st-tag { display: inline-block; padding: 1px 8px; border-radius: 10px; font-size: 12px; border: 1px solid var(--line); color: var(--muted); margin-right: 6px; }",
    ".st-tag.bad { border-color: var(--bad); color: var(--bad); }",
    ".st-tag.ok { border-color: var(--ok); color: var(--ok); }",
    ".st-tag.wait { border-color: var(--accent); color: var(--accent); }",
    ".st-group { margin: 0 0 18px; }",
    ".st-order { border-bottom: 1px solid var(--line); padding: 10px 0; }",
    ".st-order h4 { margin: 0 0 4px; font-size: 14.5px; }",
    ".st-order .st-meta { font-size: 12.5px; color: var(--muted); margin: 0 0 6px; overflow-wrap: anywhere; }",
    ".st-items { margin: 0; padding-left: 18px; font-size: 13.5px; }",
    ".st-items li { margin: 2px 0; }",
    ".st-items img { width: 40px; height: 40px; object-fit: contain; vertical-align: middle; margin-right: 6px; border: 1px solid var(--line); border-radius: 4px; background: var(--bg); cursor: zoom-in; }",
    ".st-btn { padding: 5px 12px; border: 1px solid var(--line); border-radius: 6px; background: var(--panel); color: var(--text); font: inherit; font-size: 13px; cursor: pointer; }",
    ".st-btn:hover { border-color: var(--accent); }"
  ].join("\n");

  var STATES = [
    { key: "new", label: "Mới, chưa làm" },
    { key: "doing", label: "Tool đang làm" },
    { key: "review", label: "Đã giao, còn ảnh chưa duyệt" },
    { key: "redo", label: "Có item bị chấm Làm lại ngay" },
    { key: "done", label: "Xong: mọi item đã chấm, không item nào phải làm lại" }
  ];
  var IMAGE = /\.(png|jpe?g|webp)$/i;

  var styled = false;
  var addStyle = function () {
    if (styled) return;
    styled = true;
    var s = document.createElement("style");
    s.textContent = CSS;
    document.head.appendChild(s);
  };

  var build = function (pane, ctx) {
    addStyle();
    var el = ctx.el, add = ctx.add, put = ctx.put;
    var data = window.CORESIMPLE_STUDIO || null;
    var message = null;
    var zoom = function (img, caption) {
      var box = (window.CORESIMPLE_VIEWS || {}).lightbox;
      img.title = "Bấm để xem to";
      img.addEventListener("click", function () { if (box) box(img.src, caption); else window.open(img.src, "_blank"); });
      return img;
    };
    var picture = function (file, caption) {
      var img = el("img");
      img.src = "../" + file.split("/").map(encodeURIComponent).join("/");
      img.alt = caption;
      img.loading = "lazy";
      return zoom(img, caption);
    };
    var api = function (method, path, body) {
      return fetch(path, {
        method: method,
        headers: { "Content-Type": "application/json", "X-Setup-Token": ctx.setup.token },
        body: body ? JSON.stringify(body) : undefined,
        cache: "no-store"
      }).then(function (r) { if (!r.ok) throw new Error("HTTP " + r.status); return r.json(); });
    };

    var render = function () {
      pane.textContent = "";
      add(pane, el("h1", "", "Studio"));
      add(pane, el("p", "sub", "Order ảnh, art và model 3D cho @Codex và @Blender. Lượt gen có trần; hết lượt thì tool giao bản gần order nhất với nhãn chưa chuẩn và mở phiếu nợ art."));
      if (!data) {
        add(pane, el("p", "", "Chưa có dữ liệu studio. Chạy: python Management/tools/sync_page.py"));
        return;
      }
      var lim = data.limits || {};
      add(pane, el("p", "st-limits", "Trần: mỗi item " + lim.first + " lượt đầu; mỗi lần bị chấm \"Làm lại ngay\" kèm phản hồi được thêm " +
        lim.redo + " lượt, tối đa " + lim.maxRedos + " lần mỗi item. Art: tối đa " + lim.artDaily +
        " lượt gen mỗi ngày. Ảnh concept chấm ở tab Game design; feature chấm bằng studio.py review. Đọc lúc " + (data.generatedAt || "") + "."));
      if (message) add(pane, el("p", message.bad ? "bad-text" : "ok-text", message.text));
      if (ctx.setup) {
        var reload = put(pane, el("button", "st-btn", "Tải lại"));
        reload.type = "button";
        reload.onclick = function () {
          api("GET", "/api/studio").then(function (d) { data = d; message = null; render(); },
            function (e) { message = { bad: true, text: "Không tải lại được: " + e.message }; render(); });
        };
      }

      // Phiếu nợ đang mở: trên cùng, vì đây là thứ cần quyết.
      var debts = data.debts || [];
      var open = debts.filter(function (d) { return d.state === "open"; });
      add(pane, el("h2", "", "Phiếu nợ art đang mở (" + open.length + ")"));
      if (!open.length) add(pane, el("p", "st-limits", "Không có phiếu nợ nào đang mở."));
      var cards = put(pane, el("div", "st-cards"));
      var card = function (box, d) {
        var c = put(box, el("div", "st-card" + (d.state === "open" ? "" : " closed")));
        if (d.file && IMAGE.test(d.file)) add(c, picture(d.file, d.owner + " · " + d.item + " · v" + d.version));
        add(c, el("h4", "", d.owner + " · " + d.item));
        var tags = put(c, el("p"));
        add(tags, el("span", "st-tag bad", d.kind), el("span", "st-tag", d.tool), el("span", "st-tag", d.id));
        add(c, el("p", "st-reason", d.reason || "(không ghi lý do)"));
        add(c, el("p", "", d.ref + " · v" + d.version + " · " + d.at));
        if (d.state !== "open") add(c, el("p", "", "Đóng: " + d.closedBy + " · " + d.closedAt));
        if (d.state === "open" && ctx.setup) {
          var b = put(c, el("button", "st-btn", "Chấp nhận nợ"));
          b.type = "button";
          b.title = "Đóng phiếu: dùng hàng này như hiện có, không làm lại";
          b.onclick = function () {
            api("POST", "/api/studio/debt-close", { id: d.id }).then(function (res) {
              if (!res.ok) { message = { bad: true, text: res.error }; render(); return; }
              data = res.studio; message = { bad: false, text: res.message }; render();
            }, function (e) { message = { bad: true, text: "Lỗi gọi server: " + e.message }; render(); });
          };
        }
      };
      open.forEach(function (d) { card(cards, d); });
      if (open.length && !ctx.setup) add(pane, el("p", "st-limits", "Đóng phiếu bằng tay cần mở trang qua server Setup (python Management/tools/setup_server.py), hoặc chạy: python Management/tools/studio.py debt close <mã phiếu>."));

      // Order theo trạng thái.
      var orders = data.orders || [];
      add(pane, el("h2", "", "Order (" + orders.length + ")"));
      if (!orders.length) add(pane, el("p", "st-limits", "Chưa có order nào."));
      STATES.forEach(function (s) {
        var list = orders.filter(function (o) { return o.state === s.key; });
        if (!list.length) return;
        var group = put(pane, el("div", "st-group"));
        add(group, el("h3", "", s.label + " (" + list.length + ")"));
        list.forEach(function (o) {
          var row = put(group, el("div", "st-order"));
          add(row, el("h4", "", o.ref + " · " + o.title));
          var meta = put(row, el("p", "st-meta"));
          add(meta, el("span", "st-tag", o.tool + " · " + o.kind));
          if (o.version) add(meta, el("span", "st-tag", "đã giao v" + o.version));
          if (o.design && o.state === "review") add(meta, el("span", "st-tag wait", "chờ bạn chấm (tab Game design, node " + o.node + ")"));
          var ul = put(row, el("ul", "st-items"));
          o.items.forEach(function (it) {
            var li = put(ul, el("li"));
            if (it.file && IMAGE.test(it.file)) add(li, picture(it.file, o.ref + " · " + it.id));
            add(li, el("span", "", it.id + ": đã chạy " + it.used + " lượt" + (it.left ? ", còn " + it.left : "") +
              ", làm lại " + it.round + "/" + o.maxRedos + " "));
            if (o.version) add(li, el("span", "st-tag" + (it.level === 3 ? " ok" : it.level === 0 ? " bad" : it.level === null ? " wait" : ""), it.levelName));
            if (it.feedback) add(li, el("span", "", " phản hồi: " + it.feedback + " "));
            if (it.level === 0 && !it.redo && it.why) add(li, el("span", "st-tag bad", "chưa làm lại được: " + it.why));
            if (it.ok === false) add(li, el("span", "st-tag bad", "tool tự kiểm chưa đạt: " + it.note));
          });
          o.problems.forEach(function (p) { add(row, el("p", "bad-text", "Lỗi order: " + p)); });
        });
      });

      var shut = debts.filter(function (d) { return d.state !== "open"; });
      if (shut.length) {
        var more = put(pane, el("details"));
        add(more, el("summary", "", "Phiếu nợ đã đóng (" + shut.length + ")"));
        var closedCards = put(more, el("div", "st-cards"));
        shut.forEach(function (d) { card(closedCards, d); });
      }
    };
    render();
  };

  window.CORESIMPLE_VIEWS = window.CORESIMPLE_VIEWS || {};
  window.CORESIMPLE_VIEWS.studio = build;
})();
