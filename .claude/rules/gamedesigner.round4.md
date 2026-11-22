---
paths:
  - "Management/game-design/ai/idea/*/*/*/*"
  - "Management/game-design/live/*/*/*/*"
---
# GD vòng 4: Quyết định chi tiết
Luật chung: `.claude/rules/gamedesigner.md`. File này chỉ thêm phần riêng của vòng 4 (`ai/idea/<feature>/<detail>/<tech>/`).

## Mục tiêu
Sau vòng 4, phiên code làm được game chơi được ở **mức IDEA** mà không phải hỏi lại thiết kế.

## Viết gì
- Nên làm gì.
- Làm với thông số nào.
- Thông số theo giai đoạn phát triển (xem mục riêng bên dưới).
- Trường hợp biên và ngoại lệ của các quyết định ở vòng 3 (ví dụ: hero HP = 0 nhưng còn thuốc hồi sinh).

## Cụ thể tới mức nào
- **Popup**: có bao nhiêu cái; mỗi cái có bao nhiêu nút và UI component nào bắt buộc phải có để chơi.
- **HUD**: hiện cái gì, lúc nào, mỗi icon đại diện cho gì.
- **Unit**:
  - loại nào;
  - 3D hay 2D, bounding box bao nhiêu;
  - bao nhiêu con trong từng màn;
  - chỉ số từng unit;
  - skill và chỉ số từng skill;
  - danh sách effect, lúc xuất hiện và lúc biến mất;
  - hành vi di chuyển.
- **Balance**: dự đoán và đưa ra bộ chỉ số cùng cách tính.

Con số ghi kèm đơn vị và lí do; con số phỏng đoán ghi là "dự kiến".

## Không cụ thể tới mức này (có chuyên gia riêng)
- Giao diện tương tác: màu, vị trí, kích thước. Sai: "Nút có màu xanh".
- Hình ảnh: Sai: "Unit sói có mắt đỏ khi dùng skill", "effect màu đỏ hình lưỡi đao".
- Cách viết code: lớp, hàm, kiến trúc là việc của phiên code và @CodeLead.

## Không mơ hồ
Mỗi ý phải làm theo được và kiểm được. Sai: "Unit sói hay vẫy đuôi" (bao lâu một lần, lúc nào, để làm gì?).

## Thông số theo giai đoạn
Vòng 4 là vòng duy nhất được ghi dữ liệu theo giai đoạn, và chỉ ở một chỗ:
- `CLAUDE.md` của node có mục `## Thông số theo giai đoạn`: bảng mỗi hàng một thông số, ba cột IDEA, PROTOTYPE, FULL.
- Ý cốt lõi trong `data.md` nêu giá trị của mức IDEA và không nhắc tới giai đoạn nào.

## Mức IDEA là gì
- Các tính năng đủ để thấy được gameplay.
- Unit 2D: hiển thị được hình, sprite có nghĩa (ảnh gen bằng AI); anim đơn giản bằng scale, move, rotate hoặc đổi màu.
- Unit 3D: model low poly (làm qua blender mcp); anim cơ bản bằng scale, move, rotate, đổi màu.

## Mở khoá code
Guard chỉ cho phiên feature sửa code khi nhánh của nó có ít nhất một node vòng 4 đã chốt trong `live`. Vì vậy vòng 4 phải đủ rõ để code theo; đừng viết sơ sài chỉ để mở khoá.

## `CLAUDE.md` của node
Các mục: Làm gì, Thông số, Thông số theo giai đoạn, Trường hợp biên, Chưa rõ.
