---
paths:
  - "UnityProject/Assets/Features/**"
---
# Basic, Services, Features: luật của phiên code
Áp cho tầng 5 (`@Core`), tầng 6 (`_Services/<Tên>`), tầng 7 (`<Tên>`). Luật code Unity ở `.claude/rules/unity.md`; quy trình từng việc ở skill `feature-*`.

## Chỗ để code
- Feature nằm ở `UnityProject/Assets/Features/<Tên>/`.
- Dịch vụ ở `Features/_Services/<Tên>/`.
- @Core chỉ sửa `Assets/Core`.
- Không tạo `Shared/`.
- Phiên Features/Services chỉ sửa được code của mình khi thiết kế của nó đã tới vòng 4 trong `Management/game-design/live` (guard kiểm).
- Dòng `guard:` đầu tin báo code đang mở hay khoá.
- Bị khoá thì dừng, không hỏi người, nhắn Leader của phần mình `BÁO CHẶN` rồi kết thúc lượt (`CLAUDE.md` gốc, mục Luật chung).

## Thiết kế để bám theo
- `python Management/tools/gd_pack.py <Tên>`: gói thiết kế đã chốt của feature (node feature, node con, node nhánh khác mà refs trỏ tới, thuật ngữ, frame bố cục).
- Bám theo gói này khi code.
- Gói ghi ra `Management/game-design/.pack/`; lệnh in đường dẫn và mục lục có số dòng.
- Đọc gói bằng Read (offset, limit) hoặc Grep.
- Trước khi code một phần, đọc đủ node vòng 4 và mục CHEAT của phần đó.
- Bảng dữ liệu `.excel` của node chỉ là thiết kế: phiên feature đọc (`gd_excel.py show <node>`, cũng có trong `gd_pack.py`) rồi tự nhập vào config asset của mình.
- Sổ yêu cầu `gd_req.py <Tên>`: làm từng yêu cầu theo skill `feature-implement-req`.
- Thứ người đã bỏ khỏi thiết kế bị bỏ ở mọi giai đoạn (mục "Giai đoạn" của `CLAUDE.md` gốc, người quyết 2026-11-27): xoá hẳn code, test, config, asset của nó. Không giữ cờ tắt, nhánh hay test "cho giai đoạn sau".

## Nháp và phiếu Editor (người quyết 2026-11-27)
Unity biên dịch chung cả project: code lỗi của một bên chặn mọi bên. Vì vậy code viết ở **nháp**, chỉ vào `Assets` khi tới lượt dùng Editor.
- **Viết nháp:** `python Management/tools/draft.py start <Tên> <đường dẫn trong thư mục của mình>` (file đã có) hoặc `draft.py new <Tên> <đường dẫn>` (file mới), rồi Edit/Write trong `UnityProject/Drafts/<Tên>/<đường dẫn>`. Xoá file: `draft.py delete`. Nháp được phép lỗi, kể cả dùng contract đang xin.
- **Kiểm nháp (nên làm, vài giây, không cần Editor):** `python Management/tools/compile_check.py <Tên> --draft`. `LỖI` là lỗi của mình; `CHẶN` là phụ thuộc ngoài mình đang lỗi, không phải lỗi của mình.
- **Nộp phiếu Editor:** `python Management/tools/editor_ticket.py add --session <Tên> --common refresh,clean [--task "<việc cần Editor: tạo asset, prefab>"]... [--req <node>#<id> --test <Lớp.Hàm>[,...]]...`. Rồi làm việc khác hoặc kết thúc lượt; **không BÁO CHẶN vì Editor bận**.
  - Phiếu chỉ có `refresh,clean` (chỉ nạp code): server nạp nháp của mọi phiếu như vậy cùng một lượt, biên dịch, báo kết quả. Lỗi biên dịch thì CodeLead sửa.
  - Phiếu có `--task`: tới lượt, server nhắn "Tới lượt bạn". Khi đó: `draft.py apply <Tên>` (nếu còn nháp), `refresh_unity`, đọc console, sửa lỗi, làm việc cần Editor, rồi `editor_ticket.py done --session <Tên> <việc>` cho từng việc. Kết thúc lượt là trả Editor.
  - `--req ... --test ...`: test của yêu cầu chạy ở lô test cuối vòng của server; qua hết thì server tự ghi `done` cho yêu cầu. Hỏng thì bạn nhận một tin.
  - Phiếu đang chờ: thêm việc thì `add` lại (giữ chỗ); huỷ: `editor_ticket.py drop --session <Tên> [<việc>]`. Xem hàng: `editor_ticket.py list`.
- **Nạp bị từ chối "lệch gốc":** file trong `Assets` đã đổi từ lúc bắt đầu nháp (thường là CodeLead sửa lỗi biên dịch). `draft.py diff`, trộn thay đổi đó vào nháp, `draft.py rebase <Tên> <file>`, nộp phiếu lại.
- Sửa thẳng `Assets` vẫn được (soft gate, guard nhắc), nhưng chỉ nên làm khi Editor trống và không ai chờ.
- Code của mình có thể được CodeLead sửa để gỡ lỗi biên dịch. Đầu việc mới, xem `git diff` thư mục của mình nếu cần.

## Kiểm ranh giới
- `python Management/tools/validate.py --feature <Tên>`: kiểm tra ranh giới feature.
- Bỏ `--feature` là kiểm cả project.
- Còn vi phạm thì chưa xong việc.
- Luật ranh giới và mã lỗi (STR/ASM/SRC/AST/DOC): xem đầu `Management/tools/validate.py`.
- Ngoại lệ đã duyệt nằm trong `Management/tools/rules.json`.

## Khung, xung đột
- Cần đổi khung (`Contracts`, lớp base, `*.asmdef`, `InternalsVisibleTo`): gửi @CodeLead ngữ cảnh, file, đề xuất (skill `feature-request-framework-change`).
- @CodeLead sửa khung, phiên mình chờ.
- Code mới viết trên lớp con.
- Xung đột code do @CodeLead giải quyết.
- Lỗi @CodeLead báo về thì phiên sở hữu tự sửa.
- Cần sửa `UnityProject/Packages`, `UnityProject/ProjectSettings`, `Management/tools/rules.json`: báo @CodeLead; @CodeLead hỏi người qua Manager, từng lần.
- Cấm `*.asmref`.

## CLAUDE.md của feature (DOC-02)
- Feature hay service có ít nhất một `.cs` trong `Scripts/Contracts` thì phải có `<feature>/CLAUDE.md`.
- Dòng 1: `# <Tên>`.
- Dòng 2: `Tầng: 6 · Features` (service: `Tầng: 5 · Services`).
- Rồi một câu: feature làm gì.
- Mục `## Contracts`: mỗi kiểu công khai trong `Scripts/Contracts` một mục `### <Tên kiểu>`.
- Mỗi mục ghi: dùng để làm gì; nghĩa của từng thành viên và sự kiện (lúc nào báo, giá trị gì); feature nào dùng.
- Mẫu: `UnityProject/Assets/Features/Wave/CLAUDE.md`.
- Tối đa 200 dòng (DOC-01).
- Code hiện thực hay đổi hành vi một contract thì cập nhật mục đó cùng lượt.
- `validate.py` báo DOC-02 khi thiếu file, thiếu dòng `Tầng:`, ghi sai tầng, hoặc thiếu một kiểu công khai.
- Feature không có contract: file không bắt buộc.

## Nhắn, báo cáo
- Chỉ nhắn @CodeLead (SendMessage khi nó đang chạy); không nhắn @Manager.
- Lượt có sửa file kết thúc bằng báo cáo theo `.claude/rules/report.md`, tới @CodeLead.
