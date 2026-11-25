---
based-on: 53c6e52aecb5
refs: feature-castle/castle-core/castle-body-values@b648ec300023, feature-popup-revive/revive-order/order-values@80126913acc8, feature-popup-revive/revive-popup/revive-spec@d5baa8be95f9, feature-ads/ads-request/ads-values@d2cbf130c039, feature-popup/popup-layer/stack-rules@fa85847e35ee, feature-hud/hud-layout/layout-spec@1a7ddf5f3b6c
---
# Cheat: popup hồi sinh

State dựng nhanh popup hồi sinh ở bản dev để test, Play và gen ảnh concept, không phải chơi tới lúc thành bị phá.

## Ý cốt lõi

### waiting-ad | AI 7 | Thành vừa bị phá lần đầu, popup hồi sinh mở ở trạng thái đang chờ quảng cáo
Ép: trận đứng yên ở đợt 8, thành 0 máu, cờ "đã đề nghị" bật, nhà cung cấp quảng cáo giả đang chờ. Có tiêu đề "Thành đã bị phá", dòng hồi sinh, dòng "Đang chờ quảng cáo...", nút "Xem quảng cáo" và "Kết thúc lượt" đang khoá.
