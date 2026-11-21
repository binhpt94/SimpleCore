// Hướng dẫn gán chat Claude Code theo feature. index.html nạp file này bằng thẻ <script> nên phải gán vào window.
// Thêm feature mới: thêm một dòng vào `features`. `id` là giá trị gõ sau FEATURE=.
window.CORESIMPLE_SESSIONS = {
  intro: "Mỗi chat bị guard nhốt trong một feature. Mở chat mới trong VS Code, gõ dòng đầu tiên FEATURE=<giá trị> <PIN>. Nút Chép chỉ chép phần trước PIN; bạn tự gõ PIN rồi Enter.",

  pinTitle: "Đặt PIN (một lần)",
  pinHint: "Chạy trong terminal của VS Code ở gốc project, tự tay gõ, không nhờ AI. Gõ PIN hai lần. Sai PIN 5 lần thì bị khoá cho tới khi chạy lại lệnh này.",
  pinCommand: "python .claude/hooks/bind_session.py --set-pin",

  // `copy`: chữ được chép, khi khác "FEATURE=<id> ".
  features: [
    { id: "@human", label: "Phiên của người (tích hợp)", note: "Không giới hạn feature; vẫn bị hỏi khi sửa Core, _Composition, .claude. Là phiên duy nhất biên dịch, chạy test và Play qua unity-mcp." },
    { id: "<Tên>", label: "Phiên feature", copy: "FEATURE=", note: "Tên thư mục trong Assets/Features, ví dụ Wallet; dịch vụ thì thêm _Services/, ví dụ _Services/Popup." },
    { id: "@Core", label: "Phiên Core", note: "Chỉ sửa Assets/Core (.asmdef, InternalsVisibleTo vẫn hỏi); đọc phần công khai của feature." },
    { id: "@Composition", label: "Phiên Composition", note: "Chỉ sửa Assets/_Composition: Boot variant, scene, catalog asset; đọc phần công khai của feature. Việc cần Unity (scene, prefab) nhờ @human." },
    { id: "@Manager", label: "Phiên điều phối", note: "Không sửa code. Thấy báo cáo mới của các phiên ở đầu mỗi tin, nhắn các phiên bằng SendMessage, viết đề xuất trong Management/proposals." }
  ],

  notes: [
    "Mỗi chat một feature, nhiều chat chạy chung một cửa sổ VS Code. Gõ FEATURE? để xem chat đang gán gì.",
    "Không đưa PIN cho AI.",
    "Phiên feature, dịch vụ, @Core, @Composition: lượt có sửa file phải kết thúc bằng báo cáo cho @Manager (.claude/rules/report.md); hook lưu vào Management/reports.",
    "Chat chưa gán thì không sửa được gì. Chat mới, /clear hoặc chat tách nhánh phải gán lại.",
    "Phiên feature không biên dịch hay chạy test được. Khi nó báo xong, chạy test trong Unity hoặc nhờ phiên @human.",
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
    "Dừng và hỏi người trước khi sửa: Scripts/Contracts của bạn, bất kỳ *.asmdef nào, AssemblyInfo.cs, Core, _Composition, rules.json.",
    "Cần đổi thứ gì ở feature khác: viết đề xuất vào Management/proposals rồi dừng.",
    "Xoá file chỉ qua unity-mcp (manage_asset, action delete, path bắt đầu bằng Assets/).",
    "",
    "Xong khi: python Management/tools/validate.py báo 0 vi phạm, code của bạn biên dịch được, và bạn đã nêu rõ test nào cần người chạy."
  ].join("\n")
};
