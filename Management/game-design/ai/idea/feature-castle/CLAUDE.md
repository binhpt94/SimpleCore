# Thành

Dữ liệu cốt lõi (tóm tắt và các ý có điểm): [data.md](data.md)

## Người chơi được gì
- Một mục tiêu rõ ràng để giữ: nhìn thanh máu thành là biết đang an toàn hay nguy.
- Thao tác chủ động duy nhất trong trận ngoài mua nâng cấp: bấm kĩ năng thành đúng lúc để cứu nguy.
- Thành cũng là một "chiến binh": tự bắn quái lọt qua hàng hero.

## Cốt lõi
- Thành đứng yên ở giữa, có máu; quái tới thành thì đánh trừ máu.
- Tự bắn quái gần nhất trong tầm, hai phía.
- Có kĩ năng với thời gian hồi chiêu, người chơi bấm nút để dùng.
- Đội hero xuất hiện ở thành khi vào trận; hero gục hồi sinh ở thành.
- Nâng cấp: máu, sức đánh, kĩ năng.

## Liên quan
- `feature-hero`: hero xuất phát và hồi sinh ở thành.
- `feature-enemy`: quái đánh thành, thành bắn quái.
- `feature-upgrade`: nâng ba thứ của thành.
- `feature-run`: thành hết máu thì lượt chơi kết thúc.

## Đã tự quyết
- **Máu không tự hồi.** Máu mất ở đợt trước kéo áp lực sang đợt sau. Đã loại: hồi đầy sau mỗi đợt.
- **Thành tự bắn, không cần bấm.** Người chơi đã có kĩ năng để bấm; bắt bấm cả đòn thường thì quá nhiều thao tác cho người chơi phổ thông. Đã loại: người chơi chạm vào quái để thành bắn.
- **Dự kiến 2 kĩ năng.** Vòng 1 ghi 1 đến 2; chọn 2 để có lựa chọn lúc nào dùng cái nào. Kĩ năng cụ thể (ví dụ mưa tên đánh một vùng, làm chậm quái) quyết ở vòng 3.
- **Kĩ năng dùng ngay khi bấm, không cần chọn vị trí.** Hợp thao tác một chạm trên điện thoại. Đã loại: kéo thả để nhắm vùng.
- **Nâng cấp kĩ năng gộp một mục.** Giữ số nút nâng cấp ít (xem `feature-upgrade`).
- **Bỏ việc thành tự sinh hero theo thời gian.** Người đã loại ở vòng 1; đội hero giờ cố định.

## Chưa rõ
- Không có điểm chặn.
