# Thứ tự từ lúc thành bị phá tới lúc chơi tiếp hoặc thua

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- `feature-castle` báo thành bị phá; `feature-run` dừng trận, quyết có đề nghị hồi sinh không (lần đầu trong lượt), mở popup hồi sinh.
- Popup hồi sinh (`feature-popup-revive/revive-popup`) báo lựa chọn cuối cho `feature-run`.
- `feature-run` ra lệnh `feature-castle` hồi sinh, hoặc chốt thua và mở popup thua (`feature-popup-lose`).
- `feature-wave` báo dọn sạch; `feature-run` giữ tin đó chờ khi đang có đề nghị hồi sinh.

## Định nghĩa
- Không dùng thuật ngữ riêng.

## Danh sách
| Bước | Ai làm |
|---|---|
| Thành bị phá (lần đầu) | `feature-castle` báo |
| Trận dừng, đứng yên | `feature-run` |
| Mở popup hồi sinh | `feature-run` nhờ, popup hồi sinh điền nội dung |
| Người chơi chọn, có thể xem quảng cáo | popup hồi sinh, `feature-ads` |
| Popup đóng, báo lựa chọn | popup hồi sinh |
| Hồi sinh: thành có lại máu, trận chạy tiếp | `feature-run` ra lệnh, `feature-castle` làm |
| Không hồi sinh: chốt điểm, popup thua | `feature-run` |

## Luật cơ bản
- Một lượt một đề nghị.
- Trận đứng yên suốt từ lúc dừng tới lúc chơi tiếp.
- Popup hồi sinh đứng trước tạm dừng.
- Tin dọn sạch cùng lúc thành bị phá: chờ kết quả hồi sinh.

## Đã tự quyết
- **Đợt dọn cùng lúc được tính khi hồi sinh thành công (theo ý `same-tick-clear` của vòng 2).** Hồi sinh là chơi tiếp đúng thế trận, đợt đó đã dọn thật. Lượt chốt thua thì vẫn "thua trước". Đã sửa ghi chú trường hợp biên ở nhánh điểm của `feature-run` cho khớp (2026-11-25).
- **Bỏ yêu cầu tạm dừng cùng lúc, không để chờ.** Popup hồi sinh đã dừng trận; mở thêm popup tạm dừng sau đó chỉ làm người chơi bấm thêm một lần.

## Chưa rõ
- Không có điểm chặn.
