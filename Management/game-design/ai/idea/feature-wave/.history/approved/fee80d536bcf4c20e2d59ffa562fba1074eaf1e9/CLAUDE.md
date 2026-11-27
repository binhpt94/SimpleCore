# Đợt quái

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Nhịp rõ ràng: đánh, thở một chút, đánh tiếp.
- Độ khó tăng đều, luôn biết mình đang ở đợt thứ mấy.
- Phần thưởng mỗi lần dọn sạch đợt.

## Liên quan
- `feature-enemy`: các quái được thả.
- `feature-wallet`: thưởng dọn đợt.
- `feature-run`: số đợt đã dọn là điểm; lượt kết thúc thì dừng thả quái.
- `feature-hud`: hiện số đợt.
- `feature-balance`: độ khó tăng nhanh cỡ nào.

## Đã tự quyết
- **Bỏ ý `wave-number` và `wave-stop-on-lose`.** Hiện số đợt là việc của `feature-hud`; dừng khi thua là luật nối với `feature-run`, để vòng 3.
- **Vẫn có lúc thở ngắn giữa hai đợt, không phải màn nghỉ để nâng cấp.** Nâng cấp mua được mọi lúc (vòng 1).
- **Bỏ "dự kiến 5 giây" và nút vào sớm.** Con số để vòng 4; nút vào sớm là chi tiết, xét lại ở vòng 3.
- **Màn nghỉ để nâng cấp đã bỏ.** Node con cũ về màn nghỉ đã được thay bằng `feature-wave/wave-gap` (lúc thở ngắn).

## Chưa rõ
- Không có điểm chặn.
