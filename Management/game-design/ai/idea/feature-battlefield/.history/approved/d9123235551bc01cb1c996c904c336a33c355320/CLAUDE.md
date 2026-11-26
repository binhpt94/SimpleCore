# Nền chiến trường

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Một bãi chiến nhìn ra được: biết đâu là đất, đâu là thành, hai bên là nơi quái kéo tới.
- Màn trận có không khí, không phải hình trên nền trống.

## Liên quan
- `feature-castle`: thành đứng giữa dải đất; nhánh thân thành giữ các mốc không gian chung (mép thành, mép màn, mặt đất). Nền dùng lại các mốc đó, không giữ số riêng.
- `feature-hero`, `feature-enemy`: đứng và đi trên dải đất.
- `feature-hud`, `feature-hud-upgrade`: che lên trên nền ở mép trên và mép dưới.
- `feature-balance`: bảng số chuẩn giữ bề rộng dải đất thấy được.

## Đã tự quyết
- **Tạo feature theo yêu cầu của người (2026-11-25, qua @Manager): "Nền chiến trường", thư mục `Battlefield`, bắt buộc có ở IDEA.** Trước đây góp ý `chu-so-huu-ban-do` của GPT (feature giữ bố cục bản đồ) bị loại vì các mốc không gian đã có chỗ giữ ở nhánh thân thành. Feature này khác: chỉ là phông nền để nhìn; các mốc vẫn do nhánh thân thành giữ, nền `use` lại.
- `code.json`: `Battlefield`, theo yêu cầu của người (trùng tên suy ra từ key).
- **Đưa góp ý GPT lần hỏi `r2-1` (2026-11-25, 5 thêm, 1 làm rõ).** Viết lại `field-ground-line` theo `mep-dat-theo-moc`. Năm ý mới: thứ tự lớp, decor phẳng trên đường đi, phủ kín khung ở mọi tỉ lệ màn, chạm đi xuyên, chỉ hiện ở màn trận (AI 7-8). Không góp ý nào trái thứ đã chốt.
- **Đã loại:** đổi nền theo mốc đợt (đề xuất của người ghi là có thể ở FULL; trái "một bản đồ duy nhất" của vòng 1, nên giữ `field-one-map`, xét lại khi tới FULL).

## Chưa rõ
- Các lớp nền cụ thể (trời, cảnh xa, decor) và cách chúng nằm so với thành, quái, hero: vòng 3.
