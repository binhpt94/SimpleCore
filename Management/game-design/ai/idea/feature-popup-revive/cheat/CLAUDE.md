# Cheat: popup hồi sinh

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Ở bản dev, chọn state `waiting-ad` thì game dựng ngay trận đứng yên với popup hồi sinh đang chờ quảng cáo.
- Giá trị ép đi qua đúng đường của game: thành bị phá như `feature-castle/castle-core/castle-body-values`, luồng hồi sinh như `feature-popup-revive/revive-order/order-values`, popup như `feature-popup-revive/revive-popup/revive-spec`; quảng cáo dùng nhà cung cấp giả của `feature-ads/ads-request/ads-values`, đặt kết quả chọn sẵn để thử tiếp hai nhánh.
- Code cheat nằm trong thư mục `PopupRevive`, sau cờ dev; không lọt vào bản phát hành.

## Đã tự quyết
- **Một state: đang chờ quảng cáo.** State này có đủ mọi thành phần (thêm dòng chờ); state chờ chọn chỉ ẩn dòng chờ và làm nút sáng, nên không tách theo luật node cheat. Đã loại: hai state.
- **Đợt 8 khi thành bị phá.** Chỉ để trận phía sau có quái; số đợt không hiện trên popup.
- **Frame `use` toàn bộ số** từ `revive-spec`, khung popup từ `feature-popup/popup-layer/stack-rules`, cao nút từ `feature-hud/hud-layout/layout-spec`. Chỉ tự ghi nền che toàn màn 1920x1080.
- **Không dùng ở IDEA.** IDEA chưa có hồi sinh; cheat có từ PROTOTYPE.
- **Ảnh art-concept của popup hồi sinh chỉ đặt ở node này**, style `ui-flat`.

## Chưa rõ
- Không có điểm chặn.
