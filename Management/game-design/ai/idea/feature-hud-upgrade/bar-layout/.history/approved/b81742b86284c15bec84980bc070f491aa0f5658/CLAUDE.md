# Bố cục thanh nâng cấp

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT mục nâng cấp (`feature-upgrade/upgrade-list`) có MỘT nút trên thanh.
- MỘT hero trong đội có MỘT nút hero; thành có BA nút thành.
- Thanh chiếm mép dưới; phần HUD còn lại (`feature-hud`) dùng các chỗ khác ở mép màn hình.
- Nội dung và trạng thái từng nút: `feature-hud-upgrade/button-state`.

## Danh sách
- Nhóm hero: một nút cho mỗi hero, theo thứ tự đội.
- Nhóm thành: máu, sức đánh, kĩ năng.

## Luật cơ bản
- Thanh hiện từ lúc vào trận tới lúc trận kết thúc.
- Số nút đúng bằng số mục nâng cấp của lượt; không đổi trong lượt.
- Thanh không che dải chiến trường.
- Hai nhóm tách nhau thấy rõ.

## Đã tự quyết
- **Mép dưới.** Trả lời câu "Chưa rõ" của vòng 2. Cầm ngang, ngón cái ở dưới; dải chiến trường nằm giữa màn hình. Đã loại: mép trên, hai mép bên.
- **Nhóm hero theo thứ tự đội.** Người chơi đã quen thứ tự lúc chọn đội. Đã loại: xếp theo giá.
- **Không tự quyết chỗ của HUD khác.** Chỉ ghi thanh nâng cấp giữ mép dưới; `feature-hud` tự xếp phần của nó.

## Chưa rõ
- Không có điểm chặn. `feature-hud` cần biết mép dưới đã có thanh nâng cấp.
