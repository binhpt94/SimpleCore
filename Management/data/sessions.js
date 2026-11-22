// Hướng dẫn gán chat Claude Code theo feature. index.html nạp file này bằng thẻ <script> nên phải gán vào window.
// Thêm feature mới: thêm một dòng vào `features`. `id` là giá trị gõ sau FEATURE=.
window.CORESIMPLE_SESSIONS = {
  intro: "Mỗi chat bị guard nhốt trong một feature. Mở chat mới trong VS Code, gõ dòng đầu tiên FEATURE=<giá trị> <PIN>. Nút Chép chỉ chép phần trước PIN; bạn tự gõ PIN rồi Enter.",

  pinTitle: "Đặt PIN (một lần)",
  pinHint: "Chạy trong terminal của VS Code ở gốc project, tự tay gõ, không nhờ AI. Gõ PIN hai lần. Sai PIN 5 lần thì bị khoá cho tới khi chạy lại lệnh này.",
  pinCommand: "python .claude/hooks/bind_session.py --set-pin",

  // `copy`: chữ được chép, khi khác "FEATURE=<id> ".
  features: [
    { id: "@human", group: "Master", label: "Phiên của người (tích hợp)", note: "Không giới hạn feature; vẫn bị hỏi khi sửa Core, _Composition, .claude. Biên dịch, chạy test, Play và sửa scene/prefab qua unity-mcp." },
    { id: "@Manager", group: "Leaders", label: "Phiên điều phối", note: "Không sửa code. Thấy báo cáo mới của các phiên ở đầu mỗi tin, nhắn các phiên bằng SendMessage, viết đề xuất trong Management/proposals." },
    { id: "@CodeLead", group: "Leaders", label: "Phiên kiểm code", note: "Không sửa file nào. Đọc toàn bộ source nhóm Basic, Features, Services; biên dịch, chạy test, Play qua unity-mcp; giải quyết xung đột code và báo lỗi về phiên sở hữu (.claude/rules/codelead.md)." },
    { id: "@GameDesigner", group: "Leaders", label: "Phiên thiết kế game", note: "Không code. Chỉ đọc Management/game-design và chỉ ghi trong Management/game-design/ai (cây đề xuất ở ai/idea), theo 4 vòng: ý tưởng, feature, quan hệ và định nghĩa, quyết định chi tiết (.claude/rules/gamedesigner.md). Mỗi node có data.md (các ý cốt lõi, mỗi ý có điểm AI) và CLAUDE.md (diễn giải). Ở tab Game design bạn chọn mode (Full Auto: AI tự quyết; User control: AI hỏi, bạn chấm), chấm điểm 0-10 và ghi phản hồi cho từng ý ở cả bảng đề xuất lẫn bảng đã chốt, trả lời câu hỏi, rồi bấm Sync (hoặc Chốt từng node) để chép node đạt sang live; không có gì tự sang live. Ý bị chấm dưới 5 hoặc data của node cha đổi thì node phải làm lại; lịch sử từng node nằm ở .history." },
    { id: "@Core", group: "Basic", label: "Phiên Core", note: "Chỉ sửa Assets/Core; đọc phần công khai của feature." },
    { id: "@Composition", group: "Basic", label: "Phiên Composition", note: "Chỉ sửa Assets/_Composition: Boot variant, scene, catalog asset; đọc phần công khai của feature. Việc cần Unity (scene, prefab) nhờ @human." },
    { id: "<Tên>", group: "Features / Services", label: "Phiên feature", copy: "FEATURE=", note: "Tên thư mục trong Assets/Features, ví dụ Wallet (nhóm Features); dịch vụ thì thêm _Services/, ví dụ _Services/Popup (nhóm Services)." }
  ],

  notes: [
    "Mỗi chat một feature, nhiều chat chạy chung một cửa sổ VS Code. Gõ FEATURE? để xem chat đang gán gì.",
    "Không đưa PIN cho AI.",
    "Nhóm Basic, Features, Services: lượt có sửa file phải kết thúc bằng báo cáo (.claude/rules/report.md); hook lưu vào Management/reports, @CodeLead và @Manager đều thấy.",
    "Chat chưa gán thì không sửa được gì. Chat mới, /clear hoặc chat tách nhánh phải gán lại.",
    "Phiên feature chỉ sửa được code khi thiết kế của nó đã tới vòng 4 trong Management/game-design/live (node feature khai thư mục code trong code.json). Bị khoá thì việc tiếp theo là của @GameDesigner và bạn (tab Game design).",
    "Tiến độ: @CodeLead đánh dấu từng node lá xong tới giai đoạn nào sau khi biên dịch và test; bạn sửa lại được ở tab Game design.",
    "Phiên feature không biên dịch hay chạy test được. Khi nó báo xong, @CodeLead biên dịch và chạy test (hoặc bạn chạy trong Unity).",
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
    "- Toàn bộ feature của bạn",
    "- Của feature khác chỉ được đọc: CLAUDE.md ở gốc feature, Scripts/Contracts, FeatureAssets/Public. Internal, View, Tests của họ bị guard chặn; đừng thử đọc.",
    "- <ĐỀ XUẤT LIÊN QUAN trong Management/proposals, nếu có>",
    "",
    "Việc cần làm:",
    "1. <VIỆC 1>",
    "2. <VIỆC 2>",
    "",
    "Trước khi sửa Scripts/Contracts của bạn, *.asmdef, AssemblyInfo.cs/InternalsVisibleTo: gửi @CodeLead ngữ cảnh, file, đề xuất và chờ thống nhất. Core, _Composition: nhờ @Core/@Composition. rules.json: dừng và hỏi người.",
    "Xong thì báo @CodeLead (khối báo cáo); lỗi @CodeLead báo về thì tự sửa.",
    "Cần đổi thứ gì ở feature khác: viết đề xuất vào Management/proposals rồi dừng.",
    "Xoá file chỉ qua unity-mcp (manage_asset, action delete, path bắt đầu bằng Assets/).",
    "",
    "Xong khi: python Management/tools/validate.py báo 0 vi phạm, code của bạn biên dịch được, và bạn đã nêu rõ test nào cần người chạy."
  ].join("\n")
};
