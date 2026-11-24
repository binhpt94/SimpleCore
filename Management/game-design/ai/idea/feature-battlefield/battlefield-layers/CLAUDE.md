# Các lớp nền và thứ tự vẽ

Dữ liệu cốt lõi: [data.md](data.md)

## Quan hệ
- Một trận có MỘT nền; một nền có BỐN lớp.
- Một lớp decor có NHIỀU món decor, đặt ở hai bên thành.
- Mặt đất, mép thành, mép màn do `feature-castle/castle-core` cấp; nền chỉ vẽ theo.
- `feature-hud` và `feature-hud-upgrade` che lên phần trên và phần dưới của nền; nền vẫn vẽ phần bị che.
- Nền sinh ra khi vào màn trận và mất khi rời màn trận (theo `feature-run`), cùng vòng đời các thứ khác của trận.

## Định nghĩa
- `glossary#battlefield` (dải đất nơi hero, quái, thành đứng; lớp dải đất của nền là hình của dải này).

## Danh sách
| Lớp | Là gì | Nằm đâu |
|---|---|---|
| Trời | Một màu hoặc dải chuyển màu | Từ mặt đất lên mép trên khung |
| Cảnh xa | Đồi, núi, rừng | Đứng trên mặt đất, sau dải đất |
| Dải đất | Mặt đất và lớp đất | Từ mặt đất xuống mép dưới khung |
| Decor | Cỏ, đá, cờ, hàng rào | Dọc hai bên thành, trên mặt đất |

Thứ tự vẽ cả trận (sau ra trước): trời → cảnh xa → dải đất → decor → thành → quái và hero → hiệu ứng → HUD → popup.

## Luật cơ bản
- Nền không chặn đường đi, không chặn chạm, không đổi luật nào.
- Decor trên đường hero và quái đi là hoạ tiết phẳng; decor đứng (cờ, hàng rào) không đứng trên thân thành.
- Nền phủ kín khung ở mọi tỉ lệ màn hỗ trợ.
- Nền đứng yên suốt trận.

## Đã tự quyết
- **Thành vẽ dưới quái và hero.** Hero xuất phát và hồi sinh ở mép thành, quái đánh thành ở mép thành; vẽ thành trên sẽ che unit. Đã loại: thành trên unit.
- **Decor luôn dưới mọi unit.** Không có decor che phía trước (gần người chơi), để unit không bao giờ bị che. Đã loại: một lớp decor phía trước tạo chiều sâu (che hero, quái).
- **Nền không phản ứng với trận.** Theo `field-look-only`; muốn báo nguy thì là việc của HUD.

## Chưa rõ
- Số món decor, khoảng cách, cao độ trời và cảnh xa: vòng 4.
