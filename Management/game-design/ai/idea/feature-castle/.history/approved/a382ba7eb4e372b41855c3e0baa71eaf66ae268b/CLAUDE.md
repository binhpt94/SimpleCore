# Thành

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Một mục tiêu rõ ràng để giữ: nhìn thành là biết đang an toàn hay sắp nguy.
- Thao tác chủ động trong trận: bấm kĩ năng thành đúng lúc quái dồn đông.
- Thành cũng là một "chiến binh": tự đánh quái lọt qua hàng hero.

## Liên quan
- `feature-hero`: hero xuất phát và hồi sinh ở thành.
- `feature-enemy`: quái đánh thành; thành đánh quái.
- `feature-upgrade`: nâng cấp thành.
- `feature-run`: thành bị phá thì lượt chơi kết thúc.
- `feature-hud`: hiện tình trạng thành và các nút kĩ năng.
- `feature-balance`: số liệu của thành.

## Đã tự quyết
- **Thành tự đánh, không cần bấm.** Người chơi đã có kĩ năng để bấm; bắt bấm cả đòn thường là quá nhiều thao tác cho người chơi phổ thông. Đã loại: chạm vào quái để thành bắn.
- **Kĩ năng dùng ngay khi bấm, không cần chọn vị trí.** Hợp thao tác một chạm trên điện thoại. Đã loại: kéo thả để nhắm vùng.
- **Nút kĩ năng hiển thị ở `feature-hud`.** Theo luật vòng 2, HUD là feature riêng; feature này chỉ lo kĩ năng làm gì.
- **Bỏ ý `castle-no-regen` (máu không tự hồi).** Đó là luật, thuộc vòng 3 (`feature-castle/...`). Vẫn giữ hướng: máu mất ở đợt trước kéo áp lực sang đợt sau.
- **Không ghi số kĩ năng.** Bỏ chữ "dự kiến 2 cho bản đầu": con số để vòng 4, không ghi theo bản phát hành.
- **Viết lại cả 5 ý theo luật vòng 2 mới (2026-11-23).** Thêm tiêu đề, bỏ chi tiết mức định nghĩa (máu tối đa, máu về 0, quái gần nhất trong tầm). `castle-health` người đã chấm 9 ở bản cũ; ý mới giữ đúng nội dung đó nhưng viết ở mức vòng 2.

## Chưa rõ
- Các kĩ năng cụ thể (ví dụ đánh một vùng, làm chậm quái): quyết ở vòng 3.
