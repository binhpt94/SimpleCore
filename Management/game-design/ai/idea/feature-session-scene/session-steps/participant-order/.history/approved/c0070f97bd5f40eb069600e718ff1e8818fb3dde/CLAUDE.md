# Danh sách tham gia và xử lí lỗi từng bước

Dữ liệu cốt lõi: [data.md](data.md)

## Làm gì
- Scene Loading: danh sách gồm `feature-cheat-panel` (nút cheat, có ở mọi scene) rồi `feature-scene-flow` (view màn Loading, `feature-scene-flow/scene-list/boot-loading`).
- Scene Game: danh sách theo thứ tự dưới đây. Tải và chạy từ đầu tới cuối danh sách; huỷ từ cuối về đầu.

| # | Feature | Vì sao đứng ở đây |
|---|---|---|
| 1 | `feature-sim-clock` | Nhịp chung và ngẫu nhiên theo seed; không view, không tải. Đứng đầu để mọi feature gameplay lấy được nhịp ngay lúc cài; huỷ sau cùng nên không feature nào còn chạy nhịp khi nhịp đã mất |
| 2 | `feature-popup` | Dịch vụ; mọi feature sau có thể mở popup |
| 3 | `feature-run` | Giữ luồng lượt chơi, kỉ lục; các feature trận nghe "lượt mới bắt đầu" từ nó |
| 4 | `feature-wallet` | Tiền; castle, enemy, upgrade dùng |
| 5 | `feature-battlefield` | Nền chiến trường; không cần feature nào, mọi thứ của trận vẽ lên trên nó |
| 6 | `feature-enemy` | Quái; giữ dịch vụ chiến trường mà hero và thành lấy khi cài |
| 7 | `feature-hero` | Hero; cần dịch vụ chiến trường của enemy |
| 8 | `feature-castle` | Thành; cần dịch vụ chiến trường của enemy và đội hero |
| 9 | `feature-wave` | Đợt quái; cần enemy |
| 10 | `feature-upgrade` | Nâng cấp; cần wallet, hero, castle |
| 11 | `feature-hud` | HUD trận |
| 12 | `feature-hud-upgrade` | Thanh nâng cấp; cần upgrade, wallet |
| 13 | `feature-popup-lose` | Popup thua; cần popup, run |
| 14 | `feature-popup-rating` | Popup mời đánh giá; cần popup, run; đứng sau popup thua vì lời mời mở chồng lên popup thua |
| 15 | `feature-cheat-panel` | Nút và panel cheat; đứng sau mọi feature có cheat để gom state của chúng, trước scene-flow để nút đã có khi scene Loading bỏ lớp che. Không có gì khi config cheat tắt |
| 16 | `feature-scene-flow` | Luôn cuối: đặt Game làm scene chính lúc tải; lúc chạy mở cổng báo scene Loading bỏ lớp che (mọi feature trước đã chạy xong); phiên lỗi thì báo tên feature lỗi cho màn Loading. Không view, không tải |

- Bước tải: lần lượt từng feature, feature sau bắt đầu tải khi feature trước xong. Báo tiến độ sau mỗi feature (thanh tiến độ của màn Loading).
- Bước chạy: lần lượt từng feature tạo view; cả bước xong trong một khung hình.
- Bước huỷ: lần lượt ngược lại; mỗi feature huỷ view và bỏ thứ đã tải.

## Thông số
| Thông số | Giá trị | Đơn vị | Lí do |
|---|---|---|---|
| Số feature tham gia scene Loading | 2 | feature | Nút cheat và màn Loading |
| Số feature tham gia scene Game | 16 | feature | Mọi feature có view hoặc cần tải ở mức đang chơi được, cộng `feature-sim-clock` (nhịp chung), `feature-cheat-panel` (nút cheat ở mọi scene) và `feature-scene-flow` (cổng báo scene Loading); trừ `feature-session-scene` (chính dịch vụ phiên), `feature-balance` |
| Thời gian tải tối đa một feature | 10 (dự kiến) | giây | Đủ rộng cho máy yếu; quá mức này gần như chắc là lỗi |
| Thời gian bước chạy cả scene | 1 | khung hình | Không tải gì thêm ở bước chạy |
| Thời gian bước huỷ cả scene | không giới hạn, không chờ tải | — | Huỷ chỉ dọn, không chờ mạng hay file |
| Cách tải | Lần lượt | — | Dễ biết feature nào lỗi, tiến độ rõ |

## Thông số theo giai đoạn
| Thông số | IDEA | PROTOTYPE | FULL |
|---|---|---|---|
| Cách tải các feature | Lần lượt | Lần lượt | Lần lượt (xét tải song song nếu quá 3 giây) |
| Thời gian tải tối đa một feature | 10 giây (dự kiến) | 10 giây | 10 giây |
| Số feature scene Game | 16: sim-clock, popup, run, wallet, battlefield, enemy, hero, castle, wave, upgrade, hud, hud-upgrade, popup-lose, popup-rating, cheat-panel, scene-flow | 20: sim-clock vẫn đầu; thêm `feature-combat` (trước hero, enemy, castle là bên ra đòn; chờ installer), và sau popup-rating theo thứ tự home, team-select (cần run, hero), game-flow; cheat-panel ngay trước scene-flow, scene-flow vẫn cuối | Theo danh sách feature lúc đó |

## Trường hợp biên
- **Feature thứ k tải lỗi**: không tải feature k+1 trở đi; huỷ feature k (phần đã tải được) rồi k-1 về 1; báo lỗi nêu tên feature k.
- **Feature thứ k chạy lỗi**: không chạy k+1 trở đi; huỷ từ cuối danh sách về đầu (mọi feature đều đã tải); báo lỗi nêu tên feature k.
- **Feature lỗi ở bước huỷ**: ghi lỗi, huỷ tiếp feature đứng trước nó; bước huỷ luôn đi hết danh sách.
- **Yêu cầu huỷ đến khi scene đang tải**: chờ feature đang tải xong (hoặc quá 10 giây), rồi huỷ các feature đã tải theo thứ tự ngược.
- **Một feature có tên trong danh sách hai lần**: lỗi ngay trước bước tải, nêu tên feature trùng; scene không tải.
- **Danh sách rỗng**: scene vẫn đi qua ba bước, không có view nào (chỉ xảy ra khi cấu hình sai; ghi cảnh báo).
- **Người chơi rời game lúc đang tải**: tải tiếp khi quay lại; thời gian rời game không tính vào 10 giây.

## Đã tự quyết
- **Tải lần lượt, không song song.** Đơn giản, biết rõ feature nào lỗi, thanh tiến độ thật. Đã loại: tải song song (nhanh hơn nhưng khó lần lỗi).
- **Enemy, Hero, Castle theo thứ tự phụ thuộc (2026-11-25, theo @CodeLead qua @Manager).** Hero và Castle lấy dịch vụ chiến trường của Enemy ngay khi cài; Castle lấy thêm đội hero. Bản trước xếp Castle trước vì "hero sinh ra từ thành", nhưng đó là vị trí trong trận, không đòi thành tải trước. Đã loại: giữ thứ tự cũ và đổi code ba feature (việc lớn, không được gì).
- **Danh sách scene Game ở IDEA không có màn bắt đầu, màn chọn đội (2026-11-25, theo @CodeLead qua @Manager).** Bản trước ghi 15, lệch với các node đã chốt: bảng giai đoạn của `feature-home/home-screen/home-screen-spec` ("Không, mở game vào thẳng trận"), `feature-team-select/team-slots/slots-spec` ("Không có; đội cố định") và `feature-game-flow/flow-routes/routes-values` (chỉ có màn trận). Các feature đó vào lại danh sách ở PROTOTYPE. Đã loại: nối ở IDEA các feature IDEA không dùng.
- **Thêm `feature-scene-flow` vào cuối danh sách scene Game (2026-11-25, theo @CodeLead qua @Manager).** Bản trước loại scene-flow vì nó không có view, không tải. Code thật cần nó là bên tham gia: đặt scene chính lúc tải, mở cổng cho scene Loading bỏ lớp che lúc chạy, báo feature lỗi khi phiên hỏng. Nó phải cuối để cổng chỉ mở khi mọi feature khác đã chạy. `feature-session-scene` vẫn không tính: nó là dịch vụ chạy phiên. Đã loại: giữ 12 và coi cổng là việc ngoài phiên (cổng mở sai lúc, lỗi phiên không về được màn Loading).
- **Thứ tự PROTOTYPE theo Boot.Game (2026-11-25).** Các feature của PROTOTYPE nối sau popup-lose theo đúng thứ tự @CodeLead đã cài; thoả mọi phụ thuộc (team-select sau hero). Bản trước chen ads sau popup, game-flow sau run, home và team-select trước hud: không phụ thuộc nào đòi vậy. Combat đặt trước ba bên ra đòn. Ở IDEA các feature đó không tham gia.
- **Thêm `feature-sim-clock` đứng đầu danh sách scene Game (2026-11-25, theo @CodeLead).** Nhịp chung là một bên tham gia không view, chạy nhịp mỗi khung; đứng trước popup để mọi feature gameplay lấy được nhịp ngay lúc cài, và huỷ sau cùng. Code đã nối SimClock ngay sau SessionScene, test Boot của scene Game đã có nó ở đầu. Đã loại: để nhịp ngoài phiên (không được dọn khi rời scene, trái `feature-session-scene`).
- **Popup đứng ngay sau nhịp chung (trước đây đứng đầu).** Feature nào cũng có thể cần mở popup khi chạy; popup huỷ sau cùng nên không popup nào còn chủ mà mất dịch vụ.
- **Run đứng trước các feature trận.** Các feature trận nghe "lượt mới bắt đầu" từ `feature-run/run-start`.
- **Balance không tham gia.** Số liệu nằm trong cấu hình của từng feature (`feature-balance/balance-config`), không có view hay việc tải riêng.
- **Lỗi tải, chạy thì dừng; lỗi huỷ thì đi tiếp.** Tải thiếu là chơi sai; huỷ dở là rò rỉ.
- **Bỏ tạm dừng, quảng cáo và popup hồi sinh (2026-11-26, người quyết).** Bỏ popup-pause, ads, popup-revive khỏi danh sách scene Game ở PROTOTYPE; số feature PROTOTYPE từ 21 còn 18.
- **Thêm `feature-popup-rating` (2026-11-27, người quyết).** Popup mời đánh giá thuộc IDEA, mở chồng lên popup thua, nên đứng ngay sau `feature-popup-lose`; scene Game từ 14 thành 15 feature, PROTOTYPE từ 18 thành 19. Khớp với Boot.Game và BootTests @CodeLead đã nối.

- **Thêm `feature-cheat-panel` vào scene Loading và scene Game, ngay trước `feature-scene-flow` (2026-11-27, theo @CodeLead).** Thiết kế đã chốt của cheat-panel nói nút cheat có ở mọi scene; code đã nối CheatPanelInstaller vào Boot.Loading và Boot.Game ngay trước SceneFlow. Đứng sau mọi feature có cheat để gom state của chúng; scene-flow vẫn cuối để cổng chỉ mở khi nút đã có. Loading từ 1 thành 2, Game IDEA từ 15 thành 16, PROTOTYPE từ 19 thành 20. Đã loại: để cheat-panel ngoài phiên (nút không theo vòng đời scene, huỷ scene không dọn nút).

## Chưa rõ
- Không còn điểm chặn về thứ tự enemy, hero, castle (đã chốt theo phụ thuộc trong code, 2026-11-25).
- Thêm feature mới vào game thì phải thêm vào bảng này.
