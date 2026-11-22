# Dọn sạch đợt

Dữ liệu cốt lõi (tóm tắt và các ý có điểm): [data.md](data.md)

## Người chơi làm gì
- Không làm gì: tự xảy ra khi con quái cuối của đợt chết.

## Người chơi thấy gì
- Chữ "Dọn sạch đợt N!" và số tiền thưởng "+X" bay về ô tiền.
- Hero đang sống chạy về đứng quanh thành.
- Sau một lúc thì màn nghỉ mở (`feature-wave/break-phase`).

## Luật
- Đợt kết thúc khi cả hai điều cùng đúng: đã thả hết quái của đợt, và không còn quái nào sống.
- Tiền rơi từ con quái cuối được cộng trước, rồi mới cộng thưởng dọn đợt.
- Thưởng dọn đợt tính theo số đợt (con số bên dưới), cộng vào `feature-wallet`.
- Số đợt đã dọn sạch tăng 1; đây là điểm của lượt chơi ở `feature-run`.
- Hero đang sống được giữ lại sang đợt sau, không bị xoá.
- Thưởng không phụ thuộc máu thành còn lại.

## Trường hợp biên
- Thành hết máu cùng lúc con quái cuối chết: tính là thua, đợt này không được tính là đã dọn, không có thưởng. Thua luôn được xét trước.
- Quái chết vì lí do bất kì (hero đánh, hay sau này có cách khác) đều tính là đã hạ.
- Tạm dừng đúng lúc dọn sạch: thông báo đứng yên, tiếp tục khi bỏ tạm dừng.

## Con số dự kiến
- Thưởng dọn đợt N = 20 + 5 × N tiền (đợt 1: 25, đợt 10: 70). Đủ mua ít nhất một nâng cấp rẻ ở vài đợt đầu; con số cuối cùng chỉnh cùng giá ở `feature-upgrade`.
- Thời gian hiện "Dọn sạch đợt N!" trước khi mở màn nghỉ: 1.5 giây.

## Đã tự quyết
- **Thưởng cố định theo số đợt, không theo máu thành còn lại.** Dễ hiểu; không phạt thêm người đang yếu. Đã loại: thưởng thêm khi thành không mất máu.
- **Thua xét trước dọn sạch.** Luật rõ ràng, không có tình huống nửa thắng nửa thua. Đã loại: ưu tiên dọn sạch.
- **Giữ hero còn sống.** Hero sống sót là phần thưởng cho việc chơi tốt. Đã loại: xoá hết hero mỗi đợt.

## Chưa rõ
- Không có điểm chặn.
