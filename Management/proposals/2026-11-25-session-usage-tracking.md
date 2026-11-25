# Đề xuất: các feature gọi TrackView và TrackJob để SessionScene đếm đúng

Người đề xuất: @Manager · 2026-11-25 · Chờ người duyệt

## Vấn đề
@CodeLead đã dựng khung đếm trong Core:
- contract `Core/ISessionParticipantUsage.cs`, có `HeldViews` và `RunningJobs`;
- hai hàm protected `TrackView` và `TrackJob` trong `SessionParticipantBase`.

Biên dịch 0 lỗi, test 77/77 qua.

`_Services/SessionScene` sẽ đọc số đếm này cho `feature-session-scene/session-clean/clean-checks`. Nhưng số đếm chỉ đúng khi từng feature tự gọi `TrackView` lúc tạo view và `TrackJob` lúc chạy việc hẹn giờ, rồi Dispose handle khi dọn. Không gọi thì số luôn là 0. Khi đó `#after-dispose-zero` lúc Play vẫn qua, nhưng không chứng minh được gì.

Trong `live`, không feature nào có yêu cầu trong sổ của chính nó đòi việc này. Việc đếm chỉ có ở `clean-checks`, thuộc sổ SessionScene. Phần dọn khi chơi lại của Run là `replay-checks#replay-no-growth`, đã kiểm bằng Play và không đòi API đếm. Luật giao việc theo id trong sổ, nên @Manager không giao được.

Vấn đề này cùng loại với `2026-11-25-gd-req-cross-feature-owner.md`: việc của nhiều feature nằm trong sổ của một feature khác.

## Feature phải gọi
@CodeLead dò theo chỗ đang Instantiate view và chỗ có việc hẹn giờ.
- **TrackView:** SceneFlow (trừ lớp che chuyển scene), `_Services/Popup`, Hud, HudUpgrade, TeamSelect, Battlefield, Castle, Enemy, Hero, Ads.
- **TrackJob:** chắc chắn có Run (chờ 1 giây), Wave (nghỉ giữa đợt), Combat (vòng tick), Castle (hồi chiêu), PopupRevive và PopupLose (chờ). Danh sách đủ thì mỗi feature tự rà.

## Đề xuất (chọn một)
- **A (đề xuất):** người duyệt đây là việc kỹ thuật chung, không cần ý thiết kế. @Manager giao qua hộp thư cho từng feature trong danh sách:
  - gọi `TrackView` và `TrackJob`, Dispose handle trong phần dọn tương ứng;
  - viết test EditMode trong `Tests` của chính feature: tạo rồi dọn thì số về 0.

  Sổ không có id cho việc này, nên @CodeLead kiểm bằng test và báo cáo. Nếu đề xuất `gd-req-cross-feature-owner` được duyệt theo A, có thể gắn test vào `clean-checks#after-dispose-zero` của SessionScene.
- **B:** @GameDesigner thêm vào node vòng 4 của từng feature (hoặc node cheat) một ý "báo số view và việc đang chạy cho SessionScene". Khi đó mỗi sổ tự có một yêu cầu. Cách này đổi node đã live ở khoảng 13 feature, nên người phải chốt lại.
- **C:** tạm không làm. SessionScene làm xong logic, và người bỏ qua trên trang phần `#after-dispose-zero` cần số thật cho tới PROTOTYPE.

## Ảnh hưởng
- Không chặn việc khác: SessionScene vẫn viết được phần đọc số, @Core vẫn viết test cho Core.
- Chọn A hoặc B là khoảng 13 lượt việc nhỏ. Các lượt này chạy song song được, vì mỗi feature chỉ sửa vùng của mình. Không đổi contract nào.

## Quyết định của người (2026-11-25)
Duyệt **A**, kèm hai điều:
- Ngoại lệ luật "giao theo id": việc kỹ thuật do khung đòi, có đề xuất người đã duyệt, thì giao qua hộp thư không cần id sổ. Tin nêu tên đề xuất này. @CodeLead kiểm bằng test EditMode của từng feature và ghi kết quả vào báo cáo. Luật đã ghi ở skill `manager-dispatch`, `feature-implement-req` và `.claude/rules/codelead.md`.
- **Chốt chặn**: @GameDesigner thêm **một** ý vào `feature-session-scene/session-clean/clean-checks`: "lúc trận đang chạy, mỗi feature có view thì số view nó báo lớn hơn 0; số 0 là lỗi nêu tên feature đó", rồi người chốt lên `live`. Feature nào quên gọi `TrackView` thì sổ của SessionScene đỏ, và chỉ đúng feature đó.
- @Manager giao **theo đợt, mỗi đợt tối đa 4 feature** (trần của hộp thư), theo danh sách ở mục "Feature phải gọi". Nội dung tin: gọi `TrackView` khi tạo view, `TrackJob` khi chạy việc hẹn giờ, Dispose khi dọn; test EditMode "tạo rồi dọn thì số về 0"; không có view hay việc hẹn giờ thì báo không cần. Các feature không có trong danh sách tự rà khi tới lượt.
