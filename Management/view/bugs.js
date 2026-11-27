// Tab "Bug" của trang quản lý (người quyết 2026-11-27: một nơi lưu bug để sửa sau). Ghi bug không giao ai;
// "Giao sửa" mới gửi bug vào hộp thư của feature, server làm thành một việc worker. Dữ liệu: bugs.py qua /api/bugs.
// index.html gọi: CORESIMPLE_VIEWS.bugs(pane, { el, add, put, section, setup }).
(function () {
  "use strict";

  var CSS = [
    ".bg-box { padding: 12px 14px; border: 1px solid var(--line); border-radius: 10px; background: var(--panel); margin: 0 0 14px; }",
    ".bg-row { display: flex; flex-wrap: wrap; gap: 8px 10px; align-items: center; margin: 0 0 8px; }",
    ".bg-box input, .bg-box select, .bg-box textarea { padding: 6px 9px; border: 1px solid var(--line); border-radius: 6px; background: var(--bg); color: var(--text); font: inherit; font-size: 14px; }",
    ".bg-box textarea { width: 100%; min-height: 64px; resize: vertical; }",
    ".bg-btn { padding: 5px 11px; border: 1px solid var(--line); border-radius: 6px; background: var(--panel); color: var(--text); font: inherit; font-size: 13px; cursor: pointer; }",
    ".bg-btn:hover { border-color: var(--accent); }",
    ".bg-list { list-style: none; margin: 0; padding: 0; }",
    ".bg-list li { padding: 8px 0; border-bottom: 1px solid var(--line); }",
    ".bg-list li:last-child { border-bottom: 0; }",
    ".bg-head { display: flex; flex-wrap: wrap; gap: 6px 10px; align-items: baseline; }",
    ".bg-head .bg-acts { margin-left: auto; display: flex; gap: 6px; }",
    ".bg-text { margin: 4px 0 0; white-space: pre-wrap; word-break: break-word; }",
    ".bg-st { font-weight: 600; }",
    ".bg-st.open { color: var(--bad); }",
    ".bg-st.fixing { color: var(--accent); }",
    ".bg-st.fixed { color: var(--ok); }",
    ".bg-st.dropped { color: var(--muted); font-weight: 400; }",
    ".bg-feats label { display: inline-flex; gap: 4px; align-items: center; margin-right: 10px; font-size: 13.5px; }"
  ].join("\n");

  var build = function (pane, ctx) {
    var el = ctx.el, add = ctx.add, put = ctx.put;
    if (!document.getElementById("bg-css")) {
      var st = document.createElement("style");
      st.id = "bg-css";
      st.textContent = CSS;
      document.head.appendChild(st);
    }
    add(pane, el("h1", "", "Bug"));
    if (!ctx.setup) {
      add(pane, el("p", "muted", "Tab này chỉ có khi mở trang qua python Management/tools/setup_server.py."));
      return;
    }
    add(pane, el("p", "sub", "Bug để sửa sau. Ghi vào đây không giao cho ai. Bấm \"Giao sửa\" thì bug vào hộp thư của feature đầu tiên và server làm thành một việc worker. " +
      "Leader và tester ghi bằng python Management/tools/bugs.py add."));
    var msgBox = put(pane, el("p", ""));
    var formBox = put(pane, el("div", "bg-box"));
    var listBox = put(pane, el("div", "bg-box"));
    var data = null, form = { text: "", seen: "", features: {} }, showClosed = false;

    var say = function (text, bad) { msgBox.textContent = text || ""; msgBox.className = text ? (bad ? "bad-text" : "ok-text") : ""; };
    var button = function (label, onClick) {
      var b = el("button", "bg-btn", label);
      b.type = "button";
      b.addEventListener("click", onClick);
      return b;
    };
    var get = function () {
      return fetch("/api/bugs", { headers: { "X-Setup-Token": ctx.setup.token }, cache: "no-store" }).then(function (r) {
        if (!r.ok) throw new Error("HTTP " + r.status);
        return r.json();
      });
    };
    var post = function (action, body) {
      return fetch("/api/bugs/" + action, {
        method: "POST", cache: "no-store",
        headers: { "Content-Type": "application/json", "X-Setup-Token": ctx.setup.token },
        body: JSON.stringify(body)
      }).then(function (r) {
        if (!r.ok) throw new Error("HTTP " + r.status);
        return r.json();
      }).then(function (res) {
        if (!res.ok) throw new Error(res.error || "lỗi");
        refresh();
        return res;
      });
    };
    var when = function (t) {
      var d = new Date(t * 1000), p = function (n) { return (n < 10 ? "0" : "") + n; };
      return p(d.getDate()) + "/" + p(d.getMonth() + 1) + " " + p(d.getHours()) + ":" + p(d.getMinutes());
    };

    var renderForm = function () {
      formBox.textContent = "";
      add(formBox, el("b", "", "Ghi bug mới"));
      var ta = put(formBox, el("textarea", ""));
      ta.placeholder = "Thấy gì, mong gì (ví dụ: chọn state Giữa trận chỉ ra màn bắt đầu; mong vào thẳng trận)";
      ta.value = form.text;
      ta.addEventListener("input", function () { form.text = ta.value; });
      var feats = put(formBox, el("div", "bg-row bg-feats"));
      add(feats, el("span", "muted", "Feature:"));
      (data.features || []).forEach(function (f) {
        var lab = put(feats, el("label"));
        var cb = put(lab, el("input"));
        cb.type = "checkbox";
        cb.checked = !!form.features[f];
        cb.addEventListener("change", function () { form.features[f] = cb.checked; });
        add(lab, document.createTextNode(f));
      });
      var row = put(formBox, el("div", "bg-row"));
      var seen = put(row, el("input", ""));
      seen.placeholder = "thấy ở đâu (không bắt buộc: đợt chơi thử, màn...)";
      seen.style.flex = "1";
      seen.value = form.seen;
      seen.addEventListener("input", function () { form.seen = seen.value; });
      add(row, button("Ghi bug", function () {
        var names = Object.keys(form.features).filter(function (k) { return form.features[k]; });
        if (!form.text.trim()) { say("Viết bug trước.", true); return; }
        if (!names.length) { say("Chọn ít nhất một feature.", true); return; }
        post("add", { text: form.text, features: names.join(","), seen: form.seen }).then(function (res) {
          form = { text: "", seen: "", features: {} };
          renderForm();
          say(res.new ? "Đã ghi " + res.id + ": chờ sửa, chưa giao ai." : "Bug này đã có (" + res.id + ").");
        }, function (e) { say(e.message, true); });
      }));
    };

    var renderList = function () {
      listBox.textContent = "";
      var waiting = data.bugs.filter(function (b) { return b.state === "open" || b.state === "fixing"; });
      var closed = data.bugs.filter(function (b) { return b.state === "fixed" || b.state === "dropped"; });
      var head = put(listBox, el("div", "bg-row"));
      add(head, el("b", "", "Chờ sửa và đang sửa (" + waiting.length + ")"));
      if (closed.length) {
        add(head, button(showClosed ? "Ẩn bug đã đóng" : "Hiện bug đã đóng (" + closed.length + ")", function () { showClosed = !showClosed; renderList(); }));
      }
      var shown = showClosed ? waiting.concat(closed) : waiting;
      if (!shown.length) { add(listBox, el("p", "muted", "Không có bug nào chờ sửa.")); return; }
      var ul = put(listBox, el("ul", "bg-list"));
      shown.forEach(function (b) {
        var li = put(ul, el("li"));
        var h = put(li, el("div", "bg-head"));
        add(h, el("span", "bg-st " + b.state, data.states[b.state] || b.state), el("b", "", b.features.join(", ")),
          el("span", "muted", b.id + " · " + when(b.created) + " · từ " + b.source));
        var acts = put(h, el("span", "bg-acts"));
        if (b.state === "open") {
          add(acts, button("Giao sửa", function () {
            post("fix", { id: b.id }).then(function (res) { say("Đã giao " + b.id + " cho worker " + b.features[0] + " (tin " + res.message + ")."); },
              function (e) { say(e.message, true); });
          }));
        }
        if (b.state === "fixing") {
          add(acts, button("Đã sửa", function () {
            var note = window.prompt("Đã sửa ra sao (việc worker nào, test nào)?", "");
            if (note) post("done", { id: b.id, note: note }).then(function () { say("Đã đóng " + b.id + "."); }, function (e) { say(e.message, true); });
          }));
        }
        if (b.state === "open" || b.state === "fixing") {
          add(acts, button("Bỏ", function () {
            var note = window.prompt("Lí do bỏ bug này?", "");
            if (note) post("drop", { id: b.id, note: note }).then(function () { say("Đã bỏ " + b.id + "."); }, function (e) { say(e.message, true); });
          }));
        }
        add(li, el("p", "bg-text", b.text));
        if (b.seen) add(li, el("div", "muted", "Thấy ở: " + b.seen));
        if (b.message) add(li, el("div", "muted", "Tin giao sửa: " + b.message));
        if (b.note) add(li, el("div", "muted", "Kết quả: " + b.note));
      });
    };

    var refresh = function () {
      return get().then(function (v) {
        var first = !data;
        data = v;
        if (first) renderForm();
        renderList();
      }, function (e) { say("Không tải được: " + e.message, true); });
    };
    refresh();
  };

  window.CORESIMPLE_VIEWS = window.CORESIMPLE_VIEWS || {};
  window.CORESIMPLE_VIEWS.bugs = build;
})();
