# Đề xuất: gd_req.py nhận test ở `_Composition/Tests` cho yêu cầu nối dây

Người đề xuất: @Manager · 2026-11-25 · Chờ người duyệt

## Vấn đề
Một số yêu cầu trong sổ của dịch vụ là danh sách nối dây. Danh sách đó nằm ở `Assets/_Composition` (Boot variant, catalog), không nằm trong code của dịch vụ. Test đúng cho chúng là `BootTests` trong `_Composition/Tests`, do @CodeLead viết.

`gd_req.py <Tên> done` chỉ nhận test nằm trong `Tests` của chính feature, nên các yêu cầu này không đóng được:
- `_Services/SessionScene`: `participant-order#loading-list`, `#game-list`, `#params`. Test đã có: `BootTests.LoadingScene_HasOnlySceneFlowAsParticipant`, `BootTests.GameScene_AtIdea_JoinsTwelveFeaturesInTheDesignedOrder`.
- `_Services/Popup`: `request-rules#id-list`, `#params` (catalog Popup của `Boot.Game`).

Câu lỗi: `Không ghi: không thấy test [Test] nào tên BootTests.LoadingScene_HasOnlySceneFlowAsParticipant trong UnityProject/Assets/Features/_Services/SessionScene/Tests`.

Chép test sang `Tests` của dịch vụ là sai: dịch vụ không đọc được `_Composition` theo cách test đó cần, và một test nằm hai nơi.

## Đề xuất (chọn một)
- **A (đề xuất):** `gd_req.py` nhận thêm test trong `UnityProject/Assets/_Composition/Tests` khi tên lớp là `BootTests`, hoặc với mọi lớp trong thư mục đó. Phiên sở hữu vẫn là bên ghi `done`; @CodeLead vẫn `verify`.
- **B:** cho @CodeLead ghi `done` thay phiên sở hữu cho các yêu cầu có test ở `_Composition`.
- **C:** người bấm bỏ qua 5 yêu cầu này trên trang, và chấp nhận `BootTests` là bằng chứng ngoài sổ.

## Ảnh hưởng
Không chặn việc chơi được game ở IDEA. Chỉ để sổ của SessionScene và Popup đóng được, và để các node đó đánh được IDEA `done`.

## Quyết định của người (2026-11-25)
Duyệt **A (gọn)**, đã làm: `gd_req.py done` nhận test trong `UnityProject/Assets/_Composition/Tests` cho mọi feature (không giới hạn tên lớp; chỉ @CodeLead viết vùng đó). Phiên sở hữu vẫn ghi `done`, @CodeLead vẫn `verify`. Lưu ý: test trong đề xuất đã đổi tên thành `BootTests.GameScene_AtIdea_JoinsItsFeaturesInTheDesignedOrder`.
