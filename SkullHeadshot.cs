using System;
using UnityEngine;

namespace BFKillFeedback
{
	public class SkullHeadshot : ISkull
	{
		public bool WantDestroy
		{
			get
			{
				return want_destroy;
			}
			set
			{
				want_destroy = value;
			}
		}
		public float SpawnTime
		{
			get
			{
				return spawn_time;
			}
			set
			{
				spawn_time = value;
			}
		}
		public GameObject? TheGameObject
		{
			get
			{
				return game_object;
			}
			set
			{
				game_object = value;
			}
		}
		public float EffectWidth
		{
			get
			{
				return effect_width;
			}
			set
			{
				effect_width = value;
			}
		}
		public bool want_destroy = false;
		public float spawn_time;
		public GameObject? game_object;
		public float effect_width; //用于在淡入淡出过程中改变自身的被用于所有骷髅头位置计算的宽度数值，使得所有骷髅头得以自如地计算位置
		RectTransform? rect_transform;
		UnityEngine.UI.Image? skull_image;
		UnityEngine.UI.Image? ring_thin_image;
		UnityEngine.UI.Image? ring_bold_image;
		RectTransform? ring_transform;
		public void UpdateAlpha(float now_time)
		{
			float delta = now_time - SpawnTime;
			float process;
			float process_ring; //表示圆圈的进度条，0为开始，1为结束
			float process_width;
			float scale = 1.0f;
			if (delta < ModBehaviour.skull_fadein_seconds)
			{
				//淡入
				process = Math.Clamp(delta / ModBehaviour.skull_fadein_seconds, 0.0f, 1.0f);
				process_width = process;
				scale = ModBehaviour.skull_scale_on_drop / Math.Clamp(process * ModBehaviour.skull_scale_on_drop, 1.0f, ModBehaviour.skull_scale_on_drop);
				process_ring = 1.0f;
			}
			else if (delta > (ModBehaviour.skull_fadein_seconds + ModBehaviour.skull_spacing))
			{
				//淡出
				process = 1.0f - Math.Clamp((delta - ModBehaviour.skull_fadein_seconds - ModBehaviour.skull_stay_seconds) / ModBehaviour.skull_fadeout_seconds, 0.0f, 1.0f);
				process_width = 1.0f - Math.Clamp((delta - ModBehaviour.skull_fadein_seconds - ModBehaviour.skull_stay_seconds - ModBehaviour.skull_fadeout_seconds) / ModBehaviour.skull_fadein_seconds, 0.0f, 1.0f);
				if (ModBehaviour.headshot_ring_stay_seconds == 0.0f)
				{
					process_ring = 1.0f;
				}
				else
				{
					process_ring = Math.Clamp((delta - ModBehaviour.skull_fadein_seconds) / (ModBehaviour.headshot_ring_stay_seconds), 0.0f, 1.0f);
				}
			}
			else
			{
				//留置
				process = 1.0f;
				process_width = 1.0f;
				if (ModBehaviour.headshot_ring_stay_seconds == 0.0f)
				{
					process_ring = 1.0f;
				}
				else
				{
					process_ring = Math.Clamp((delta - ModBehaviour.skull_fadein_seconds) / (ModBehaviour.headshot_ring_stay_seconds), 0.0f, 1.0f);
				}
			}
			// 不透明度计算
			if (ring_thin_image != null && ring_bold_image != null)
			{
				ring_thin_image.color = new Color(ring_thin_image.color.r, ring_thin_image.color.g, ring_thin_image.color.b, ModBehaviour.headshot_skull_color.a * (1.0f - process_ring));
				ring_bold_image.color = new Color(ring_bold_image.color.r, ring_bold_image.color.g, ring_bold_image.color.b, Math.Clamp(ModBehaviour.headshot_skull_color.a * (1.0f - process_ring) - ModBehaviour.headshot_ring_bold_alpha_decrease, 0.0f, 1.0f));
			}
			if (skull_image != null)
			{
				skull_image.color = new Color(skull_image.color.r, skull_image.color.g, skull_image.color.b, ModBehaviour.headshot_skull_color.a * process);
				if (delta > (2.0f * ModBehaviour.skull_fadein_seconds + ModBehaviour.skull_stay_seconds + ModBehaviour.skull_fadeout_seconds))
				{
					want_destroy = true;
				}
			}
			if (rect_transform != null)
			{
				effect_width = rect_transform.rect.width * process_width * (1.0f + ModBehaviour.skull_spacing * 2.0f);
				rect_transform.localScale = new Vector3(scale, scale);
			}
			if (ring_transform != null)
			{
				float ring_scale = ModBehaviour.headshot_ring_init_size_rate + process_ring * (ModBehaviour.headshot_ring_max_size_rate - ModBehaviour.headshot_ring_init_size_rate);
				ring_transform.localScale = new Vector3(ring_scale, ring_scale);
			}
		}
		public void UpdatePosition(float now_time, float total_width, int index)
		{
			// 位置计算
			if (rect_transform != null)
			{
				float left_side_width = 0.0f; //左侧所有骷髅头的效果宽度加上自身的左侧宽度和半个图标宽度
				for (int i = 0; i < ModBehaviour.skulls.Count && i < index; i++)
				{
					left_side_width += ModBehaviour.skulls[i].EffectWidth;
				}
				left_side_width += effect_width / 2.0f;
				rect_transform.localPosition = new Vector3(left_side_width - total_width / 2.0f, 0.0f, 0.0f);
			}
		}
		public static bool Create(out GameObject gameObject, out ISkull skull)
		{
			gameObject = new GameObject("SkullHeadshot");
			SkullHeadshot skull_headshot = new SkullHeadshot(Time.time, gameObject)
			{
				rect_transform = gameObject.AddComponent<RectTransform>(),
				skull_image = gameObject.AddComponent<UnityEngine.UI.Image>(),
			};
			GameObject ring_object = new GameObject("Ring");
			GameObject ring_thin = new GameObject("RingThin");
			GameObject ring_bold = new GameObject("RingBold");
			skull_headshot.ring_transform = ring_object.AddComponent<RectTransform>();
			skull_headshot.ring_transform.SetParent(skull_headshot.rect_transform);
			RectTransform ring_transform_thin = ring_thin.AddComponent<RectTransform>();
			ring_transform_thin.SetParent(ring_object.transform);
			RectTransform ring_transform_bold = ring_bold.AddComponent<RectTransform>();
			ring_transform_bold.SetParent(ring_object.transform);
			skull_headshot.ring_thin_image = ring_thin.AddComponent<UnityEngine.UI.Image>();
			skull_headshot.ring_bold_image = ring_bold.AddComponent<UnityEngine.UI.Image>();
			skull_headshot.ring_transform.localScale = new Vector3(0.0f, 0.0f);
			skull_headshot.skull_image.color = ModBehaviour.headshot_skull_color;
			skull_headshot.ring_thin_image.color = ModBehaviour.headshot_skull_color;
			skull_headshot.ring_bold_image.color = ModBehaviour.headshot_skull_color;
			skull = skull_headshot;
			if (ModBehaviour.Images.ContainsKey("headkill"))
			{
				skull_headshot.skull_image.sprite = ModBehaviour.Images["headkill"];
				skull_headshot.skull_image.type = UnityEngine.UI.Image.Type.Simple;
			}
			else
			{
				UnityEngine.Debug.LogError("BFKillFeedback: 缺少需要使用的图像资源/Missing needed image resource");
				return false;
			}
			if (ModBehaviour.Images.ContainsKey("headkill_frame_thin") && ModBehaviour.Images.ContainsKey("headkill_frame_bold"))
			{
				skull_headshot.ring_thin_image.sprite = ModBehaviour.Images["headkill_frame_thin"];
				skull_headshot.ring_thin_image.type = UnityEngine.UI.Image.Type.Simple;
				skull_headshot.ring_bold_image.sprite = ModBehaviour.Images["headkill_frame_bold"];
				skull_headshot.ring_bold_image.type = UnityEngine.UI.Image.Type.Simple;
			}
			else
			{
				UnityEngine.Debug.LogError("BFKillFeedback: 缺少需要使用的图像资源/Missing needed image resource");
				return false;
			}
			return true;
		}
		public void Destroy()
		{
			//UnityEngine.Debug.Log("SN: on destroy call, id=" + id.ToString());
			if (game_object != null) {
				UnityEngine.Object.Destroy(game_object);
				return;
			}
			//UnityEngine.Debug.LogWarning("BFKillFeedback: 丢失对GameObject的引用而无法将其删除/Failed to destroy GameObject because the reference was lost");
		}
		public void DisappearRightNow()
		{
			if ((Time.time - SpawnTime) < (ModBehaviour.skull_fadein_seconds + ModBehaviour.skull_stay_seconds)){
				SpawnTime = Time.time - ModBehaviour.skull_fadein_seconds - ModBehaviour.skull_stay_seconds;
			}
		}
		public SkullHeadshot(float new_spawn_time, GameObject new_game_object)
		{
			spawn_time = new_spawn_time;
			game_object = new_game_object;
		}
	}
}
