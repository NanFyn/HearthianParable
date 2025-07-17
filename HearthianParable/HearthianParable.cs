using HarmonyLib;//
using OWML.Common;//
using OWML.ModHelper;//
using System;//
using System.Collections.Generic;//
using System.IO;//
using System.Reflection;//
using UnityEngine;
using UnityEngine.InputSystem;//

namespace HearthianParable;

public class HearthianParable : ModBehaviour {
    public static HearthianParable Instance;
    public INewHorizons NewHorizons;
    readonly GameObject[] layers = new GameObject[4];
    readonly Transform[] triggers = new Transform[3];
    readonly Dictionary<string, Transform> endVolumes = [];
    GravityVolume grav;

    GameObject player;
    AudioSource audioSource, devSource;
    readonly Dictionary<string, AudioClip> audioClips = [];
    readonly Dictionary<string, float> audioLength = [];
    readonly List<(float, Action)> actionsQueue = [];
    int gameState = 0;
    bool landed, disappointed, holeFound, holeSaw, nomaiFound, upsideDown, sawSettings, heardDev, devCom, devSpedUp, devFound, reachedCore;

    public void Awake() {
        Instance = this;
        // You won't be able to access OWML's mod helper in Awake.
        // So you probably don't want to do anything here.
        // Use Start() instead.
    }

    public void Start() {
        // Starting here, you'll have access to OWML's mod helper.
        ModHelper.Console.WriteLine($"My mod {nameof(HearthianParable)} is loaded!", MessageType.Success);

        // Get the New Horizons API and load configs
        NewHorizons = ModHelper.Interaction.TryGetModApi<INewHorizons>("xen.NewHorizons");
        NewHorizons.LoadConfigs(this);

        new Harmony("Vambok.HearthianParable").PatchAll(Assembly.GetExecutingAssembly());

        ModHelper.Events.Unity.RunWhen(PlayerData.IsLoaded, LoadData);
        OnCompleteSceneLoad(OWScene.TitleScreen, OWScene.TitleScreen); // We start on title screen
        LoadManager.OnCompleteSceneLoad += OnCompleteSceneLoad;
        //NewHorizons.GetStarSystemLoadedEvent().AddListener(SpawnIntoSystem);
    }

    void LoadData() {
        ShipLogFactSave saveData = PlayerData.GetShipLogFactSave("HearthlingParable_gameState");
        if(saveData != null) gameState = int.Parse(saveData.id);
        devCom = ModHelper.Config.GetSettingsValue<bool>("Developper's commentary");
    }
    void SaveState() {
        actionsQueue.Clear();
        PlayerData._currentGameSave.shipLogFactSaves["HearthlingParable_gameState"] = new ShipLogFactSave(gameState.ToString());
        PlayerData.SaveCurrentGame();
    }
    public override void Configure(IModConfig config) {
        if(LoadManager.GetCurrentScene() == OWScene.SolarSystem) {
            devCom = config.GetSettingsValue<bool>("Developper's commentary");
            if(devSource != null) devSource.volume = (devCom ? 1 : 0);
            if(!config.GetSettingsValue<bool>("Mod")) {
                Ending("deactivated");
            }
            if(landed && !sawSettings && !devCom) {
                sawSettings = true;
                Narration("settings");
            }
        }
    }

    public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene) {
        ModHelper.Config.SetSettingsValue("Mod", true);
        if(newScene != OWScene.SolarSystem) return;
        devCom = ModHelper.Config.GetSettingsValue<bool>("Developper's commentary");
        landed = disappointed = holeFound = holeSaw = nomaiFound = upsideDown = sawSettings = heardDev = devSpedUp = devFound = reachedCore = false;
        if(audioClips.Count <= 0) {
            AssetBundle audioBundle = AssetBundle.LoadFromFile(Path.Combine(ModHelper.Manifest.ModFolderPath, "Assets", "audiobundle"));
            if(audioBundle != null)
                foreach(string name in audioBundle.GetAllAssetNames()) {
                    string shortName = name.Split('/')[2].Split('.')[0];
                    audioClips[shortName] = audioBundle.LoadAsset<AudioClip>(name);
                    audioLength[shortName] = audioClips[shortName].length;
                    //ModHelper.Console.WriteLine(shortName + " (" + name + ")", MessageType.Success);
                }
        }
        SubmitActionLoadScene actionLoadScene = GameObject.Find("PauseMenuBlock").transform.Find("PauseMenuItems/PauseMenuItemsLayout/Button-ExitToMainMenu").GetComponent<SubmitActionLoadScene>();
        actionLoadScene.OnSubmitAction -= SaveState;
        actionLoadScene.OnSubmitAction += SaveState;
        SpawnIntoSystem();
    }

    void SpawnIntoSystem(string systemName = "Jam5") {
        ModHelper.Console.WriteLine("Spawn into Why system: " + (systemName != "Jam5") + (layers[1] != null), MessageType.Success);
        if(systemName != "Jam5" || (layers[1] != null && layers[1].GetComponent<Gravity_reverse>() != null)) return;
        ModHelper.Events.Unity.FireInNUpdates(() => {
            for(int i = 1;i < 6;i++) GameObject.Find("Vambok_THP_Platform_Body/Sector/Item" + i).SetActive((gameState & 1 << (i - 1)) > 0);
            if((gameState & 31) > 30) {
                GameObject.Find("Vambok_THP_Platform_Body/Sector/Treepot/TreeGood").SetActive(true);
                GameObject.Find("Vambok_THP_Platform_Body/Sector/Treepot/TreeBad").SetActive(false);
            } else {
                GameObject.Find("Vambok_THP_Platform_Body/Sector/Treepot/TreeGood").SetActive(false);
                GameObject.Find("Vambok_THP_Platform_Body/Sector/Treepot/TreeBad").SetActive(true);
            }
            player = GameObject.Find("Player_Body");
            audioSource = player.AddComponent<AudioSource>();
            devSource = player.AddComponent<AudioSource>();
            devSource.clip = audioClips["devcom"];//TODO
            devSource.volume = (devCom ? 1 : 0);
            devSource.Play();
            layers[0] = NewHorizons.GetPlanet("Big_Little_Planet");
            layers[1] = layers[0].transform.Find("Sector/Layer1").gameObject;
            layers[2] = layers[0].transform.Find("Sector/Layer2").gameObject;
            layers[3] = layers[0].transform.Find("Sector/Layer3").gameObject;
            Destroy(layers[3].transform.Find("Line1").gameObject);
            triggers[0] = layers[0].transform.Find("Sector/endVolumes");
            foreach(Transform endVol in triggers[0].transform.GetComponentsInChildren<Transform>()) {
                endVolumes[endVol.gameObject.name] = endVol;
                SphereShape titi = endVol.GetComponent<SphereShape>();
                if(titi != null) titi.enabled = true;
            }
            triggers[1] = layers[3].transform.Find("rotato3");
            Destroy(triggers[1].GetComponent<MeshRenderer>());
            triggers[2] = NewHorizons.GetPlanet("Little_Big_Planet").transform.Find("Sector/Vambok");
            GameObject toto;
            Transform rotato;
            for(int j = 1;j < 4;j++) {
                layers[j].AddComponent<Gravity_reverse>().modInstance = Instance;
                toto = layers[j].transform.Find("ring" + j).gameObject;
                toto.AddComponent<MeshCollider>();
                rotato = layers[j].transform.Find("rotato" + j);
                for(int i = 0;i < 15;i++) {
                    Instantiate(toto, rotato, true);
                    rotato.localEulerAngles += new Vector3(6.3158f, 0, 0);
                }
                for(int i = 0;i < 28;i++) {
                    Instantiate(toto, rotato, true);
                    rotato.localEulerAngles -= new Vector3(6.3158f, 0, 0);
                }
                for(int i = 0;i < 14;i++) {
                    Instantiate(toto, rotato, true);
                    rotato.localEulerAngles += new Vector3(6.3158f, 0, 0);
                }
                foreach(MeshCollider toti in rotato.GetComponentsInChildren<MeshCollider>()) {
                    toti.enabled = true;
                }
                foreach(MeshRenderer toti in rotato.GetComponentsInChildren<MeshRenderer>()) {
                    toti.enabled = true;
                }
            }
            grav = layers[0].transform.Find("GravityWell").GetComponent<GravityVolume>();
            grav._cutoffAcceleration = 2.4f;// gravity on inner layer = 12*groundSize/500
            grav._gravitationalMass = 1000f * 12 * 400 * 400;// 1000*surfaceGravity*surfaceSize^gravFallOff
            grav._lowerSurfaceRadius = 400;// = surfaceSize
            grav._cutoffRadius = 100;// = groundSize
            /*MeshRenderer[] dr = layers[0].GetComponentsInChildren<MeshRenderer>();
            foreach(MeshRenderer mr in dr) {
                mr.material = new Material(Shader.Find("Diffuse"));
            }*/
        }, 61);
    }

    void Update() {
        if(layers[0] != null) {
            float planet_dist = (player.transform.position - layers[0].transform.position).magnitude;
            if(planet_dist < 370 && planet_dist > 190) {
                if(planet_dist > 310) {
                    layers[1].transform.localEulerAngles += (Vector3.up - Vector3.forward) * 5 * Time.deltaTime;
                    layers[3].transform.localEulerAngles -= Vector3.forward * 5 * Time.deltaTime;
                } else {
                    layers[2].transform.localEulerAngles += (Vector3.forward - Vector3.up) * 5 * Time.deltaTime;
                    layers[3].transform.localEulerAngles -= Vector3.up * 5 * Time.deltaTime;
                }
            } else {
                layers[1].transform.localEulerAngles += Vector3.up * 5 * Time.deltaTime;
                layers[2].transform.localEulerAngles += Vector3.forward * 5 * Time.deltaTime;
            }
            if(Keyboard.current.vKey.wasPressedThisFrame) {
                Gravity_reverse();
            }
            if(devCom) {
                if(!heardDev) heardDev = true;
                if(devSpedUp) {
                    if(devSource.isPlaying) {
                        if(Keyboard.current.kKey.wasReleasedThisFrame && devSource.time < 80) {
                            devSource.time = 80;
                        }
                    } else {
                        devCom = false;
                        Ending("kickedOut");
                    }
                } else {
                    if(!devFound && Keyboard.current.kKey.wasPressedThisFrame && devSource.isPlaying) {
                        float currentTime = (devSource.time + (landed ? 33.44f : 0) + (holeFound ? 38.33f : 0) + (reachedCore ? 95.28f : 0)) / 4;
                        if(currentTime < 78) {
                            devSpedUp = true;
                            devSource.Stop();
                            devSource.clip = audioClips["devcomfast"];
                            devSource.Play();
                            devSource.time = currentTime;
                        }
                    }
                }
            }
            if(!landed) {
                if(planet_dist < 410) {
                    landed = true;
                    Narration("landing");
                }
            } else if(!holeFound) {
                if((triggers[1].position + Vector3.right * 400 - player.transform.position).magnitude < 100) {
                    holeFound = true;
                    Narration("hole");
                }
            } else if(!nomaiFound) {
                if((triggers[1].position + Vector3.right * 400 - player.transform.position).magnitude < 35) {
                    nomaiFound = true;
                    Narration("nomaiFloors");
                }
            } else if(!upsideDown) {
                if(planet_dist < 400 && (triggers[1].position + Vector3.right * 400 - player.transform.position).magnitude > 35 && grav._cutoffAcceleration < 0) {
                    upsideDown = true;
                    Narration("innerSide");
                }
            }
            if(!devFound && (triggers[2].position - player.transform.position).magnitude < 5) {
                devFound = true;
                Narration("devFound");
            }
            if(!reachedCore && planet_dist < 110) {
                reachedCore = true;
                Narration("planetCore");
            }
            if(actionsQueue.Count > 0 && Time.realtimeSinceStartup > actionsQueue[0].Item1) {
                actionsQueue[0].Item2();
                actionsQueue.RemoveAt(0);
            }
        }
    }

    void Narration(string audioId) {
        ModHelper.Console.WriteLine(audioId + " playing", MessageType.Success);
        if(audioSource.isPlaying) {
            audioSource.Stop();
            actionsQueue.Clear();
        }
        devSource.volume = (devCom ? 1 : 0);
        switch(audioId) {
            case "landing":
                if(!devCom) {
                    audioSource.clip = audioClips["landing"];
                    actionsQueue.Add((Time.realtimeSinceStartup + 2, () => { disappointed = true; }));
                    audioSource.Play();
                }
                devSource.Stop();
                devSource.clip = audioClips["devlanding"];
                devSource.Play();
                break;
            case "hole":
                if(!devCom) {
                    if(disappointed) {
                        audioSource.clip = audioClips["holea"];
                        actionsQueue.Add((Time.realtimeSinceStartup + audioLength["holea"], () => { Narration("hole2"); }));
                    } else {
                        audioSource.clip = audioClips["hole"];
                        actionsQueue.Add((Time.realtimeSinceStartup + audioLength["hole"], () => { Narration("hole2"); }));
                    }
                    audioSource.Play();
                }
                devSource.Stop();
                devSource.clip = audioClips["devhole"];
                devSource.Play();
                break;
            case "hole2":
                audioSource.clip = audioClips["hole2"];
                actionsQueue.Add((Time.realtimeSinceStartup + 2.6f, () => { holeSaw = true; }));
                audioSource.Play();
                if(disappointed) actionsQueue.Add((Time.realtimeSinceStartup + audioLength["hole2"], () => { Narration("hole2A"); }));
                break;
            case "hole2A":
                audioSource.clip = audioClips["hole2a"];
                audioSource.Play();
                break;
            case "settings":
                if(nomaiFound) audioSource.clip = audioClips["settingsa"];
                else {
                    audioSource.clip = audioClips["settings"];
                    actionsQueue.Add((Time.realtimeSinceStartup + audioLength["settings"], () => { Narration("settings2"); }));
                }
                audioSource.Play();
                break;
            case "settings2":
                if(holeSaw) {
                    audioSource.clip = audioClips["settings2a"];
                    actionsQueue.Add((Time.realtimeSinceStartup + audioLength["settings2a"], () => { Narration("settings3"); }));
                } else {
                    audioSource.clip = audioClips["settings2"];
                    actionsQueue.Add((Time.realtimeSinceStartup + audioLength["settings2"], () => { Narration("settings3"); }));
                }
                audioSource.Play();
                break;
            case "settings3":
                audioSource.clip = audioClips["settings3"];
                audioSource.Play();
                break;
            case "nomaiFloors":
                nomaiFound = true;
                if(!devCom) {
                    if(holeSaw) audioSource.clip = audioClips["nomaia"];
                    else audioSource.clip = audioClips["nomai"];
                    audioSource.Play();
                }
                break;
            case "innerSide":
                if(!devCom) {
                    audioSource.clip = audioClips["innerside"];
                    actionsQueue.Add((Time.realtimeSinceStartup + audioLength["innerside"], () => { Narration("innerSide2"); }));
                    audioSource.Play();
                }
                break;
            case "innerSide2":
                if(sawSettings) audioSource.clip = audioClips["innerside2a"];
                else audioSource.clip = audioClips["innerside2"];
                audioSource.Play();
                break;
            case "planetCore":
                audioSource.clip = audioClips["core"];
                actionsQueue.Add((Time.realtimeSinceStartup + audioLength["core"], () => { Narration("planetCore2"); }));
                audioSource.Play();
                break;
            case "planetCore2":
                if(heardDev) {
                    if(devCom) {
                        audioSource.clip = audioClips["core1"];
                        actionsQueue.Add((Time.realtimeSinceStartup + audioLength["core1"], () => { Narration("devCore"); }));
                    } else {
                        audioSource.clip = audioClips["core2"];
                        devSource.Stop();
                        devSource.clip = audioClips["devcore"];
                        devSource.Play();
                    }
                } else {
                    audioSource.clip = audioClips["core3"];
                    actionsQueue.Add((Time.realtimeSinceStartup + audioLength["core3"], () => { Ending("cheater"); }));
                }
                audioSource.Play();
                break;
            case "devCore":
                devSource.Stop();
                devSource.clip = audioClips["devcore"];
                devSource.Play();
                break;
            case "devFound":
                if(devCom) {
                    devSource.Stop();
                    devSource.clip = audioClips["devdead"];
                    devSource.Play();
                    actionsQueue.Add((Time.realtimeSinceStartup + audioLength["devdead"], () => { Ending("ernestoDev"); }));
                } else {
                    audioSource.clip = audioClips["devfound"];
                    actionsQueue.Add((Time.realtimeSinceStartup + audioLength["devfound"], () => { Ending("ernesto"); }));
                    audioSource.Play();
                }
                break;
            default:
                return;
            }
    }

    public void Gravity_reverse() {
        grav._surfaceAcceleration *= -1;
        grav._cutoffAcceleration *= -1;
        grav._gravitationalMass *= -1;
    }

    void Ending(string type) {
        switch(type) {
        case "deactivated":
            gameState |= 1 << 4;
            break;
        case "ernestoDev":
            gameState |= 1 << 3;
            break;
        case "ernesto":
            gameState |= 1 << 2;
            break;
        case "cheater":
            gameState |= 1 << 1;
            break;
        case "kickedOut":
            gameState |= 1 << 0;
            break;
        default: break;
        }
        SaveState();
        endVolumes[type].position = player.transform.position;
        endVolumes[type].parent = player.transform;
    }
}

public class Gravity_reverse : MonoBehaviour {
    public HearthianParable modInstance;
    private void OnTriggerEnter(Collider col) {
        if(col.CompareTag("Player")) modInstance.Gravity_reverse();
    }
    private void OnTriggerExit(Collider col) {
        if(col.CompareTag("Player")) modInstance.Gravity_reverse();
    }
}