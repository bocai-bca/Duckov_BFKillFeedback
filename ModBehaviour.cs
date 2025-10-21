using FMOD;
using FMODUnity;
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BFKillFeedback
{
	public class ModBehaviour : Duckov.Modding.ModBehaviour
	{
		public static bool Loaded = false;
		public static Dictionary<string, object> DefaultConfig = new Dictionary<string, object>(){
			{"volume", 0.5f}, //音量
			{"max_skull_count", 10}, //最大骷髅头数
			{"enforce_max_skull_count", 15}, //强制最大骷髅头数
			{"disable_icon", false}, //禁用图标
			{"skull_fadein_seconds", 0.25f}, //骷髅头淡入时间
			{"skull_stay_seconds", 3.0f}, //骷髅头留置时间
			{"skull_fadeout_seconds", 0.5f}, //骷髅头淡出时间
			{"skull_spacing", 0.5f}, //骷髅头之间的间距乘数，基于骷髅头纹理的宽度
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
			"bf1_headkill",
			"bf1_headkill_oneshot",
			"bf5_"
		};
		// 从硬盘加载到内存的图片资源
		public static Dictionary<string, Texture2D> Images = new Dictionary<string, Texture2D>();
		// 从硬盘加载到内存的音频资源
		//public static Dictionary<string, AudioClip> KillFeedbackAudios = new Dictionary<string, AudioClip>();
		public static Dictionary<string, Sound> Audios = new Dictionary<string, Sound>();
		public static RectTransform? ui_transform;
		// 配置文件-音量，0-1决定音量大小
		public static float volume = 0.5f;
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
		public static float skull_spacing = 0.5f;
		public static List<SkullBase> skulls = new List<SkullBase>();
		private void Update()
		{
			List<int> skulls_want_destroy = new List<int>();
			for (int i = 0; i < skulls.Count; i++)
			{
				skulls[i].Update(Time.time, new SkullUpdateData(skulls.Count, i, 1024.0f, 512.0f));
				if (skulls[i].want_destroy)
				{
					skulls_want_destroy.Add(i);
				}
			}
			for (int i = skulls_want_destroy.Count - 1; i >= 0; i--)
			{
				skulls[i].Destroy();
				skulls.RemoveAt(i);
			}
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
		public void PlayKill(bool headshot, bool oneshotkill)
		{
			if (ui_transform == null)
			{
				CreateUI();
			}
			// 确定使用的资源
			
			Sound audio = new Sound();
			
			// 应用资源
			ChannelGroup channel_group;
			RuntimeManager.GetBus("bus:/Master/SFX").getChannelGroup(out channel_group);
			Channel channel;
			RuntimeManager.CoreSystem.playSound(audio, channel_group, false, out channel);
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
				Sound sound;
				RESULT fmod_create_result = RuntimeManager.CoreSystem.createSound(this_path, MODE.LOOP_OFF, out sound);
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
			Sound sound;
			RESULT fmod_result = RuntimeManager.CoreSystem.createSound(path, MODE.LOOP_OFF, out sound);
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
			UnityEngine.Debug.Log("BFKillFeedback: 已创建UI/UI created");
		}

		public struct SkullUpdateData
		{
			public int total_count_now;
			public int this_index;
			public float skull_width;
			public float skull_spacing;
			public SkullUpdateData(int new_total_count_now, int new_this_index, float new_skull_width, float new_skull_spacing)
			{
				total_count_now = new_total_count_now;
				this_index = new_this_index;
				skull_width = new_skull_width;
				skull_spacing = new_skull_spacing;
			}
		}
	}
}
