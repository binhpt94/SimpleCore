# Nút tạm dừng trên HUD

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Hiện một nút biểu tượng hai vạch đứng suốt trận, ở chỗ ghi trong `feature-hud/hud-layout/layout-spec`.
- Chạm khi trận đang đánh: báo `feature-run/run-pause` một lần.
- Biết trạng thái trận (đang đánh, tạm dừng, đã kết thúc) từ `feature-run` để bỏ qua chạm khi không đang đánh.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số nút | 1 | nút | `pause-button-one` |
| Biểu tượng | hai vạch đứng | | Người chơi điện thoại đã quen |
| Số lần báo mỗi lần chạm | 1 | lần | |
| Khoảng cách tới nút kĩ năng và thanh nâng cấp | khác mép (nút ở mép trên, hai thứ kia ở mép dưới) | | `pause-button-apart` |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Có nút tạm dừng | Không | Không bắt buộc | Có |
| Biểu tượng | | Chữ hoặc hai vạch | Sprite hai vạch hoàn thiện |

## Trường hợp biên
- **Chạm hai lần rất nhanh:** lần đầu báo tạm dừng; lần sau rơi vào nền che popup hoặc bị bỏ qua vì trận đã tạm dừng.
- **Chạm trong 1 giây chờ trước popup thua:** lượt đã kết thúc, không báo.
- **Chạm cùng khung hình thành bị phá:** thành bị phá xử lí trước; chạm bị bỏ qua.

## Đã tự quyết
- **Không có chữ trên nút.** Biểu tượng đã đủ hiểu, tiết kiệm chỗ mép trên.
- **Nút vẫn hiện khi lượt kết thúc.** Không cần ẩn, chỉ bỏ qua chạm; popup thua che lên sau 1 giây.
- **`ui.md` (2026-11-25, quy trình mới):** một nút, mức chính trong node này (phần tử duy nhất); trong cả HUD nó là phụ (`feature-hud/hud-layout/layout-spec`). Hai trạng thái bật và vô tác dụng. Giữ hoặc kéo không có thao tác riêng.

## Chưa rõ
- Không có điểm chặn.
