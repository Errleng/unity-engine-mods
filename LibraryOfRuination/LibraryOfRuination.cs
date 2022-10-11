using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UnityEngine;
using UnityEngine.Networking;
using System.Reflection;
using HarmonyLib;

namespace LibraryOfRuination
{
    public class Initializer : ModInitializer
    {
        public static readonly string MOD_ID = "libraryofruination";
        public static readonly string ASSET_DIR = "../Resource";
        public static readonly string SOUND_DIR = $"{ASSET_DIR}/Sound/";
        public static readonly string[] audioClipPaths = {
            $"{SOUND_DIR}/you-face-jaraxxus.ogg",
            $"{SOUND_DIR}/you-face-jaraxxus-song.ogg",
        };

        public static List<EnemyUnitClassInfo> spawnList;
        public static Dictionary<string, AudioClip> audioClips = new Dictionary<string, AudioClip>();

        public static readonly LorId WillOfTheJavaScript = new LorId(MOD_ID, 1);
        public static readonly LorId RuneOfTheArchmage = new LorId(MOD_ID, 2);
        public static readonly LorId PrimeGaming = new LorId(MOD_ID, 3);
        public static readonly LorId SummoningPortal = new LorId(MOD_ID, 4);
        public static readonly LorId TriflingGnome = new LorId(MOD_ID, 5);
        public static readonly LorId Inferno = new LorId(MOD_ID, 6);
        public static readonly LorId GreaterSummoningPortal = new LorId(MOD_ID, 7);

        public static readonly LorId Passive_EradarLordPhase2 = new LorId(MOD_ID, 7);

        public override void OnInitializeMod()
        {
            base.OnInitializeMod();
            InitSpawnList();
            LoadSounds();
            Debug.Log($"{MOD_ID} loaded");

            var harmony = new Harmony(MOD_ID);
            harmony.PatchAll();
            foreach (var method in harmony.GetPatchedMethods())
            {
                Debug.Log($"Patched method {method.DeclaringType.Name}.{method.Name}");
            }
            Debug.Log($"Plugin {MOD_ID} is done patching!");
        }

        private void InitSpawnList()
        {
            spawnList = new List<EnemyUnitClassInfo>();
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch2").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch3").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch4").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch5").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch5_2").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch6").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch7").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_creature").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_creature_final").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch7_Philip").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch7_Eileen").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch7_Greta").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch7_Bremen").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch7_Oswald").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch7_Jaeheon").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch7_Elena").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch7_Pluto").list);
            spawnList.AddRange(LoadNewEnemyUnit("Xml/EnemyUnitInfo_ch7_BandFinal").list);
            foreach (EnemyUnitClassInfo classInfo in spawnList)
            {
                classInfo.height = RandomUtil.SystemRange(classInfo.maxHeight - classInfo.minHeight) + classInfo.minHeight;
            }
        }

        private EnemyUnitClassRoot LoadNewEnemyUnit(string path)
        {
            EnemyUnitClassRoot result;
            using (StringReader stringReader = new StringReader(Resources.Load<TextAsset>(path).text))
            {
                result = (EnemyUnitClassRoot)new XmlSerializer(typeof(EnemyUnitClassRoot)).Deserialize(stringReader);
            }
            return result;
        }

        private void LoadSounds()
        {
            foreach (var relativePath in audioClipPaths)
            {
                var absolutePath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), relativePath);
                LoadAudioClip(absolutePath, audioClips);
            }
        }

        async void LoadAudioClip(string path, Dictionary<string, AudioClip> clipDict)
        {
            using (UnityWebRequest uwr = UnityWebRequestMultimedia.GetAudioClip(path, AudioType.OGGVORBIS))
            {
                uwr.SendWebRequest();

                // wrap tasks in try/catch, otherwise it'll fail silently
                try
                {
                    while (!uwr.isDone) await Task.Delay(5);
                    if (uwr.isNetworkError || uwr.isHttpError)
                    {
                        Debug.Log($"Network error loading audio clip at {path}: {uwr.error}");
                    }
                    else
                    {
                        string filename = Path.GetFileNameWithoutExtension(path);
                        var clip = DownloadHandlerAudioClip.GetContent(uwr);
                        clipDict[filename] = clip;
                    }
                }
                catch (Exception err)
                {
                    Debug.Log($"Error loading audio clip at {path}: {err.Message}, {err.StackTrace}");
                }
            }
        }
    }
}
