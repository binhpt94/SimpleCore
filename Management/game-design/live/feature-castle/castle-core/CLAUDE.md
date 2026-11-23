# Thân thành

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một trận có MỘT thành.
- Một thành có MỘT thanh máu.
- NHIỀU quái có thể cùng đánh thành một lúc; mỗi đòn của một quái trừ máu thành một lần.
- Một thành là điểm xuất phát và điểm hồi sinh của MỌI hero trong đội.

## Định nghĩa
- `glossary#reach-castle`

## Danh sách
- Trạng thái của thành: còn đứng, bị phá.

## Luật cơ bản
- Vào trận: thành có đầy máu.
- Quái đánh thành thì máu thành giảm theo sức đánh của quái.
- Máu thành không tự tăng. Chỉ nâng cấp máu (`feature-castle/castle-upgrade-effects`) làm máu tăng.
- Máu về 0: thành bị phá, báo `feature-run`, lượt thua.
- Hero và đòn của chính thành không trừ máu thành.
- Hero xuất phát từ thành khi vào trận và hồi sinh tại thành (`feature-hero/hero-respawn`).
- Phạt: để quái lọt tới thành là mất máu vĩnh viễn trong lượt.

## Đã tự quyết
- **Máu không tự hồi, cả khi dọn sạch đợt.** Giữ áp lực tăng dần qua các đợt như vòng 1. Đã loại: hồi theo thời gian, hồi đầy giữa hai đợt.
- **Thêm máu chỉ qua nâng cấp.** Biến nâng cấp máu thành lựa chọn có ý nghĩa: tiền vào máu thành hay vào hero. Đã loại: kĩ năng thành hồi máu thành (làm mất áp lực của `castle-no-regen`).
- **Thành chỉ là vị trí của hero.** Luật gục, chờ, hồi sinh để ở `feature-hero/hero-respawn`, tránh hai node cùng giữ một luật.
- **Gộp ý `castle-health` và `castle-hero-base` của vòng 2 vào node này.** Cả hai nói về chính công trình thành; tách riêng thì node hero-base quá mỏng.

## Chưa rõ
- Không có điểm chặn.
