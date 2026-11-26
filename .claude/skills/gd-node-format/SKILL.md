---
name: gd-node-format
description: Chỉ phiên FEATURE=@GameDesigner (@UIDesigner và @DesignAdvisor đọc để hiểu cấu trúc node). Cấu trúc một node thiết kế - các file của node, khuôn data.md (based-on, refs, ý cốt lõi, tiêu đề, một ý một quyết định), hash data, trang so với live, khuôn CLAUDE.md của node. Dùng trước khi viết hay sửa data.md, CLAUDE.md của một node, khi khai refs, hoặc khi gd_status.py báo lỗi cấu trúc.
---
# @GameDesigner: cấu trúc một node
Luật chung: `.claude/rules/gamedesigner.md`. Skill này là khuôn file của node; vòng nào viết gì thì ở skill `gd-round-<N>`.

## Một node
```
<key>/
  data.md                    dữ liệu cốt lõi: tiêu đề, tóm tắt, các ý có điểm        GD ghi
  CLAUDE.md                  diễn giải chi tiết; dòng đầu trỏ tới data.md           GD ghi
  priority.json              độ quan trọng do AI chấm, chỉ ở node lá                GD ghi
  code.json                  chỉ ở node feature (vòng 2): thư mục code của nó       GD ghi (xem skill gd-round-2)
  .progress/progress.json    chỉ ở node lá: tiến độ từng giai đoạn                  chỉ đọc (@CodeLead và người ghi)
  .images/                   ảnh + index.md (`- <tên file>: <ảnh để làm gì>`)       người bỏ ảnh, GD ghi index.md
  ui.md                      node vòng 4 có giao diện: phần tử UI, tương tác, flow    GD ghi (xem skill gd-round-4)
  .frame-concept/<tên>.frame bố cục dạng chữ (vị trí, kích thước), người chấm từng frame  @UIDesigner ghi (uidesigner.md)
  .excel/                    bảng dữ liệu config: <node>.xlsx, Index.csv, <Sheet>.csv       GD sửa csv, gd_excel.py ghi xlsx (skill gd-excel-data)
  .ai-need-user-choose/      questions.json (GD ghi), answers.json (người)
  .user-review/review.json   điểm và phản hồi của người cho từng ý                  chỉ đọc
  .history/                  log.jsonl, bản sao mọi phiên bản data.md               chỉ đọc
  <key con>/                 node của vòng sau
```
Key (tên thư mục): tiếng Anh, chữ thường, số, gạch nối. Key đã chốt trong `live` thì không đổi tên; muốn đổi thì hỏi người.

### `data.md`: chỉ gồm đúng ba phần
```
---
based-on: 3f9a1c2b7d10
refs: feature-enemy/enemy-stats/enemy-stat-values@91c0e2aa4b17, glossary#wave@5d2f8e01c3a9
---
# Hero

Nhân vật người chơi điều khiển, có máu và kĩ năng.

## Ý cốt lõi

### hero-count | AI 8 | Mỗi trận tối đa 5 hero cùng lúc
Đủ đông để thấy đội hình, đủ ít để màn hình điện thoại không rối. Đã loại: không giới hạn.

### hero-spawn | AI 6 | Hero tự sinh theo thời gian, không cần bấm
```
- `based-on`: hash data của node cha mà node này được viết dựa theo (lấy từ `gd_status.py`). Node gốc không có dòng này.
- `refs`: các node ở **nhánh khác** mà node này dựa vào, mỗi cái kèm hash data lúc bạn đã đọc và rà theo nó; một từ của glossary ghi `glossary#<id>@<hash của ý>`. Không tính vào hash data.
  - **Bắt buộc**: nhắc trong backtick tới một node vòng 3-4 không thuộc dòng dõi của mình (ví dụ `` `feature-enemy/enemy-stats` ``), hoặc tới một từ (`` `glossary#wave` ``), thì phải khai. Nhắc node vòng 2 (`` `feature-run` ``) thì không bắt.
  - Tổ tiên, con cháu và chính nó không khai (based-on đã lo); khai thì là lỗi cấu trúc.
  - Thiếu ref, hoặc node đích đã đổi hash, thì node phải làm lại. `gd_status.py` in sẵn dòng `refs:` đúng hash hiện tại; chỉ chép sau khi đã đọc node đích.
- Tiêu đề tiếng Việt, rồi tóm tắt 1 đến 3 câu (trang hiện đoạn này trên bản đồ).
- `## Ý cốt lõi`: mỗi ý một dòng `### <id> | AI <0-10> | <tiêu đề>`, dưới đó là chi tiết (không bắt buộc).
  - **Tiêu đề** là chính ý đó nói gọn trong một câu ngắn, đọc riêng vẫn hiểu và chấm điểm được. **Một ý một quyết định**: hai quyết định thì tách hai ý, không gộp dài (`gd_status.py` nhắc "Ý DÀI" khi tiêu đề quá 120 kí tự hoặc chi tiết quá 500). Trang quản lý hiện tiêu đề ở cả danh sách "Ý cốt lõi" lẫn panel duyệt; người chấm theo tiêu đề.
  - **Chi tiết** chỉ hiện khi người bấm vào ý: lí do, ví dụ, phương án đã loại (con số chỉ từ vòng 4). Không có gì thêm thì bỏ trống.
  - `id` tiếng Anh, không trùng trong node, giữ nguyên qua các lần sửa. Người không thấy `id` trừ khi bấm mở ý.
  - Ý viết theo dạng cũ (không có tiêu đề) vẫn đọc được nhưng `gd_status.py` sẽ nhắc "THIẾU TIÊU ĐỀ": lần tới sửa node đó thì thêm tiêu đề. Thêm tiêu đề làm đổi hash data.
- Danh sách ý cốt lõi chỉ nằm ở `data.md`. Không chép lại thành mục "Cốt lõi" trong `CLAUDE.md`: hai danh sách sẽ lệch nhau và người duyệt sẽ thấy hai thứ khác nhau.
- Không thêm mục `##` nào khác vào `data.md`: giải thích, ví dụ, trường hợp biên, bảng số, mục "Chưa rõ" để ở `CLAUDE.md`.
- Phần đầu còn hai dòng không bắt buộc, không tính vào hash: `screen: landscape|portrait` (chỉ ở node gốc, xem skill `gd-round-1`) và `frame: none` (node vòng 4 không có bố cục, xem skill `gd-round-4`). Ba dòng về ý của GPT, `by:`, `gpt-dropped:` và `node-by:`, cũng không tính vào hash; xem skill `gd-integrate-advice`.
- **Hash data** tính từ tiêu đề node, tóm tắt, id, tiêu đề, chi tiết của các ý, và nội dung các file `.frame-concept/*.frame`. Đổi một trong các thứ đó là đổi hash: mọi node con thành "cần làm lại". Đổi điểm AI, `based-on` hay `CLAUDE.md` thì không. Vì vậy chỉ sửa `data.md` khi quyết định thật sự đổi; sửa câu chữ cho hay thì đừng.
- **Trang hiện mọi thay đổi so với `live`** (bản chốt gần nhất), tới khi node được chốt lại:
  - Thẻ node mang nhãn `Đổi +mới ✎sửa −bỏ` (`Mới` khi `live` chưa có node). Ý có nhãn `Mới` hoặc `Đã sửa`, chữ cũ gạch, chữ mới tô; ý bị xoá nằm ở mục "Ý đã bỏ so với live"; ý đổi mà người chưa chấm được đánh dấu.
  - Mọi file của node (`CLAUDE.md`, `code.json`, frame, ảnh, style, phần đầu `data.md`) và điểm AI của từng ý đều có bản live đặt cạnh để so. Vì vậy chỉnh `CLAUDE.md` hay đổi điểm AI cũng là thứ người phải đọc lại.
  - Hệ quả: sửa một ý thì giữ `id` (đổi `id` là một ý bỏ và một ý mới, mất so sánh và điểm cũ); mỗi chữ sửa trong `data.md` là một ý "Đã sửa" người phải đọc lại, kể cả trong Full Auto, nên đừng sửa câu chữ kèm khi sửa ý khác; báo cáo cuối lượt nêu đúng các ý đã thêm, sửa, bỏ (`<node>#<id>`).
### `CLAUDE.md`
Dòng đầu `# <Tên>`, ngay dưới là dòng trỏ tới `data.md` (ví dụ `Dữ liệu cốt lõi: [data.md](data.md)`). Phần còn lại là diễn giải của vòng đó, chia mục bằng `##`; các mục của từng vòng ghi trong file luật của vòng.
Mode Full Auto thêm mục `## Đã tự quyết`: quyết gì, vì sao, phương án đã loại.
