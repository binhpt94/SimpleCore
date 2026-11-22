# Quái

Dữ liệu cốt lõi (tóm tắt và các ý có điểm): [data.md](data.md)

## Người chơi được gì
- Áp lực nhìn thấy được: quái càng đông, càng gần thành thì càng nguy.
- Phần thưởng mỗi lần hạ quái, tiêu được ngay vì nâng cấp mua trong trận.

## Cốt lõi
- Quái có chỉ số và tiền rơi.
- Đi thẳng vào thành, bị hero chắn thì đánh hero, tới thành thì đánh thành.
- Bị hạ bởi bất kì nguồn nào cũng rơi tiền.
- Vài loại quái, mở dần theo đợt (`feature-wave`).

## Liên quan
- `feature-wave`: quyết định quái nào ra, bao nhiêu, mạnh cỡ nào.
- `feature-hero`: đánh nhau với hero.
- `feature-castle`: đánh thành; bị thành bắn và trúng kĩ năng thành.
- `feature-wallet`: tiền rơi khi bị hạ.

## Đã tự quyết
- **Có vài loại quái thay vì một.** Để các đợt khác nhau và hero nào cũng có chỗ mạnh. Đã loại: một loại chỉ tăng chỉ số.
- **Chưa có quái bắn xa hay trùm.** Giữ phạm vi bản đầu. Đã loại: trùm mỗi N đợt.
- **Quái ưu tiên đánh hero chắn đường trước thành.** Cho hero cận chiến vai trò chắn. Đã loại: quái bỏ qua hero, chỉ nhắm thành.
- **Quái không đánh lại thành từ xa, chỉ khi đã tới thành.** Đòn tự bắn của thành nhờ vậy là lợi thế rõ ràng.

## Chưa rõ
- Không có điểm chặn.
