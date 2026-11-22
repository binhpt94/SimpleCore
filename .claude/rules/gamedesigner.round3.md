---
paths:
  - "Management/game-design/ai/idea/*/*/*"
  - "Management/game-design/live/*/*/*"
---
# GD vòng 3: Quan hệ và định nghĩa
Luật chung: `.claude/rules/gamedesigner.md`. File này chỉ thêm phần riêng của vòng 3 (`ai/idea/<feature>/<detail>/`).

## Viết gì
Vòng 3 là nơi quyết định các quan hệ và định nghĩa, hiện thực hoá dần các ý của vòng 2. Ví dụ:
- Tương quan số lượng: MỘT hero đánh MỘT hay NHIỀU quái.
- Định nghĩa: HP = máu (heal point).
- Điều kiện thua: HP = 0.
- Danh sách: vật phẩm gồm Bình HP lớn, Bình HP nhỏ.
- Thưởng-phạt: chết mất MỘT hoặc NHIỀU vật phẩm.

## Chưa viết (để vòng 4)
- **Con số chính xác và kĩ thuật.** Sai: "Bình HP lớn = 500 HP". Đúng: "Bình HP lớn hồi nhiều hơn Bình HP nhỏ".
- **Trường hợp biên, ngoại lệ.** Vòng 3 chỉ gồm các quyết định tất yếu cơ bản. Sai: "Hero HP = 0 thì chết, nhưng sẽ hồi sinh khi dùng thuốc hồi sinh". Đúng: "Hero HP = 0 thì chết".

## `CLAUDE.md` của node
Các mục: Quan hệ, Định nghĩa, Danh sách, Luật cơ bản, Chưa rõ.
