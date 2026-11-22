# 專案接手入口

開始修改前，先讀 `README.md` 與 `docs/PROJECT_STATUS.md`，再確認 `git status`。
狀態文件是接手摘要；若與實際程式或本次使用者要求不同，以實際程式與本次要求為準。

## 專案定位

- 使用 Unity **6000.0.76f1**。目前是程序化世界生成與參數編輯工具原型。
- 實際生成入口是 `Assets/Script/Meta/Editor/WorldMapEditor.cs`。
- `Assets/Script/Meta/WorldMap.cs` 是尚未接通的執行期骨架；不要誤認它已提供完整生成流程。
- 回合、角色移動、戰鬥、AI 與勝敗流程尚未實作。

## 修改時維持的約定

- 格子陣列統一使用 `MathUtility.MapIndex(x, y, height)`，也就是 `x * height + y`。
- 生態表的順序固定為「濕度 → 高度 → 溫度」。
- Editor 專用類別放在名稱正確的 `Editor` 資料夾，執行期程式不引用 `UnityEditor`。
- `Edtitor` 是保留中的舊拼字資料夾，內有場景使用的執行期展示元件，不能整個當作 Editor 程式刪除。
- 移動 Unity 資產時連同原 `.meta` 搬移，保留 GUID；新增資產與資料夾要有對應 `.meta`。
- 生態、人口參數陣列調整尺寸時保留重疊範圍，只初始化新增元素；縮小尺寸會移除範圍外資料。
- 不因修改演算法而覆寫使用者的參數資產或正常存檔。
- Odin、Ads、Analytics、UGUI、Timeline、NavMesh、XR 輸入輔助等未使用插件已移除；不要為了解決快取中的舊引用直接裝回。
- 不提交 `Library/`、`Temp/`、`Logs/`、`UserSettings/`、本機 IDE 設定與自動產生的 solution/project 檔案。

## 驗證與交接

- 生成、參數編輯或存檔變更，執行 `WorldGenerationRegressionTests` 的 EditMode 測試。
- 測試檔在 `Assets/Tests/Editor/WorldGenerationRegressionTests.cs`；執行方式見現況文件。
- 只做外部 C# 編譯不能替代 Unity 中的序列化、SerializedProperty、資產引用與檔案測試。
- 清楚區分「編譯通過」「EditMode 測試通過」「已實際操作介面」「完整打包通過」。
- 文件中的測試數字與 commit 是指定日期的基準，不代表目前工作目錄已重新驗證。
- 完成會改變架構、操作方式或待辦狀態的工作時，同步更新相關文件。
