# Mức tiền thu

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Nhận lời gọi cộng tiền từ hai bên báo: `feature-enemy` khi một quái bị hạ, `feature-wave` khi một đợt được dọn sạch. Mỗi lời gọi kèm đúng số tiền.
- Cộng đúng số đó vào số dư ngay trong khung hình được gọi, theo luật số dư của `feature-wallet/balance-spend/wallet-rules` (cộng 0 bỏ qua, số âm bị từ chối, trần 999.999.999).
- Nhiều lời gọi trong một khung hình: cộng lần lượt theo thứ tự tới.
- Lượt đã kết thúc thì bỏ qua mọi lời gọi cộng cho tới khi lượt mới đặt lại số dư.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số lần cộng cho mỗi lời gọi | 1 | lần | `add-once-per-call` |
| Độ trễ từ lúc được gọi tới lúc số dư đổi | 0 | khung hình | `income-same-frame` |
| Lời gọi cộng sau khi lượt kết thúc | bỏ qua | | `ignore-after-run-end` |

Số tiền cụ thể (tham khảo, không phải của node này):
- Tiền hạ quái: `feature-enemy/enemy-reward/reward-values` (Lính 5, Sói 4, Khổng lồ 15, Cung thủ quái 6, Trùm 100).
- Thưởng dọn đợt: `feature-wave/wave-clear/clear-rules` (20 + 5 × số đợt vừa dọn).

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Nhận cộng từ quái bị hạ | có | có | có |
| Nhận cộng từ dọn đợt | có | có | có |
| Bỏ qua lời gọi sau khi lượt kết thúc | có | có | có |

## Trường hợp biên
- **Hai lời gọi cộng cùng khung hình** (ví dụ đòn vùng hạ nhiều quái): mỗi lời gọi một lần cộng, lần lượt theo thứ tự tới.
- **Con quái cuối của đợt gục**: tiền con quái tới trước, thưởng dọn đợt tới sau; ví cộng đúng thứ tự đó.
- **Quái gục cùng khung hình thành bị phá**: không cộng. Bên báo không gửi (`feature-enemy/enemy-reward/reward-values`); nếu lời gọi vẫn tới sau khi lượt đã kết thúc thì ví bỏ qua.
- **Tiền của lượt cũ tới sau khi lượt mới đã bắt đầu**: bỏ qua, không cộng vào lượt mới (`feature-run/run-start/start-values`).
- **Lời gọi cộng 0 tiền hoặc số âm**: theo `feature-wallet/balance-spend/wallet-rules` (0 bỏ qua, âm bị từ chối và ghi lỗi).
- **Cộng làm số dư vượt trần**: dừng ở 999.999.999 tiền theo `feature-wallet/balance-spend/wallet-rules`.

## Đã tự quyết
- **Viết lại node chỉ còn luật của ví (2026-11-25, người chọn hướng A).** Bản trước chép bảng tiền hạ quái, công thức thưởng dọn đợt và luật "mỗi quái, mỗi đợt một lần" của nhánh quái và nhánh đợt, nên sổ yêu cầu giao các luật đó cho Wallet dù ví không có code cho chúng. Các ý `kill-table`, `kill-once`, `kill-only-defeat`, `wave-formula`, `wave-once` bỏ khỏi node này; luật đó vẫn có ở `feature-enemy/enemy-reward/reward-values` (`reward-table`, `reward-once-instant`) và `feature-wave/wave-clear/clear-rules` (`clear-bonus`, `clear-order`). Đã loại: giữ bản chép (hai nơi cùng một luật, sẽ lệch).
- **Sửa lệch "quái gục cùng khung hình thành bị phá" theo nhánh quái: không cộng (2026-11-25).** Bản trước ghi "ví vẫn cộng", ngược với `reward-values`. Đã loại: giữ "vẫn cộng" (hai node đã chốt nói ngược nhau).
- **Ví bỏ qua lời gọi sau khi lượt kết thúc.** Một chốt chặn phía ví cho cùng luật đó, khớp trường hợp biên "tiền tới sau khi lượt cũ kết thúc: bỏ qua" của `start-values`. Đã loại: chỉ trông vào bên báo (lỗi thứ tự ở bên báo sẽ làm tiền lọt vào lượt mới).
- **Thứ tự cộng là thứ tự gọi tới.** Ví không tự sắp xếp; thứ tự tiền con quái rồi thưởng đợt do nhánh đợt bảo đảm.

## Chưa rõ
- Không có điểm chặn.
