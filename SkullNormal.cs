using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace BFKillFeedback
{
	public class SkullNormal : ISkull
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
				//UnityEngine.Debug.Log("WantDestroy set\n" + Environment.StackTrace);
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
				//UnityEngine.Debug.Log("SpawnTime set\n" + Environment.StackTrace);
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
		Image? skull_image;
		public void UpdateAlpha(float now_time)
		{
			float delta = now_time - SpawnTime;
			float process;
			float process_width;
			float scale = 1.0f;
			if (delta < ModBehaviour.skull_fadein_seconds)
			{
				//淡入
				process = Math.Clamp(delta / ModBehaviour.skull_fadein_seconds, 0.0f, 1.0f);
				process_width = process;
				scale = ModBehaviour.skull_scale_on_drop / Math.Clamp(process * ModBehaviour.skull_scale_on_drop, 1.0f, ModBehaviour.skull_scale_on_drop);
			}
			else if (delta > (ModBehaviour.skull_fadein_seconds + ModBehaviour.skull_spacing))
			{
				//淡出
				process = 1.0f - Math.Clamp((delta - ModBehaviour.skull_fadein_seconds - ModBehaviour.skull_stay_seconds) / ModBehaviour.skull_fadeout_seconds, 0.0f, 1.0f);
				process_width = 1.0f - Math.Clamp((delta - ModBehaviour.skull_fadein_seconds - ModBehaviour.skull_stay_seconds - ModBehaviour.skull_fadeout_seconds) / ModBehaviour.skull_fadein_seconds, 0.0f, 1.0f);
			}
			else
			{
				//留置
				process = 1.0f;
				process_width = 1.0f;
			}
			// 不透明度计算
			if (skull_image != null)
			{
				skull_image.color = new Color(skull_image.color.r, skull_image.color.g, skull_image.color.b, ModBehaviour.skull_color.a * process);
				if (delta > (2.0f * ModBehaviour.skull_fadein_seconds + ModBehaviour.skull_stay_seconds + ModBehaviour.skull_fadeout_seconds))
				{
					want_destroy = true;
				}
			}
			if (rect_transform != null && skull_image != null)
			{
				effect_width = rect_transform.rect.width * process_width * (1.0f + ModBehaviour.skull_spacing * 2.0f);
				rect_transform.localScale = new Vector3(scale, scale);
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
		public static bool Create(out GameObject game_object, out ISkull skull)
		{
			game_object = new GameObject("SkullNormal");
			SkullNormal skull_normal = new SkullNormal(Time.time, game_object)
			{
				rect_transform = game_object.AddComponent<RectTransform>(),
				skull_image = game_object.AddComponent<Image>()
			};
			skull_normal.skull_image.color = ModBehaviour.skull_color;
			skull = skull_normal;
			if (ModBehaviour.ImageNames.Contains("kill"))
			{
				skull_normal.skull_image.sprite = ModBehaviour.Images["kill"];
				skull_normal.skull_image.type = Image.Type.Simple;
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
		public SkullNormal(float new_spawn_time, GameObject new_game_object)
		{
			spawn_time = new_spawn_time;
			game_object = new_game_object;
		}
	}
}
