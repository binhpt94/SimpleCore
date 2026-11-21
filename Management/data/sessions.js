// Hướng dẫn mở phiên Claude Code theo feature. index.html nạp file này bằng thẻ <script> nên phải gán vào window.
// Thêm feature mới: thêm một dòng vào `features`. `id` là giá trị của biến FEATURE.
window.CORESIMPLE_SESSIONS = {
  root: "d:\\AI\\CoreSimple",

  intro: "Mỗi phiên là một terminal PowerShell riêng. Biến FEATURE chỉ có hiệu lực trong terminal gõ nó, nên mở nhiều terminal là chạy song song nhiều feature; guard nhốt mỗi phiên trong feature của nó.",

  features: [
    { id: "@human", label: "Phiên của người (tích hợp)", note: "Không giới hạn feature; vẫn bị hỏi khi sửa Core, _Composition, .claude. Là phiên duy nhất biên dịch, chạy test và Play qua unity-mcp." },
    { id: "Wallet", label: "Wallet", note: "Số xu của người chơi." },
    { id: "Shop", label: "Shop", note: "Bán vật phẩm bằng xu. Cài sau Wallet và Popup." },
    { id: "SceneFlow", label: "SceneFlow", note: "Chuyển từ scene Loading sang scene Game." },
    { id: "_Services/Popup", label: "Popup (dịch vụ)", note: "Hàng đợi popup." },
    { id: "_Services/SessionScene", label: "SessionScene (dịch vụ)", note: "Vòng đời phiên của một scene." }
  ],

  notes: [
    "Dùng extension VS Code thì đặt FEATURE trước khi mở VS Code: extension lấy biến môi trường của cửa sổ đó.",
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
