// Nội dung tab Cài đặt của trang quản lý (chuẩn bị một máy). index.html nạp file này bằng thẻ <script> nên phải gán
// vào window. Gán chat làm ở tab Setup (Management/tools/setup_server.py).
window.CORESIMPLE_SESSIONS = {
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
        { command: "python Management/tools/test_art.py" },
        { text: "Setup, thư mục và nhóm chat:", command: "python Management/tools/test_scaffold.py\npython Management/tools/test_vscode_groups.py" },
        { text: "Mở một chat chưa gán rồi nhờ AI đọc một file bất kỳ. Guard phải trả lời (đọc được, hoặc chặn kèm lý do bắt đầu bằng guard:). Nếu báo lỗi đường dẫn thì dừng lại, đừng gán chat, chạy lại test_guard.py." }
      ] },
      { title: "3. PIN và gán chat", steps: [
        { text: "PIN và các chat đã gán nằm ngoài project, trong ~/.coresimple-guard (Windows: C:\\Users\\<bạn>\\.coresimple-guard). Mỗi máy đặt PIN riêng bằng lệnh:", command: "python .claude/hooks/bind_session.py --set-pin" }
      ] },
      { title: "4. Trang quản lý và Setup", steps: [
        { text: "Mở trang qua server cục bộ để lưu được điểm, câu trả lời, tạo thread, và chọn art game dùng (tab Studio). Server chỉ nghe trên 127.0.0.1, cổng 8770.", command: "python Management/tools/setup_server.py" },
        { text: "Setup full gồm cả các feature mà thiết kế live đã khai trong code.json, kể cả khi chưa có thư mục trong Unity: thread được tạo và gán ngay, và sửa được sau khi @Manager tạo thư mục (lệnh dưới, có --dry-run để xem trước).", command: "python Management/tools/scaffold.py" },
        { text: "Sau khi đổi luật hay guard: ở tab Setup, bảng \"Thread hiện có\" có nút \"Đọc lại quy trình\" cho từng thread và nút \"Đọc lại quy trình: tất cả\". Server chỉ báo cho thread các file luật của vai trò nó (CLAUDE.md gốc, luật chung, luật vai trò) đã đổi từ lần đọc trước, bằng một lượt ngầm (claude --resume); thread không có file nào đổi thì không chạy lượt nào, nên bấm \"tất cả\" chỉ tốn lượt ở thread cần đọc. Chat đang mở trong VS Code có thể chưa thấy lượt này: mở lại chat đó. Thread do Setup tạo mới được gán trước rồi nhận luôn câu này ở lượt đầu." },
        { text: "Cùng cột đó có hai nút nữa, mỗi nút cũng có bản \"tất cả\". Compact: server chạy /compact trong thread để nén ngữ cảnh; không cần PIN, không đổi quyền. Renew: làm mới thread ngay trong chat đó: bỏ toàn bộ ngữ cảnh cũ, gán lại đúng FEATURE đang gán, rồi nhắc đọc quy trình; cần PIN (ô PIN của tab Setup) vì có gán quyền. Renew tốn ba lượt ngắn; việc dở dang chưa ghi ra file sẽ mất, nên chỉ bấm khi thread đang rảnh. Tin nhắn cũ vẫn còn trong lịch sử chat (phía trên vạch compact) nhưng thread không còn nhớ chúng. \"Renew: tất cả\" hỏi lại một lần và chạy từng thread một; PIN sai thì dừng ngay." },
        { text: "Gom các thread theo tầng (1 · Master … 6 · Features) trong danh sách chat của VS Code: tắt hẳn VS Code (mọi cửa sổ), chạy lệnh trong terminal ngoài VS Code, rồi mở lại. VS Code đang chạy thì lệnh từ chối, vì nó sẽ ghi đè. Có sao lưu; khôi phục bằng --restore. Nơi extension lưu nhóm không có trong tài liệu; nếu bản cập nhật đổi cách lưu, lệnh dừng và báo, khi đó gom tay bằng chuột phải vào session.", command: "python Management/tools/vscode_groups.py" },
        { text: "Tạo thread cần lệnh claude (Claude Code CLI) có trong PATH. Thread đã lưu trữ trong VS Code được bỏ qua: server tự đọc trạng thái VS Code ở đúng chỗ của từng hệ (Windows: %APPDATA%\\Code, macOS: ~/Library/Application Support/Code, Linux: ~/.config/Code).", command: "claude --version" }
      ] },
      { title: "5. Codex, Blender và công cụ vẽ SVG", steps: [
        { text: "@DesignAdvisor hỏi GPT qua lệnh codex (Codex CLI, đã đăng nhập). Worker vẽ art Simple: model 3D bằng blender chạy headless, ảnh bằng SVG đổi ra PNG (art.py svg, cần rsvg-convert, inkscape, magick, Chrome hoặc Edge). Kiểm tra:", command: "codex --version\nblender --version\ninkscape --version" }
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
  }
};
