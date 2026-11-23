// Hướng dẫn gán chat Claude Code theo feature. index.html nạp file này bằng thẻ <script> nên phải gán vào window.
// Thêm feature mới: thêm một dòng vào `features`. `id` là giá trị gõ sau FEATURE=.
window.CORESIMPLE_SESSIONS = {
  intro: "Mỗi chat bị guard nhốt trong một feature. Mở chat mới trong VS Code, gõ dòng đầu tiên FEATURE=<giá trị> <PIN>. Nút Chép chỉ chép phần trước PIN; bạn tự gõ PIN rồi Enter.",

  pinTitle: "Đặt PIN (một lần)",
  pinHint: "Chạy trong terminal của VS Code ở gốc project, tự tay gõ, không nhờ AI. Gõ PIN hai lần. Sai PIN 5 lần thì bị khoá cho tới khi chạy lại lệnh này.",
  pinCommand: "python .claude/hooks/bind_session.py --set-pin",

  // Tab Cài đặt: chuẩn bị một máy (Windows, Linux, macOS) để chạy project. Mỗi bước: `text`, tuỳ chọn `os`
  // (chỉ áp dụng cho hệ đó) và `command` (khối có nút Chép). Mọi lệnh chạy ở gốc project.
  config: {
    title: "Cài đặt máy",
    intro: "Làm một lần trên mỗi máy, Windows, Linux hay macOS đều được. Các hook và công cụ trong Management/tools tự tìm thư mục project ($CLAUDE_PROJECT_DIR), không có đường dẫn ổ đĩa viết cứng, nên clone project vào thư mục nào cũng chạy.",
    sections: [
      { title: "1. Python 3 với lệnh python", steps: [
        { text: "Mọi hook trong .claude/settings.json gọi lệnh python. Thiếu lệnh này thì hook lỗi mà không chặn gì: guard bị bỏ qua mà không báo. Kiểm tra trước tiên:", command: "python --version" },
        { os: "Windows", text: "Cài Python 3 từ python.org, tick \"Add python.exe to PATH\". Tắt alias của Microsoft Store nếu python mở Store: Settings > Apps > Advanced app settings > App execution aliases." },
        { os: "Linux (Ubuntu/Debian)", text: "Thường chỉ có python3. Cài gói để có lệnh python:", command: "sudo apt install python-is-python3" },
        { os: "Linux khác, macOS", text: "Tạo lệnh python trỏ tới python3 (ví dụ một symlink trong ~/.local/bin, thư mục này phải có trong PATH):", command: "mkdir -p ~/.local/bin && ln -s \"$(command -v python3)\" ~/.local/bin/python" }
      ] },
      { title: "2. Kiểm tra guard và công cụ", steps: [
        { text: "Chạy sau khi clone, đổi máy hoặc sửa hook. Mỗi lệnh phải kết thúc bằng TOTAL: ... 0 failed.", command: "python .claude/hooks/test_guard.py" },
        { text: "Công cụ kiểm ranh giới và thiết kế:", command: "python Management/tools/test_validate.py" },
        { command: "python Management/tools/test_gamedesign.py" },
        { command: "python Management/tools/test_studio.py" },
        { text: "Setup, thư mục và nhóm chat:", command: "python Management/tools/test_scaffold.py\npython Management/tools/test_vscode_groups.py" },
        { text: "Mở một chat chưa gán rồi nhờ AI đọc một file bất kỳ. Guard phải trả lời (đọc được, hoặc chặn kèm lý do bắt đầu bằng guard:). Nếu báo lỗi đường dẫn thì dừng lại, đừng gán chat, chạy lại test_guard.py." }
      ] },
      { title: "3. PIN và gán chat", steps: [
        { text: "PIN và các chat đã gán nằm ngoài project, trong ~/.coresimple-guard (Windows: C:\\Users\\<bạn>\\.coresimple-guard). Mỗi máy đặt PIN riêng, ở tab Gán chat hoặc bằng lệnh:", command: "python .claude/hooks/bind_session.py --set-pin" }
      ] },
      { title: "4. Trang quản lý và Setup", steps: [
        { text: "Mở trang qua server cục bộ để lưu được điểm, câu trả lời, tạo thread, và duyệt gen, nhận, trả ảnh art. Server chỉ nghe trên 127.0.0.1, cổng 8770.", command: "python Management/tools/setup_server.py" },
        { text: "Setup full gồm cả các feature mà thiết kế live đã khai trong code.json, kể cả khi chưa có thư mục trong Unity: thread được tạo và gán ngay, và sửa được sau khi @Manager tạo thư mục (lệnh dưới, có --dry-run để xem trước).", command: "python Management/tools/scaffold.py" },
        { text: "Gom các thread theo tầng (1 · Master … 6 · Features) trong danh sách chat của VS Code: tắt hẳn VS Code (mọi cửa sổ), chạy lệnh trong terminal ngoài VS Code, rồi mở lại. VS Code đang chạy thì lệnh từ chối, vì nó sẽ ghi đè. Có sao lưu; khôi phục bằng --restore. Nơi extension lưu nhóm không có trong tài liệu; nếu bản cập nhật đổi cách lưu, lệnh dừng và báo, khi đó gom tay bằng chuột phải vào session.", command: "python Management/tools/vscode_groups.py" },
        { text: "Tạo thread cần lệnh claude (Claude Code CLI) có trong PATH. Thread đã lưu trữ trong VS Code được bỏ qua: server tự đọc trạng thái VS Code ở đúng chỗ của từng hệ (Windows: %APPDATA%\\Code, macOS: ~/Library/Application Support/Code, Linux: ~/.config/Code).", command: "claude --version" }
      ] },
      { title: "5. Codex và Blender (cho @Codex, @Blender)", steps: [
        { text: "studio.py gọi lệnh codex (Codex CLI, đã đăng nhập) và blender (chạy headless). Thiếu lệnh nào thì phiên tool phải đi đường dự phòng (blender MCP, unity-mcp; xem .claude/rules/studio.md). Kiểm tra:", command: "codex --version\nblender --version" },
        { text: "Codex chỉ đọc vẫn sinh ảnh vào ~/.codex/generated_images; studio.py lấy đúng ảnh mới ở đó. Đường dẫn khác thì đặt biến CODEX_HOME." }
      ] },
      { title: "6. Git", steps: [
        { text: "Đặt tên và email tác giả cho riêng repo này (không thêm --global nếu không muốn đổi cả máy):", command: "git config user.name \"<tên>\"\ngit config user.email \"<email>\"" },
        { text: ".gitattributes ép xuống dòng LF cho mọi file văn bản, nên sửa trên Windows hay Linux đều cho cùng một diff." }
      ] },
      { title: "7. Khác nhau giữa Windows và Linux", steps: [
        { text: "Linux và macOS phân biệt chữ hoa thường. FEATURE=, đường dẫn và tên thư mục phải đúng chính tả: FEATURE=Wallet được, FEATURE=wallet thì không; Assets/core không phải Assets/Core. Windows không phân biệt." },
        { text: "Trên Linux và macOS, đường dẫn dùng /. Dấu \\ là một ký tự trong tên file, nên guard chặn mọi đường dẫn có \\. Windows nhận cả / lẫn \\." },
        { text: "Symlink (Linux, macOS) hay junction (Windows) bên trong project đều bị guard chặn khi đi qua. Đừng dùng link để chia sẻ thư mục giữa các feature." }
      ] }
    ]
  },

  // `copy`: chữ được chép, khi khác "FEATURE=<id> ".
  features: [
    { id: "@human", group: "Master", label: "Phiên của người (tích hợp)", note: "Không giới hạn feature; vẫn bị hỏi khi sửa Core, _Composition, .claude. Biên dịch, chạy test, Play và sửa scene/prefab qua unity-mcp." },
    { id: "@Manager", group: "Leaders", label: "Phiên điều phối", note: "Không sửa code. Thấy báo cáo mới của các phiên ở đầu mỗi tin, nhắn các phiên bằng SendMessage, viết đề xuất trong Management/proposals. Tạo thư mục feature, service, Core, _Composition còn thiếu theo thiết kế live bằng python Management/tools/scaffold.py (chỉ thư mục, không code)." },
    { id: "@CodeLead", group: "Leaders", label: "Phiên quản khung và kiểm code", note: "Chỉ sửa cấu trúc khung: Scripts/Contracts, Scripts/Internal/Base, Scripts/View/Base, *.asmdef, AssemblyInfo.cs của feature đã mở khoá code; *.asmdef, AssemblyInfo.cs, interface I*.cs của Core/_Composition. Ngoài khung không sửa gì; lượt có sửa file phải có báo cáo. Đọc toàn bộ source nhóm Basic, Features, Services. Qua unity-mcp: biên dịch, chạy test, Play, menu, tag/layer, package và các cài đặt project (giữ khoá Editor tới hết lượt), nhưng không sửa asset hay lưu scene. Dựng khung theo gói thiết kế (python Management/tools/gd_pack.py <Tên>); ghi tiến độ node lá bằng gd_progress.py. Giải quyết xung đột code và báo lỗi về phiên sở hữu (.claude/rules/codelead.md)." },
    { id: "@GameDesigner", group: "Leaders", label: "Phiên thiết kế game", note: "Không code. Chỉ đọc Management/game-design và chỉ ghi trong Management/game-design/ai (cây đề xuất ở ai/idea), theo 4 vòng: ý tưởng, feature, quan hệ và định nghĩa, quyết định chi tiết (.claude/rules/gamedesigner.md). Mỗi node có data.md (các ý cốt lõi, mỗi ý có điểm AI) và CLAUDE.md (diễn giải). Ở tab Game design bạn chọn mode (Full Auto: AI tự quyết; User control: AI hỏi, bạn chấm), chấm điểm 0-10 và ghi phản hồi cho từng ý ở cả bảng đề xuất lẫn bảng đã chốt, trả lời câu hỏi, rồi bấm Sync (hoặc Chốt từng node) để chép node đạt sang live; không có gì tự sang live. Ý bị chấm dưới 5, data của node cha đổi, hoặc node ở nhánh khác mà nó khai trong refs đã đổi (hay nhắc tới mà chưa khai) thì node phải làm lại; thuật ngữ chung nằm ở node glossary; lịch sử từng node nằm ở .history. Vòng 1 chốt hướng màn (screen: landscape hoặc portrait); node vòng 4 có bố cục viết frame (.frame-concept), bạn xem hình vẽ và cảnh báo ngay trong panel node. Vòng 1 còn chốt style (.art-style); Feature có frame giao diện phải có node cheat (tối đa 3 state dựng nhanh ở bản dev); order ảnh art chỉ đặt ở node cheat, chờ bạn duyệt gen." },
    { id: "@Codex", group: "Tools", label: "Phiên ảnh và tham vấn GPT", note: "Không code. Nhận order của feature (Management/studio/codex/orders), gen ảnh qua python Management/tools/studio.py codex (Codex CLI ghi trực tiếp, không được thì chỉ đọc ảnh Codex sinh ra, rồi gián tiếp qua unity-mcp), tham vấn GPT bằng studio.py consult. Order art (orders/_design, chỉ cho node cheat của feature) chỉ chạy sau khi bạn duyệt gen, theo style và frame của từng state. Mỗi item 2 lượt (+1 mỗi lần bị trả, tối đa 2 lần làm lại); ghi tự kiểm work/<item>.check.md; hết lượt thì pick bản gần nhất và giao với nhãn chưa chuẩn kèm phiếu nợ. Gen đúng số lượng; đọc lessons.md trước khi gen; giao bằng studio.py deliver vào Resources/<Feature>/codex/<id>/v<n>. Luật: .claude/rules/studio.md." },
    { id: "@Blender", group: "Tools", label: "Phiên model 3D", note: "Không code game. Nhận order của feature (Management/studio/blender/orders), viết script bpy ở work/<item>.py rồi chạy headless bằng python Management/tools/studio.py blender; không được thì blender MCP, rồi gián tiếp qua unity-mcp. Gen đúng số lượng; đọc lessons.md trước; giao bằng studio.py deliver vào Resources/<Feature>/blender/<id>/v<n>. Cùng trần lượt như @Codex: 2 lượt mỗi item, +1 mỗi lần bị trả; hết lượt thì pick bản gần nhất, giao với nhãn chưa chuẩn." },
    { id: "@Core", group: "Basic", label: "Phiên Core", note: "Chỉ sửa Assets/Core, trừ khung (*.asmdef, AssemblyInfo.cs, interface I*.cs: @CodeLead); đọc phần công khai của feature. Tự biên dịch và đọc console qua unity-mcp. Core không chứa luật game, không biết tên feature." },
    { id: "@Composition", group: "Basic", label: "Phiên Composition", note: "Chỉ sửa Assets/_Composition: Boot variant, scene, catalog asset; đọc phần công khai của feature. Mở, sửa, lưu scene và Boot prefab của mình qua unity-mcp (giữ khoá Editor tới hết lượt); Play và test nhờ @CodeLead." },
    { id: "<Tên>", group: "Features / Services", label: "Phiên feature", copy: "FEATURE=", note: "Tên thư mục trong Assets/Features, ví dụ Wallet (nhóm Features); dịch vụ thì thêm _Services/, ví dụ _Services/Popup (nhóm Services). Code khi thiết kế đã mở khoá, bám gói python Management/tools/gd_pack.py <Tên> (kèm frame bố cục: kích thước, vị trí); khung chỉ đọc, code trên lớp con. Qua unity-mcp: tự biên dịch, đọc console, tạo và sửa asset, prefab trong vùng mình; scene của mình thì giữ khoá Editor. Cần ảnh hay model 3D thì đặt order cho @Codex/@Blender; hàng chưa chuẩn vẫn take được (xem QUALITY.md trong Generated), trả tối đa 2 lần, lần thứ 3 order đóng kèm phiếu nợ." }
  ],

  notes: [
    "Sáu tầng, từ trên xuống: Master (@human) · Leaders (@Manager, @CodeLead, @GameDesigner) · Tools (@Codex, @Blender) · Basic (@Core, @Composition) · Services (_Services/*) · Features. Setup và nhóm chat VS Code xếp theo thứ tự này.",
    "Mỗi chat một feature, nhiều chat chạy chung một cửa sổ VS Code. Gõ FEATURE? để xem chat đang gán gì.",
    "Không đưa PIN cho AI.",
    "Nhóm Basic, Features, Services và @CodeLead: lượt có sửa file (kể cả tạo asset, lưu scene qua unity-mcp) phải kết thúc bằng báo cáo (.claude/rules/report.md); hook lưu vào Management/reports, @CodeLead và @Manager đều thấy. @Codex, @Blender không báo cáo: studio.py giữ lịch sử của từng order.",
    "Chat chưa gán thì không sửa được gì. Chat mới, /clear hoặc chat tách nhánh phải gán lại.",
    "Phiên feature chỉ sửa được code khi thiết kế của nó đã tới vòng 4 trong Management/game-design/live (node feature khai thư mục code trong code.json). Bị khoá thì việc tiếp theo là của @GameDesigner và bạn (tab Game design). Mở rồi thì phiên đọc thiết kế bằng python Management/tools/gd_pack.py <Tên>.",
    "Tên chat: vai trò bỏ @ (Codex, GameDesigner, CodeLead, Manager, Core, Composition, Blender, human), feature giữ tên (Wallet). SendMessage hiểu @ là địa chỉ team nên phiên tên @Codex không nhận được tin; quyền vẫn gán FEATURE=@Codex như cũ. Chat cũ còn tên có @ thì tab Setup báo đỏ: gõ /rename Codex (bỏ @) trong chat đó. Setup tạo chat mới đã đặt tên không có @.",
    "Tab Game design: bản đồ chung (nút Tất cả feature) chỉ vẽ vòng 1 và 2; bấm một feature thì bản đồ mở nhánh của nó tới vòng 4. Bấm một node, panel bên dưới hiện trước thứ cần đọc và quyết: tóm tắt, độ quan trọng, tiến độ, ý cốt lõi (bấm từng ý để xem chi tiết và chấm), câu hỏi, chốt, ảnh, style, art, frame. Thông tin phụ gập sẵn ở cuối panel, bấm tiêu đề để mở: Bám theo (node phía trên), Tham chiếu (refs), Hướng dẫn (độ quan trọng, thang điểm), Diễn giải (CLAUDE.md, bảng hiện thành bảng), và Debug (hash, lịch sử cập nhật node; chỉ để dò lỗi).",
    "Thiết kế: node nhắc tới node vòng 3-4 ở nhánh khác phải khai refs kèm hash; nhánh kia đổi thì node phải làm lại, nên số liệu giữa các nhánh không lệch. Thuật ngữ chung nằm ở node glossary, chỉ gồm từ dễ hiểu nhiều cách, từng bị định nghĩa khác nhau, hoặc là khái niệm riêng của game; tên đã đủ nghĩa thì không đưa vào. Nghĩa chỉ nói từ đó là gì, còn luật và con số nằm ở node vòng 3-4 sở hữu nó. Lời nhắc về thuật ngữ hiện dưới bản đồ ở tab Game design.",
    "Frame (bố cục ước lượng): node vòng 4 có popup, màn hình, HUD hay vị trí, tầm trên chiến trường thì có file .frame-concept/<tên>.frame: các hình cơ bản (khung, nút, lưới, thanh, vòng tầm, mũi tên) mang số thật, px trên màn chuẩn (chốt ở vòng 1 bằng dòng screen: landscape 1920x1080 hoặc portrait 1080x1920) hoặc u trên chiến trường. Tab Game design vẽ frame trong panel node (di chuột lên hình để xem số) và cảnh báo nút nhỏ hơn 96 px, hình tràn màn, hình chồng nhau. AI đọc bản đã giải bằng python Management/tools/gd_frame.py <ai/idea|live>/<node>; bạn xuất trang HTML riêng bằng thêm --html <file>. ",
    "Lượt gen có trần (mọi order ảnh, art, model): mỗi item 2 lượt, mỗi lần bị trả thêm 1, làm lại tối đa 2 lần; lần trả thứ 3 đóng order. Hết lượt thì @Codex/@Blender chọn bản gần order nhất và giao với nhãn chưa chuẩn, kèm phiếu nợ art. Feature vẫn dùng được hàng đó (QUALITY.md trong Generated). Tab Studio hiện mọi order, lượt đã dùng, item chưa chuẩn và phiếu nợ; phiếu tự đóng khi có bản giao đạt cho đúng item, hoặc bạn bấm Chấp nhận nợ (mở trang qua setup_server.py).",
    "Node cheat (<feature>/cheat, chip \"cheat\" trên bản đồ): mỗi ý là một state có nhiều thành phần nhất của feature, ghi giá trị cần ép (ví dụ điểm 16, kỉ lục 16), tối đa 3 state, mỗi state một frame dùng số từ node vòng 4. Phiên code dựng các state này ở bản dev để test và Play nhanh, bạn xem 1-3 ảnh là nắm được cả feature. Bắt buộc khi feature có frame giao diện (thiếu thì node vòng 4 không chốt được); feature chỉ có frame chiến trường không cần. Code cheat chỉ chạy ở bản dev.",
    "Art-concept (ảnh hình dung phong cách): style chốt ở vòng 1 (thư mục .art-style của node gốc: mỗi style một file mô tả, kèm ảnh tham chiếu bạn bỏ vào; mix được nhiều style; tính vào hash nên đổi style là cả cây phải rà lại). @GameDesigner đặt order art chỉ cho node cheat của feature (<feature>/cheat, đã chốt; mỗi state một ảnh, tối đa 3 ảnh mỗi feature); order chờ bạn bấm Duyệt gen và nhập PIN ở tab Game design rồi @Codex mới chạy được (trần lượt như mọi order: 2 lượt lần đầu, mỗi lần bị trả thêm 1 lượt và phải duyệt lại, tối đa 2 lần làm lại; cả project tối đa 20 lần chạy art một ngày). Codex nhận frame vẽ ra PNG để giữ bố cục, ảnh của style và ảnh mốc. Ảnh hiện trong panel node với nhãn chờ nhận, đang dùng, đã trả, CŨ · node đã đổi hoặc CŨ · style đã đổi; bạn Nhận, Trả kèm phản hồi hay Đặt làm mốc ngay đó (cần mở trang qua setup_server.py). Bấm vào ảnh hay frame để xem to.",
    "Order ảnh và 3D: feature viết order.md trong Management/studio/<codex|blender>/orders/<Feature>/<id>/, @Codex/@Blender làm và giao vào Resources/<Feature>/<tool>/<id>/v<n>, feature lấy bằng studio.py take. Xem mọi order: python Management/tools/studio.py status. Lỗi đã gặp nằm trong lessons.md của từng tool. Mọi phiên đã gán đều đọc được Management/studio và Resources.",
    "Tiến độ: @CodeLead đánh dấu từng node lá xong tới giai đoạn nào sau khi biên dịch và test; bạn sửa lại được ở tab Game design.",
    "Phiên code tự biên dịch (refresh_unity) và đọc console qua unity-mcp, tạo và sửa asset, prefab trong vùng của nó; chạy test, Play, menu và cài đặt project là việc của @CodeLead (hoặc bạn chạy trong Unity). Bảng lệnh ở .claude/rules/unity.md.",
    "Mở, sửa, lưu scene hay prefab stage: guard giữ khoá Editor cho một chat tới hết lượt; chat khác chờ. Khoá kẹt thì bạn gỡ bằng: python .claude/hooks/stop_unlock.py --release",
    "Phiên feature chỉ nên báo xong khi code của nó biên dịch được: cả project dùng chung một Unity Editor, lỗi biên dịch của một feature chặn test của mọi feature.",
    "Việc phụ thuộc nhau thì làm nối tiếp: feature cần contract mới của feature khác phải chờ feature kia xong.",
    "Thử lại guard sau khi sửa nó: python .claude/hooks/test_guard.py"
  ],

  briefTitle: "Mẫu lệnh giao việc",
  briefHint: "Dán vào phiên feature rồi điền các chỗ trong ngoặc nhọn.",
  brief: [
    "Bạn làm việc trong feature <TÊN FEATURE>. Mục tiêu: <MỤC TIÊU, 1-2 câu>.",
    "",
    "Đọc trước:",
    "- CLAUDE.md ở gốc project và .claude/rules/unity.md",
    "- Gói thiết kế đã chốt: python Management/tools/gd_pack.py <TÊN FEATURE> (node feature, node con, node nhánh khác mà nó dựa vào, thuật ngữ, frame bố cục đã giải ra px hoặc u). Code bám theo gói này, kích thước và vị trí lấy từ frame; thiếu hay mâu thuẫn thì dừng và báo người.",
    "- Toàn bộ feature của bạn",
    "- Của feature khác chỉ được đọc: CLAUDE.md ở gốc feature, Scripts/Contracts, FeatureAssets/Public. Internal, View, Tests của họ bị guard chặn; đừng thử đọc.",
    "- <ĐỀ XUẤT LIÊN QUAN trong Management/proposals, nếu có>",
    "",
    "Việc cần làm:",
    "1. <VIỆC 1>",
    "2. <VIỆC 2>",
    "",
    "Khung chỉ đọc: Scripts/Contracts, Scripts/Internal/Base, Scripts/View/Base, *.asmdef, AssemblyInfo.cs (InternalsVisibleTo). Viết code mới trên lớp con ở phần còn lại của Internal/View. Cần đổi khung: gửi @CodeLead ngữ cảnh, file, đề xuất và chờ @CodeLead sửa. Core, _Composition: nhờ @Core/@Composition. rules.json: dừng và hỏi người.",
    "Xong thì báo @CodeLead (khối báo cáo); lỗi @CodeLead báo về thì tự sửa.",
    "Cần ảnh hay model 3D: đặt order theo khung ở .claude/rules/studio.md (Write mới Management/studio/<codex|blender>/orders/<TÊN FEATURE>/<id>/order.md, chạy python Management/tools/studio.py check), nhắn @Codex/@Blender; nhận hàng bằng studio.py take <TÊN FEATURE>/<id>.",
    "Cần đổi thứ gì ở feature khác: viết đề xuất vào Management/proposals rồi dừng.",
    "Xoá file chỉ qua unity-mcp (manage_asset, action delete, path bắt đầu bằng Assets/). Asset, prefab trong vùng mình: tạo, sửa qua unity-mcp; code chỉ bằng Edit/Write. Biên dịch (refresh_unity) rồi đọc console, sửa hết lỗi của mình.",
    "",
    "Xong khi: python Management/tools/validate.py báo 0 vi phạm, code của bạn biên dịch được, và bạn đã nêu rõ test nào cần người chạy."
  ].join("\n")
};
