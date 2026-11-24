# Thời điểm từng bước của luồng hồi sinh

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
Dòng thời gian từ lúc thành bị phá lần đầu (t = 0):
| t (giây) | Việc | Ai làm |
|---|---|---|
| 0 | Thành báo bị phá; trận dừng; bật cờ "đã đề nghị" | `feature-castle`, `feature-run` |
| 0 – 0.5 | Thành thu nhỏ còn 0 | `feature-castle` |
| 1.0 | Mở `PopupRevive/Main` | `feature-run` nhờ dịch vụ popup |
| (người chơi chọn, có thể xem quảng cáo) | | popup hồi sinh, `feature-ads` |
| T | Popup đóng, báo lựa chọn | popup hồi sinh |
| T – T+0.5 | Hồi sinh: thành phóng lên đủ cỡ, máu 30% | `feature-castle` |
| T+0.5 | Trận chạy tiếp; tin dọn sạch đang giữ (nếu có) được xử lí | `feature-run` |
| T (không hồi sinh) | Chốt điểm, mở popup thua ngay | `feature-run` |

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Trễ trước popup hồi sinh | 1 (dự kiến) | giây | Bằng trễ trước popup thua ở `feature-run/run-flow/flow-rules` |
| Thành hiện lại | 0.5 | giây | Ngược lúc bị phá ở `feature-castle/castle-core/castle-body-values` |
| Máu khi hồi sinh | 30% máu tối đa lúc đó | | Như trên |
| Đếm ngược khi chạy tiếp | 0 | giây | Trận đứng đúng chỗ, người chơi đã thấy thế trận |
| Trễ trước popup thua sau khi từ chối | 0 | giây | Người chơi đã thấy thành bị phá từ trước |
| Lúc thở sau đợt dọn cùng lúc (khi hồi sinh) | 3 | giây | Như mọi lần dọn sạch |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Luồng hồi sinh | Không có: thành bị phá là thua (popup thua sau 1 giây) | Có đủ dòng thời gian trên | Như PROTOTYPE, thêm hiệu ứng hồi sinh riêng |
| Thành hiện lại | | Phóng từ 0 lên, 0.5 giây | Hiệu ứng dựng lại thành, âm thanh |

## Trường hợp biên
- **Hero đang chờ hồi sinh khi thành bị phá:** đồng hồ 8 giây của hero đứng yên suốt luồng, chạy tiếp khi trận chạy tiếp.
- **Băng giá đang làm chậm quái:** thời gian chậm còn lại đứng yên, chạy tiếp sau đó.
- **Thành bị phá lần hai ngay sau khi hồi sinh (trong vài khung hình):** vào thẳng popup thua sau 1 giây.
- **Rời game trong 1 giây trễ:** quay lại thì popup hồi sinh mở (trễ không tính lúc ở ngoài).
- **Tiền hạ quái tới cùng khung hình thành bị phá:** quái bị hạ trước khi trận dừng vẫn cho tiền; sau khi dừng thì không có đòn nào.

## Đã tự quyết
- **Không bất tử sau hồi sinh.** Theo nhánh thân thành; bấm Sấm sét là việc người chơi nên làm ngay.
- **Từ chối thì popup thua hiện ngay.** Trễ 1 giây đã có trước popup hồi sinh; thêm lần nữa chỉ làm chậm.
- **Đợt dọn cùng lúc được tính khi hồi sinh, kèm lúc thở 3 giây.** Theo ý `same-tick-clear` của node cha.

## Chưa rõ
- Không có điểm chặn.
