using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace BFKillFeedback
{
	public class SkullNormal : SkullBase
	{
		RectTransform? rect_transform;
		Image? skull_image;
		public override void Update(float now_time, ModBehaviour.SkullUpdateData update_data)
		{
			
		}
		public override bool Create(out GameObject gameObject, out SkullBase skull)
		{
			gameObject = new GameObject("SkullNormal");
			rect_transform = gameObject.AddComponent<RectTransform>();
			skull = new SkullNormal(Time.time, gameObject);
			skull_image = gameObject.AddComponent<Image>();
			if (ModBehaviour.ImageNames.Contains("kill"))
			{
				skull_image.sprite = Sprite.Create(ModBehaviour.Images["kill"], new Rect(0.0f, 0.0f, 1024.0f, 1024.0f), new Vector2(512.0f, 512.0f));
			}
			return true;
		}
		public override void Destroy()
		{
			if (game_object != null) {
				UnityEngine.Object.Destroy(game_object);
			}
		}
		public override void DisappearRightNow()
		{
			if ((Time.time - spawn_time) < (ModBehaviour.skull_fadein_seconds + ModBehaviour.skull_stay_seconds)){
				spawn_time = Time.time - ModBehaviour.skull_fadein_seconds - ModBehaviour.skull_stay_seconds;
			}
		}
		public SkullNormal(float new_spawn_time, GameObject new_game_object)
		{
			spawn_time = new_spawn_time;
			game_object = new_game_object;
		}
	}
}
