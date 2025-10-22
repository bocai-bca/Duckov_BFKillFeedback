using FMOD;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Mathematics;

namespace BFKillFeedback
{
	public class ModBehaviour : Duckov.Modding.ModBehaviour
	{
		public static bool Loaded = false;
		public static Dictionary<string, object> DefaultConfig = new Dictionary<string, object>(){
			{"use_bf5_sfx", false}, //使用BF5音效
			{"volume", 0.5f}, //音量
			{"alpha", 0.75f }, //不透明度
			{"max_skull_count", 10}, //最大骷髅头数
			{"enforce_max_skull_count", 15}, //强制最大骷髅头数
			{"disable_icon", false}, //禁用图标
			{"skull_fadein_seconds", 0.25f}, //骷髅头淡入时间
			{"skull_stay_seconds", 3.0f}, //骷髅头留置时间
			{"skull_fadeout_seconds", 0.5f}, //骷髅头淡出时间
			{"skull_spacing", 0.3f}, //骷髅头之间的间距乘数，基于骷髅头Transform的宽度，实际的间距是两个骷髅头的一左一右间距相加
			{"skull_color", "FFFFFF"}, //普通骷髅头颜色
			{"headshot_skull_color", "FF8C00"}, //爆头骷髅头颜色
			{"position_offset_x", 0.0f}, //坐标偏移X
			{"position_offset_y", 0.0f}, //坐标偏移Y
			{"scale", 1.0f}, //缩放倍率
			{"skull_scale_on_drop", 1.3f}, //图标在初始化时的尺寸乘数，基于正常尺寸，用于图标掉入效果
		};
		public static ModBehaviour? Instance;
		// 所有图标名称，这些名称将用于拼接成定位图片资源的路径(.png)
		public static readonly string[] ImageNames = new string[]
		{
			"kill",
			"headkill",
			"headkill_frame"
		};
		// 所有音频名称，这些名称将用于拼接成定位音频资源的路径(.wav)
		public static readonly string[] AudioNames = new string[]
		{
			"bf1_kill",
			"bf1_headkill_0",
			"bf1_headkill_1",
			"bf1_headkill_2",
			"bf1_headkill_3",
			"bf1_headkill_4",
			"bf5_kill",
			"bf5_headkill_0",
			"bf5_headkill_1",
			"bf5_headkill_2",
			"bf5_headkill_3",
			"bf5_headkill_4",
		};
		// 从硬盘加载到内存的图片资源
		public static Dictionary<string, Texture2D> Images = new Dictionary<string, Texture2D>();
		// 从硬盘加载到内存的音频资源
		//public static Dictionary<string, AudioClip> KillFeedbackAudios = new Dictionary<string, AudioClip>();
		public static Dictionary<string, Sound> Audios = new Dictionary<string, Sound>();
		public static RectTransform? ui_transform;
		// 配置文件-音量，0-1决定音量大小
		public static float volume = 0.5f;
		// 配置文件-不透明度，0-1决定不透明度
		public static float alpha = 0.75f;
		// 配置文件-允许同时存在的最大骷髅头数，超过此数时添加新骷髅头将导致旧骷髅头立即开始消失
		public static int max_skull_count = 10;
		// 配置文件-强制允许同时存在的最大骷髅头数，超过此数时击杀敌人不会添加新骷髅头
		public static int enforce_max_skull_count = 15;
		// 配置文件-禁用图标，为true时不显示图标
		public static bool disable_icon = false;
		// 配置文件-骷髅头淡入时间
		public static float skull_fadein_seconds = 0.25f;
		// 配置文件-骷髅头滞留时间
		public static float skull_stay_seconds = 3.0f;
		// 配置文件-骷髅头淡出时间
		public static float skull_fadeout_seconds = 0.5f;
		// 配置文件-骷髅头间距乘数
		public static float skull_spacing = 0.0f;
		// 配置文件-普通骷髅头颜色
		public static Color skull_color = Color.white;
		// 配置文件-爆头骷髅头颜色
		public static Color headshot_skull_color = new Color(1.0f, 0.55f, 0.0f);
		// 配置文件-坐标偏移
		public static Vector2 position_offset = Vector2.zero;
		// 配置文件-缩放倍率
		public static float scale = 1.0f;
		// 配置文件-使用BF5音效
		public static bool use_bf5_sfx = false;
		// 配置文件-图标在初始化时的尺寸
		public static float skull_scale_on_drop = 2.0f;
		public static int skull_id_counter = 0;
		public static List<ISkull> skulls = new List<ISkull>();
		public static System.Random random = new System.Random(System.DateTime.Now.Second);
		private void Update()
		{
			//UnityEngine.Debug.Log(" ");
			//UnityEngine.Debug.Log("======NEW FRAME======");
			List<int> skulls_want_destroy = new List<int>();
			float total_width = 0.0f;
			for (int i = 0; i < skulls.Count; i++)
			{
				//UnityEngine.Debug.Log("MB: call skull update alpha, index=" + i.ToString());
				skulls[i].UpdateAlpha(Time.time);
				total_width += skulls[i].EffectWidth;
			}
			for (int i = 0; i < skulls.Count; i++)
			{
				//UnityEngine.Debug.Log("MB: call skull update position, index=" + i.ToString());
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
			//UnityEngine.Debug.Log("=====================");
		}
		public void OnDead(Health health, DamageInfo damageInfo)
		{
			// 防空引用
			if (health == null)
			{
				return;
			}
			// 如果伤害来自玩家队
			if (damageInfo.fromCharacter.Team == Teams.player)
			{
				bool headshot = damageInfo.crit > 0;
				bool oneshotkill = damageInfo.finalDamage >= health.MaxHealth * 0.9f;
				PlayKill(headshot, oneshotkill);
			}
		}
		public void PlayKill(bool headshot, bool use_bf5_sfx)
		{
			if (ui_transform == null)
			{
				CreateUI();
			}
			else
			{
				ui_transform.localPosition = Vector3.zero;
			}
			// 确定使用的资源
			Sound audio;
			string audio_key = "bf";
			if (use_bf5_sfx)
			{
				audio_key += "5_";
			}
			else
			{
				audio_key += "1_";
			}
			if (headshot)
			{
				//爆头
				audio_key += "headkill_" + random.Next(0, 4).ToString();
				
			}
			else
			{
				//普通
				audio_key += "kill";
				SkullNormal.Create(out GameObject gameObject, out ISkull skull, skull_id_counter);
				skull_id_counter++;
				gameObject.transform.SetParent(ui_transform);
				skulls.Insert(0, skull);
			}
			audio = Audios[audio_key];
			// 应用资源
			RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out ChannelGroup channel_group);
			RuntimeManager.CoreSystem.playSound(audio, channel_group, false, out Channel channel);
			channel.setVolume(volume);
		}
		private void Awake()
		{
			Instance = this;
			if (Loaded)
			{
				return;
			}
			if (LoadRes())
			{
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
			Health.OnDead += OnDead;
			// 读取或创建配置文件
			string config_path = Path.Combine(Application.streamingAssetsPath, "BFKillFeedback.cfg");
			if (File.Exists(config_path))
			{
				string config_content = File.ReadAllText(config_path);
				JObject? config_parsed = JsonConvert.DeserializeObject<JObject>(config_content);
				if (config_parsed != null)
				{
					foreach (JProperty property in config_parsed.Properties())
					{
						if (property.Name == "volume" && property.Value.Type == JTokenType.Float)
						{
							volume = (float)property.Value;
							continue;
						}
						if (property.Name == "alpha" && property.Value.Type == JTokenType.Float)
						{
							alpha = (float)property.Value;
							continue;
						}
						if (property.Name == "max_skull_count" && property.Value.Type == JTokenType.Integer)
						{
							max_skull_count = (int)property.Value;
							continue;
						}
						if (property.Name == "enforce_max_skull_count" && property.Value.Type == JTokenType.Integer)
						{
							enforce_max_skull_count = (int)property.Value;
							continue;
						}
						if (property.Name == "disable_icon" && property.Value.Type == JTokenType.Boolean)
						{
							disable_icon = (bool)property.Value;
							continue;
						}
						if (property.Name == "skull_fadein_seconds" && property.Value.Type == JTokenType.Float)
						{
							skull_fadein_seconds = (float)property.Value;
							continue;
						}
						if (property.Name == "skull_stay_seconds" && property.Value.Type == JTokenType.Float)
						{
							skull_stay_seconds = (float)property.Value;
							continue;
						}
						if (property.Name == "skull_fadeout_seconds" && property.Value.Type == JTokenType.Float)
						{
							skull_fadeout_seconds = (float)property.Value;
							continue;
						}
						if (property.Name == "skull_spacing" && property.Value.Type == JTokenType.Float)
						{
							skull_spacing = (float)property.Value;
							continue;
						}
						if (property.Name == "skull_color" && property.Value.Type == JTokenType.String)
						{
							string value = property.Value.ToString();
							if (value.Length == 6)
							{
								short r = Convert.ToInt16(value[..2], 16);
								short g = Convert.ToInt16(value.Substring(2, 2), 16);
								short b = Convert.ToInt16(value.Substring(4, 2), 16);
								skull_color = new Color((float)r / 255.0f, (float)g / 255.0f, (float)b / 255.0f);
							}
							continue;
						}
						if (property.Name == "headshot_skull_color" && property.Value.Type == JTokenType.String)
						{
							string value = property.Value.ToString();
							if (value.Length == 6)
							{
								short r = Convert.ToInt16(value[..2], 16);
								short g = Convert.ToInt16(value.Substring(2, 2), 16);
								short b = Convert.ToInt16(value.Substring(4, 2), 16);
								headshot_skull_color = new Color((float)r / 255.0f, (float)g / 255.0f, (float)b / 255.0f);
							}
							continue;
						}
						if (property.Name == "position_offset_x" && property.Value.Type == JTokenType.Float)
						{
							position_offset.x = (float)property.Value;
							continue;
						}
						if (property.Name == "position_offset_y" && property.Value.Type == JTokenType.Float)
						{
							position_offset.y = (float)property.Value;
							continue;
						}
						if (property.Name == "scale" && property.Value.Type == JTokenType.Float)
						{
							scale = (float)property.Value;
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
				File.WriteAllText(config_path, Newtonsoft.Json.JsonConvert.SerializeObject(DefaultConfig, Formatting.Indented));
			}
		}
		private void OnDisable()
		{
			Health.OnDead -= OnDead;
		}
		private void OnDestroy()
		{
			if (ui_transform != null)
			{
				UnityEngine.Object.Destroy(ui_transform.gameObject);
			}
		}
		// 加载资源方法，返回成功与否
		public bool LoadRes()
		{
			UnityEngine.Debug.Log("BFKillFeedback: 开始加载资源/Starting loading resources");
			bool success = true;
			string absolute_path = Utils.GetDllDirectory();
			UnityEngine.Debug.Log("BFKillFeedback: Absolute path = " + absolute_path);
			UnityEngine.Debug.Log("BFKillFeedback: 正在遍历图像名称列表/Foreaching ImageNames list");
			foreach (string image_name in ImageNames)
			{
				string this_path = Path.Combine(absolute_path, image_name + ".png");
				UnityEngine.Debug.Log("BFKillFeedback: Now path is " + this_path);
				if (!File.Exists(this_path))
				{
					UnityEngine.Debug.LogError("BFKillFeedback: 文件不存在 = " + this_path);
					success = false;
					continue;
				}
				byte[] icon_bytes = File.ReadAllBytes(this_path);
				Texture2D icon_texture = new Texture2D(256, 256);
				if (icon_texture.LoadImage(icon_bytes))
				{
					Images.TryAdd(image_name, icon_texture);
					success = success && true;
					UnityEngine.Debug.Log("BFKillFeedback: 纹理加载成功 = " + this_path);
					continue;
				}
				success = false;
				UnityEngine.Debug.LogError("BFKillFeedback: 加载纹理失败/Failed to load texture = " + this_path);
			}
			UnityEngine.Debug.Log("BFKillFeedback: 正在遍历音频名称列表/Foreaching AudioNames list");
			foreach (string audio_name in AudioNames)
			{
				string this_path = Path.Combine(absolute_path, audio_name + ".wav");
				UnityEngine.Debug.Log("BFKillFeedback: Now path is " + this_path);
				if (!File.Exists(this_path))
				{
					UnityEngine.Debug.LogError("BFKillFeedback: 文件不存在 = " + this_path);
					success = false;
					continue;
				}
				RESULT fmod_create_result = RuntimeManager.CoreSystem.createSound(this_path, MODE.LOOP_OFF, out Sound sound);
				if (fmod_create_result == RESULT.OK)
				{
					Audios.TryAdd(audio_name, sound);
					success = success && true;
					UnityEngine.Debug.Log("BFKillFeedback: 成功加载音频 = " + this_path);
				}
				else
				{
					UnityEngine.Debug.LogError("BFKillFeedback: 加载音频时出错 = " + fmod_create_result.ToString());
					success = false;
				}
			}
			//加载资源覆盖
			string config_path = Path.Combine(Application.streamingAssetsPath, "BFKillFeedback");
			if (Directory.Exists(config_path)) {
				string[] files = Directory.GetFiles(config_path);
				foreach (string file in files){
					if (Audios.ContainsKey(Path.GetFileNameWithoutExtension(file))){
						
					}
				}
			}
			return success;
		}
		public static bool LoadSound(string path, string audio_name)
		{
			RESULT fmod_result = RuntimeManager.CoreSystem.createSound(path, MODE.LOOP_OFF, out Sound sound);
			if (fmod_result == RESULT.OK)
			{
				Audios.TryAdd(audio_name, sound);
				UnityEngine.Debug.Log("BFKillFeedback: 成功加载音频 = " + path);
				return true;
			}
			UnityEngine.Debug.LogError("BFKillFeedback: 加载音频时出错 = " + fmod_result.ToString());
			return false;
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
			ui_transform.localPosition = Vector3.zero;
			UnityEngine.Debug.Log("BFKillFeedback: 已创建UI/UI created");
		}
	}
}
