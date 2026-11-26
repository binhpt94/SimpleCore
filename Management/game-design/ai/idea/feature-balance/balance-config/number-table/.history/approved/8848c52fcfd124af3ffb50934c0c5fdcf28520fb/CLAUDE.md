# Bảng số chuẩn và nơi đặt

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Giữ bảng dưới đây làm bản đối chiếu duy nhất của bộ số chuẩn (dự kiến).
- Mỗi phiên feature ghi các số của mình vào cấu hình riêng (asset cấu hình ở `FeatureAssets/Private` của feature đó), đúng giá trị trong bảng.
- Đổi một số: sửa ở đây trước, xem lại các số ở cột "Xem lại khi đổi", rồi báo feature sở hữu sửa cấu hình.
- `feature-balance` không có code và không có cấu hình riêng.

## Thông số

Đơn vị: độ dài u; thời gian s; tiền xu. Mọi số là dự kiến.

### Bản đồ và thành (`feature-castle`, bản đồ chung của trận)
| Số | Giá trị | Sở hữu | Xem lại khi đổi |
|---|---|---|---|
| Dải đất thấy được | x = −16 … +16 u, một làn | `feature-castle` (vị trí thành), màn hình chung | điểm ra quái, tầm ra của hero |
| Thành | x = 0, rộng 3u, cao 4u (mép ở x = ±1.5) | `feature-castle` | tầm đòn tự bắn, tầm Sấm sét |
| Máu thành lv1 | 1000, không tự hồi | `feature-castle` | sát thương quái, đợt gãy |
| Đòn tự bắn | 10 sát thương / 1.0 s, tầm 6u từ mép, 1 mục tiêu | `feature-castle` | máu Lính, Sói |
| Sấm sét | 80 sát thương mọi quái trong 6u từ mép hai bên, hồi 20 s | `feature-castle` | máu Lính, Sói, Cung thủ quái theo đợt |
| Băng giá | mọi quái chậm 50% di chuyển và tốc độ đánh trong 5 s, hồi 35 s | `feature-castle` | nhịp thả |
| Cổ vũ | hồi 30% máu tối đa mọi hero đang đứng, hồi 25 s | `feature-castle` | máu hero, sát thương quái |
| Mỗi cấp máu thành | +150 máu tối đa | `feature-castle` | giá nâng máu thành |
| Mỗi cấp sức đánh thành | +20% sát thương lv1 (+2 mỗi cấp) | `feature-castle` | giá nâng sức đánh |
| Mỗi cấp kĩ năng | +20% hiệu lực lv1, hồi chiêu không đổi | `feature-castle` | giá nâng kĩ năng |

"Hiệu lực" của kĩ năng: Sấm sét là sát thương (80 → 96 → 112…); Băng giá là thời gian chậm (5 → 6 → 7 s, mức chậm giữ 50%); Cổ vũ là phần trăm hồi (30% → 36% → 42%…).

### Hero (`feature-hero`), lv1
| Hero | Máu | Sát thương | s mỗi đòn | Tầm (u) | Đi (u/s) | Kiểu đòn | Sát thương mỗi giây |
|---|---|---|---|---|---|---|---|
| Hiệp sĩ | 300 | 12 | 1.0 | 1.2 | 2.0 | 1 mục tiêu | 12.0 |
| Đấu sĩ rìu | 240 | 14 | 1.4 | 1.3 | 2.4 | chém vùng 1.5u trước mặt | 10.0 mỗi mục tiêu |
| Kiếm khách | 180 | 16 | 0.7 | 1.2 | 3.5 | 1 mục tiêu | 22.9 |
| Cung thủ | 120 | 14 | 0.8 | 7 | 3.0 | 1 mục tiêu | 17.5 |
| Pháp sư | 100 | 18 | 1.6 | 6 | 2.6 | nổ bán kính 1.5u | 11.25 mỗi mục tiêu |

Chung: đội 3 hero, chọn từ 5, không trùng; nhắm quái gần nhất cả hai bên, ra tối đa x = ±15u; hồi sinh 8 s, đầy máu, giữ cấp. Mỗi cấp: máu và sát thương +15% của lv1 (cấp L: ×(1 + 0.15×(L−1))); tốc độ đánh +3% mỗi cấp, tối đa +60% (cấp 21). Xem lại khi đổi: giá nâng hero, `feature-balance/balance-team/hero-compare`.

### Quái (`feature-enemy`), gốc ở đợt 1
| Loại | Máu | Sát thương | s mỗi đòn | Tầm (u) | Đi (u/s) | Tiền (xu) | Sát thương mỗi giây |
|---|---|---|---|---|---|---|---|
| Lính | 40 | 5 | 1.0 | 1.0 | 1.5 | 5 | 5.0 |
| Sói | 20 | 3 | 0.8 | 1.0 | 3.0 | 4 | 3.75 |
| Khổng lồ | 150 | 15 | 1.5 | 1.2 | 0.8 | 15 | 10.0 |
| Cung thủ quái | 25 | 6 | 1.2 | 5 | 1.5 | 6 | 5.0 |
| Trùm | 400 | 40 | 2.0 | 1.5 | 0.6 | 100 | 20.0 |

Xem lại khi đổi: hệ số đợt, đòn tự bắn và Sấm sét của thành, đợt gãy dự kiến.

### Đợt (`feature-wave`)
| Số | Giá trị | Xem lại khi đổi |
|---|---|---|
| Số quái đợt w | 6 + 2×w (có Trùm thì tính trong số này) | tiền mỗi đợt, đợt gãy |
| Nhịp thả | max(0.3, 1.0 − 0.05×(w−1)) s | đợt gãy, giới hạn quái sống |
| Hệ số máu, sát thương | 1 + 0.10×(w−1); tiền không nhân | đợt gãy |
| Mở loại | Lính 1, Sói 3, Khổng lồ 5, Cung thủ quái 7 | `feature-balance/balance-team/hero-compare` |
| Trùm | 1 con ở đợt chia hết cho 10 | đợt gãy |
| Bảng phần trăm loại | xem `feature-wave/difficulty-scaling/wave-formula` | tiền mỗi đợt |
| Giới hạn quái sống | 40 | hiệu năng |
| Lúc thở | 3 s | độ dài lượt |
| Thưởng dọn đợt | 20 + 5×w xu | `feature-balance/balance-economy/income-and-prices` |

### Nâng cấp (`feature-upgrade`) và ví (`feature-wallet`)
| Số | Giá trị | Xem lại khi đổi |
|---|---|---|
| Giá nâng hero từ cấp c | ⌈30×1.35^(c−1)⌉ xu | thu mỗi đợt |
| Giá nâng máu thành từ cấp c | ⌈40×1.4^(c−1)⌉ xu | thu mỗi đợt |
| Giá nâng sức đánh thành từ cấp c | ⌈40×1.4^(c−1)⌉ xu | thu mỗi đợt |
| Giá nâng mỗi kĩ năng từ cấp c | ⌈60×1.5^(c−1)⌉ xu | thu mỗi đợt |
| Cấp tối đa | không có | |
| Tiền đầu lượt | 50 xu | đợt 1–2 dễ |

Khác: điểm = số đợt đã dọn sạch; tạm dừng thì mọi thứ đứng yên.

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Bộ số dùng | bộ số chuẩn ở trên | bộ số chuẩn, chỉnh theo chơi thử | chốt sau chơi thử |
| Số nằm trong cấu hình, không viết cứng | có | có | có |
| Bảng đối chiếu cập nhật khi đổi số | có | có | có |

## Trường hợp biên
- **Số trong cấu hình của một feature khác số trong bảng:** bảng là đúng; feature sửa cấu hình. Nếu số trong cấu hình là số mới đã thống nhất với người thì sửa bảng.
- **Một số không có feature sở hữu rõ** (ví dụ bề rộng dải đất thấy được): ghi ở `feature-castle` vì thành là mốc của bản đồ; hỏi lại nếu một feature khác cần đổi nó.
- **Hai feature cùng cần một số** (ví dụ mép thành x = ±1.5 dùng cho tầm bắn và cho quái dừng lại): chỉ `feature-castle` giữ, feature khác lấy qua dịch vụ của thành, không chép số.

## Đã tự quyết
- **Ghi bộ số chuẩn đúng như agent chính giao, không đổi.** Đề xuất đổi số ghi ở "Chưa rõ" của các node `feature-balance` khác.
- **Ghi thêm cột "Sát thương mỗi giây" tính sẵn.** Dễ so hero với hero, quái với quái. Đây là số suy ra, không phải số cấu hình.
- **Định nghĩa "hiệu lực" của từng kĩ năng thành.** Bộ số chuẩn chỉ ghi "+20% hiệu lực"; chọn thứ dễ thấy nhất của mỗi kĩ năng. Đã loại: tăng mức chậm của Băng giá (50% → 60% → … sẽ chạm 100% ở cấp 6, quái đứng yên).

- **Trùm máu gốc 800 → 400, sát thương 40 giữ nguyên (2026-11-23, quyết định đã chốt).** Theo `feature-balance/balance-difficulty-curve/break-forecast`; đợt Trùm là ngoại lệ có chủ đích về độ khó.
- **Nghĩa "hiệu lực" kĩ năng đã chốt (2026-11-23).** Mỗi cấp +20% hiệu lực lv1: Sấm sét +16 sát thương, Băng giá +1 s thời gian chậm (mức chậm giữ 50%), Cổ vũ +6% máu tối đa hồi; hồi chiêu không đổi. Đã đối chiếu `feature-castle/castle-skills/skill-values`, `feature-castle/castle-upgrade-effects/upgrade-effect-values`, `feature-upgrade/upgrade-effect/effect-numbers`: cả ba dùng cùng nghĩa.
- **Rà lại sau khi node báo lên cấp của nâng cấp được viết lại (2026-11-25).** `feature-upgrade/upgrade-effect/effect-numbers` giờ chỉ giữ luật báo "mục X lên cấp L", không còn chép mức tăng; mức tăng mỗi cấp vẫn ở node cách tính cấp của nhánh hero và `feature-castle/castle-upgrade-effects/upgrade-effect-values`, khớp bảng này. Không đổi số nào.

## Chưa rõ
- Không có điểm chặn.
