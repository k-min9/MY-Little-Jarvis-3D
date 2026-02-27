using System.Collections.Generic;
using System;

// 캐릭터 이름 → actor ID 매핑 데이터
public static class STTDataActor
{
    // 자동 생성된 C# Dictionary 데이터
    private static Dictionary<string, string> actorMap = new Dictionary<string, string>()
    {
        // arona
        {"アロナ", "arona"},
        {"arona", "arona"},
        {"Arona", "arona"},
        {"아로나", "arona"},

        // plana
        {"プラナ", "plana"},
        {"plana", "plana"},
        {"Plana", "plana"},
        {"플라나", "plana"},

        // airi
        {"Airi", "airi"},
        {"Airi (Band)", "airi"},
        {"airi", "airi"},
        {"airi (band)", "airi"},
        {"アイリ", "airi"},
        {"アイリ（バンド）", "airi"},
        {"아이리", "airi"},
        {"아이리(밴드)", "airi"},

        // akane
        {"Akane", "akane"},
        {"Akane (Bunny)", "akane"},
        {"akane", "akane"},
        {"akane (bunny)", "akane"},
        {"アカネ", "akane"},
        {"アカネ（バニーガール）", "akane"},
        {"아카네", "akane"},
        {"아카네(바니걸)", "akane"},

        // akari
        {"Akari", "akari"},
        {"Akari (New Year)", "akari"},
        {"akari", "akari"},
        {"akari (new year)", "akari"},
        {"アカリ", "akari"},
        {"アカリ（正月）", "akari"},
        {"아카리", "akari"},
        {"아카리(새해)", "akari"},

        // aoba
        {"Aoba", "aoba"},
        {"aoba", "aoba"},
        {"アオバ", "aoba"},
        {"아오바", "aoba"},

        // aoi
        {"Aoi", "aoi"},
        {"aoi", "aoi"},
        {"アオイ", "aoi"},
        {"아오이", "aoi"},

        // ako
        {"Ako", "ako"},
        {"Ako (Dress)", "ako"},
        {"ako", "ako"},
        {"ako (dress)", "ako"},
        {"アコ", "ako"},
        {"アコ（ドレス）", "ako"},
        {"아코", "ako"},
        {"아코(드레스)", "ako"},

        // aris
        {"Aris", "aris"},
        {"Aris (Maid)", "aris"},
        {"aris", "aris"},
        {"aris (maid)", "aris"},
        {"アリス", "aris"},
        {"アリス（メイド）", "aris"},
        {"아리스", "aris"},
        {"아리스(메이드)", "aris"},

        // aru
        {"Aru", "aru"},
        {"Aru (Dress)", "aru"},
        {"Aru (New Year)", "aru"},
        {"aru", "aru"},
        {"aru (dress)", "aru"},
        {"aru (new year)", "aru"},
        {"アル", "aru"},
        {"アル（ドレス）", "aru"},
        {"アル（正月）", "aru"},
        {"아루", "aru"},
        {"아루(드레스)", "aru"},
        {"아루(새해)", "aru"},

        // asuna
        {"Asuna", "asuna"},
        {"Asuna (Bunny)", "asuna"},
        {"asuna", "asuna"},
        {"asuna (bunny)", "asuna"},
        {"アスナ", "asuna"},
        {"アスナ（バニーガール）", "asuna"},
        {"아스나", "asuna"},
        {"아스나(바니걸)", "asuna"},

        // atsuko
        {"Atsuko", "atsuko"},
        {"Atsuko (Swimsuit)", "atsuko"},
        {"atsuko", "atsuko"},
        {"atsuko (swimsuit)", "atsuko"},
        {"アツコ", "atsuko"},
        {"アツコ（水着）", "atsuko"},
        {"아츠코", "atsuko"},
        {"아츠코(수영복)", "atsuko"},

        // ayane
        {"Ayane", "ayane"},
        {"Ayane (Swimsuit)", "ayane"},
        {"ayane", "ayane"},
        {"ayane (swimsuit)", "ayane"},
        {"アヤネ", "ayane"},
        {"アヤネ（水着）", "ayane"},
        {"아야네", "ayane"},
        {"아야네(수영복)", "ayane"},

        // ayumu
        {"Ayumu", "ayumu"},
        {"ayumu", "ayumu"},
        {"アユム", "ayumu"},
        {"아유무", "ayumu"},

        // azusa
        {"Azusa", "azusa"},
        {"Azusa (Swimsuit)", "azusa"},
        {"azusa", "azusa"},
        {"azusa (swimsuit)", "azusa"},
        {"アズサ", "azusa"},
        {"アズサ（水着）", "azusa"},
        {"아즈사", "azusa"},
        {"아즈사(수영복)", "azusa"},

        // cherino
        {"Cherino", "cherino"},
        {"Cherino (Hot Spring)", "cherino"},
        {"cherino", "cherino"},
        {"cherino (hot spring)", "cherino"},
        {"チェリノ", "cherino"},
        {"チェリノ（温泉）", "cherino"},
        {"체리노", "cherino"},
        {"체리노(온천)", "cherino"},

        // chihiro
        {"Chihiro", "chihiro"},
        {"chihiro", "chihiro"},
        {"チヒロ", "chihiro"},
        {"치히로", "chihiro"},

        // chinatsu
        {"Chinatsu", "chinatsu"},
        {"Chinatsu (Hot Spring)", "chinatsu"},
        {"chinatsu", "chinatsu"},
        {"chinatsu (hot spring)", "chinatsu"},
        {"チナツ", "chinatsu"},
        {"チナツ（温泉）", "chinatsu"},
        {"치나츠", "chinatsu"},
        {"치나츠(온천)", "chinatsu"},

        // chise
        {"Chise", "chise"},
        {"Chise (Swimsuit)", "chise"},
        {"chise", "chise"},
        {"chise (swimsuit)", "chise"},
        {"チセ", "chise"},
        {"チセ（水着）", "chise"},
        {"치세", "chise"},
        {"치세(수영복)", "chise"},

        // eimi
        {"Eimi", "eimi"},
        {"Eimi (Swimsuit)", "eimi"},
        {"eimi", "eimi"},
        {"eimi (swimsuit)", "eimi"},
        {"エイミ", "eimi"},
        {"エイミ（水着）", "eimi"},
        {"에이미", "eimi"},
        {"에이미(수영복)", "eimi"},

        // fubuki
        {"Fubuki", "fubuki"},
        {"Fubuki (Swimsuit)", "fubuki"},
        {"fubuki", "fubuki"},
        {"fubuki (swimsuit)", "fubuki"},
        {"フブキ", "fubuki"},
        {"フブキ（水着）", "fubuki"},
        {"후부키", "fubuki"},
        {"후부키(수영복)", "fubuki"},

        // fuuka
        {"Fuuka", "fuuka"},
        {"Fuuka (New Year)", "fuuka"},
        {"fuuka", "fuuka"},
        {"fuuka (new year)", "fuuka"},
        {"フウカ", "fuuka"},
        {"フウカ（正月）", "fuuka"},
        {"후우카", "fuuka"},
        {"후우카(새해)", "fuuka"},

        // hanae
        {"Hanae", "hanae"},
        {"Hanae (Christmas)", "hanae"},
        {"hanae", "hanae"},
        {"hanae (christmas)", "hanae"},
        {"ハナエ", "hanae"},
        {"ハナエ（クリスマス）", "hanae"},
        {"하나에", "hanae"},
        {"하나에(크리스마스)", "hanae"},

        // hanako
        {"Hanako", "hanako"},
        {"Hanako (Swimsuit)", "hanako"},
        {"hanako", "hanako"},
        {"hanako (swimsuit)", "hanako"},
        {"ハナコ", "hanako"},
        {"ハナコ（水着）", "hanako"},
        {"하나코", "hanako"},
        {"하나코(수영복)", "hanako"},

        // hare
        {"Hare", "hare"},
        {"Hare (Camp)", "hare"},
        {"hare", "hare"},
        {"hare (camp)", "hare"},
        {"ハレ", "hare"},
        {"ハレ（キャンプ）", "hare"},
        {"하레", "hare"},
        {"하레(캠핑)", "hare"},

        // haruka
        {"Haruka", "haruka"},
        {"Haruka (New Year)", "haruka"},
        {"haruka", "haruka"},
        {"haruka (new year)", "haruka"},
        {"ハルカ", "haruka"},
        {"ハルカ（正月）", "haruka"},
        {"하루카", "haruka"},
        {"하루카(새해)", "haruka"},

        // haruna
        {"Haruna", "haruna"},
        {"Haruna (New Year)", "haruna"},
        {"Haruna (Track)", "haruna"},
        {"haruna", "haruna"},
        {"haruna (new year)", "haruna"},
        {"haruna (track)", "haruna"},
        {"ハルナ", "haruna"},
        {"ハルナ（体操服）", "haruna"},
        {"ハルナ（正月）", "haruna"},
        {"하루나", "haruna"},
        {"하루나(새해)", "haruna"},
        {"하루나(체육복)", "haruna"},

        // hasumi
        {"Hasumi", "hasumi"},
        {"Hasumi (Track)", "hasumi"},
        {"hasumi", "hasumi"},
        {"hasumi (track)", "hasumi"},
        {"ハスミ", "hasumi"},
        {"ハスミ（体操服）", "hasumi"},
        {"하스미", "hasumi"},
        {"하스미(체육복)", "hasumi"},

        // hatsune
        // {"Hatsune Miku", "hatsune"},
        // {"hatsune miku", "hatsune"},
        // {"初音ミク", "hatsune"},
        // {"하츠네", "hatsune"},
        // {"하츠네 미쿠", "hatsune"},

        // hibiki
        {"Hibiki", "hibiki"},
        {"Hibiki (Cheer Squad)", "hibiki"},
        {"hibiki", "hibiki"},
        {"hibiki (cheer squad)", "hibiki"},
        {"ヒビキ", "hibiki"},
        {"ヒビキ（応援団）", "hibiki"},
        {"히비키", "hibiki"},
        {"히비키(응원단)", "hibiki"},

        // hifumi
        {"Hifumi", "hifumi"},
        {"Hifumi (Swimsuit)", "hifumi"},
        {"hifumi", "hifumi"},
        {"hifumi (swimsuit)", "hifumi"},
        {"ヒフミ", "hifumi"},
        {"ヒフミ（水着）", "hifumi"},
        {"히후미", "hifumi"},
        {"히후미(수영복)", "hifumi"},

        // himari
        {"Himari", "himari"},
        {"himari", "himari"},
        {"ヒマリ", "himari"},
        {"히마리", "himari"},

        // hina
        {"Hina", "hina"},
        {"Hina (Dress)", "hina"},
        {"Hina (Swimsuit)", "hina"},
        {"hina", "hina"},
        {"hina (dress)", "hina"},
        {"hina (swimsuit)", "hina"},
        {"ヒナ", "hina"},
        {"ヒナ（ドレス）", "hina"},
        {"ヒナ（水着）", "hina"},
        {"히나", "hina"},
        {"히나(드레스)", "hina"},
        {"히나(수영복)", "hina"},

        // hinata
        {"Hinata", "hinata"},
        {"Hinata (Swimsuit)", "hinata"},
        {"hinata", "hinata"},
        {"hinata (swimsuit)", "hinata"},
        {"ヒナタ", "hinata"},
        {"ヒナタ（水着）", "hinata"},
        {"히나타", "hinata"},
        {"히나타(수영복)", "hinata"},

        // hiyori
        {"Hiyori", "hiyori"},
        {"Hiyori (Swimsuit)", "hiyori"},
        {"hiyori", "hiyori"},
        {"hiyori (swimsuit)", "hiyori"},
        {"ヒヨリ", "hiyori"},
        {"ヒヨリ（水着）", "hiyori"},
        {"히요리", "hiyori"},
        {"히요리(수영복)", "hiyori"},

        // hoshino
        {"Hoshino", "hoshino"},
        {"Hoshino (Battle)", "hoshino"},
        {"Hoshino (Swimsuit)", "hoshino"},
        {"hoshino", "hoshino"},
        {"hoshino (battle)", "hoshino"},
        {"hoshino (swimsuit)", "hoshino"},
        {"ホシノ", "hoshino"},
        {"ホシノ（水着）", "hoshino"},
        {"ホシノ（臨戦）", "hoshino"},
        {"호시노", "hoshino"},
        {"호시노(무장)", "hoshino"},
        {"호시노(수영복)", "hoshino"},

        // ibuki
        {"Ibuki", "ibuki"},
        {"ibuki", "ibuki"},
        {"イブキ", "ibuki"},
        {"이부키", "ibuki"},

        // ichika
        {"Ichika", "ichika"},
        {"ichika", "ichika"},
        {"イチカ", "ichika"},
        {"이치카", "ichika"},

        // iori
        {"Iori", "iori"},
        {"Iori (Swimsuit)", "iori"},
        {"iori", "iori"},
        {"iori (swimsuit)", "iori"},
        {"イオリ", "iori"},
        {"イオリ（水着）", "iori"},
        {"이오리", "iori"},
        {"이오리(수영복)", "iori"},

        // iroha
        {"Iroha", "iroha"},
        {"iroha", "iroha"},
        {"イロハ", "iroha"},
        {"이로하", "iroha"},

        // izumi
        {"Izumi", "izumi"},
        {"Izumi (Swimsuit)", "izumi"},
        {"izumi", "izumi"},
        {"izumi (swimsuit)", "izumi"},
        {"イズミ", "izumi"},
        {"イズミ（水着）", "izumi"},
        {"이즈미", "izumi"},
        {"이즈미(수영복)", "izumi"},

        // izuna
        {"Izuna", "izuna"},
        {"Izuna (Swimsuit)", "izuna"},
        {"izuna", "izuna"},
        {"izuna (swimsuit)", "izuna"},
        {"イズナ", "izuna"},
        {"イズナ（水着）", "izuna"},
        {"이즈나", "izuna"},
        {"이즈나(수영복)", "izuna"},

        // junko
        {"Junko", "junko"},
        {"Junko (New Year)", "junko"},
        {"junko", "junko"},
        {"junko (new year)", "junko"},
        {"ジュンコ", "junko"},
        {"ジュンコ（正月）", "junko"},
        {"준코", "junko"},
        {"준코(새해)", "junko"},

        // juri
        {"Juri", "juri"},
        {"juri", "juri"},
        {"ジュリ", "juri"},
        {"주리", "juri"},

        // kaede
        {"Kaede", "kaede"},
        {"kaede", "kaede"},
        {"カエデ", "kaede"},
        {"카에데", "kaede"},

        // kaho
        {"Kaho", "kaho"},
        {"kaho", "kaho"},
        {"カホ", "kaho"},
        {"카호", "kaho"},

        // kanna
        {"Kanna", "kanna"},
        {"Kanna (Swimsuit)", "kanna"},
        {"kanna", "kanna"},
        {"kanna (swimsuit)", "kanna"},
        {"カンナ", "kanna"},
        {"カンナ（水着）", "kanna"},
        {"칸나", "kanna"},
        {"칸나(수영복)", "kanna"},

        // karin
        {"Karin", "karin"},
        {"Karin (Bunny)", "karin"},
        {"karin", "karin"},
        {"karin (bunny)", "karin"},
        {"カリン", "karin"},
        {"カリン（バニーガール）", "karin"},
        {"카린", "karin"},
        {"카린(바니걸)", "karin"},

        // kasumi
        {"Kasumi", "kasumi"},
        {"kasumi", "kasumi"},
        {"カスミ", "kasumi"},
        {"카스미", "kasumi"},

        // kayoko
        {"Kayoko", "kayoko"},
        {"Kayoko (Dress)", "kayoko"},
        {"Kayoko (New Year)", "kayoko"},
        {"kayoko", "kayoko"},
        {"kayoko (dress)", "kayoko"},
        {"kayoko (new year)", "kayoko"},
        {"カヨコ", "kayoko"},
        {"カヨコ（ドレス）", "kayoko"},
        {"カヨコ（正月）", "kayoko"},
        {"카요코", "kayoko"},
        {"카요코(드레스)", "kayoko"},
        {"카요코(새해)", "kayoko"},

        // kazusa
        {"Kazusa", "kazusa"},
        {"Kazusa (Band)", "kazusa"},
        {"kazusa", "kazusa"},
        {"kazusa (band)", "kazusa"},
        {"カズサ", "kazusa"},
        {"カズサ（バンド）", "kazusa"},
        {"카즈사", "kazusa"},
        {"카즈사(밴드)", "kazusa"},

        // kei
        {"Kei", "kei"},
        {"kei", "kei"},
        {"ケイ", "kei"},
        {"케이", "kei"},

        // kikyou
        {"Kikyou", "kikyou"},
        {"kikyou", "kikyou"},
        {"キキョウ", "kikyou"},
        {"키쿄", "kikyou"},

        // kirara
        {"Kirara", "kirara"},
        {"kirara", "kirara"},
        {"キララ", "kirara"},
        {"키라라", "kirara"},

        // kirino
        {"Kirino", "kirino"},
        {"Kirino (Swimsuit)", "kirino"},
        {"kirino", "kirino"},
        {"kirino (swimsuit)", "kirino"},
        {"キリノ", "kirino"},
        {"キリノ（水着）", "kirino"},
        {"키리노", "kirino"},
        {"키리노(수영복)", "kirino"},

        // koharu
        {"Koharu", "koharu"},
        {"Koharu (Swimsuit)", "koharu"},
        {"koharu", "koharu"},
        {"koharu (swimsuit)", "koharu"},
        {"コハル", "koharu"},
        {"コハル（水着）", "koharu"},
        {"코하루", "koharu"},
        {"코하루(수영복)", "koharu"},

        // kokona
        {"Kokona", "kokona"},
        {"kokona", "kokona"},
        {"ココナ", "kokona"},
        {"코코나", "kokona"},

        // kotama
        {"Kotama", "kotama"},
        {"Kotama (Camp)", "kotama"},
        {"kotama", "kotama"},
        {"kotama (camp)", "kotama"},
        {"コタマ", "kotama"},
        {"コタマ（キャンプ）", "kotama"},
        {"코타마", "kotama"},
        {"코타마(캠핑)", "kotama"},

        // kotori
        {"Kotori", "kotori"},
        {"Kotori (Cheer Squad)", "kotori"},
        {"kotori", "kotori"},
        {"kotori (cheer squad)", "kotori"},
        {"コトリ", "kotori"},
        {"コトリ（応援団）", "kotori"},
        {"코토리", "kotori"},
        {"코토리(응원단)", "kotori"},

        // koyuki
        {"Koyuki", "koyuki"},
        {"koyuki", "koyuki"},
        {"コユキ", "koyuki"},
        {"코유키", "koyuki"},

        // maki
        {"Maki", "maki"},
        {"maki", "maki"},
        {"マキ", "maki"},
        {"마키", "maki"},

        // makoto
        {"Makoto", "makoto"},
        {"makoto", "makoto"},
        {"マコト", "makoto"},
        {"마코토", "makoto"},

        // mari
        {"Mari", "mari"},
        {"Mari (Track)", "mari"},
        {"mari", "mari"},
        {"mari (track)", "mari"},
        {"マリー", "mari"},
        {"マリー（体操服）", "mari"},
        {"마리", "mari"},
        {"마리(체육복)", "mari"},

        // marina
        {"Marina", "marina"},
        {"marina", "marina"},
        {"マリナ", "marina"},
        {"마리나", "marina"},

        // mashiro
        {"Mashiro", "mashiro"},
        {"Mashiro (Swimsuit)", "mashiro"},
        {"mashiro", "mashiro"},
        {"mashiro (swimsuit)", "mashiro"},
        {"マシロ", "mashiro"},
        {"マシロ（水着）", "mashiro"},
        {"마시로", "mashiro"},
        {"마시로(수영복)", "mashiro"},

        // megu
        {"Megu", "megu"},
        {"megu", "megu"},
        {"メグ", "megu"},
        {"메구", "megu"},

        // meru
        {"Meru", "meru"},
        {"meru", "meru"},
        {"メル", "meru"},
        {"메루", "meru"},

        // michiru
        {"Michiru", "michiru"},
        {"michiru", "michiru"},
        {"ミチル", "michiru"},
        {"미치루", "michiru"},

        // midori
        {"Midori", "midori"},
        {"Midori (Maid)", "midori"},
        {"midori", "midori"},
        {"midori (maid)", "midori"},
        {"ミドリ", "midori"},
        {"ミドリ（メイド）", "midori"},
        {"미도리", "midori"},
        {"미도리(메이드)", "midori"},

        // mika
        {"Mika", "mika"},
        {"mika", "mika"},
        {"ミカ", "mika"},
        {"미카", "mika"},

        // mimori
        {"Mimori", "mimori"},
        {"Mimori (Swimsuit)", "mimori"},
        {"mimori", "mimori"},
        {"mimori (swimsuit)", "mimori"},
        {"ミモリ", "mimori"},
        {"ミモリ（水着）", "mimori"},
        {"미모리", "mimori"},
        {"미모리(수영복)", "mimori"},

        // mina
        {"Mina", "mina"},
        {"mina", "mina"},
        {"ミナ", "mina"},
        {"미나", "mina"},

        // mine
        {"Mine", "mine"},
        {"mine", "mine"},
        {"ミネ", "mine"},
        {"미네", "mine"},

        // minori
        {"Minori", "minori"},
        {"minori", "minori"},
        {"ミノリ", "minori"},
        {"미노리", "minori"},

        // misaka
        // {"Misaka Mikoto", "misaka"},
        // {"misaka mikoto", "misaka"},
        // {"御坂美琴", "misaka"},
        // {"미사카", "misaka"},
        // {"미사카 미코토", "misaka"},

        // misaki
        {"Misaki", "misaki"},
        {"misaki", "misaki"},
        {"ミサキ", "misaki"},
        {"미사키", "misaki"},

        // miyako
        {"Miyako", "miyako"},
        {"Miyako (Swimsuit)", "miyako"},
        {"miyako", "miyako"},
        {"miyako (swimsuit)", "miyako"},
        {"ミヤコ", "miyako"},
        {"ミヤコ（水着）", "miyako"},
        {"미야코", "miyako"},
        {"미야코(수영복)", "miyako"},

        // miyu
        {"Miyu", "miyu"},
        {"Miyu (Swimsuit)", "miyu"},
        {"miyu", "miyu"},
        {"miyu (swimsuit)", "miyu"},
        {"ミユ", "miyu"},
        {"ミユ（水着）", "miyu"},
        {"미유", "miyu"},
        {"미유(수영복)", "miyu"},

        // moe
        {"Moe", "moe"},
        {"Moe (Swimsuit)", "moe"},
        {"moe", "moe"},
        {"moe (swimsuit)", "moe"},
        {"モエ", "moe"},
        {"モエ（水着）", "moe"},
        {"모에", "moe"},
        {"모에(수영복)", "moe"},

        // momiji
        {"Momiji", "momiji"},
        {"momiji", "momiji"},
        {"モミジ", "momiji"},
        {"모미지", "momiji"},

        // momoi
        {"Momoi", "momoi"},
        {"Momoi (Maid)", "momoi"},
        {"momoi", "momoi"},
        {"momoi (maid)", "momoi"},
        {"モモイ", "momoi"},
        {"モモイ（メイド）", "momoi"},
        {"모모이", "momoi"},
        {"모모이(메이드)", "momoi"},

        // momoka
        {"Momoka", "momoka"},
        {"momoka", "momoka"},
        {"モモカ", "momoka"},
        {"모모카", "momoka"},

        // mutsuki
        {"Mutsuki", "mutsuki"},
        {"Mutsuki (New Year)", "mutsuki"},
        {"mutsuki", "mutsuki"},
        {"mutsuki (new year)", "mutsuki"},
        {"ムツキ", "mutsuki"},
        {"ムツキ（正月）", "mutsuki"},
        {"무츠키", "mutsuki"},
        {"무츠키(새해)", "mutsuki"},

        // nagisa
        {"Nagisa", "nagisa"},
        {"nagisa", "nagisa"},
        {"ナギサ", "nagisa"},
        {"나기사", "nagisa"},

        // nagusa
        {"Nagusa", "nagusa"},
        {"nagusa", "nagusa"},
        {"ナグサ", "nagusa"},
        {"나구사", "nagusa"},

        // natsu
        {"Natsu", "natsu"},
        {"natsu", "natsu"},
        {"ナツ", "natsu"},
        {"나츠", "natsu"},

        // neru
        {"Neru", "neru"},
        {"Neru (Bunny)", "neru"},
        {"neru", "neru"},
        {"neru (bunny)", "neru"},
        {"ネル", "neru"},
        {"ネル（バニーガール）", "neru"},
        {"네루", "neru"},
        {"네루(바니걸)", "neru"},

        // noa
        {"Noa", "noa"},
        {"noa", "noa"},
        {"ノア", "noa"},
        {"노아", "noa"},

        // nodoka
        {"Nodoka", "nodoka"},
        {"Nodoka (Hot Spring)", "nodoka"},
        {"nodoka", "nodoka"},
        {"nodoka (hot spring)", "nodoka"},
        {"ノドカ", "nodoka"},
        {"ノドカ（温泉）", "nodoka"},
        {"노도카", "nodoka"},
        {"노도카(온천)", "nodoka"},

        // nonomi
        {"Nonomi", "nonomi"},
        {"Nonomi (Swimsuit)", "nonomi"},
        {"nonomi", "nonomi"},
        {"nonomi (swimsuit)", "nonomi"},
        {"ノノミ", "nonomi"},
        {"ノノミ（水着）", "nonomi"},
        {"노노미", "nonomi"},
        {"노노미(수영복)", "nonomi"},

        // pina
        {"Pina", "pina"},
        {"pina", "pina"},
        {"フィーナ", "pina"},
        {"피나", "pina"},

        // reisa
        {"Reisa", "reisa"},
        {"reisa", "reisa"},
        {"レイサ", "reisa"},
        {"레이사", "reisa"},

        // renge
        {"Renge", "renge"},
        {"renge", "renge"},
        {"レンゲ", "renge"},
        {"렌게", "renge"},

        // rin
        {"Rin", "rin"},
        {"rin", "rin"},
        {"リン", "rin"},
        {"린", "rin"},

        // rumi
        {"Rumi", "rumi"},
        {"rumi", "rumi"},
        {"ルミ", "rumi"},
        {"루미", "rumi"},

        // saki
        {"Saki", "saki"},
        {"Saki (Swimsuit)", "saki"},
        {"saki", "saki"},
        {"saki (swimsuit)", "saki"},
        {"サキ", "saki"},
        {"サキ（水着）", "saki"},
        {"사키", "saki"},
        {"사키(수영복)", "saki"},

        // sakurako
        {"Sakurako", "sakurako"},
        {"sakurako", "sakurako"},
        {"サクラコ", "sakurako"},
        {"사쿠라코", "sakurako"},

        // saori
        {"Saori", "saori"},
        {"Saori (Swimsuit)", "saori"},
        {"saori", "saori"},
        {"saori (swimsuit)", "saori"},
        {"サオリ", "saori"},
        {"サオリ（水着）", "saori"},
        {"사오리", "saori"},
        {"사오리(수영복)", "saori"},

        // saten
        // {"Saten Ruiko", "saten"},
        // {"saten ruiko", "saten"},
        // {"佐天涙子", "saten"},
        // {"사텐", "saten"},
        // {"사텐 루이코", "saten"},

        // saya
        {"Saya", "saya"},
        {"Saya (Casual)", "saya"},
        {"saya", "saya"},
        {"saya (casual)", "saya"},
        {"サヤ", "saya"},
        {"サヤ（私服）", "saya"},
        {"사야", "saya"},
        {"사야(사복)", "saya"},

        // sena
        {"Sena", "sena"},
        {"sena", "sena"},
        {"セナ", "sena"},
        {"세나", "sena"},

        // serika
        {"Serika", "serika"},
        {"Serika (New Year)", "serika"},
        {"Serika (Swimsuit)", "serika"},
        {"serika", "serika"},
        {"serika (new year)", "serika"},
        {"serika (swimsuit)", "serika"},
        {"セリカ", "serika"},
        {"セリカ（正月）", "serika"},
        {"セリカ（水着）", "serika"},
        {"세리카", "serika"},
        {"세리카(새해)", "serika"},
        {"세리카(수영복)", "serika"},

        // serina
        {"Serina", "serina"},
        {"Serina (Christmas)", "serina"},
        {"serina", "serina"},
        {"serina (christmas)", "serina"},
        {"セリナ", "serina"},
        {"セリナ（クリスマス）", "serina"},
        {"세리나", "serina"},
        {"세리나(크리스마스)", "serina"},

        // shigure
        {"Shigure", "shigure"},
        {"Shigure (Hot Spring)", "shigure"},
        {"shigure", "shigure"},
        {"shigure (hot spring)", "shigure"},
        {"シグレ", "shigure"},
        {"シグレ（温泉）", "shigure"},
        {"시구레", "shigure"},
        {"시구레(온천)", "shigure"},

        // shimiko
        {"Shimiko", "shimiko"},
        {"shimiko", "shimiko"},
        {"シミコ", "shimiko"},
        {"시미코", "shimiko"},

        // shiroko
        {"Shiroko", "shiroko"},
        {"Shiroko (Cycling)", "shiroko"},
        {"Shiroko (Swimsuit)", "shiroko"},
        {"Shiroko Terror", "shiroko"},
        {"shiroko", "shiroko"},
        {"shiroko (cycling)", "shiroko"},
        {"shiroko (swimsuit)", "shiroko"},
        {"shiroko terror", "shiroko"},
        {"シロコ", "shiroko"},
        {"シロコ（ライディング）", "shiroko"},
        {"シロコ（水着）", "shiroko"},
        {"シロコ＊テラー", "shiroko"},
        {"시로코", "shiroko"},
        {"시로코(라이딩)", "shiroko"},
        {"시로코(수영복)", "shiroko"},
        {"시로코*테러", "shiroko"},

        // shizuko
        {"Shizuko", "shizuko"},
        {"Shizuko (Swimsuit)", "shizuko"},
        {"shizuko", "shizuko"},
        {"shizuko (swimsuit)", "shizuko"},
        {"シズコ", "shizuko"},
        {"シズコ（水着）", "shizuko"},
        {"시즈코", "shizuko"},
        {"시즈코(수영복)", "shizuko"},

        // shokuhou
        // {"Shokuhou Misaki", "shokuhou"},
        // {"shokuhou misaki", "shokuhou"},
        // {"食蜂操祈", "shokuhou"},
        // {"쇼쿠호", "shokuhou"},
        // {"쇼쿠호 미사키", "shokuhou"},

        // shun
        {"Shun", "shun"},
        {"Shun (Small)", "shun"},
        {"shun", "shun"},
        {"shun (small)", "shun"},
        {"シュン", "shun"},
        {"シュン（幼女）", "shun"},
        {"슌", "shun"},
        {"슌(어린이)", "shun"},

        // sumire
        {"Sumire", "sumire"},
        {"sumire", "sumire"},
        {"スミレ", "sumire"},
        {"스미레", "sumire"},

        // suzumi
        {"Suzumi", "suzumi"},
        {"suzumi", "suzumi"},
        {"スズミ", "suzumi"},
        {"스즈미", "suzumi"},

        // toki
        {"Toki", "toki"},
        {"Toki (Bunny)", "toki"},
        {"toki", "toki"},
        {"toki (bunny)", "toki"},
        {"トキ", "toki"},
        {"トキ（バニーガール）", "toki"},
        {"토키", "toki"},
        {"토키(바니걸)", "toki"},

        // tomoe
        {"Tomoe", "tomoe"},
        {"tomoe", "tomoe"},
        {"トモエ", "tomoe"},
        {"토모에", "tomoe"},

        // tsubaki
        {"Tsubaki", "tsubaki"},
        {"Tsubaki (Guide)", "tsubaki"},
        {"tsubaki", "tsubaki"},
        {"tsubaki (guide)", "tsubaki"},
        {"ツバキ", "tsubaki"},
        {"ツバキ（ガイド）", "tsubaki"},
        {"츠바키", "tsubaki"},
        {"츠바키(가이드)", "tsubaki"},

        // tsukuyo
        {"Tsukuyo", "tsukuyo"},
        {"tsukuyo", "tsukuyo"},
        {"ツクヨ", "tsukuyo"},
        {"츠쿠요", "tsukuyo"},

        // tsurugi
        {"Tsurugi", "tsurugi"},
        {"Tsurugi (Swimsuit)", "tsurugi"},
        {"tsurugi", "tsurugi"},
        {"tsurugi (swimsuit)", "tsurugi"},
        {"ツルギ", "tsurugi"},
        {"ツルギ（水着）", "tsurugi"},
        {"츠루기", "tsurugi"},
        {"츠루기(수영복)", "tsurugi"},

        // ui
        {"Ui", "ui"},
        {"Ui (Swimsuit)", "ui"},
        {"ui", "ui"},
        {"ui (swimsuit)", "ui"},
        {"ウイ", "ui"},
        {"ウイ（水着）", "ui"},
        {"우이", "ui"},
        {"우이(수영복)", "ui"},

        // umika
        {"Umika", "umika"},
        {"umika", "umika"},
        {"ウミカ", "umika"},
        {"우미카", "umika"},

        // utaha
        {"Utaha", "utaha"},
        {"Utaha (Cheer Squad)", "utaha"},
        {"utaha", "utaha"},
        {"utaha (cheer squad)", "utaha"},
        {"ウタハ", "utaha"},
        {"ウタハ（応援団）", "utaha"},
        {"우타하", "utaha"},
        {"우타하(응원단)", "utaha"},

        // wakamo
        {"Wakamo", "wakamo"},
        {"Wakamo (Swimsuit)", "wakamo"},
        {"wakamo", "wakamo"},
        {"wakamo (swimsuit)", "wakamo"},
        {"ワカモ", "wakamo"},
        {"ワカモ（水着）", "wakamo"},
        {"와카모", "wakamo"},
        {"와카모(수영복)", "wakamo"},

        // yoshimi
        {"Yoshimi", "yoshimi"},
        {"Yoshimi (Band)", "yoshimi"},
        {"yoshimi", "yoshimi"},
        {"yoshimi (band)", "yoshimi"},
        {"ヨシミ", "yoshimi"},
        {"ヨシミ（バンド）", "yoshimi"},
        {"요시미", "yoshimi"},
        {"요시미(밴드)", "yoshimi"},

        // yukari
        {"Yukari", "yukari"},
        {"yukari", "yukari"},
        {"ユカリ", "yukari"},
        {"유카리", "yukari"},

        // yuuka
        {"Yuuka", "yuuka"},
        {"Yuuka (Track)", "yuuka"},
        {"yuuka", "yuuka"},
        {"yuuka (track)", "yuuka"},
        {"ユウカ", "yuuka"},
        {"ユウカ（体操服）", "yuuka"},
        {"유우카", "yuuka"},
        {"유우카(체육복)", "yuuka"},

        // yuzu
        {"Yuzu", "yuzu"},
        {"Yuzu (Maid)", "yuzu"},
        {"yuzu", "yuzu"},
        {"yuzu (maid)", "yuzu"},
        {"ユズ", "yuzu"},
        {"ユズ（メイド）", "yuzu"},
        {"유즈", "yuzu"},
        {"유즈(메이드)", "yuzu"},

        ///////////////////////////////////////////////////////
        /// 추가
        ///////////////////////////////////////////////////////

        // sora
        {"Sora", "sora"},
        {"sora", "sora"},
        {"ソラ", "sora"},
        {"소라", "sora"},

        // // rin
        // {"Rin", "rin"},
        // {"rin", "rin"},
        // {"リン", "rin"},
        // {"린", "rin"},

        // Adult1 - Gintoki
        {"Adult1", "adult1"},
        {"adult1", "adult1"},

        // // Adult2 - Satoru
        {"Adult2", "adult2"},
        {"adult2", "adult2"},

        // // Adult3 - Leleouch
        {"Adult3", "adult3"},
        {"adult3", "adult3"},

    };

    
    // 블랙리스트 (메타 정보, 불필요한 단어 등)
    // 자동 생성된 블랙리스트 데이터
    // Source: SchaleDB localization (School, SchoolLong, Club)
    // 포함 언어: KR, JP, EN (Key 값 포함)
    private static HashSet<string> wordBlacklist = new HashSet<string>()
    {
        "227号特別クラス",
        "227호 특별반",
        "227호특별반",
        "Abydos",
        "Abydos High School",
        "Abydos Student Council",
        "AbydosHighSchool",
        "AbydosStudentCouncil",
        "After-School Sweets Club",
        "After-SchoolSweetsClub",
        "Allied Hyakkiyako Academy",
        "AlliedHyakkiyakoAcademy",
        "Arius",
        "Arius Satellite School",
        "Arius Squad",
        "AriusSatelliteSchool",
        "AriusSquad",
        "AriusSqud",
        "Athletics Training Club",
        "AthleticsTrainingClub",
        "Black Tortoise Promenade",
        "BlackTortoisePromenade",
        "BookClub",
        "Class227",
        "CleanNClearing",
        "Cleaning & Clearing",
        "Cleaning&Clearing",
        "Countermeasure",
        "ETC",
        "Eastern Alchemy Society",
        "EasternAlchemySociety",
        "Emergentology",
        "EmptyClub",
        "Endanbou",
        "Engineer",
        "Engineering Department",
        "EngineeringDepartment",
        "Etc.",
        "Festival Operations Department",
        "FestivalOperationsDepartment",
        "FoodService",
        "Foreclosure Task Force",
        "ForeclosureTaskForce",
        "Fuuki",
        "Game Development Department",
        "GameDev",
        "GameDevelopmentDepartment",
        "Gehenna",
        "Gehenna Academy",
        "GehennaAcademy",
        "Genryumon",
        "Gourmet Research Society",
        "GourmetClub",
        "GourmetResearchSociety",
        "Hot Springs Department",
        "HotSpringsDepartment",
        "HoukagoDessert",
        "Hyakkaryouran Resolution Council",
        "HyakkaryouranResolutionCouncil",
        "Hyakkayouran",
        "Hyakkiyako",
        "Inner Discipline Club",
        "InnerDisciplineClub",
        "Justice",
        "Justice Task Force",
        "JusticeTaskForce",
        "KnightsHospitaller",
        "Knowledge Liberation Front",
        "KnowledgeLiberationFront",
        "Kohshinjo68",
        "Labor Party",
        "LaborParty",
        "Library Committee",
        "LibraryCommittee",
        "Make-Up Work Club",
        "Make-UpWorkClub",
        "MatsuriOffice",
        "Medical Emergency Club",
        "MedicalEmergencyClub",
        "Meihuayuan",
        "Millennium",
        "Millennium Science School",
        "MillenniumScienceSchool",
        "Ninjutsu Research Club",
        "NinjutsuResearchClub",
        "NinpoKenkyubu",
        "None",
        "Onmyobu",
        "Pandemonium Society",
        "PandemoniumSociety",
        "Plum Blossom Garden",
        "PlumBlossomGarden",
        "Prefect Team",
        "PrefectTeam",
        "Problem Solver 68",
        "ProblemSolver68",
        "Public Peace Bureau",
        "Public Safety Bureau",
        "PublicPeaceBureau",
        "PublicSafetyBureau",
        "RABBIT Squad",
        "RABBIT 소대",
        "RABBITSquad",
        "RABBIT小隊",
        "RABBIT소대",
        "RabbitPlatoon",
        "Red Winter",
        "Red Winter Federal Academy",
        "Red Winter Office",
        "RedWinter",
        "RedWinterFederalAcademy",
        "RedWinterOffice",
        "RedwinterSecretary",
        "Remedial Knights",
        "RemedialClass",
        "RemedialKnights",
        "SPTF",
        "SRT",
        "SRT Academy",
        "SRT 특수학원",
        "SRTAcademy",
        "SRT特殊学園",
        "SRT특수학원",
        "Sakugawa",
        "Sakugawa Middle School",
        "SakugawaMiddleSchool",
        "School Lunch Club",
        "SchoolLunchClub",
        "Seminar",
        "Shanhaijing",
        "Shanhaijing Academy",
        "ShanhaijingAcademy",
        "ShinySparkleSociety",
        "Shugyobu",
        "SisterHood",
        "Sparkle Club",
        "SparkleClub",
        "Spec Ops No. 227",
        "SpecOpsNo.227",
        "Super Phenomenon Task Force",
        "SuperPhenomenonTaskForce",
        "Tea Party",
        "TeaParty",
        "The Sisterhood",
        "TheSeminar",
        "TheSisterhood",
        "Tokiwadai",
        "Tokiwadai Middle School",
        "TokiwadaiMiddleSchool",
        "TrainingClub",
        "Trinity",
        "Trinity General School",
        "Trinity's Vigilante Crew",
        "Trinity'sVigilanteCrew",
        "TrinityGeneralSchool",
        "TrinityVigilance",
        "Valkyrie",
        "Valkyrie Police School",
        "ValkyriePoliceSchool",
        "Veritas",
        "Yin-Yang Club",
        "Yin-YangClub",
        "abydos",
        "abydos high school",
        "abydos student council",
        "abydoshighschool",
        "abydosstudentcouncil",
        "after-school sweets club",
        "after-schoolsweetsclub",
        "allied hyakkiyako academy",
        "alliedhyakkiyakoacademy",
        "anzenkyoku",
        "arius",
        "arius satellite school",
        "arius squad",
        "ariussatelliteschool",
        "ariussquad",
        "ariussqud",
        "athletics training club",
        "athleticstrainingclub",
        "black tortoise promenade",
        "blacktortoisepromenade",
        "bookclub",
        "class227",
        "cleaning & clearing",
        "cleaning&clearing",
        "cleannclearing",
        "countermeasure",
        "eastern alchemy society",
        "easternalchemysociety",
        "emergentology",
        "emptyclub",
        "endanbou",
        "engineer",
        "engineering department",
        "engineeringdepartment",
        "etc",
        "etc.",
        "festival operations department",
        "festivaloperationsdepartment",
        "foodservice",
        "foreclosure task force",
        "foreclosuretaskforce",
        "fuuki",
        "game development department",
        "gamedev",
        "gamedevelopmentdepartment",
        "gehenna",
        "gehenna academy",
        "gehennaacademy",
        "genryumon",
        "gourmet research society",
        "gourmetclub",
        "gourmetresearchsociety",
        "hot springs department",
        "hotspringsdepartment",
        "houkagodessert",
        "hyakkaryouran resolution council",
        "hyakkaryouranresolutioncouncil",
        "hyakkayouran",
        "hyakkiyako",
        "inner discipline club",
        "innerdisciplineclub",
        "justice",
        "justice task force",
        "justicetaskforce",
        "knightshospitaller",
        "knowledge liberation front",
        "knowledgeliberationfront",
        "kohshinjo68",
        "labor party",
        "laborparty",
        "library committee",
        "librarycommittee",
        "make-up work club",
        "make-upworkclub",
        "matsurioffice",
        "medical emergency club",
        "medicalemergencyclub",
        "meihuayuan",
        "millennium",
        "millennium science school",
        "millenniumscienceschool",
        "ninjutsu research club",
        "ninjutsuresearchclub",
        "ninpokenkyubu",
        "none",
        "onmyobu",
        "pandemonium society",
        "pandemoniumsociety",
        "plum blossom garden",
        "plumblossomgarden",
        "prefect team",
        "prefectteam",
        "problem solver 68",
        "problemsolver68",
        "public peace bureau",
        "public safety bureau",
        "publicpeacebureau",
        "publicsafetybureau",
        "rabbit squad",
        "rabbit 소대",
        "rabbitplatoon",
        "rabbitsquad",
        "rabbit小隊",
        "rabbit소대",
        "red winter",
        "red winter federal academy",
        "red winter office",
        "redwinter",
        "redwinterfederalacademy",
        "redwinteroffice",
        "redwintersecretary",
        "remedial knights",
        "remedialclass",
        "remedialknights",
        "sakugawa",
        "sakugawa middle school",
        "sakugawamiddleschool",
        "school lunch club",
        "schoollunchclub",
        "seminar",
        "shanhaijing",
        "shanhaijing academy",
        "shanhaijingacademy",
        "shinysparklesociety",
        "shugyobu",
        "sisterhood",
        "sparkle club",
        "sparkleclub",
        "spec ops no. 227",
        "specopsno.227",
        "sptf",
        "srt",
        "srt academy",
        "srt 특수학원",
        "srtacademy",
        "srt特殊学園",
        "srt특수학원",
        "super phenomenon task force",
        "superphenomenontaskforce",
        "tea party",
        "teaparty",
        "the sisterhood",
        "theseminar",
        "thesisterhood",
        "tokiwadai",
        "tokiwadai middle school",
        "tokiwadaimiddleschool",
        "trainingclub",
        "trinity",
        "trinity general school",
        "trinity's vigilante crew",
        "trinity'svigilantecrew",
        "trinitygeneralschool",
        "trinityvigilance",
        "valkyrie",
        "valkyrie police school",
        "valkyriepoliceschool",
        "veritas",
        "yin-yang club",
        "yin-yangclub",
        "お祭り運営委員会",
        "その他",
        "アビドス",
        "アビドス生徒会",
        "アビドス高等学校",
        "アリウス",
        "アリウススクワッド",
        "アリウス分校",
        "エンジニア部",
        "キラキラ部",
        "ゲヘナ",
        "ゲヘナ学園",
        "ゲーム開発部",
        "シスターフッド",
        "セミナー",
        "セミナ一",  // 一(한 일로 끝나는 것 OCR이 잘못 판별하는 이슈)
        "ティーパーティー",
        "ティーパーティ一",  // 一(한 일로 끝나는 것 OCR이 잘못 판별하는
        "トリニティ",
        "トリニティ総合学園",
        "トリニティ自警団",
        "トレーニング部",
        "パンデモニウム・ソサエティー",
        "ミレニアム",
        "ミレニアムサイエンススクール",
        "レッドウィンター",
        "レッドウィンター事務局",
        "レッドウィンター連邦学園",
        "ヴァルキューレ",
        "ヴァルキューレ警察学校",
        "ヴェリタス",
        "便利屋68",
        "修行部",
        "公安局",
        "図書委員会",
        "対策委員会",
        "山海経",
        "山海経高級中学校",
        "工務部",
        "常盤台中学",
        "忍術研究部",
        "放課後スイーツ部",
        "救急医学部",
        "救護騎士団",
        "柵川中学",
        "梅花園",
        "正義実現委員会",
        "温泉開発部",
        "無し",
        "特異現象捜査部",
        "玄武商会",
        "玄竜門",
        "生活安全局",
        "百花繚乱紛争調停委員会",
        "百鬼夜行",
        "百鬼夜行連合学院",
        "知識解放戦線",
        "給食部",
        "美食研究会",
        "補習授業部",
        "錬丹術研究会",
        "陰陽部",
        "風紀委員会",
        "게임개발부",
        "게헨나",
        "게헨나 학원",
        "게헨나학원",
        "공안국",
        "구호기사단",
        "그 외",
        "그외",
        "급양부",
        "대책위원회",
        "도서부",
        "마츠리운영관리부",
        "매화원",
        "미식연구회",
        "밀레니엄",
        "밀레니엄 사이언스 스쿨",
        "밀레니엄사이언스스쿨",
        "반짝반짝부",
        "발키리",
        "발키리 경찰학교",
        "발키리경찰학교",
        "방과후 디저트부",
        "방과후디저트부",
        "백귀야행",
        "백귀야행 연합학원",
        "백귀야행연합학원",
        "백화요란 분쟁조정위원회",
        "백화요란분쟁조정위원회",
        "베리타스",
        "보충수업부",
        "붉은겨울",
        "붉은겨울 사무국",
        "붉은겨울 연방학원",
        "붉은겨울사무국",
        "붉은겨울연방학원",
        "사쿠가와 중학교",
        "사쿠가와중학교",
        "산해경",
        "산해경 고급중학교",
        "산해경고급중학교",
        "생활안전국",
        "선도부",
        "세미나",
        "수행부",
        "시스터후드",
        "아리우스",
        "아리우스 분교",
        "아리우스 스쿼드",
        "아리우스분교",
        "아리우스스쿼드",
        "아비도스",
        "아비도스 고등학교",
        "아비도스 학생회",
        "아비도스고등학교",
        "아비도스학생회",
        "없음",
        "엔지니어부",
        "연단방",
        "온천개발부",
        "용역부",
        "음양부",
        "응급의학부",
        "인법연구부",
        "정의실현부",
        "지식해방전선",
        "초현상특무부",
        "토키와다이 중학교",
        "토키와다이중학교",
        "트레이닝부",
        "트리니티",
        "트리니티 자경단",
        "트리니티 종합학원",
        "트리니티자경단",
        "트리니티종합학원",
        "티파티",
        "판데모니움 소사이어티",
        "판데모니움소사이어티",
        "현룡문",
        "현무상회",
        "흥신소 68",
        "흥신소68",

        // 그 외 추가
        "엔젤 24",
        "エンジェル24",
        "Angel 24",

    };

    
    // 텍스트에서 캐릭터 actor ID 추출
    // 매칭되는 캐릭터가 있으면 actor ID 반환, 없으면 null
    public static string GetActorFromText(string text)
    {
        if (string.IsNullOrEmpty(text)) return null;
        
        string trimmed = text.Trim();
        
        if (actorMap.ContainsKey(trimmed))
        {
            return actorMap[trimmed];
        }
        
        return null;
    }
    
    // 텍스트가 블랙리스트에 포함되는지 확인
    public static bool IsBlacklisted(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        
        string trimmed = text.Trim();
        return wordBlacklist.Contains(trimmed);
    }
    
    // actor ID로부터 매핑된 모든 텍스트(키) 가져오기
    // 예: "arona" → {"アロナ", "arona", "Arona", "아로나"} 반환
    public static HashSet<string> GetAllActorNamesFromActorId(string actorId)
    {
        HashSet<string> result = new HashSet<string>();
        
        if (string.IsNullOrEmpty(actorId)) return result;
        
        foreach (var kvp in actorMap)
        {
            if (kvp.Value == actorId)
            {
                result.Add(kvp.Key);
            }
        }
        
        return result;
    }
        
    // 사용 가능한 모든 Actor ID 목록 반환 (중복 제거)
    // UI Dropdown 등에서 사용
    public static List<string> GetAllActorIds()
    {
        HashSet<string> uniqueActors = new HashSet<string>();
        
        foreach (var kvp in actorMap)
        {
            uniqueActors.Add(kvp.Value);
        }
        
        // 정렬된 리스트로 반환
        List<string> actorList = new List<string>(uniqueActors);
        actorList.Sort();
        return actorList;
    }
    
    // Actor ID를 표시용 이름으로 변환 (그대로 반환)
    // 예: "arona" → "arona", "mika" → "mika"
    public static string GetDisplayName(string actorId)
    {
        if (string.IsNullOrEmpty(actorId)) return "";
        
        // actorId를 그대로 반환 (소문자 형태 유지)
        return actorId;
    }
}

