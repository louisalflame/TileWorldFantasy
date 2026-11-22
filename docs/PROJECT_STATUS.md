# TileWorldFantasy 專案現況

更新日期：2026-11-22（Asia/Taipei）

程式基準：`6706568` — `fix: harden world generation and map persistence`

## 目前做到哪裡

最初目標是隨機世界、回合制、roguelike 策略遊戲。目前完成的是世界生成工具原型，
尚未形成可玩的遊戲循環。原本停在 Unity 2020.3.14f1，現已升級到 **6000.0.76f1**。

操作入口：Unity 選單 **Editor > WorldMapEditor**。
唯一的 `Assets/Script/Meta/Edtitor/EditScene.unity` 是噪聲與天氣展示場景。
`ProjectSettings/EditorBuildSettings.asset` 目前沒有設定建置場景。

## 架構與檔案導覽

| 路徑 | 用途 |
| --- | --- |
| `Assets/Script/Meta/Editor/WorldMapEditor.cs` | 參數、生成流程、預覽、存讀檔操作 |
| `Assets/Script/Meta/Generator/` | 地形、濕度、溫度、魔力、河流、人口與隨機點生成器 |
| `Assets/Script/Meta/GeneratorParameter/` | 生成參數與預覽色階的 ScriptableObject 類型 |
| `Assets/Script/Meta/BiomeDistribute/` | 生態分布資料、分類器與自訂 Inspector |
| `Assets/Script/Meta/PopulationDistribute/` | 人口機率資料與自訂 Inspector |
| `Assets/Script/Meta/GenerationValidation.cs` | 地圖尺寸、陣列、數值與生成參數驗證 |
| `Assets/Script/Meta/TileUnit.cs` | 單格地形、氣候、魔力、河流、生態等資料 |
| `Assets/Script/Meta/WorldMap.cs` | 尚未完成的執行期世界骨架 |
| `Assets/Script/Meta/Edtitor/` | 舊展示場景與仍供場景使用的 MonoBehaviour；資料夾拼字尚未整理 |
| `Assets/Script/SaveLoad/SaveData.cs` | 存檔入口、版本格式、驗證、備份與舊格式遷移 |
| `Assets/Data/` | 目前使用的世界參數、生態分布、27 種生態與色階，共 40 個設定資產 |
| `Assets/plugin/Utility/` | 自製 Coroutine、Executor、Monad 與 Tuple 工具，與已移除的 Odin 無關 |
| `Assets/Tests/Editor/WorldGenerationRegressionTests.cs` | Unity EditMode 回歸測試 |

生成順序：

```text
seed
  → 地形、濕度、溫度、魔力
  → 根據地形產生河流
  → 根據濕度／高度／溫度分類生態並組裝 TileUnit
  → 根據環境機率產生人口陣列
  → TileDataUnit
  → 編輯器預覽或存檔
```

`BlockMonad<T>` 接收生成結果，`Executor` 分段在主執行緒執行 Coroutine。
這套流程不是背景執行緒平行計算。

## 本次重啟已完成的工作

| Commit | 內容 |
| --- | --- |
| `e9a5968` | 升級 Unity 6000.0.76f1、套件與新版專案設定 |
| `42fa32e` | 移除未使用的 Odin Inspector 與編譯符號 |
| `ac54061` | 先以條件編譯排除 Editor 程式，解除遊戲端編譯阻礙 |
| `a6150a7` | 清理未使用套件、Ads 帶入的 Resolver，補齊 Git 忽略規則 |
| `804d31f` | 正式分離 EditorWindow 與三個自訂 Inspector，保留原場景腳本 GUID |
| `6706568` | 修正生成索引、邊界、參數陣列與存檔，新增回歸測試 |

目前保留的非引擎模組直接套件：2D Sprite、2D Tilemap、Visual Studio／VS Code 整合、Test Framework。
Unity 內建引擎模組仍保留。

### 生成與編輯器修正

- 河流統一使用 column-major 索引，修正非正方形地圖的錯誤鄰接關係。
- 河流無來源或平坦地形回傳全零圖；平均水量使用浮點計算，避免除以零與整數截斷。
- 人口生成允許正規化數值的兩端 `0` 與 `1`，不再產生超出陣列的索引。
- 中等正魔力節點使用自己的數量參數，避免誤用高等節點數量。
- 生態表使用正確維度，新增元素明確初始化，保留既有重疊資料。
- 舊生態資產有 100 個有效格，另有多餘陣列槽位。編輯器會依宣告尺寸修剪；
  現有設定資產未被這次修正直接覆寫，測試確認修剪可保留原有 100 格引用。
- 人口參數編輯器同樣保留舊機率值，新增欄位預設為零。
- 生成前檢查尺寸、必要參數、陣列、機率、非有限數值與噪聲設定。
- 生成錯誤會停止目前工作並顯示原因，不會反覆執行同一失敗 Coroutine。

### 存檔行為

- 新存檔：`Application.persistentDataPath/world-map.json`，格式版本目前為 `1`。
- 驗證資料後先寫 `.tmp` 並 flush，完成後替換主檔；前一版保留為 `.bak`。
- 主檔無法讀取時嘗試讀取備份，介面會說明備份回復。
- 存檔失敗會回傳失敗與原因，不再顯示假的成功訊息。
- 讀檔失敗保留編輯器目前已載入的地圖。
- 沒有主檔與備份時，支援讀取舊 `PlayerPrefs` 的 `SAVE_KEY` 並遷移。
  舊 preference 不會刪除；遷移寫入失敗時仍可使用讀到的有效舊資料。
- `TileDataUnit.Populations` 是目前生成與存檔使用的人口陣列。

## 驗證基準與重跑方式

在 `6706568` 對應的程式內容上，使用 **Unity 6000.0.76f1** 於獨立測試專案實跑：
**28 項 EditMode 測試全部通過，沒有略過項目**。

涵蓋矩形河流、平坦地形、無來源、非法尺寸與數值端點、參數陣列保留、
生態維度與舊表修剪、魔力數量、存檔往返與版本、備份回復、檔案寫入失敗、
壞資料保護、舊格式遷移，以及兩種長寬方向的完整世界生成與 seed 重現性。

在 Unity Test Runner 執行 EditMode 的 `WorldGenerationRegressionTests`。
也可以關閉這個專案的 Editor 後，於專案根目錄使用 PowerShell：

```powershell
$unityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.0.76f1\Editor\Unity.exe'
$projectPath = (Get-Location).Path
New-Item -ItemType Directory -Force -Path Logs | Out-Null
& $unityEditor -batchmode -nographics -projectPath $projectPath `
  -runTests -testPlatform EditMode -testFilter WorldGenerationRegressionTests `
  -testResults (Join-Path $projectPath 'Logs/EditMode-results.xml') `
  -logFile (Join-Path $projectPath 'Logs/EditMode.log')
```

Editor 安裝路徑依機器調整。不要對已開啟的同一專案再啟動 batch Editor；
需要並行驗證時可使用獨立副本。測試本身使用暫存存檔目錄，不覆寫正常遊戲存檔。

**尚未驗證完整遊戲打包與修正後的全部介面操作。** 多個 Unity 專案同時開啟時，
全域 `Editor.log` 可能屬於另一個專案；必須核對 project path，或指定測試專用 log。

## 尚待處理

### 主要架構工作：執行期世界整合

`WorldMap.Initialize()` 仍未把結果組成格子並填入 `_map`；`IWorldMap` 是空介面。
它仍直接 `new` 未配置的 ScriptableObject 參數，溫度與魔力也仍使用一般 WeatherGenerator。
可用的完整流程目前在 WorldMapEditor。

使用者在上一輪明確將這項排除，因此本輪沒有修改 `WorldMap.cs`。
這是獨立的後續工作，並非永久禁止修改；未來應依當次任務範圍規劃共用生成流程與資料模型。

### 其他仍存在的問題

- `TileUnit.Population` 尚未與 `TileDataUnit.Populations` 同步，現在讀前者仍會得到零。
- Biome 預覽仍沿用地形高度色階，未依 `BiomeData.Color` 顯示生態。
- 切換預覽會建立新 Texture2D，舊材質的釋放尚未整理。
- 大型地圖的排序、部分整張圖迴圈、預覽與序列化仍可能阻塞主執行緒；尚未量測與優化。
- 缺少正式遊戲場景、回合與角色流程，以及可執行遊戲的建置驗證。

後續建議先完成執行期世界整合與人口資料來源統一，再做最小可玩循環。
預覽與效能工作可依當次需求安排；不要把上述待辦誤認為已修復。
