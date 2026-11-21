# Shop

Bán vật phẩm bằng xu. Làm chủ danh mục vật phẩm và giá; không giữ số xu.

- Dùng `IWalletService.TrySpend` (Wallet) để trừ xu, `IPopupService.Show` (Popup) để báo mua xong.
- Chưa có Contracts: chưa feature nào cần gọi Shop. Ai cần thì đề xuất.
- Danh mục, prefab popup và prefab view mua: `ShopConfig.asset` trong `FeatureAssets/Private`, lúc chạy chỉ đọc.
- Popup "đã mua" là của Shop (`PurchasedPopup.prefab`), Shop đưa prefab cho dịch vụ Popup lúc chạy.
- Logic nằm trong `ShopLogic` (C# thuần). Không có static: `ShopBuyPresenter` tham gia `ISessionScene`, tạo view từ `ShopBuyView.prefab` trong `Run`, huỷ trong `DisposeAsync`.
- View (`ShopBuyButton`) chỉ hiện thực `IShopBuyView` và báo `BuyRequested`; không biết `ShopLogic`. Tạo, huỷ object nằm sau `IShopBuyViewSource`.
- Installer lỗi ngay nếu thiếu config, prefab popup, prefab view, hoặc prefab view không có `IShopBuyView` ở gốc.
- Phải được cài sau SessionScene, Wallet và Popup trong `Boot.prefab`.

Test: assembly `Game.Shop.Tests` (EditMode), dùng wallet, popup, session và view giả.
