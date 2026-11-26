---
based-on: d0ede8765995
refs: feature-session-scene/session-steps@70f3b17acc2e, glossary#caller@71914587fe09, glossary#popup-id@6773c11a645d
---
# Nhờ hiện và đóng popup

Feature muốn hiện popup đưa cho dịch vụ id popup của mình; dịch vụ dựng popup, trả cho feature gọi để điền nội dung và nghe nút bấm, rồi đóng khi được yêu cầu.

## Ý cốt lõi

### popup-owned-by-caller | AI 8 | Hình dáng và nội dung popup thuộc feature gọi, dịch vụ không biết popup nói gì
Ví dụ popup thua là của `feature-popup-lose`. Đã loại: dịch vụ giữ sẵn mẫu popup cho mọi feature.

### popup-id | AI 8 | Feature gọi xin hiện popup bằng id dạng `<Owner>/<Name>`
`Owner` là tên feature gọi, `Name` là tên popup của nó. Đã loại: đưa thẳng object popup cho dịch vụ.

### popup-caller-fills | AI 7 | Dịch vụ dựng popup xong thì feature gọi điền nội dung và xử lí nút bấm
Dịch vụ không quyết định kết quả bấm nút.

### popup-close | AI 7 | Popup đóng khi feature gọi yêu cầu đóng
Thường sau khi người chơi bấm một nút của popup. Đã loại: popup tự đóng khi bấm ra ngoài (dễ bấm nhầm mất popup thua).

### popup-scene-bound | AI 7 | Popup thuộc scene đang chạy; rời scene thì popup đang mở bị huỷ theo
Dịch vụ tham gia vòng đời scene (`feature-session-scene`).
