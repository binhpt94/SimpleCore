# Tiền khi hạ quái

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT quái bị hạ cho tiền MỘT lần.
- MỘT loại quái có MỘT mức tiền thưởng.
- Tiền từ mọi quái đổ vào MỘT ví (`feature-wallet`).
- Thưởng dọn sạch đợt: `feature-wave`. Mức tiền cụ thể: `feature-balance`.

## Định nghĩa
- **Tiền thưởng của quái**: số tiền người chơi nhận khi một quái loại đó bị hạ.
- **Bên hạ**: hero, đòn tự bắn của thành, hoặc kĩ năng thành; không ảnh hưởng tiền thưởng.

## Danh sách
So sánh tiền thưởng theo loại (`feature-enemy/enemy-list`):
- Trùm: nhiều nhất.
- Khổng lồ: nhiều hơn Lính.
- Lính: mức chuẩn.
- Sói, Cung thủ quái: không nhiều hơn Lính.

## Luật cơ bản
- Quái bị hạ: tiền thưởng của loại đó vào ví ngay.
- Ai hạ cũng cho cùng một mức tiền.
- Quái đánh thành hay đánh hero không làm mất tiền của người chơi.
- Tiền nhận được chỉ dùng trong lượt (`feature-wallet`).
- Thưởng-phạt: hạ được nhiều quái thì nhiều tiền để nâng cấp; để quái sống lâu thì không có tiền mà thành còn mất máu.

## Đã tự quyết
- **Tiền theo loại, không theo đợt.** Đợt sau có nhiều quái hơn và loại trâu hơn nên tự nhiều tiền hơn; nếu cần tăng theo đợt thì `feature-wave` hay `feature-balance` quyết. Đã loại: tiền mỗi quái tăng theo số đợt ở node này.
- **Không có tiền rơi cần nhặt.** Giữ thao tác ít cho người chơi phổ thông.
- **Hạ quái không cho điểm.** Điểm là số đợt đã dọn theo `feature-run`.

## Chưa rõ
- Không có điểm chặn.
