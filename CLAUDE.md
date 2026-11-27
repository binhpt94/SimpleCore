# SimpleCore
Game Unity ghép từ các feature ĐỘC LẬP. AI viết code và đề xuất ý tưởng; người định hướng và duyệt.

## Luật chung
- Việc chưa được duyệt thì không làm.
- Người chỉ chat trực tiếp với `@human` và `@Manager`, ở khung chat VS Code (người quyết 2026-11-27). Hai chat này không chạy nền; Manager ở khung chat không có profile của vai (khoảng 2× token mỗi lượt, người chấp nhận).
- **Chỉ @Manager hỏi người.** Mọi thứ một phiên cần từ người (quyết định, chọn phương án, cho phép một lệnh guard cần người duyệt) đều đi qua Manager.
- Leader (tầng 2) chạy nền có profile của vai (bật Live ở trang Setup; mở tay trong VS Code thì mất profile, tốn gấp đôi token). Người không chat với Leader; xem khi cần bằng `claude attach <Tên>`.
- Leader không hỏi người. Guard chặn AskUserQuestion và mọi lần cần người duyệt của Leader. Cần người thì:
  - ghi câu hỏi: `python Management/tools/ask.py add --from <vai> "<việc>" --blocker "<vướng ở>" --options "<A …·B …>" --suggest "<đề nghị>" --waiting "<ai đang chờ>"`;
  - nhắn Manager (SendMessage) `HỎI NGƯỜI · <id> · <việc>`, rồi làm tiếp việc không phụ thuộc;
  - Manager hỏi người, ghi `ask.py answer`, nhắn lại `TRẢ LỜI · <id> · <người quyết gì>`. Người cho phép một lệnh thì Manager ghi kèm `--allow`, và đúng lệnh đó của Leader chạy được trong 30 phút, số lần người cho.
- Tầng 3–7 chạy theo việc (headless), không ai trả lời câu hỏi: không hỏi người. Guard chặn AskUserQuestion và mọi lần cần người duyệt của các phiên này (người quyết 2026-11-26).
  - Cần người quyết hay bị chặn: dừng, nhắn Leader của phần mình (cột "Báo cáo, báo chặn tới") `BÁO CHẶN · chờ người · Việc: … · Vướng ở: … · Cần quyết: …`, rồi kết thúc lượt.
  - Leader tự trả lời nếu được; không thì hỏi người qua Manager (như trên). Có câu trả lời thì Leader gửi `TRẢ LỜI · <việc> · <người quyết gì>` vào hộp thư của phiên đó (`dispatch.py send`). Tin đó mở lượt mới; phiên làm tiếp từ chỗ dừng.
- Việc người đã quyết mà phải sửa công cụ, guard hay luật: Leader ghi bằng `python Management/tools/toolwork.py add "<việc>" --from <vai> --decided "<người quyết gì, ngày nào>"`. Mọi phiên xem bằng `toolwork.py list`. Phiên `@human` làm rồi ghi `done` hoặc `drop`.
- Quyền do `.claude/hooks/guard.py` ép theo `FEATURE` của phiên.
- Người gán trong chat bằng `FEATURE=<giá trị> <PIN>`. Chưa gán thì cấm mọi thao tác sửa. AI không tự gán.
- Bị guard chặn thì dừng, không tìm đường vòng: Leader hỏi người qua Manager (`ask.py`); tầng 3–7 nhắn Leader `BÁO CHẶN`.
- Lượt có sửa file kết thúc bằng báo cáo theo `.claude/rules/report.md`.
- Không dùng Claude in Chrome.
- Tool deferred (unity-mcp, SendMessage...): nạp mọi tool lượt này sẽ cần trong MỘT `ToolSearch` `select:a,b,c` ở đầu lượt, không nạp từng cái (mỗi lần nạp là một lần gọi, đọc lại cả ngữ cảnh). Bộ thường dùng (tên đủ `mcp__UnityMCP__<tool>`): feature, service, @Core `refresh_unity,read_console,manage_asset,manage_scriptable_object` và `SendMessage`; @CodeLead thêm `run_tests,get_test_job,manage_scene,manage_editor`; tester `manage_scene,manage_editor,read_console,execute_menu_item,execute_code,manage_camera`; Manager, Leader khác `SendMessage`.

## Tầng
| Tầng | Nhóm | `FEATURE` | Nhận việc từ | Báo cáo, báo chặn tới | Đọc lúc bắt đầu | Skill |
|---|---|---|---|---|---|---|
| 0 | Master | `@human` | | | | |
| 1 | Managers | `@Manager` | người | người (chat trực tiếp) | `.claude/rules/manager.md` | `manager-` |
| 2 | Leaders | `@CodeLead`, `@GameDesigner`, `@UIDesigner` | @Manager | @Manager | `.claude/rules/<vai>.md` | `codelead-`, `gd-`, `ui-` |
| 3 | Tools | `@Codex`, `@Blender`, `@ClaudeImage`, `@DesignAdvisor` | order của feature, @GameDesigner, @UIDesigner | Leader đã giao order (order của feature: @CodeLead) | `studio.md`; `designadvisor.md` | `studio-`, `advisor-` |
| 4 | Testers | `@UnityAutoPlay` | @CodeLead, người | @CodeLead | `tester.md` | `tester-` |
| 5 | Basic | `@Core` | @CodeLead | @CodeLead | `feature.md` | `feature-` |
| 6 | Services | `_Services/<Tên>` | @CodeLead | @CodeLead | `feature.md` | `feature-` |
| 7 | Features | `<Tên>` (không có `@`) | @CodeLead | @CodeLead | `feature.md` | `feature-` |

- Tầng của phiên: dòng `guard:` đầu tin ghi `(tầng N · <nhóm>)`.
- Tầng dưới là tầng 4–7; @CodeLead nắm.
- Tên chat là tên vai bỏ `@`.
- Read file luật của vai khi dòng `guard:` nhắc (sau khi gán, sau mỗi lần compact).
- Quy trình từng việc nằm ở skill `.claude/skills/<tên>/SKILL.md`. Gọi bằng tool Skill khi làm đúng việc đó. Dòng `guard:` nêu skill của vai.
- `.claude/rules` là luật nền. Skill không mở thêm quyền nào.

## Cần làm việc gì thì đọc gì
| Việc | Đọc |
|---|---|
| Code feature, service, @Core: chỗ để code, cổng vòng 4, đổi khung, `validate.py`, `gd_pack.py`, `CLAUDE.md` của feature | `.claude/rules/feature.md` |
| Sổ yêu cầu `gd_req.py` | skill `feature-implement-req`; phía kiểm: `codelead-verify-batch` |
| unity-mcp, khoá Editor, chụp màn hình, xoá file | `.claude/rules/unity.md` |
| Order ảnh, 3D, `studio.py` | `.claude/rules/studio.md`; feature đặt và chấm: skill `feature-order-asset` |
| Bảng dữ liệu `.excel`, `gd_excel.py` | skill `gd-excel-data` |
| Frame bố cục `gd_frame.py`, ảnh tham chiếu `references.py` | `.claude/rules/uidesigner.md` |
| Chơi thử, `tester.py` | `.claude/rules/tester.md` |
| Tạo thư mục feature `scaffold.py` (@Manager, @CodeLead) | `.claude/rules/codelead.md`, `.claude/rules/manager.md` |
| Hỏi người, `ask.py` (Leader ghi, Manager hỏi và trả lời) | `.claude/rules/manager.md`, mục "Hỏi người" |
| Nháp `draft.py`, phiếu Editor `editor_ticket.py`, kiểm biên dịch `compile_check.py` | `.claude/rules/unity.md`, mục "Nháp, phiếu Editor"; feature: `.claude/rules/feature.md` |
| Thử lại guard | `python .claude/hooks/test_guard.py` |

- Lệnh shell chạy từ gốc project.
- Mọi phiên đã gán, kể cả subagent của nó, chạy được mọi lệnh shell và Python (người quyết 2026-11-26): pipe, nối lệnh, `python -c`, script riêng. Guard chỉ còn giữ cổng của `dispatch.py send`, `ask.py add|answer|cancel`, `draft.py` (nháp của mình; `apply` chỉ khi giữ phiếu Editor hay Editor trống), `editor_ticket.py` (phiếu của mình), `gd_req.py doing|done`, `studio.py review|cancel` và giai đoạn, cả trong chuỗi lệnh. Sửa file vẫn bằng Edit/Write theo bảng Vùng; không dùng shell để sửa vùng mình bị cấm. Phiên chưa gán chỉ chạy được các lệnh trong danh sách của guard. Gộp lệnh để bớt lượt gọi.

## Nhắn ai
- Tầng 3, 5, 6, 7 chạy theo việc. Giao việc cho chúng bằng hộp thư: `python Management/tools/dispatch.py send <tên> --from <vai> "<việc>"` (Leader, theo phần mình) hoặc tab Hộp thư (người).
- Nội dung tin trong ngoặc kép, không chứa `;` `&` `|` `<` `>` `$` `(` `)` `{` `}`, dấu ngoặc kép hay `\`. Ngăn ý bằng `·` hoặc dấu phẩy.
- Trong PowerShell, `@Tên` không có ngoặc là splatting nên tên biến mất: viết `'@Codex'` trong ngoặc đơn, bỏ `@` (`Codex`), hoặc chạy bằng Bash. Guard chặn lệnh PowerShell có `@Tên` trần.
- `gd_req.py reject` và `studio.py check|review 0|deliver|redo` tự gửi tin.
- `SendMessage` chỉ dùng giữa Manager và các Leader, giữa các Leader, và tới tester đang chạy.
- Tầng 3–7 chỉ nhắn Leader của phần mình (tầng dưới: @CodeLead). Không nhắn @Manager.
- Lệnh hay dòng `guard:` báo SERVER TẮT: dừng việc phụ thuộc, BÁO CHẶN, chờ người bật server.
- unity-mcp lỗi kết nối, hay lệnh in `CODEX LỖI`: dừng việc đó, không thử lại, không đi đường khác; báo Leader (Unity) hay bên giao việc (Codex) MỘT tin BÁO CHẶN rồi kết thúc lượt. Guard chặn lần thử lại trong cùng lượt.

## Giai đoạn
- Project làm theo giai đoạn IDEA, PROTOTYPE, FULL. Người đặt "giai đoạn đang làm" ở tab Game design. Dòng `guard:`, `gd_status.py` và `gd_req.py status` nêu nó.
- Mỗi node thiết kế có giai đoạn của nó (điểm độ quan trọng).
- Node của giai đoạn đang làm và trước đó là tới hạn: thiết kế tới vòng 4, yêu cầu của nó là việc dev.
- Node của giai đoạn sau: sổ yêu cầu ghi "để sau", không giao, `doing`/`done` bị từ chối.
- Chưa đặt giai đoạn: @CodeLead chưa giao được việc dev, @Manager hỏi người.
- Nghĩa của giai đoạn (người quyết 2026-11-27): IDEA, PROTOTYPE, FULL là ba mức của cùng một game, lồng nhau. PROTOTYPE gồm mọi thứ của IDEA rồi thêm; FULL gồm mọi thứ của PROTOTYPE rồi thêm.
- Người bỏ một thứ (ý, node, feature, luật) thì nó bị bỏ ở mọi giai đoạn: cả giai đoạn sau, cột PROTOTYPE, FULL của bảng "Thông số theo giai đoạn", bảng `.excel`, node để sau, cheat, code, test, asset. Chỉ giữ cho giai đoạn sau khi người nói rõ "bỏ ở <giai đoạn>, giữ cho <giai đoạn>".
- "Chưa làm ở giai đoạn này" khác "bỏ": thứ chưa làm nằm ở node có điểm của giai đoạn sau. Thứ đã bỏ không được giữ lại dưới dạng cờ tắt, nhánh code, test, hay ghi chú "không thuộc IDEA".

## Vùng
| Vùng | Đọc | Sửa |
|---|---|---|
| Feature của mình | được | được; code (`.cs`) nên viết ở nháp, nạp khi giữ phiếu Editor (soft gate) |
| `UnityProject/Drafts/<Tên>` (nháp code) | được | phiên của feature đó, cùng luật vùng như thư mục thật; @CodeLead mọi nháp; `.draft.json` chỉ `draft.py` |
| Khung của feature mình: `Scripts/Contracts`, `Scripts/Internal/Base`, `Scripts/View/Base`, `*.asmdef`, `AssemblyInfo.cs` | được | chỉ @CodeLead |
| Khung của Core: `*.asmdef`, `AssemblyInfo.cs`, `I*.cs`, `*Base.cs` (ngoài `Tests/`) | được | chỉ @CodeLead |
| `Scripts/Contracts`, `FeatureAssets/Public`, `CLAUDE.md` của feature khác | được | cấm |
| `Scripts/Internal`, `Scripts/View`, `FeatureAssets/Private` của feature khác | cấm | cấm |
| `Assets/Core` | được | chỉ `@Core` (trừ khung) |
| `Assets/_Composition` | được | chỉ @CodeLead |
| Code (`Scripts/Internal`, `Scripts/View`, `Tests`) của mọi feature, service, `Assets/Core` | được | thêm @CodeLead, chỉ để sửa lỗi biên dịch nhanh (soft gate, người quyết 2026-11-27) |
| `Assets/SimpleCoreTools` (script Editor của project) | được | chỉ `@human` |
| `UnityProject/Packages`, `UnityProject/ProjectSettings`, `Management/tools/rules.json` | được | báo @CodeLead; @CodeLead hỏi người qua Manager (`ask.py`, `--allow`), từng lần |
| `Management/toolwork` | được | chỉ `toolwork.py` |
| `Management/asks` | được | chỉ `ask.py` (`add`: Leader; `answer`: @Manager) |
| `Management/editor-tickets` | được | chỉ `editor_ticket.py` và server |
| `Management/studio` | được | feature, @GameDesigner, @UIDesigner: tạo `order.md` mới trong thư mục order của mình; tool: `lessons.md`, `work/` của order; còn lại chỉ `studio.py` và trang |
| `Resources` | được | chỉ `studio.py` |
| `Management/game-design/ai` | được | chỉ @GameDesigner; `.frame-concept/*.frame` chỉ @UIDesigner |
| `Management/game-design/advice` | được | chỉ @DesignAdvisor và `gd_advise.py` |
| Phần còn lại của `Management` (`game-design/live` là thiết kế đã chốt: bám theo nó) | được | cấm |
| `.claude/**`, `CLAUDE.md` gốc | được | cấm |
| `Tester` | tầng 1, 2, 4, 5, 6, 7 | chỉ tester, trong thư mục của mình |
| `Tester/<Tên>/Archive` | chỉ tầng 4 | chỉ `tester.py archive` |
| `Library`, `Temp`, `Logs`, `obj`, `UserSettings` | cấm | cấm |
| Thư mục tạm của chính phiên (`tool-results`, `tasks`, `scratchpad`) | được | chỉ `scratchpad` |
| Thư mục tạm của phiên khác | cấm; @CodeLead đọc được của tầng 6, 7 | cấm |

- Vùng không có trong bảng: cấm sửa; phiên chưa gán cấm cả đọc.
- Cột "Đọc" chỉ còn giữ phiên chưa gán (người quyết 2026-11-26): mọi phiên đã gán, kể cả subagent, đọc và tìm (Read, Grep, Glob, shell) được mọi nơi trong project, trừ `Library`, `Temp`, `Logs`, `obj`, `UserSettings`, `Tester/<Tên>/Archive` (chỉ tầng 4) và thư mục tạm của phiên khác. Cột "Sửa" giữ nguyên.
- `Management/index.html` chỉ là vỏ hiển thị; nội dung đã duyệt nằm trong `Management/data/`.
