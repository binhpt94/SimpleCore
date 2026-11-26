# SimpleCore
Game Unity ghép từ các feature ĐỘC LẬP. AI viết code và đề xuất ý tưởng; người định hướng và duyệt.

## Luật chung
- Việc chưa được duyệt thì không làm.
- Leader (tầng 1) cần người quyết thì hỏi ngay trong chat (vấn đề, các phương án, phương án đề nghị, ai đang bị chặn), rồi làm tiếp việc không phụ thuộc.
- Tầng 2–6 chạy theo việc (headless), không ai trả lời câu hỏi: không hỏi người. Guard chặn AskUserQuestion và mọi lần cần người duyệt của các phiên này (người quyết 2026-11-26).
  - Cần người quyết hay bị chặn: dừng, nhắn Leader của phần mình (cột "Báo cáo, báo chặn tới") `BÁO CHẶN · chờ người · Việc: … · Vướng ở: … · Cần quyết: …`, rồi kết thúc lượt.
  - Leader hỏi người trong chat của nó, rồi gửi `TRẢ LỜI · <việc> · <người quyết gì>` vào hộp thư của phiên đó (`dispatch.py send`). Tin đó mở lượt mới; phiên làm tiếp từ chỗ dừng.
- Việc người đã quyết mà phải sửa công cụ, guard hay luật: Leader ghi bằng `python Management/tools/toolwork.py add "<việc>" --from <vai> --decided "<người quyết gì, ngày nào>"`. Mọi phiên xem bằng `toolwork.py list`. Phiên `@human` làm rồi ghi `done` hoặc `drop`.
- Quyền do `.claude/hooks/guard.py` ép theo `FEATURE` của phiên.
- Người gán trong chat bằng `FEATURE=<giá trị> <PIN>`. Chưa gán thì cấm mọi thao tác sửa. AI không tự gán.
- Bị guard chặn thì dừng, không tìm đường vòng: Leader báo người trong chat; tầng 2–6 nhắn Leader `BÁO CHẶN`.
- Lượt có sửa file kết thúc bằng báo cáo theo `.claude/rules/report.md`.
- Không dùng Claude in Chrome.

## Tầng
| Tầng | Nhóm | `FEATURE` | Nhận việc từ | Báo cáo, báo chặn tới | Đọc lúc bắt đầu | Skill |
|---|---|---|---|---|---|---|
| 0 | Master | `@human` | | | | |
| 1 | Leaders | `@Manager`, `@CodeLead`, `@GameDesigner`, `@UIDesigner` | người; @Manager điều phối ba Leader còn lại | @Manager | `.claude/rules/<vai>.md` | `manager-`, `codelead-`, `gd-`, `ui-` |
| 2 | Tools | `@Codex`, `@Blender`, `@ClaudeImage`, `@DesignAdvisor` | order của feature, @GameDesigner, @UIDesigner | Leader đã giao order (order của feature: @CodeLead) | `studio.md`; `designadvisor.md` | `studio-`, `advisor-` |
| 3 | Testers | `@UnityAutoPlay` | @CodeLead, người | @CodeLead | `tester.md` | `tester-` |
| 4 | Basic | `@Core` | @CodeLead | @CodeLead | `feature.md` | `feature-` |
| 5 | Services | `_Services/<Tên>` | @CodeLead | @CodeLead | `feature.md` | `feature-` |
| 6 | Features | `<Tên>` (không có `@`) | @CodeLead | @CodeLead | `feature.md` | `feature-` |

- Tầng của phiên: dòng `guard:` đầu tin ghi `(tầng N · <nhóm>)`.
- Tầng dưới là tầng 3–6; @CodeLead nắm.
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
| Tạo thư mục feature `scaffold.py` | `.claude/rules/manager.md` |
| Thử lại guard | `python .claude/hooks/test_guard.py` |

- Lệnh shell chạy từ gốc project.
- Bốn Leader nối được các lệnh đã cho phép bằng `;` trong một lần gọi (tối đa 6, mỗi lệnh phải tự nó được phép) và chạy được `scaffold.py --dry-run`. Phiên khác mỗi lần gọi một lệnh.

## Nhắn ai
- Tầng 2, 4, 5, 6 chạy theo việc. Giao việc cho chúng bằng hộp thư: `python Management/tools/dispatch.py send <tên> --from <vai> "<việc>"` (Leader, theo phần mình) hoặc tab Hộp thư (người).
- Nội dung tin trong ngoặc kép, không chứa `;` `&` `|` `<` `>` `$` `(` `)` `{` `}`, dấu ngoặc kép hay `\`. Ngăn ý bằng `·` hoặc dấu phẩy.
- `gd_req.py reject` và `studio.py check|review 0|deliver|redo` tự gửi tin.
- `SendMessage` chỉ dùng giữa các Leader và tới tester đang chạy.
- Tầng 2–6 chỉ nhắn Leader của phần mình (tầng dưới: @CodeLead). Không nhắn @Manager.
- Lệnh hay dòng `guard:` báo SERVER TẮT: dừng việc phụ thuộc, BÁO CHẶN, chờ người bật server.
- unity-mcp lỗi kết nối, hay lệnh in `CODEX LỖI`: dừng việc đó, không thử lại, không đi đường khác; báo Leader (Unity) hay bên giao việc (Codex) MỘT tin BÁO CHẶN rồi kết thúc lượt. Guard chặn lần thử lại trong cùng lượt.

## Giai đoạn
- Project làm theo giai đoạn IDEA, PROTOTYPE, FULL. Người đặt "giai đoạn đang làm" ở tab Game design. Dòng `guard:`, `gd_status.py` và `gd_req.py status` nêu nó.
- Mỗi node thiết kế có giai đoạn của nó (điểm độ quan trọng).
- Node của giai đoạn đang làm và trước đó là tới hạn: thiết kế tới vòng 4, yêu cầu của nó là việc dev.
- Node của giai đoạn sau: sổ yêu cầu ghi "để sau", không giao, `doing`/`done` bị từ chối.
- Chưa đặt giai đoạn: @CodeLead chưa giao được việc dev, @Manager hỏi người.

## Vùng
| Vùng | Đọc | Sửa |
|---|---|---|
| Feature của mình | được | được |
| Khung của feature mình: `Scripts/Contracts`, `Scripts/Internal/Base`, `Scripts/View/Base`, `*.asmdef`, `AssemblyInfo.cs` | được | chỉ @CodeLead |
| Khung của Core: `*.asmdef`, `AssemblyInfo.cs`, `I*.cs`, `*Base.cs` (ngoài `Tests/`) | được | chỉ @CodeLead |
| `Scripts/Contracts`, `FeatureAssets/Public`, `CLAUDE.md` của feature khác | được | cấm |
| `Scripts/Internal`, `Scripts/View`, `FeatureAssets/Private` của feature khác | cấm | cấm |
| `Assets/Core` | được | chỉ `@Core` (trừ khung) |
| `Assets/_Composition` | được | chỉ @CodeLead |
| `UnityProject/Packages`, `UnityProject/ProjectSettings`, `Management/tools/rules.json` | được | báo @CodeLead; @CodeLead hỏi người, từng lần |
| `Management/toolwork` | được | chỉ `toolwork.py` |
| `Management/studio` | được | feature, @GameDesigner, @UIDesigner: tạo `order.md` mới trong thư mục order của mình; tool: `lessons.md`, `work/` của order; còn lại chỉ `studio.py` và trang |
| `Resources` | được | chỉ `studio.py` |
| `Management/game-design/ai` | được | chỉ @GameDesigner; `.frame-concept/*.frame` chỉ @UIDesigner |
| `Management/game-design/advice` | được | chỉ @DesignAdvisor và `gd_advise.py` |
| Phần còn lại của `Management` (`game-design/live` là thiết kế đã chốt: bám theo nó) | được | cấm |
| `.claude/**`, `CLAUDE.md` gốc | được | cấm |
| `Tester` | tầng 1, 3, 4, 5, 6 | chỉ tester, trong thư mục của mình |
| `Tester/<Tên>/Archive` | chỉ tầng 3 | chỉ `tester.py archive` |
| `Library`, `Temp`, `Logs`, `obj`, `UserSettings` | cấm | cấm |
| Thư mục tạm của chính phiên (`tool-results`, `tasks`, `scratchpad`) | được | chỉ `scratchpad` |
| Thư mục tạm của phiên khác | cấm; @CodeLead đọc được của tầng 5, 6 | cấm |

- Vùng không có trong bảng: cấm đọc và sửa.
- Tìm kiếm (`ls`, `find`, `grep`, `rg`, mỗi lần một lệnh; hoặc Grep/Glob) phải nêu thư mục nằm trong một vùng được đọc. Mẫu đặt trong ngoặc đơn; không pipe, không chuyển hướng, không `-exec`.
- `Management/index.html` chỉ là vỏ hiển thị; nội dung đã duyệt nằm trong `Management/data/`.
