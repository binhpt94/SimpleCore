# Đợt quái

Dữ liệu cốt lõi (tóm tắt và các ý có điểm): [data.md](data.md)

## Người chơi được gì
- Nhịp rõ ràng: đánh, thở một chút, đánh tiếp.
- Độ khó tăng đều, luôn biết mình đang ở đợt thứ mấy.
- Phần thưởng mỗi lần dọn sạch đợt.

## Cốt lõi
- Đợt đánh số từ 1, vô tận.
- Mỗi đợt một danh sách quái và nhịp thả, chia hai bên.
- Độ khó tăng theo công thức.
- Dọn sạch: thưởng tiền, tăng điểm, nghỉ ngắn, tự sang đợt sau.
- Thành hết máu: dừng thả, báo thua.

## Liên quan
- `feature-enemy`: các quái được thả.
- `feature-wallet`: thưởng dọn đợt.
- `feature-run`: số đợt đã dọn là điểm; nhận báo thua.
- `feature-upgrade`: không còn phụ thuộc; nâng cấp mua được mọi lúc.

## Đã tự quyết
- **Bỏ màn nghỉ dài để nâng cấp.** Vòng 1 đã đổi: nâng cấp ngay trong trận (người loại bản "chỉ nâng cấp giữa hai đợt"). Node con `feature-wave/break-phase` sẽ phải làm lại hoặc xoá khi tới vòng 3.
- **Vẫn có khoảng nghỉ ngắn tự đếm ngược.** Cho người chơi thở và thấy rõ ranh giới giữa hai đợt. Đã loại: đợt sau nối liền ngay (mất cảm giác "dọn sạch"), nghỉ tới khi bấm (không cần nữa vì nâng cấp mua được lúc đánh).
- **Có thưởng tiền khi dọn sạch đợt.** Thêm phần thưởng cho việc trụ được. Đã loại: chỉ tiền từ quái.
- **Độ khó theo công thức.** Vì chơi vô tận. Đã loại: bảng đợt viết tay.

## Chưa rõ
- Không có điểm chặn.
