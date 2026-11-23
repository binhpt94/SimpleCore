# Bộ mục nâng cấp của lượt

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Khi vào trận, nhận đội (3 hero theo thứ tự chỗ 1, 2, 3) từ `feature-run`.
- Lập 6 mục theo thứ tự:
  1. `hero-1`: cấp hero của hero ở chỗ 1.
  2. `hero-2`: cấp hero của hero ở chỗ 2.
  3. `hero-3`: cấp hero của hero ở chỗ 3.
  4. `castle-health`: máu thành.
  5. `castle-attack`: sức đánh thành (đòn tự bắn).
  6. `castle-skill`: kĩ năng thành (cả ba kĩ năng).
- Mỗi mục giữ: mã mục (ở trên), loại (hero hoặc thành), hero nó trỏ tới (với mục hero), cấp hiện tại.
- Đưa danh sách theo đúng thứ tự cho `feature-hud-upgrade` để lập nút.
- `feature-run` ra lệnh đặt lại đầu lượt: lập lại 6 mục, mọi mục về cấp 1.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số hero trong đội | 3 (dự kiến) | hero | Bộ số chung |
| Số mục hero | 3 | mục | Một mục mỗi hero |
| Số mục thành | 3 | mục | Máu, sức đánh, kĩ năng (vòng 3) |
| Tổng số mục | 6 | mục | 3 + 3 |
| Cấp ban đầu | 1 | cấp | Chưa mua lần nào |
| Cấp tối đa | không có | | Giá tăng tự hãm (vòng 3) |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Số mục hero | 3 | 3 | 3 |
| Số mục thành | 3 | 3 | 3 |
| Tên hiển thị của mục | mã mục hoặc tên hero | tên tiếng Việt | tên và mô tả ngắn đủ art |

## Trường hợp biên
- **Hai hero cùng loại trong đội** (nếu `feature-team-select` cho phép): hai mục riêng, mỗi mục trỏ đúng một hero theo chỗ.
- **Đội nhận được ít hơn 3 hero** (lỗi dữ liệu): báo lỗi ngay khi vào trận, nêu số hero nhận được; không lập danh sách thiếu.
- **Hero gục**: mục vẫn còn, vẫn mua được (`feature-upgrade/upgrade-effect`).
- **Lượt mới với đội khác**: danh sách lập lại theo đội mới, mọi cấp về 1.

## Đã tự quyết
- **Mục hero theo chỗ trong đội, không theo loại hero.** Đúng cả khi trùng loại; khớp thứ tự nút. Đã loại: nhận diện theo loại hero.
- **Thành sau hero trong thứ tự.** Khớp `feature-hud-upgrade/bar-layout` (nhóm hero, rồi nhóm thành). Đã loại: thành trước.
- **Thiếu hero là lỗi, không tự co danh sách.** Đội luôn đủ 3 khi chơi đúng; thiếu là dấu hiệu dữ liệu sai. Đã loại: âm thầm lập ít mục hơn.

## Chưa rõ
- `feature-team-select` có cho chọn hai hero cùng loại không: không chặn node này.
