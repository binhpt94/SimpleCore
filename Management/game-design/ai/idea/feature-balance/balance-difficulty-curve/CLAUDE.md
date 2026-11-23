# Đường độ khó

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Sức quái của MỘT đợt lấy từ `feature-wave/difficulty-scaling` (số quái, nhịp thả, máu, sức đánh, loại đã mở) nhân với chỉ số gốc của `feature-enemy`.
- Sức đội tại MỘT đợt gồm sức các hero trong đội (`feature-hero`) và sức thành (`feature-castle`), sau các nâng cấp (`feature-upgrade`) mua được bằng tiền kiếm tới đợt đó.
- Tiền kiếm tới một đợt là của `feature-balance/balance-economy`; node này dùng kết quả đó, không quyết thu và giá.
- Máu thành không tự hồi, nên máu mất ở đợt trước cộng vào áp lực đợt sau.

## Định nghĩa
- `glossary#enemy-power`
- `glossary#team-power` (tính trên năm chỉ số chung)
- `glossary#break-wave`
- `glossary#pressure`

## Danh sách
Thứ người chơi kéo đợt gãy ra muộn hơn:
- Chọn đội hợp với các loại quái (`feature-team-select`).
- Chọn nâng cấp nào, lúc nào (`feature-upgrade`).
- Canh lúc dùng kĩ năng thành (`feature-castle`).

## Luật cơ bản
- Áp lực ở đợt sau không nhỏ hơn đợt trước. Ngoại lệ có chủ đích: đợt có Trùm (theo chu kì) khó vọt hơn đợt trước, rồi đợt sau dịu lại.
- Ở các đợt đầu áp lực thấp; sau đó tăng đều.
- Chơi khéo thì đợt gãy muộn hơn chơi vụng; khoảng cách đó đủ lớn để người chơi thấy.
- Không có đội và cách mua nào làm áp lực ngừng tăng.

## Đã tự quyết
- **Đo bằng áp lực (sức quái so với sức đội).** Gom quyết định của nhiều feature về một thước đo. Đã loại: chỉnh từng feature riêng rẽ.
- **Không có trần độ khó.** Vòng 1: chơi vô tận, lượt nào cũng kết thúc.
- **Đội vào trận phải trụ được các đợt đầu mà không cần mua nhiều.** Hợp người chơi phổ thông.
- **Đợt Trùm là ngoại lệ có chủ đích của `curve-smooth` (2026-11-23, quyết định đã chốt).** Viết lại ý `curve-smooth` và luật "áp lực đợt sau không nhỏ hơn đợt trước": đợt có Trùm khó vọt hơn rồi đợt sau dịu lại. Lí do: Trùm có trong `feature-enemy/enemy-list` và ra theo chu kì ở `feature-wave`; bỏ Trùm thì mất đỉnh căng thẳng. Đã loại: bỏ Trùm theo chu kì; Trùm chiếm nhiều chỗ để san phẳng đợt.

## Chưa rõ
- Đợt gãy của người chơi trung bình nằm ở khoảng nào: vòng 4.
