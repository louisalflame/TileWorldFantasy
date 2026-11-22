using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "World/ParamTerrain", order = 11)]
public class TerrainParameter : ScriptableObject
{
    [Tooltip("啟用大陸、山區、山脈與細節分層。關閉時使用舊版 Perlin 與局部隆起；外海規則仍生效。")]
    public bool USE_LAYERED_TERRAIN = true;

    [Min(1f), Tooltip("固定世界的基準尺度，預設 256。波長與海岸寬度除以此值就是占整張地圖的比例，例如寬度 20 約占 7.8%。保持此值與 seed 不變，只改地圖長寬即可提高取樣解析度。")]
    public float WORLD_SCALE = 256f;

    [Header("分層地形")]
    [Tooltip("大陸輪廓噪聲。波長越大，陸塊越大；關閉時整片使用下方的基準陸地高度。")]
    public TerrainNoiseLayer CONTINENTS = new TerrainNoiseLayer(160f, 2);
    [Range(0f, 1f), Tooltip("大陸噪聲的海陸分界。降低會增加陸地，提高會增加海洋；不保證精確陸地百分比。")]
    public float LAND_THRESHOLD = 0.45f;
    [Range(0f, 1f), Tooltip("大陸高度相對海平面的變化幅度。數值越大，海盆越深、內陸越高。")]
    public float CONTINENT_RELIEF = 0.8f;
    [Range(0f, 1f), Tooltip("關閉大陸層時的基準高度。其他層與外海過渡仍可套用。")]
    public float BASE_LAND_HEIGHT = 0.35f;
    [Min(0.001f), Tooltip("海平面以上的緩衝高度；山脈與細節在此區間逐漸出現，避免直接抬高海岸或海床。")]
    public float LAND_BLEND_HEIGHT = 0.08f;

    [Tooltip("山區分布噪聲，決定哪些陸地可以出現山脈。關閉時所有陸地都允許出現山脈。")]
    public TerrainNoiseLayer MOUNTAIN_REGIONS = new TerrainNoiseLayer(100f, 2);
    [Range(0f, 1f), Tooltip("山區分布門檻。提高會減少山區，降低會增加山區。")]
    public float MOUNTAIN_THRESHOLD = 0.5f;
    [Min(0.001f), Tooltip("山區門檻之上的平滑過渡範圍。越大，平原到山區的變化越緩和。")]
    public float MOUNTAIN_BLEND = 0.15f;
    [Tooltip("使用脊狀噪聲產生山稜。關閉後不產生山脈高度。")]
    public TerrainNoiseLayer RIDGES = new TerrainNoiseLayer(32f, 4);
    [Range(0f, 1f), Tooltip("山脈最高增加的高度，受到山區分布與海岸緩衝限制。設為零會跳過山脈取樣。")]
    public float MOUNTAIN_HEIGHT = 0.5f;
    [Min(0.01f), Tooltip("山稜的尖銳程度。越大，山稜越窄；越小，山勢越寬。")]
    public float RIDGE_SHARPNESS = 3f;
    [Tooltip("地表細節噪聲；平原與山區共用取樣，但使用不同的起伏幅度。")]
    public TerrainNoiseLayer DETAILS = new TerrainNoiseLayer(12f, 2);
    [Range(0f, 1f), Tooltip("平原細節的最大正負起伏。設為零可讓平原平坦。")]
    public float PLAIN_DETAIL_HEIGHT = 0.015f;
    [Range(0f, 1f), Tooltip("山區細節的最大正負起伏。與平原幅度之間依山區分布平滑混合。")]
    public float MOUNTAIN_DETAIL_HEIGHT = 0.06f;

    [Header("舊版地形（關閉分層時使用）")]
    [Tooltip("舊版基礎噪聲在整張地圖上的頻率。越大，地形越細碎。")]
    public float SCALE;
    [Tooltip("舊版低地高度曲線的次方，必須大於零。")]
    public float LOW_GROUND_FACTOR;
    [Tooltip("舊版高地高度曲線的次方，必須大於零。")]
    public float HIGH_MOUNTAIN_FACTOR;

    [Tooltip("舊版 Perlin 疊加層數。越多越細緻，也會增加生成成本。")]
    public int FREQ_COUNT_TIMES;
    [Tooltip("舊版每層頻率的倍數，振幅取頻率的倒數。")]
    public int FREQ_GROW_FACTOR;

    // Retain serialized legacy values without applying the old radial falloff.
    [HideInInspector] public float SURROUND_DOWN_OFFSET;
    [HideInInspector] public float SURROUND_DOWN_SPEED;

    [Header("外海與海岸")]
    [Range(0f, 1f), Tooltip("分層地形的海平面，也是海底高度的上限。生態高度分段、預覽色階與河流終點仍需另外設定。")]
    public float SEA_LEVEL = 0.23f;
    [Range(0f, 1f), Tooltip("固定外海帶的高度，必須嚴格低於海平面。")]
    public float OCEAN_FLOOR_HEIGHT = 0.05f;
    [Min(1), Tooltip("固定外海帶的基準寬度，占地圖單邊的比例為此值／WORLD SCALE。改變解析度會等比例調整實際格數；最外圈一定是海。")]
    public int OCEAN_BORDER_WIDTH = 3;
    [Min(0.01f), Tooltip("固定外海帶之後的平滑過渡基準寬度，占單邊比例為此值／WORLD SCALE；不隨輸出解析度改變。")]
    public float COAST_TRANSITION_WIDTH = 20f;
    [Min(0f), Tooltip("海岸噪聲最多向內陸增加的基準寬度，占單邊比例為此值／WORLD SCALE。設為零會關閉海岸噪聲。")]
    public float COAST_NOISE_AMPLITUDE = 8f;
    [Min(0.01f), Tooltip("海岸噪聲的基準波長，占單邊比例為此值／WORLD SCALE。越大，海岸彎曲越寬廣；改變解析度不會改變輪廓。")]
    public float COAST_NOISE_SCALE = 40f;

    [Tooltip("舊版局部隆起參數。分層模式不使用，也不需要指定。")]
    public RandomPointParameter RANDOM_POINT_GEN_PARA;
}

[System.Serializable]
public class TerrainNoiseLayer
{
    [Tooltip("開啟此層；關閉後跳過此層的噪聲計算。")]
    public bool ENABLED = true;
    [Min(0.01f), Tooltip("基礎噪聲波長，占地圖單邊比例為此值／WORLD SCALE。保持此值不變，提高地圖長寬只會增加取樣細節，不會增加世界範圍。")]
    public float WAVELENGTH;
    [Range(1, 12), Tooltip("疊加層數。每增加一層，每個取樣點就多計算一次 Perlin；細節層數不宜超過格子解析度。")]
    public int OCTAVES;
    [Range(1f, 4f), Tooltip("每層頻率增加的倍數。越大，高頻細節越密集。")]
    public float LACUNARITY = 2f;
    [Range(0f, 1f), Tooltip("每層振幅保留的比例。越小，高頻細節的影響越弱。")]
    public float PERSISTENCE = 0.5f;

    public TerrainNoiseLayer(float wavelength, int octaves)
    {
        WAVELENGTH = wavelength;
        OCTAVES = octaves;
    }
}
