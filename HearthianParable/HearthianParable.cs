using HarmonyLib;//
using OWML.Common;//
using OWML.ModHelper;//
using System;//
using System.Collections.Generic;//
using System.IO;//
using System.Reflection;//
using UnityEngine;//
using UnityEngine.InputSystem;//

namespace HearthianParable;

public class HearthianParable : ModBehaviour {
    public static HearthianParable Instance;
    public INewHorizons NewHorizons;
    readonly GameObject[] layers = new GameObject[4];
    readonly Transform[] triggers = new Transform[3];
    readonly Dictionary<string, Transform> endVolumes = [];
    GravityVolume grav;
    GameObject player, daTree;
    ShipLogManager shipLogManager;
    SubmitActionCloseMenu closeMenu, closeOptMenu;
    int subtitlesState = 0;
    DialogueBoxVer2 subtitles;
    ScreenPrompt speedrunPrompt;
    float speedrunTime, speedrunIGTime;
    AudioSource audioSource, devSource;
    readonly Dictionary<string, AudioClip> audioClips = [];
    readonly Dictionary<string, float> audioLength = [];
    readonly List<(float, Action)> actionsQueue = [];
    int gameState = 0, difficulty = 0;
    bool sawTree, landed, disappointed, holeFound, holeSaw, nomaiFound, upsideDown, sawSettings, heardDev, devCom, devSpedUp, devFound, reachedCore, speedRunTimer;
    readonly string[] dialogues = ["<color=#f08080>- Wait, that's all?</color>",
        "<color=#add8e6>- Seems like it!</color>",
        "<color=#f08080>- That's all they made in two weeks?</color>",
        "<color=#add8e6>- I guess so.</color> <color=#f08080>- Ok that's very bad I mean</color> <color=#add8e6>- Yeah.</color>",
        "<color=#f08080>- You know?</color> <color=#add8e6>- Sure sure.</color>",
        "<color=#f08080>- What even is that planet? It's huge right?</color> <color=#add8e6>- Yep.</color>",
        "<color=#f08080>- But the theme was \"miniature\"! I don't think that mod, or whatever this is really, even qualifies.</color>",
        "<color=#add8e6>- Very true, it's a disqualification right there.</color> <color=#f08080>- You bet!</color>",
        "<color=#f08080>Is there even one single prop on that planet?</color>",
        "<color=#add8e6>- I don't see any.</color> <color=#f08080>- Yeah.</color>",
        "<color=#add8e6>- Guess it's time to try another one then?</color>",
        "<color=#f08080>- Sure let's get out of that crap we tried everything...</color> <color=#add8e6>- Did you</color> <color=orange>check the settings</color><color=#add8e6>?</color> <color=#f08080>- Why?</color>",
        "<color=#add8e6>- I don't know. Maybe there is something like</color> <color=#f08080>- Like what?</color>",
        "<color=#add8e6>- I don't know!</color> <color=#f08080>- Like a \"Please toggle to have an actual mod\" setting?</color>",
        "<color=#add8e6>- Well maybe</color> <color=#f08080>- That's dumb.</color>",
        "<color=#add8e6>- Sure sure, but doesn't hurt to check right?</color> <color=#f08080>- I guess...</color>",
        "",
        "<color=#f08080>- What's that?</color>",
        "<color=#add8e6>- Idk, it's...</color>",
        "",
        "<color=#add8e6>- Oh! Ok so there IS at least one thing.</color>",
        "<color=#f08080>- Well it's a...</color> <color=#add8e6>- Yeah.</color>",
        "",
        "<color=#f08080>- Just a hole right?</color> <color=#add8e6>- Yup.</color>",
        "",
        "<color=#f08080>- Man that mod is so bad!</color> <color=#add8e6>- Yeah it's funny at this point.</color>",
        "",
        "<color=#f08080>- Developper's comm... seriously?</color> <color=#add8e6>- Ok sure!</color>",
        "",
        "<color=#f08080>- Right like: \"Oh here we though: what about making an empty mod?\"!</color>",
        "",
        "<color=#f08080>- Right like: \"Oh here we though: what about... a hole!\"!</color>",
        "",
        "<color=#f08080>- \"That's contemporary art you know. When I saw the theme I instantly though about a saxophone. It came to me in a dream actually!\"</color>",
        "<color=#f08080>- That's a joke right? I mean...</color> <color=#add8e6>- Let's try it!</color>",
        "<color=#f08080>- For real?</color> <color=#add8e6>- Why not?</color>",
        "<color=#f08080>- Well, there are serious entries to play and this is clearly...</color>",
        "<color=#f08080>- Well ok but if it starts to brag about this empty sphere I swear I leave that system never to return.</color>",
        "",
        "<color=#f08080>- Developpers commentary?</color> <color=#add8e6>- Mmh.</color>",
        "<color=#f08080>- That's a bit... strange but, ok let's see.</color>",
        "",
        "<color=#f08080>- Oh, Nomai floors...</color> <color=#add8e6>- Interesting.</color>",
        "",
        "<color=#add8e6>- Wait look! I see Nomai floors too.</color>",
        "<color=#f08080>- Oh. Ok let's check this out.</color>",
        "",
        "<color=#f08080>- Are we?</color> <color=#add8e6>- Upside down?</color> <color=#f08080>- Yeah.</color>",
        "<color=#add8e6>- What now?</color>",
        "<color=#f08080>- Idk. There should be something to let us go lower. But I don't see...</color> <color=#add8e6>- Did you</color> <color=orange>check the settings</color><color=#add8e6>?</color>",
        "",
        "<color=#f08080>- Mmh, let's see..</color>",
        "",
        "<color=#f08080>- Yeah, but there's only... mmh.</color>",
        "",
        "<color=yellow>Pssst, hey you! Hey you!</color>",
        "<color=yellow>Gigantous four-eyed creature.</color>",
        "<color=yellow>I'm here! On the floor!</color>",
        "<color=yellow>Yeah, well I'm really reaaaally small so you... well nevermind.</color>",
        "<color=yellow>These dev commentaries are pretty boring right?</color>",
        "",
        "<color=yellow>Well I added a key for you to fast forward them!</color>",
        "<color=yellow>You just need to</color> <color=orange>press K and it will speed up!</color>",
        "<color=yellow>Yeah yeah no problem, I'm happy to help!</color>",
        "<color=yellow>But I'll go now before you... you know, before you step on me by accident.</color>",
        "<color=yellow>Well, at least I hope it would be by accident...</color>",
        "<color=yellow>Bye!</color>",
        "",
        "<color=yellow>Oh! I see you muted them already, good call!</color>",
        "<color=yellow>I guess you don't need my help then!</color>",
        "<color=yellow>Bye!</color>",
        "",
        "<color=yellow>Wait... You never found them? But, how did you...</color>",
        "<color=yellow>Ooooooh, you're cheating! You came here after a reset, or you looked up the solution online!</color>",
        "<color=yellow>Ok well, here is what you came for I guess!</color>",
        "",
        "<color=#f08080>- Wow! What's that?</color>",
        "<color=#add8e6>- Is that the dev?</color>",
        "<color=#f08080>- Certainly looks like them.</color>",
        "<color=#add8e6>- Ouch. I guess that explains... well</color>",
        "<color=#f08080>- The mod</color> <color=#add8e6>- Yeah</color>",
        "<color=#f08080>- I think I know that anglerfish.</color>",
        "<color=#add8e6>- You what?</color> <color=#f08080>- Nevermind.</color>",
        "<color=#add8e6>- Do you think the fish is an allegory for the challenges of human communication?</color>",
        "<color=#f08080>- What are you even talking about?</color>",
        "<color=#f08080>- I wonder though... how did the mod get uploaded?</color>",
        "",
        "Hi there! Welcome to this little tour of the future mod!",
        "So this is a work-in-progress entry for the Miniature Mod Jam of 2025.",
        "The idea is to build something that fits within the shared solar system,",
        "so I decided to go with a kind of layered planet with a shrinking mechanic at its core.",
        "The theme got me thinking about scale and recursion and I just ran with it. <color=#f08080>- Please let's get it over with!</color> <color=#add8e6>- Sssh listen.</color>",
        "Right now you're seeing this big planet orbiting a purple star right? Go check it out.",
        "",
        "Yup! That's my planet! <color=#f08080>- Really nothing to brag about.</color>",
        "It doesn't have a name yet, I've just been calling it \"the onion\" because, well, it's built in layers.",
        "The outer surface is just temporary for now, kind of a placeholder. <color=#f08080>- No kidding.</color>",
        "But eventually there'll be several puzzles scattered around here, and you'll need to solve them to unlock access to a big building.",
        "It'll be like a hub to descend further into the planet. <color=#f08080>- Doesn't they know the jam is over?</color>",
        "Inside the building, you'll find a huge hole. <color=#add8e6>- Yeah that's strange.</color>",
        "And the hole is already there actually! <color=#f08080>- What a blessing!</color>",
        "Try finding it, it shouldn't be far.",
        "",
        "So you see it's covered in gravitational surfaces, so when you step up on the edge, gravity flips and you can walk down the inside.",
        "You end up upside down under the surface but, well it feels natural once you get used to it.",
        "You end up like, below the ground walking on its ceiling. I mean, the inner floor? Yeah let's call it the ceiling.",
        "Yeah I guess that's more like a ceiling.",
        "Well all ceilings are upside-down floors anyway, right? <color=#f08080>- Oh my god won't you shut up?!</color>",
        "So the plan is that by exploring each layer's ceiling, you can solve puzzles to finally flip gravity again to descend onto the next layer's \"floor\".",
        "For now of course there is no puzzle so <color=orange>you can flip gravity by hitting the V key</color>.",
        "<color=#add8e6>- That's good to know!</color> <color=#f08080>- Yay. More empty areas...</color>",
        "So I guess it's as a kind of vertical puzzle dungeon, each layer being its own little world with its own gravitational orientation.",
        "Well, actually each layer is two worlds, because you know there's a floor but there's also a ceiling every time which, as we saw earlier, is basically a reversed floor.",
        "<color=#f08080>- Can we just try another mod now?</color>",
        "I guess we could say that floors are reversed ceilings? <color=#add8e6>- Maybe it'll get interesting at some point?</color>",
        "But well we just need to agree on what to keep.",
        "Anyway. Maybe you could try to reach the center now?",
        "",
        "Oh you're there great! Now here's where the miniature part comes in.",
        "Once you reach this very core you'll find a sort of device (which I haven't built yet, but it'll be there). <color=#f08080>- Well, no it's not.</color>",
        "Activating it will shrink you, not just visually, but mechanically too.",
        "So when the time loop restarts, you'll still be tiny, and that means you can't just go back through the original puzzles:",
        "the entrances will be too big, or the jumps too far, or you won't weigh enough to trigger stuff idk.",
        "Each layer will also have miniature-sized puzzles that are tailored to your new size.",
        "So you'll have to find new paths, use new tools, and interact with the environment again differently.",
        "Of course it's not implemented yet <color=#f08080>- Of course.</color> but I tried binding the probe's camera rotation to it and...",
        "well let's not talk about what can happen if you change size too much. I absolutely need to fix that at some point.",
        "Ok so, you get to the core, you shrink, the planet doesn't change but you do, and that changes how you interact with everything.",
        "And eventually you'll reach a point where you're small enough to fit through a tiny passage.",
        "Humm it'll probably be somewhere like, somewhere like here for example, near the core, and it'll lead to the end of the mod!",
        "<color=#f08080>- Please tell me that's the end of their rambling!</color>",
        "Most of this isn't implemented yet, but the holes are there! And I've got a bunch of prototypes sketched out.",
        "The biggest challenge is going to be making each layer feel unique even though you're technically traversing the same space again and again, but smaller.",
        "I might also play around with sound or perception like, what if when you're small, the ambient noise changes? Or time feels different?",
        "But for now I'll be working on the <color=orange>secret easter egg below the star</color>. If it works it should be able to turn the player into a Nomai,",
        "and it will be a place to chill under the stars with marshmallows!",
        "<color=#f08080>- Easter eggs before functionalities! What a nice working routine... no wonder they never finished.</color>",
        "So now let's get back to work!",
        "<color=#add8e6>- Wait, is it over?</color>",
        "<color=#f08080>- I don't hear them anymore.</color>",
        "<color=#add8e6>- Ok so just like that they went to their secret place or whatever and then... nothing?</color>",
        "<color=#f08080>- Are you're kidding?? I'm glad it's finally over!</color>",
        "",
        "- Wha- what? Woooow! Did you fast forward my entire commentary??",
        "That's rude!",
        "Just... please go away now.",
        "Go on.",
        "Get out!",
        "Ok you know what?",
        "",
        "- Oh it worked! I can't believe it I'm inside the game!",
        "Waw! That star is soo big! And it's not that hot. Wow!",
        "Now I'll be chillin a bit, playing banjo and eating marshmallows...",
        "Oh what's that growing light?",
        "Oh, oh no it's AAAAAAAAAAAAAAAAAAAAAAAAAAAAAH"];
    readonly int[] dialoguesTimings = [0, 3, 4, 7, 11, 12, 16, 23, 30, 34, 37, 39, 45, 48, 53, 55, 666, 0, 2, 666, 0, 4, 666, 0, 666, 0, 666, 0, 666, 0, 666, 0, 666, 0, 10, 13, 16, 21, 666, 0, 3, 666, 0, 666, 0, 3, 666, 0, 5, 7, 666, 0, 666, 0, 666, 2, 5, 8, 12, 18, 666, 0, 4, 8, 11, 17, 21, 666, 0, 5, 7, 666, 0, 6, 14, 666, 0, 3, 5, 7, 10, 14, 16, 19, 23, 26, 666, 0, 4, 10, 14, 21, 27, 666, 0, 4, 10, 16, 24, 29, 33, 36, 666, 0, 9, 17, 30, 34, 40, 50, 56, 59, 69, 81, 83, 88, 91, 666, 0, 6, 15, 20, 30, 38, 45, 53, 61, 70, 80, 86, 96, 100, 109, 120, 132, 142, 145, 151, 158, 160, 162, 168, 666, 80, 86, 93, 101, 106, 110, 666, 0, 5, 13, 19, 22];

    public void Awake() {
        Instance = this;
        // You won't be able to access OWML's mod helper in Awake.
        // So you probably don't want to do anything here.
        // Use Start() instead.
    }

    public void Start() {
        // Starting here, you'll have access to OWML's mod helper.
        //ModHelper.Console.WriteLine($"My mod {nameof(HearthianParable)} is loaded!", MessageType.Success);

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
        gameState = (saveData != null) ? int.Parse(saveData.id) : 0;
        saveData = PlayerData.GetShipLogFactSave("HearthlingParable_gameSettings");
        if(saveData != null) {
            int settings = int.Parse(saveData.id);
            devCom = (settings & 1) > 0;
            speedRunTimer = (settings & 2) > 0;
            difficulty = (settings >> 2);
        } else {
            devCom = false;
            speedRunTimer = false;
            difficulty = 0;
        }
        ModHelper.Config.SetSettingsValue("DevCom", devCom);
        ModHelper.Config.SetSettingsValue("Speedrun", speedRunTimer);
        ModHelper.Config.SetSettingsValue("Difficulty", difficulty switch {
            1 => "Normal: Subtitles",
            2 => "Hard: Only audio",
            3 => "Insane: Nothing",
            _ => "Easy: Shiplogs"
        });
        ModHelper.Config.SetSettingsValue("Mod", true);
    }
    void SaveQuit() {
        actionsQueue.Clear();
        subtitles.SetVisible(false);
        subtitlesState = 0;
        SaveState();
    }
    void SaveState() {
        PlayerData._currentGameSave.shipLogFactSaves["HearthlingParable_gameState"] = new ShipLogFactSave(gameState.ToString());
        PlayerData._currentGameSave.shipLogFactSaves["HearthlingParable_gameSettings"] = new ShipLogFactSave((difficulty * 4 + (speedRunTimer ? 2 : 0) + (devCom ? 1 : 0)).ToString());
        PlayerData.SaveCurrentGame();
    }
    public override void Configure(IModConfig config) {
        if(LoadManager.GetCurrentScene() == OWScene.SolarSystem) {
            GetSettings(config);
            SaveState();
            if(!config.GetSettingsValue<bool>("Mod")) {
                closeOptMenu?.Submit();
                closeMenu?.Submit();
                Ending("deactivated");
            }
            speedrunPrompt?.SetVisibility(speedRunTimer);
            if(devSource != null) devSource.volume = (devCom ? 1 : 0);
            if(devCom) {
                audioSource.Stop();
                actionsQueue.Clear();
            }
            if(landed && !sawSettings && !devCom) {
                sawSettings = true;
                Narration("settings");
            }
        }
    }
    void GetSettings(IModConfig config = null) {
        config ??= ModHelper.Config;
        devCom = config.GetSettingsValue<bool>("DevCom");
        speedRunTimer = config.GetSettingsValue<bool>("Speedrun");
        difficulty = config.GetSettingsValue<string>("Difficulty") switch {
            "Normal: Subtitles" => 1,
            "Hard: Only audio" => 2,
            "Insane: Nothing" => 3,
            _ => 0
        };
    }

    public void OnCompleteSceneLoad(OWScene previousScene, OWScene newScene) {
        ModHelper.Config.SetSettingsValue("Mod", true);
        if(newScene != OWScene.SolarSystem) {
            if(speedrunPrompt != null && speedrunIGTime>0) speedrunIGTime -= Time.realtimeSinceStartup;
            return;
        }
        GetSettings();
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
        actionLoadScene.OnSubmitAction -= SaveQuit;
        actionLoadScene.OnSubmitAction += SaveQuit;
        closeMenu = GameObject.Find("PauseMenuBlock").transform.Find("PauseMenuItems/PauseMenuItemsLayout/Button-Unpause").GetComponent<SubmitActionCloseMenu>();
        closeOptMenu = GameObject.Find("PauseMenu/OptionsCanvas").transform.Find("OptionsMenu-Panel/OptionsButtons/UIElement-SaveAndExit").GetComponent<SubmitActionCloseMenu>();
        SpawnIntoSystem();
    }

    void SpawnIntoSystem(string systemName = "Jam5") {
        //ModHelper.Console.WriteLine("Spawn into Why??? system: " + (systemName != "Jam5") + (layers[1] != null), MessageType.Success);
        if(systemName != "Jam5" || (layers[1] != null && layers[1].GetComponent<Gravity_reverse>() != null)) return;
        ModHelper.Events.Unity.FireInNUpdates(() => {
            for(int i = 1;i < 6;i++) GameObject.Find("Vambok_THP_Platform_Body/Sector/Item" + i).SetActive((gameState & 1 << (i - 1)) > 0);
            daTree = GameObject.Find("Vambok_THP_Platform_Body/Sector/Treepot/TreeBad");
            if((gameState & 31) > 30) {
                GameObject.Find("Vambok_THP_Platform_Body/Sector/Treepot/TreeGood").SetActive(true);
                daTree.SetActive(false);
            } else {
                GameObject.Find("Vambok_THP_Platform_Body/Sector/Treepot/TreeGood").SetActive(false);
                daTree.SetActive(true);
            }
            if(speedrunPrompt == null) {
                speedrunPrompt = new ScreenPrompt("");
                speedrunTime = Time.realtimeSinceStartup;
            }
            speedrunIGTime += Time.realtimeSinceStartup;
            Locator.GetPromptManager().AddScreenPrompt(speedrunPrompt, PromptPosition.UpperRight);
            speedrunPrompt.SetVisibility(speedRunTimer);
            shipLogManager = Locator.GetShipLogManager();
            subtitles = GameObject.FindWithTag("DialogueGui").GetRequiredComponent<DialogueBoxVer2>();
            player = GameObject.Find("Player_Body");
            audioSource = player.AddComponent<AudioSource>();
            devSource = player.AddComponent<AudioSource>();
            devSource.clip = audioClips["devcom"];
            if(devCom) SubtitlesManager(88);
            devSource.volume = ((devCom && difficulty < 3) ? 1 : 0);
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
            speedrunPrompt.SetText("Real time: " + (Time.realtimeSinceStartup - speedrunTime).ToString("f") + "\nIn game time: " + (Time.realtimeSinceStartup - speedrunIGTime).ToString("f"));
            if(!sawTree && (player.transform.position - daTree.transform.position).magnitude < 10) {
                sawTree = true;
                shipLogManager.RevealFact("VAM-THP_ROOT_RUM");
                shipLogManager.RevealFact("VAM-THP_END1_RUM");
                shipLogManager.RevealFact("VAM-THP_END2_RUM");
                shipLogManager.RevealFact("VAM-THP_END3_RUM");
                shipLogManager.RevealFact("VAM-THP_END4_RUM");
                shipLogManager.RevealFact("VAM-THP_END5_RUM");
            }
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
                        ModHelper.Config.SetSettingsValue("DevCom", devCom);
                        Ending("kickedOut");
                    }
                } else {
                    if(!devFound && Keyboard.current.kKey.wasPressedThisFrame && devSource.isPlaying) {
                        float currentTime = (devSource.time + (landed ? 33.44f : 0) + (holeFound ? 38.33f : 0) + (reachedCore ? 95.28f : 0)) / 4;
                        if(currentTime < 78) {
                            devSpedUp = true;
                            devSource.Stop();
                            if(difficulty > 2) return;
                            devSource.clip = audioClips["devcomfast"];
                            SubtitlesManager(144);
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
            if(actionsQueue.Count > 0 && Time.realtimeSinceStartup > actionsQueue[0].Item1)
                actionsQueue[0].Item2();
            SubtitlesManager();
        }
    }

    void Narration(string audioId) {
        //ModHelper.Console.WriteLine(audioId + " playing", MessageType.Success);
        audioSource.Stop();
        actionsQueue.Clear();
        if(difficulty > 2) return;
        devSource.volume = (devCom ? 1 : 0);
        switch(audioId) {
        case "landing":
            if(!devCom) {
                audioSource.clip = audioClips["landing"];
                SubtitlesManager(1);
                actionsQueue.Add((Time.realtimeSinceStartup + 2, () => { disappointed = true; actionsQueue.RemoveAt(0); }));
                audioSource.Play();
            } else SubtitlesManager(95);
            devSource.Stop();
            devSource.clip = audioClips["devlanding"];
            devSource.Play();
            break;
        case "hole":
            if(!devCom) {
                if(disappointed) {
                    audioSource.clip = audioClips["holea"];
                    SubtitlesManager(21);
                    actionsQueue.Add((Time.realtimeSinceStartup + audioLength["holea"], () => { Narration("hole2"); }));
                } else {
                    audioSource.clip = audioClips["hole"];
                    SubtitlesManager(18);
                    actionsQueue.Add((Time.realtimeSinceStartup + audioLength["hole"], () => { Narration("hole2"); }));
                }
                audioSource.Play();
            } else SubtitlesManager(104);
            devSource.Stop();
            devSource.clip = audioClips["devhole"];
            devSource.Play();
            break;
        case "hole2":
            audioSource.clip = audioClips["hole2"];
            SubtitlesManager(24);
            actionsQueue.Add((Time.realtimeSinceStartup + 2.6f, () => { holeSaw = true; actionsQueue.RemoveAt(0); }));
            audioSource.Play();
            if(disappointed) actionsQueue.Add((Time.realtimeSinceStartup + audioLength["hole2"], () => { Narration("hole2A"); }));
            break;
        case "hole2A":
            audioSource.clip = audioClips["hole2a"];
            SubtitlesManager(26);
            audioSource.Play();
            break;
        case "settings":
            if(nomaiFound) {
                audioSource.clip = audioClips["settingsa"];
                SubtitlesManager(40);
            } else {
                audioSource.clip = audioClips["settings"];
                SubtitlesManager(28);
                actionsQueue.Add((Time.realtimeSinceStartup + audioLength["settings"], () => { Narration("settings2"); }));
            }
            audioSource.Play();
            break;
        case "settings2":
            if(holeSaw) {
                audioSource.clip = audioClips["settings2a"];
                SubtitlesManager(32);
                actionsQueue.Add((Time.realtimeSinceStartup + audioLength["settings2a"], () => { Narration("settings3"); }));
            } else {
                audioSource.clip = audioClips["settings2"];
                SubtitlesManager(30);
                actionsQueue.Add((Time.realtimeSinceStartup + audioLength["settings2"], () => { Narration("settings3"); }));
            }
            audioSource.Play();
            break;
        case "settings3":
            audioSource.clip = audioClips["settings3"];
            SubtitlesManager(34);
            audioSource.Play();
            break;
        case "nomaiFloors":
            nomaiFound = true;
            if(!devCom) {
                if(holeSaw) {
                    audioSource.clip = audioClips["nomaia"];
                    SubtitlesManager(45);
                } else {
                    audioSource.clip = audioClips["nomai"];
                    SubtitlesManager(43);
                }
                audioSource.Play();
            }
            break;
        case "innerSide":
            if(!devCom) {
                audioSource.clip = audioClips["innerside"];
                SubtitlesManager(48);
                actionsQueue.Add((Time.realtimeSinceStartup + audioLength["innerside"], () => { Narration("innerSide2"); }));
                audioSource.Play();
            }
            break;
        case "innerSide2":
            if(sawSettings) {
                audioSource.clip = audioClips["innerside2a"];
                SubtitlesManager(54);
            } else {
                audioSource.clip = audioClips["innerside2"];
                SubtitlesManager(52);
            }
            audioSource.Play();
            break;
        case "planetCore":
            audioSource.clip = audioClips["core"];
            SubtitlesManager(56);
            actionsQueue.Add((Time.realtimeSinceStartup + audioLength["core"], () => { Narration("planetCore2"); }));
            audioSource.Play();
            break;
        case "planetCore2":
            if(heardDev) {
                if(devCom) {
                    audioSource.clip = audioClips["core1"];
                    SubtitlesManager(62);
                    actionsQueue.Add((Time.realtimeSinceStartup + audioLength["core1"], () => { Narration("devCore"); }));
                } else {
                    audioSource.clip = audioClips["core2"];
                    SubtitlesManager(69);
                    devSource.Stop();
                    devSource.clip = audioClips["devcore"];
                    devSource.Play();
                }
            } else {
                audioSource.clip = audioClips["core3"];
                SubtitlesManager(73);
                actionsQueue.Add((Time.realtimeSinceStartup + audioLength["core3"], () => { Ending("cheater"); }));
            }
            audioSource.Play();
            break;
        case "devCore":
            devSource.Stop();
            devSource.clip = audioClips["devcore"];
            SubtitlesManager(119);
            devSource.Play();
            break;
        case "devFound":
            if(devCom) {
                devSource.Stop();
                devSource.clip = audioClips["devdead"];
                SubtitlesManager(151);
                devSource.Play();
                actionsQueue.Add((Time.realtimeSinceStartup + audioLength["devdead"], () => { Ending("ernestoDev"); }));
            } else {
                audioSource.clip = audioClips["devfound"];
                SubtitlesManager(77);
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
        string factUnlocked = "";
        switch(type) {
        case "deactivated":
            factUnlocked = "VAM-THP_END5_FACT";
            gameState |= 1 << 4;
            break;
        case "ernestoDev":
            factUnlocked = "VAM-THP_END1_FACT";
            gameState |= 1 << 3;
            break;
        case "ernesto":
            factUnlocked = "VAM-THP_END2_FACT";
            gameState |= 1 << 2;
            break;
        case "cheater":
            factUnlocked = "VAM-THP_END4_FACT";
            gameState |= 1 << 1;
            break;
        case "kickedOut":
            factUnlocked = "VAM-THP_END3_FACT";
            gameState |= 1 << 0;
            break;
        default: break;
        }
        shipLogManager.RevealFact(factUnlocked);
        if((gameState & 31) > 30) shipLogManager.RevealFact("VAM-THP_ROOT_FACT");
        SaveQuit();
        endVolumes[type].position = player.transform.position;
        endVolumes[type].parent = player.transform;
        endVolumes[type].GetComponent<SphereShape>().enabled = true;
    }

    void SubtitlesManager(int inState = 0) {
        if(difficulty > 1) return;
        if(inState > 0) subtitlesState = inState;
        if(subtitlesState > 0) {
            if(audioSource.isPlaying || (devCom && devSource.isPlaying)) {
                if((audioSource.isPlaying ? audioSource.time : devSource.time) > dialoguesTimings[subtitlesState - 1]) {
                    subtitles._potentialOptions = null;
                    subtitles.ResetAllText();
                    subtitles.SetMainFieldDialogueText(dialogues[subtitlesState - 1]);
                    subtitles._buttonPromptElement.gameObject.SetActive(false);
                    subtitles._mainFieldTextEffect?.StartTextEffect();
                    SubtitleShipLogs(subtitlesState);
                    subtitlesState++;
                }
            } else if(dialogues[subtitlesState - 1] == "") {
                //subtitles.InitializeOptionsUI();
                subtitles.SetVisible(false);
                subtitlesState = 0;
            }
        }
    }
    void SubtitleShipLogs(int state) {
        if(difficulty > 0) return;
        switch(state) {
        case 12:
        case 50:
            shipLogManager.RevealFact("VAM-THP_END5_1");
            break;
        case 102:
            shipLogManager.RevealFact("VAM-THP_END4_1");
            break;
        case 43:
        case 45:
        case 104:
            shipLogManager.RevealFact("VAM-THP_END4_3");
            break;
        case 110:
            shipLogManager.RevealFact("VAM-THP_END4_2");
            shipLogManager.RevealFact("VAM-THP_END3_1");
            break;
        case 70:
            shipLogManager.RevealFact("VAM-THP_END3_2");
            break;
        case 63:
            shipLogManager.RevealFact("VAM-THP_END_1");
            break;
        case 135:
            shipLogManager.RevealFact("VAM-THP_END1_1");
            shipLogManager.RevealFact("VAM-THP_END2_1");
            break;
        default:
            break;
        }
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
