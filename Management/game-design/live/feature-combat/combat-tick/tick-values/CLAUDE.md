# Thông số nhịp giao chiến

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Mỗi bước cố định 0.02 giây của trận: chạy một nhịp theo thứ tự ở `tick-order`.
- Nhận đòn trúng từ hero, quái, thành (đòn tự bắn, Sấm sét), lọc theo trạng thái đầu nhịp, cộng theo bên trúng.
- Chuyển tổng cho feature sở hữu; chờ mọi bên trừ máu và báo xong; báo nhịp xong cho `feature-wave` và `feature-run`.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Bước cập nhật | 0.02 (50 bước/giây) | giây | Nhịp đòn nhanh nhất là 0.7 giây (Kiếm khách, `feature-hero/hero-list/hero-roster-values`); 0.02 giây đủ mịn |
| Số bước tối đa trong một khung hình | 5 | bước | Khung hình chậm (0.1 giây) vẫn theo kịp; chậm hơn thì trận chạy chậm lại, không nhảy cóc |
| Làm tròn sát thương | không | | Hệ số đợt cho số lẻ |
| Đòn Sấm sét | một đòn mỗi quái trong vùng | đòn | `feature-castle/castle-skills/skill-values` |
| Tiền khi hạ quái | một lần mỗi quái | lần | Dù nhiều bên cùng hạ |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Giao chiến | Không có: mỗi bên trừ máu ngay khi trúng | Có, bước 0.02 giây | Như PROTOTYPE |
| Bước cập nhật cố định | Không bắt buộc | 0.02 giây | Như PROTOTYPE |

## Trường hợp biên
- **Hero và quái kết liễu nhau cùng nhịp:** cả hai hết máu; hero gục, quái bị hạ, quái vẫn cho tiền.
- **Thành và con quái cuối cùng hết máu cùng nhịp:** thành bị phá xét trước; luồng hồi sinh giữ tin dọn sạch.
- **Đòn vào quái đã ra ngoài màn (±16u):** vẫn trúng nếu bên ra đòn đã chọn nó; luật chọn là của bên ra đòn.
- **Cổ vũ hồi máu cùng nhịp hero nhận đòn:** Cổ vũ không qua giao chiến; hồi máu áp ở bước "đi và nhắm" trước khi trừ máu.

## Đã tự quyết
- **Bước cố định 0.02 giây.** Kết quả lặp lại được, cần cho "hai bên cùng ra đòn thì cùng trúng". Đã loại: theo khung hình (khác nhau giữa máy).
- **Tối đa 5 bước mỗi khung.** Tránh máy yếu rơi vào vòng chậm dần.
- **Cổ vũ áp trước khi trừ máu.** Người chơi bấm cứu đúng lúc thì hero được cứu.

## Chưa rõ
- Giao chiến là dịch vụ riêng hay một bước trong vòng lặp trận: phiên code cùng @CodeLead.
