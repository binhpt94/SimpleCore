# Màn nghỉ

Dữ liệu cốt lõi (tóm tắt và các ý có điểm): [data.md](data.md)

## Người chơi làm gì
- Mua nâng cấp (giao diện mua thuộc `feature-upgrade`).
- Bấm nút "Đánh tiếp" khi sẵn sàng.

## Người chơi thấy gì
- Bảng nâng cấp mở ra, phía sau là thành và hero đứng quanh thành.
- Số tiền hiện tại, số đợt sắp tới ("Đợt tiếp theo: N+1").
- Nếu đợt sắp tới có loại quái lần đầu xuất hiện: dòng báo trước, kèm hình loại quái đó (ví dụ "Quái nhanh sắp xuất hiện!").
- Mua nâng cấp xong thấy hiệu quả ngay: thêm hero khi tăng giới hạn, thanh máu thành dài ra khi tăng máu tối đa.

## Luật
- Màn nghỉ mở sau khi dọn sạch một đợt (`feature-wave/wave-clear`).
- Không có giới hạn thời gian; chỉ kết thúc khi người chơi bấm "Đánh tiếp".
- Trong màn nghỉ không có quái, không có trận đánh.
- Vào màn nghỉ, thành sinh bù ngay cho đủ giới hạn số hero (không chờ nhịp sinh).
- Mua nâng cấp tăng giới hạn hero trong màn nghỉ: thành sinh thêm ngay phần chênh.
- Nâng cấp chỉ số hero áp dụng ngay cho cả hero đang sống.
- Bấm "Đánh tiếp": bảng đóng lại, đợt N+1 bắt đầu với chữ "Đợt N+1" (`feature-wave/spawning`).
- Đợt 1 bắt đầu ngay khi vào lượt chơi, không có màn nghỉ trước đợt 1.

## Trường hợp biên
- Không đủ tiền mua gì: vẫn vào màn nghỉ như thường, nút "Đánh tiếp" vẫn bấm được.
- Người chơi thoát app hoặc tạm dừng trong màn nghỉ: quay lại vẫn đúng màn nghỉ, tiền và nâng cấp đã mua giữ nguyên.
- Bấm "Đánh tiếp" liên tiếp nhiều lần: chỉ bắt đầu một đợt.

## Con số dự kiến
- Không có con số riêng; tiền thưởng nằm ở `feature-wave/wave-clear`, giá nằm ở `feature-upgrade`.

## Đã tự quyết
- **Không đếm ngược.** Đã quyết ở `feature-wave`; người chơi phổ thông cần thời gian nghĩ.
- **Sinh bù đủ hero khi vào màn nghỉ.** Người chơi không bị thiệt vì hero chết sát cuối đợt, và thấy ngay giá trị của nâng cấp giới hạn hero. Đã loại: chờ nhịp sinh bình thường khi đợt sau bắt đầu.
- **Không hồi máu thành trong màn nghỉ.** Theo `feature-castle`; máu chỉ tăng qua nâng cấp máu tối đa.
- **Không có màn nghỉ trước đợt 1.** Bắt đầu nhanh, đúng tinh thần `feature-run`; tiền ban đầu dùng ở màn nghỉ đầu tiên. Đã loại: màn chuẩn bị trước đợt 1.
- **Báo trước loại quái mới.** Cho người chơi lí do chọn nâng cấp hợp với đợt sau. Đã loại: không báo trước (dễ bị bất ngờ không công bằng).

## Chưa rõ
- Không có điểm chặn.
