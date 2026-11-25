---
based-on: 46d96a55a205
refs: feature-run/run-flow/flow-rules@f011f018b6c5, feature-castle/castle-core/castle-body-values@b648ec300023, feature-popup-revive/revive-popup/revive-spec@d5baa8be95f9, feature-wave/wave-clear/clear-rules@b10f4f27eaf5, feature-hero/hero-respawn/respawn-values@0ece6d5f17b9, feature-castle/castle-skills/skill-values@ad45ef4fdcc9, feature-wallet/income/income-amounts@f36980378e51, feature-enemy/enemy-reward/reward-values@30a4a6c88e50
frame: none
---
# Thời điểm từng bước của luồng hồi sinh

Phần của popup hồi sinh trong dòng thời gian từ lúc thành bị phá tới lúc chơi tiếp hoặc thua. Các mốc thời gian (trễ 1 giây, thành hiện lại, chạy tiếp, giữ tin dọn sạch) do lượt chơi và thành giữ, ghi ở node của hai feature đó.

## Ý cốt lõi

### no-own-timer | AI 7 | Popup hồi sinh không giữ mốc thời gian nào: mở khi lượt chơi nhờ, chỉ đóng khi người chơi chọn hoặc quảng cáo có kết quả
Popup không tự đếm trễ trước khi mở, không tự đóng sau một thời gian, không ra lệnh cho thành hay trận.
