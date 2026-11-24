# TileWorldFantasy 專案現況

更新日期：2026-11-25（Asia/Taipei）

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
| `Assets/Script/SaveLoad/SaveData.cs` | 地圖資料類型與存檔資料驗證 |
| `Assets/Script/SaveLoad/WorldMapLibrary.cs` | 多地圖目錄、版本 2 格式、同檔設定快照、原子寫入與備份 |
| `Assets/Script/Meta/Editor/WorldSettingsSnapshot.cs` | 設定值與引用圖的快照、獨立副本還原與驗證 |
| `Assets/Script/Meta/Editor/WorldMapEditor.Persistence.cs` | WorldMapEditor 的存檔列表、命名、讀寫與設定套用（partial class） |
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

- 新存檔：`Application.persistentDataPath/Maps/<唯一ID>.json`，現行格式版本為 `2`。
- 每個 JSON 同時包含地圖、seed、尺寸與完整生成設定快照，不拆成設定 sidecar。
- 驗證地圖與設定後先寫 `.tmp` 並 flush，完成後替換主檔；每份地圖各自保留前一版 `.bak`。
- 主檔的地圖或設定無法讀取時嘗試讀取備份，介面會說明備份回復。
- 存檔失敗會回傳失敗與原因，不再顯示假的成功訊息。
- 讀檔失敗保留編輯器目前已載入的地圖。
- 按使用者已確認的範圍，移除舊 JSON 與 PlayerPrefs 的讀取／遷移；舊 `world-map.json`、
  `SAVE_KEY` 不再使用，也不會自動刪除。
- `TileDataUnit.Populations` 是目前生成與存檔使用的人口陣列。

### 2026-11-23：外海帶與海岸過渡（本次工作目錄）

- 地形改用到最近邊界的格數距離，固定外海帶後平滑過渡到原始高度；移除全圖徑向衰減。
- 海岸噪聲只增加向內陸的過渡寬度，不會侵入固定外海帶；超出最大過渡範圍不套用遮罩。
- `TerrainParameter` 與 WorldMapEditor 的 Parameter 畫面皆可編輯海平面、海底高度、
  外海帶格數、過渡格數、噪聲幅度與波長；使用 SerializedObject 編輯參數資產。
- 預設海平面 0.23、海底 0.05、外海 3 格、過渡 20 格、噪聲最多加寬 8 格、波長 40 格。
  小地圖不自動縮放寬度；可能全部為海，Editor 會提示。
- 海平面目前是海底高度驗證基準，尚未統一生態高度分段、預覽色階與河流終點參數；
  修改水位時應一併檢查這些設定。此變更沒有加入新的海洋生態分類。
- 局部隆起後先限制高度，再以 Mathf.Pow 重塑高度，避免負底數的小數次方造成無效值。
- 舊 `SURROUND_DOWN_*` 欄位隱藏並保留序列化資料，不再參與計算；未覆寫既有參數資產或存檔。
  舊 seed 重新生成會得到不同地圖。
- 新增外海邊界、長方形與極小地圖、內陸保留、海岸控制、參數驗證、SerializedProperty 編輯與
  大幅隆起搭配小數次方的 EditMode 測試。
- 本次於獨立暫存專案使用 Unity **6000.0.76f1** 執行 `WorldGenerationRegressionTests`：
  **45 項全部通過，失敗 0、略過 0**（原有 28 項，加上本次 17 項）。
  原專案被另一個 Unity 實例占用，故未在原專案啟動第二個 Editor。
  結果與日誌在忽略的 `Logs/EditMode-coast-results.xml`、`Logs/EditMode-coast-copy.log`。
  未實際操作介面，也未驗證完整打包。

### 2026-11-23：分層地形與中文提示（本次工作目錄）

- 已接入大陸、山區分布、脊狀山脈、地表細節四層，預設開啟 `USE_LAYERED_TERRAIN`。
  每層可設定開關、格數波長、層數、頻率倍數、振幅保留比例；山脈高度、尖銳程度與平原／山區細節幅度分開控制。
- 大陸使用海平面與海陸門檻控制高度，門檻不是精確陸地百分比；山區噪聲決定山脈與粗糙程度的分布。
  兩軸使用相同格數波長，不會因矩形尺寸拉伸。分層模式不再掃描舊版局部隆起點。
- 舊版可透過模式開關使用；原有設定值與引用保留，未覆寫參數資產或存檔。
  新模式會改變同 seed 的新生成結果，也會因少了舊版隨機點流程而改變後續各生成器的隨機取樣序列。
- 頻率與權重每次生成預先計算；舊版隆起重疊時每格共用噪聲，固定外海跳過地形噪聲，
  深處內陸跳過海岸噪聲；海床不計算山區、山脈與細節。Coroutine 仍是主執行緒分段執行。
- 地形參數（包含原有外海設定）滑鼠提示改為繁體中文；WorldMapEditor 的「地形分層與外海設定」
  可直接編輯所有地形欄位及展開的巢狀噪聲設定，Parameter 區域可捲動，SerializedObject 保留 Undo。
- 海平面現在也參與大陸層的高度計算；生態高度分段、色階與河流終點仍各自設定，尚未統一。
- 本次獨立暫存副本 Unity **6000.0.76f1** 的 `WorldGenerationRegressionTests`：
  **62 項通過，失敗 0、略過 0**。包含矩形／極小分層地圖、四層控制、海陸／山區門檻、
  固定格數尺度、巢狀序列化、中文 tooltip、預先計算噪聲權重與舊版隆起重疊等價性。
  結果在忽略的 `Logs/EditMode-layered-results.xml`、`Logs/EditMode-layered.log`。
- 生成效能於相同機器的 batch Editor 測量，seed 9876、地形偏移 (1234, 5678)，每組暖機一次再取三次平均；
  使用現有資產的複製品切換模式，連續 drain coroutine，不包含 Editor 更新等待、預覽貼圖或存檔：

  | 尺寸 | 優化後舊版地形 | 新分層地形 | 優化後舊版完整世界 | 新分層完整世界 |
  | --- | ---: | ---: | ---: | ---: |
  | 256² | 54.58 ms | 30.59 ms | 228.54 ms | 207.90 ms |
  | 512² | 215.07 ms | 122.87 ms | 894.81 ms | 952.44 ms |
  | 1024² | 843.31 ms | 449.99 ms | 3656.32 ms | 3272.49 ms |

  先前未優化版本的 1024² 地形約 985.46 ms。各模式地形不同，也會改變後續河流成本；
  此表是指定 seed 的基準，不代表所有參數或地圖都會變快。
  日誌在忽略的 `Logs/Terrain-layered-performance.log`；已檢查輸出的 512² 彩色高度預覽
  `Logs/Terrain-layered-preview.png`。未實際操作 Editor 介面，未驗證完整打包。

### 2026-11-23：濕度噪聲越界修正（本次工作目錄）

- 使用既有濕度資產重現 128×128、seed 32 的失敗：座標 (108, 107)，
  濕度偏移 (3606, 8693)，原噪聲結果為 -0.003733337，導致生態分類的 `biome sample` 範圍檢查失敗。
- Unity 的 PerlinNoise 可能略超出 0～1；共用 `CountRecursivePerlinNoise` 原先沒有在疊加後限制輸出。
  現在輸出使用 Clamp01，與分層地形的正常化範圍一致；未放寬生態分類驗證，未覆寫參數資產與存檔。
- 新增直接噪聲、濕度圖與 seed 32 的正方形／矩形完整世界重現案例；修正前新增四項皆失敗。
  修正後在獨立副本 Unity **6000.0.76f1** 執行全部 `WorldGenerationRegressionTests`：
  **66 項通過，失敗 0、略過 0**。日誌與結果位於忽略的 `Logs/EditMode-humidity.log`、
  `Logs/EditMode-humidity-results.xml`；未實際操作原專案介面或驗證打包。

### 2026-11-23：恢復小圖預覽與解析度獨立地形（本次工作目錄）

- 分層地形改為固定的 0～WORLD_SCALE 世界範圍，透過 x/(width-1)、y/(height-1) 取樣；
  Width／Height 只改解析度，不再擴大世界。四層與海岸噪聲皆使用相同的固定世界座標。
- 新增可在 Editor 調整的 `WORLD_SCALE`，預設 256。現有波長與寬度數字保留，
  除以 WORLD_SCALE 即為單邊比例；未自動覆寫參數資產或既有地圖存檔。
- 外海帶、過渡與海岸噪聲也採相對世界尺度，最外圈固定為海；寬度不再扣掉一格，
  不因低解析度而把外海比例擴大。尺寸為 1 的軸映射到邊界，整張為海，避免除以零。
- 相同 seed、參數與世界尺度下，小圖與大圖在對應世界座標的高度相同；低解析度仍可能漏掉細小島嶼。
  改變長寬比會拉伸同一個正規化世界，並非擴增範圍。舊版基礎噪聲保留原 x/width、y/height 取樣，
  外海遮罩則共用新的相對尺度；河流與人口仍依格網生成，不保證跨解析度一一對應。
- 已更新中文 tooltip、Editor 說明與海岸比例提示；增加輸出尺寸不再作為修正過寬海岸的建議。
- 本次獨立副本 Unity **6000.0.76f1** 執行 `WorldGenerationRegressionTests`：
  **71 項通過，失敗 0、略過 0**。新增正方形與兩種矩形方向的跨解析度全圖對應比較、
  長寬比變更後的對應座標比較，以及世界尺度非法數值檢查；海岸與四層細節皆涵蓋。
  結果為忽略的 `Logs/EditMode-resolution-results.xml`、`Logs/EditMode-resolution.log`。
- 已輸出並檢查同 seed、同參數的 128²／512² 並排地形預覽，左側小圖放大、右側大圖：
  `Logs/Terrain-resolution-comparison.png`。大陸、海岸與山區位置一致，高解析度可見較細山稜。
  未實際操作原專案介面或驗證打包。

### 2026-11-23：地圖預覽縮放與手動尺寸（本次工作目錄）

- WorldMapEditor 的 View Map 不再固定顯示為 200×200；可用縮放滑桿（1～1600%）或輸入顯示寬／高，
  寬高依原地圖比例連動，按 Enter 套用。僅調整顯示，不重生地圖、不改資料解析度。
- 提供「符合視窗」、「1:1」、「清晰格子」控制，放大超出可視範圍時使用水平／垂直捲軸查看。
  預覽使用剩餘視窗區域；控制提示皆為繁體中文。
- 替換預覽、重設與關閉視窗時釋放舊 Texture2D；縮放不建立新貼圖。
- 獨立副本 Unity **6000.0.76f1** 編譯並執行既有 `WorldGenerationRegressionTests`：
  **71 項通過，失敗 0、略過 0**。結果為忽略的 `Logs/EditMode-preview-results.xml`、
  `Logs/EditMode-preview.log`。未新增針對簡單顯示控制的單元測試。
- 使用者要求自行檢查介面，因此未實際操作新控制；未驗證完整打包。

### 2026-11-25：回溯核對與補回多存檔（本次工作目錄）

- 開始時 HEAD 為 `6741f9c`（editor enlarge），工作目錄乾淨。地形四層、固定世界尺度、
  中文提示、濕度噪聲修正與預覽縮放均已存在；多存檔與設定快照當時不在檔案中。
  先以全新獨立副本重新驗證，原版本 **71 項 EditMode 測試通過**。
- 本次依使用者要求補回命名另存、覆寫目前存檔、Load 列表與開啟存檔資料夾。
  同名另存使用不同 GUID；載入後重生仍可明確選擇覆寫同一存檔。
- 同一份 JSON 內嵌所有生成參數、生態表與 BiomeData、預覽色階，以及巢狀／共用引用圖；
  不依賴資產路徑或跨 Session 無效的 instance ID。快照 schema 版本 1、存檔 envelope 版本 2。
- 生成開始時凍結設定並用獨立副本生成；完成後保存該次快照，避免生成後的參數修改與地圖錯配。
  載入也還原可編輯的獨立副本，不覆寫原參數資產；取消／失敗／關閉時釋放生成副本。
- 列表只讀小型 metadata 標頭，不需載入大型格子陣列；沒有額外設定或索引檔。
  壞主檔／壞設定可回復各自的備份；僅剩備份時仍可列出並讀取。載入失敗保留目前地圖與參數。
- 已移除舊存檔相容程式與遷移測試，保留舊格式拒絕、壞資料與備份測試。
- 補回後於全新獨立副本使用 Unity **6000.0.76f1** 執行 `WorldGenerationRegressionTests`：
  **77 項通過，失敗 0、略過 0**。涵蓋設定與共用引用往返、同名多檔、各自備份、
  壞設定／壞主檔復原、僅備份列表、非法 ID、寫入失敗、舊格式拒絕、載入後重新生成、
  原參數資產保護、載入失敗保留現況與取消生成的副本釋放。
  結果為忽略的 `Logs/EditMode-save-restore-results.xml`、`Logs/EditMode-save-restore.log`。
- 地形參數資產的 SHA256 在本次修改前後一致，未還原或改寫使用者的參數值。
  未實際操作新增儲存介面，未驗證完整打包；依使用者偏好由使用者自行操作介面。

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
- 大型地圖的排序、部分整張圖迴圈、預覽與序列化仍可能阻塞主執行緒；尚未量測與優化。
- 缺少正式遊戲場景、回合與角色流程，以及可執行遊戲的建置驗證。

後續建議先完成執行期世界整合與人口資料來源統一，再做最小可玩循環。
預覽與效能工作可依當次需求安排；不要把上述待辦誤認為已修復。
