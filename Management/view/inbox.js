// Tab "Hộp thư" của trang quản lý: tin gửi cho các thread chạy theo việc (feature, service, @Core, tool). Courier của
// setup_server.py giao từng tin thành một lượt của thread đó (bật thread lên với tin làm prompt), chỉ coi là đã giao khi
// mã tin có trong transcript. Ở đây: xem tin, gửi tin (của người), huỷ, thử lại, đổi chế độ và trần.
// Dữ liệu chỉ có khi mở trang qua setup_server.py (GET /api/inbox); mở thẳng từ file thì chỉ báo.
// index.html gọi: CORESIMPLE_VIEWS.inbox(pane, { el, add, put, section, setup }).
(function () {
  "use strict";

  var CSS = [
    ".ib-box { padding: 14px 16px; border: 1px solid var(--line); border-radius: 10px; background: var(--panel); margin: 0 0 16px; }",
    ".ib-box h2 { margin: 0 0 10px; font-size: 16px; }",
    ".ib-row { display: flex; flex-wrap: wrap; gap: 8px 12px; align-items: center; margin: 0 0 10px; }",
    ".ib-row label { display: inline-flex; gap: 6px; align-items: center; font-size: 14px; }",
    ".ib-box input, .ib-box select, .ib-box textarea { padding: 6px 9px; border: 1px solid var(--line); border-radius: 6px; background: var(--bg); color: var(--text); font: inherit; font-size: 14px; }",
    ".ib-box input[type=number] { width: 72px; }",
    ".ib-box textarea { width: 100%; min-height: 90px; resize: vertical; }",
    ".ib-btn { padding: 6px 12px; border: 1px solid var(--line); border-radius: 6px; background: var(--panel); color: var(--text); font: inherit; font-size: 13.5px; cursor: pointer; }",
    ".ib-btn:hover { border-color: var(--accent); }",
    ".ib-btn:disabled { opacity: .5; cursor: default; }",
    ".ib-btn[aria-pressed='true'] { background: var(--accent); border-color: var(--accent); color: var(--panel); font-weight: 600; }",
    ".ib-btn:focus-visible { outline: 2px solid var(--accent); outline-offset: 2px; }",
    ".ib-pools { display: flex; flex-wrap: wrap; gap: 10px; margin: 0 0 16px; }",
    ".ib-pool { flex: 1; min-width: 200px; padding: 10px 14px; border: 1px solid var(--line); border-radius: 10px; background: var(--panel); }",
    ".ib-pool b { font-size: 22px; }",
    ".ib-reqs { max-height: 180px; overflow: auto; border: 1px solid var(--line); border-radius: 6px; margin: 0 0 10px; }",
    ".ib-reqs button { display: block; width: 100%; text-align: left; padding: 5px 9px; border: 0; border-bottom: 1px solid var(--line); background: transparent; color: var(--text); font: inherit; font-size: 13px; cursor: pointer; }",
    ".ib-reqs button:hover { background: var(--bg); }",
    ".ib-table { width: 100%; border-collapse: collapse; font-size: 13.5px; }",
    ".ib-table th, .ib-table td { padding: 6px 8px; border-bottom: 1px solid var(--line); text-align: left; vertical-align: top; }",
    ".ib-table td.ib-text { white-space: pre-wrap; word-break: break-word; max-width: 520px; }",
    ".ib-state { font-weight: 600; }",
    ".ib-state.queued, .ib-state.waiting, .ib-state.delivering { color: var(--accent); }",
    ".ib-state.delivered { color: var(--ok, inherit); }",
    ".ib-state.error { color: var(--bad); }",
    ".ib-state.cancelled { color: var(--muted); }",
    ".ib-who { display: flex; align-items: center; gap: 6px; margin-top: 4px; font-size: 13px; }",
    ".ib-who .ib-btn { margin-left: auto; padding: 1px 8px; }",
    ".ib-wrap { overflow-x: auto; }"
  ].join("\n");

  var STATE_NAMES = { queued: "chờ giao", waiting: "đang chờ", delivering: "đang giao", delivered: "đã giao",
                      error: "lỗi", cancelled: "đã huỷ" };
  var MODE_HELP = {
    "always-live": "Thread không bị tự tắt. Hộp thư vẫn giao tin: thread đang tắt thì bật lên với tin đó, nhưng chỉ khi nhóm còn dưới trần (giao nhiều tin cùng lúc không bật hàng chục thread).",
    "on-demand": "Thread feature và tool chỉ chạy khi có việc: rảnh đủ lâu thì tự tắt; mỗi nhóm chạy cùng lúc tối đa theo trần (chat bạn mở tính vào). Leader và tester không bị đụng."
  };
  var REFRESH_MS = 5000;

  var build = function (pane, ctx) {
    var el = ctx.el, add = ctx.add, put = ctx.put;
    if (!document.getElementById("ib-css")) {
      var st = document.createElement("style");
      st.id = "ib-css";
      st.textContent = CSS;
      document.head.appendChild(st);
    }
    add(pane, el("h1", "", "Hộp thư"));
    add(pane, el("p", "sub", "Tin cho feature, service, @Core thành một việc worker (người quyết 2026-11-27): một lượt trong git worktree riêng, " +
      "sửa được mọi feature, qua cổng kiểm thì tự nạp, server biên dịch, chạy test và ghi done. Tin cho @UnityAutoPlay là một đợt chơi thử, chạy khi tới phiếu Editor. " +
      "Tin cho @DesignAdvisor vẫn là một lượt của thread đó. Tối đa 3 worker cùng lúc."));
    if (!ctx.setup) {
      add(pane, el("p", "muted", "Hộp thư chỉ có khi mở trang qua python Management/tools/setup_server.py (server cũng là bên giao tin)."));
      return;
    }
    var msgBox = put(pane, el("p", ""));
    var workBox = put(pane, el("div", "ib-box"));
    var poolsBox = put(pane, el("div", "ib-pools"));
    var cfgBox = put(pane, el("div", "ib-box"));
    var sendBox = put(pane, el("div", "ib-box"));
    var listBox = put(pane, el("div", "ib-box"));
    var armed = false;      // "Xoá cả hộp thư" phải bấm hai lần
    var workers = null, workForm = { feature: "", task: "", model: "", effort: "" };
    var data = null, sending = false, form = { target: "", text: "" }, reqs = { target: "", items: [], note: "" };

    var say = function (text, bad) {
      msgBox.textContent = text || "";
      msgBox.className = text ? (bad ? "bad-text" : "ok-text") : "";
    };
    var button = function (label, onClick) {
      var b = el("button", "ib-btn", label);
      b.type = "button";
      b.addEventListener("click", onClick);
      return b;
    };
    var get = function (path) {
      return fetch(path, { headers: { "X-Setup-Token": ctx.setup.token }, cache: "no-store" }).then(function (r) {
        if (!r.ok) throw new Error("HTTP " + r.status);
        return r.json();
      });
    };
    var post = function (action, body) {
      return fetch("/api/inbox/" + action, {
        method: "POST", cache: "no-store",
        headers: { "Content-Type": "application/json", "X-Setup-Token": ctx.setup.token },
        body: JSON.stringify(body)
      }).then(function (r) {
        if (!r.ok) throw new Error("HTTP " + r.status);
        return r.json();
      }).then(function (res) {
        if (!res.ok) throw new Error(res.error || "lỗi");
        if (res.data) { data = res.data; renderAll(); }
        return res;
      });
    };
    var when = function (t) {
      if (!t) return "";
      var d = new Date(t * 1000), p = function (n) { return (n < 10 ? "0" : "") + n; };
      return p(d.getDate()) + "/" + p(d.getMonth() + 1) + " " + p(d.getHours()) + ":" + p(d.getMinutes());
    };

    var postWorker = function (action, body) {
      return fetch("/api/workers/" + action, {
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
    var STATE_NAMES = { queued: "chờ chỗ", starting: "đang dựng worktree", running: "đang chạy", "waiting-editor": "chờ phiếu Editor",
      applied: "đã nạp", "applied-partial": "đã nạp, còn dở", "applied-blocked": "chờ người trả lời", "checks-failed": "hỏng cổng kiểm",
      drift: "lệch project chính", "needs-codelead": "cần CodeLead", interrupted: "bị ngắt (server khởi động lại)", error: "lỗi",
      played: "đã chơi thử", "play-failed": "chơi thử lỗi", dropped: "đã gỡ" };
    var renderWorkers = function () {
      workBox.textContent = "";
      if (!workers) return;
      var head = put(workBox, el("div", ""));
      var running = workers.jobs.filter(function (j) { return j.running; }).length;
      add(head, el("b", "", "Worker"), el("span", "muted", " · " + running + " / " + workers.slots + " đang chạy"));
      // Mặc định: model của mọi việc worker không ghi model riêng (tin hộp thư, việc Leader giao). Đổi là lưu ngay.
      var dflt = workers.defaults || { model: "opus", effort: "medium" };
      var pick = function (values, value, onChange) {
        var s = el("select", "");
        values.forEach(function (v) { var o = put(s, el("option", "", v)); o.value = v; if (v === value) o.selected = true; });
        s.addEventListener("change", function () { onChange(s.value); });
        return s;
      };
      var saveDefault = function (model, effort) {
        postWorker("defaults", { model: model, effort: effort }).then(function () { say("Mặc định của worker: " + model + " · " + effort + "."); },
                                                                       function (e) { say(e.message, true); });
      };
      var drow = put(workBox, el("div", "ib-row"));
      add(drow, el("span", "", "Mặc định cho mọi việc worker:"),
        pick(workers.models || [], dflt.model, function (v) { saveDefault(v, dflt.effort); }),
        pick(workers.efforts || [], dflt.effort, function (v) { saveDefault(dflt.model, v); }),
        el("span", "muted", "(tin hộp thư và việc Leader giao; chơi thử vẫn sonnet · medium)"));
      var row = put(workBox, el("div", "ib-row"));
      var sel = put(row, el("select", ""));
      add(sel, el("option", "", "chọn feature…"));
      ((data && data.targets) || []).filter(function (t) { return t.pool === "feature"; }).forEach(function (t) {
        var o = put(sel, el("option", "", t.target));
        o.value = t.target;
        if (t.target === workForm.feature) o.selected = true;
      });
      sel.addEventListener("change", function () { workForm.feature = sel.value; });
      var ta = put(row, el("input", ""));
      ta.placeholder = "việc thêm (không bắt buộc: worker làm mọi yêu cầu mở của feature)";
      ta.value = workForm.task;
      ta.style.flex = "1";
      ta.addEventListener("input", function () { workForm.task = ta.value; });
      add(row, pick(["(mặc định)"].concat(workers.models || []), workForm.model || "(mặc định)", function (v) { workForm.model = v === "(mặc định)" ? "" : v; }),
        pick(["(mặc định)"].concat(workers.efforts || []), workForm.effort || "(mặc định)", function (v) { workForm.effort = v === "(mặc định)" ? "" : v; }));
      add(row, button("Làm bằng worker", function () {
        if (!workForm.feature) { say("Chọn feature trước.", true); return; }
        postWorker("run", { feature: workForm.feature, task: workForm.task, model: workForm.model, effort: workForm.effort }).then(function (res) { workForm.task = ""; say("Đã xếp việc " + res.id + "."); },
                                                                                   function (e) { say(e.message, true); });
      }));
      if (!workers.jobs.length) { add(workBox, el("p", "muted", "Chưa có việc worker nào.")); return; }
      var tbl = put(workBox, el("table", ""));
      var hr = put(put(tbl, el("thead", "")), el("tr", ""));
      ["Lúc tạo", "Việc", "Loại", "Feature", "Model", "Trạng thái", "Yêu cầu", "Lượt", "$", "Tóm tắt / câu hỏi", ""].forEach(function (h) { add(hr, el("th", "", h)); });
      var tb = put(tbl, el("tbody", ""));
      workers.jobs.forEach(function (j) {
        var tr = put(tb, el("tr", ""));
        var state = (STATE_NAMES[j.state] || j.state || "") + (j.running ? " ●" : "") + (j.pending ? " · sắp chạy tiếp" : "");
        [when(j.created), j.id, j.kind === "play" ? "chơi thử" : "code", j.feature,
         (j.model || "") + (j.effort ? " · " + j.effort : ""), state, String(j.requirements || 0), String(j.turns || 0),
         j.cost ? j.cost.toFixed(2) : ""].forEach(function (x) { add(tr, el("td", "", x)); });
        add(tr, el("td", "", (j.question ? "HỎI: " + j.question + " · " : "") + (j.summary || "")));
        var act = put(tr, el("td", ""));
        if (!j.running && j.kind !== "play" && ["applied", "dropped", "queued"].indexOf(j.state) < 0) {
          add(act, button("Chạy tiếp", function () {
            var msg = window.prompt("Lời nhắn cho worker (để trống: làm tiếp việc đang dở)", "") || "";
            postWorker("resume", { id: j.id, message: msg }).then(function () { say("Việc " + j.id + " sẽ chạy tiếp ở vòng sau."); }, function (e) { say(e.message, true); });
          }));
        }
        if (!j.running && j.state !== "dropped") {
          add(act, button("Gỡ", function () {
            postWorker("drop", { id: j.id }).then(function () { say("Đã gỡ " + j.id + "."); }, function (e) { say(e.message, true); });
          }));
        }
      });
    };

    var renderPools = function () {
      poolsBox.textContent = "";
      ["tool"].forEach(function (pool) {      // features and the tester are workers now (khung Worker ở trên)
        var p = data.pools[pool], on = data.config.mode === "on-demand";
        var card = put(poolsBox, el("div", "ib-pool"));
        add(card, el("div", "muted", pool === "feature" ? "Feature, service, @Core" : "Tool"));
        var line = put(card, el("div", ""));
        add(line, el("b", "", String(p.running)), el("span", "", " / " + p.limit + " đang chạy" + (on ? "" : " (trần khi bật thread mới)")));
        if (p.open_chats) add(card, el("div", "muted", p.open_chats + " là chat bạn đang mở"));
        // Ai đang chiếm chỗ: tên thread, chạy ở đâu, đang làm gì, đã im bao lâu. Thread nền có nút Tắt (thread không mất;
        // tin trong hộp thư sẽ bật nó lại).
        (p.threads || []).slice().sort(function (a, b) { return a.idle - b.idle; }).forEach(function (t) {
          var row = put(card, el("div", "ib-who"));
          var bg = t.kind === "background";
          var doing = t.hung ? "TREO: " + t.hung : t.state === "blocked" ? "chờ người" : t.status === "busy" ? "đang chạy một lượt" : "rảnh";
          var quiet = t.idle >= 3600 ? Math.floor(t.idle / 3600) + " giờ" : t.idle >= 60 ? Math.floor(t.idle / 60) + " phút" : t.idle + " giây";
          add(row, el("b", "", t.title), el("span", t.hung ? "bad-text" : "muted", " · " + (bg ? "nền" : "VS Code") + " · " + doing + (t.hung ? "" : " · im " + quiet) +
            (t.inbox ? " · ✉ " + t.inbox : "")));
          if (bg) {
            var off = put(row, button("Tắt", function () {
              off.disabled = true;
              fetch("/api/sessions/stop", { method: "POST", cache: "no-store",
                headers: { "Content-Type": "application/json", "X-Setup-Token": ctx.setup.token }, body: JSON.stringify({ id: t.id }) })
                .then(function (r) { return r.json(); })
                .then(function (res) { say(res.ok ? "Đã tắt phiên nền " + t.title + "." : "Không tắt được " + t.title + ": " + (res.error || ""), !res.ok); return refresh(); },
                      function (e) { say("Không tắt được " + t.title + ": " + e.message, true); });
            }));
            off.title = "Tắt phiên nền này để nhường chỗ. Thread không mất; tin trong hộp thư sẽ bật nó lại.";
          }
        });
        if (!(p.threads || []).length) add(card, el("div", "muted", "không thread nào đang chạy"));
      });
    };

    var renderConfig = function () {
      cfgBox.textContent = "";
      add(cfgBox, el("h2", "", "Chế độ"));
      var row = put(cfgBox, el("div", "ib-row"));
      data.modes.forEach(function (m) {
        var b = put(row, button(m === "on-demand" ? "Theo nhu cầu (on-demand)" : "Luôn live (always-live)", function () {
          if (m === data.config.mode) return;
          post("config", { mode: m }).then(function () { say("Đã chuyển chế độ: " + m); }, function (e) { say(e.message, true); });
        }));
        b.setAttribute("aria-pressed", m === data.config.mode ? "true" : "false");
      });
      add(cfgBox, el("p", "muted", MODE_HELP[data.config.mode] || ""));
      var row2 = put(cfgBox, el("div", "ib-row"));
      var num = function (label, value, min, max) {
        var lab = put(row2, el("label", "", label));
        var inp = el("input");
        inp.type = "number"; inp.min = min; inp.max = max; inp.value = value;
        add(lab, inp);
        return inp;
      };
      var fIn = num("Trần feature", data.config.limits.feature, 1, 32);
      var tIn = num("Trần tool", data.config.limits.tool, 1, 32);
      var iIn = num("Tắt sau (phút rảnh)", data.config.idle_minutes, 5, 240);
      // Phiên treo: một lượt đang chạy mà transcript không có dòng mới. Báo @Manager sau N phút; tắt và giao lại sau M phút (0: không tự tắt).
      var hnIn = num("Lượt im: báo sau (phút)", data.config.hung_note, 2, 120);
      var hsIn = num("tắt và giao lại sau (phút, 0 = không)", data.config.hung_stop, 0, 240);
      // Chat của feature/tool đang mở ở VS Code mà rảnh: đóng nó khi có tin chờ, sau N phút rảnh (0 = không bao giờ đóng).
      var tkIn = num("Chat mở mà rảnh: đóng để giao sau (phút, 0 = không)", data.config.takeover_minutes, 0, 240);
      add(row2, button("Lưu", function () {
        post("config", { limits: { feature: parseInt(fIn.value, 10), tool: parseInt(tIn.value, 10) }, idle_minutes: parseInt(iIn.value, 10),
                         hung_note: parseInt(hnIn.value, 10), hung_stop: parseInt(hsIn.value, 10), takeover_minutes: parseInt(tkIn.value, 10) })
          .then(function () { say("Đã lưu trần, thời gian rảnh và ngưỡng phiên treo."); }, function (e) { say(e.message, true); });
      }));
    };

    var loadReqs = function (target) {
      reqs = { target: target, items: [], note: target ? "Đang tải sổ yêu cầu…" : "" };
      renderSend();
      var pool = (data.targets.filter(function (t) { return t.target === target; })[0] || {}).pool;
      if (!target || pool !== "feature" || target === "@Core") { reqs.note = ""; renderSend(); return; }
      get("/api/inbox/reqs?feature=" + encodeURIComponent(target)).then(function (res) {
        if (reqs.target !== target) return;
        reqs.items = res.ok ? res.reqs : [];
        reqs.note = !res.ok ? "Không đọc được sổ: " + res.error : !res.reqs.length ? "Sổ không còn yêu cầu nào chưa kiểm." :
          (res.allowed === false ? "Feature đang khoá code: " + res.reason : "Bấm một yêu cầu để thêm vào tin.");
        renderSend();
      }, function (e) { reqs.note = "Không đọc được sổ: " + e.message; renderSend(); });
    };

    var renderSend = function () {
      sendBox.textContent = "";
      add(sendBox, el("h2", "", "Gửi tin"));
      var row = put(sendBox, el("div", "ib-row"));
      var sel = put(row, el("select"));
      var opt0 = put(sel, el("option", "", "— chọn thread nhận —"));
      opt0.value = "";
      [["feature", "Feature, service, @Core (worker code)"], ["tool", "Tool"], ["tester", "Tester (worker chơi thử, chờ phiếu Editor)"]].forEach(function (g) {
        var og = put(sel, el("optgroup"));
        og.label = g[1];
        data.targets.filter(function (t) { return t.pool === g[0]; }).forEach(function (t) {
          var o = put(og, el("option", "", t.target));
          o.value = t.target;
        });
      });
      sel.value = form.target;
      sel.addEventListener("change", function () { form.target = sel.value; loadReqs(sel.value); });
      if (!data.targets.length) add(row, el("span", "muted", "Chưa có thread feature hay tool nào đã gán: chạy Setup trước."));
      if (reqs.note) add(sendBox, el("p", "muted", reqs.note));
      if (reqs.items.length) {
        var list = put(sendBox, el("div", "ib-reqs"));
        reqs.items.forEach(function (r) {
          add(list, button("[" + (r.name || r.state) + "] " + r.ref + ": " + r.title, function () {
            form.text = (form.text ? form.text.replace(/\s+$/, "") + "\n" : "") + "Làm yêu cầu " + r.ref + " (" + r.title + ").";
            renderSend();
          }));
        });
      }
      var ta = put(sendBox, el("textarea"));
      ta.placeholder = "Việc cho thread này. Nêu id yêu cầu (<node>#<id>) hoặc đường dẫn báo cáo, vì thread có thể vừa renew và không nhớ.";
      ta.value = form.text;
      ta.maxLength = 8000;
      ta.addEventListener("input", function () { form.text = ta.value; });
      var row2 = put(sendBox, el("div", "ib-row"));
      var sb = put(row2, button(sending ? "Đang gửi…" : "Gửi", function () {
        if (!form.target || !form.text.trim()) { say("Chọn thread nhận và gõ việc.", true); return; }
        sending = true; renderSend();
        post("send", { target: form.target, text: form.text }).then(function (res) {
          say("Đã đưa vào hộp thư (" + res.id + "): server sẽ giao trong vài giây.");
          form.text = ""; sending = false; renderSend();
        }, function (e) { sending = false; renderSend(); say(e.message, true); });
      }));
      sb.disabled = sending;
    };

    var renderList = function () {
      listBox.textContent = "";
      var s = data.summary;
      add(listBox, el("h2", "", "Tin (" + s.open + " chưa giao" + (s.error ? ", " + s.error + " lỗi" : "") + (s.old ? ", " + s.old + " chờ quá 30 phút" : "") + ")"));
      if (!data.messages.length) { add(listBox, el("p", "muted", "Chưa có tin nào.")); return; }
      // Xoá hộp thư: tin đã xong (đã giao, đã huỷ, lỗi) hoặc mọi tin, kể cả tin chưa giao (sẽ không bao giờ được giao).
      // Tin đang được giao đúng lúc đó thì giữ lại. Danh sách chỉ hiện các tin gần nhất; lệnh xoá áp dụng cho cả hộp thư.
      var openCount = s.open;
      var clearRow = put(listBox, el("div", "ib-row"));
      add(clearRow, button("Xoá tin đã xong", function () {
        post("clear", { scope: "done" }).then(function (res) { armed = false; say("Đã xoá " + res.removed + " tin đã xong."); }, function (e) { say(e.message, true); });
      }));
      var allBtn = put(clearRow, button(armed ? "Bấm lần nữa: xoá MỌI tin" + (openCount ? ", kể cả " + openCount + " tin chưa giao" : "") : "Xoá cả hộp thư", function () {
        if (!armed) { armed = true; renderList(); return; }
        post("clear", { scope: "all" }).then(function (res) { armed = false; say("Đã xoá " + res.removed + " tin; hộp thư trống."); renderList(); },
                                             function (e) { armed = false; say(e.message, true); });
      }));
      if (armed) {
        allBtn.className += " bad-text";
        add(clearRow, button("Thôi", function () { armed = false; renderList(); }));
        add(clearRow, el("span", "muted", "Tin chưa giao bị xoá sẽ không tới phiên nào; không lấy lại được."));
      }
      var wrap = put(listBox, el("div", "ib-wrap"));
      var table = put(wrap, el("table", "ib-table"));
      var head = put(table, el("tr"));
      ["Lúc gửi", "Nhận", "Từ", "Trạng thái", "Nội dung", ""].forEach(function (h) { add(head, el("th", "", h)); });
      data.messages.forEach(function (m) {
        var tr = put(table, el("tr"));
        add(tr, el("td", "", when(m.created)), el("td", "", m.target), el("td", "", m.source));
        var stc = put(tr, el("td"));
        add(stc, el("span", "ib-state " + m.state, STATE_NAMES[m.state] || m.state));
        var waitedMin = (m.state === "queued" || m.state === "waiting") ? Math.floor((Date.now() / 1000 - m.created) / 60) : 0;
        if (waitedMin >= 1) add(stc, el("div", waitedMin >= 5 ? "bad-text" : "muted", "đã chờ " + waitedMin + " phút"));
        if (m.why) add(stc, el("div", "muted", m.why));
        if (m.error) add(stc, el("div", "bad-text", m.error));
        if (m.tries) add(stc, el("div", "muted", "lần thử: " + m.tries + (m.state === "delivered" ? " · " + when(m.delivered_at) : "")));
        var txt = m.text.length > 400 ? m.text.slice(0, 400) + "…" : m.text;
        var tc = put(tr, el("td", "ib-text", txt));
        tc.title = m.text;
        var act = put(tr, el("td"));
        if (m.state === "queued" || m.state === "waiting") {
          // Giao ngay: bỏ qua lí do chờ (chat đang mở mà rảnh thì server đóng chat rồi giao ở nền; hết chỗ thì giao vượt trần).
          var now = put(act, button("Giao ngay", function () {
            now.disabled = true;
            say("Đang giao " + m.id + "…");
            post("deliver", { id: m.id }).then(function () { say("Đã giao " + m.id + " cho " + m.target + "."); },
                                               function (e) { say("Chưa giao được " + m.id + ": " + e.message, true); refresh(); });
          }));
          now.title = "Giao tin này ngay, bỏ qua lí do chờ. Chat của thread đang mở ở VS Code mà rảnh thì bị đóng (nội dung thread không mất) và thread chạy ở nền.";
        }
        if (m.state === "queued" || m.state === "waiting" || m.state === "error") {
          add(act, button("Huỷ", function () { post("cancel", { id: m.id }).then(function () { say("Đã huỷ " + m.id); }, function (e) { say(e.message, true); }); }));
        }
        if (m.state === "error" || m.state === "cancelled") {
          add(act, button("Thử lại", function () { post("retry", { id: m.id }).then(function () { say("Đã đưa lại vào hàng: " + m.id); }, function (e) { say(e.message, true); }); }));
        }
        if (m.state !== "delivering" && m.state !== "queued" && m.state !== "waiting") {
          add(act, button("Xoá", function () { post("remove", { id: m.id }).then(function () { say("Đã xoá " + m.id); }, function (e) { say(e.message, true); }); }));
        }
      });
    };

    var renderAll = function () {
      renderPools();
      renderConfig();
      renderList();
      if (!sendBox.firstChild) renderSend();      // giữ ô gõ dở khi bảng tự làm mới
    };
    var refresh = function () {
      get("/api/workers").then(function (v) {
        workers = v;
        var typing = document.activeElement && workBox.contains(document.activeElement);
        if (!typing) renderWorkers();      // giữ ô gõ dở khi bảng tự làm mới
      }, function (e) { say("Không tải được danh sách worker: " + e.message, true); });
      return get("/api/inbox").then(function (v) { data = v; renderPools(); renderList(); if (!cfgBox.firstChild) renderConfig(); if (!sendBox.firstChild) renderSend(); },
        function (e) { say("Không tải được hộp thư: " + e.message, true); });
    };
    refresh();
    var timer = setInterval(function () {
      if (!pane.isConnected) { clearInterval(timer); return; }
      if (!document.hidden && pane.offsetParent !== null) refresh();
    }, REFRESH_MS);
  };

  window.CORESIMPLE_VIEWS = window.CORESIMPLE_VIEWS || {};
  window.CORESIMPLE_VIEWS.inbox = build;
})();
