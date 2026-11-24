# Giao chiến

Dữ liệu cốt lõi: [data.md](data.md)

## Người chơi được gì
- Không thấy trực tiếp; trận đánh công bằng và lặp lại được: hai bên cùng ra đòn thì cùng trúng, không có kết quả "may rủi" do thứ tự xử lí.

## Liên quan
- `feature-hero`, `feature-enemy`, `feature-castle`: bên ra đòn và bên trúng; mỗi bên tự chọn mục tiêu, tự tính sát thương, tự trừ máu.
- `feature-wave`: nhận tin quái bị hạ sau khi một lúc phân xử xong.
- `feature-run`: nhận tin thành bị phá sau khi một lúc phân xử xong.

## Đã tự quyết
- **Tạo feature theo đề xuất của GPT (2026-11-24, `advice/_general/r2-all-2.md`, góp ý `phan-xu-sat-thuong`; cùng hướng với `tach-phan-xu-giao-chien` ở lần hỏi r2-3 của `feature-hero`).** Khai `node-by`.
- **Điểm AI 5 cho ý chính.** Lợi: một luật phân xử chung giữ kết quả nhất quán khi nhiều đòn trùng lúc. Bất lợi: thêm một nơi mà hero, quái và thành cùng phải đi qua, làm các feature dính nhau hơn trong một project ghép từ feature độc lập; ở game nhịp chậm như thế này, đòn trùng đúng lúc hiếm. Giữ để vòng 3 xét cách nhẹ nhất (một luật chung hay một dịch vụ).
- Không giữ máu hay chỉ số ở đây: giữ đúng luật "máu chỉ do feature sở hữu cập nhật" (ý `owner-applies-hp` ở `feature-hero`).
- `code.json`: `Combat`, theo luật đặt tên vòng 2.

- **Đưa góp ý GPT lần hỏi `r2-1` (2026-11-24, 11 góp ý).** GPT chọn có code riêng và mọi đòn đều đi qua (`combat-own-code`, `combat-every-hit`), AI 5: chạy đúng nhưng mọi đòn của ba feature phải qua một nơi, kể cả nhịp chỉ có một đòn. Ba góp ý làm rõ viết lại chính ý cũ (`combat-same-moment`, `combat-sides-own-hp`, `combat-report-after`). Chia việc với bên ra đòn và bên trúng (AI 7) khớp ý đã có ở `feature-hero`.

## Chưa rõ
- Cách nhẹ nhất cho code riêng (dịch vụ chung, hay một bước xử lí trong vòng lặp trận): vòng 3.
