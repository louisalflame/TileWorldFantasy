using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Biome
{
    /*
     *   濕潤  =寒帶=　　  =亞寒帶=　  =溫帶=　　  =亞熱帶=　　  =熱帶=
     *   平原  [寒帶草原]  [針葉林　]  [混合林　]  [闊葉林　　]  [熱帶雨林　]
     *   土坡  [寒帶草原]  [針葉林　]  [混合林　]  [闊葉林　　]  [熱帶雨林　]
     *   丘陵  [寒帶草原]  [針葉林　]  [混合林　]  [闊葉林　　]  [熱帶雨林　]
     *   山地  [冰川　　]  [山地森林]  [山地森林]  [山地森林　]  [山地森林　]
     *   高峰  [冰川　　]  [山地森林]  [山地森林]  [山地森林　]  [山地森林　]
     *   
     *   半濕  =寒帶=　　  =亞寒帶=　  =溫帶=　　  =亞熱帶=　　  =熱帶=
     *   平原  [苔原　　]  [灌木林　]  [落葉林　]  [季風闊葉林]  [熱帶季風林]
     *   土坡  [苔原　　]  [灌木林　]  [落葉林　]  [季風闊葉林]  [熱帶季風林]
     *   丘陵  [苔原　　]  [灌木林　]  [落葉林　]  [季風闊葉林]  [熱帶季風林]
     *   山地  [冰川　　]  [溫帶高原]  [溫帶高原]  [亞熱帶高原]  [熱帶高原　]
     *   高峰  [冰川　　]  [溫帶高原]  [溫帶高原]  [亞熱帶高原]  [熱帶高原　]
     *   
     *   半乾  =寒帶=　　  =亞寒帶=　  =溫帶=　　  =亞熱帶=　　  =熱帶=
     *   平原  [苔原　　]  [溫帶草原]  [溫帶疏林]  [季風疏林　]  [熱帶莽原　]
     *   土坡  [苔原　　]  [溫帶草原]  [溫帶疏林]  [季風疏林　]  [熱帶莽原　]
     *   丘陵  [苔原　　]  [溫帶草原]  [溫帶疏林]  [季風疏林　]  [熱帶莽原　]
     *   山地  [冰川　　]  [溫帶高原]  [溫帶高原]  [亞熱帶高原]  [熱帶高原　]
     *   高峰  [冰川　　]  [溫帶高原]  [溫帶高原]  [亞熱帶高原]  [熱帶高原　]
     *   
     *   乾燥  =寒帶=　　  =亞寒帶=　  =溫帶=　　  =亞熱帶=　　  =熱帶=
     *   平原  [寒帶荒漠]  [溫帶礫漠]  [溫帶沙漠]  [亞熱帶沙漠]  [熱帶沙漠　]
     *   土坡  [寒帶荒漠]  [溫帶礫漠]  [溫帶沙漠]  [亞熱帶沙漠]  [熱帶沙漠　]
     *   丘陵  [寒帶荒漠]  [溫帶礫漠]  [溫帶沙漠]  [亞熱帶沙漠]  [熱帶沙漠　]
     *   山地  [凍岩　　]  [荒岩　　]  [荒岩　　]  [焦岩　　　]  [焦岩　　　]
     *   高峰  [凍岩　　]  [荒岩　　]  [荒岩　　]  [焦岩　　　]  [焦岩　　　]
     */

    COLD_GRASSLAND,//寒帶草原
    TUNDRA,//苔原
    COLD_DESERT,//寒帶荒漠

    TAIGA,//針葉林
    SHRUBLAND,//灌木林
    TEMPERATE_GRASSLAND,//溫帶草原
    TEMPERATE_GRAVEL,//溫帶礫漠

    MIXED_FOREST,//混合林
    DECIDUOUS_FOREST,//落葉林
    TEMPERATE_SAVANNA,//溫帶疏林
    TEMPERATE_DESERT,//溫帶沙漠

    BROAD_LEAVED_FOREST,//闊葉林
    SEASONAL_BROAD_LEAVEED_FOREST,//季風闊葉林
    SEASONAL_SAVANNA,//季風疏林
    SUBTROPICAL_DESERT,//亞熱帶沙漠

    TROPICAL_RAIN_FOREST,//熱帶雨林
    TROPICAL_SEASONAL_FOREST,//熱帶季風林
    TROPICAL_SAVANNA,//熱帶莽原
    TROPICAL_DESERT,//熱帶沙漠

    GLACIER,//冰川
    COLD_BARE_ROCK,//凍岩

    MOUNTAIN_FOREST,//山地森林
    TEMPERATE_PLATEAU,//溫帶高原
    TEMPERATE_BARE_ROCK,//荒岩

    SUBTROPICAL_PLATEAU,//亞熱帶高原
    TROPICAL_PLATEAU,//熱帶高原
    TROPICAL_BARE_ROCK,//焦岩

}
