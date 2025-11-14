using Duckov;
using FMOD.Studio;
using HarmonyLib;
using ItemStatsSystem;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Duckov.Modding;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BFKillFeedback
{
	public class ModBehaviour : Duckov.Modding.ModBehaviour
	{
		public static Harmony harmony = new Harmony("net.bcasoft.bfkillfeedback");
		public const string MOD_NAME = "BFKillFeedback";
		public static readonly Vector2 BaseResolution = new Vector2(1920.0f, 1080.0f);
		public static bool Loaded = false;
		public static Dictionary<string, object> DefaultConfig = new Dictionary<string, object>(){
			{"use_sfx_namespace", "bf5"}, //使用的音效命名空间
			{"volume", 0.6f}, //音量
			{"max_skull_count", 10}, //最大骷髅头数
			{"enforce_max_skull_count", 25}, //强制最大骷髅头数
			{"disable_icon", false}, //禁用图标
			{"skull_fadein_seconds", 0.4f}, //骷髅头淡入时间
			{"skull_stay_seconds", 5.0f}, //骷髅头留置时间
			{"skull_fadeout_seconds", 0.3f}, //骷髅头淡出时间
			{"skull_spacing", 0.125f}, //骷髅头之间的间距乘数，基于骷髅头Transform的宽度，实际的间距是两个骷髅头的一左一右间距相加
			{"skull_color", "FFFFFFD9"}, //普通骷髅头颜色(含不透明度)
			{"headshot_skull_color", "FF8C00D9"}, //爆头骷髅头颜色(含不透明度)
			{"headshot_ring_init_size_rate", 0.75f}, //爆头骷髅头圆圈起始放大倍率
			{"headshot_ring_max_size_rate", 6.0f}, //爆头骷髅头圆圈最大放大倍率
			{"headshot_ring_bold_alpha_decrease", 0.4f}, //爆头骷髅头圆圈粗线差分的不透明度减值
			{"headshot_ring_stay_seconds", 1.0f}, //爆头骷髅头圆圈存在持续时间
			{"position_offset_x", 0.0f}, //坐标偏移X
			{"position_offset_y", -0.6f}, //坐标偏移Y
			{"scale", 0.6f}, //缩放倍率
			{"addition_scale", false}, //启用增量缩放，增量缩放是会跟随窗口分辨率变化而反向缩放功能
			{"skull_scale_on_drop", 2.0f}, //图标在初始化时的尺寸乘数，基于正常尺寸，用于图标掉入效果
			{"disable_text", false}, //禁用击杀文本
			{"text_template", "{0}#{1}[{2}]{3}m +{4}"}, //击杀文本模板，0=敌人名字，1=击杀计数，2=使用的武器，3=距离，4=加分(经验值)
			{"text_color", "FFFFFFD9"}, //击杀文本颜色(含不透明度)
			{"text_position_offset_x", 0.0f}, //文本坐标偏移X，为1.0时代表半个屏幕分辨率
			{"text_position_offset_y", -0.175f}, //文本坐标偏移Y，为1.0时代表半个屏幕分辨率
			{"text_scale", 1.0f}, //文本缩放倍率
			{"text_stay_seconds", 2.0f}, //单个击杀文本的停留时间(含淡入时间)
			{"text_memory_length", 30}, //同时存储的击杀文本数量
			{"text_fade_seconds", 0.25f}, //文本淡入淡出时间(不可为0)
			{"disable_score", false}, //禁用分数
			{"score_color", "FFFFFFD9"}, //分数文本颜色(含不透明度)
			{"score_text_position_offset_x", 0.0f}, //分数文本坐标偏移X，为1.0时代表半个屏幕分辨率
			{"score_text_position_offset_y", -0.3f}, //分数文本坐标偏移Y，为1.0时代表半个屏幕分辨率
			{"score_scale", 2.5f}, //分数缩放倍率
			{"score_text_number_increase_per_second", 200}, //分数文本数字增加速度(每秒)
			{"score_text_stay_seconds", 7.0f}, //分数文本在消失前的等待时间
			{"hide_reload_progress_bar", true}, //隐藏换弹进度条
			{"disable_vanilla_kill_feedback_sound", false} //禁用原版反馈音效
		};
		public static ModBehaviour? Instance;
		// 所有图标名称，这些名称将用于拼接成定位图片资源的路径(.png)
		public static readonly string[] ImageNames = new string[]
		{
			"kill",
			"headkill",
			"headkill_frame_thin",
			"headkill_frame_bold"
		};
		// 从硬盘加载到内存的图片资源
		public static Dictionary<string, Sprite> Images = new Dictionary<string, Sprite>();
		// 从硬盘加载到内存的音频资源
		/*
		public static List<Sound> AudiosDeath = new List<Sound>();
		public static List<Sound> AudiosKill = new List<Sound>();
		public static List<Sound> AudiosHeadKill = new List<Sound>();
		public static List<Sound> AudiosOneshotHeadKill = new List<Sound>();
		public static List<Sound> AudiosMeleeKill = new List<Sound>();
		public static List<Sound> AudiosCritMeleeKill = new List<Sound>();
		public static List<Sound> AudiosOneshotMeleeKill = new List<Sound>();
		public static List<Sound> AudiosExplosionKill = new List<Sound>();
		public static List<Sound> AudiosOneshotExplosionKill = new List<Sound>();
		*/
		public static List<string> AudiosDeath = new List<string>();
		public static List<string> AudiosKill = new List<string>();
		public static List<string> AudiosHeadKill = new List<string>();
		public static List<string> AudiosOneshotHeadKill = new List<string>();
		public static List<string> AudiosMeleeKill = new List<string>();
		public static List<string> AudiosCritMeleeKill = new List<string>();
		public static List<string> AudiosOneshotMeleeKill = new List<string>();
		public static List<string> AudiosExplosionKill = new List<string>();
		public static List<string> AudiosOneshotExplosionKill = new List<string>();
		public static RectTransform? ui_transform;
		public static RectTransform? ui_text_transform;
		public static TextMeshProUGUI? ui_text;
		public static RectTransform? ui_score_transform;
		public static TextMeshProUGUI? ui_score;
		public static ActionProgressHUD? action_process_hud;
		public static CharacterMainControl? player_character_control;
		// 配置文件-使用的音效命名空间
		public static string sfx_namespace = "bf5";
		// 配置文件-音量，0-1决定音量大小
		public static float volume = 0.6f;
		// 配置文件-允许同时存在的最大骷髅头数，超过此数时添加新骷髅头将导致旧骷髅头立即开始消失
		public static int max_skull_count = 10;
		// 配置文件-强制允许同时存在的最大骷髅头数，超过此数时击杀敌人不会添加新骷髅头
		public static int enforce_max_skull_count = 25;
		// 配置文件-禁用图标，为true时不显示图标
		public static bool disable_icon = false;
		// 配置文件-骷髅头淡入时间
		public static float skull_fadein_seconds = 0.4f;
		// 配置文件-骷髅头滞留时间
		public static float skull_stay_seconds = 3.0f;
		// 配置文件-骷髅头淡出时间
		public static float skull_fadeout_seconds = 0.3f;
		// 配置文件-骷髅头间距乘数
		public static float skull_spacing = 0.0f;
		// 配置文件-普通骷髅头颜色
		public static Color skull_color = Color.white;
		// 配置文件-爆头骷髅头颜色
		public static Color headshot_skull_color = new Color(1.0f, 0.55f, 0.0f, 0.85f);
		// 配置文件-爆头骷髅头圆圈起始放大倍率
		public static float headshot_ring_init_size_rate = 0.75f;
		// 配置文件-爆头骷髅头圆圈最大放大倍率
		public static float headshot_ring_max_size_rate = 6.0f;
		// 配置文件-爆头骷髅头圆圈的粗线差分精灵图的不透明度的减值
		public static float headshot_ring_bold_alpha_decrease = 0.4f;
		// 配置文件-爆头骷髅头圆圈存在持续时间
		public static float headshot_ring_stay_seconds = 1.0f;
		// 配置文件-坐标偏移
		public static Vector2 position_offset = new Vector2(0.0f, -0.6f);
		// 配置文件-总体缩放倍率
		public static float scale = 0.6f;
		// 配置文件-禁用增量缩放
		public static bool addition_scale = false;
		// 配置文件-图标在初始化时的尺寸
		public static float skull_scale_on_drop = 2.0f;
		// 配置文件-禁用击杀文本
		public static bool disable_text = false;
		// 配置文件-击杀文本模板
		public static string text_template = "{0}[{1}]{2}m +{3}";
		// 配置文件-击杀文本颜色
		public static Color text_color = new Color(1.0f, 1.0f, 1.0f, 0.85f);
		// 配置文件-文本坐标偏移率
		public static Vector2 text_position_offset = new Vector2(0.0f, -0.175f);
		// 配置文件-文本缩放倍率
		public static float text_scale = 1.0f;
		// 配置文件-文本停留时间
		public static float text_stay_seconds = 2.0f;
		// 配置文件-同时存储的击杀文本数量
		public static int text_memory_length = 30;
		// 配置文件-文本淡入淡出时间
		public static float text_fade_seconds = 0.25f;
		// 配置文件-禁用分数
		public static bool disable_score = false;
		// 配置文件-分数文本颜色
		public static Color score_color = new Color(1.0f, 1.0f, 1.0f, 0.85f);
		// 配置文件-分数文本坐标偏移
		public static Vector2 score_text_position_offset = new Vector2(0.0f, -0.3f);
		// 配置文件-分数缩放倍率
		public static float score_scale = 2.5f;
		// 配置文件-分数文本数字增加速度(每秒)
		public static int score_text_number_increase_per_second = 200;
		// 配置文件-分数文本在消失前的等待时间
		public static float score_text_stay_seconds = 7.0f;
		// 配置文件-隐藏换弹进度条
		public static bool hide_reload_progress_bar = true;
		// 配置文件-禁用原版击杀反馈音效
		public static bool disable_vanilla_kill_feedback_sound = false;
		public static bool is_last_frame_progressing = false;
		public static List<ISkull> skulls = new List<ISkull>();
		public static System.Random random = new System.Random(DateTime.Now.Second);
		public static Dictionary<string, int> kill_counter = new Dictionary<string, int>(); //击杀计数器，记录当前局内各种敌人都杀了几次
		public static string last_scene_name = "";
		private void Update()
		{
			if (ui_text_transform != null && ui_text != null && ui_score_transform != null && ui_score != null)
			{
				KillTextHandler.Update(Time.time, ui_text_transform, ui_text, ui_score_transform, ui_score);
			}
			if (ui_transform != null)
			{
				float addition_scale_num = Math.Min(Screen.width / BaseResolution.x, Screen.height / BaseResolution.y);
				if (!addition_scale)
				{
					addition_scale_num = 1.0f;
				}
				ui_transform.localPosition = new Vector3(position_offset.x * BaseResolution.x / 2.0f / addition_scale_num, position_offset.y * BaseResolution.y / 2.0f / addition_scale_num);
				ui_transform.localScale = new Vector3(scale * addition_scale_num, scale * addition_scale_num);
			}
			List<int> skulls_want_destroy = new List<int>();
			float total_width = 0.0f;
			for (int i = 0; i < skulls.Count; i++)
			{
				skulls[i].UpdateAlpha(Time.time);
				total_width += skulls[i].EffectWidth;
			}
			for (int i = 0; i < skulls.Count; i++)
			{
				skulls[i].UpdatePosition(Time.time, total_width, i);
				if (skulls[i].WantDestroy)
				{
					skulls_want_destroy.Add(i);
				}
			}
			for (int i = skulls_want_destroy.Count - 1; i >= 0; i--)
			{
				skulls[skulls_want_destroy[i]].Destroy();
				skulls.RemoveAt(skulls_want_destroy[i]);
			}
			if (hide_reload_progress_bar)
			{
				if (player_character_control == null)
				{
					if (LevelManager.Instance != null && LevelManager.Instance.MainCharacter != null)
					{
						player_character_control = LevelManager.Instance.MainCharacter;
						player_character_control.OnActionStartEvent += OnActionStart;
					}
				}
			}
		}
		public void OnDead(Health health, DamageInfo damageInfo)
		{
			// 防空引用
			if (health == null)
			{
				return;
			}
			// 如果死掉的是玩家
			if (health.IsMainCharacterHealth)
			{
				if (AudiosDeath.Count > 0)
				{
					//RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out ChannelGroup channelgroup);
					//RuntimeManager.CoreSystem.playSound(AudiosDeath[random.Next(0, AudiosDeath.Count)], channelgroup, false, out Channel channel);
					//channel.setVolume(volume);
					EventInstance? event_instance = AudioManager.PostCustomSFX(AudiosDeath[random.Next(0, AudiosDeath.Count)]);
					event_instance?.setVolume(volume);
				}
				return;
			}
			// 如果伤害来自玩家队
			if (damageInfo.fromCharacter.IsMainCharacter())
			{
				bool headshot = damageInfo.crit > 0;
				bool oneshotkill = damageInfo.finalDamage >= health.MaxHealth * 0.9f;
				PlayKill(headshot, oneshotkill, damageInfo.fromCharacter.GetMeleeWeapon() != null, damageInfo.isExplosion);
				if (damageInfo.toDamageReceiver.health.TryGetCharacter() != null)
				{
					string killed_enemy_name = damageInfo.toDamageReceiver.health.TryGetCharacter().characterPreset.DisplayName;
					int killed_count = AddNewKillToCounter(killed_enemy_name);
					KillTextHandler.NewKill(new KillTextHandler.KillData(killed_enemy_name, ItemAssetsCollection.GetMetaData(damageInfo.fromWeaponItemID).DisplayName, ((Item)(typeof(Health).GetField("item", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.GetField).GetValue(health))).GetInt("Exp", 114514), Vector3.Distance(damageInfo.fromCharacter.modelRoot.position, damageInfo.toDamageReceiver.health.TryGetCharacter().modelRoot.position), killed_count));
				}
			}
		}
		public void PlayKill(bool crit, bool oneshotkill, bool melee_kill, bool explosion_kill)
		{
			if (ui_transform == null)
			{
				CreateUI();
			}
			// 确定使用的资源
			string audio = "";
			bool audio_set = false;
			if (explosion_kill) //爆炸
			{
				if (oneshotkill)
				{
					audio_set = SoundChooser(out audio, new List<List<string>>() { AudiosOneshotExplosionKill, AudiosExplosionKill, AudiosKill });
				}
				else
				{
					audio_set = SoundChooser(out audio, new List<List<string>>() { AudiosExplosionKill, AudiosKill });
				}
			}
			if (melee_kill && !audio_set) //近战
			{
				if (oneshotkill)
				{
					if (crit)
					{
						audio_set = SoundChooser(out audio, new List<List<string>>() { AudiosOneshotMeleeKill, AudiosCritMeleeKill, AudiosMeleeKill, AudiosKill });
					}
					else
					{
						audio_set = SoundChooser(out audio, new List<List<string>>() { AudiosOneshotMeleeKill, AudiosMeleeKill, AudiosKill });
					}
				}
				else
				{
					if (crit)
					{
						audio_set = SoundChooser(out audio, new List<List<string>>() { AudiosCritMeleeKill, AudiosMeleeKill, AudiosKill });
					}
					else
					{
						audio_set = SoundChooser(out audio, new List<List<string>>() { AudiosMeleeKill, AudiosKill });
					}
				}
			}
			if (!audio_set) //枪械
			{
				if (oneshotkill)
				{
					audio_set = SoundChooser(out audio, new List<List<string>>() { AudiosOneshotHeadKill, AudiosHeadKill, AudiosKill });
				}
				else if (crit)
				{
					audio_set = SoundChooser(out audio, new List<List<string>>() { AudiosHeadKill, AudiosKill });
				}
				else
				{
					audio_set = SoundChooser(out audio, new List<List<string>>() { AudiosKill });
				}
			}
			if (audio_set)
			{
				/*
				RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out ChannelGroup channel_group);
				RuntimeManager.CoreSystem.playSound(audio, channel_group, false, out Channel channel);
				channel.setVolume(volume);
				*/
				EventInstance? event_instance = AudioManager.PostCustomSFX(audio);
				event_instance?.setVolume(volume);
			}
			// 图标
			if (crit && !explosion_kill)
			{
				//爆头
				if (skulls.Count < enforce_max_skull_count)
				{
					int overcount = Math.Clamp(skulls.Count - max_skull_count, 0, 114514);
					for (int i = 0; i < overcount; i++)
					{
						int i_anti = skulls.Count - i - 1;
						skulls[i_anti].DisappearRightNow();
					}
					SkullHeadshot.Create(out GameObject skull_game_object, out ISkull skull);
					skull_game_object.transform.SetParent(ui_transform);
					skulls.Insert(0, skull);
				}
			}
			else
			{
				//普通
				if (skulls.Count < enforce_max_skull_count)
				{
					int overcount = Math.Clamp(skulls.Count - max_skull_count, 0, 114514);
					for (int i = 0; i < overcount; i++)
					{
						int i_anti = skulls.Count - i - 1;
						skulls[i_anti].DisappearRightNow();
					}
					SkullNormal.Create(out GameObject skull_game_object, out ISkull skull);
					skull_game_object.transform.SetParent(ui_transform);
					skulls.Insert(0, skull);
				}
			}
		}
		public static bool SoundChooser(out string result, List<List<string>> source_list_and_fallbacks)
		{
			result = "";
			if (!(source_list_and_fallbacks != null && source_list_and_fallbacks.Count > 0))
			{
				return false;
			}
			for (int i = 0; i < source_list_and_fallbacks.Count; i++)
			{
				if (source_list_and_fallbacks[i] != null && source_list_and_fallbacks[i].Count > 0)
				{
					result = source_list_and_fallbacks[i][random.Next(0, source_list_and_fallbacks[i].Count)];
					return true;
				}
			}
			return false;
		}
		private void Awake()
		{
			Instance = this;
			if (Loaded)
			{
				return;
			}
			Localization.LoadLocalization(Path.Combine(Utils.GetDllDirectory(), "Localization"));
			LoadConfig();
			if (LoadImage() && LoadSounds(sfx_namespace))
			{
				if (ModConfigAPI.IsAvailable())
				{
					UnityEngine.Debug.Log("BFKillFeedback: ModConfig可用/ModConfig is available");
					InjectModConfig();
					LoadConfigThroughModConfig();
				}
				
				UnityEngine.Debug.Log("BFKillFeedback: 已载入/Loaded");
				Loaded = true;
			}
			else
			{
				UnityEngine.Debug.LogError("BFKillFeedback: 载入资源时出现问题/Something wrong when loading resources");
			}
		}
		private void OnEnable()
		{
			harmony.PatchAll();
			Health.OnDead += OnDead;
			//ModManager.OnModActivated += ModManager_OnModActivated;
			SceneLoader.onStartedLoadingScene += OnSceneLoading;
			LoadConfig();
			if (ModConfigAPI.IsAvailable())
			{
				LoadConfigThroughModConfig();
			}
		}
		private void OnDisable()
		{
			harmony.UnpatchAll("net.bcasoft.bfkillfeedback");
			Health.OnDead -= OnDead;
			//ModManager.OnModActivated -= ModManager_OnModActivated;
			if (player_character_control != null)
			{
				player_character_control.OnActionStartEvent -= OnActionStart;
			}
		}
		private void OnDestroy()
		{
			if (ui_transform != null)
			{
				UnityEngine.Object.Destroy(ui_transform.gameObject);
			}
		}
		public static void OnSceneLoading(SceneLoadingContext content)
		{
			if (content.sceneName != "Base")
			{
				ClearKillCounter();
			}
			last_scene_name = content.sceneName;
		}

		public static void OnActionStart(CharacterActionBase action)
		{
			UnityEngine.Debug.Log("OnActionStart");
			if (hide_reload_progress_bar && action is CA_Reload)
			{
				// 如果启用了隐藏换弹进度条功能，并且当前捕捉到了换弹动作
				SetProgressBarVisible(false);
			}
			else
			{
				SetProgressBarVisible(true);
			}
		}
		public static void SetProgressBarVisible(bool visible)
		{
			if (action_process_hud == null)
			{
				action_process_hud = UnityEngine.Object.FindObjectOfType<ActionProgressHUD>();
			}
			if (action_process_hud == null)
			{
				return;
			}
			if (visible)
			{
				UnityEngine.Debug.Log("Hide progress");
				action_process_hud.transform.localScale = Vector3.one;
			}
			else
			{
				action_process_hud.transform.localScale = Vector3.zero;
			}
		}

		public static void ClearKillCounter()
		{
			kill_counter.Clear();
		}
		public static int AddNewKillToCounter(string enemy_name)
		{
			if (kill_counter.ContainsKey(enemy_name))
			{
				kill_counter[enemy_name] += 1;
				return kill_counter[enemy_name];
			}
			kill_counter.Add(enemy_name, 1);
			return kill_counter[enemy_name];
		}
		public void LoadConfig()
		{
			// 读取或创建配置文件
			Directory.CreateDirectory(Path.Combine(Application.streamingAssetsPath, "BFKillFeedback"));
			string config_path = Path.Combine(Application.streamingAssetsPath, "BFKillFeedback", "config.json");
			if (File.Exists(config_path))
			{
				string config_content = File.ReadAllText(config_path);
				JObject? config_parsed = JsonConvert.DeserializeObject<JObject>(config_content);
				if (config_parsed != null)
				{
					foreach (JProperty property in config_parsed.Properties())
					{
						if (property.Name == "use_sfx_namespace" && property.Value.Type == JTokenType.String)
						{
							sfx_namespace = property.Value.ToString();
							continue;
						}
						if (property is { Name: "volume", Value: { Type: JTokenType.Float } })
						{
							volume = (float)property.Value;
							continue;
						}
						if (property is { Name: "max_skull_count", Value: { Type: JTokenType.Integer } })
						{
							max_skull_count = (int)property.Value;
							continue;
						}
						if (property is { Name: "enforce_max_skull_count", Value: { Type: JTokenType.Integer } })
						{
							enforce_max_skull_count = (int)property.Value;
							continue;
						}
						if (property is { Name: "disable_icon", Value: { Type: JTokenType.Boolean } })
						{
							disable_icon = (bool)property.Value;
							continue;
						}
						if (property is { Name: "skull_fadein_seconds", Value: { Type: JTokenType.Float } })
						{
							skull_fadein_seconds = (float)property.Value;
							continue;
						}
						if (property is { Name: "skull_stay_seconds", Value: { Type: JTokenType.Float } })
						{
							skull_stay_seconds = (float)property.Value;
							continue;
						}
						if (property is { Name: "skull_fadeout_seconds", Value: { Type: JTokenType.Float } })
						{
							skull_fadeout_seconds = (float)property.Value;
							continue;
						}
						if (property is { Name: "skull_spacing", Value: { Type: JTokenType.Float } })
						{
							skull_spacing = (float)property.Value;
							continue;
						}
						if (property is { Name: "skull_color", Value: { Type: JTokenType.String } })
						{
							string value = property.Value.ToString();
							if (value.Length == 8)
							{
								short r = Convert.ToInt16(value[..2], 16);
								short g = Convert.ToInt16(value.Substring(2, 2), 16);
								short b = Convert.ToInt16(value.Substring(4, 2), 16);
								short a = Convert.ToInt16(value.Substring(6, 2), 16);
								skull_color = new Color((float)r / 255.0f, (float)g / 255.0f, (float)b / 255.0f, (float)a / 255.0f);
							}
							continue;
						}
						if (property is { Name: "headshot_skull_color", Value: { Type: JTokenType.String } })
						{
							string value = property.Value.ToString();
							if (value.Length == 6)
							{
								short r = Convert.ToInt16(value[..2], 16);
								short g = Convert.ToInt16(value.Substring(2, 2), 16);
								short b = Convert.ToInt16(value.Substring(4, 2), 16);
								short a = Convert.ToInt16(value.Substring(6, 2), 16);
								headshot_skull_color = new Color((float)r / 255.0f, (float)g / 255.0f, (float)b / 255.0f, (float)a / 255.0f);
							}
							continue;
						}
						if (property is { Name: "headshot_ring_init_size_rate", Value: { Type: JTokenType.Float } })
						{
							headshot_ring_init_size_rate = (float)property.Value;
							continue;
						}
						if (property is { Name: "headshot_ring_max_size_rate", Value: { Type: JTokenType.Float } })
						{
							headshot_ring_max_size_rate = (float)property.Value;
							continue;
						}
						if (property is { Name: "headshot_ring_bold_alpha_decrease", Value: { Type: JTokenType.Float } })
						{
							headshot_ring_bold_alpha_decrease = (float)property.Value;
							continue;
						}
						if (property is { Name: "headshot_ring_stay_seconds", Value: { Type: JTokenType.Float } })
						{
							headshot_ring_stay_seconds = (float)property.Value;
							continue;
						}
						if (property is { Name: "position_offset_x", Value: { Type: JTokenType.Float } })
						{
							position_offset.x = (float)property.Value;
							continue;
						}
						if (property is { Name: "position_offset_y", Value: { Type: JTokenType.Float } })
						{
							position_offset.y = (float)property.Value;
							continue;
						}
						if (property is { Name: "scale", Value: { Type: JTokenType.Float } })
						{
							scale = (float)property.Value;
							continue;
						}
						if (property is { Name: "addition_scale", Value: { Type: JTokenType.Boolean } })
						{
							addition_scale = (bool)property.Value;
							continue;
						}
						if (property is { Name: "skull_scale_on_drop", Value: { Type: JTokenType.Float } })
						{
							skull_scale_on_drop = (float)property.Value;
							continue;
						}
						if (property is { Name: "disable_text", Value: { Type: JTokenType.Boolean } })
						{
							disable_text = (bool)property.Value;
							continue;
						}
						if (property is { Name: "text_color", Value: { Type: JTokenType.String } })
						{
							string value = property.Value.ToString();
							if (value.Length == 6)
							{
								short r = Convert.ToInt16(value[..2], 16);
								short g = Convert.ToInt16(value.Substring(2, 2), 16);
								short b = Convert.ToInt16(value.Substring(4, 2), 16);
								short a = Convert.ToInt16(value.Substring(6, 2), 16);
								text_color = new Color((float)r / 255.0f, (float)g / 255.0f, (float)b / 255.0f, (float)a / 255.0f);
							}
							continue;
						}
						if (property is { Name: "text_position_offset_x", Value: { Type: JTokenType.Float } })
						{
							text_position_offset.x = (float)property.Value;
							continue;
						}
						if (property is { Name: "text_position_offset_y", Value: { Type: JTokenType.Float } })
						{
							text_position_offset.y = (float)property.Value;
							continue;
						}
						if (property is { Name: "text_scale", Value: { Type: JTokenType.Float } })
						{
							text_scale = (float)property.Value;
							continue;
						}
						if (property is { Name: "text_template", Value: { Type: JTokenType.String } })
						{
							text_template = property.Value.ToString();
							continue;
						}
						if (property is { Name: "text_stay_seconds", Value: { Type: JTokenType.Float } })
						{
							text_stay_seconds = (float)property.Value;
							continue;
						}
						if (property is { Name: "text_memory_length", Value: { Type: JTokenType.Integer } })
						{
							text_memory_length = (int)property.Value;
							continue;
						}
						if (property is { Name: "text_fade_seconds", Value: { Type: JTokenType.Float } })
						{
							text_fade_seconds = (float)property.Value;
							continue;
						}
						if (property is { Name: "disable_score", Value: { Type: JTokenType.Boolean } })
						{
							disable_score = (bool)property.Value;
							continue;
						}
						if (property is { Name: "score_color", Value: { Type: JTokenType.String } })
						{
							string value = property.Value.ToString();
							if (value.Length == 6)
							{
								short r = Convert.ToInt16(value[..2], 16);
								short g = Convert.ToInt16(value.Substring(2, 2), 16);
								short b = Convert.ToInt16(value.Substring(4, 2), 16);
								short a = Convert.ToInt16(value.Substring(6, 2), 16);
								score_color = new Color((float)r / 255.0f, (float)g / 255.0f, (float)b / 255.0f, (float)a / 255.0f);
							}
							continue;
						}
						if (property is { Name: "score_text_position_offset_x", Value: { Type: JTokenType.Float } })
						{
							score_text_position_offset.x = (float)property.Value;
							continue;
						}
						if (property is { Name: "score_text_position_offset_y", Value: { Type: JTokenType.Float } })
						{
							score_text_position_offset.y = (float)property.Value;
							continue;
						}
						if (property is { Name: "score_scale", Value: { Type: JTokenType.Float } })
						{
							score_scale = (float)property.Value;
							continue;
						}
						if (property is { Name: "score_text_number_increase_per_second", Value: { Type: JTokenType.Integer } })
						{
							score_text_number_increase_per_second = (int)property.Value;
							continue;
						}
						if (property is { Name: "score_text_stay_seconds", Value: { Type: JTokenType.Float } })
						{
							score_text_stay_seconds = (float)property.Value;
							continue;
						}
						if (property is { Name: "hide_reload_progress_bar", Value: { Type: JTokenType.Boolean } })
						{
							hide_reload_progress_bar = (bool)property.Value;
							continue;
						}
						if (property is { Name: "disable_vanilla_kill_feedback_sound", Value: { Type: JTokenType.Boolean } })
						{
							disable_vanilla_kill_feedback_sound = (bool)property.Value;
							continue;
						}
					}
				}
				else
				{
					UnityEngine.Debug.LogError("BFKillFeedback: 读取配置文件时出错/Failed to read config file");
				}
			}
			else
			{
				File.WriteAllText(config_path, JsonConvert.SerializeObject(DefaultConfig, Formatting.Indented));
			}
		}
		public void InjectModConfig()
		{
			SortedDictionary<string, object> namespaces = new SortedDictionary<string, object>();
			foreach (string audio_namespace in GetAudioNamespaces())
			{
				namespaces.Add(audio_namespace, audio_namespace);
			}
			ModConfigAPI.SafeAddInputWithSlider(MOD_NAME, "volume", Localization.Tr("settings.volume"), typeof(float), 0.6f, new Vector2(0.0f, 1.0f));
			ModConfigAPI.SafeAddBoolDropdownList(MOD_NAME, "disable_icon", Localization.Tr("settings.disable_icon"), false);
			ModConfigAPI.SafeAddInputWithSlider(MOD_NAME, "scale", Localization.Tr("settings.scale"), typeof(float), 0.7f, new Vector2(0.0f, 3.0f));
			ModConfigAPI.SafeAddInputWithSlider(MOD_NAME, "position_offset_x", Localization.Tr("settings.position_offset_x"), typeof(float), 0.0f, new Vector2(-2.0f, 2.0f));
			ModConfigAPI.SafeAddInputWithSlider(MOD_NAME, "position_offset_y", Localization.Tr("settings.position_offset_y"), typeof(float), -0.6f, new Vector2(-2.0f, 2.0f));
			ModConfigAPI.SafeAddBoolDropdownList(MOD_NAME, "addition_scale", Localization.Tr("settings.addition_scale"), false);
			ModConfigAPI.SafeAddDropdownList(MOD_NAME, "use_sfx_namespace", Localization.Tr("settings.use_sfx_namespace"), namespaces, typeof(string), "bf5");
			ModConfigAPI.SafeAddBoolDropdownList(MOD_NAME, "hide_reload_progress_bar", Localization.Tr("settings.hide_reload_progress_bar"), true);
			ModConfigAPI.SafeAddBoolDropdownList(MOD_NAME, "disable_vanilla_kill_feedback_sound", Localization.Tr("settings.disable_vanilla_kill_feedback_sound"), false);
			ModConfigAPI.SafeAddOnOptionsChangedDelegate(OnModConfigOptionsChanged);
		}
		public void InjectModSetting()
		{
			ModSettingAPI.AddSlider("volume", Localization.Tr("settings.volume"), 0.6f, new Vector2(0f, 1f), value => volume = value, 1, 4);
			ModSettingAPI.AddToggle("disable_icon", Localization.Tr("settings.disable_icon"), false, value => disable_icon = value);
			ModSettingAPI.AddSlider("scale", Localization.Tr("settings.scale"), 0.7f, new Vector2(0f, 3f), value => scale = value, 2, 5);
			ModSettingAPI.AddSlider("position_offset_x", Localization.Tr("settings.position_offset_x"), 0.0f, new Vector2(-2f, 2f), value => position_offset.x = value, 1, 4);
			ModSettingAPI.AddSlider("position_offset_y", Localization.Tr("settings.position_offset_y"), -0.6f, new Vector2(-2f, 2f), value => position_offset.y = value, 1, 4);
			ModSettingAPI.AddToggle("addition_scale", Localization.Tr("settings.addition_scale"), false, value => addition_scale = value);
			ModSettingAPI.AddDropdownList("use_sfx_namespace", Localization.Tr("settings.use_sfx_namespace"), GetAudioNamespaces(), "bf5", OnModSettingSFXNamespaceChanged);
			ModSettingAPI.AddToggle("hide_reload_progress_bar", Localization.Tr("settings.hide_reload_progress_bar"), true, value => hide_reload_progress_bar = value);
			ModSettingAPI.AddToggle("disable_vanilla_kill_feedback_sound", Localization.Tr("settings.disable_vanilla_kill_feedback_sound"), false, value => disable_vanilla_kill_feedback_sound = value);
		}
		public void OnModSettingSFXNamespaceChanged(string newNameSpace)
		{
			if (GetAudioNamespaces().Contains(newNameSpace) && newNameSpace != sfx_namespace)
			{
				sfx_namespace = newNameSpace;
				LoadSounds(sfx_namespace);
			}
		}
		public void OnModConfigOptionsChanged(string key)
		{
			LoadConfigThroughModConfig();
		}
		public void LoadConfigThroughModConfig()
		{
			volume = ModConfigAPI.SafeLoad<float>(MOD_NAME, "volume", 0.6f);
			disable_icon = ModConfigAPI.SafeLoad<bool>(MOD_NAME, "disable_icon", false);
			scale = ModConfigAPI.SafeLoad<float>(MOD_NAME, "scale", 0.7f);
			position_offset.x = ModConfigAPI.SafeLoad<float>(MOD_NAME, "position_offset_x", 0.0f);
			position_offset.y = ModConfigAPI.SafeLoad<float>(MOD_NAME, "position_offset_y", -0.2f);
			addition_scale = ModConfigAPI.SafeLoad<bool>(MOD_NAME, "addition_scale", false);
			hide_reload_progress_bar = ModConfigAPI.SafeLoad<bool>(MOD_NAME, "hide_reload_progress_bar", true);
			disable_vanilla_kill_feedback_sound = ModConfigAPI.SafeLoad<bool>(MOD_NAME, "disable_vanilla_kill_feedback_sound", false);
			string new_namespace = ModConfigAPI.SafeLoad<string>(MOD_NAME, "use_sfx_namespace", "bf5");
			if (GetAudioNamespaces().Contains(new_namespace) && new_namespace != sfx_namespace)
			{
				sfx_namespace = new_namespace;
				LoadSounds(sfx_namespace);
			}
		}
		public void LoadConfigThroughModSetting()
		{
			if (ModSettingAPI.HasConfig())
			{
				volume = ModSettingAPI.GetSavedValue("volume", out float value_volume) ? value_volume : 0.6f;
				disable_icon = ModSettingAPI.GetSavedValue("disable_icon", out bool value_disable_icon) && value_disable_icon;
				scale = ModSettingAPI.GetSavedValue("scale", out float value_scale) ? value_scale : 0.7f;
				position_offset.x = ModSettingAPI.GetSavedValue("position_offset_x", out float value_position_offset_x) ? value_position_offset_x : 0.0f;
				position_offset.y = ModSettingAPI.GetSavedValue("position_offset_y", out float value_position_offset_y) ? value_position_offset_y : 0.0f;
				addition_scale = ModSettingAPI.GetSavedValue("addition_scale", out bool value_addition_scale) && value_addition_scale;
				hide_reload_progress_bar = !ModSettingAPI.GetSavedValue("hide_reload_progress_bar", out bool value_hide_reload_progress_bar) || value_hide_reload_progress_bar;
				disable_vanilla_kill_feedback_sound = ModSettingAPI.GetSavedValue("disable_vanilla_kill_feedback_sound", out bool value_disable_vanilla_kill_feedback_sound) && value_disable_vanilla_kill_feedback_sound;
				string new_namespace = ModConfigAPI.SafeLoad<string>(MOD_NAME, "use_sfx_namespace", "bf5");
				if (GetAudioNamespaces().Contains(new_namespace) && new_namespace != sfx_namespace)
				{
					sfx_namespace = new_namespace;
					LoadSounds(sfx_namespace);
				}
			}
		}
		// 加载资源方法，返回成功与否
		public bool LoadImage()
		{
			GameObject preloader = new GameObject("preloader");
			Image preloader_image = preloader.AddComponent<Image>();
			UnityEngine.Debug.Log("BFKillFeedback: 开始加载图像资源/Starting loading image resources");
			string dll_dir = Path.Combine(Utils.GetDllDirectory(), "Icons");
			string exe_dir = Path.Combine(Application.streamingAssetsPath, "BFKillFeedback", "Icons");
			Directory.CreateDirectory(exe_dir);
			bool success = true;
			UnityEngine.Debug.Log("BFKillFeedback: 正在遍历图像名称列表/Foreaching ImageNames list");
			foreach (string image_name in ImageNames)
			{
				byte[] icon_bytes;
				Texture2D icon_texture;
				string dll_path = Path.Combine(dll_dir, image_name + ".png");
				string exe_path = Path.Combine(exe_dir, image_name + ".png");
				UnityEngine.Debug.Log("BFKillFeedback: Now path is " + dll_path);
				if (File.Exists(exe_path))
				{
					icon_bytes = File.ReadAllBytes(exe_path);
					icon_texture = new Texture2D(2560, 2560);
					if (icon_texture.LoadImage(icon_bytes))
					{
						Images.TryAdd(image_name, Sprite.Create(icon_texture, new Rect(0.0f, 0.0f, icon_texture.width, icon_texture.height), new Vector2(icon_texture.width / 2.0f, icon_texture.height / 2.0f)));
						preloader_image.sprite = Images[image_name];
						success = success && true;
						UnityEngine.Debug.Log("BFKillFeedback: 覆盖纹理加载成功 = " + exe_path);
						continue;
					}
					UnityEngine.Debug.LogError("BFKillFeedback: 加载覆盖纹理失败/Failed to load texture = " + exe_path);
					UnityEngine.Debug.LogError("BFKillFeedback: 将尝试回退到原版纹理/Trying to fallback");
					if (!File.Exists(dll_path))
					{
						UnityEngine.Debug.LogError("BFKillFeedback: 文件不存在 = " + dll_path);
						success = false;
						continue;
					}
					icon_bytes = File.ReadAllBytes(dll_path);
					icon_texture = new Texture2D(2560, 2560);
					if (icon_texture.LoadImage(icon_bytes))
					{
						Images.TryAdd(image_name, Sprite.Create(icon_texture, new Rect(0.0f, 0.0f, icon_texture.width, icon_texture.height), new Vector2(icon_texture.width / 2.0f, icon_texture.height / 2.0f)));
						preloader_image.sprite = Images[image_name];
						success = success && true;
						UnityEngine.Debug.Log("BFKillFeedback: 回退纹理加载成功 = " + dll_path);
						continue;
					}
				}
				else if (!File.Exists(dll_path))
				{
					UnityEngine.Debug.LogError("BFKillFeedback: 文件不存在 = " + dll_path);
					success = false;
					continue;
				}
				icon_bytes = File.ReadAllBytes(dll_path);
				icon_texture = new Texture2D(2560, 2560);
				if (icon_texture.LoadImage(icon_bytes))
				{
					Images.TryAdd(image_name, Sprite.Create(icon_texture, new Rect(0.0f, 0.0f, icon_texture.width, icon_texture.height), new Vector2(icon_texture.width / 2.0f, icon_texture.height / 2.0f)));
					preloader_image.sprite = Images[image_name];
					success = success && true;
					UnityEngine.Debug.Log("BFKillFeedback: 纹理加载成功 = " + dll_path);
					continue;
				}
				success = false;
				UnityEngine.Debug.LogError("BFKillFeedback: 加载纹理失败/Failed to load texture = " + dll_path);
			}
			UnityEngine.Object.Destroy(preloader);
			return success;
		}

		public static List<string> GetAudioNamespaces()
		{
			List<string> namespaces = new List<string>();
			foreach (string dir in Directory.GetDirectories(Path.Combine(Application.streamingAssetsPath, "BFKillFeedback", "AudioNamespaces")))
			{
				string name = Path.GetFileName(dir);
				if (namespaces.Contains(name))
				{
					continue;
				}
				namespaces.Add(name);
			}
			foreach (string dir in Directory.GetDirectories(Path.Combine(Utils.GetDllDirectory(), "AudioNamespaces")))
			{
				string name = Path.GetFileName(dir);
				if (namespaces.Contains(name))
				{
					continue;
				}
				namespaces.Add(name);
			}

			return namespaces;
		}
		public static bool LoadSounds(string the_namespace)
		{
			UnityEngine.Debug.Log("BFKillFeedback: 开始从命名空间" + the_namespace + "加载音频/Start to load audios from namespace " + the_namespace);
			string dll_dir = Path.Combine(Utils.GetDllDirectory(), "AudioNamespaces", the_namespace);
			string exe_dir = Path.Combine(Application.streamingAssetsPath, "BFKillFeedback", "AudioNamespaces", the_namespace);
			Directory.CreateDirectory(exe_dir);
			AudiosKill.Clear();
			AudiosHeadKill.Clear();
			AudiosOneshotHeadKill.Clear();
			AudiosMeleeKill.Clear();
			AudiosCritMeleeKill.Clear();
			AudiosOneshotMeleeKill.Clear();
			AudiosExplosionKill.Clear();
			AudiosOneshotExplosionKill.Clear();
			//加载玩家自己死亡 无法回退
			Utils.LoadSoundWhiler(dll_dir, exe_dir, "death_", ".wav", ref AudiosDeath);
			//加载普通击杀 无法回退
			Utils.LoadSoundWhiler(dll_dir, exe_dir, "kill_", ".wav", ref AudiosKill);
			//加载爆头击杀 回退到 普通击杀
			Utils.LoadSoundWhiler(dll_dir, exe_dir, "headkill_", ".wav", ref AudiosHeadKill);
			//加载一发秒爆头击杀 回退到 爆头击杀
			Utils.LoadSoundWhiler(dll_dir, exe_dir, "oneshotheadkill_", ".wav", ref AudiosOneshotHeadKill);
			//加载近战击杀 回退到 普通击杀
			Utils.LoadSoundWhiler(dll_dir, exe_dir, "meleekill_", ".wav", ref AudiosMeleeKill);
			//加载暴击近战击杀 回退到 近战击杀
			Utils.LoadSoundWhiler(dll_dir, exe_dir, "critmeleekill_", ".wav", ref AudiosCritMeleeKill);
			//加载一发秒近战击杀 回退到 暴击近战击杀 或 近战击杀(无暴击时)
			Utils.LoadSoundWhiler(dll_dir, exe_dir, "onemeleekill_", ".wav", ref AudiosOneshotMeleeKill);
			//加载爆炸击杀 回退到 普通击杀
			Utils.LoadSoundWhiler(dll_dir, exe_dir, "explosionkill_", ".wav", ref AudiosExplosionKill);
			//加载一发秒爆炸击杀 回退到 爆炸击杀
			Utils.LoadSoundWhiler(dll_dir, exe_dir, "oneexplosionkill_", ".wav", ref AudiosOneshotExplosionKill);
			return true;
		}
		// 创建UI
		public void CreateUI()
		{
			HUDManager hud_manager = UnityEngine.Object.FindObjectOfType<HUDManager>();
			if (hud_manager == null)
			{
				return;
			}
			GameObject game_object = new GameObject("BFKillFeedbackUI");
			ui_transform = game_object.AddComponent<RectTransform>();
			ui_transform.SetParent(hud_manager.transform);
			GameObject text_game_object = new GameObject("Text");
			ui_text_transform = text_game_object.AddComponent<RectTransform>();
			ui_text_transform.SetParent(ui_transform);
			ui_text_transform.localPosition = Vector3.zero;
			ui_text = text_game_object.AddComponent<TextMeshProUGUI>();
			ui_text.alignment = TextAlignmentOptions.Center;
			GameObject score_game_object = new GameObject("Score");
			ui_score_transform = score_game_object.AddComponent<RectTransform>();
			ui_score_transform.SetParent(ui_transform);
			ui_score_transform.localPosition = Vector3.zero;
			ui_score = score_game_object.AddComponent<TextMeshProUGUI>();
			ui_score.alignment = TextAlignmentOptions.Center;
			UnityEngine.Debug.Log("BFKillFeedback: 已创建UI/UI created");
		}
		private void ModManager_OnModActivated(ModInfo arg1, Duckov.Modding.ModBehaviour arg2) {
			if (arg1.name != ModSettingAPI.MOD_NAME || !ModSettingAPI.Init(info)) return;
			//(触发时机:此mod在ModSetting之前启用)检查启用的mod是否是ModSetting,是进行初始化
			//先从ModSetting中读取配置
			LoadConfigThroughModSetting();
			InjectModSetting();
		}
		protected override void OnAfterSetup() {
			//(触发时机:ModSetting在此mod之前启用)此mod，Setup后,尝试进行初始化
			if (!ModSettingAPI.Init(info)) return;
			//先从ModSetting中读取配置
			LoadConfigThroughModSetting();
			InjectModSetting();
		}
	}
}
