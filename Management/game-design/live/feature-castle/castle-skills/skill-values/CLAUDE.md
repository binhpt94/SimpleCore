# Thông số ba kĩ năng thành

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ trạng thái sẵn sàng và thời gian hồi còn lại của từng kĩ năng; báo cho `feature-hud` để hiện nút.
- Nhận lệnh bấm từ `feature-hud`. Nếu kĩ năng sẵn sàng, thành còn đứng: áp hiệu lực ngay, bắt đầu hồi chiêu.
- Sấm sét: lấy mọi quái còn sống có khoảng cách tới mép thành gần nhất ≤ 6u (hai bên), trừ máu mỗi con bằng sát thương Sấm sét hiện tại.
- Băng giá: gắn trạng thái "chậm" lên mọi quái đang sống trong thời gian chậm hiện tại; quái bị chậm có tốc độ đi ×0.5 và giây mỗi đòn ×2 (`feature-enemy/enemy-behavior/enemy-move-values`).
- Cổ vũ: mọi hero đang đứng trên chiến trường (không gục) cộng máu = tỉ lệ hồi hiện tại × máu tối đa hiện tại của nó, chặn ở máu tối đa.
- Nhận mức tăng hiệu lực từ `feature-castle/castle-upgrade-effects`; hồi chiêu không đổi theo cấp.

## Thông số
| Kĩ năng | Hiệu lực lv1 | Vùng / đối tượng | Hồi chiêu | Lí do |
|---|---|---|---|---|
| Sấm sét | 80 máu mỗi quái | Quái cách mép thành ≤ 6u, cả hai bên | 20s | Giải vây sát thành; hồi ngắn nhất để là nút bấm chính |
| Băng giá | chậm 50% đi và đánh, 5s | Mọi quái đang sống | 35s | Mạnh nhất (cả màn) nên hồi lâu nhất |
| Cổ vũ | hồi 30% máu tối đa mỗi hero | Mọi hero đang đứng | 25s | Hiệp sĩ 300 máu hồi 90; Pháp sư 100 máu hồi 30 |

| Hiệu ứng | Xuất hiện | Biến mất |
|---|---|---|
| Sấm sét trên quái | lúc trúng | sau 0.3s (kể cả khi quái bị hạ) |
| Dấu chậm trên quái | lúc trúng Băng giá | hết thời gian chậm hoặc quái bị hạ |
| Cổ vũ trên hero | lúc được hồi | sau 0.5s |

Cân bằng dự kiến: Sấm sét dùng được 3 lần mỗi phút, mỗi lần dọn mọi Lính/Sói sát thành tới đợt 10. Băng giá 5s trong 35s nghĩa là quái bị chậm khoảng 14% thời gian.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Số kĩ năng có trong trận | 1 (Sấm sét) | 3 (Sấm sét, Băng giá, Cổ vũ) | 3 |
| Sấm sét | 80 máu, 6u, hồi 20s | như IDEA | theo số đã chỉnh sau chơi thử |
| Băng giá | không có | chậm 50%, 5s, hồi 35s | theo số đã chỉnh |
| Cổ vũ | không có | 30% máu tối đa, hồi 25s | theo số đã chỉnh |
| Hiệu ứng | sprite hiện 0.3s trên quái trúng (scale lên rồi tắt) | đủ ba hiệu ứng ở bảng trên | hiệu ứng toàn màn, âm thanh riêng từng kĩ năng |

## Trường hợp biên
- Bấm kĩ năng đang hồi: không có gì xảy ra, không trừ gì, không đặt lại hồi chiêu.
- Bấm Sấm sét khi vùng không có quái, Cổ vũ khi mọi hero đang gục, Băng giá khi không có quái: vẫn tính là đã dùng, bắt đầu hồi chiêu.
- Bấm hai kĩ năng trong cùng khung hình: xử lý theo thứ tự nhận lệnh; mỗi cái độc lập.
- Sấm sét hạ quái: quái bị hạ ngay, cho tiền (`feature-enemy/enemy-reward`).
- Quái bị chậm rồi bị hạ: trạng thái chậm và dấu chậm biến mất cùng quái.
- Băng giá trúng quái đang chậm: thời gian chậm đặt lại về đủ (5s ở lv1), mức chậm vẫn 50%.
- Cổ vũ trên hero đầy máu: không đổi gì, hiệu ứng vẫn hiện.
- Thành bị phá: không bấm được nữa; quái đang chậm giữ nguyên tới khi `feature-run` dừng trận.
- Hồi chiêu đang chạy khi chuyển sang đợt mới: không đặt lại; hồi chiêu chạy suốt trận, kể cả lúc nghỉ 3s giữa hai đợt.

## Đã tự quyết
- **Vùng Sấm sét bằng tầm tự bắn (6u).** Một vùng nguy hiểm cho người chơi nhớ. Đã loại: vùng rộng hơn (giảm ý nghĩa canh lúc).
- **Băng giá trúng cả quái đã sinh chưa vào màn.** Người chơi hiểu "mọi quái"; quái đã sinh chỉ cách mép màn 1u. Quái sinh sau không bị trúng, tránh hiểu thành vùng băng kéo dài.
- **Bấm khi không có mục tiêu vẫn mất hồi chiêu.** Giữ luật "bấm là dùng", không cần nút biết trạng thái chiến trường.
- **Làm mới, không cộng dồn Băng giá.** Một mức chậm, dễ đọc.

- **Frame `thunder-zone.frame` (2026-11-23).** Vùng Sấm sét `use` tầm tự bắn (`feature-castle/castle-attack/auto-shot-values#shot-range`), đúng quyết định "vùng Sấm sét bằng tầm tự bắn". Số tính ra: vùng trái bắt đầu ở x=-7.5u = -1.5u − 6u. Vùng chồng lên thành (`overlap`) chỉ ở cạnh chung, cố ý.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ điều kiện "không tạm dừng" khi bấm, trường hợp biên tạm dừng và trường hợp Băng giá trong luồng hồi sinh thành.

## Chưa rõ
- Sấm sét 80 sát thương có thể quá mạnh đợt 1-10 (hạ ngay mọi Lính, Sói): giữ theo bộ số chung; đề xuất xem lại sau chơi thử, có thể 50.
