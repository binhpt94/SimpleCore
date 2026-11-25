# Kiểm tra dọn sạch sau bước huỷ

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ một bộ đếm cho mỗi feature tham gia: số view đang có và số việc đang chạy (hẹn giờ, việc chờ, việc lặp mỗi khung hình). Chỉ bật ở bản dev.
- Sau bước huỷ của một scene: đọc bộ đếm của mọi feature tham gia; phải bằng 0. Lớp che của `feature-scene-flow` không tính.
- Còn sót: ghi lỗi nêu tên feature và số còn lại.
- Cho feature khác đọc số đếm bất kì lúc nào trong scene để tự kiểm.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| View còn lại sau bước huỷ | 0 | view | `nothing-survives` của node cha |
| Việc đang chạy còn lại sau bước huỷ | 0 | việc | `nothing-survives` của node cha |
| Hình không tính | lớp che của `feature-scene-flow` | | Chỉ sống trong lúc chuyển |
| Bộ đếm có ở bản phát hành | không | | Chỉ để kiểm ở bản dev |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Kiểm sau bước huỷ | Không bắt buộc (chỉ một lần chuyển, Loading → Game) | Đếm view và việc đang chạy | Như PROTOTYPE |
| Bộ đếm theo feature tham gia | Không bắt buộc | Có | Có |

## Trường hợp biên
- **Feature lỗi ở bước huỷ**: vẫn huỷ tiếp các feature còn lại (luật xử lí lỗi ở nhánh ba bước của dịch vụ); số đếm của feature lỗi vẫn được đọc và ghi lỗi nếu còn sót.
- **Lớp che của chuyển scene còn hiện lúc đếm**: không tính.
- **Feature không có view hay việc nào**: số đếm 0, không ghi gì.

## Đã tự quyết
- **Kiểm bằng số đếm, không bằng mắt.** Đếm view và việc đang chạy thì kiểm được. Đã loại: "chơi lại vẫn mượt" (không kiểm được).
- **Thu hẹp node về đúng việc của dịch vụ (2026-11-25, người chọn hướng A).** Bản trước có cả kiểm chơi lại nhiều lượt, popup sót ở đầu lượt, chỉ lưu kỉ lục và các trường hợp biên của lượt. Đó là luật của `feature-run` (đặt lại lượt trong scene, không qua vòng đời scene), dịch vụ popup và các feature trận. Các ý `replay-no-growth`, `popup-cleared-on-reset`, `only-record-saved` và các trường hợp biên đó chuyển sang node kiểm tra chơi lại mới dưới nhánh bắt đầu lượt của `feature-run`. Đã loại: giữ ở đây (sổ yêu cầu giao cho SessionScene việc nó không có code).
- **Thêm bộ đếm theo feature và lỗi nêu tên.** Làm rõ "cách đếm" mà bản trước chỉ ghi ở mục Làm gì; node kiểm tra chơi lại của `feature-run` dùng lại số đếm này.

## Chưa rõ
- Không có điểm chặn.
