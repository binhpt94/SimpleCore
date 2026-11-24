# Quảng cáo

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Một cơ hội cứu lượt khi thành sắp mất: tự chọn xem một quảng cáo để chơi tiếp.
- Không bị làm phiền bởi quảng cáo tự bật.

## Liên quan
- `feature-popup-revive`: bên đầu tiên nhờ hiện quảng cáo (hồi sinh thành).
- `feature-run`: dừng trận trong lúc chờ kết quả.

## Đã tự quyết
- **Tạo feature theo yêu cầu của người (2026-11-24): "xem quảng cáo nhận hồi sinh thành".** Quảng cáo cắt ngang nhiều feature nên tách riêng (luật vòng 2), không gộp vào popup hồi sinh.
- **Chỉ quảng cáo có thưởng.** Hợp người chơi phổ thông, lượt ngắn; quảng cáo tự bật làm đứt nhịp trận.
- **Là feature của game, không phải dịch vụ hạ tầng.** Ba dịch vụ hạ tầng đã cố định theo luật vòng 2; `code.json` là `Ads`.

## Chưa rõ
- Nhà cung cấp quảng cáo và cách xử lí khi không có mạng ở mức kĩ thuật: phiên code cùng @CodeLead.
- Nhiều ý (đã nhận ở lần hỏi `r2-1`, 11 góp ý) ghi việc cho popup hồi sinh, lượt chơi và popup thua; khi viết vòng 3 cần chuyển đúng phần sang node đó.

## Góp ý của GPT đã đưa vào
- Lần hỏi `r2-1` (2026-11-24): 9 thêm, 2 làm rõ (`ads-result`, `ads-game-waits`), AI 6-8. `ads-game-waits` đổi nghĩa từ "game đứng yên khi quảng cáo chạy" thành "lượt chơi giữ trận đứng yên suốt luồng hồi sinh", rộng hơn và đúng chỗ hơn.
