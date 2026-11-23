# Kĩ năng thành

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- MỘT thành có BA kĩ năng, mỗi kĩ năng MỘT hồi chiêu riêng.
- Một lần bấm dùng MỘT kĩ năng.
- Sấm sét và Băng giá tác động NHIỀU quái một lúc; Cổ vũ tác động NHIỀU hero một lúc.
- Một nâng cấp "kĩ năng" của thành tác động CẢ BA kĩ năng (`feature-castle/castle-upgrade-effects`).

## Định nghĩa
- `glossary#castle-skill`
- `glossary#cooldown`
- `glossary#castle-zone` (Sấm sét chỉ trúng quái trong vùng này)

## Danh sách
| Kĩ năng | Làm gì | Trúng ai | Hồi chiêu so với nhau |
|---|---|---|---|
| Sấm sét | Trừ máu | Mọi quái trong vùng quanh thành, cả hai bên | Vừa |
| Băng giá | Làm chậm đi và đánh | Mọi quái trên chiến trường | Dài nhất |
| Cổ vũ | Hồi một phần máu | Mọi hero đang trên chiến trường | Ngắn hơn Băng giá |

## Luật cơ bản
- Vào trận: cả ba kĩ năng sẵn sàng.
- Bấm kĩ năng sẵn sàng: dùng ngay, bắt đầu hồi chiêu của riêng kĩ năng đó.
- Kĩ năng đang hồi: không bấm được.
- Ba kĩ năng hồi độc lập; dùng cái này không ảnh hưởng cái kia.
- Kĩ năng không tốn tiền.
- Lúc tạm dừng: không bấm được, hồi chiêu không chạy.
- Quái bị Sấm sét hạ vẫn cho tiền (`feature-enemy/enemy-reward`).
- Nâng cấp kĩ năng làm kĩ năng mạnh hơn, không làm hồi chiêu ngắn lại (`feature-castle/castle-upgrade-effects`).
- Thưởng-phạt: dùng đúng lúc quái dồn đông thì dọn được nhiều; dùng phí thì lúc nguy phải chờ hồi.

## Đã tự quyết
- **Ba kĩ năng: Sấm sét, Băng giá, Cổ vũ.** Ba vai rõ ràng (đánh, kìm, cứu), người chơi phổ thông dễ nhớ. Đã loại: gọi thêm hero (trái quyết định người đã loại), hồi máu thành (phá áp lực máu không hồi), một kĩ năng duy nhất.
- **Mọi kĩ năng có ngay từ đầu trận, không mở khoá.** Game không có tiến trình lâu dài; nâng cấp kĩ năng chỉ làm mạnh hơn. Đã loại: mở kĩ năng bằng nâng cấp.
- **Sấm sét đánh vùng quanh thành ở cả hai bên.** Không chọn vị trí mà vẫn có ý nghĩa: dùng khi quái dồn sát thành. Đã loại: đánh toàn màn hình (quá mạnh, không cần canh lúc).
- **Kĩ năng miễn phí, chỉ giới hạn bằng hồi chiêu.** Giữ tiền cho nâng cấp; không bắt người chơi so hai thứ cùng lúc. Đã loại: tốn tiền.
- **Nút kĩ năng hiển thị ở `feature-hud`.** Node này chỉ lo kĩ năng làm gì.

## Chưa rõ
- Không có điểm chặn.
